using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cooney.AI.WindowsDesktop.ViewModels;

public partial class IconButtonViewModel : ObservableObject
{
	[ObservableProperty]
	private string _icon = string.Empty;

	[ObservableProperty]
	private string _label = string.Empty;

	[ObservableProperty]
	private string _toolTip = string.Empty;

	[ObservableProperty]
	private ICommand? _command;

	public IconButtonViewModel() { }

	public IconButtonViewModel(string icon, string label, ICommand command, string? toolTip = null)
	{
		Icon = icon;
		Label = label;
		Command = command;
		ToolTip = toolTip ?? label;
	}
}
