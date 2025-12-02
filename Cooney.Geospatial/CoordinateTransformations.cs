using Cooney.Common.Maths;
using System;
using System.Numerics;

namespace Cooney.Geospatial
{
	public static class CoordinateTransformations
	{
		public static Double3x3 CalculateEcefToEnuMatrix(double latitudeInDegrees, double longitudeInDegrees)
		{
			double latRad = latitudeInDegrees * Maths.DegreesToRadians;
			double lonRad = longitudeInDegrees * Maths.DegreesToRadians;

			double sinLat = Math.Sin(latRad);
			double cosLat = Math.Cos(latRad);
			double sinLon = Math.Sin(lonRad);
			double cosLon = Math.Cos(lonRad);

			Double3 east = new(-sinLon, cosLon, 0.0);
			Double3 north = new(-sinLat * cosLon, -sinLat * sinLon, cosLat);
			Double3 up = new(cosLat * cosLon, cosLat * sinLon, sinLat);

			return new Double3x3(east, north, up);
		}

		public static Double3x3 CalculateEnuToEcefMatrix(double latitudeInDegrees, double longitudeInDegrees)
		{
			Double3x3 ecefToEnu = CalculateEcefToEnuMatrix(latitudeInDegrees, longitudeInDegrees);
			return Maths.Transpose(ecefToEnu);
		}

		public static Double3 EcefToEnu(EcefCoordinates ecef, EcefCoordinates origin, Double3x3 ecefToEnuMatrix)
		{
			Double3 relativeEcef = new(
				ecef.X - origin.X,
				ecef.Y - origin.Y,
				ecef.Z - origin.Z
			);

			return Maths.Mul(ecefToEnuMatrix, relativeEcef);
		}

		public static EcefCoordinates EnuToEcef(Double3 enu, EcefCoordinates origin, Double3x3 enuToEcefMatrix)
		{
			Double3 relativeEcef = Maths.Mul(enuToEcefMatrix, enu);

			return new EcefCoordinates(
				relativeEcef.x + origin.X,
				relativeEcef.y + origin.Y,
				relativeEcef.z + origin.Z
			);
		}

		public static Vector3 EnuToUnity(Double3 enu)
		{
			return new Vector3((float)enu.x, (float)enu.z, (float)enu.y);
		}

		public static Double3 UnityToEnu(Vector3 unity)
		{
			return new Double3(unity.X, unity.Z, unity.Y);
		}

		public static Vector3 EcefToUnity(EcefCoordinates ecef, EcefCoordinates origin, Double3x3 ecefToEnuMatrix)
		{
			Double3 enu = EcefToEnu(ecef, origin, ecefToEnuMatrix);
			return EnuToUnity(enu);
		}

		public static EcefCoordinates UnityToEcef(Vector3 unity, EcefCoordinates origin, Double3x3 enuToEcefMatrix)
		{
			Double3 enu = UnityToEnu(unity);
			return EnuToEcef(enu, origin, enuToEcefMatrix);
		}
	}
}
