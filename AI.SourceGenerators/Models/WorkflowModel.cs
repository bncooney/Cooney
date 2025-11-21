namespace AI.SourceGenerators.Models;

/// <summary>
/// Represents a parsed workflow from a JSON file.
/// </summary>
internal sealed class WorkflowModel
{
	/// <summary>
	/// Gets the name of the workflow (derived from the filename without extension).
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the original JSON content with mustache parameters.
	/// </summary>
	public string JsonTemplate { get; }

	/// <summary>
	/// Gets the list of parameter names extracted from mustache syntax ({{paramName}}).
	/// </summary>
	public IReadOnlyList<string> Parameters { get; }

	/// <summary>
	/// Gets the file path of the source JSON file.
	/// </summary>
	public string FilePath { get; }

	public WorkflowModel(string name, string jsonTemplate, IReadOnlyList<string> parameters, string filePath)
	{
		Name = name;
		JsonTemplate = jsonTemplate;
		Parameters = parameters;
		FilePath = filePath;
	}
}
