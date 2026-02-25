using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DevMap.Tiles;

public class TileCache
{
	private readonly string _diskCacheDir;
	private readonly Dictionary<TileCoordinate, byte[]> _memoryCache = [];
	private readonly LinkedList<TileCoordinate> _lruOrder = new();
	private readonly int _maxMemoryEntries;
	private readonly Lock _lock = new();

	public TileCache(int maxMemoryEntries = 256)
	{
		_maxMemoryEntries = maxMemoryEntries;
		_diskCacheDir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"DevMap", "tilecache");
		Directory.CreateDirectory(_diskCacheDir);
	}

	public byte[]? TryGet(TileCoordinate coord)
	{
		lock (_lock)
		{
			// Check memory first
			if (_memoryCache.TryGetValue(coord, out var data))
			{
				// Move to front of LRU
				_lruOrder.Remove(coord);
				_lruOrder.AddFirst(coord);
				return data;
			}

			// Check disk
			string path = coord.ToCacheFilePath(_diskCacheDir);
			if (File.Exists(path))
			{
				data = File.ReadAllBytes(path);
				AddToMemory(coord, data);
				return data;
			}

			return null;
		}
	}

	public void Store(TileCoordinate coord, byte[] pngData)
	{
		lock (_lock)
		{
			// Store to disk
			string path = coord.ToCacheFilePath(_diskCacheDir);
			File.WriteAllBytes(path, pngData);

			// Store to memory
			AddToMemory(coord, pngData);
		}
	}

	private void AddToMemory(TileCoordinate coord, byte[] data)
	{
		if (_memoryCache.ContainsKey(coord))
		{
			_lruOrder.Remove(coord);
		}
		else if (_memoryCache.Count >= _maxMemoryEntries)
		{
			// Evict LRU entry
			var lru = _lruOrder.Last!.Value;
			_lruOrder.RemoveLast();
			_memoryCache.Remove(lru);
		}

		_memoryCache[coord] = data;
		_lruOrder.AddFirst(coord);
	}
}
