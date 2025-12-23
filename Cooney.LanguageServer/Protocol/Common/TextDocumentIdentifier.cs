namespace Cooney.LanguageServer.Protocol.Common;

/// <summary>
/// Text documents are identified using a URI. On the protocol level, URIs are passed as strings.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TextDocumentIdentifier"/> struct.
/// </remarks>
/// <param name="uri">The text document's URI.</param>
public struct TextDocumentIdentifier(string uri)
{
	/// <summary>
	/// The text document's URI.
	/// </summary>
	public string Uri { get; set; } = uri;

	/// <summary>
	/// Returns a string representation of this identifier.
	/// </summary>
	public override string ToString() => Uri;
}
