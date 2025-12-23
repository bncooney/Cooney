using CommunityToolkit.Diagnostics;
using Cooney.LanguageServer.Protocol.Common;
using Cooney.LanguageServer.Protocol.Diagnostics;
using Cooney.LanguageServer.Protocol.Initialize;
using Cooney.LanguageServer.Protocol.TextDocument;
using Cooney.LanguageServer.Transport;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cooney.LanguageServer.Client;

/// <summary>
/// Language Server Protocol (LSP) client for communicating with language servers.
/// Supports go to definition, find references, and diagnostics across multiple transports.
/// </summary>
public sealed class LanguageServerClient : IDisposable
{
	private readonly LanguageServerConnection _connection;
	private bool _disposed;
	private bool _initialized;

	/// <summary>
	/// Event raised when diagnostics are published by the server.
	/// </summary>
	public event EventHandler<PublishDiagnosticsParams>? DiagnosticsPublished
	{
		add => _connection.DiagnosticsPublished += value;
		remove => _connection.DiagnosticsPublished -= value;
	}

	private LanguageServerClient(ILanguageServerTransport transport)
	{
		_connection = new LanguageServerConnection(transport);
	}

	/// <summary>
	/// Creates a client that connects via stdio (standard input/output) to a process-based language server.
	/// This is the most common transport mechanism for LSP servers.
	/// </summary>
	/// <param name="serverExecutablePath">Path to the language server executable.</param>
	/// <param name="arguments">Optional command-line arguments for the server.</param>
	/// <returns>A new <see cref="LanguageServerClient"/> instance.</returns>
	public static LanguageServerClient CreateStdioClient(string serverExecutablePath, string? arguments = null)
	{
		Guard.IsNotNullOrWhiteSpace(serverExecutablePath, nameof(serverExecutablePath));
		var transport = new StdioTransport(serverExecutablePath, arguments);
		return new LanguageServerClient(transport);
	}

	/// <summary>
	/// Creates a client that connects via TCP to a network-based language server.
	/// </summary>
	/// <param name="host">The hostname or IP address of the language server.</param>
	/// <param name="port">The port number (1-65535).</param>
	/// <returns>A new <see cref="LanguageServerClient"/> instance.</returns>
	public static LanguageServerClient CreateTcpClient(string host, int port)
	{
		Guard.IsNotNullOrWhiteSpace(host, nameof(host));
		Guard.IsInRange(port, 1, 65535, nameof(port));
		var transport = new TcpTransport(host, port);
		return new LanguageServerClient(transport);
	}

	/// <summary>
	/// Creates a client that connects via named pipe to a local language server (Windows).
	/// </summary>
	/// <param name="pipeName">The name of the named pipe.</param>
	/// <returns>A new <see cref="LanguageServerClient"/> instance.</returns>
	public static LanguageServerClient CreateNamedPipeClient(string pipeName)
	{
		Guard.IsNotNullOrWhiteSpace(pipeName, nameof(pipeName));
		var transport = new NamedPipeTransport(pipeName);
		return new LanguageServerClient(transport);
	}

	/// <summary>
	/// Initializes the connection to the language server.
	/// This must be called before any other operations.
	/// </summary>
	/// <param name="initializeParams">The initialization parameters.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>The server's capabilities.</returns>
	public async Task<InitializeResult> InitializeAsync(
		InitializeParams initializeParams,
		CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(initializeParams, nameof(initializeParams));

		if (_disposed)
			throw new ObjectDisposedException(nameof(LanguageServerClient));

		if (_initialized)
			throw new InvalidOperationException("Client is already initialized.");

		// Connect to the server
		await _connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

		// Send initialize request
		var result = await _connection.InitializeAsync(initializeParams, cancellationToken).ConfigureAwait(false);

		// Send initialized notification
		await _connection.SendInitializedAsync().ConfigureAwait(false);

		_initialized = true;
		return result;
	}

	/// <summary>
	/// Requests the definition location(s) for a symbol at the given position.
	/// </summary>
	/// <param name="textDocument">The text document identifier.</param>
	/// <param name="position">The position of the symbol.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Array of locations where the symbol is defined, or null if not found.</returns>
	public async Task<Location[]?> GetDefinitionAsync(
		TextDocumentIdentifier textDocument,
		Position position,
		CancellationToken cancellationToken = default)
	{
		EnsureInitialized();

		var parameters = new DefinitionParams
		{
			TextDocument = textDocument,
			Position = position
		};

		return await _connection.GetDefinitionAsync(parameters, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Finds all references to a symbol at the given position.
	/// </summary>
	/// <param name="textDocument">The text document identifier.</param>
	/// <param name="position">The position of the symbol.</param>
	/// <param name="includeDeclaration">Whether to include the declaration of the symbol in the results.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Array of locations where the symbol is referenced, or null if not found.</returns>
	public async Task<Location[]?> GetReferencesAsync(
		TextDocumentIdentifier textDocument,
		Position position,
		bool includeDeclaration = true,
		CancellationToken cancellationToken = default)
	{
		EnsureInitialized();

		var parameters = new ReferenceParams
		{
			TextDocument = textDocument,
			Position = position,
			Context = new ReferenceContext
			{
				IncludeDeclaration = includeDeclaration
			}
		};

		return await _connection.GetReferencesAsync(parameters, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Notifies the server that a document was opened.
	/// </summary>
	/// <param name="textDocument">The text document identifier.</param>
	/// <param name="languageId">The language identifier (e.g., "python", "typescript", "csharp").</param>
	/// <param name="version">The document version number.</param>
	/// <param name="text">The full text content of the document.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task DidOpenTextDocumentAsync(
		TextDocumentIdentifier textDocument,
		string languageId,
		int version,
		string text,
		CancellationToken cancellationToken = default)
	{
		Guard.IsNotNullOrWhiteSpace(languageId, nameof(languageId));
		Guard.IsNotNull(text, nameof(text));

		EnsureInitialized();

		var parameters = new DidOpenTextDocumentParams
		{
			TextDocument = new TextDocumentItem
			{
				Uri = textDocument.Uri,
				LanguageId = languageId,
				Version = version,
				Text = text
			}
		};

		await _connection.DidOpenTextDocumentAsync(parameters).ConfigureAwait(false);
	}

	/// <summary>
	/// Notifies the server that a document was closed.
	/// </summary>
	/// <param name="textDocument">The text document identifier.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task DidCloseTextDocumentAsync(
		TextDocumentIdentifier textDocument,
		CancellationToken cancellationToken = default)
	{
		EnsureInitialized();

		var parameters = new DidCloseTextDocumentParams
		{
			TextDocument = textDocument
		};

		await _connection.DidCloseTextDocumentAsync(parameters).ConfigureAwait(false);
	}

	/// <summary>
	/// Notifies the server to shut down gracefully.
	/// Should be called before <see cref="ExitAsync"/>.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	public async Task ShutdownAsync(CancellationToken cancellationToken = default)
	{
		EnsureInitialized();
		await _connection.ShutdownAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Notifies the server to exit.
	/// Should be called after <see cref="ShutdownAsync"/>.
	/// </summary>
	public async Task ExitAsync()
	{
		EnsureInitialized();
		await _connection.ExitAsync().ConfigureAwait(false);
	}

	private void EnsureInitialized()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(LanguageServerClient));

		if (!_initialized)
			throw new InvalidOperationException("Client is not initialized. Call InitializeAsync first.");
	}

	/// <summary>
	/// Disposes the client and closes the connection to the language server.
	/// </summary>
	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		_connection?.Dispose();
	}
}
