using Microsoft.Extensions.AI;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Cooney.AI.Tools;

/// <summary>
/// AI function that performs search‑and‑replace operations on a file.
/// </summary>
/// <remarks>
/// The <c>content</c> argument must contain one or more SEARCH/REPLACE blocks
/// formatted exactly as the <c>search_replace</c> tool specification describes.
/// Each block is applied in order, replacing the **first** occurrence of the
/// search text with the replacement text. The function validates that each
/// search text appears exactly once in the current file content before
/// performing the replacement.
/// </remarks>
public class SearchReplace : AIFunction
{
	public override string Name => "search_replace";

	public override string Description =>
		"Performs search‑and‑replace on a file using SEARCH/REPLACE blocks. " +
		"Each block must be formatted with <<<<<<< SEARCH, ======, >>>>>>> REPLACE " +
		"and the search text must occur exactly once in the file at the time of replacement.";

	private static readonly JsonElement s_jsonSchema;
	public override JsonElement JsonSchema => s_jsonSchema;

	static SearchReplace()
	{
		s_jsonSchema = JsonSerializer.SerializeToElement(new
		{
			type = "object",
			properties = new
			{
				file_path = new
				{
					type = "string",
					description = "Absolute path to the file to modify."
				},
				content = new
				{
					type = "string",
					description = "SEARCH/REPLACE blocks defining the changes."
				}
			},
			required = new[] { "file_path", "content" },
			additionalProperties = false
		});
	}

	protected override async ValueTask<object?> InvokeCoreAsync(
		AIFunctionArguments arguments,
		CancellationToken cancellationToken)
	{
		// -----------------------------------------------------------------
		// 1. Validate required parameters
		// -----------------------------------------------------------------
		if (!arguments.TryGetValue("file_path", out object? filePathObj) ||
			string.IsNullOrWhiteSpace(filePathObj?.ToString()))
		{
			return CreateErrorResponse("Missing required parameter: file_path");
		}

		string filePath = filePathObj!.ToString()!;
		if (!Path.IsPathRooted(filePath))
		{
			return CreateErrorResponse("Parameter 'file_path' must be an absolute path.");
		}

		if (!File.Exists(filePath))
		{
			return CreateErrorResponse($"File not found: {filePath}", "FileNotFound");
		}

		if (!arguments.TryGetValue("content", out object? contentObj) ||
			string.IsNullOrWhiteSpace(contentObj?.ToString()))
		{
			return CreateErrorResponse("Missing required parameter: content");
		}

		string content = contentObj!.ToString();

		// -----------------------------------------------------------------
		// 2. Read the file content
		// -----------------------------------------------------------------
		string fileContent;
		try
		{
#if NETSTANDARD2_0
			fileContent = await Task.Run(() => File.ReadAllText(filePath), cancellationToken);
#else
			await File.ReadAllTextAsync(filePath, cancellationToken);
#endif
		}
		catch (UnauthorizedAccessException)
		{
			return CreateErrorResponse($"Access denied to file: {filePath}", "AccessDenied");
		}
		catch (IOException ex)
		{
			return CreateErrorResponse($"I/O error reading file: {ex.Message}", "IOException");
		}

		// -----------------------------------------------------------------
		// 3. Parse SEARCH/REPLACE blocks
		// -----------------------------------------------------------------
		// Each block looks like:
		// <<<<<<< SEARCH
		// <search text>
		// ======
		// <replace text>
		// >>>>>>> REPLACE
		var pattern = new Regex(
			@"<<<<<<< SEARCH\r?\n(.*?)\r?\n=====+\r?\n(.*?)\r?\n>>>>>>> REPLACE",
			RegexOptions.Singleline | RegexOptions.Compiled);


		var matches = pattern.Matches(content);
		if (matches.Count == 0)
		{
			return CreateErrorResponse(
				"No valid SEARCH/REPLACE blocks found in the 'content' argument.");
		}

		// -----------------------------------------------------------------
		// 4. Apply each block in order
		// -----------------------------------------------------------------
		for (int i = 0; i < matches.Count; i++)
		{
			var match = matches[i];
			string searchText = match.Groups[1].Value; // raw search text (preserves newlines)
			string replaceText = match.Groups[2].Value;

			// Count exact occurrences (including line breaks) in the current file content
			int exactCount = CountExactOccurrences(fileContent, searchText);
			if (exactCount != 1)
			{
				return CreateErrorResponse(
					$"Search text from block #{i + 1} occurs {exactCount} time(s) in the current file content. " +
					"It must appear exactly once before replacement.",
					"InvalidSearchOccurrences");
			}

			// Perform the replacement (only the first occurrence, though we know it's unique)
			fileContent = fileContent.Replace(searchText, replaceText);
		}

		// -----------------------------------------------------------------
		// 5. Write the modified content back to the file
		// -----------------------------------------------------------------
		try
		{
#if NETSTANDARD2_0
			await Task.Run(() => File.WriteAllText(filePath, fileContent), cancellationToken);
#else
			await File.WriteAllTextAsync(filePath, fileContent, cancellationToken);
#endif
		}
		catch (UnauthorizedAccessException)
		{
			return CreateErrorResponse($"Access denied when writing file: {filePath}", "AccessDenied");
		}
		catch (IOException ex)
		{
			return CreateErrorResponse($"I/O error writing file: {ex.Message}", "IOException");
		}

		// -----------------------------------------------------------------
		// 6. Build success response
		// -----------------------------------------------------------------
		var response = new JsonObject
		{
			["success"] = true,
			["file_path"] = filePath,
			["blocks_processed"] = matches.Count,
			["file_size_after"] = fileContent.Length
		};

		return response;
	}

	/// <summary>
	/// Counts non‑overlapping exact occurrences of <paramref name="value"/> in <paramref name="text"/>.
	/// </summary>
	private static int CountExactOccurrences(string text, string value)
	{
		if (string.IsNullOrEmpty(value))
			return 0;
		int count = 0;
		int startIndex = 0;
		while ((startIndex = text.IndexOf(value, startIndex, StringComparison.Ordinal)) != -1)
		{
			count++;
			startIndex += value.Length; // move past this occurrence
		}
		return count;
	}

	/// <summary>
	/// Creates a standard error JSON response.
	/// </summary>
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
