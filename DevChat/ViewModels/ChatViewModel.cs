using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cooney.AI.Services;
using Cooney.AI.Tools;
using DevChat.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace DevChat.ViewModels;

public partial class ChatViewModel(
	IChatService chatService,
	IDbContextFactory<ChatDbContext> dbFactory,
	ITodoService todoService,
	ConversationViewModel conversation) : ObservableObject, IAsyncDisposable
{
	private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(300);

	private readonly IChatService _chatService = chatService;
	private readonly IDbContextFactory<ChatDbContext> _dbFactory = dbFactory;
	private readonly IReadOnlyList<AITool> _tools = [new Todo(todoService, conversation.Id)];
	private bool _messagesLoaded;

	public ConversationViewModel Conversation { get; } = conversation;
	public ObservableCollection<ChatMessageViewModel> Messages { get; } = [];

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SendCommand))]
	private string _userInput = string.Empty;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SendCommand))]
	private bool _isBusy;

	private CancellationTokenSource? _sendCts;

	public async Task LoadMessagesAsync()
	{
		if (_messagesLoaded)
			return;
		_messagesLoaded = true;

		using var db = await _dbFactory.CreateDbContextAsync();
		var entities = await db.ChatMessages
			.Where(m => m.ConversationId == Conversation.Id)
			.OrderBy(m => m.CreatedAt)
			.ToListAsync();

		foreach (var entity in entities)
			Messages.Add(new ChatMessageViewModel(entity));
	}

	private bool CanSend() => !IsBusy && !string.IsNullOrWhiteSpace(UserInput);

	[RelayCommand(CanExecute = nameof(CanSend))]
	private async Task SendAsync()
	{
		var text = UserInput.Trim();
		UserInput = string.Empty;

		// Create, persist, and track user message
		var userMsg = await ChatMessageViewModel.CreateUserAsync(_dbFactory, Conversation.Id, text);
		Messages.Add(userMsg);

		// Auto-title on first message
		if (Messages.Count == 1)
			await ApplyAutoTitleAsync(text);

		// Build API history from Messages (before adding assistant placeholder)
		var history = Messages
			.Select(m => new ChatMessage(
				m.IsUser ? ChatRole.User : ChatRole.Assistant,
				m.Content))
			.ToList();

		// Create assistant placeholder (persisted after streaming completes)
		var assistantItem = ChatMessageViewModel.CreateAssistantPlaceholder(_dbFactory, Conversation.Id);
		Messages.Add(assistantItem);

		IsBusy = true;
		_sendCts = new CancellationTokenSource();

		// Periodic flush timer — drains PendingContent into RenderedContent
		var flushTimer = new DispatcherTimer(DispatcherPriority.Background)
		{
			Interval = FlushInterval
		};
		flushTimer.Tick += (_, _) => assistantItem.Flush();
		flushTimer.Start();

		try
		{
			await foreach (var token in _chatService.GetStreamingResponseAsync(history, _tools, _sendCts.Token))
			{
				assistantItem.PendingContent += token;
			}
		}
		catch (AggregateException)
		{
			// TODO: Handle timeout
		}
		catch (OperationCanceledException)
		{
			// User cancelled — keep whatever was streamed so far
		}
		finally
		{
			flushTimer.Stop();
			assistantItem.Flush();

			if (!string.IsNullOrEmpty(assistantItem.Content))
				await assistantItem.PersistAsync();

			_sendCts.Dispose();
			_sendCts = null;
			IsBusy = false;
		}
	}

	private async Task ApplyAutoTitleAsync(string firstMessage)
	{
		var titleText = firstMessage.ReplaceLineEndings(" ");
		Conversation.Title = titleText.Length <= 40 ? titleText : titleText[..40] + "...";
		using var db = await _dbFactory.CreateDbContextAsync();
		await db.Conversations
			.Where(c => c.Id == Conversation.Id)
			.ExecuteUpdateAsync(s => s.SetProperty(c => c.Title, Conversation.Title));
	}

	[RelayCommand]
	private void CancelSend()
	{
		_sendCts?.Cancel();
	}

	public ValueTask DisposeAsync()
	{
		_sendCts?.Cancel();
		_sendCts?.Dispose();
		GC.SuppressFinalize(this);
		return ValueTask.CompletedTask;
	}
}
