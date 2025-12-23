using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cooney.LanguageServer.Transport;

/// <summary>
/// Abstraction for language server transport mechanisms (stdio, TCP, named pipes, etc.).
/// </summary>
public interface ILanguageServerTransport : IDisposable
{
	/// <summary>
	/// Connects to the language server.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>A task that completes when the connection is established.</returns>
	Task ConnectAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the input stream for reading messages from the server.
	/// </summary>
	Stream InputStream { get; }

	/// <summary>
	/// Gets the output stream for writing messages to the server.
	/// </summary>
	Stream OutputStream { get; }

	/// <summary>
	/// Gets a value indicating whether the transport is connected.
	/// </summary>
	bool IsConnected { get; }
}
