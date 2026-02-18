using System.IO;
using DevChat.Models;
using Microsoft.EntityFrameworkCore;

namespace DevChat.Data;

public class ChatDbContext(DbContextOptions<ChatDbContext> options) : DbContext(options)
{
	public static string DatabaseFolder { get; } =
		Path.Combine(Path.GetTempPath(), "DevChat");

	public static string DatabasePath { get; } =
		Path.Combine(DatabaseFolder, "devchat.db");

	public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
	public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<ConversationEntity>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasMany(e => e.Messages)
				.WithOne(e => e.Conversation)
				.HasForeignKey(e => e.ConversationId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ChatMessageEntity>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
		});
	}
}
