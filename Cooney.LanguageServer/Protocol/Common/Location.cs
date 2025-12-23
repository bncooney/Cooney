namespace Cooney.LanguageServer.Protocol.Common;

/// <summary>
/// Represents a location inside a resource, such as a line inside a text file.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Location"/> struct.
/// </remarks>
/// <param name="uri">The document URI.</param>
/// <param name="range">The range in the document.</param>
public struct Location(string uri, Range range)
{
	/// <summary>
	/// The URI of the document.
	/// </summary>
	public string Uri { get; set; } = uri;

	/// <summary>
	/// The range inside the document.
	/// </summary>
	public Range Range { get; set; } = range;

	/// <summary>
	/// Returns a string representation of this location.
	/// </summary>
	public override string ToString() => $"{Uri} {Range}";
}
