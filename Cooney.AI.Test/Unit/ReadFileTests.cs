using Microsoft.Extensions.AI;
using System.Text.Json.Nodes;

namespace Cooney.AI.Test.Unit
{
	[TestClass]
	public sealed class ReadFileTests
	{
		private string _testFilePath = null!;
		private const string TestFileContent = "Line 1\nLine 2\nLine 3\nLine 4\nLine 5";

		[TestInitialize]
		public void TestSetup()
		{
			// Create a temporary test file
			_testFilePath = Path.Combine(Path.GetTempPath(), $"ReadFileTest_{Guid.NewGuid()}.txt");
			File.WriteAllText(_testFilePath, TestFileContent);
		}

		[TestCleanup]
		public void TestCleanup()
		{
			// Clean up test file
			if (File.Exists(_testFilePath))
			{
				File.Delete(_testFilePath);
			}
		}

		[TestMethod]
		public async Task ReadFile_ReadEntireFile_ReturnsAllContent()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = _testFilePath
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			Assert.IsInstanceOfType<JsonObject>(result);
			var jsonResult = (JsonObject)result;

			Assert.IsTrue((bool)jsonResult["success"]!);
			Assert.AreEqual(5, (int)jsonResult["lines_read"]!);
			Assert.AreEqual(5, (int)jsonResult["total_lines"]!);
			Assert.IsFalse((bool)jsonResult["was_truncated"]!);
			Assert.AreEqual(0, (int)jsonResult["offset"]!);

			string content = jsonResult["content"]!.ToString();
			Assert.Contains("Line 1", content);
			Assert.Contains("Line 5", content);
		}

		[TestMethod]
		public async Task ReadFile_ReadWithLimit_ReturnsLimitedContent()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = _testFilePath,
				["limit"] = 3
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsTrue((bool)jsonResult["success"]!);
			Assert.AreEqual(3, (int)jsonResult["lines_read"]!);
			Assert.AreEqual(5, (int)jsonResult["total_lines"]!);
			Assert.IsTrue((bool)jsonResult["was_truncated"]!);
			Assert.AreEqual(3, (int)jsonResult["limit"]!);

			string content = jsonResult["content"]!.ToString();
			Assert.Contains("Line 1", content);
			Assert.Contains("Line 3", content);
			Assert.DoesNotContain("Line 4", content);
		}

		[TestMethod]
		public async Task ReadFile_ReadWithOffset_ReturnsOffsetContent()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = _testFilePath,
				["offset"] = 2
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsTrue((bool)jsonResult["success"]!);
			Assert.AreEqual(3, (int)jsonResult["lines_read"]!);
			Assert.AreEqual(5, (int)jsonResult["total_lines"]!);
			Assert.IsFalse((bool)jsonResult["was_truncated"]!);
			Assert.AreEqual(2, (int)jsonResult["offset"]!);

			string content = jsonResult["content"]!.ToString();
			Assert.DoesNotContain("Line 1", content);
			Assert.DoesNotContain("Line 2", content);
			Assert.Contains("Line 3", content);
			Assert.Contains("Line 5", content);
		}

		[TestMethod]
		public async Task ReadFile_ReadWithOffsetAndLimit_ReturnsChunk()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = _testFilePath,
				["offset"] = 1,
				["limit"] = 2
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsTrue((bool)jsonResult["success"]!);
			Assert.AreEqual(2, (int)jsonResult["lines_read"]!);
			Assert.AreEqual(5, (int)jsonResult["total_lines"]!);
			Assert.IsTrue((bool)jsonResult["was_truncated"]!);
			Assert.AreEqual(1, (int)jsonResult["offset"]!);
			Assert.AreEqual(2, (int)jsonResult["limit"]!);

			string content = jsonResult["content"]!.ToString();
			Assert.DoesNotContain("Line 1", content);
			Assert.Contains("Line 2", content);
			Assert.Contains("Line 3", content);
			Assert.DoesNotContain("Line 4", content);
		}

		[TestMethod]
		public async Task ReadFile_FileNotFound_ReturnsError()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = "C:\\NonExistent\\File.txt"
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsFalse((bool)jsonResult["success"]!);
			Assert.AreEqual("FileNotFound", jsonResult["error_type"]!.ToString());
			Assert.Contains("File not found", jsonResult["error"]!.ToString());
		}

		[TestMethod]
		public async Task ReadFile_RelativePath_ReturnsError()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = "relative/path/file.txt"
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsFalse((bool)jsonResult["success"]!);
			Assert.Contains("must be absolute", jsonResult["error"]!.ToString());
		}

		[TestMethod]
		public async Task ReadFile_MissingFilePath_ReturnsError()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments();

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsFalse((bool)jsonResult["success"]!);
			Assert.Contains("Missing required parameter: file_path", jsonResult["error"]!.ToString());
		}

		[TestMethod]
		public async Task ReadFile_NegativeOffset_ReturnsError()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = _testFilePath,
				["offset"] = -1
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsFalse((bool)jsonResult["success"]!);
			Assert.Contains("must be >= 0", jsonResult["error"]!.ToString());
		}

		[TestMethod]
		public async Task ReadFile_LimitTooLarge_ReturnsError()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = _testFilePath,
				["limit"] = 20000
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsFalse((bool)jsonResult["success"]!);
			Assert.Contains("exceeds maximum allowed value", jsonResult["error"]!.ToString());
		}

		[TestMethod]
		public async Task ReadFile_EmptyFile_ReturnsEmptyContent()
		{
			// Arrange
			var emptyFilePath = Path.Combine(Path.GetTempPath(), $"EmptyFile_{Guid.NewGuid()}.txt");
			File.WriteAllText(emptyFilePath, string.Empty);

			try
			{
				var readFile = new Tools.ReadFile();
				var arguments = new AIFunctionArguments
				{
					["file_path"] = emptyFilePath
				};

				// Act
				var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

				// Assert
				Assert.IsNotNull(result);
				var jsonResult = (JsonObject)result;

				Assert.IsTrue((bool)jsonResult["success"]!);
				Assert.AreEqual(0, (int)jsonResult["lines_read"]!);
				Assert.AreEqual(0, (int)jsonResult["total_lines"]!);
				Assert.IsFalse((bool)jsonResult["was_truncated"]!);
			}
			finally
			{
				if (File.Exists(emptyFilePath))
				{
					File.Delete(emptyFilePath);
				}
			}
		}

		[TestMethod]
		public async Task ReadFile_OffsetBeyondFileEnd_ReturnsEmptyContent()
		{
			// Arrange
			var readFile = new Tools.ReadFile();
			var arguments = new AIFunctionArguments
			{
				["file_path"] = _testFilePath,
				["offset"] = 100
			};

			// Act
			var result = await readFile.InvokeAsync(arguments, CancellationToken.None);

			// Assert
			Assert.IsNotNull(result);
			var jsonResult = (JsonObject)result;

			Assert.IsTrue((bool)jsonResult["success"]!);
			Assert.AreEqual(0, (int)jsonResult["lines_read"]!);
			Assert.AreEqual(5, (int)jsonResult["total_lines"]!);
			Assert.IsFalse((bool)jsonResult["was_truncated"]!);
		}
	}
}
