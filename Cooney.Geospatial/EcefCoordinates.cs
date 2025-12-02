using Cooney.Common.Maths;
using System;

namespace Cooney.Geospatial
{
	public struct EcefCoordinates
	{
		private double _x;
		private double _y;
		private double _z;

		public double X
		{
			readonly get => _x;
			set => _x = value;
		}

		public double Y
		{
			readonly get => _y;
			set => _y = value;
		}

		public double Z
		{
			readonly get => _z;
			set => _z = value;
		}

		public EcefCoordinates(double x, double y, double z)
		{
			_x = x;
			_y = y;
			_z = z;
		}

		public readonly Double3 ToDouble3()
		{
			return new Double3(_x, _y, _z);
		}

		public static EcefCoordinates FromDouble3(Double3 value)
		{
			return new EcefCoordinates(value.x, value.y, value.z);
		}

		public readonly double Magnitude()
		{
			return Math.Sqrt(_x * _x + _y * _y + _z * _z);
		}

		public readonly EcefCoordinates Normalised()
		{
			double mag = Magnitude();
			if (mag > 0)
			{
				return new EcefCoordinates(_x / mag, _y / mag, _z / mag);
			}
			return this;
		}

		public override readonly string ToString()
		{
			return $"ECEF(X: {_x:F3}m, Y: {_y:F3}m, Z: {_z:F3}m)";
		}

		public override readonly bool Equals(object obj)
		{
			if (obj is EcefCoordinates other)
			{
				return Math.Abs(_x - other._x) < 1e-6 &&
					   Math.Abs(_y - other._y) < 1e-6 &&
					   Math.Abs(_z - other._z) < 1e-6;
			}
			return false;
		}

		public override readonly int GetHashCode()
		{
			return HashCode.Combine(_x, _y, _z);
		}

		public static bool operator ==(EcefCoordinates a, EcefCoordinates b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(EcefCoordinates a, EcefCoordinates b)
		{
			return !a.Equals(b);
		}

		public static EcefCoordinates operator +(EcefCoordinates a, EcefCoordinates b)
		{
			return new EcefCoordinates(a._x + b._x, a._y + b._y, a._z + b._z);
		}

		public static EcefCoordinates operator -(EcefCoordinates a, EcefCoordinates b)
		{
			return new EcefCoordinates(a._x - b._x, a._y - b._y, a._z - b._z);
		}

		public static EcefCoordinates operator *(EcefCoordinates a, double scalar)
		{
			return new EcefCoordinates(a._x * scalar, a._y * scalar, a._z * scalar);
		}

		public static EcefCoordinates operator /(EcefCoordinates a, double scalar)
		{
			return new EcefCoordinates(a._x / scalar, a._y / scalar, a._z / scalar);
		}
	}
}
