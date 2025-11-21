namespace AI;

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
	/// <returns>The JSON string representing the workflow.</returns>
	string Build();
}
