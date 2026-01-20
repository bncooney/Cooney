using System.Windows.Media;

namespace Cooney.AI.WindowsDesktop.Helpers;

/// <summary>
/// Darcula theme colors based on IntelliJ IDEA Darcula Theme.
/// </summary>
public class DarculaThemeColors : IThemeColors
{
    // Background Colors
    public Brush BackgroundPrimary { get; } = CreateFrozenBrush(0x2B, 0x2B, 0x2B);
    public Brush BackgroundSecondary { get; } = CreateFrozenBrush(0x3C, 0x3F, 0x41);
    public Brush BackgroundTertiary { get; } = CreateFrozenBrush(0x31, 0x33, 0x35);
    public Brush BackgroundElevated { get; } = CreateFrozenBrush(0x45, 0x48, 0x4A);

    // Text Colors
    public Brush TextPrimary { get; } = CreateFrozenBrush(0xBB, 0xBB, 0xBB);
    public Brush TextSecondary { get; } = CreateFrozenBrush(0x80, 0x80, 0x80);
    public Brush TextDisabled { get; } = CreateFrozenBrush(0x60, 0x60, 0x60);
    public Brush TextBright { get; } = CreateFrozenBrush(0xFF, 0xFF, 0xFF);

    // Accent Colors
    public Brush AccentPrimary { get; } = CreateFrozenBrush(0x4A, 0x88, 0xC7);
    public Brush AccentSecondary { get; } = CreateFrozenBrush(0x58, 0x9D, 0xF6);
    public Brush AccentHighlight { get; } = CreateFrozenBrush(0x21, 0x42, 0x83);

    // Chat Message Colors
    public Brush UserMessageBackground { get; } = CreateFrozenBrush(0x36, 0x58, 0x80);
    public Brush BotMessageBackground { get; } = CreateFrozenBrush(0x3C, 0x3F, 0x41);

    // Border Colors
    public Brush BorderPrimary { get; } = CreateFrozenBrush(0x55, 0x55, 0x55);
    public Brush BorderSecondary { get; } = CreateFrozenBrush(0x32, 0x32, 0x32);
    public Brush BorderFocused { get; } = CreateFrozenBrush(0x4A, 0x88, 0xC7);

    // Code Block Colors
    public Brush CodeBackground { get; } = CreateFrozenBrush(0x2B, 0x2B, 0x2B);
    public Brush CodeForeground { get; } = CreateFrozenBrush(0xA9, 0xB7, 0xC6);

    // Semantic Colors
    public Brush Success { get; } = CreateFrozenBrush(0x6A, 0x87, 0x59);
    public Brush Warning { get; } = CreateFrozenBrush(0xBE, 0x91, 0x17);
    public Brush Error { get; } = CreateFrozenBrush(0xCC, 0x66, 0x6E);
    public Brush Info { get; } = CreateFrozenBrush(0x4A, 0x88, 0xC7);

    private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
