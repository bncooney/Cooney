using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DevMap.Tiles;

public class TileFetcher : IDisposable
{
	private readonly HttpClient _httpClient;

	public TileFetcher()
	{
		_httpClient = new HttpClient();
		_httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("DevMap/1.0 (educational project)");
		_httpClient.Timeout = TimeSpan.FromSeconds(10);
	}

	public async Task<byte[]?> FetchTileAsync(TileCoordinate coord, CancellationToken ct = default)
	{
		try
		{
			var response = await _httpClient.GetAsync(coord.ToUrl(), ct);
			if (!response.IsSuccessStatusCode)
				return null;
			return await response.Content.ReadAsByteArrayAsync(ct);
		}
		catch (Exception) when (!ct.IsCancellationRequested)
		{
			return null;
		}
	}

	public void Dispose()
	{
		_httpClient.Dispose();
	}
}
