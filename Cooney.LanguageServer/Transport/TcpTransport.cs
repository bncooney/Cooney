using CommunityToolkit.Diagnostics;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cooney.LanguageServer.Transport;

/// <summary>
/// TCP socket transport for network-based language servers.
/// </summary>
public sealed class TcpTransport : ILanguageServerTransport
{
	private readonly string _host;
	private readonly int _port;
	private TcpClient? _client;
	private NetworkStream? _stream;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="TcpTransport"/> class.
	/// </summary>
	/// <param name="host">The hostname or IP address of the language server.</param>
	/// <param name="port">The port number.</param>
	public TcpTransport(string host, int port)
	{
		Guard.IsNotNullOrWhiteSpace(host, nameof(host));
		Guard.IsInRange(port, 1, 65535, nameof(port));

		_host = host;
		_port = port;
	}

	/// <inheritdoc/>
	public async Task ConnectAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(TcpTransport));

		if (_client != null)
			throw new InvalidOperationException("Transport is already connected.");

		_client = new TcpClient();

		try
		{
			await _client.ConnectAsync(_host, _port).ConfigureAwait(false);
			_stream = _client.GetStream();
		}
		catch
		{
			_client?.Dispose();
			_client = null;
			_stream = null;
			throw;
		}
	}

	/// <inheritdoc/>
	public Stream InputStream
	{
		get
		{
			if (_stream == null)
				throw new InvalidOperationException("Transport is not connected.");
			return _stream;
		}
	}

	/// <inheritdoc/>
	public Stream OutputStream
	{
		get
		{
			if (_stream == null)
				throw new InvalidOperationException("Transport is not connected.");
			return _stream;
		}
	}

	/// <inheritdoc/>
	public bool IsConnected => _client?.Connected ?? false;

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		_stream?.Dispose();
		_stream = null;

		_client?.Dispose();
		_client = null;
	}
}
