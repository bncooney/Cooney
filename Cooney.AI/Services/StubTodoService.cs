using Cooney.AI.Models;
using System.Collections.Generic;
using System.Linq;

namespace Cooney.AI.Services;

/// <summary>
/// In-memory <see cref="ITodoService"/> used when no persistent service is injected.
/// State lives for the lifetime of the instance.
/// </summary>
public class StubTodoService : ITodoService
{
	private readonly object _lock = new();
	private readonly Dictionary<Guid, TodoList> _lists = new();
	private readonly TodoList _defaultList = new() { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };

	public Task<TodoList> ReadAsync(Guid? contextId = null, CancellationToken ct = default)
		=> Task.FromResult(GetOrCreate(contextId));

	public Task<TodoList> WriteAsync(IEnumerable<TodoItem> items, Guid? contextId = null, CancellationToken ct = default)
	{
		lock (_lock)
		{
			var list = GetOrCreate(contextId);
			var itemList = items as List<TodoItem> ?? [.. items];

			if (itemList.Count == 0)
			{
				list.Items.Clear();
			}
			else
			{
				foreach (var item in itemList)
				{
					item.TodoListId = list.Id;
					var idx = list.Items.FindIndex(t => t.Id == item.Id);
					if (idx >= 0)
						list.Items[idx] = item;
					else
						list.Items.Add(item);
				}
			}

			return Task.FromResult(list);
		}
	}

	private TodoList GetOrCreate(Guid? contextId)
	{
		if (!contextId.HasValue)
			return _defaultList;

		if (!_lists.TryGetValue(contextId.Value, out var list))
		{
			list = new TodoList { Id = Guid.NewGuid(), ContextId = contextId, CreatedAt = DateTime.UtcNow };
			_lists[contextId.Value] = list;
		}

		return list;
	}
}
