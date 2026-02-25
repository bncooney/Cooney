using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DevMap.Camera;

namespace DevMap.Globe;

public class GlobeRenderer : IDisposable
{
	private readonly SphereMesh _mesh;
	private readonly BasicEffect _effect;
	private readonly GraphicsDevice _device;

	public GlobeRenderer(GraphicsDevice device)
	{
		_device = device;
		_mesh = new SphereMesh(device);

		_effect = new BasicEffect(device)
		{
			LightingEnabled = false,
			TextureEnabled = false,
			DiffuseColor = new Vector3(0.3f, 0.5f, 0.8f), // Ocean blue fallback
		};
	}

	public void Draw(ArcballCamera camera, Texture2D? atlasTexture)
	{
		_effect.World = Matrix.Identity;
		_effect.View = camera.View;
		_effect.Projection = camera.Projection;

		// Use atlas texture if available
		if (atlasTexture != null)
		{
			_effect.TextureEnabled = true;
			_effect.Texture = atlasTexture;
			_effect.DiffuseColor = Vector3.One;
		}
		else
		{
			_effect.TextureEnabled = false;
			_effect.DiffuseColor = new Vector3(0.3f, 0.5f, 0.8f);
		}

		_device.SetVertexBuffer(_mesh.VertexBuffer);
		_device.Indices = _mesh.IndexBuffer;

		foreach (var pass in _effect.CurrentTechnique.Passes)
		{
			pass.Apply();
			_device.DrawIndexedPrimitives(
				PrimitiveType.TriangleList,
				0, 0,
				_mesh.PrimitiveCount);
		}
	}

	public void Dispose()
	{
		_mesh.Dispose();
		_effect.Dispose();
	}
}
