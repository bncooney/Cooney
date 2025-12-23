using Cooney.LanguageServer.Protocol.Common;
using System.Text.Json;

namespace Cooney.LanguageServer.Test.Protocol;

[TestClass]
public class PositionTests
{
	[TestMethod]
	public void Constructor_SetsProperties()
	{
		// Arrange & Act
		var position = new Position(line: 10, character: 25);

		// Assert
		Assert.AreEqual(10, position.Line);
		Assert.AreEqual(25, position.Character);
	}

	[TestMethod]
	public void ToString_ReturnsExpectedFormat()
	{
		// Arrange
		var position = new Position(5, 12);

		// Act
		var result = position.ToString();

		// Assert
		Assert.AreEqual("(5, 12)", result);
	}

	[TestMethod]
	public void Serialization_MatchesLspSpec()
	{
		// Arrange
		var position = new Position(line: 3, character: 16);
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		// Act
		var json = JsonSerializer.Serialize(position, options);
		var deserialized = JsonSerializer.Deserialize<Position>(json, options);

		// Assert
		Assert.AreEqual(position.Line, deserialized.Line);
		Assert.AreEqual(position.Character, deserialized.Character);
		Assert.Contains("\"line\":3", json);
		Assert.Contains("\"character\":16", json);
	}
}
