using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevChat.Data;
using DevChat.Models;
using Microsoft.EntityFrameworkCore;

namespace DevChat.ViewModels;

public partial class ChatMessageViewModel(ChatMessageEntity entity) : ObservableObject
{
	private readonly IDbContextFactory<ChatDbContext>? _dbFactory;

	private ChatMessageViewModel(IDbContextFactory<ChatDbContext> dbFactory, ChatMessageEntity entity)
		: this(entity)
	{
		_dbFactory = dbFactory;
	}

	public static async Task<ChatMessageViewModel> CreateUserAsync(
		IDbContextFactory<ChatDbContext> dbFactory, Guid conversationId, string text)
	{
		var entity = new ChatMessageEntity
		{
			ConversationId = conversationId,
			Role = "user",
			Content = text,
			CreatedAt = DateTime.UtcNow
		};
		using var db = await dbFactory.CreateDbContextAsync();
		db.ChatMessages.Add(entity);
		await db.SaveChangesAsync();
		return new ChatMessageViewModel(entity);
	}

	public static ChatMessageViewModel CreateAssistantPlaceholder(
		IDbContextFactory<ChatDbContext> dbFactory, Guid conversationId)
	{
		var entity = new ChatMessageEntity
		{
			ConversationId = conversationId,
			Role = "assistant",
			CreatedAt = DateTime.UtcNow
		};
		return new ChatMessageViewModel(dbFactory, entity);
	}

	public async Task PersistAsync()
	{
		entity.Content = Content;
		using var db = await _dbFactory!.CreateDbContextAsync();
		db.ChatMessages.Add(entity);
		await db.SaveChangesAsync();
	}

	public string Role => entity.Role;

	public bool IsUser => string.Equals(Role, "user", StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// The markdown content that has been flushed for FlowDocument rendering.
	/// Updated in batches to avoid expensive re-renders on every token.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Content))]
	[NotifyPropertyChangedFor(nameof(MarkdownContent))]
	private string _renderedContent = entity.Content;

	/// <summary>
	/// Raw text that has arrived since the last flush. Displayed as plain text
	/// until the next flush moves it into <see cref="RenderedContent"/>.
	/// </summary>
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Content))]
	private string _pendingContent = string.Empty;

	/// <summary>
	/// The full content of the message (rendered + pending).
	/// </summary>
	public string Content => RenderedContent + PendingContent;

	/// <summary>
	/// Content formatted for Markdown rendering. Applies hard breaks to user
	/// messages so that plain-text newlines survive the Markdown renderer.
	/// </summary>
	public string MarkdownContent => IsUser
		? RenderedContent.Replace("\r\n", "\n").Replace("\n", "  \n")
		: RenderedContent;

	[ObservableProperty]
	private bool _isRawMode;

	[RelayCommand]
	private void ToggleRawMode() => IsRawMode = !IsRawMode;

	/// <summary>
	/// Moves all pending text into the rendered portion, triggering a single
	/// FlowDocument re-render.
	/// </summary>
	public void Flush()
	{
		if (PendingContent.Length == 0)
			return;

		RenderedContent += PendingContent;
		PendingContent = string.Empty;
	}
}
