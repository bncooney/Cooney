using Microsoft.Extensions.AI;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.Runtime.CompilerServices;
using System.Text;
using Cooney.AI.Tools;

namespace Cooney.AI.Services;

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
				}
			)
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

		_contextLength = config.ChatOptions.ContextLength;

		_chatHistory =
		[
			new(ChatRole.System, config.SystemPrompt)
		];
	}

	public async IAsyncEnumerable<ChatResponseUpdate> SendMessageAsync(
		string userMessage,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var userChatMessage = new ChatMessage(ChatRole.User, userMessage);
		_chatHistory.Add(userChatMessage);

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

			if (update.Contents.OfType<UsageContent>().FirstOrDefault() is UsageContent usage)
			{
				_totalTokenCount = (int)(usage.Details?.TotalTokenCount ?? _totalTokenCount);
			}

			yield return update;
		}

		if (assistantResponse.Length > 0)
		{
			_chatHistory.Add(new ChatMessage(ChatRole.Assistant, assistantResponse.ToString()));
		}
	}

	public void ClearHistory()
	{
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
