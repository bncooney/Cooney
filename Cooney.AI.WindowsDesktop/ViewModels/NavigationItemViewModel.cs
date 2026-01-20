using CommunityToolkit.Mvvm.ComponentModel;

namespace Cooney.AI.WindowsDesktop.ViewModels;

public partial class NavigationItemViewModel : ObservableObject
{
	[ObservableProperty]
	private string _icon = string.Empty;

	[ObservableProperty]
	private string _label = string.Empty;

	[ObservableProperty]
	private string _tag = string.Empty;

	[ObservableProperty]
	private object? _content;
}
