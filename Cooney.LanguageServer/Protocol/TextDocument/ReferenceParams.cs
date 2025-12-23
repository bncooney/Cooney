using Cooney.LanguageServer.Protocol.Common;

namespace Cooney.LanguageServer.Protocol.TextDocument;

/// <summary>
/// Parameters for the `textDocument/references` request.
/// </summary>
public class ReferenceParams
{
	/// <summary>
	/// The text document.
	/// </summary>
	public TextDocumentIdentifier TextDocument { get; set; }

	/// <summary>
	/// The position inside the text document.
	/// </summary>
	public Position Position { get; set; }

	/// <summary>
	/// The reference context.
	/// </summary>
	public ReferenceContext Context { get; set; } = new();
}
