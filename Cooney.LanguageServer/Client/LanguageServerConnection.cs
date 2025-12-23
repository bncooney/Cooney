using Cooney.LanguageServer.Protocol.Common;
using Cooney.LanguageServer.Protocol.Diagnostics;
using Cooney.LanguageServer.Protocol.Initialize;
using Cooney.LanguageServer.Protocol.TextDocument;
using Cooney.LanguageServer.Transport;
using StreamJsonRpc;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Cooney.LanguageServer.Client;

/// <summary>
/// Low-level connection to a language server using JSON-RPC over a transport.
/// </summary>
internal sealed class LanguageServerConnection(ILanguageServerTransport transport) : IDisposable
{
	private readonly ILanguageServerTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));
	private JsonRpc? _jsonRpc;
	private bool _disposed;

	/// <summary>
	/// Event raised when diagnostics are published by the server.
	/// </summary>
	public event EventHandler<PublishDiagnosticsParams>? DiagnosticsPublished;

	/// <summary>
	/// Connects to the language server and starts listening for messages.
	/// </summary>
	public async Task ConnectAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(LanguageServerConnection));

		if (_jsonRpc != null)
			throw new InvalidOperationException("Already connected.");

		// Connect the transport
		await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);

		// Create JSON-RPC formatter with System.Text.Json
		var jsonOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			PropertyNameCaseInsensitive = true,
			IgnoreReadOnlyProperties = true,
			IgnoreReadOnlyFields = true
		};

		var formatter = new SystemTextJsonFormatter
		{
			JsonSerializerOptions = jsonOptions
		};

		// Create message handler
		var messageHandler = new HeaderDelimitedMessageHandler(
			_transport.OutputStream,
			_transport.InputStream,
			formatter);

		// Create JSON-RPC connection
		_jsonRpc = new JsonRpc(messageHandler);

		// Register notification handlers
		_jsonRpc.AddLocalRpcMethod("textDocument/publishDiagnostics", new Action<PublishDiagnosticsParams>(OnDiagnosticsPublished));

		// Start listening for messages
		_jsonRpc.StartListening();
	}

	/// <summary>
	/// Sends an initialize request to the server.
	/// </summary>
	public async Task<InitializeResult> InitializeAsync(InitializeParams initializeParams, CancellationToken cancellationToken = default)
	{
		EnsureConnected();
		try
		{
			// Call the generic invoke method with the single parameter
			var result = await _jsonRpc!.InvokeWithParameterObjectAsync<InitializeResult>(
				"initialize",
				initializeParams,
				cancellationToken).ConfigureAwait(false);
			return result ?? throw new LanguageServerException("Initialize request returned null.");
		}
		catch (Exception ex) when (ex is not LanguageServerException)
		{
			throw new LanguageServerException("Failed to initialize language server.", ex);
		}
	}

	/// <summary>
	/// Sends an initialized notification to the server.
	/// </summary>
	public async Task SendInitializedAsync()
	{
		EnsureConnected();
		// LSP initialized notification has an empty params object
		await _jsonRpc!.NotifyWithParameterObjectAsync("initialized", new { }).ConfigureAwait(false);
	}

	/// <summary>
	/// Requests the definition location(s) for a symbol.
	/// </summary>
	public async Task<Location[]?> GetDefinitionAsync(DefinitionParams parameters, CancellationToken cancellationToken = default)
	{
		EnsureConnected();
		try
		{
			var result = await _jsonRpc!.InvokeWithParameterObjectAsync<Location[]?>(
				"textDocument/definition",
				parameters,
				cancellationToken).ConfigureAwait(false);
			return result;
		}
		catch (Exception ex)
		{
			throw new LanguageServerException("Failed to get definition.", ex);
		}
	}

	/// <summary>
	/// Finds all references to a symbol.
	/// </summary>
	public async Task<Location[]?> GetReferencesAsync(ReferenceParams parameters, CancellationToken cancellationToken = default)
	{
		EnsureConnected();
		try
		{
			var result = await _jsonRpc!.InvokeWithParameterObjectAsync<Location[]?>(
				"textDocument/references",
				parameters,
				cancellationToken).ConfigureAwait(false);
			return result;
		}
		catch (Exception ex)
		{
			throw new LanguageServerException("Failed to get references.", ex);
		}
	}

	/// <summary>
	/// Sends a didOpen notification to the server.
	/// </summary>
	public async Task DidOpenTextDocumentAsync(DidOpenTextDocumentParams parameters)
	{
		EnsureConnected();
		await _jsonRpc!.NotifyWithParameterObjectAsync("textDocument/didOpen", parameters).ConfigureAwait(false);
	}

	/// <summary>
	/// Sends a didClose notification to the server.
	/// </summary>
	public async Task DidCloseTextDocumentAsync(DidCloseTextDocumentParams parameters)
	{
		EnsureConnected();
		await _jsonRpc!.NotifyWithParameterObjectAsync("textDocument/didClose", parameters).ConfigureAwait(false);
	}

	/// <summary>
	/// Sends a shutdown request to the server.
	/// </summary>
	public async Task ShutdownAsync(CancellationToken cancellationToken = default)
	{
		EnsureConnected();
		await _jsonRpc!.InvokeAsync("shutdown", cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Sends an exit notification to the server.
	/// </summary>
	public async Task ExitAsync()
	{
		EnsureConnected();
		await _jsonRpc!.NotifyWithParameterObjectAsync("exit", new { }).ConfigureAwait(false);
	}

	private void OnDiagnosticsPublished(PublishDiagnosticsParams parameters)
	{
		DiagnosticsPublished?.Invoke(this, parameters);
	}

	private void EnsureConnected()
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(LanguageServerConnection));

		if (_jsonRpc == null)
			throw new InvalidOperationException("Not connected. Call ConnectAsync first.");
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;

		_jsonRpc?.Dispose();
		_jsonRpc = null;

		_transport?.Dispose();
	}
}
