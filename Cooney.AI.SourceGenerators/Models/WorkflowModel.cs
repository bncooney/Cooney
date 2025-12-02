using System.Collections.Generic;

namespace Cooney.AI.SourceGenerators.Models;

/// <summary>
/// Represents a parsed workflow from a JSON file.
/// </summary>
internal sealed class WorkflowModel(string name, string jsonTemplate, IReadOnlyList<string> parameters, string filePath)
{
	/// <summary>
	/// Gets the name of the workflow (derived from the filename without extension).
	/// </summary>
	public string Name { get; } = name;

	/// <summary>
	/// Gets the original JSON content with mustache parameters.
	/// </summary>
	public string JsonTemplate { get; } = jsonTemplate;

	/// <summary>
	/// Gets the list of parameter names extracted from mustache syntax ({{paramName}}).
	/// </summary>
	public IReadOnlyList<string> Parameters { get; } = parameters;

	/// <summary>
	/// Gets the file path of the source JSON file.
	/// </summary>
	public string FilePath { get; } = filePath;
}
