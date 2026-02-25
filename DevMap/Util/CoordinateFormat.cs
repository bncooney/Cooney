using System;
using System.Globalization;

namespace DevMap.Util;

public static class CoordinateFormat
{
	/// <summary>
	/// Formats latitude and longitude as "48.8566° N, 2.3522° E".
	/// </summary>
	public static string ToDegreesString(double lat, double lon)
	{
		string latDir = lat >= 0 ? "N" : "S";
		string lonDir = lon >= 0 ? "E" : "W";
		return string.Create(CultureInfo.InvariantCulture,
			$"{Math.Abs(lat):F4}\u00b0 {latDir}, {Math.Abs(lon):F4}\u00b0 {lonDir}");
	}
}
