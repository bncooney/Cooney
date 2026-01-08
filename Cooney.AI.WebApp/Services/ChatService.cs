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
	private readonly int _contextLength;
	private int _totalTokenCount = 0;

	public int ContextLength => _contextLength;
	public int TotalTokenCount => _totalTokenCount;
	public IReadOnlyList<ChatMessage> ChatHistory => _chatHistory.AsReadOnly();

	public ChatService(IOptions<AIServiceOptions> options)
	{
		var config = options.Value;

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

		// 
		_contextLength = config.ChatOptions.ContextLength;

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

			// Track token usage
			if (update.Contents.OfType<UsageContent>().FirstOrDefault() is UsageContent usage)
			{
				_totalTokenCount = (int)(usage.Details?.TotalTokenCount ?? _totalTokenCount);
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
		_totalTokenCount = 0;
	}

	public void AddSystemMessage(string message)
	{
		_chatHistory.Add(new ChatMessage(ChatRole.System, message));
	}
}
