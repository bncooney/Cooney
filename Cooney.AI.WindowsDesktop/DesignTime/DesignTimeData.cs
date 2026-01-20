using System.Collections.ObjectModel;
using System.Windows.Input;
using Cooney.AI.WindowsDesktop.Models;
using Cooney.AI.WindowsDesktop.ViewModels;

namespace Cooney.AI.WindowsDesktop.DesignTime;

/// <summary>
/// Provides design-time data for the XAML designer.
/// </summary>
public class DesignTimeChatViewModel : IChatViewModel
{
	public ObservableCollection<ChatMessageViewModel> Messages { get; } =
	[
		new ChatMessageViewModel(new ChatMessage(Sender.User, "Hello! What can you help me with?")),
		new ChatMessageViewModel(new ChatMessage(Sender.Assistant, "I'm an **AI assistant** that can help you with:\n\n- Answering questions\n- Writing and editing text\n- *Code assistance*\n- And much more!")),
		new ChatMessageViewModel(new ChatMessage(Sender.User, "Show me a comparison table")),
		new ChatMessageViewModel(new ChatMessage(Sender.Assistant, "Here's a quick comparison:\n\n| Feature | Free | Pro |\n|---------|:----:|:---:|\n| Basic chat | Yes | Yes |\n| Code assist | Limited | Unlimited |\n| Priority | Normal | **High** |\n\nLet me know if you need more details!"))
	];

	public string CurrentInput { get; set; } = "Type your message here...";

	public bool IsBusy { get; set; } = false;

	// Design-time commands (no-op)
	public ICommand SendCommand { get; } = new NoOpCommand();
	public ICommand ClearCommand { get; } = new NoOpCommand();
	public ICommand CancelCommand { get; } = new NoOpCommand();

	/// <summary>
	/// A no-operation command for design-time use.
	/// </summary>
	private class NoOpCommand : ICommand
	{
		public event EventHandler? CanExecuteChanged
		{
			add { }
			remove { }
		}
		public bool CanExecute(object? parameter) => true;
		public void Execute(object? parameter) { }
	}
}
