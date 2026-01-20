using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Cooney.AI.WindowsDesktop.Behaviors;

public static class RichTextBoxDocumentBehavior
{
	public static readonly DependencyProperty DocumentProperty =
		DependencyProperty.RegisterAttached(
			"Document",
			typeof(FlowDocument),
			typeof(RichTextBoxDocumentBehavior),
			new PropertyMetadata(null, OnDocumentChanged));

	public static FlowDocument GetDocument(DependencyObject obj) =>
		(FlowDocument)obj.GetValue(DocumentProperty);

	public static void SetDocument(DependencyObject obj, FlowDocument value) =>
		obj.SetValue(DocumentProperty, value);

	private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is RichTextBox richTextBox && e.NewValue is FlowDocument flowDocument)
		{
			richTextBox.Document = flowDocument;
		}
	}
}
