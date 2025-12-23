using Cooney.LanguageServer.Protocol.Common;

namespace Cooney.LanguageServer.Protocol.Diagnostics;

/// <summary>
/// Represents a diagnostic, such as a compiler error or warning.
/// </summary>
public class Diagnostic
{
	/// <summary>
	/// The range at which the diagnostic applies.
	/// </summary>
	public Range Range { get; set; }

	/// <summary>
	/// The diagnostic's severity. If omitted it is up to the client to interpret diagnostics as error, warning, info or hint.
	/// </summary>
	public DiagnosticSeverity? Severity { get; set; }

	/// <summary>
	/// The diagnostic's code, which might appear in the user interface.
	/// </summary>
	public object? Code { get; set; }

	/// <summary>
	/// An optional property to describe the error code.
	/// </summary>
	public object? CodeDescription { get; set; }

	/// <summary>
	/// A human-readable string describing the source of this diagnostic, e.g. 'typescript' or 'super lint'.
	/// </summary>
	public string? Source { get; set; }

	/// <summary>
	/// The diagnostic's message.
	/// </summary>
	public string Message { get; set; } = string.Empty;

	/// <summary>
	/// Additional metadata about the diagnostic.
	/// </summary>
	public int[]? Tags { get; set; }

	/// <summary>
	/// An array of related diagnostic information, e.g. when symbol-names within a scope collide
	/// all definitions can be marked via this property.
	/// </summary>
	public DiagnosticRelatedInformation[]? RelatedInformation { get; set; }

	/// <summary>
	/// A data entry field that is preserved between a `textDocument/publishDiagnostics`
	/// notification and `textDocument/codeAction` request.
	/// </summary>
	public object? Data { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="Diagnostic"/> class.
	/// </summary>
	public Diagnostic()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Diagnostic"/> class.
	/// </summary>
	/// <param name="range">The range at which the diagnostic applies.</param>
	/// <param name="message">The diagnostic's message.</param>
	public Diagnostic(Range range, string message)
	{
		Range = range;
		Message = message;
	}
}
