using System.Windows.Media;

namespace Cooney.AI.WindowsDesktop.Helpers;

/// <summary>
/// Static helper class providing theme colors for use in code-behind and converters.
/// This is necessary for classes like MarkdownToFlowDocumentConverter that create
/// UI elements programmatically and cannot use DynamicResource bindings.
/// Use SetTheme() to swap between different color themes.
/// </summary>
public static class ThemeColors
{
    private static IThemeColors _current = new DarculaThemeColors();

    /// <summary>
    /// Gets the current theme.
    /// </summary>
    public static IThemeColors Current => _current;

    /// <summary>
    /// Sets the current theme.
    /// </summary>
    public static void SetTheme(IThemeColors theme)
    {
        _current = theme;
    }

    // Background Colors
    public static Brush BackgroundPrimary => _current.BackgroundPrimary;
    public static Brush BackgroundSecondary => _current.BackgroundSecondary;
    public static Brush BackgroundTertiary => _current.BackgroundTertiary;
    public static Brush BackgroundElevated => _current.BackgroundElevated;

    // Text Colors
    public static Brush TextPrimary => _current.TextPrimary;
    public static Brush TextSecondary => _current.TextSecondary;
    public static Brush TextDisabled => _current.TextDisabled;
    public static Brush TextBright => _current.TextBright;

    // Accent Colors
    public static Brush AccentPrimary => _current.AccentPrimary;
    public static Brush AccentSecondary => _current.AccentSecondary;
    public static Brush AccentHighlight => _current.AccentHighlight;

    // Chat Message Colors
    public static Brush UserMessageBackground => _current.UserMessageBackground;
    public static Brush BotMessageBackground => _current.BotMessageBackground;

    // Border Colors
    public static Brush BorderPrimary => _current.BorderPrimary;
    public static Brush BorderSecondary => _current.BorderSecondary;
    public static Brush BorderFocused => _current.BorderFocused;

    // Code Block Colors
    public static Brush CodeBackground => _current.CodeBackground;
    public static Brush CodeForeground => _current.CodeForeground;

    // Semantic Colors
    public static Brush Success => _current.Success;
    public static Brush Warning => _current.Warning;
    public static Brush Error => _current.Error;
    public static Brush Info => _current.Info;
}
