using System;

namespace Cooney.Geospatial
{
	/// <summary>
	/// Simple test class to verify Haversine distance and bearing calculations
	/// </summary>
	public static class TestHaversine
	{
		public static void RunTests()
		{
			Console.WriteLine("Testing Haversine Distance and Bearing Calculations...\n");

			// Test 1: Sydney to Melbourne distance
			TestSydneyMelbourneDistance();

			// Test 2: North Pole to Equator distance
			TestPoleToEquatorDistance();

			// Test 3: Bearing calculations
			TestBearingCalculations();

			// Test 4: Same point distance (should be 0)
			TestSamePointDistance();

			Console.WriteLine("All tests completed!");
		}

		private static void TestSydneyMelbourneDistance()
		{
			var sydney = new GeographicCoordinates(-33.8688, 151.2093, 0);
			var melbourne = new GeographicCoordinates(-37.8136, 144.9631, 0);

			double distance = Wgs84Ellipsoid.HaversineDistance(sydney, melbourne);
			double distanceKm = distance / 1000.0;

			Console.WriteLine("Test 1: Sydney to Melbourne");
			Console.WriteLine($"  Distance: {distanceKm:F1} km");
			Console.WriteLine($"  Expected: ~713.7 km");
			Console.WriteLine($"  Status: {(Math.Abs(distanceKm - 713.7) < 1.0 ? "PASS" : "FAIL")}");
			Console.WriteLine();
		}

		private static void TestPoleToEquatorDistance()
		{
			var northPole = new GeographicCoordinates(90, 0, 0);
			var equator = new GeographicCoordinates(0, 0, 0);

			double distance = Wgs84Ellipsoid.HaversineDistance(northPole, equator);
			double distanceKm = distance / 1000.0;

			Console.WriteLine("Test 2: North Pole to Equator");
			Console.WriteLine($"  Distance: {distanceKm:F1} km");
			Console.WriteLine($"  Expected: ~10,007.5 km (quarter meridian)");
			Console.WriteLine($"  Status: {(Math.Abs(distanceKm - 10007.5) < 10.0 ? "PASS" : "FAIL")}");
			Console.WriteLine();
		}

		private static void TestBearingCalculations()
		{
			var sydney = new GeographicCoordinates(-33.8688, 151.2093, 0);
			var melbourne = new GeographicCoordinates(-37.8136, 144.9631, 0);

			double initialBearing = Wgs84Ellipsoid.CalculateInitialBearing(sydney, melbourne);
			double finalBearing = Wgs84Ellipsoid.CalculateFinalBearing(sydney, melbourne);

			Console.WriteLine("Test 3: Bearing Calculations");
			Console.WriteLine($"  Initial bearing (Sydney to Melbourne): {initialBearing:F1}°");
			Console.WriteLine($"  Final bearing (Melbourne arrival): {finalBearing:F1}°");
			Console.WriteLine($"  Expected initial: ~230.1°");
			Console.WriteLine($"  Expected final: ~228.3°");
			Console.WriteLine($"  Status: {(Math.Abs(initialBearing - 230.1) < 1.0 && Math.Abs(finalBearing - 228.3) < 1.0 ? "PASS" : "FAIL")}");
			Console.WriteLine();
		}

		private static void TestSamePointDistance()
		{
			var point = new GeographicCoordinates(-33.8688, 151.2093, 0);

			double distance = Wgs84Ellipsoid.HaversineDistance(point, point);

			Console.WriteLine("Test 4: Same Point Distance");
			Console.WriteLine($"  Distance: {distance:F3} meters");
			Console.WriteLine($"  Expected: 0 meters");
			Console.WriteLine($"  Status: {(distance < 0.001 ? "PASS" : "FAIL")}");
			Console.WriteLine();
		}
	}
}
