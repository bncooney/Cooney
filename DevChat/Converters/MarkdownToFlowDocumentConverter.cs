using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Markdig;
using Markdig.Syntax;
using MarkdigTable = Markdig.Extensions.Tables.Table;
using MarkdigTableRow = Markdig.Extensions.Tables.TableRow;
using MarkdigTableCell = Markdig.Extensions.Tables.TableCell;
using MarkdigTableColumnAlign = Markdig.Extensions.Tables.TableColumnAlign;
using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigContainer = Markdig.Syntax.Inlines.ContainerInline;
using MarkdigLiteral = Markdig.Syntax.Inlines.LiteralInline;
using MarkdigEmphasis = Markdig.Syntax.Inlines.EmphasisInline;
using MarkdigLineBreak = Markdig.Syntax.Inlines.LineBreakInline;
using MarkdigLink = Markdig.Syntax.Inlines.LinkInline;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using MarkdigCodeInline = Markdig.Syntax.Inlines.CodeInline;
using MarkdigAutoLink = Markdig.Syntax.Inlines.AutolinkInline;
using MarkdigHtmlInline = Markdig.Syntax.Inlines.HtmlInline;

namespace DevChat.Converters;

public class MarkdownToFlowDocumentConverter : IValueConverter
{
	private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
		.UseAutoLinks()
		.UsePipeTables()
		.Build();

	// Code styling
	private static readonly FontFamily CodeFontFamily = new("Consolas");

	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		var markdown = value as string ?? string.Empty;
		var document = Markdown.Parse(markdown, Pipeline);

		var flowDocument = new FlowDocument
		{
			FontFamily = new FontFamily("Segoe UI"),
			FontSize = 14,
			PagePadding = new Thickness(0)
		};

		foreach (var block in document)
		{
			var element = ConvertBlock(block);
			if (element != null)
				flowDocument.Blocks.Add(element);
		}

		return flowDocument;
	}

	private static System.Windows.Documents.Block? ConvertBlock(MarkdigBlock block)
	{
		switch (block)
		{
			case ParagraphBlock paragraph:
				var para = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
				foreach (var inline in ProcessInlines(paragraph.Inline))
					para.Inlines.Add(inline);
				return para;

			case HeadingBlock heading:
				var headingPara = new Paragraph
				{
					FontWeight = FontWeights.Bold,
					FontSize = heading.Level switch
					{
						1 => 24,
						2 => 20,
						3 => 18,
						4 => 16,
						5 => 14,
						_ => 12
					},
					Margin = new Thickness(0, heading.Level == 1 ? 12 : 8, 0, 8)
				};
				foreach (var inline in ProcessInlines(heading.Inline))
					headingPara.Inlines.Add(inline);
				return headingPara;

			case ListBlock list:
				var wpfList = new List
				{
					Margin = new Thickness(0, 0, 0, 8),
					MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc
				};
				foreach (var item in list)
				{
					if (item is ListItemBlock listItem)
					{
						var listItemPara = new Paragraph { Margin = new Thickness(0) };
						foreach (var subBlock in listItem)
						{
							if (subBlock is ParagraphBlock listPara)
							{
								foreach (var inline in ProcessInlines(listPara.Inline))
									listItemPara.Inlines.Add(inline);
							}
						}
						wpfList.ListItems.Add(new ListItem(listItemPara));
					}
				}
				return wpfList;

			case FencedCodeBlock fencedCode:
				var codeText = GetCodeBlockText(fencedCode);
				return CreateCodeBlock(codeText);

			case CodeBlock codeBlock:
				var code = GetCodeBlockText(codeBlock);
				return CreateCodeBlock(code);

			case QuoteBlock quote:
				var section = new Section
				{
					Margin = new Thickness(20, 0, 0, 8),
					Padding = new Thickness(10, 5, 10, 5),
					BorderBrush = ThemeColors.BorderPrimary,
					BorderThickness = new Thickness(2, 0, 0, 0),
					Foreground = ThemeColors.TextSecondary,
					FontStyle = FontStyles.Italic
				};
				foreach (var subBlock in quote)
				{
					var element = ConvertBlock(subBlock);
					if (element != null)
						section.Blocks.Add(element);
				}
				return section;

			case ThematicBreakBlock:
				var hrPara = new Paragraph
				{
					Margin = new Thickness(0, 8, 0, 8),
					BorderBrush = ThemeColors.BorderPrimary,
					BorderThickness = new Thickness(0, 0, 0, 1),
					Padding = new Thickness(0, 0, 0, 4)
				};
				return hrPara;

			case MarkdigTable table:
				return ConvertTable(table);

			default:
				// Try to extract any inlines from unknown block types
				if (block is LeafBlock leaf && leaf.Inline != null)
				{
					var fallbackPara = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
					foreach (var inline in ProcessInlines(leaf.Inline))
						fallbackPara.Inlines.Add(inline);
					return fallbackPara;
				}
				break;
		}
		return null;
	}

	private static Table ConvertTable(MarkdigTable table)
	{
		var wpfTable = new Table
		{
			CellSpacing = 0,
			BorderBrush = ThemeColors.BorderPrimary,
			BorderThickness = new Thickness(1),
			Margin = new Thickness(0, 0, 0, 8)
		};

		// Determine column count
		var headerRow = table.OfType<MarkdigTableRow>().FirstOrDefault(r => r.IsHeader);
		var columnCount = headerRow?.Count ?? table.ColumnDefinitions.Count;

		// Add columns
		for (int i = 0; i < columnCount; i++)
		{
			wpfTable.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
		}

		// Create row groups
		var headerRowGroup = new TableRowGroup();
		var bodyRowGroup = new TableRowGroup();

		foreach (var rowBlock in table)
		{
			if (rowBlock is MarkdigTableRow row)
			{
				var wpfRow = new TableRow();

				// Process cells
				for (int colIndex = 0; colIndex < columnCount; colIndex++)
				{
					var cell = new TableCell
					{
						BorderBrush = ThemeColors.BorderPrimary,
						BorderThickness = new Thickness(1),
						Padding = new Thickness(6, 4, 6, 4)
					};

					if (row.IsHeader)
					{
						cell.FontWeight = FontWeights.Bold;
					}

					var cellPara = new Paragraph { Margin = new Thickness(0) };

					if (colIndex < row.Count && row[colIndex] is MarkdigTableCell markdigCell)
					{
						foreach (var cellBlock in markdigCell)
						{
							if (cellBlock is ParagraphBlock para && para.Inline != null)
							{
								foreach (var inline in ProcessInlines(para.Inline))
									cellPara.Inlines.Add(inline);
							}
						}
					}

					// Apply alignment
					if (colIndex < table.ColumnDefinitions.Count)
					{
						var alignment = table.ColumnDefinitions[colIndex].Alignment;
						cellPara.TextAlignment = alignment switch
						{
							MarkdigTableColumnAlign.Center => TextAlignment.Center,
							MarkdigTableColumnAlign.Right => TextAlignment.Right,
							_ => TextAlignment.Left
						};
					}

					cell.Blocks.Add(cellPara);
					wpfRow.Cells.Add(cell);
				}

				if (row.IsHeader)
					headerRowGroup.Rows.Add(wpfRow);
				else
					bodyRowGroup.Rows.Add(wpfRow);
			}
		}

		if (headerRowGroup.Rows.Count > 0)
			wpfTable.RowGroups.Add(headerRowGroup);
		if (bodyRowGroup.Rows.Count > 0)
			wpfTable.RowGroups.Add(bodyRowGroup);

		return wpfTable;
	}

	private static Paragraph CreateCodeBlock(string text)
	{
		var para = new Paragraph
		{
			Margin = new Thickness(0, 0, 0, 8),
			Padding = new Thickness(10),
			Background = ThemeColors.CodeBackground,
			FontFamily = CodeFontFamily,
			Foreground = ThemeColors.CodeForeground
		};
		para.Inlines.Add(new Run(text));
		return para;
	}

	private static string GetCodeBlockText(LeafBlock codeBlock)
	{
		if (codeBlock.Lines.Count == 0)
			return string.Empty;

		var lines = new List<string>();
		for (int i = 0; i < codeBlock.Lines.Count; i++)
		{
			lines.Add(codeBlock.Lines.Lines[i].Slice.ToString());
		}
		return string.Join(Environment.NewLine, lines);
	}

	private static IEnumerable<Inline> ConvertInline(MarkdigInline? inline)
	{
		if (inline == null)
			yield break;

		switch (inline)
		{
			case MarkdigLiteral lit:
				yield return new Run(lit.Content.ToString());
				break;

			case MarkdigEmphasis emp:
				Span emphasisWrapper;
				// Handle bold+italic (***text*** or ___text___)
				if (emp.DelimiterCount >= 3)
				{
					emphasisWrapper = new Bold();
					var italic = new Italic();
					foreach (var child in emp)
					{
						foreach (var converted in ConvertInline(child))
							italic.Inlines.Add(converted);
					}
					emphasisWrapper.Inlines.Add(italic);
				}
				else if (emp.DelimiterCount == 2)
				{
					emphasisWrapper = new Bold();
					foreach (var child in emp)
					{
						foreach (var converted in ConvertInline(child))
							emphasisWrapper.Inlines.Add(converted);
					}
				}
				else
				{
					emphasisWrapper = new Italic();
					foreach (var child in emp)
					{
						foreach (var converted in ConvertInline(child))
							emphasisWrapper.Inlines.Add(converted);
					}
				}
				yield return emphasisWrapper;
				break;

			case MarkdigCodeInline code:
				yield return new Run(code.Content)
				{
					FontFamily = CodeFontFamily,
					Background = ThemeColors.CodeBackground,
					Foreground = ThemeColors.CodeForeground
				};
				break;

			case MarkdigLink link:
				// Check if it's an image
				if (link.IsImage)
				{
					// For images, just show alt text or URL as placeholder
					var altText = GetLinkText(link);
					yield return new Run($"[Image: {altText}]") { Foreground = ThemeColors.TextSecondary };
				}
				else
				{
					var linkText = GetLinkText(link);
					var hyperlink = new Hyperlink { NavigateUri = CreateSafeUri(link.Url) };
					hyperlink.Inlines.Add(new Run(linkText));
					hyperlink.RequestNavigate += (s, e) =>
					{
						System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
					};
					yield return hyperlink;
				}
				break;

			case MarkdigAutoLink autoLink:
				var autoHyperlink = new Hyperlink { NavigateUri = CreateSafeUri(autoLink.Url) };
				autoHyperlink.Inlines.Add(new Run(autoLink.Url));
				autoHyperlink.RequestNavigate += (s, e) =>
				{
					System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
				};
				yield return autoHyperlink;
				break;

			case MarkdigLineBreak lb:
				yield return lb.IsHard ? new LineBreak() : new Run(" ");
				break;

			case MarkdigHtmlInline html:
				// Strip HTML tags for display, show raw if it's meaningful
				var htmlContent = html.Tag;
				if (!string.IsNullOrWhiteSpace(htmlContent) && !htmlContent.StartsWith('<'))
				{
					yield return new Run(htmlContent);
				}
				break;

			case MarkdigContainer container:
				// Generic container - process children
				foreach (var child in container)
				{
					foreach (var result in ConvertInline(child))
						yield return result;
				}
				break;

			default:
				// Fallback: treat as plain text
				var text = inline.ToString();
				if (!string.IsNullOrEmpty(text))
					yield return new Run(text);
				break;
		}
	}

	private static string GetLinkText(MarkdigLink link)
	{
		// Extract text from link children
		var textParts = new List<string>();
		foreach (var child in link)
		{
			if (child is MarkdigLiteral lit)
				textParts.Add(lit.Content.ToString());
			else
				textParts.Add(child.ToString() ?? string.Empty);
		}
		var text = string.Join("", textParts);
		return !string.IsNullOrEmpty(text) ? text : (link.Title ?? link.Url ?? "link");
	}

	private static Uri CreateSafeUri(string? url)
	{
		if (string.IsNullOrWhiteSpace(url))
			return new Uri("about:blank");

		if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
			return uri;

		// Try prepending https://
		if (Uri.TryCreate("https://" + url, UriKind.Absolute, out uri))
			return uri;

		return new Uri("about:blank");
	}

	private static IEnumerable<Inline> ProcessInlines(MarkdigContainer? container)
	{
		if (container == null)
			yield break;

		foreach (var inline in container)
		{
			foreach (var result in ConvertInline(inline))
				yield return result;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		=> throw new NotImplementedException();
}
