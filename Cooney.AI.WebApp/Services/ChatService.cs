using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using Cooney.AI.Tools;

namespace Cooney.AI.WebApp.Services;

public class ChatService : IChatService
{
	private readonly IChatClient _chatClient;
	private readonly ChatOptions _chatOptions;
	private readonly List<ChatMessage> _chatHistory;

	public IReadOnlyList<ChatMessage> ChatHistory => _chatHistory.AsReadOnly();

	public ChatService(IOptions<AIServiceOptions> options)
	{
		var config = options.Value;

		// Initialize chat client using the pattern from IntegrationTests.cs
		_chatClient = new ChatClientBuilder(
			new OpenAIClient(
				new ApiKeyCredential(config.ApiKey),
				new OpenAIClientOptions
				{
					Endpoint = new Uri(config.Endpoint),
				})
				.GetChatClient(config.Model)
				.AsIChatClient())
			.UseFunctionInvocation()
			.Build();

		// Configure chat options with all 5 tools
		_chatOptions = new ChatOptions
		{
			AllowBackgroundResponses = config.ChatOptions.AllowBackgroundResponses,
			AllowMultipleToolCalls = config.ChatOptions.AllowMultipleToolCalls,
			MaxOutputTokens = config.ChatOptions.MaxOutputTokens,
			Temperature = config.ChatOptions.Temperature,
			Tools =
			[
				new Calculator(),
				new WordCount(),
			]
		};

		// Initialize chat history with system prompt
		_chatHistory =
		[
			new(ChatRole.System, config.SystemPrompt)
		];
	}

	public async IAsyncEnumerable<ChatResponseUpdate> SendMessageAsync(
		string userMessage,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		// Add user message to history
		var userChatMessage = new ChatMessage(ChatRole.User, userMessage);
		_chatHistory.Add(userChatMessage);

		// Stream responses
		var assistantResponse = new StringBuilder();

		await foreach (var update in _chatClient.GetStreamingResponseAsync(
			_chatHistory,
			_chatOptions,
			cancellationToken: cancellationToken))
		{
			if (update.Text != null)
			{
				assistantResponse.Append(update.Text);
			}

			yield return update;
		}

		// Add complete assistant response to history
		if (assistantResponse.Length > 0)
		{
			_chatHistory.Add(new ChatMessage(ChatRole.Assistant, assistantResponse.ToString()));
		}
	}

	public void ClearHistory()
	{
		// Keep only system message
		var systemMessage = _chatHistory.FirstOrDefault(m => m.Role == ChatRole.System);
		_chatHistory.Clear();
		if (systemMessage != null)
		{
			_chatHistory.Add(systemMessage);
		}
	}

	public void AddSystemMessage(string message)
	{
		_chatHistory.Add(new ChatMessage(ChatRole.System, message));
	}
}
