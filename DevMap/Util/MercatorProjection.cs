using System;

namespace DevMap.Util;

public static class MercatorProjection
{
	public const double MaxLatitudeDeg = 85.0511287798;
	public const double MaxLatitudeRad = MaxLatitudeDeg * Math.PI / 180.0;

	/// <summary>
	/// Convert latitude (radians) to Mercator V coordinate in [0, 1].
	/// V=0 is the north pole (top of map), V=1 is the south pole (bottom).
	/// </summary>
	public static double LatitudeToMercatorV(double latRad)
	{
		double clamped = Math.Clamp(latRad, -MaxLatitudeRad, MaxLatitudeRad);
		return (1.0 - Math.Log(Math.Tan(clamped) + 1.0 / Math.Cos(clamped)) / Math.PI) / 2.0;
	}

	/// <summary>
	/// Convert longitude index [0..lonSegments] to U coordinate in [0, 1].
	/// </summary>
	public static double LongitudeToU(int lonIndex, int lonSegments)
	{
		return (double)lonIndex / lonSegments;
	}

	/// <summary>
	/// Get the geographic bounds of an OSM tile.
	/// Returns (lonMinDeg, lonMaxDeg, latMinDeg, latMaxDeg).
	/// </summary>
	public static (double lonMin, double lonMax, double latMin, double latMax) TileBounds(int x, int y, int zoom)
	{
		double n = Math.Pow(2, zoom);
		double lonMin = x / n * 360.0 - 180.0;
		double lonMax = (x + 1) / n * 360.0 - 180.0;
		double latMax = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * y / n))) * 180.0 / Math.PI;
		double latMin = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * (y + 1) / n))) * 180.0 / Math.PI;
		return (lonMin, lonMax, latMin, latMax);
	}

	/// <summary>
	/// Determine appropriate zoom level based on camera distance from sphere.
	/// </summary>
	public static int ZoomFromDistance(float distance, float fov, int screenHeight, int maxZoom = 4)
	{
		// At distance ~3 (default), zoom 0-1 is appropriate.
		// As we zoom in (distance decreases toward 1.2), we want higher zoom.
		double zoom = Math.Log2(Math.PI * screenHeight / (fov * distance * 256.0));
		return (int)Math.Clamp(Math.Floor(zoom), 0, maxZoom);
	}

	/// <summary>
	/// Get total number of tiles along each axis at a given zoom level.
	/// </summary>
	public static int TileCount(int zoom) => 1 << zoom;

	/// <summary>
	/// Convert geographic coordinates to absolute pixel position at a given zoom level.
	/// Uses the standard OSM slippy-map formula. At zoom Z the world is (2^Z * 256) pixels wide/tall.
	/// </summary>
	public static (double px, double py) GeoToPixel(double latDeg, double lonDeg, double zoom)
	{
		double n = Math.Pow(2, zoom);
		double mapSize = n * 256.0;

		double px = (lonDeg + 180.0) / 360.0 * mapSize;

		double latRad = latDeg * Math.PI / 180.0;
		double py = (1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * mapSize;

		return (px, py);
	}

	/// <summary>
	/// Convert absolute pixel position back to geographic coordinates at a given zoom level.
	/// </summary>
	public static (double latDeg, double lonDeg) PixelToGeo(double px, double py, double zoom)
	{
		double n = Math.Pow(2, zoom);
		double mapSize = n * 256.0;

		double lonDeg = px / mapSize * 360.0 - 180.0;

		double latRad = Math.Atan(Math.Sinh(Math.PI * (1.0 - 2.0 * py / mapSize)));
		double latDeg = latRad * 180.0 / Math.PI;

		return (latDeg, lonDeg);
	}

	/// <summary>
	/// Get the tile coordinate containing a geographic point at a given integer zoom level.
	/// </summary>
	public static (int x, int y) GeoToTile(double latDeg, double lonDeg, int zoom)
	{
		double n = Math.Pow(2, zoom);

		int x = (int)Math.Floor((lonDeg + 180.0) / 360.0 * n);
		double latRad = latDeg * Math.PI / 180.0;
		int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);

		// Clamp to valid range
		int max = (1 << zoom) - 1;
		x = Math.Clamp(x, 0, max);
		y = Math.Clamp(y, 0, max);

		return (x, y);
	}

	/// <summary>
	/// Convert camera distance to a fractional zoom level (not clamped to integer).
	/// </summary>
	public static double FractionalZoomFromDistance(float distance, float fov, int screenHeight)
	{
		return Math.Log2(Math.PI * screenHeight / (fov * distance * 256.0));
	}

	/// <summary>
	/// Convert a fractional zoom level back to camera distance.
	/// </summary>
	public static float DistanceFromZoom(double zoom, float fov, int screenHeight)
	{
		return (float)(Math.PI * screenHeight / (fov * Math.Pow(2, zoom) * 256.0));
	}
}
