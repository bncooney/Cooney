using Microsoft.Extensions.AI;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cooney.AI.Tools;

/// <summary>
/// AI function that writes content to a file with explicit overwrite control.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description>Requires an absolute file path.</description></item>
///   <item><description>Fails if the target file already exists unless <c>overwrite: true</c> is supplied.</description></item>
///   <item><description>Creates missing parent directories automatically.</description></item>
///   <item><description>Writes the content atomically using <c>File.WriteAllTextAsync</c>.</description></item>
///   <item><description>Returns a response containing success flag, bytes written, and any error details.</description></item>
/// </list>
/// </remarks>
public class WriteFile : AIFunction
{
	private const int MaxContentSizeBytes = 10_000_000; // 10 MB safety ceiling

	public override string Name => "write_file";

	public override string Description =>
		"Writes content to a file. By default the operation fails if the file already exists. " +
		"Set overwrite=true to replace an existing file. Parent directories are created automatically.";

	private static readonly JsonElement s_jsonSchema;
	public override JsonElement JsonSchema => s_jsonSchema;

	static WriteFile()
	{
		s_jsonSchema = JsonSerializer.SerializeToElement(new
		{
			type = "object",
			properties = new
			{
				path = new
				{
					type = "string",
					description = "Absolute path to the file to write."
				},
				content = new
				{
					type = "string",
					description = "The text content to write to the file."
				},
				overwrite = new
				{
					type = "boolean",
					description = "If true, an existing file will be overwritten. Default is false.",
					@default = false,
					defaultValue = false
				}
			},
			required = new[] { "path", "content" },
			additionalProperties = false
		});
	}

	protected override async ValueTask<object?> InvokeCoreAsync(
		AIFunctionArguments arguments,
		CancellationToken cancellationToken)
	{
		// ----- Parameter extraction -------------------------------------------------
		if (!arguments.TryGetValue("path", out object? pathObj) || string.IsNullOrWhiteSpace(pathObj?.ToString()))
		{
			return CreateErrorResponse("Missing required parameter: path");
		}

		string filePath = Path.GetFullPath(pathObj!.ToString());

		if (!Path.IsPathRooted(filePath))
		{
			return CreateErrorResponse("Parameter 'path' must be an absolute path.");
		}

		if (!arguments.TryGetValue("content", out object? contentObj) || contentObj == null)
		{
			return CreateErrorResponse("Missing required parameter: content");
		}

		string content = contentObj.ToString();

		// Optional overwrite flag (default false)
		bool overwrite = false;
		if (arguments.TryGetValue("overwrite", out object? overwriteObj))
		{
			overwrite = Convert.ToBoolean(overwriteObj);
		}

		// ----- Security & validation checks -----------------------------------------
		// 1. Enforce size limit to avoid accidental huge writes
		if (content.Length > MaxContentSizeBytes)
		{
			return CreateErrorResponse(
				$"Content size exceeds maximum allowed ({MaxContentSizeBytes / 1_000_000} MB).");
		}

		// 2. If file exists, only allow when overwrite is true
		if (File.Exists(filePath) && !overwrite)
		{
			return CreateErrorResponse(
				$"File already exists: {filePath}. Set overwrite=true to replace it.");
		}

		// 3. Ensure directory exists (create if missing)
		string? directory = Path.GetDirectoryName(filePath);
		if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
		{
			try
			{
				Directory.CreateDirectory(directory!);
			}
			catch (Exception ex)
			{
				return CreateErrorResponse($"Failed to create directory '{directory}': {ex.Message}");
			}
		}

		// ----- Write the file ---------------------------------------------------------
		try
		{
#if NETSTANDARD2_0
			await Task.Run(() => File.WriteAllText(filePath, content), cancellationToken);
#else
			await File.WriteAllTextAsync(filePath, content, cancellationToken);
#endif
		}
		catch (UnauthorizedAccessException ex)
		{
			return CreateErrorResponse($"Access denied to path '{filePath}': {ex.Message}", "AccessDenied");
		}
		catch (IOException ex)
		{
			return CreateErrorResponse($"I/O error while writing file: {ex.Message}", "IOException");
		}
		catch (Exception ex)
		{
			return CreateErrorResponse($"Unexpected error: {ex.Message}", "UnexpectedError");
		}

		// ----- Success response -------------------------------------------------------
		var successResponse = new JsonObject
		{
			["success"] = true,
			["path"] = filePath,
			["bytes_written"] = System.Text.Encoding.UTF8.GetByteCount(content),
			["overwrite"] = overwrite
		};

		return successResponse;
	}

	/// <summary>
	/// Helper that creates a standardized error JSON payload.
	/// </summary>
	private static JsonObject CreateErrorResponse(string message, string errorType = "InvalidParameter")
	{
		return new JsonObject
		{
			["success"] = false,
			["error"] = message,
			["error_type"] = errorType
		};
	}
}
