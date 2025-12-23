using Cooney.LanguageServer.Protocol.Common;

namespace Cooney.LanguageServer.Protocol.TextDocument;

/// <summary>
/// An item to transfer a text document from the client to the server.
/// </summary>
public class TextDocumentItem
{
	/// <summary>
	/// The text document's URI.
	/// </summary>
	public string Uri { get; set; } = string.Empty;

	/// <summary>
	/// The text document's language identifier.
	/// </summary>
	public string LanguageId { get; set; } = string.Empty;

	/// <summary>
	/// The version number of this document (it will increase after each change, including undo/redo).
	/// </summary>
	public int Version { get; set; }

	/// <summary>
	/// The content of the opened text document.
	/// </summary>
	public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Parameters for the `textDocument/didOpen` notification.
/// </summary>
public class DidOpenTextDocumentParams
{
	/// <summary>
	/// The document that was opened.
	/// </summary>
	public TextDocumentItem TextDocument { get; set; } = new();
}

/// <summary>
/// Parameters for the `textDocument/didClose` notification.
/// </summary>
public class DidCloseTextDocumentParams
{
	/// <summary>
	/// The document that was closed.
	/// </summary>
	public TextDocumentIdentifier TextDocument { get; set; }
}
