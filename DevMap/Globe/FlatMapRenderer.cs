using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DevMap.Camera;
using DevMap.Tiles;

namespace DevMap.Globe;

/// <summary>
/// Renders visible map tiles as textured quads in orthographic projection.
/// Each visible tile is drawn as a separate quad mapped to its atlas slot UV.
/// </summary>
public class FlatMapRenderer : IDisposable
{
	private readonly GraphicsDevice _device;
	private readonly BasicEffect _effect;

	// Reusable vertex array for drawing quads (4 vertices per tile)
	private readonly VertexPositionTexture[] _quadVertices = new VertexPositionTexture[4];
	private readonly short[] _quadIndices = [0, 1, 2, 2, 1, 3];

	public FlatMapRenderer(GraphicsDevice device)
	{
		_device = device;

		_effect = new BasicEffect(device)
		{
			LightingEnabled = false,
			TextureEnabled = true,
			VertexColorEnabled = false,
			DiffuseColor = Vector3.One,
		};
	}

	public void Draw(MapCamera2D camera, VirtualTileAtlas atlas)
	{
		if (atlas.AtlasTexture == null)
			return;

		// Set up orthographic projection matching viewport
		_effect.World = Matrix.Identity;
		_effect.View = Matrix.Identity;
		_effect.Projection = Matrix.CreateOrthographicOffCenter(
			0, camera.ViewportWidth, camera.ViewportHeight, 0, 0, 1);
		_effect.Texture = atlas.AtlasTexture;

		var visibleTiles = camera.GetVisibleTiles();

		foreach (var tile in visibleTiles)
		{
			// Try to find this tile or a parent tile in the atlas
			if (!TryDrawTile(camera, atlas, tile))
			{
				// Try LOD fallback: walk up zoom levels to find a loaded parent
				TryDrawFallbackTile(camera, atlas, tile);
			}
		}
	}

	private bool TryDrawTile(MapCamera2D camera, VirtualTileAtlas atlas, TileCoordinate tile)
	{
		if (!atlas.IsTileLoaded(tile))
			return false;

		var (uvMin, uvMax) = atlas.GetSlotUV(tile);
		var (sx, sy, sw, sh) = camera.GetTileScreenRect(tile);

		DrawQuad(sx, sy, sw, sh, uvMin, uvMax);
		return true;
	}

	private void TryDrawFallbackTile(MapCamera2D camera, VirtualTileAtlas atlas, TileCoordinate tile)
	{
		// Walk up zoom levels to find a loaded ancestor
		int childX = tile.X;
		int childY = tile.Y;

		for (int z = tile.Zoom - 1; z >= 0; z--)
		{
			// Which quadrant of the parent does the child occupy?
			int subX = childX % 2;
			int subY = childY % 2;
			int parentX = childX / 2;
			int parentY = childY / 2;

			var parentTile = new TileCoordinate(z, parentX, parentY);

			if (atlas.IsTileLoaded(parentTile))
			{
				// Compute the sub-region UV for the original tile within this ancestor
				// We need to accumulate the sub-region across zoom level differences
				var (uvMin, uvMax) = atlas.GetSlotUV(parentTile);
				int zoomDiff = tile.Zoom - z;
				int divisions = 1 << zoomDiff;

				// Which sub-cell within the ancestor does the original tile occupy?
				int relX = tile.X % divisions;
				int relY = tile.Y % divisions;

				var uvSize = (uvMax - uvMin) / divisions;
				var subUvMin = uvMin + new Vector2(relX * uvSize.X, relY * uvSize.Y);
				var subUvMax = subUvMin + uvSize;

				var (sx, sy, sw, sh) = camera.GetTileScreenRect(tile);
				DrawQuad(sx, sy, sw, sh, subUvMin, subUvMax);
				return;
			}

			childX = parentX;
			childY = parentY;
		}

		// No fallback found — tile area will show atlas background color
	}

	private void DrawQuad(float x, float y, float w, float h, Vector2 uvMin, Vector2 uvMax)
	{
		_quadVertices[0] = new VertexPositionTexture(new Vector3(x, y, 0), uvMin);
		_quadVertices[1] = new VertexPositionTexture(new Vector3(x + w, y, 0), new Vector2(uvMax.X, uvMin.Y));
		_quadVertices[2] = new VertexPositionTexture(new Vector3(x, y + h, 0), new Vector2(uvMin.X, uvMax.Y));
		_quadVertices[3] = new VertexPositionTexture(new Vector3(x + w, y + h, 0), uvMax);

		foreach (var pass in _effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_device.DrawUserIndexedPrimitives(
				PrimitiveType.TriangleList,
				_quadVertices, 0, 4,
				_quadIndices, 0, 2);
		}
	}

	public void Dispose()
	{
		_effect.Dispose();
	}
}
