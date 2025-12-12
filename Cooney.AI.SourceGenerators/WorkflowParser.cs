using System.Text.Json;
using System.Text.RegularExpressions;
using Cooney.AI.SourceGenerators.Models;
using Microsoft.CodeAnalysis;

namespace Cooney.AI.SourceGenerators;

/// <summary>
/// Parses workflow JSON files to extract parameters and metadata.
/// </summary>
internal static class WorkflowParser
{
	private static readonly Regex MustacheParameterRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

	/// <summary>
	/// Parses a workflow JSON file from an AdditionalText source.
	/// </summary>
	/// <param name="file">The additional text file containing the workflow JSON.</param>
	/// <returns>A WorkflowModel if parsing succeeds, or null if the file is invalid.</returns>
	public static WorkflowModel? Parse(AdditionalText file)
	{
		var fileName = Path.GetFileNameWithoutExtension(file.Path);
		var jsonContent = file.GetText()?.ToString();

		if (string.IsNullOrWhiteSpace(jsonContent))
		{
			return null;
		}

		// Validate JSON structure
#pragma warning disable CS8604 // Possible null reference argument.
		if (!IsValidJson(jsonContent))
		{
			//return null;
		}
#pragma warning restore CS8604 // Possible null reference argument.

		// Extract parameters using mustache pattern
		var parameters = ExtractParameters(jsonContent);

		return new WorkflowModel(
			name: fileName,
			jsonTemplate: jsonContent,
			parameters: parameters,
			filePath: file.Path);
	}

	/// <summary>
	/// Validates that the content is valid JSON.
	/// </summary>
	private static bool IsValidJson(string jsonContent)
	{
		try
		{
			using var document = JsonDocument.Parse(jsonContent);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}

	/// <summary>
	/// Extracts unique parameter names from mustache syntax {{paramName}}.
	/// </summary>
	private static IReadOnlyList<string> ExtractParameters(string jsonContent)
	{
		var matches = MustacheParameterRegex.Matches(jsonContent);
		var parameters = new HashSet<string>(StringComparer.Ordinal);

		foreach (Match match in matches)
		{
			if (match.Groups.Count > 1)
			{
				parameters.Add(match.Groups[1].Value);
			}
		}

		return [.. parameters.OrderBy(p => p, StringComparer.Ordinal)];
	}
}
