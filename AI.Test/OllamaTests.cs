using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Text;

namespace AI.Test;

[TestClass]
public sealed class OllamaTests
{
	public TestContext? TestContext { get; set; }

	[TestMethod]
	public async Task TestMethod1()
	{
		IChatClient chatClient = new OllamaApiClient(new Uri("http://192.168.1.41:11434/"), "gpt-oss:20b");
		List<ChatMessage> chatHistory =
		[
			new ChatMessage(ChatRole.System, "You are a helpful assistant, answering questions in a concise manner, as quickly as possible."),
		];

		var response = new StringBuilder();
		
		await foreach (ChatResponseUpdate item in chatClient.GetStreamingResponseAsync(chatHistory))
		{
			response.Append(item.Text);
		}

		TestContext?.WriteLine(response.ToString());
	}
}
