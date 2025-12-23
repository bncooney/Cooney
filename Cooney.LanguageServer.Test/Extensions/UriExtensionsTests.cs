using Cooney.LanguageServer.Extensions;

namespace Cooney.LanguageServer.Test.Extensions;

[TestClass]
public class UriExtensionsTests
{
	[TestMethod]
	public void ToFileUri_WithAbsolutePath_ReturnsFileUri()
	{
		// Arrange
		var filePath = Path.Combine(Path.GetTempPath(), "test.txt");

		// Act
		var fileUri = UriExtensions.ToFileUri(filePath);

		// Assert
		Assert.StartsWith("file:///", fileUri);
		Assert.Contains("test.txt", fileUri);
	}

	[TestMethod]
	public void ToFilePath_WithFileUri_ReturnsLocalPath()
	{
		// Arrange
		var originalPath = Path.Combine(Path.GetTempPath(), "test.txt");
		var fileUri = UriExtensions.ToFileUri(originalPath);

		// Act
		var resultPath = UriExtensions.ToFilePath(fileUri);

		// Assert
		Assert.AreEqual(originalPath, resultPath);
	}

	[TestMethod]
	public void ToFileUri_WithNullPath_ThrowsArgumentException()
	{
		// Act & Assert
		try
		{
			UriExtensions.ToFileUri(null!);
			Assert.Fail("Expected ArgumentException was not thrown");
		}
		catch (ArgumentException)
		{
			// Expected
		}
	}

	[TestMethod]
	public void ToFilePath_WithNullUri_ThrowsArgumentException()
	{
		// Act & Assert
		try
		{
			UriExtensions.ToFilePath((string)null!);
			Assert.Fail("Expected ArgumentException was not thrown");
		}
		catch (ArgumentException)
		{
			// Expected
		}
	}

	[TestMethod]
	public void ToFilePath_WithNonFileUri_ThrowsArgumentException()
	{
		// Act & Assert
		try
		{
			UriExtensions.ToFilePath("https://example.com/test.txt");
			Assert.Fail("Expected ArgumentException was not thrown");
		}
		catch (ArgumentException)
		{
			// Expected
		}
	}
}
