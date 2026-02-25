using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DevMap.Util;

namespace DevMap.Globe;

public class SphereMesh : IDisposable
{
	public VertexBuffer VertexBuffer { get; }
	public IndexBuffer IndexBuffer { get; }
	public int PrimitiveCount { get; }

	public SphereMesh(GraphicsDevice device, float radius = 1.0f, int lonSegments = 128, int latSegments = 64)
	{
		int vertexCount = (lonSegments + 1) * (latSegments + 1);
		var vertices = new VertexPositionNormalTexture[vertexCount];

		int idx = 0;
		for (int lat = 0; lat <= latSegments; lat++)
		{
			// theta: polar angle from north pole (0) to south pole (pi)
			double theta = Math.PI * lat / latSegments;
			double sinTheta = Math.Sin(theta);
			double cosTheta = Math.Cos(theta);

			// Geographic latitude in radians: pi/2 at north pole, -pi/2 at south pole
			double latRad = Math.PI / 2.0 - theta;
			double v = MercatorProjection.LatitudeToMercatorV(latRad);

			for (int lon = 0; lon <= lonSegments; lon++)
			{
				double phi = 2.0 * Math.PI * lon / lonSegments;

				float x = radius * (float)(sinTheta * Math.Cos(phi));
				float y = radius * (float)cosTheta;
				float z = radius * (float)(sinTheta * Math.Sin(phi));

				var position = new Vector3(x, y, z);
				var normal = Vector3.Normalize(position);

				float u = 1.0f - (float)lon / lonSegments;

				vertices[idx++] = new VertexPositionNormalTexture(
					position,
					normal,
					new Vector2(u, (float)v)
				);
			}
		}

		// Generate indices
		int indexCount = latSegments * lonSegments * 6;
		var indices = new int[indexCount];
		int iIdx = 0;

		for (int lat = 0; lat < latSegments; lat++)
		{
			for (int lon = 0; lon < lonSegments; lon++)
			{
				int current = lat * (lonSegments + 1) + lon;
				int next = current + lonSegments + 1;

				// First triangle
				indices[iIdx++] = current;
				indices[iIdx++] = next;
				indices[iIdx++] = current + 1;

				// Second triangle
				indices[iIdx++] = current + 1;
				indices[iIdx++] = next;
				indices[iIdx++] = next + 1;
			}
		}

		VertexBuffer = new VertexBuffer(device, typeof(VertexPositionNormalTexture), vertexCount, BufferUsage.WriteOnly);
		VertexBuffer.SetData(vertices);

		if (vertexCount > 65535)
		{
			IndexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indexCount, BufferUsage.WriteOnly);
			IndexBuffer.SetData(indices);
		}
		else
		{
			var shortIndices = new short[indexCount];
			for (int i = 0; i < indexCount; i++)
				shortIndices[i] = (short)indices[i];
			IndexBuffer = new IndexBuffer(device, IndexElementSize.SixteenBits, indexCount, BufferUsage.WriteOnly);
			IndexBuffer.SetData(shortIndices);
		}

		PrimitiveCount = indexCount / 3;
	}

	public void Dispose()
	{
		VertexBuffer?.Dispose();
		IndexBuffer?.Dispose();
	}
}
