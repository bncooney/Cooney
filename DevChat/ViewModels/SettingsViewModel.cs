using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevChat.ViewModels;

public partial class SettingsViewModel(string databasePath) : ObservableObject
{
	public string DatabasePath { get; } = databasePath;

	[RelayCommand]
	private void OpenDatabaseFolder()
	{
		Process.Start("explorer.exe", Path.GetDirectoryName(DatabasePath)!);
	}
}
