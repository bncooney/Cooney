using Cooney.AI.Models;
using Cooney.AI.Services;
using DevChat.Data;
using Microsoft.EntityFrameworkCore;

namespace DevChat.Services;

public class EfTodoService(IDbContextFactory<TodoDbContext> factory) : ITodoService
{
	public async Task<TodoList> ReadAsync(Guid? contextId = null, CancellationToken ct = default)
	{
		await using var db = await factory.CreateDbContextAsync(ct);
		return await GetOrCreateListAsync(db, contextId, ct);
	}

	public async Task<TodoList> WriteAsync(IEnumerable<TodoItem> items, Guid? contextId = null, CancellationToken ct = default)
	{
		await using var db = await factory.CreateDbContextAsync(ct);
		var list = await GetOrCreateListAsync(db, contextId, ct);
		var itemList = items as List<TodoItem> ?? [.. items];

		if (itemList.Count == 0)
		{
			db.TodoItems.RemoveRange(list.Items);
			list.Items.Clear();
		}
		else
		{
			foreach (var incoming in itemList)
			{
				incoming.TodoListId = list.Id;
				var existing = list.Items.FirstOrDefault(t => t.Id == incoming.Id);
				if (existing is not null)
					db.Entry(existing).CurrentValues.SetValues(incoming);
				else
				{
					list.Items.Add(incoming);
					db.TodoItems.Add(incoming);
				}
			}
		}

		await db.SaveChangesAsync(ct);
		return list;
	}

	private static async Task<TodoList> GetOrCreateListAsync(TodoDbContext db, Guid? contextId, CancellationToken ct)
	{
		var list = await db.TodoLists
			.Include(l => l.Items)
			.FirstOrDefaultAsync(l => l.ContextId == contextId, ct);

		if (list is null)
		{
			list = new TodoList { Id = Guid.NewGuid(), ContextId = contextId, CreatedAt = DateTime.UtcNow };
			db.TodoLists.Add(list);
			await db.SaveChangesAsync(ct);
		}

		return list;
	}
}
