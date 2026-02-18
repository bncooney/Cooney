using System.Windows;
using System.Windows.Media;

namespace DevChat;

/// <summary>
/// Provides theme-aware brushes that respond to the system Fluent theme (light/dark).
/// Uses SystemColors where possible so colours update automatically with theme changes.
/// </summary>
public static class ThemeColors
{
	public static Brush BorderPrimary =>
		(Brush)Application.Current.FindResource(SystemColors.ActiveBorderBrushKey);

	public static Brush TextSecondary =>
		(Brush)Application.Current.FindResource(SystemColors.GrayTextBrushKey);

	public static Brush CodeBackground => new SolidColorBrush(Color.FromArgb(30, 128, 128, 128));

	public static Brush CodeForeground =>
		(Brush)Application.Current.FindResource(SystemColors.WindowTextBrushKey);
}
