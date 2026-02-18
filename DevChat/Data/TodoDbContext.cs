using Cooney.AI.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace DevChat.Data;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
	public static string DatabasePath { get; } =
		Path.Combine(ChatDbContext.DatabaseFolder, "todos.db");

	public DbSet<TodoList> TodoLists => Set<TodoList>();
	public DbSet<TodoItem> TodoItems => Set<TodoItem>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TodoList>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => e.ContextId);
			entity.HasMany(e => e.Items)
				.WithOne(e => e.TodoList)
				.HasForeignKey(e => e.TodoListId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<TodoItem>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.Status).HasConversion<string>();
			entity.Property(e => e.Priority).HasConversion<string>();
		});
	}
}
