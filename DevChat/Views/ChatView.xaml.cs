using System.Windows.Controls;
using System.Windows.Input;

namespace DevChat.Views;

public partial class ChatView : UserControl
{
	public ChatView()
	{
		InitializeComponent();
	}

	private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Return && Keyboard.Modifiers != ModifierKeys.Shift)
		{
			e.Handled = true;
			if (DataContext is ViewModels.ChatViewModel vm && vm.SendCommand.CanExecute(null))
				vm.SendCommand.Execute(null);
		}
	}

	/// <summary>
	/// FlowDocumentScrollViewer swallows MouseWheel events even when its scrollbar is disabled.
	/// Re-raise the event so the outer ScrollViewer receives it.
	/// </summary>
	private void FlowDocumentScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
	{
		if (sender is not FlowDocumentScrollViewer)
			return;

		e.Handled = true;
		var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
		{
			RoutedEvent = MouseWheelEvent,
			Source = sender
		};
		MessagesScrollViewer.RaiseEvent(args);
	}
}
