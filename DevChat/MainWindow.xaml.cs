using System.Windows;
using DevChat.ViewModels;

namespace DevChat;

public partial class MainWindow : Window
{
	public MainWindow(MainViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
	}
}
