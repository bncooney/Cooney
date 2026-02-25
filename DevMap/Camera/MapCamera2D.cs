using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using DevMap.Tiles;
using DevMap.Util;

namespace DevMap.Camera;

public class MapCamera2D
{
	private double _centerLat, _centerLon;
	private double _zoom = 5.0;
	private int _viewportWidth, _viewportHeight;
	private int _version;

	public double CenterLat
	{
		get => _centerLat;
		set { if (_centerLat != value) { _centerLat = value; _version++; } }
	}

	public double CenterLon
	{
		get => _centerLon;
		set { if (_centerLon != value) { _centerLon = value; _version++; } }
	}

	public double Zoom
	{
		get => _zoom;
		set { if (_zoom != value) { _zoom = value; _version++; } }
	}

	public int ViewportWidth
	{
		get => _viewportWidth;
		set { if (_viewportWidth != value) { _viewportWidth = value; _version++; } }
	}

	public int ViewportHeight
	{
		get => _viewportHeight;
		set { if (_viewportHeight != value) { _viewportHeight = value; _version++; } }
	}

	/// <summary>
	/// Monotonically increasing version number. Incremented whenever any camera property changes.
	/// </summary>
	public int Version => _version;

	public double MinZoom { get; set; } = 4.0;
	public double MaxZoom { get; set; } = 12.0;

	private const double ZoomFactor = 1.1;

	// Reusable collections for GetVisibleTiles to avoid per-call allocations
	private readonly List<TileCoordinate> _visibleTiles = new(64);
	private readonly HashSet<(int, int)> _seenTiles = new(64);

	/// <summary>
	/// Convert screen position to geographic coordinates.
	/// Screen center maps to (CenterLat, CenterLon).
	/// </summary>
	public (double lat, double lon) ScreenToGeo(Vector2 screenPos)
	{
		// Convert center to pixel space at current zoom
		var (cx, cy) = MercatorProjection.GeoToPixel(CenterLat, CenterLon, Zoom);

		// Screen center offset
		double px = cx + (screenPos.X - ViewportWidth / 2.0);
		double py = cy + (screenPos.Y - ViewportHeight / 2.0);

		return MercatorProjection.PixelToGeo(px, py, Zoom);
	}

	/// <summary>
	/// Convert geographic coordinates to screen position.
	/// </summary>
	public Vector2 GeoToScreen(double lat, double lon)
	{
		var (cx, cy) = MercatorProjection.GeoToPixel(CenterLat, CenterLon, Zoom);
		var (px, py) = MercatorProjection.GeoToPixel(lat, lon, Zoom);

		float sx = (float)(px - cx + ViewportWidth / 2.0);
		float sy = (float)(py - cy + ViewportHeight / 2.0);

		return new Vector2(sx, sy);
	}

	/// <summary>
	/// Get the integer tile zoom level for tile requests.
	/// </summary>
	public int GetTileZoom()
	{
		return (int)Math.Clamp(Math.Floor(Zoom), 0, MaxZoom);
	}

	/// <summary>
	/// Get the scale factor between tile pixels and screen pixels.
	/// At fractional zoom, tiles are scaled by 2^(Zoom - tileZoom).
	/// </summary>
	public double GetTileScale()
	{
		int tileZoom = GetTileZoom();
		return Math.Pow(2, Zoom - tileZoom);
	}

	/// <summary>
	/// Get the screen rectangle for a given tile coordinate.
	/// Handles wrapping: picks the copy of the tile closest to the viewport center.
	/// </summary>
	public (float x, float y, float width, float height) GetTileScreenRect(TileCoordinate tile)
	{
		double scale = Math.Pow(2, Zoom - tile.Zoom);
		double tileScreenSize = 256.0 * scale;

		// Tile's top-left in absolute pixel space at current zoom
		double tilePixelX = tile.X * tileScreenSize;
		double tilePixelY = tile.Y * tileScreenSize;

		// Center in absolute pixel space
		var (cx, cy) = MercatorProjection.GeoToPixel(CenterLat, CenterLon, Zoom);

		double screenX = tilePixelX - cx + ViewportWidth / 2.0;
		float screenY = (float)(tilePixelY - cy + ViewportHeight / 2.0);

		// Map width in pixels at current zoom (for wrapping)
		double mapWidth = Math.Pow(2, Zoom) * 256.0;

		// Pick the wrapped copy closest to the viewport center
		double viewCenterX = ViewportWidth / 2.0;
		double bestX = screenX;
		double bestDist = Math.Abs(screenX + tileScreenSize / 2.0 - viewCenterX);

		for (int offset = -1; offset <= 1; offset++)
		{
			if (offset == 0)
				continue;
			double candidateX = screenX + offset * mapWidth;
			double dist = Math.Abs(candidateX + tileScreenSize / 2.0 - viewCenterX);
			if (dist < bestDist)
			{
				bestDist = dist;
				bestX = candidateX;
			}
		}

		return ((float)bestX, screenY, (float)tileScreenSize, (float)tileScreenSize);
	}

	/// <summary>
	/// Get all tile coordinates visible in the current viewport.
	/// Works in pixel space to handle dateline wrapping correctly.
	/// </summary>
	public List<TileCoordinate> GetVisibleTiles()
	{
		int tileZoom = GetTileZoom();
		int tileCount = MercatorProjection.TileCount(tileZoom);
		double scale = Math.Pow(2, Zoom - tileZoom);
		double tileScreenSize = 256.0 * scale;

		// Center in absolute pixel space
		var (cx, cy) = MercatorProjection.GeoToPixel(CenterLat, CenterLon, Zoom);

		// Viewport bounds in absolute pixel space
		double leftPx = cx - ViewportWidth / 2.0;
		double rightPx = cx + ViewportWidth / 2.0;
		double topPy = cy - ViewportHeight / 2.0;
		double bottomPy = cy + ViewportHeight / 2.0;

		// Convert to tile indices (can be negative or beyond tileCount for wrapping)
		int minTX = (int)Math.Floor(leftPx / tileScreenSize) - 1;
		int maxTX = (int)Math.Floor(rightPx / tileScreenSize) + 1;
		int minTY = Math.Max(0, (int)Math.Floor(topPy / tileScreenSize) - 1);
		int maxTY = Math.Min(tileCount - 1, (int)Math.Floor(bottomPy / tileScreenSize) + 1);

		_visibleTiles.Clear();
		_seenTiles.Clear();

		for (int y = minTY; y <= maxTY; y++)
		{
			for (int x = minTX; x <= maxTX; x++)
			{
				// Wrap X to valid range (horizontal wrapping)
				int wrappedX = ((x % tileCount) + tileCount) % tileCount;
				if (_seenTiles.Add((wrappedX, y)))
				{
					_visibleTiles.Add(new TileCoordinate(tileZoom, wrappedX, y));
				}
			}
		}

		return _visibleTiles;
	}

	/// <summary>
	/// Pan by a screen-space pixel delta.
	/// </summary>
	public void HandleMouseDrag(Vector2 delta)
	{
		var (cx, cy) = MercatorProjection.GeoToPixel(CenterLat, CenterLon, Zoom);
		var (newLat, newLon) = MercatorProjection.PixelToGeo(cx + delta.X, cy + delta.Y, Zoom);

		CenterLat = Math.Clamp(newLat, -MercatorProjection.MaxLatitudeDeg, MercatorProjection.MaxLatitudeDeg);
		CenterLon = newLon;

		// Wrap longitude to [-180, 180]
		if (CenterLon > 180)
			CenterLon -= 360;
		if (CenterLon < -180)
			CenterLon += 360;
	}

	/// <summary>
	/// Zoom by mouse wheel delta (120 per notch).
	/// </summary>
	public void HandleMouseWheel(int delta)
	{
		int notches = delta / 120;
		double factor = notches > 0 ? Math.Log2(ZoomFactor) : -Math.Log2(ZoomFactor);
		Zoom += notches * Math.Log2(ZoomFactor);
		Zoom = Math.Clamp(Zoom, MinZoom, MaxZoom);
	}
}
