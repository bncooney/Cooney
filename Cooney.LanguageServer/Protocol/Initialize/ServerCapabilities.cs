namespace Cooney.LanguageServer.Protocol.Initialize;

/// <summary>
/// Defines how the server handles definition requests.
/// </summary>
public class DefinitionOptions
{
	/// <summary>
	/// Whether the server supports definition.
	/// </summary>
	public bool? WorkDoneProgress { get; set; }
}

/// <summary>
/// Defines how the server handles references requests.
/// </summary>
public class ReferencesOptions
{
	/// <summary>
	/// Whether the server supports finding references.
	/// </summary>
	public bool? WorkDoneProgress { get; set; }
}

/// <summary>
/// Defines the capabilities the language server provides.
/// </summary>
public class ServerCapabilities
{
	/// <summary>
	/// The server provides definition support.
	/// </summary>
	public bool? DefinitionProvider { get; set; }

	/// <summary>
	/// The server provides find references support.
	/// </summary>
	public bool? ReferencesProvider { get; set; }

	/// <summary>
	/// Defines how text documents are synced. Is either a detailed structure defining each notification or
	/// for backwards compatibility the TextDocumentSyncKind number.
	/// </summary>
	public object? TextDocumentSync { get; set; }
}
