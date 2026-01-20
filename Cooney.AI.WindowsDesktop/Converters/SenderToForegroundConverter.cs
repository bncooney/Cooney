using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Cooney.AI.WindowsDesktop.Models;

namespace Cooney.AI.WindowsDesktop.Converters;

public class SenderToForegroundConverter : IValueConverter
{
	/// <summary>
	/// Foreground brush for user messages. Set via XAML StaticResource binding.
	/// </summary>
	public Brush UserForeground { get; set; } = Brushes.White;

	/// <summary>
	/// Foreground brush for bot/assistant messages. Set via XAML StaticResource binding.
	/// </summary>
	public Brush BotForeground { get; set; } = Brushes.Black;

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is Sender sender)
		{
			return sender == Sender.User ? UserForeground : BotForeground;
		}
		return BotForeground;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
