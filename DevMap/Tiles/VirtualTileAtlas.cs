using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DevMap.Tiles;

/// <summary>
/// GPU tile cache using a fixed-size atlas texture with LRU eviction.
/// The atlas is a grid of 256px tile slots (e.g. 4096x4096 = 16x16 = 256 slots).
/// </summary>
public class VirtualTileAtlas : IDisposable
{
	public Texture2D? AtlasTexture { get; private set; }

	private readonly GraphicsDevice _device;
	private readonly int _slotsPerAxis;
	private readonly int _totalSlots;

	private const int TilePixelSize = 256;

	// Slot management: tile coord → slot index
	private readonly Dictionary<TileCoordinate, int> _slotMap = [];

	// LRU tracking: front = most recently used, back = least recently used
	private readonly LinkedList<TileCoordinate> _lruList = new();
	private readonly Dictionary<TileCoordinate, LinkedListNode<TileCoordinate>> _lruNodes = [];

	// Which slots are occupied (slot index → tile coord)
	private readonly Dictionary<int, TileCoordinate> _slotToTile = [];

	// Free slot stack
	private readonly Stack<int> _freeSlots = new();

	public VirtualTileAtlas(GraphicsDevice device, int atlasSize = 4096)
	{
		_device = device;
		_slotsPerAxis = atlasSize / TilePixelSize;
		_totalSlots = _slotsPerAxis * _slotsPerAxis;

		AtlasTexture = new Texture2D(device, atlasSize, atlasSize, false, SurfaceFormat.Color);

		// Fill with dark gray placeholder
		var fill = new Color[atlasSize * atlasSize];
		Array.Fill(fill, new Color(40, 40, 50));
		AtlasTexture.SetData(fill);

		// All slots start free
		for (int i = _totalSlots - 1; i >= 0; i--)
			_freeSlots.Push(i);
	}

	/// <summary>
	/// Check if a tile is loaded in the atlas.
	/// </summary>
	public bool IsTileLoaded(TileCoordinate coord) => _slotMap.ContainsKey(coord);

	/// <summary>
	/// Mark a tile as recently used (move to front of LRU).
	/// </summary>
	public void Touch(TileCoordinate coord)
	{
		if (_lruNodes.TryGetValue(coord, out var node))
		{
			_lruList.Remove(node);
			_lruList.AddFirst(node);
		}
	}

	/// <summary>
	/// Get the UV rectangle for a tile's slot within the atlas.
	/// Returns (uvMin, uvMax) in [0,1] atlas space.
	/// </summary>
	public (Vector2 uvMin, Vector2 uvMax) GetSlotUV(TileCoordinate coord)
	{
		if (!_slotMap.TryGetValue(coord, out int slot))
			return (Vector2.Zero, Vector2.Zero);

		int slotX = slot % _slotsPerAxis;
		int slotY = slot / _slotsPerAxis;

		float atlasSize = AtlasTexture!.Width; // Square atlas
		float uvTileSize = TilePixelSize / atlasSize;

		var uvMin = new Vector2(slotX * uvTileSize, slotY * uvTileSize);
		var uvMax = new Vector2((slotX + 1) * uvTileSize, (slotY + 1) * uvTileSize);

		return (uvMin, uvMax);
	}

	/// <summary>
	/// Get the UV rectangle for a sub-region of a tile's slot.
	/// Used for LOD fallback where a parent tile covers 4 child tiles.
	/// subX/subY are 0 or 1 indicating which quadrant of the parent.
	/// </summary>
	public (Vector2 uvMin, Vector2 uvMax) GetSlotSubUV(TileCoordinate coord, int subX, int subY)
	{
		var (uvMin, uvMax) = GetSlotUV(coord);
		var halfSize = (uvMax - uvMin) / 2f;

		var subMin = uvMin + new Vector2(subX * halfSize.X, subY * halfSize.Y);
		var subMax = subMin + halfSize;

		return (subMin, subMax);
	}

	/// <summary>
	/// Blit a tile's PNG data into the atlas at the next available (or evicted) slot.
	/// </summary>
	public void BlitTile(TileCoordinate coord, byte[] pngData)
	{
		if (AtlasTexture == null)
			return;

		// Already loaded?
		if (_slotMap.ContainsKey(coord))
		{
			Touch(coord);
			return;
		}

		int slot = AllocateSlot(coord);

		// Decode PNG and write to slot
		using var stream = new MemoryStream(pngData);
		using var tileTex = Texture2D.FromStream(_device, stream);

		var pixels = new Color[tileTex.Width * tileTex.Height];
		tileTex.GetData(pixels);

		// Scale if tile isn't exactly 256x256
		Color[] finalPixels;
		if (tileTex.Width == TilePixelSize && tileTex.Height == TilePixelSize)
		{
			finalPixels = pixels;
		}
		else
		{
			finalPixels = ScalePixels(pixels, tileTex.Width, tileTex.Height, TilePixelSize, TilePixelSize);
		}

		int slotX = slot % _slotsPerAxis;
		int slotY = slot / _slotsPerAxis;
		int destX = slotX * TilePixelSize;
		int destY = slotY * TilePixelSize;

		var destRect = new Rectangle(destX, destY, TilePixelSize, TilePixelSize);
		AtlasTexture.SetData(0, destRect, finalPixels, 0, finalPixels.Length);
	}

	private int AllocateSlot(TileCoordinate coord)
	{
		int slot;

		if (_freeSlots.Count > 0)
		{
			slot = _freeSlots.Pop();
		}
		else
		{
			// Evict least recently used
			var evictNode = _lruList.Last!;
			var evictCoord = evictNode.Value;

			slot = _slotMap[evictCoord];
			_slotMap.Remove(evictCoord);
			_slotToTile.Remove(slot);
			_lruList.Remove(evictNode);
			_lruNodes.Remove(evictCoord);
		}

		_slotMap[coord] = slot;
		_slotToTile[slot] = coord;

		var node = _lruList.AddFirst(coord);
		_lruNodes[coord] = node;

		return slot;
	}

	/// <summary>
	/// Touch all tiles in the given set to prevent them from being evicted.
	/// </summary>
	public void TouchAll(IEnumerable<TileCoordinate> coords)
	{
		foreach (var coord in coords)
			Touch(coord);
	}

	private static Color[] ScalePixels(Color[] source, int srcW, int srcH, int dstW, int dstH)
	{
		var result = new Color[dstW * dstH];
		for (int y = 0; y < dstH; y++)
		{
			int srcY = y * srcH / dstH;
			for (int x = 0; x < dstW; x++)
			{
				int srcX = x * srcW / dstW;
				result[y * dstW + x] = source[srcY * srcW + srcX];
			}
		}
		return result;
	}

	public void Dispose()
	{
		AtlasTexture?.Dispose();
	}
}
