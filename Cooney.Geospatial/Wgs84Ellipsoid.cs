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
	}

}
