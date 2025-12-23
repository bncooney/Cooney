using CommunityToolkit.Diagnostics;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Cooney.LanguageServer.Transport;

/// <summary>
/// Named pipe transport for local language servers (Windows).
/// </summary>
public sealed class NamedPipeTransport : ILanguageServerTransport
{
	private readonly string _pipeName;
	private NamedPipeClientStream? _pipeStream;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="NamedPipeTransport"/> class.
	/// </summary>
	/// <param name="pipeName">The name of the named pipe.</param>
	public NamedPipeTransport(string pipeName)
	{
		Guard.IsNotNullOrWhiteSpace(pipeName, nameof(pipeName));
		_pipeName = pipeName;
	}

	/// <inheritdoc/>
	public async Task ConnectAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(NamedPipeTransport));

		if (_pipeStream != null)
			throw new InvalidOperationException("Transport is already connected.");

		_pipeStream = new NamedPipeClientStream(
			serverName: ".",
			pipeName: _pipeName,
			direction: PipeDirection.InOut,
			options: PipeOptions.Asynchronous);

		try
		{
			await _pipeStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
		}
		catch
		{
			_pipeStream?.Dispose();
			_pipeStream = null;
			throw;
		}
	}

	/// <inheritdoc/>
	public Stream InputStream
	{
		get
		{
			if (_pipeStream == null || !_pipeStream.IsConnected)
				throw new InvalidOperationException("Transport is not connected.");
			return _pipeStream;
		}
	}

	/// <inheritdoc/>
	public Stream OutputStream
	{
		get
		{
			if (_pipeStream == null || !_pipeStream.IsConnected)
				throw new InvalidOperationException("Transport is not connected.");
			return _pipeStream;
		}
	}

	/// <inheritdoc/>
	public bool IsConnected => _pipeStream?.IsConnected ?? false;

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		_pipeStream?.Dispose();
		_pipeStream = null;
	}
}
