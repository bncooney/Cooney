using System.Collections.Generic;

namespace Cooney.AI.Models;

public class TodoList
{
	public Guid Id { get; set; }
	public string Name { get; set; } = "Default";
	public Guid? ContextId { get; set; }
	public DateTime CreatedAt { get; set; }
	public List<TodoItem> Items { get; set; } = [];
}
