using Cooney.AI.SourceGenerators.Models;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cooney.AI.SourceGenerators;

/// <summary>
/// Incremental source generator that creates strongly-typed workflow classes from JSON files.
/// </summary>
[Generator]
public sealed class WorkflowSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		// Filter for JSON files in the Workflows folder
		var workflowFiles = context.AdditionalTextsProvider
			.Where(file => file.Path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
			.Where(file => file.Path.Contains("Workflows", StringComparison.OrdinalIgnoreCase));

		// Parse each workflow file
		var workflowModels = workflowFiles
			.Select((file, cancellationToken) => WorkflowParser.Parse(file))
			.Where(model => model is not null)
			.Select((model, cancellationToken) => model!);

		// Collect all workflows to detect duplicates
		var allWorkflows = workflowModels.Collect();

		// Generate source code for each workflow
		context.RegisterSourceOutput(allWorkflows, (context, workflows) =>
		{
			var workflowsByName = new Dictionary<string, WorkflowModel>(StringComparer.OrdinalIgnoreCase);

			// Check for duplicates and emit diagnostics
			foreach (var workflow in workflows)
			{
				if (workflowsByName.TryGetValue(workflow.Name, out var existing))
				{
					var diagnostic = Diagnostic.Create(
						DiagnosticDescriptors.DuplicateWorkflowName,
						Location.None,
						workflow.Name,
						existing.FilePath);
					context.ReportDiagnostic(diagnostic);
					continue;
				}

				workflowsByName[workflow.Name] = workflow;
			}

			// Generate code for each unique workflow
			foreach (var workflow in workflowsByName.Values)
			{
				try
				{
					var sourceCode = WorkflowCodeGenerator.Generate(workflow);
					var className = GetClassName(workflow.Name);
					context.AddSource($"{className}.g.cs", sourceCode);

					// Emit info diagnostic for successful generation
					var infoDiagnostic = Diagnostic.Create(
						DiagnosticDescriptors.WorkflowGenerated,
						Location.None,
						className,
						workflow.Parameters.Count,
						Path.GetFileName(workflow.FilePath));
					context.ReportDiagnostic(infoDiagnostic);
				}
				catch (Exception)
				{
					// If code generation fails, report as error
					var diagnostic = Diagnostic.Create(
						DiagnosticDescriptors.InvalidJsonFormat,
						Location.None,
						Path.GetFileName(workflow.FilePath));
					context.ReportDiagnostic(diagnostic);
				}
			}
		});

		// Register source output for invalid JSON files
		var invalidFiles = workflowFiles
			.Select((file, cancellationToken) => (file, model: WorkflowParser.Parse(file)))
			.Where(tuple => tuple.model is null)
			.Select((tuple, cancellationToken) => tuple.file);

		context.RegisterSourceOutput(invalidFiles, (context, file) =>
		{
			var diagnostic = Diagnostic.Create(
				DiagnosticDescriptors.InvalidJsonFormat,
				Location.None,
				Path.GetFileName(file.Path));
			context.ReportDiagnostic(diagnostic);
		});
	}

	private static string GetClassName(string workflowName)
	{
		var pascalCase = ToPascalCase(workflowName);
		return $"{pascalCase}Workflow";
	}

	private static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input))
			return input;

		var words = SplitIntoWords(input);
		var result = string.Concat(words.Select(w =>
		{
			if (string.IsNullOrEmpty(w))
				return w;
			return char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant();
		}));

		return result;
	}

	private static IEnumerable<string> SplitIntoWords(string input)
	{
		var words = new List<string>();
		var currentWord = new System.Text.StringBuilder();

		for (int i = 0; i < input.Length; i++)
		{
			var c = input[i];

			if (char.IsLetterOrDigit(c))
			{
				if (currentWord.Length > 0 && char.IsUpper(c) && i > 0 && char.IsLower(input[i - 1]))
				{
					words.Add(currentWord.ToString());
					currentWord.Clear();
				}

				currentWord.Append(c);
			}
			else
			{
				if (currentWord.Length > 0)
				{
					words.Add(currentWord.ToString());
					currentWord.Clear();
				}
			}
		}

		if (currentWord.Length > 0)
		{
			words.Add(currentWord.ToString());
		}

		return words;
	}
}
