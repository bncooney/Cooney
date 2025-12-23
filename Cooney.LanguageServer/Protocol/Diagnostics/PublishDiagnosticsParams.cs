using System;

namespace Cooney.LanguageServer.Protocol.Diagnostics;

/// <summary>
/// Parameters for the `textDocument/publishDiagnostics` notification.
/// </summary>
public class PublishDiagnosticsParams
{
	/// <summary>
	/// The URI for which diagnostic information is reported.
	/// </summary>
	public string Uri { get; set; } = string.Empty;

	/// <summary>
	/// Optional the version number of the document the diagnostics are published for.
	/// </summary>
	public int? Version { get; set; }

	/// <summary>
	/// An array of diagnostic information items.
	/// </summary>
	public Diagnostic[] Diagnostics { get; set; } = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="PublishDiagnosticsParams"/> class.
	/// </summary>
	public PublishDiagnosticsParams()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PublishDiagnosticsParams"/> class.
	/// </summary>
	/// <param name="uri">The URI for which diagnostic information is reported.</param>
	/// <param name="diagnostics">The diagnostics.</param>
	public PublishDiagnosticsParams(string uri, Diagnostic[] diagnostics)
	{
		Uri = uri;
		Diagnostics = diagnostics;
	}
}
