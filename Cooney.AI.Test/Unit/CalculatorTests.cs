using Cooney.AI.Tools;

namespace Cooney.AI.Test.Unit
{
	[TestClass]
	public class CalculatorTests
	{
		[TestMethod]
		public void EvaluateExpression_BasicAddition()
		{
			// Arrange
			string expression = "2 + 3";

			// Act
			var result = Calculator.EvaluateExpression(expression, out var error);

			// Assert
			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(5m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_BasicSubtraction()
		{
			string expression = "10 - 3";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(7m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_BasicMultiplication()
		{
			string expression = "4 * 5";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(20m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_BasicDivision()
		{
			string expression = "20 / 4";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(5m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_MultiplicationWithAlternateSymbol()
		{
			string expression = "3 × 4";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(12m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_DivisionWithAlternateSymbol()
		{
			string expression = "20 ÷ 5";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(4m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_ChainedOperations()
		{
			// Note: This evaluates left to right, so 2 + 3 * 4 = (2 + 3) * 4 = 20
			string expression = "2 + 3 * 4";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(20m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_LongChain()
		{
			// 10 + 5 - 3 * 2 = ((10 + 5) - 3) * 2 = (15 - 3) * 2 = 12 * 2 = 24
			string expression = "10 + 5 - 3 * 2";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(24m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_DecimalNumbers()
		{
			string expression = "2.5 + 3.7";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(6.2m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_NegativeNumbers()
		{
			string expression = "-5 + 10";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(5m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_DivisionByZero()
		{
			string expression = "10 / 0";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(result);
			Assert.IsNotNull(error);
			Assert.AreEqual("Cannot divide by zero", error.Value.error);
			Assert.AreEqual("DivisionByZero", error.Value.errorType);
		}

		[TestMethod]
		public void EvaluateExpression_InvalidFormat_TooFewTokens()
		{
			string expression = "5 +";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(result);
			Assert.IsNotNull(error);
			Assert.AreEqual("Expression must have format: number operator number [operator number ...]", error.Value.error);
			Assert.AreEqual("InvalidFormat", error.Value.errorType);
		}

		[TestMethod]
		public void EvaluateExpression_InvalidFormat_EvenTokens()
		{
			string expression = "5 + 3 *";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(result);
			Assert.IsNotNull(error);
			Assert.AreEqual("Expression must have format: number operator number [operator number ...]", error.Value.error);
			Assert.AreEqual("InvalidFormat", error.Value.errorType);
		}

		[TestMethod]
		public void EvaluateExpression_InvalidNumber_First()
		{
			string expression = "abc + 5";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(result);
			Assert.IsNotNull(error);
			Assert.Contains("Could not parse number", error.Value.error);
			Assert.AreEqual("InvalidNumber", error.Value.errorType);
		}

		[TestMethod]
		public void EvaluateExpression_InvalidNumber_Second()
		{
			string expression = "5 + xyz";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(result);
			Assert.IsNotNull(error);
			Assert.Contains("Could not parse number", error.Value.error);
			Assert.AreEqual("InvalidNumber", error.Value.errorType);
		}

		[TestMethod]
		public void EvaluateExpression_UnsupportedOperator()
		{
			string expression = "5 ^ 2";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(result);
			Assert.IsNotNull(error);
			Assert.AreEqual("Operator '^' is not supported", error.Value.error);
			Assert.AreEqual("UnsupportedOperator", error.Value.errorType);
		}

		[TestMethod]
		public void EvaluateExpression_ExtraSpaces()
		{
			string expression = "  5   +   3  ";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(8m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_LargeNumbers()
		{
			string expression = "1000000 * 2";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(2000000m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_ZeroResult()
		{
			string expression = "5 - 5";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(0m, result.Value);
		}

		[TestMethod]
		public void EvaluateExpression_FractionalDivision()
		{
			string expression = "5 / 2";

			var result = Calculator.EvaluateExpression(expression, out var error);

			Assert.IsNull(error);
			Assert.IsNotNull(result);
			Assert.AreEqual(2.5m, result.Value);
		}
	}
}
