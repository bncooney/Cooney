namespace Cooney.LanguageServer.Protocol.Common;

/// <summary>
/// Position in a text document expressed as zero-based line and zero-based character offset.
/// A position is between two characters like an 'insert' cursor in an editor.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Position"/> struct.
/// </remarks>
/// <param name="line">Line position (zero-based).</param>
/// <param name="character">Character offset (zero-based).</param>
public struct Position(int line, int character)
{
	/// <summary>
	/// Line position in a document (zero-based).
	/// </summary>
	public int Line { get; set; } = line;

	/// <summary>
	/// Character offset on a line in a document (zero-based).
	/// Assuming that the line is represented as a string, the character value represents
	/// the gap between the character at the specified index and the character at the next index.
	/// </summary>
	public int Character { get; set; } = character;

	/// <summary>
	/// Returns a string representation of this position.
	/// </summary>
	public override string ToString() => $"({Line}, {Character})";
}
