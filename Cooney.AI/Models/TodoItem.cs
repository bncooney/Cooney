using System.Text.Json.Serialization;

namespace Cooney.AI.Models;

public enum TodoStatus { pending, in_progress, completed, cancelled }
public enum TodoPriority { high, medium, low }

public class TodoItem
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = default!;

	[JsonIgnore]
	public Guid TodoListId { get; set; }

	[JsonPropertyName("content")]
	public string Content { get; set; } = default!;

	[JsonPropertyName("status")]
	public TodoStatus Status { get; set; }

	[JsonPropertyName("priority")]
	public TodoPriority Priority { get; set; }

	[JsonIgnore]
	public TodoList TodoList { get; set; } = null!;
}
