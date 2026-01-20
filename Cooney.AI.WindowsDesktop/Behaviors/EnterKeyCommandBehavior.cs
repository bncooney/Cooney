using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cooney.AI.WindowsDesktop.Behaviors;

/// <summary>
/// Attached behavior that executes a command when Enter is pressed in a TextBox.
/// Shift+Enter allows inserting a newline without triggering the command.
/// </summary>
public static class EnterKeyCommandBehavior
{
	/// <summary>
	/// Gets or sets the command to execute when Enter is pressed.
	/// </summary>
	public static readonly DependencyProperty CommandProperty =
		DependencyProperty.RegisterAttached(
			"Command",
			typeof(ICommand),
			typeof(EnterKeyCommandBehavior),
			new PropertyMetadata(null, OnCommandChanged));

	public static ICommand? GetCommand(DependencyObject obj) =>
		(ICommand?)obj.GetValue(CommandProperty);

	public static void SetCommand(DependencyObject obj, ICommand? value) =>
		obj.SetValue(CommandProperty, value);

	/// <summary>
	/// Gets or sets the command parameter to pass when executing the command.
	/// </summary>
	public static readonly DependencyProperty CommandParameterProperty =
		DependencyProperty.RegisterAttached(
			"CommandParameter",
			typeof(object),
			typeof(EnterKeyCommandBehavior),
			new PropertyMetadata(null));

	public static object? GetCommandParameter(DependencyObject obj) =>
		obj.GetValue(CommandParameterProperty);

	public static void SetCommandParameter(DependencyObject obj, object? value) =>
		obj.SetValue(CommandParameterProperty, value);

	private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not TextBox textBox)
			return;

		if (e.OldValue != null)
		{
			textBox.PreviewKeyDown -= OnPreviewKeyDown;
		}

		if (e.NewValue != null)
		{
			textBox.PreviewKeyDown += OnPreviewKeyDown;
		}
	}

	private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key != Key.Enter || Keyboard.Modifiers == ModifierKeys.Shift)
			return;

		if (sender is not TextBox textBox)
			return;

		var command = GetCommand(textBox);
		var parameter = GetCommandParameter(textBox);

		if (command?.CanExecute(parameter) == true)
		{
			command.Execute(parameter);
			e.Handled = true;
		}
	}
}
