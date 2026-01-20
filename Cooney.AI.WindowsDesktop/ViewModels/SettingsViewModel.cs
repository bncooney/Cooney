using CommunityToolkit.Mvvm.ComponentModel;
using Cooney.AI.WindowsDesktop.Helpers;
using System.Windows;

namespace Cooney.AI.WindowsDesktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private ApplicationTheme _selectedTheme = ApplicationTheme.Darcula;

    partial void OnSelectedThemeChanged(ApplicationTheme value)
    {
        ApplyTheme(value);
    }

    private void ApplyTheme(ApplicationTheme theme)
    {
        // Update the resource dictionary in App.xaml
        var app = App.Current;
        var resourceDictionary = theme == ApplicationTheme.Darcula
            ? new ResourceDictionary { Source = new Uri("Themes/DarculaTheme.xaml", UriKind.Relative) }
            : new ResourceDictionary { Source = new Uri("Themes/DraculaTheme.xaml", UriKind.Relative) };

        // Clear existing merged dictionaries
        app.Resources.MergedDictionaries.Clear();
        
        // Add the selected theme
        app.Resources.MergedDictionaries.Add(resourceDictionary);

        // Update the ThemeColors helper for code-behind usage
        ThemeColors.SetTheme(theme == ApplicationTheme.Darcula
            ? new DarculaThemeColors()
            : new DraculaThemeColors());
    }

    // Initialize with current theme
    public SettingsViewModel()
    {
        // Default to Darcula theme
        ApplyTheme(ApplicationTheme.Darcula);
    }
}
