using Cooney.Common.Maths;
using System;

namespace Cooney.Geospatial
{
	public static class Wgs84Ellipsoid
	{
		public const double SemiMajorAxis = 6378137.0;
		public const double Flattening = 1.0 / 298.257223563;

		public const double SemiMinorAxis = SemiMajorAxis * (1.0 - Flattening);
		public const double EccentricitySquared = 2.0 * Flattening - Flattening * Flattening;
		public const double SecondEccentricitySquared = EccentricitySquared / (1.0 - EccentricitySquared);

		public static EcefCoordinates GeographicToEcef(GeographicCoordinates geographic)
		{
			double latRad = geographic.Latitude * Maths.DegreesToRadians;
			double lonRad = geographic.Longitude * Maths.DegreesToRadians;
			double h = geographic.Altitude;

			double sinLat = Math.Sin(latRad);
			double cosLat = Math.Cos(latRad);
			double sinLon = Math.Sin(lonRad);
			double cosLon = Math.Cos(lonRad);

			double N = SemiMajorAxis / Math.Sqrt(1.0 - EccentricitySquared * sinLat * sinLat);

			double x = (N + h) * cosLat * cosLon;
			double y = (N + h) * cosLat * sinLon;
			double z = (N * (1.0 - EccentricitySquared) + h) * sinLat;

			return new EcefCoordinates(x, y, z);
		}

		public static GeographicCoordinates EcefToGeographic(EcefCoordinates ecef)
		{
			double x = ecef.X;
			double y = ecef.Y;
			double z = ecef.Z;

			double longitude = Math.Atan2(y, x);

			double p = Math.Sqrt(x * x + y * y);

			double theta = Math.Atan2(z * SemiMajorAxis, p * SemiMinorAxis);
			double sinTheta = Math.Sin(theta);
			double cosTheta = Math.Cos(theta);

			double latitude = Math.Atan2(
				z + SecondEccentricitySquared * SemiMinorAxis * sinTheta * sinTheta * sinTheta,
				p - EccentricitySquared * SemiMajorAxis * cosTheta * cosTheta * cosTheta
			);

			double sinLat = Math.Sin(latitude);
			double cosLat = Math.Cos(latitude);
			double N = SemiMajorAxis / Math.Sqrt(1.0 - EccentricitySquared * sinLat * sinLat);

			double altitude;
			if (Math.Abs(cosLat) > 1e-10)
			{
				altitude = p / cosLat - N;
			}
			else
			{
				altitude = Math.Abs(z) - N * (1.0 - EccentricitySquared);
			}

			double latDeg = latitude * Maths.RadiansToDegrees;
			double lonDeg = longitude * Maths.RadiansToDegrees;

			return new GeographicCoordinates(latDeg, lonDeg, altitude);
		}

		public static double CalculateRadiusOfCurvature(double latitudeInDegrees)
		{
			double latRad = latitudeInDegrees * Maths.DegreesToRadians;
			double sinLat = Math.Sin(latRad);
			return SemiMajorAxis / Math.Sqrt(1.0 - EccentricitySquared * sinLat * sinLat);
		}

		public static double CalculateMeridionalRadius(double latitudeInDegrees)
		{
			double latRad = latitudeInDegrees * Maths.DegreesToRadians;
			double sinLat = Math.Sin(latRad);
			double denominator = 1.0 - EccentricitySquared * sinLat * sinLat;
			return SemiMajorAxis * (1.0 - EccentricitySquared) / (denominator * Math.Sqrt(denominator));
		}

		/// <summary>
		/// Calculates the great-circle distance between two geographic points using the Haversine formula.
		/// This method uses the WGS84 ellipsoid's semi-major axis as the Earth radius.
		/// </summary>
		/// <param name="point1">First geographic coordinate</param>
		/// <param name="point2">Second geographic coordinate</param>
		/// <returns>Distance in meters between the two points along the great circle</returns>
		public static double HaversineDistance(GeographicCoordinates point1, GeographicCoordinates point2)
		{
			return HaversineDistance(point1.Latitude, point1.Longitude, point2.Latitude, point2.Longitude);
		}

		/// <summary>
		/// Calculates the great-circle distance between two geographic points using the Haversine formula.
		/// This method uses the WGS84 ellipsoid's semi-major axis as the Earth radius.
		/// </summary>
		/// <param name="lat1">Latitude of first point in degrees</param>
		/// <param name="lon1">Longitude of first point in degrees</param>
		/// <param name="lat2">Latitude of second point in degrees</param>
		/// <param name="lon2">Longitude of second point in degrees</param>
		/// <returns>Distance in meters between the two points along the great circle</returns>
		public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
		{
			// Convert degrees to radians
			double lat1Rad = lat1 * Maths.DegreesToRadians;
			double lon1Rad = lon1 * Maths.DegreesToRadians;
			double lat2Rad = lat2 * Maths.DegreesToRadians;
			double lon2Rad = lon2 * Maths.DegreesToRadians;

			// Differences in coordinates
			double dLat = lat2Rad - lat1Rad;
			double dLon = lon2Rad - lon1Rad;

			// Haversine formula
			double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
					  Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
					  Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

			// Distance in meters using WGS84 semi-major axis as Earth radius
			return SemiMajorAxis * c;
		}

		/// <summary>
		/// Calculates the initial bearing (azimuth) from one geographic point to another.
		/// The bearing is the angle measured clockwise from north.
		/// </summary>
		/// <param name="point1">Starting geographic coordinate</param>
		/// <param name="point2">Destination geographic coordinate</param>
		/// <returns>Initial bearing in degrees (0-360°), where 0° is north, 90° is east, etc.</returns>
		public static double CalculateInitialBearing(GeographicCoordinates point1, GeographicCoordinates point2)
		{
			return CalculateInitialBearing(point1.Latitude, point1.Longitude, point2.Latitude, point2.Longitude);
		}

		/// <summary>
		/// Calculates the initial bearing (azimuth) from one geographic point to another.
		/// The bearing is the angle measured clockwise from north.
		/// </summary>
		/// <param name="lat1">Latitude of starting point in degrees</param>
		/// <param name="lon1">Longitude of starting point in degrees</param>
		/// <param name="lat2">Latitude of destination point in degrees</param>
		/// <param name="lon2">Longitude of destination point in degrees</param>
		/// <returns>Initial bearing in degrees (0-360°), where 0° is north, 90° is east, etc.</returns>
		public static double CalculateInitialBearing(double lat1, double lon1, double lat2, double lon2)
		{
			// Convert degrees to radians
			double lat1Rad = lat1 * Maths.DegreesToRadians;
			double lon1Rad = lon1 * Maths.DegreesToRadians;
			double lat2Rad = lat2 * Maths.DegreesToRadians;
			double lon2Rad = lon2 * Maths.DegreesToRadians;

			// Differences in coordinates
			double dLon = lon2Rad - lon1Rad;

			// Calculate bearing using spherical trigonometry
			double y = Math.Sin(dLon) * Math.Cos(lat2Rad);
			double x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) -
					  Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

			double bearingRad = Math.Atan2(y, x);

			// Convert to degrees and normalize to 0-360 range
			double bearingDeg = bearingRad * Maths.RadiansToDegrees;
			return (bearingDeg + 360) % 360;
		}

		/// <summary>
		/// Calculates the final bearing (azimuth) from one geographic point to another.
		/// The bearing is the angle measured clockwise from north at the destination point.
		/// </summary>
		/// <param name="point1">Starting geographic coordinate</param>
		/// <param name="point2">Destination geographic coordinate</param>
		/// <returns>Final bearing in degrees (0-360°), where 0° is north, 90° is east, etc.</returns>
		public static double CalculateFinalBearing(GeographicCoordinates point1, GeographicCoordinates point2)
		{
			return CalculateFinalBearing(point1.Latitude, point1.Longitude, point2.Latitude, point2.Longitude);
		}

		/// <summary>
		/// Calculates the final bearing (azimuth) from one geographic point to another.
		/// The bearing is the angle measured clockwise from north at the destination point.
		/// </summary>
		/// <param name="lat1">Latitude of starting point in degrees</param>
		/// <param name="lon1">Longitude of starting point in degrees</param>
		/// <param name="lat2">Latitude of destination point in degrees</param>
		/// <param name="lon2">Longitude of destination point in degrees</param>
		/// <returns>Final bearing in degrees (0-360°), where 0° is north, 90° is east, etc.</returns>
		public static double CalculateFinalBearing(double lat1, double lon1, double lat2, double lon2)
		{
			// Final bearing is the initial bearing from point2 to point1, reversed by 180 degrees
			double initialBearing = CalculateInitialBearing(lat2, lon2, lat1, lon1);
			return (initialBearing + 180) % 360;
		}
	}
}
