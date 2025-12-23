using CommunityToolkit.Diagnostics;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cooney.LanguageServer.Transport;

/// <summary>
/// Standard input/output (stdio) transport for process-based language servers.
/// This is the most common transport mechanism for LSP servers.
/// </summary>
public sealed class StdioTransport : ILanguageServerTransport
{
	private readonly string _executablePath;
	private readonly string? _arguments;
	private Process? _process;
	private bool _disposed;

	/// <summary>
	/// Initializes a new instance of the <see cref="StdioTransport"/> class.
	/// </summary>
	/// <param name="executablePath">Path to the language server executable.</param>
	/// <param name="arguments">Optional command-line arguments for the server.</param>
	public StdioTransport(string executablePath, string? arguments = null)
	{
		Guard.IsNotNullOrWhiteSpace(executablePath, nameof(executablePath));
		_executablePath = executablePath;
		_arguments = arguments;
	}

	/// <inheritdoc/>
	public async Task ConnectAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(StdioTransport));

		if (_process != null)
			throw new InvalidOperationException("Transport is already connected.");

		var startInfo = new ProcessStartInfo
		{
			FileName = _executablePath,
			Arguments = _arguments ?? string.Empty,
			UseShellExecute = false,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		_process = new Process { StartInfo = startInfo };

		try
		{
			if (!_process.Start())
			{
				throw new InvalidOperationException($"Failed to start language server process: {_executablePath}");
			}

			// Give the process a moment to initialize
			await Task.Delay(100, cancellationToken).ConfigureAwait(false);
		}
		catch
		{
			_process?.Dispose();
			_process = null;
			throw;
		}
	}

	/// <inheritdoc/>
	public Stream InputStream
	{
		get
		{
			if (_process == null || _process.HasExited)
				throw new InvalidOperationException("Transport is not connected or process has exited.");
			return _process.StandardOutput.BaseStream;
		}
	}

	/// <inheritdoc/>
	public Stream OutputStream
	{
		get
		{
			if (_process == null || _process.HasExited)
				throw new InvalidOperationException("Transport is not connected or process has exited.");
			return _process.StandardInput.BaseStream;
		}
	}

	/// <inheritdoc/>
	public bool IsConnected => _process != null && !_process.HasExited;

	/// <inheritdoc/>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		if (_process != null)
		{
			try
			{
				if (!_process.HasExited)
				{
					_process.Kill();
					_process.WaitForExit(1000);
				}
			}
			catch
			{
				// Ignore errors during cleanup
			}
			finally
			{
				_process.Dispose();
				_process = null;
			}
		}
	}
}
