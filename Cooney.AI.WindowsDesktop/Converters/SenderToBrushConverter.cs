using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Cooney.AI.WindowsDesktop.Models;

namespace Cooney.AI.WindowsDesktop.Converters;

public class SenderToBrushConverter : IValueConverter
{
	/// <summary>
	/// Brush for user message backgrounds. Set via XAML StaticResource binding.
	/// </summary>
	public Brush UserBrush { get; set; } = Brushes.Blue;

	/// <summary>
	/// Brush for bot/assistant message backgrounds. Set via XAML StaticResource binding.
	/// </summary>
	public Brush BotBrush { get; set; } = Brushes.Gray;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Sender sender)
		{
			return sender == Sender.User ? UserBrush : BotBrush;
		}
		return Brushes.Transparent;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
