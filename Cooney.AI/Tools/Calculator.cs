using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cooney.AI.Tools;

public class Calculator : AIFunction
{
	public override string Name => "calculator";

	public override string Description =>
		"Use calculator to perform basic arithmetic calculations such as addition, subtraction, multiplication, and division.";

	private static readonly JsonElement s_jsonSchema;
	public override JsonElement JsonSchema => s_jsonSchema;

	static Calculator()
	{
		s_jsonSchema = JsonSerializer.SerializeToElement(new
		{
			type = "object",
			properties = new
			{
				expression = new
				{
					type = "string",
					description = "The arithmetic expression to evaluate, e.g., '2 + 2 * 3'"
				}
			},
			required = new[] { "expression" },
			additionalProperties = false
		});
	}

	protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
	{
		if (!arguments.TryGetValue("expression", out object? expressionObj))
			return CreateErrorResponse("Missing required parameter: expression", "InvalidParameter");

		string? expression = expressionObj?.ToString();
		if (string.IsNullOrWhiteSpace(expression))
			return CreateErrorResponse("Parameter 'expression' cannot be empty", "InvalidParameter");

		// expression is guaranteed non-null after the null check above
		return EvaluateExpressionJsonObject(expression!);
	}

	private static JsonObject EvaluateExpressionJsonObject(string expression)
	{
		var result = EvaluateExpression(expression, out var error);

		if (error.HasValue)
			return CreateErrorResponse(error.Value.error, error.Value.errorType);

		return new JsonObject
		{
			["success"] = true,
			["result"] = new JsonObject { ["value"] = result }
		};
	}

	// TODO: The idea of using Recognizers here is to support numbers in forms like "3/4" (i.e. fractions),
	// "one hundred twenty three", "2.5 million", etc. which the LLM may pass in verbatim.
	// We would still need to disambiguate operators from numbers with spaces (e.g. "2 + 3 / 2" and "2 + 3/2"),
	// but interpreting the order of operation will be up to the LLM, once we decide whether to use RPN or not.
	// Different models display different tendencies to burn tokens "reasoning", which we're trying to avoid.
	// We could also use Recognizers to parse the entire expression at once, but that has its own challenges.
	public static decimal? EvaluateExpression(string expression, out (string error, string errorType)? error)
	{
		// Assert expression is non-null after the null check above
		Guard.IsNotNullOrWhiteSpace(expression);
		error = null;

		// Split expression into tokens (using char array for .NET Standard 2.0 compatibility)
		var tokens = expression.Split([' '], StringSplitOptions.RemoveEmptyEntries);
		// '+','-','*','×','/','÷'

		// Must have odd number of tokens: number operator number [operator number ...]
		if (tokens.Length < 3 || tokens.Length % 2 == 0)
		{
			error = ("Expression must have format: number operator number [operator number ...]", "InvalidFormat");
			return null;
		}
		// Parse the first number
		var firstResults = NumberRecognizer.RecognizeNumber(tokens[0], Culture.English);
		if (firstResults.Count == 0)
		{
			error = ($"Could not parse number: {tokens[0]}", "InvalidNumber");
			return null;
		}

		decimal accumulator = Convert.ToDecimal(firstResults.First().Resolution["value"]);

		// Process each operator and number pair
		for (int i = 1; i < tokens.Length; i += 2)
		{
			string operatorToken = tokens[i];
			string numberToken = tokens[i + 1];

			// Parse the next number
			var numberResults = NumberRecognizer.RecognizeNumber(numberToken, Culture.English);
			if (numberResults.Count == 0)
			{
				error = ($"Could not parse number: {numberToken}", "InvalidNumber");
				return null;
			}

			decimal operand = Convert.ToDecimal(numberResults.First().Resolution["value"]);

			// Apply the operator
			switch (operatorToken)
			{
				case "+":
					accumulator += operand;
					break;
				case "-":
					accumulator -= operand;
					break;
				case "*":
				case "×":
					accumulator *= operand;
					break;
				case "/":
				case "÷":
					if (operand == 0)
					{
						error = ("Cannot divide by zero", "DivisionByZero");
						return null;
					}
					accumulator /= operand;
					break;
				default:
					error = ($"Operator '{operatorToken}' is not supported", "UnsupportedOperator");
					return null;
			}
		}

		return accumulator;
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
