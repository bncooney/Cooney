using Microsoft.Extensions.AI;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cooney.AI.Tools;

/// <summary>
/// Simple todo‑list manager that can be called as an AI function.
/// Supports "read" (return current list) and "write" (replace the whole list).
/// </summary>
/// <remarks>
/// The implementation is deliberately lightweight – it stores the list in a static
/// field so that the state persists only for the lifetime of the process.
/// Replace the static store with a DB/file if you need persistence across calls.
/// </remarks>
public class Todo : AIFunction
{
	// In‑memory store – thread‑safe enough for demo purposes.
	private static readonly List<TodoItem> _store = [];

	private const string ReadAction = "read";
	private const string WriteAction = "write";

	public override string Name => "todo";
	public override string Description =>
		"Manages a simple todo list. " +
		"Use action='read' to get the current list of todos. " +
		"Use action='write' with a 'todos' array to replace the entire list. " +
		"Each todo item must have 'id', 'content', 'status', and 'priority'.";

	// --------------------------------------------------------------------
	// JSON schema – mirrors the structure described in the spec.
	// --------------------------------------------------------------------
	private static readonly JsonElement s_jsonSchema;
	public override JsonElement JsonSchema => s_jsonSchema;

	static Todo()
	{
		s_jsonSchema = JsonSerializer.SerializeToElement(new
		{
			type = "object",
			properties = new
			{
				action = new
				{
					type = "string",
					@enum = new[] { ReadAction, WriteAction }
				},
				todos = new
				{
					type = "array",
					items = new
					{
						type = "object",
						properties = new
						{
							id = new { type = "string" },
							content = new { type = "string" },
							status = new { type = "string", @enum = new[] { "pending", "in_progress", "completed", "cancelled" } },
							priority = new { type = "string", @enum = new[] { "high", "medium", "low" } }
						},
						required = new[] { "id", "content", "status", "priority" }
					}
				}
			},
			required = new[] { "action" },
			additionalProperties = false
		});
	}

	// --------------------------------------------------------------------
	// Core logic – called by the AI runtime.
	// --------------------------------------------------------------------
	protected override async ValueTask<object?> InvokeCoreAsync(
		AIFunctionArguments arguments,
		CancellationToken cancellationToken)
	{
		// --------------------------------------------------------------------
		// 1️⃣  Extract the "action" parameter (required)
		// --------------------------------------------------------------------
		if (!arguments.TryGetValue("action", out var actionObj) || actionObj is not string action)
		{
			return CreateErrorResponse("Missing required parameter: action", "InvalidParameter");
		}

		// --------------------------------------------------------------------
		// 2️⃣  Dispatch based on the action
		// --------------------------------------------------------------------
		return action.Equals(ReadAction, StringComparison.OrdinalIgnoreCase)
			? await HandleReadAsync(arguments, cancellationToken)
			: await HandleWriteAsync(arguments, cancellationToken);
	}

	// --------------------------------------------------------------------
	// READ – returns the current todo list (or an empty array)
	// --------------------------------------------------------------------
	private async ValueTask<object?> HandleReadAsync(AIFunctionArguments _1, CancellationToken _2)
	{
		// No extra arguments are expected for a read.
		// Return the whole list wrapped in the same shape the write expects.
		var response = new JsonObject
		{
			["action"] = ReadAction,
			["todos"] = ToJsonArray(_store)
		};

		return response;
	}

	// --------------------------------------------------------------------
	// WRITE – replace the entire todo list with the supplied array.
	// --------------------------------------------------------------------
	private async ValueTask<object?> HandleWriteAsync(AIFunctionArguments arguments, CancellationToken _)
	{
		// The "todos" property must be present and be an array.
		if (!arguments.TryGetValue("todos", out var todosObj) || todosObj is not JsonElement todosElement ||
			!todosElement.TryGetProperty("todos", out var todosProp) || !todosProp.ValueKind.Equals(JsonValueKind.Array))
		{
			return CreateErrorResponse("Missing or invalid 'todos' array for write action.", "InvalidParameter");
		}

		// Deserialize the supplied array into a List<TodoItem>.
		List<TodoItem>? newList;
		try
		{
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			newList = JsonSerializer.Deserialize<List<TodoItem>>(todosProp.GetRawText(), options);
		}
		catch (JsonException je)
		{
			return CreateErrorResponse($"Failed to deserialize 'todos': {je.Message}", "DeserializationError");
		}

		if (newList == null || newList.Count == 0)
		{
			return CreateErrorResponse("'todos' array cannot be empty.", "InvalidParameter");
		}

		// Validate each item.
		var validationErrors = ValidateTodoItems(newList);
		if (validationErrors.Count > 0)
		{
			// Return the first error – the caller can adjust the payload.
			var firstError = validationErrors.First();
			return CreateErrorResponse(firstError, "ValidationError");
		}

		// Replace the in‑memory store.
		lock (_store)
		{
			_store.Clear();
			_store.AddRange(newList);
		}

		// Echo back the newly stored list (useful for confirmation).
		var response = new JsonObject
		{
			["action"] = WriteAction,
			["todos"] = ToJsonArray(_store)
		};

		return response;
	}

	// --------------------------------------------------------------------
	// Helper: validate a collection of TodoItem objects.
	// Returns a list of human‑readable error messages (empty when valid).
	// --------------------------------------------------------------------
	private static List<string> ValidateTodoItems(IEnumerable<TodoItem> items)
	{
		var errors = new List<string>();

		foreach (var item in items)
		{
			if (string.IsNullOrWhiteSpace(item.Id))
				errors.Add("Each todo must have a non‑empty 'id'.");
			if (string.IsNullOrWhiteSpace(item.Content))
				errors.Add("Each todo must have a non‑empty 'content'.");
			if (!Enum.IsDefined(typeof(TodoStatus), item.Status))
				errors.Add($"Invalid 'status' value '{item.Status}'. Allowed: pending, in_progress, completed, cancelled.");
			if (!Enum.IsDefined(typeof(TodoPriority), item.Priority))
				errors.Add($"Invalid 'priority' value '{item.Priority}'. Allowed: high, medium, low.");
		}

		return errors;
	}

	// --------------------------------------------------------------------
	// Helper: convert the current store to a JSON array for the response.
	// --------------------------------------------------------------------
	private static JsonArray ToJsonArray(List<TodoItem> items)
	{
		var jsonArray = new JsonArray();
		foreach (var item in items)
		{
			var obj = new JsonObject
			{
				["id"] = item.Id,
				["content"] = item.Content,
				["status"] = JsonValue.Create(item.Status.ToString()),
				["priority"] = JsonValue.Create(item.Priority.ToString())
			};
			jsonArray.Add(obj);
		}
		return jsonArray;
	}

	// --------------------------------------------------------------------
	// Helper: build a standard error response object.
	// --------------------------------------------------------------------
	private static JsonObject CreateErrorResponse(string errorMessage, string errorType = "InvalidParameter")
	{
		return new JsonObject
		{
			["success"] = false,
			["error"] = errorMessage,
			["error_type"] = errorType
		};
	}

	// --------------------------------------------------------------------
	// POCOs that map to the JSON structure.
	// --------------------------------------------------------------------
	private enum TodoStatus { pending, in_progress, completed, cancelled }
	private enum TodoPriority { high, medium, low }

	private sealed class TodoItem
	{
		[JsonPropertyName("id")]
		public string Id { get; set; } = default!;

		[JsonPropertyName("content")]
		public string Content { get; set; } = default!;

		[JsonPropertyName("status")]
		public TodoStatus Status { get; set; }

		[JsonPropertyName("priority")]
		public TodoPriority Priority { get; set; }
	}
}
