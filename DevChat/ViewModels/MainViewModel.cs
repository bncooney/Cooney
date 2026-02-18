using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Cooney.AI.Services;
using DevChat.Data;
using Microsoft.EntityFrameworkCore;

namespace DevChat.ViewModels;

public partial class MainViewModel(
	IChatService chatService,
	IDbContextFactory<ChatDbContext> dbFactory,
	ITodoService todoService,
	SettingsViewModel settingsViewModel) : ObservableObject, IAsyncDisposable
{
	private readonly IChatService _chatService = chatService;
	private readonly IDbContextFactory<ChatDbContext> _dbFactory = dbFactory;
	private readonly ITodoService _todoService = todoService;
	private readonly Dictionary<Guid, ChatViewModel> _chatViewModels = [];

	public ObservableCollection<ConversationViewModel> Conversations { get; } = [];

	public SettingsViewModel Settings { get; } = settingsViewModel;

	[ObservableProperty]
	private ConversationViewModel? _selectedConversation;

	[ObservableProperty]
	private object? _currentView;

	[ObservableProperty]
	private bool _isNavPaneOpen = true;

	public async Task LoadConversationsAsync()
	{
		using var db = await _dbFactory.CreateDbContextAsync();
		var entities = await db.Conversations
			.OrderByDescending(c => c.CreatedAt)
			.ToListAsync();

		foreach (var entity in entities)
		{
			var conversation = new ConversationViewModel(entity);
			var chatVm = new ChatViewModel(_chatService, _dbFactory, _todoService, conversation);
			_chatViewModels[conversation.Id] = chatVm;

			Conversations.Add(conversation);
		}
	}

	async partial void OnSelectedConversationChanged(ConversationViewModel? value)
	{
		if (value is not null && _chatViewModels.TryGetValue(value.Id, out var vm))
		{
			await vm.LoadMessagesAsync();
			CurrentView = vm;
		}
	}

	[RelayCommand]
	private async Task NewChatAsync()
	{
		var conversation = await ConversationViewModel.CreateNewAsync(_dbFactory);
		var chatVm = new ChatViewModel(_chatService, _dbFactory, _todoService, conversation);
		_chatViewModels[conversation.Id] = chatVm;

		Conversations.Insert(0, conversation);
		SelectedConversation = conversation;
	}

	[RelayCommand]
	private async Task DeleteConversationAsync(ConversationViewModel conversation)
	{
		if (SelectedConversation == conversation)
		{
			SelectedConversation = null;
			CurrentView = null;
		}

		Conversations.Remove(conversation);

		if (_chatViewModels.Remove(conversation.Id, out var vm))
			await vm.DisposeAsync();

		using var db = await _dbFactory.CreateDbContextAsync();
		var entity = await db.Conversations.FindAsync(conversation.Id);
		if (entity is not null)
		{
			db.Conversations.Remove(entity);
			await db.SaveChangesAsync();
		}
	}

	[RelayCommand]
	private void ToggleNavPane()
	{
		IsNavPaneOpen = !IsNavPaneOpen;
	}

	[RelayCommand]
	private void ShowSettings()
	{
		SelectedConversation = null;
		CurrentView = Settings;
	}

	public async ValueTask DisposeAsync()
	{
		foreach (var vm in _chatViewModels.Values)
			await vm.DisposeAsync();

		_chatViewModels.Clear();
		GC.SuppressFinalize(this);
	}
}
