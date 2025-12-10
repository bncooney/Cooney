using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Text;

namespace Cooney.AI.Test.Integration;

[TestClass]
public sealed class OllamaTests
{
	public TestContext TestContext { get; set; }

	[TestMethod]
	public async Task ResponseGenerationTest()
	{
		IChatClient chatClient = new OllamaApiClient(new Uri("http://127.0.0.1:11434/"), "gpt-oss:20b");
		List<ChatMessage> chatHistory =
		[
			new ChatMessage(ChatRole.System, "You are a helpful assistant, answering questions in a concise manner, as quickly as possible."),
			new ChatMessage(ChatRole.User, "Write a short poem about the sea.")
		];

		var response = new StringBuilder();

		await foreach (ChatResponseUpdate item in chatClient.GetStreamingResponseAsync(chatHistory, cancellationToken: TestContext.CancellationToken))
		{
			response.Append(item.Text);
		}

		TestContext?.WriteLine(response.ToString());
	}
}
