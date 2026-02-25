using System.Windows;
using System.Windows.Input;

namespace DevMap;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		var page = new GlobeViewPage();
		GameFrame.Navigate(page);
		GameFrame.DataContext = page.GameControl.DataContext;
	}

	private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
	{
		Application.Current.Shutdown();
	}
}
