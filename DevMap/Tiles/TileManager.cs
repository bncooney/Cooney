using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using DevMap.Camera;
using DevMap.Util;

namespace DevMap.Tiles;

public class TileManager(GraphicsDevice device) : IDisposable
{
	private readonly TileFetcher _fetcher = new();
	private readonly TileCache _cache = new();
	private readonly TileAtlas _atlas = new(device);
	private readonly VirtualTileAtlas _virtualAtlas = new(device);
	private readonly ConcurrentQueue<(TileCoordinate coord, byte[] data)> _completedTiles = new();
	private readonly CancellationTokenSource _shutdownCts = new();

	// Snapshot-based queue: main thread publishes, workers consume
	private volatile List<TileCoordinate> _desiredTiles = [];
	private readonly ManualResetEventSlim _snapshotReady = new(false);
	private readonly ConcurrentDictionary<TileCoordinate, byte> _inflight = new();
	private bool _workersStarted;

	private int _currentZoom = -1;
	private int _lastFlatCameraVersion = -1;

	private const int MaxBlitsPerFrame = 4;
	private const int WorkerCount = 4;

	public Texture2D? AtlasTexture => _atlas.AtlasTexture;
	public VirtualTileAtlas VirtualAtlas => _virtualAtlas;

	public void Update(ArcballCamera camera, Viewport viewport)
	{
		EnsureWorkersStarted();

		int desiredZoom = MercatorProjection.ZoomFromDistance(camera.Distance, camera.FieldOfView, viewport.Height);

		if (desiredZoom != _currentZoom)
		{
			_currentZoom = desiredZoom;
			_atlas.SetZoom(desiredZoom);
			PublishGlobeTiles(desiredZoom);
		}

		// Process completed downloads on the main thread
		int blits = 0;
		while (blits < MaxBlitsPerFrame && _completedTiles.TryDequeue(out var completed))
		{
			if (completed.coord.Zoom == _currentZoom)
			{
				_atlas.BlitTile(completed.coord, completed.data);
				blits++;
			}
		}
	}

	private void PublishGlobeTiles(int zoom)
	{
		int count = MercatorProjection.TileCount(zoom);
		var needed = new List<TileCoordinate>();

		for (int y = 0; y < count; y++)
		{
			for (int x = 0; x < count; x++)
			{
				var coord = new TileCoordinate(zoom, x, y);

				if (_atlas.IsTileLoaded(coord))
					continue;

				// Check cache synchronously — fast path
				var cached = _cache.TryGet(coord);
				if (cached != null)
				{
					_atlas.BlitTile(coord, cached);
					continue;
				}

				if (!_inflight.ContainsKey(coord))
					needed.Add(coord);
			}
		}

		_desiredTiles = needed;
		_snapshotReady.Set();
	}

	/// <summary>
	/// Update for flat-map mode: only load tiles visible in the viewport.
	/// </summary>
	public void UpdateFlat(MapCamera2D camera)
	{
		EnsureWorkersStarted();

		// Process completed downloads on the main thread (always, even when static)
		int blits = 0;
		while (blits < MaxBlitsPerFrame && _completedTiles.TryDequeue(out var completed))
		{
			_virtualAtlas.BlitTile(completed.coord, completed.data);
			blits++;
		}

		// Skip visible-tile recalculation when camera hasn't changed and no new tiles arrived
		int cameraVersion = camera.Version;
		if (cameraVersion == _lastFlatCameraVersion && blits == 0)
			return;
		_lastFlatCameraVersion = cameraVersion;

		var visibleTiles = camera.GetVisibleTiles();

		// Touch visible tiles to keep them in the atlas LRU
		_virtualAtlas.TouchAll(visibleTiles);

		// Build the desired set: visible tiles not yet loaded or in-flight
		// Sorted by distance from viewport center (center tiles load first)
		double centerX = camera.ViewportWidth / 2.0;
		double centerY = camera.ViewportHeight / 2.0;

		var needed = new List<TileCoordinate>();
		foreach (var coord in visibleTiles)
		{
			if (_virtualAtlas.IsTileLoaded(coord))
				continue;

			// Check cache synchronously — fast path
			var cached = _cache.TryGet(coord);
			if (cached != null)
			{
				_virtualAtlas.BlitTile(coord, cached);
				continue;
			}

			if (!_inflight.ContainsKey(coord))
				needed.Add(coord);
		}

		// Sort center-first using tile screen position
		needed.Sort((a, b) =>
		{
			var rectA = camera.GetTileScreenRect(a);
			var rectB = camera.GetTileScreenRect(b);
			double distA = DistSq(rectA.x + rectA.width / 2, rectA.y + rectA.height / 2, centerX, centerY);
			double distB = DistSq(rectB.x + rectB.width / 2, rectB.y + rectB.height / 2, centerX, centerY);
			return distA.CompareTo(distB);
		});

		// Publish snapshot for workers
		_desiredTiles = needed;
		_snapshotReady.Set();
	}

	private static double DistSq(double x1, double y1, double x2, double y2)
	{
		double dx = x1 - x2;
		double dy = y1 - y2;
		return dx * dx + dy * dy;
	}

	private void EnsureWorkersStarted()
	{
		if (_workersStarted)
			return;

		_workersStarted = true;
		for (int i = 0; i < WorkerCount; i++)
		{
			Task.Run(() => WorkerLoop(_shutdownCts.Token));
		}
	}

	private async Task WorkerLoop(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			// Wait for a snapshot to be published
			try
			{
				_snapshotReady.Wait(ct);
			}
			catch (OperationCanceledException)
			{
				return;
			}

			// Read the latest snapshot
			var snapshot = _desiredTiles;
			if (snapshot.Count == 0)
			{
				// Nothing to do — reset and wait for next snapshot
				_snapshotReady.Reset();
				continue;
			}

			foreach (var coord in snapshot)
			{
				if (ct.IsCancellationRequested)
					return;

				// Re-check: is this tile still in the latest desired set?
				var latest = _desiredTiles;
				if (!latest.Contains(coord))
					continue;

				// Claim this tile (prevent other workers from picking it up)
				if (!_inflight.TryAdd(coord, 0))
					continue;

				try
				{
					// Check cache first
					var cached = _cache.TryGet(coord);
					if (cached != null)
					{
						_completedTiles.Enqueue((coord, cached));
						continue;
					}

					// Download
					var data = await _fetcher.FetchTileAsync(coord, ct);
					if (data != null)
					{
						_cache.Store(coord, data);
						_completedTiles.Enqueue((coord, data));
					}
				}
				catch (OperationCanceledException)
				{
					return;
				}
				catch
				{
					// Download failed — tile will be retried on next snapshot
				}
				finally
				{
					_inflight.TryRemove(coord, out _);
				}
			}

			// Finished processing snapshot — reset and wait for next one
			_snapshotReady.Reset();
		}
	}

	public void Dispose()
	{
		_shutdownCts.Cancel();
		_snapshotReady.Set(); // Unblock any waiting workers
		_shutdownCts.Dispose();
		_snapshotReady.Dispose();
		_fetcher.Dispose();
		_atlas.Dispose();
		_virtualAtlas.Dispose();
	}
}
