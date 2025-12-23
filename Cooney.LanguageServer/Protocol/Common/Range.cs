namespace Cooney.LanguageServer.Protocol.Common;

/// <summary>
/// A range in a text document expressed as (zero-based) start and end positions.
/// A range is comparable to a selection in an editor. Therefore the end position is exclusive.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Range"/> struct.
/// </remarks>
/// <param name="start">The start position.</param>
/// <param name="end">The end position (exclusive).</param>
public struct Range(Position start, Position end)
{
	/// <summary>
	/// The range's start position.
	/// </summary>
	public Position Start { get; set; } = start;

	/// <summary>
	/// The range's end position (exclusive).
	/// </summary>
	public Position End { get; set; } = end;

	/// <summary>
	/// Returns a string representation of this range.
	/// </summary>
	public override string ToString() => $"{Start}-{End}";
}
