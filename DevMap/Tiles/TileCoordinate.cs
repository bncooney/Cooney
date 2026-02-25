using System.IO;

namespace DevMap.Tiles;

public readonly record struct TileCoordinate(int Zoom, int X, int Y)
{
	public string ToUrl() =>
		$"https://tile.openstreetmap.org/{Zoom}/{X}/{Y}.png";

	public string ToCacheFileName() => $"{Zoom}_{X}_{Y}.png";

	public string ToCacheFilePath(string cacheDir) =>
		Path.Combine(cacheDir, ToCacheFileName());
}
