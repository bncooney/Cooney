using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cooney.AI.Tools;

/// <summary>
/// AI function that manages a simple task list.
/// </summary>
/// <remarks>
/// Use todo to manage tasks and their progress. This tool helps you track tasks and their status.
/// - Use action: "read" to view the current todo list
/// - Use action: "write" with the complete todos list to update (replaces everything)
///
/// Each todo item has:
/// - id: A unique string identifier
/// - content: The task description
/// - status: "pending", "in_progress", "completed", or "cancelled"
/// - priority: "high", "medium", or "low"
/// </remarks>
public class Todo : AIFunction
{
	public override string Name => "todo";

	public override string Description =>
		"Use todo to manage a simple task list. This tool helps you track tasks and their progress. " +
		"Use action: 'read' to view the current todo list, or action: 'write' with the complete todos list to update. " +
		"When writing, you must provide the ENTIRE list - this replaces everything.";

	public override JsonElement JsonSchema { get; }

	public Todo()
	{
		var schema = new JsonObject
		{
			["type"] = "object",
			["properties"] = new JsonObject
			{
				["action"] = new JsonObject
				{
					["type"] = "string",
					["description"] = "The action to perform: 'read' to view current todos, 'write' to update the entire list",
					["enum"] = new JsonArray("read", "write")
				},
				["todos"] = new JsonObject
				{
					["type"] = "array",
					["description"] = "The complete list of todos when action is 'write'",
					["items"] = new JsonObject
					{
						["type"] = "object",
						["properties"] = new JsonObject
						{
							["id"] = new JsonObject
							{
								["type"] = "string",
								["description"] = "A unique identifier for the todo item"
							},
							["content"] = new JsonObject
							{
								["type"] = "string",
								["description"] = "The task description"
							},
							["status"] = new JsonObject
							{
								["type"] = "string",
								["description"] = "The task status",
								["enum"] = new JsonArray("pending", "in_progress", "completed", "cancelled")
							},
							["priority"] = new JsonObject
							{
								["type"] = "string",
								["description"] = "The task priority",
								["enum"] = new JsonArray("high", "medium", "low")
							}
						},
						["required"] = new JsonArray("id", "content", "status", "priority")
					}
				}
			},
			["required"] = new JsonArray("action"),
			["additionalProperties"] = false
		};

		JsonSchema = JsonSerializer.Deserialize<JsonElement>(schema.ToJsonString());
	}

	protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
	{
		// Extract action
		if (!arguments.TryGetValue("action", out object? actionObj))
		{
			return CreateErrorResponse("Missing required parameter: action");
		}

		string? action = actionObj?.ToString();
		if (string.IsNullOrWhiteSpace(action))
		{
			return CreateErrorResponse("Parameter 'action' cannot be empty");
		}

		// Handle read action
		if (action == "read")
		{
			return await ReadTodosAsync(cancellationToken);
		}
		// Handle write action
		else if (action == "write")
		{
			return await WriteTodosAsync(arguments, cancellationToken);
		}
		else
		{
			return CreateErrorResponse($"Invalid action: {action}. Must be 'read' or 'write'", "InvalidParameter");
		}
	}

	private async Task<object> ReadTodosAsync(CancellationToken cancellationToken)
	{
		// In a real implementation, this would read from persistent storage
		// For this example, we'll return an empty list
		var response = new JsonObject
		{
			["success"] = true,
			["todos"] = new JsonArray()
		};
		return response;
	}

	private async Task<object> WriteTodosAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
	{
		// Extract todos
		if (!arguments.TryGetValue("todos", out object? todosObj))
		{
			return CreateErrorResponse("Missing required parameter: todos", "InvalidParameter");
		}

		if (todosObj is not JsonArray todosArray)
		{
			return CreateErrorResponse("Parameter 'todos' must be an array", "InvalidParameter");
		}

		var todos = new List<JsonObject>();
		var seenIds = new HashSet<string>();

		foreach (var item in todosArray)
		{
			if (item is not JsonObject todoItem)
			{
				return CreateErrorResponse("Each todo item must be an object", "InvalidParameter");
			}

			// Validate required fields
			if (!todoItem.ContainsKey("id") ||
				!todoItem.ContainsKey("content") ||
				!todoItem.ContainsKey("status") ||
				!todoItem.ContainsKey("priority"))
			{
				return CreateErrorResponse("Each todo item must have id, content, status, and priority", "InvalidParameter");
			}

			string? id = todoItem["id"]?.ToString();
			string? content = todoItem["content"]?.ToString();
			string? status = todoItem["status"]?.ToString();
			string? priority = todoItem["priority"]?.ToString();

			if (string.IsNullOrWhiteSpace(id) ||
				string.IsNullOrWhiteSpace(content) ||
				string.IsNullOrWhiteSpace(status) ||
				string.IsNullOrWhiteSpace(priority))
			{
				return CreateErrorResponse("Todo item fields cannot be empty", "InvalidParameter");
			}

			// Validate status and priority
			var validStatuses = new[] { "pending", "in_progress", "completed", "cancelled" };
			var validPriorities = new[] { "high", "medium", "low" };

			if (!validStatuses.Contains(status))
			{
				return CreateErrorResponse($"Invalid status: {status}. Must be one of: {string.Join(", ", validStatuses)}", "InvalidParameter");
			}

			if (!validPriorities.Contains(priority))
			{
				return CreateErrorResponse($"Invalid priority: {priority}. Must be one of: {string.Join(", ", validPriorities)}", "InvalidParameter");
			}

			// Check for duplicate ids
			if (seenIds.Contains(id))
			{
				return CreateErrorResponse($"Duplicate todo id: {id}", "InvalidParameter");
			}
			seenIds.Add(id);

			// Validate only one task is in_progress
			if (status == "in_progress")
			{
				if (todos.Any(t => t["status"]?.ToString() == "in_progress"))
				{
					return CreateErrorResponse("Only one task can be in_progress at a time", "InvalidParameter");
				}
			}

			todos.Add(todoItem);
		}

		// In a real implementation, this would write to persistent storage
		// For this example, we'll just return success

		var response = new JsonObject
		{
			["success"] = true,
			["message"] = "Todos updated successfully"
		};
		return response;
	}

	private static JsonObject CreateErrorResponse(string errorMessage, string errorType = "InvalidParameter")
	{
		return new JsonObject
		{
			["success"] = false,
			["error"] = errorMessage,
			["error_type"] = errorType
		};
	}
}
