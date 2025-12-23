using Cooney.LanguageServer.Protocol.Common;

namespace Cooney.LanguageServer.Protocol.TextDocument;

/// <summary>
/// Parameters for the `textDocument/definition` request.
/// </summary>
public class DefinitionParams
{
	/// <summary>
	/// The text document.
	/// </summary>
	public TextDocumentIdentifier TextDocument { get; set; }

	/// <summary>
	/// The position inside the text document.
	/// </summary>
	public Position Position { get; set; }
}
