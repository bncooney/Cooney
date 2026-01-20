using System.Windows.Media;

namespace Cooney.AI.WindowsDesktop.Helpers;

/// <summary>
/// Interface defining theme colors for use in code-behind and converters.
/// Implement this interface to create custom color themes.
/// </summary>
public interface IThemeColors
{
    // Background Colors
    Brush BackgroundPrimary { get; }
    Brush BackgroundSecondary { get; }
    Brush BackgroundTertiary { get; }
    Brush BackgroundElevated { get; }

    // Text Colors
    Brush TextPrimary { get; }
    Brush TextSecondary { get; }
    Brush TextDisabled { get; }
    Brush TextBright { get; }

    // Accent Colors
    Brush AccentPrimary { get; }
    Brush AccentSecondary { get; }
    Brush AccentHighlight { get; }

    // Chat Message Colors
    Brush UserMessageBackground { get; }
    Brush BotMessageBackground { get; }

    // Border Colors
    Brush BorderPrimary { get; }
    Brush BorderSecondary { get; }
    Brush BorderFocused { get; }

    // Code Block Colors
    Brush CodeBackground { get; }
    Brush CodeForeground { get; }

    // Semantic Colors
    Brush Success { get; }
    Brush Warning { get; }
    Brush Error { get; }
    Brush Info { get; }
}
