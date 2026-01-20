using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Cooney.AI.WindowsDesktop.ViewModels;

/// <summary>
/// Interface for ChatViewModel to ensure consistency between runtime and design-time implementations.
/// </summary>
public interface IChatViewModel
{
	ObservableCollection<ChatMessageViewModel> Messages { get; }
	string CurrentInput { get; set; }
	bool IsBusy { get; set; }
	ICommand SendCommand { get; }
	ICommand ClearCommand { get; }
	ICommand CancelCommand { get; }
}
