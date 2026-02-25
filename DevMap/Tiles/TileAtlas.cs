using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DevMap.Tiles;

public class TileAtlas : IDisposable
{
	public Texture2D? AtlasTexture { get; private set; }

	private readonly GraphicsDevice _device;
	private readonly HashSet<TileCoordinate> _loadedTiles = [];
	private int _currentZoom = -1;
	private int _tilesPerAxis;

	private const int TilePixelSize = 256;
	private readonly int _maxAtlasWidth;
	private readonly int _maxAtlasHeight;

	public TileAtlas(GraphicsDevice device)
	{
		_device = device;
		int maxSize = device.GraphicsProfile == GraphicsProfile.HiDef ? 8192 : 4096;
		_maxAtlasWidth = maxSize;
		_maxAtlasHeight = maxSize;
	}

	public int CurrentZoom => _currentZoom;

	public void SetZoom(int zoom)
	{
		if (zoom == _currentZoom)
			return;

		_currentZoom = zoom;
		_tilesPerAxis = 1 << zoom;
		_loadedTiles.Clear();

		// Calculate atlas size: tiles * 256, capped at max dimensions
		int width = Math.Min(_tilesPerAxis * TilePixelSize, _maxAtlasWidth);
		int height = Math.Min(_tilesPerAxis * TilePixelSize, _maxAtlasHeight);

		AtlasTexture?.Dispose();
		AtlasTexture = new Texture2D(_device, width, height, false, SurfaceFormat.Color);

		// Fill with dark gray as placeholder
		var fill = new Color[width * height];
		Array.Fill(fill, new Color(40, 40, 50));
		AtlasTexture.SetData(fill);
	}

	public bool IsTileLoaded(TileCoordinate coord) => _loadedTiles.Contains(coord);

	public void BlitTile(TileCoordinate coord, byte[] pngData)
	{
		if (AtlasTexture == null || coord.Zoom != _currentZoom)
			return;

		// Decode PNG to pixel data
		using var stream = new MemoryStream(pngData);
		using var tileTex = Texture2D.FromStream(_device, stream);

		var pixels = new Color[tileTex.Width * tileTex.Height];
		tileTex.GetData(pixels);

		// Calculate destination rectangle in the atlas
		int atlasWidth = AtlasTexture.Width;
		int atlasHeight = AtlasTexture.Height;

		// Scale tile pixels to fit atlas grid cell
		int cellWidth = atlasWidth / _tilesPerAxis;
		int cellHeight = atlasHeight / _tilesPerAxis;

		if (cellWidth == TilePixelSize && cellHeight == TilePixelSize)
		{
			// Direct blit, no scaling needed
			int destX = coord.X * TilePixelSize;
			int destY = coord.Y * TilePixelSize;
			var destRect = new Rectangle(destX, destY, TilePixelSize, TilePixelSize);
			AtlasTexture.SetData(0, destRect, pixels, 0, pixels.Length);
		}
		else
		{
			// Need to scale tile down to fit atlas cell
			var scaled = ScalePixels(pixels, tileTex.Width, tileTex.Height, cellWidth, cellHeight);
			int destX = coord.X * cellWidth;
			int destY = coord.Y * cellHeight;
			var destRect = new Rectangle(destX, destY, cellWidth, cellHeight);
			AtlasTexture.SetData(0, destRect, scaled, 0, scaled.Length);
		}

		_loadedTiles.Add(coord);
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
