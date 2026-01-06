using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.AI;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cooney.AI.Tools;

public class WordCount : AIFunction
{
	public override string Name => "word_count";
	public override string Description => "Counts the number of words, sentences, and paragraphs in a given text.";

	private static readonly JsonElement s_jsonSchema;
	public override JsonElement JsonSchema => s_jsonSchema;

	static WordCount()
	{
		s_jsonSchema = JsonSerializer.SerializeToElement(new
		{
			type = "object",
			properties = new
			{
				text = new
				{
					type = "string",
					description = "The text to analyze for word, sentence, and paragraph counts."
				}
			},
			required = new[] { "text" },
			additionalProperties = false
		});
	}

	protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
	{
		if (!arguments.TryGetValue("text", out object? textObj))
			return new ValueTask<object?>(CreateErrorResponse("Missing required parameter: text", "InvalidParameter"));
		string? text = textObj?.ToString();
		if (string.IsNullOrWhiteSpace(text))
			return new ValueTask<object?>(CreateErrorResponse("Parameter 'text' cannot be empty", "InvalidParameter"));
		// text is guaranteed non-null after the null check above
		return new ValueTask<object?>(CountWordsSentencesParagraphsJsonObject(text!));
	}

	private static JsonObject CountWordsSentencesParagraphsJsonObject(string text)
	{
		var (wordCount, sentenceCount, paragraphCount) = CountWordsSentencesParagraphs(text);

		return new JsonObject
		{
			["success"] = true,
			["word_count"] = wordCount,
			["sentence_count"] = sentenceCount,
			["paragraph_count"] = paragraphCount
		};
	}

	public static (int, int, int) CountWordsSentencesParagraphs(string text)
	{
		Guard.IsNotNullOrWhiteSpace(text);

		ReadOnlySpan<char> span = text.AsSpan();
		int wordCount = 0;
		int sentenceCount = 0;
		int paragraphCount = 0;

		bool inWord = false;
		bool separatorDetected = false; // used to avoid double-counting paragraph breaks

		for (int i = 0; i < span.Length; i++)
		{
			char c = span[i];

			// ---- Word counting ----
			if (char.IsWhiteSpace(c))
			{
				inWord = false;
			}
			else
			{
				if (!inWord)
				{
					wordCount++;
					inWord = true;
				}
			}

			// ---- Sentence counting (simple heuristic) ----
			if (c == '.' || c == '!' || c == '?')
			{
				// Count a sentence end if the punctuation is followed by whitespace or end of text
				if (i + 1 == span.Length || char.IsWhiteSpace(span[i + 1]))
				{
					sentenceCount++;
				}
			}

			// ---- Paragraph counting ----
			// A paragraph break is defined as two consecutive newline characters
			// (either "\n\n" or "\r\n\r\n" or "\r\r").
			// We increment when we see the second newline in such a sequence.
			if (c == '\n' || c == '\r')
			{
				// If the next character is also a newline, we have a paragraph break
				if (i + 1 < span.Length && (span[i + 1] == '\n' || span[i + 1] == '\r'))
				{
					// Only count the break once per sequence
					if (!separatorDetected)
					{
						paragraphCount++;
						separatorDetected = true;
					}
				}
				else
				{
					// Single newline resets the detection flag
					separatorDetected = false;
				}
			}
			else
			{
				separatorDetected = false;
			}
		}

		// If the text ends without a trailing paragraph break, ensure we count at least one paragraph
		// (the original logic returns 0 only when text is empty, which is already guarded)
		// No extra adjustment needed because we count separators; the Json result expects the count of paragraphs.
		return (wordCount, sentenceCount, paragraphCount + 1);
	}

	private static JsonObject CreateErrorResponse(string errorMessage, string errorType = "InvalidParameter")
	{
		return new JsonObject
		{
			["success"] = false,
			["error"] = errorMessage,
			["error_type"] = errorType
		};
	}
}
