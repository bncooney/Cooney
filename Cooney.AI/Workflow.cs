using System.Text.Json.Nodes;

namespace Cooney.AI;

/// <summary>
/// Basic workflow implementation that wraps a JsonNode.
/// </summary>
/// <remarks>
/// Initialises a new instance of the <see cref="Workflow"/> class.
/// </remarks>
/// <param name="name">The name of the workflow.</param>
/// <param name="jsonNode">The JSON node representing the workflow.</param>
public class Workflow
{
	/// <summary>
	/// Builds and returns the JSON representation of the workflow.
	/// </summary>
	/// <returns>A <see cref="JsonNode"/> representing the workflow structure.</returns>
	public virtual JsonNode Build() => new JsonObject();
}

public class DynamicWorkflow(JsonNode jsonNode) : Workflow()
{
	/// <inheritdoc/>
	public override JsonNode Build() => jsonNode;
}
