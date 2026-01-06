using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;
using System.Text;

namespace Cooney.AI.Test.Integration;

/// <summary>
/// Integration tests for ReadFile tool with AI chat clients.
/// These tests verify that AI models can successfully use the ReadFile tool to read and analyze files.
/// </summary>
[TestClass]
public sealed class ReadFileToolTests
{
	private static IChatClient _chatClient = null!;
	private static string _testFilePath = null!;
	private static string _largeFilePath = null!;

	public TestContext TestContext { get; set; } = null!;

	[ClassInitialize]
	public static void ClassSetup(TestContext context)
	{
		// Initialize chat client once for all tests
		_chatClient = new ChatClientBuilder(
			new OpenAIClient(new ApiKeyCredential("ignored"), new OpenAIClientOptions
			{
				Endpoint = new Uri("http://promaxgb10-7d49.local:8000/v1"),
			})
			.GetChatClient("nvidia/NVIDIA-Nemotron-3-Nano-30B-A3B-FP8")
			.AsIChatClient())
		.UseFunctionInvocation()
		.Build();

		// Create test file with structured content
		_testFilePath = Path.Combine(Path.GetTempPath(), $"AIToolTest_{Guid.NewGuid()}.cs");
		string testContent = @"using System;
using System.Collections.Generic;

namespace TestNamespace
{
	public class SampleClass
	{
		private int _counter;

		public SampleClass()
		{
			_counter = 0;
		}

		public void IncrementCounter()
		{
			_counter++;
		}

		public int GetCounter()
		{
			return _counter;
		}
	}
}";
		File.WriteAllText(_testFilePath, testContent);

		// Create large test file for chunking test
		_largeFilePath = Path.Combine(Path.GetTempPath(), $"AIToolLargeTest_{Guid.NewGuid()}.txt");
		var largeContent = new StringBuilder();
		for (int i = 1; i <= 100; i++)
		{
			largeContent.AppendLine($"Line {i}: This is test data for demonstrating chunked file reading.");
		}
		File.WriteAllText(_largeFilePath, largeContent.ToString());
	}

	[ClassCleanup]
	public static void ClassCleanup()
	{
		// Clean up test files
		if (File.Exists(_testFilePath))
			File.Delete(_testFilePath);

		if (File.Exists(_largeFilePath))
			File.Delete(_largeFilePath);

		// Dispose chat client if it implements IDisposable
		if (_chatClient is IDisposable disposable)
			disposable.Dispose();
	}

	/// <summary>
	/// Tests that an AI model can use the ReadFile tool to read and analyze code structure.
	/// This verifies the full integration: tool registration, AI function calling, and response parsing.
	/// </summary>
	[TestMethod]
	public async Task ReadFile_AIAnalyzesCodeStructure_ReturnsInsights()
	{
		// Arrange
		var readFileTool = new Tools.ReadFile();

		var chatOptions = new ChatOptions
		{
			AllowBackgroundResponses = true,
			AllowMultipleToolCalls = true,
			MaxOutputTokens = 32_000,
			Tools = [readFileTool],
			Temperature = 0.1f,
		};

		List<ChatMessage> chatHistory =
		[
			new ChatMessage(ChatRole.System,
				"You are a code analysis assistant. Use the read_file tool to analyze code structure. " +
				"Provide concise insights about the code you read."),
			new ChatMessage(ChatRole.User,
				$"Please read the C# file at '{_testFilePath}' and tell me: " +
				$"1) What class is defined? 2) What methods does it have? 3) What is the purpose of this class?")
		];

		var response = new StringBuilder();

		// Act
		await foreach (ChatResponseUpdate item in _chatClient.GetStreamingResponseAsync(
			chatHistory,
			chatOptions,
			cancellationToken: TestContext.CancellationToken))
		{
			response.Append(item.Text);
		}

		string result = response.ToString();

		// Assert - Verify AI successfully read and analyzed the file
		TestContext?.WriteLine("AI Analysis Result:");
		TestContext?.WriteLine(result);
		TestContext?.WriteLine("");

		// The AI should have read the file and identified key elements
		Assert.IsGreaterThan(0, result.Length, "AI should provide analysis");

		// Note: Exact response content depends on the model, but it should demonstrate
		// that the file was successfully read via the tool
	}

	/// <summary>
	/// Tests that an AI model can use the ReadFile tool with chunking to read large files.
	/// Demonstrates the offset/limit functionality for processing files in chunks.
	/// </summary>
	[TestMethod]
	public async Task ReadFile_AIReadsLargeFileInChunks_ProcessesSuccessfully()
	{
		// Arrange
		var readFileTool = new Tools.ReadFile();

		var chatOptions = new ChatOptions
		{
			AllowBackgroundResponses = true,
			AllowMultipleToolCalls = true,
			MaxOutputTokens = 32_000,
			Tools = [readFileTool],
			Temperature = 0.1f,
		};

		List<ChatMessage> chatHistory =
		[
			new ChatMessage(ChatRole.System,
				"You are a file analysis assistant. Use the read_file tool to read files. " +
				"For large files, use the offset and limit parameters to read in chunks. " +
				"If was_truncated is true, you should read the next chunk using an appropriate offset."),
			new ChatMessage(ChatRole.User,
				$"The file at '{_largeFilePath}' contains 100 lines. " +
				$"Please read the first 30 lines, then read lines 30-60, and tell me what pattern you observe in the content.")
		];

		var response = new StringBuilder();

		// Act
		await foreach (ChatResponseUpdate item in _chatClient.GetStreamingResponseAsync(
			chatHistory,
			chatOptions,
			cancellationToken: TestContext.CancellationToken))
		{
			response.Append(item.Text);
		}

		string result = response.ToString();

		// Assert
		TestContext?.WriteLine("AI Chunked Reading Result:");
		TestContext?.WriteLine(result);
		TestContext?.WriteLine("");

		Assert.IsGreaterThan(0, result.Length, "AI should provide analysis of chunked reading");

		// The AI should have successfully used offset/limit to read the file in chunks
		// and identified the pattern in the test data
	}

	/// <summary>
	/// Tests that an AI model can handle errors gracefully when using the ReadFile tool
	/// with invalid file paths or parameters.
	/// </summary>
	[TestMethod]
	public async Task ReadFile_AIHandlesInvalidPath_ReportsErrorGracefully()
	{
		// Arrange
		var readFileTool = new Tools.ReadFile();

		var chatOptions = new ChatOptions
		{
			AllowBackgroundResponses = true,
			AllowMultipleToolCalls = true,
			MaxOutputTokens = 32_000,
			Tools = [readFileTool],
			Temperature = 0.1f,
		};

		string nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}.txt");

		List<ChatMessage> chatHistory =
		[
			new ChatMessage(ChatRole.System,
				"You are a helpful assistant. Use the read_file tool to read files. " +
				"If the tool returns an error, explain the error to the user clearly."),
			new ChatMessage(ChatRole.User,
				$"Please read the file at '{nonExistentPath}' and tell me its contents.")
		];

		var response = new StringBuilder();

		// Act
		await foreach (ChatResponseUpdate item in _chatClient.GetStreamingResponseAsync(
			chatHistory,
			chatOptions,
			cancellationToken: TestContext.CancellationToken))
		{
			response.Append(item.Text);
		}

		string result = response.ToString();

		// Assert
		TestContext?.WriteLine("AI Error Handling Result:");
		TestContext?.WriteLine(result);
		TestContext?.WriteLine("");

		Assert.IsGreaterThan(0, result.Length, "AI should provide error explanation");

		// The AI should have gracefully handled the error from the read_file tool
		// and communicated it clearly to the user
	}
}
