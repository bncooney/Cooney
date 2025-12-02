using System;

namespace Cooney.Common.Maths
{
	public struct Double3
	{
		public double x, y, z;
		public Double3(double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}

	public struct Double3x3
	{
		public Double3 c0, c1, c2;
		public Double3x3(Double3 c0, Double3 c1, Double3 c2)
		{
			this.c0 = c0;
			this.c1 = c1;
			this.c2 = c2;
		}
	}

	public static class Maths
	{
		public const double DegreesToRadians = Math.PI / 180.0;
		public const double RadiansToDegrees = 180.0 / Math.PI;

		public static Double3 Mul(Double3x3 m, Double3 v)
		{
			return new Double3(
				m.c0.x * v.x + m.c1.x * v.y + m.c2.x * v.z,
				m.c0.y * v.x + m.c1.y * v.y + m.c2.y * v.z,
				m.c0.z * v.x + m.c1.z * v.y + m.c2.z * v.z
			);
		}

		public static Double3x3 Transpose(Double3x3 m)
		{
			return new Double3x3(
				new Double3(m.c0.x, m.c1.x, m.c2.x),
				new Double3(m.c0.y, m.c1.y, m.c2.y),
				new Double3(m.c0.z, m.c1.z, m.c2.z)
			);
		}
	}
}
