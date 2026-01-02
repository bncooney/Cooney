using Microsoft.Extensions.AI;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cooney.AI.Tools
{
	/// <summary>
	/// AI function that reads file content with support for large files through chunking.
	/// </summary>
	/// <remarks>
	/// Use read_file to read the content of a file. It's designed to handle large files safely.
	/// By default, it reads from the beginning of the file.
	/// Use offset (line number) and limit (number of lines) to read specific parts or chunks of a file.
	///
	/// Strategy for large files:
	/// 1. Call read_file with a limit (e.g., 1000 lines) to get the start of the file.
	/// 2. If was_truncated is true, you know the file is large.
	/// 3. To read the next chunk, call read_file again with an offset. For example, offset=1000, limit=1000.
	/// </remarks>
	public class ReadFile : AIFunction
	{
		private const int MaxLineLimit = 10000;

		public override string Name => "read_file";

		public override string Description =>
			"Use read_file to read the content of a file. It's designed to handle large files safely. " +
			"By default, it reads from the beginning of the file. " +
			"Use offset (line number) and limit (number of lines) to read specific parts or chunks of a file. " +
			"This is efficient for exploring large files.";

		private static readonly JsonElement s_jsonSchema;
		public override JsonElement JsonSchema => s_jsonSchema;

		static ReadFile()
		{
			s_jsonSchema = JsonSerializer.SerializeToElement(new
			{
				type = "object",
				properties = new
				{
					file_path = new
					{
						type = "string",
						description = "The absolute path to the file to read"
					},
					offset = new
					{
						type = "integer",
						description = "The line number to start reading from (0-based). Default is 0.",
						@default = 0,
						minimum = 0
					},
					limit = new
					{
						type = "integer",
						description = $"The maximum number of lines to read. If not specified, reads entire file. Maximum allowed: {MaxLineLimit}",
						minimum = 1,
						maximum = MaxLineLimit
					}
				},
				required = new[] { "file_path" },
				additionalProperties = false
			});
		}

		protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
		{
			// Extract parameters
			if (!arguments.TryGetValue("file_path", out object? filePathObj))
			{
				return CreateErrorResponse("Missing required parameter: file_path");
			}

			string? filePath = filePathObj?.ToString();
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return CreateErrorResponse("Parameter 'file_path' cannot be empty");
			}

			// Extract optional parameters
			int offset = 0;
			if (arguments.TryGetValue("offset", out object? offsetObj))
			{
				if (offsetObj is int offsetInt)
					offset = offsetInt;
				else if (offsetObj is long offsetLong)
					offset = (int)offsetLong;
				else if (int.TryParse(offsetObj?.ToString(), out int parsedOffset))
					offset = parsedOffset;
			}

			int? limit = null;
			if (arguments.TryGetValue("limit", out object? limitObj))
			{
				if (limitObj is int limitInt)
					limit = limitInt;
				else if (limitObj is long limitLong)
					limit = (int)limitLong;
				else if (int.TryParse(limitObj?.ToString(), out int parsedLimit))
					limit = parsedLimit;
			}

			// Validate parameters
			if (offset < 0)
			{
				return CreateErrorResponse($"Parameter 'offset' must be >= 0, got: {offset}");
			}

			if (limit.HasValue && limit.Value < 1)
			{
				return CreateErrorResponse($"Parameter 'limit' must be >= 1, got: {limit.Value}");
			}

			if (limit.HasValue && limit.Value > MaxLineLimit)
			{
				return CreateErrorResponse($"Parameter 'limit' exceeds maximum allowed value of {MaxLineLimit}, got: {limit.Value}");
			}

			// Validate file path security
			if (!Path.IsPathRooted(filePath))
			{
				return CreateErrorResponse($"File path must be absolute, got relative path: {filePath}");
			}

			// Check if file exists
			if (!File.Exists(filePath))
			{
				return CreateErrorResponse($"File not found: {filePath}", "FileNotFound");
			}

			try
			{
				// Read file content
				string[] allLines;
				try
				{
#if NETSTANDARD2_0
					// netstandard2.0 doesn't have File.ReadAllLinesAsync
					allLines = await Task.Run(() => File.ReadAllLines(filePath), cancellationToken);
#else
					allLines = await File.ReadAllLinesAsync(filePath, cancellationToken);
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

				int totalLines = allLines.Length;

				// Apply offset and limit
				var linesToRead = allLines.Skip(offset);
				bool wasTruncated = false;

				if (limit.HasValue)
				{
					int availableLines = Math.Max(0, totalLines - offset);
					wasTruncated = availableLines > limit.Value;
					linesToRead = linesToRead.Take(limit.Value);
				}

				var readLines = linesToRead.ToArray();
				string content = string.Join(Environment.NewLine, readLines);

				// Build response
				var response = new JsonObject
				{
					["success"] = true,
					["content"] = content,
					["lines_read"] = readLines.Length,
					["total_lines"] = totalLines,
					["was_truncated"] = wasTruncated,
					["offset"] = offset
				};

				if (limit.HasValue)
				{
					response["limit"] = limit.Value;
				}

				return response;
			}
			catch (Exception ex)
			{
				return CreateErrorResponse($"Unexpected error reading file: {ex.Message}", "UnexpectedError");
			}
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
}
