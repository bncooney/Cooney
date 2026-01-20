using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Cooney.AI.WindowsDesktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isNavCollapsed;

    [RelayCommand]
    private void ToggleNav()
    {
        IsNavCollapsed = !IsNavCollapsed;
    }

	[ObservableProperty]
	private ObservableCollection<NavigationItemViewModel> _navigationItems = [];

	[ObservableProperty]
	private ObservableCollection<NavigationItemViewModel> _footerNavigationItems = [];

	[ObservableProperty]
	private NavigationItemViewModel? _selectedNavigationItem;

	[ObservableProperty]
	private NavigationItemViewModel? _selectedFooterNavigationItem;

	[ObservableProperty]
	private object? _currentContent;

	public MainWindowViewModel(ChatViewModel chatViewModel, SettingsViewModel settingsViewModel)
	{
		NavigationItems.Add(new NavigationItemViewModel
		{
			Icon = "\uE8BD",
			Label = "Chat",
			Tag = "Chat",
			Content = chatViewModel
		});

		FooterNavigationItems.Add(new NavigationItemViewModel
		{
			Icon = "\uE713",
			Label = "Settings",
			Tag = "Settings",
			Content = settingsViewModel
		});

		SelectedNavigationItem = NavigationItems[0];
	}

	partial void OnSelectedNavigationItemChanged(NavigationItemViewModel? value)
	{
		if (value != null)
		{
			SelectedFooterNavigationItem = null;
			CurrentContent = value.Content;
		}
	}

	partial void OnSelectedFooterNavigationItemChanged(NavigationItemViewModel? value)
	{
		if (value != null)
		{
			SelectedNavigationItem = null;
			CurrentContent = value.Content;
		}
	}
}
