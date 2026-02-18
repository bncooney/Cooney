using Cooney.AI.Models;
using System.Collections.Generic;

namespace Cooney.AI.Services;

public interface ITodoService
{
	Task<TodoList> ReadAsync(Guid? contextId = null, CancellationToken ct = default);
	Task<TodoList> WriteAsync(IEnumerable<TodoItem> items, Guid? contextId = null, CancellationToken ct = default);
}
