using System.Windows.Media;

namespace Cooney.AI.WindowsDesktop.Helpers;

/// <summary>
/// Dracula theme colors based on Official Dracula Theme (https://draculatheme.com).
/// </summary>
public class DraculaThemeColors : IThemeColors
{
    // Background Colors
    public Brush BackgroundPrimary { get; } = CreateFrozenBrush(0x28, 0x2A, 0x36);
    public Brush BackgroundSecondary { get; } = CreateFrozenBrush(0x44, 0x47, 0x5A);
    public Brush BackgroundTertiary { get; } = CreateFrozenBrush(0x34, 0x37, 0x46);
    public Brush BackgroundElevated { get; } = CreateFrozenBrush(0x4D, 0x50, 0x66);

    // Text Colors
    public Brush TextPrimary { get; } = CreateFrozenBrush(0xF8, 0xF8, 0xF2);
    public Brush TextSecondary { get; } = CreateFrozenBrush(0x62, 0x72, 0xA4);
    public Brush TextDisabled { get; } = CreateFrozenBrush(0x54, 0x59, 0x77);
    public Brush TextBright { get; } = CreateFrozenBrush(0xFF, 0xFF, 0xFF);

    // Accent Colors (Purple and Pink)
    public Brush AccentPrimary { get; } = CreateFrozenBrush(0xBD, 0x93, 0xF9);
    public Brush AccentSecondary { get; } = CreateFrozenBrush(0xFF, 0x79, 0xC6);
    public Brush AccentHighlight { get; } = CreateFrozenBrush(0x44, 0x47, 0x5A);

    // Chat Message Colors
    public Brush UserMessageBackground { get; } = CreateFrozenBrush(0x44, 0x47, 0x5A);
    public Brush BotMessageBackground { get; } = CreateFrozenBrush(0x34, 0x37, 0x46);

    // Border Colors
    public Brush BorderPrimary { get; } = CreateFrozenBrush(0x62, 0x72, 0xA4);
    public Brush BorderSecondary { get; } = CreateFrozenBrush(0x44, 0x47, 0x5A);
    public Brush BorderFocused { get; } = CreateFrozenBrush(0xBD, 0x93, 0xF9);

    // Code Block Colors
    public Brush CodeBackground { get; } = CreateFrozenBrush(0x28, 0x2A, 0x36);
    public Brush CodeForeground { get; } = CreateFrozenBrush(0xF8, 0xF8, 0xF2);

    // Semantic Colors
    public Brush Success { get; } = CreateFrozenBrush(0x50, 0xFA, 0x7B);
    public Brush Warning { get; } = CreateFrozenBrush(0xFF, 0xB8, 0x6C);
    public Brush Error { get; } = CreateFrozenBrush(0xFF, 0x55, 0x55);
    public Brush Info { get; } = CreateFrozenBrush(0x8B, 0xE9, 0xFD);

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
