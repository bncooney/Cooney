using System.Text.Json.Nodes;

namespace Cooney.AI;

/// <summary>
/// Represents a workflow that can be built into JSON format.
/// </summary>
public interface IWorkflow
{
	/// <summary>
	/// Gets the name of the workflow.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Builds the workflow into its JSON representation.
	/// </summary>
	/// <returns>The JsonNode representing the workflow.</returns>
	JsonNode Build();
}

/// <summary>
/// Basic workflow implementation that wraps a JsonNode.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Workflow"/> class.
/// </remarks>
/// <param name="name">The name of the workflow.</param>
/// <param name="jsonNode">The JSON node representing the workflow.</param>
public sealed class Workflow(string name, JsonNode jsonNode) : IWorkflow
{

	/// <inheritdoc/>
	public string Name { get; } = name;

	/// <inheritdoc/>
	public JsonNode Build() => jsonNode;
}
