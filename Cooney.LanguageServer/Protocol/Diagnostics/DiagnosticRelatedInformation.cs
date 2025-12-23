using Cooney.LanguageServer.Protocol.Common;

namespace Cooney.LanguageServer.Protocol.Diagnostics;

/// <summary>
/// Represents a related message and source code location for a diagnostic.
/// This should be used to point to code locations that cause or related to a diagnostics.
/// </summary>
public class DiagnosticRelatedInformation
{
	/// <summary>
	/// The location of this related diagnostic information.
	/// </summary>
	public Location Location { get; set; }

	/// <summary>
	/// The message of this related diagnostic information.
	/// </summary>
	public string Message { get; set; } = string.Empty;
}
