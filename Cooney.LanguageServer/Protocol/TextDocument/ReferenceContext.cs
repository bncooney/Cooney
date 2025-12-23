namespace Cooney.LanguageServer.Protocol.TextDocument;

/// <summary>
/// Context information for a references request.
/// </summary>
public class ReferenceContext
{
	/// <summary>
	/// Include the declaration of the current symbol.
	/// </summary>
	public bool IncludeDeclaration { get; set; }
}
