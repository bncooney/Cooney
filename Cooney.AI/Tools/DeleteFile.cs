using Microsoft.Extensions.AI;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Cooney.AI.Tools
{
	/// <summary>
	/// AI function that deletes a file safely, with optional dry‑run simulation.
	/// </summary>
	public class DeleteFile : AIFunction
	{
		private const int MaxLineLimit = 10000; // kept for consistency with other functions

		public override string Name => "delete_file";

		public override string Description =>
			"Use delete_file to delete a file. " +
			"Set dry_run=true to simulate deletion without actually removing the file.";

		private static readonly JsonElement s_jsonSchema;
		public override JsonElement JsonSchema => s_jsonSchema;

		static DeleteFile()
		{
			s_jsonSchema = JsonSerializer.SerializeToElement(new
			{
				type = "object",
				properties = new
				{
					file_path = new
					{
						type = "string",
						description = "The absolute path to the file to delete"
					},
					dry_run = new
					{
						type = "boolean",
						description = "If true, only simulate deletion; no file is removed.",
						@default = false
					}
				},
				required = new[] { "file_path" },
				additionalProperties = false
			});
		}

		protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
		{
			// Extract required file_path
			if (!arguments.TryGetValue("file_path", out object? filePathObj))
				return CreateErrorResponse("Missing required parameter: file_path", "InvalidParameter");

			string? filePath = filePathObj?.ToString();
			if (string.IsNullOrWhiteSpace(filePath))
				return CreateErrorResponse("Parameter 'file_path' cannot be empty", "InvalidParameter");

			// Extract optional dry_run
			bool dryRun = false;
			if (arguments.TryGetValue("dry_run", out object? dryRunObj))
			{
				if (dryRunObj is bool dryRunBool)
					dryRun = dryRunBool;
				else if (dryRunObj is string dryRunStr && bool.TryParse(dryRunStr, out bool parsed))
					dryRun = parsed;
			}

			// Security & validation checks
			if (!Path.IsPathRooted(filePath))
				return CreateErrorResponse("File path must be absolute", "InvalidParameter");

			if (!File.Exists(filePath))
				return CreateErrorResponse($"File not found: {filePath}", "FileNotFound");

			if (dryRun)
			{
				// Simulation path: return success without deleting
				return new JsonObject
				{
					["success"] = true,
					["file_path"] = filePath,
					["would_delete"] = true,
					["dry_run"] = true,
					["message"] = "File would be deleted (dry run)."
				};
			}

			try
			{
				File.Delete(filePath);
				return new JsonObject
				{
					["success"] = true,
					["file_path"] = filePath,
					["deleted"] = true,
					["dry_run"] = false
				};
			}
			catch (UnauthorizedAccessException)
			{
				return CreateErrorResponse($"Access denied to file: {filePath}", "AccessDenied");
			}
			catch (IOException ex)
			{
				return CreateErrorResponse($"I/O error deleting file: {ex.Message}", "IOException");
			}
			catch (Exception ex)
			{
				return CreateErrorResponse($"Unexpected error deleting file: {ex.Message}", "UnexpectedError");
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
