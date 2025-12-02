using Microsoft.CodeAnalysis;

namespace Cooney.AI.SourceGenerators;

/// <summary>
/// Diagnostic descriptors for the workflow source generator.
/// </summary>
internal static class DiagnosticDescriptors
{
	private const string Category = "WorkflowGenerator";

	/// <summary>
	/// Diagnostic for invalid JSON format in workflow files.
	/// </summary>
	public static readonly DiagnosticDescriptor InvalidJsonFormat = new(
		id: "WFGEN001",
		title: "Invalid JSON format",
		messageFormat: "Workflow file '{0}' contains invalid JSON and will be skipped",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "The workflow JSON file is not well-formed and cannot be parsed.");

	/// <summary>
	/// Diagnostic for duplicate workflow names.
	/// </summary>
	public static readonly DiagnosticDescriptor DuplicateWorkflowName = new(
		id: "WFGEN002",
		title: "Duplicate workflow name",
		messageFormat: "Workflow name '{0}' is already defined in '{1}'. Each workflow must have a unique name.",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true,
		description: "Multiple workflow files resolve to the same class name, which would cause compilation errors.");

	/// <summary>
	/// Informational diagnostic emitted when a workflow is successfully generated.
	/// </summary>
	public static readonly DiagnosticDescriptor WorkflowGenerated = new(
		id: "WFGEN003",
		title: "Workflow generated",
		messageFormat: "Generated workflow class '{0}' with {1} parameter(s) from '{2}'",
		category: Category,
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true,
		description: "A workflow class was successfully generated from a JSON file.");
}
