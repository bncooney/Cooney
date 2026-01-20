using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Cooney.AI.Services;
using Cooney.AI.WindowsDesktop.Models;

namespace Cooney.AI.WindowsDesktop.ViewModels;

public partial class ChatViewModel : ObservableRecipient, IChatViewModel
{
	partial void OnCurrentInputChanged(string value)
	{
		SendCommand?.NotifyCanExecuteChanged();
	}

	private readonly IChatService _chatService;
	private CancellationTokenSource? _cancellationTokenSource;

	[ObservableProperty]
	private ObservableCollection<ChatMessageViewModel> _messages = [];

	[ObservableProperty]
	private string _currentInput = string.Empty;

	[ObservableProperty]
	private bool _isBusy;

	public IAsyncRelayCommand SendCommand { get; }
	public IRelayCommand ClearCommand { get; }
	public IRelayCommand CancelCommand { get; }

	// Explicit interface implementation for ICommand
	ICommand IChatViewModel.SendCommand => SendCommand;
	ICommand IChatViewModel.ClearCommand => ClearCommand;
	ICommand IChatViewModel.CancelCommand => CancelCommand;

	public ChatViewModel(IChatService chatService)
	{
		_chatService = chatService;
		SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
		ClearCommand = new RelayCommand(Clear);
		CancelCommand = new RelayCommand(Cancel, CanCancel);
	}

	private bool CanSend()
	{
		return !IsBusy && !string.IsNullOrWhiteSpace(CurrentInput);
	}

	private async Task SendAsync()
	{
		var userMessage = CurrentInput.Trim();
		CurrentInput = string.Empty;
		IsBusy = true;
		_cancellationTokenSource = new CancellationTokenSource();
		NotifyCommandsCanExecuteChanged();

		// Add user message to UI
		var userModel = new ChatMessage(Sender.User, userMessage);
		Messages.Add(new ChatMessageViewModel(userModel));

		// Add assistant message with debouncing enabled for streaming
		var assistantModel = new ChatMessage(Sender.Assistant, string.Empty);
		var assistantViewModel = new ChatMessageViewModel(assistantModel, enableDebouncing: true);
		Messages.Add(assistantViewModel);

		var wasCancelled = false;
		try
		{
			await foreach (var update in _chatService.SendMessageAsync(userMessage, _cancellationTokenSource.Token))
			{
				if (!string.IsNullOrEmpty(update.Text))
				{
					assistantViewModel.AppendText(update.Text);
				}
			}
		}
		catch (OperationCanceledException)
		{
			wasCancelled = true;
		}
		finally
		{
			assistantViewModel.Flush();

			if (wasCancelled)
			{
				if (string.IsNullOrEmpty(assistantViewModel.RenderedText) && string.IsNullOrEmpty(assistantViewModel.PendingText))
				{
					assistantViewModel.AppendText("[Cancelled]");
				}
				else
				{
					assistantViewModel.AppendText(" [Cancelled]");
				}
				assistantViewModel.Flush();
			}
			else if (string.IsNullOrEmpty(assistantViewModel.RenderedText) && string.IsNullOrEmpty(assistantViewModel.PendingText))
			{
				assistantViewModel.AppendText("[No response received]");
				assistantViewModel.Flush();
			}

			_cancellationTokenSource.Dispose();
			_cancellationTokenSource = null;
			IsBusy = false;
			NotifyCommandsCanExecuteChanged();
		}
	}

	private bool CanCancel() => IsBusy && _cancellationTokenSource is { IsCancellationRequested: false };

	private void Cancel()
	{
		_cancellationTokenSource?.Cancel();
		CancelCommand.NotifyCanExecuteChanged();
	}

	private void NotifyCommandsCanExecuteChanged()
	{
		SendCommand.NotifyCanExecuteChanged();
		CancelCommand.NotifyCanExecuteChanged();
	}

	private void Clear()
	{
		Messages.Clear();
		_chatService.ClearHistory();
	}
}
