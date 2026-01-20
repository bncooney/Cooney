using System.Windows;

namespace Cooney.AI.WindowsDesktop.Views;

/// <summary>
/// Main application window. Hosts views with navigation.
/// </summary>
public partial class MainWindow : Window
{
	public MainWindow(ViewModels.MainWindowViewModel viewModel)
	{
		InitializeComponent();
		DataContext = viewModel;
	}
}
