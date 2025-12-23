namespace Cooney.LanguageServer.Protocol.Initialize;

/// <summary>
/// Defines capabilities the editor / tool provides on text documents.
/// </summary>
public class TextDocumentClientCapabilities
{
	/// <summary>
	/// Capabilities specific to the `textDocument/definition` request.
	/// </summary>
	public DefinitionCapability? Definition { get; set; }

	/// <summary>
	/// Capabilities specific to the `textDocument/references` request.
	/// </summary>
	public ReferencesCapability? References { get; set; }

	/// <summary>
	/// Capabilities specific to the `textDocument/publishDiagnostics` notification.
	/// </summary>
	public PublishDiagnosticsCapability? PublishDiagnostics { get; set; }
}

/// <summary>
/// Capabilities specific to the `textDocument/definition` request.
/// </summary>
public class DefinitionCapability
{
	/// <summary>
	/// Whether definition supports dynamic registration.
	/// </summary>
	public bool? DynamicRegistration { get; set; }

	/// <summary>
	/// The client supports additional metadata in the form of definition links.
	/// </summary>
	public bool? LinkSupport { get; set; }
}

/// <summary>
/// Capabilities specific to the `textDocument/references` request.
/// </summary>
public class ReferencesCapability
{
	/// <summary>
	/// Whether references supports dynamic registration.
	/// </summary>
	public bool? DynamicRegistration { get; set; }
}

/// <summary>
/// Capabilities specific to the `textDocument/publishDiagnostics` notification.
/// </summary>
public class PublishDiagnosticsCapability
{
	/// <summary>
	/// Whether the client accepts diagnostics with related information.
	/// </summary>
	public bool? RelatedInformation { get; set; }

	/// <summary>
	/// Client supports the tag property to provide meta data about a diagnostic.
	/// </summary>
	public bool? TagSupport { get; set; }

	/// <summary>
	/// Client supports a codeDescription property.
	/// </summary>
	public bool? CodeDescriptionSupport { get; set; }
}

/// <summary>
/// Defines the capabilities provided by the client.
/// </summary>
public class ClientCapabilities
{
	/// <summary>
	/// Text document specific client capabilities.
	/// </summary>
	public TextDocumentClientCapabilities? TextDocument { get; set; }
}
