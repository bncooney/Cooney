using Cooney.Geospatial;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Cooney.Geospatial.Test
{
	[TestClass]
	public class HaversineTests
	{
		[TestMethod]
		public void TestHaversineDistance_SydneyToMelbourne()
		{
			// Arrange
			var sydney = new GeographicCoordinates(-33.8688, 151.2093, 0);
			var melbourne = new GeographicCoordinates(-37.8136, 144.9631, 0);

			// Act
			double distance = Wgs84Ellipsoid.HaversineDistance(sydney, melbourne);
			double distanceKm = distance / 1000.0;

			// Assert
			Assert.IsLessThan(1.0, Math.Abs(distanceKm - 713.7), $"Expected ~713.7 km, got {distanceKm:F1} km");
		}

		[TestMethod]
		public void TestHaversineDistance_PoleToEquator()
		{
			// Arrange
			var northPole = new GeographicCoordinates(90, 0, 0);
			var equator = new GeographicCoordinates(0, 0, 0);

			// Act
			double distance = Wgs84Ellipsoid.HaversineDistance(northPole, equator);
			double distanceKm = distance / 1000.0;

			// Assert - quarter meridian on WGS84 ellipsoid using Haversine (spherical approximation)
			// The actual value is ~10,018.8 km due to using semi-major axis as radius
			Assert.IsLessThan(1.0, Math.Abs(distanceKm - 10018.8), $"Expected ~10,018.8 km, got {distanceKm:F1} km");
		}

		[TestMethod]
		public void TestHaversineDistance_SamePoint()
		{
			// Arrange
			var point = new GeographicCoordinates(-33.8688, 151.2093, 0);

			// Act
			double distance = Wgs84Ellipsoid.HaversineDistance(point, point);

			// Assert - distance should be effectively zero
			Assert.IsLessThan(0.001, distance, $"Expected 0 meters, got {distance:F3} meters");
		}

		[TestMethod]
		public void TestInitialBearing_SydneyToMelbourne()
		{
			// Arrange
			var sydney = new GeographicCoordinates(-33.8688, 151.2093, 0);
			var melbourne = new GeographicCoordinates(-37.8136, 144.9631, 0);

			// Act
			double bearing = Wgs84Ellipsoid.CalculateInitialBearing(sydney, melbourne);

			// Assert - should be approximately 230.1° (southwest)
			Assert.IsLessThan(1.0, Math.Abs(bearing - 230.1), $"Expected ~230.1°, got {bearing:F1}°");
		}

		[TestMethod]
		public void TestFinalBearing_SydneyToMelbourne()
		{
			// Arrange
			var sydney = new GeographicCoordinates(-33.8688, 151.2093, 0);
			var melbourne = new GeographicCoordinates(-37.8136, 144.9631, 0);

			// Act
			double bearing = Wgs84Ellipsoid.CalculateFinalBearing(sydney, melbourne);

			// Assert - should be approximately 233.9° (this is the actual final bearing)
			Assert.IsLessThan(1.0, Math.Abs(bearing - 233.9), $"Expected ~233.9°, got {bearing:F1}°");
		}

		[TestMethod]
		public void TestBearing_NorthToSouth()
		{
			// Arrange - point directly north to point directly south
			var north = new GeographicCoordinates(45, 0, 0);
			var south = new GeographicCoordinates(-45, 0, 0);

			// Act
			double initialBearing = Wgs84Ellipsoid.CalculateInitialBearing(north, south);
			double finalBearing = Wgs84Ellipsoid.CalculateFinalBearing(north, south);

			// Assert - should be 180° (south) and 180° (south, since we're arriving from north)
			Assert.IsLessThan(0.1, Math.Abs(initialBearing - 180.0), $"Expected 180°, got {initialBearing:F1}°");
			Assert.IsLessThan(0.1, Math.Abs(finalBearing - 180.0), $"Expected 180°, got {finalBearing:F1}°");
		}

		[TestMethod]
		public void TestBearing_EastToWest()
		{
			// Arrange - point directly east to point directly west
			var east = new GeographicCoordinates(0, 45, 0);
			var west = new GeographicCoordinates(0, -45, 0);

			// Act
			double initialBearing = Wgs84Ellipsoid.CalculateInitialBearing(east, west);
			double finalBearing = Wgs84Ellipsoid.CalculateFinalBearing(east, west);

			// Assert - should be 270° (west) and 270° (west, since we're arriving from east)
			Assert.IsLessThan(0.1, Math.Abs(initialBearing - 270.0), $"Expected 270°, got {initialBearing:F1}°");
			Assert.IsLessThan(0.1, Math.Abs(finalBearing - 270.0), $"Expected 270°, got {finalBearing:F1}°");
		}

		[TestMethod]
		public void TestBearing_Normalization()
		{
			// Arrange - bearing should be normalized to 0-360° range
			var point1 = new GeographicCoordinates(0, 0, 0);
			var point2 = new GeographicCoordinates(0, 1, 0); // Slightly east

			// Act
			double bearing = Wgs84Ellipsoid.CalculateInitialBearing(point1, point2);

			// Assert - should be in valid range
			Assert.IsTrue(bearing >= 0 && bearing <= 360, $"Bearing {bearing}° is outside valid range [0, 360]");
		}
	}
}
