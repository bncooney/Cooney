using System;

namespace Cooney.Geospatial
{
	public struct GeographicCoordinates
	{
		private double _latitude;
		private double _longitude;
		private double _altitude;

		public double Latitude
		{
			readonly get => _latitude;
			set => _latitude = value;
		}

		public double Longitude
		{
			readonly get => _longitude;
			set => _longitude = value;
		}

		public double Altitude
		{
			readonly get => _altitude;
			set => _altitude = value;
		}

		public GeographicCoordinates(double latitude, double longitude, double altitude = 0)
		{
			_latitude = latitude;
			_longitude = longitude;
			_altitude = altitude;
		}

		public readonly bool IsValid()
		{
			return _latitude >= -90.0 && _latitude <= 90.0 &&
				   _longitude >= -180.0 && _longitude <= 180.0 &&
				   !double.IsNaN(_latitude) && !double.IsNaN(_longitude) && !double.IsNaN(_altitude) &&
				   !double.IsInfinity(_latitude) && !double.IsInfinity(_longitude) && !double.IsInfinity(_altitude);
		}

		public void Normalize()
		{
			_latitude = Math.Max(-90.0, Math.Min(90.0, _latitude));

			while (_longitude > 180.0)
				_longitude -= 360.0;
			while (_longitude < -180.0)
				_longitude += 360.0;
		}

		public override readonly string ToString()
		{
			return $"Lat: {_latitude:F6}°, Lon: {_longitude:F6}°, Alt: {_altitude:F2}m";
		}

		public readonly string ToDetailedString()
		{
			char latDir = _latitude >= 0 ? 'N' : 'S';
			char lonDir = _longitude >= 0 ? 'E' : 'W';
			return $"{Math.Abs(_latitude):F6}°{latDir}, {Math.Abs(_longitude):F6}°{lonDir}, {_altitude:F2}m";
		}

		public override readonly bool Equals(object obj)
		{
			if (obj is GeographicCoordinates other)
			{
				return Math.Abs(_latitude - other._latitude) < 1e-9 &&
					   Math.Abs(_longitude - other._longitude) < 1e-9 &&
					   Math.Abs(_altitude - other._altitude) < 1e-6;
			}
			return false;
		}

		public override readonly int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + _latitude.GetHashCode();
				hash = hash * 23 + _longitude.GetHashCode();
				hash = hash * 23 + _altitude.GetHashCode();
				return hash;
			}
		}

		public static bool operator ==(GeographicCoordinates a, GeographicCoordinates b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(GeographicCoordinates a, GeographicCoordinates b)
		{
			return !a.Equals(b);
		}
	}
}
