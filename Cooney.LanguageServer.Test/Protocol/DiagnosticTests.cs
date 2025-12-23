using Cooney.LanguageServer.Protocol.Common;
using Cooney.LanguageServer.Protocol.Diagnostics;
using System.Text.Json;
using Range = Cooney.LanguageServer.Protocol.Common.Range;

namespace Cooney.LanguageServer.Test.Protocol;

[TestClass]
public class DiagnosticTests
{
	[TestMethod]
	public void Constructor_WithRangeAndMessage_SetsProperties()
	{
		// Arrange
		var range = new Range(new Position(0, 0), new Position(0, 5));
		var message = "Test error";

		// Act
		var diagnostic = new Diagnostic(range, message);

		// Assert
		Assert.AreEqual(range, diagnostic.Range);
		Assert.AreEqual(message, diagnostic.Message);
	}

	[TestMethod]
	public void Severity_CanBeSet()
	{
		// Arrange
		var diagnostic = new Diagnostic();

		// Act
		diagnostic.Severity = DiagnosticSeverity.Error;

		// Assert
		Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
	}

	[TestMethod]
	public void Serialization_WithAllProperties_MatchesLspSpec()
	{
		// Arrange
		var diagnostic = new Diagnostic
		{
			Range = new Range(new Position(1, 0), new Position(1, 10)),
			Severity = DiagnosticSeverity.Warning,
			Code = "W001",
			Source = "test-linter",
			Message = "This is a test warning"
		};

		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
		};

		// Act
		var json = JsonSerializer.Serialize(diagnostic, options);
		var deserialized = JsonSerializer.Deserialize<Diagnostic>(json, options);

		// Assert
		Assert.IsNotNull(deserialized);
		Assert.AreEqual(diagnostic.Message, deserialized.Message);
		Assert.AreEqual(diagnostic.Severity, deserialized.Severity);
		Assert.AreEqual("W001", deserialized.Code?.ToString());
		Assert.AreEqual(diagnostic.Source, deserialized.Source);
	}

	[TestMethod]
	public void PublishDiagnosticsParams_Constructor_SetsProperties()
	{
		// Arrange
		var uri = "file:///C:/test.cs";
		var diagnostics = new[]
		{
			new Diagnostic(new Range(new Position(0, 0), new Position(0, 5)), "Error 1"),
			new Diagnostic(new Range(new Position(1, 0), new Position(1, 10)), "Error 2")
		};

		// Act
		var params_ = new PublishDiagnosticsParams(uri, diagnostics);

		// Assert
		Assert.AreEqual(uri, params_.Uri);
		Assert.HasCount(2, params_.Diagnostics);
		Assert.AreEqual("Error 1", params_.Diagnostics[0].Message);
		Assert.AreEqual("Error 2", params_.Diagnostics[1].Message);
	}
}
