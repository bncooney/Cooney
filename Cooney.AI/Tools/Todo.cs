using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cooney.AI.Models;
using Cooney.AI.Services;
using System.Collections.Generic;

namespace Cooney.AI.Tools;

/// <summary>
/// Simple todo‑list manager that can be called as an AI function.
/// Supports "read" (return current list) and "write" (upsert/clear).
/// </summary>
/// <remarks>
/// Pass an <see cref="ITodoService"/> for persistent storage; omit it to fall back to
/// an in-process <see cref="StubTodoService"/>. Pass a <paramref name="contextId"/> to
/// scope the list to a specific context (e.g. a conversation id).
/// </remarks>
public class Todo : AIFunction
{
	private readonly ITodoService _service;
	private readonly Guid? _contextId;

	public Todo(ITodoService? service = null, Guid? contextId = null)
	{
		_service = service ?? new StubTodoService();
		_contextId = contextId;
	}

	private const string ReadAction = "read";
	private const string WriteAction = "write";

	public override string Name => "todo";
	public override string Description =>
		"Manages a simple todo list. " +
		"Use action='read' (or 'list'/'get') to get the current list. " +
		"Use action='write' with a 'todos' array to upsert todos: items whose 'id' matches an existing todo are updated in place; items with a new or omitted 'id' are appended. Unmentioned todos are left unchanged. Pass an empty array to clear the entire list. " +
		"Each todo item requires 'content'. " +
		"'id' is optional and will be auto-generated if omitted; supply an existing id to update that item. " +
		"'status' defaults to 'pending' if omitted; accepted values (and common aliases): pending/todo, in_progress/wip/active, completed/done, cancelled/canceled. " +
		"'priority' defaults to 'medium' if omitted; accepted values (and common aliases): high/urgent/critical, medium/normal, low/minor.";

	private static readonly JsonElement s_jsonSchema;
	public override JsonElement JsonSchema => s_jsonSchema;

	static Todo()
	{
		var todoItemSchema = new
		{
			type = "object",
			properties = new
			{
				id = new { type = "string", description = "Optional. Auto-generated if omitted." },
				content = new { type = "string" },
				status = new
				{
					type = "string",
					description = "Defaults to 'pending' if omitted.",
					@enum = new[] { "pending", "in_progress", "completed", "cancelled" }
				},
				priority = new
				{
					type = "string",
					description = "Defaults to 'medium' if omitted.",
					@enum = new[] { "high", "medium", "low" }
				}
			},
			required = new[] { "content" }
		};

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
					items = todoItemSchema
				}
			},
			required = new[] { "action" },
			@if = new { properties = new { action = new { @const = WriteAction } } },
			then = new { required = new[] { "action", "todos" } }
		});
	}

	protected override async ValueTask<object?> InvokeCoreAsync(
		AIFunctionArguments arguments,
		CancellationToken cancellationToken)
	{
		arguments.TryGetValue("action", out var actionObj);

		var action = actionObj switch
		{
			string s => s,
			JsonElement { ValueKind: JsonValueKind.String } je => je.GetString(),
			_ => null
		};

		// No action or empty action defaults to read.
		var normalizedAction = action?.Trim().ToLowerInvariant() switch
		{
			null or "" or "read" or "list" or "get" => ReadAction,
			_ => WriteAction
		};

		return normalizedAction == ReadAction
			? await HandleReadAsync(cancellationToken)
			: await HandleWriteAsync(arguments, cancellationToken);
	}

	private async ValueTask<object?> HandleReadAsync(CancellationToken ct)
	{
		var list = await _service.ReadAsync(_contextId, ct);
		return new JsonObject
		{
			["action"] = ReadAction,
			["todos"] = ToJsonArray(list.Items)
		};
	}

	private async ValueTask<object?> HandleWriteAsync(AIFunctionArguments arguments, CancellationToken ct)
	{
		if (!arguments.TryGetValue("todos", out var todosObj) || todosObj is not JsonElement todosElement ||
			todosElement.ValueKind != JsonValueKind.Array)
		{
			return CreateErrorResponse("Missing or invalid 'todos' array for write action.", "InvalidParameter");
		}

		var incoming = new List<TodoItem>();
		foreach (var element in todosElement.EnumerateArray())
		{
			var (item, error) = ParseTodoItem(element);
			if (error != null)
				return CreateErrorResponse(error, "ValidationError");
			incoming.Add(item!);
		}

		var list = await _service.WriteAsync(incoming, _contextId, ct);
		return new JsonObject
		{
			["action"] = WriteAction,
			["todos"] = ToJsonArray(list.Items)
		};
	}

	private static (TodoItem? item, string? error) ParseTodoItem(JsonElement element)
	{
		var id = GetString(element, "id");
		if (string.IsNullOrWhiteSpace(id))
			id = Guid.NewGuid().ToString("N").Substring(0, 8);

		var content = GetString(element, "content");
		if (string.IsNullOrWhiteSpace(content))
			return (null, "Each todo item must have a non-empty 'content'.");

		var statusRaw = GetString(element, "status") ?? "pending";
		if (!TryNormalizeStatus(statusRaw, out var status))
			return (null, $"Unrecognized status '{statusRaw}'. Try: pending, in_progress, completed, or cancelled.");

		var priorityRaw = GetString(element, "priority") ?? "medium";
		if (!TryNormalizePriority(priorityRaw, out var priority))
			return (null, $"Unrecognized priority '{priorityRaw}'. Try: high, medium, or low.");

		return (new TodoItem { Id = id, Content = content, Status = status, Priority = priority }, null);
	}

	private static string? GetString(JsonElement element, string propertyName)
	{
		if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
			return prop.GetString();
		return null;
	}

	private static bool TryNormalizeStatus(string raw, out TodoStatus status)
	{
		status = (raw.Trim().ToLowerInvariant().Replace("-", "_").Replace(" ", "_")) switch
		{
			"pending" or "todo" or "not_started" or "open" => TodoStatus.pending,
			"in_progress" or "active" or "wip" or "doing" or "started" or "in_process" => TodoStatus.in_progress,
			"completed" or "done" or "finished" or "closed" or "resolved" => TodoStatus.completed,
			"cancelled" or "canceled" or "skipped" or "dropped" or "abandoned" => TodoStatus.cancelled,
			_ => (TodoStatus)(-1)
		};
		return (int)status >= 0;
	}

	private static bool TryNormalizePriority(string raw, out TodoPriority priority)
	{
		priority = raw.Trim().ToLowerInvariant() switch
		{
			"high" or "urgent" or "critical" or "important" or "hi" => TodoPriority.high,
			"medium" or "med" or "normal" or "moderate" or "mid" or "default" => TodoPriority.medium,
			"low" or "lo" or "minor" or "trivial" => TodoPriority.low,
			_ => (TodoPriority)(-1)
		};
		return (int)priority >= 0;
	}

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
