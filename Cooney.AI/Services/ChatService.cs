using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Cooney.AI.Services;

public class ChatService : IChatService
{
	private readonly IChatClient _chatClient;
	private readonly ChatOptions _chatOptions;
	private readonly List<AITool>? _defaultTools;
	private long _tokenCount = 0;

	public long TokenCount => _tokenCount;

	public ChatService(IOptions<ChatServiceOptions> options, List<AITool> tools)
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

		_defaultTools = tools?.Count > 0 ? tools : null;

		_chatOptions = new ChatOptions
		{
			AllowBackgroundResponses = config.ChatOptions.AllowBackgroundResponses,
			AllowMultipleToolCalls = config.ChatOptions.AllowMultipleToolCalls,
			Tools = tools
		};
	}

	public async IAsyncEnumerable<string> GetStreamingResponseAsync(
		IReadOnlyList<ChatMessage> chatMessages,
		IReadOnlyList<AITool>? extraTools = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		long previousTokenCount = _tokenCount;

		var options = BuildOptions(extraTools);

		await foreach (var update in _chatClient.GetStreamingResponseAsync(
			chatMessages,
			options,
			cancellationToken: cancellationToken))
		{
			if (update.Contents.OfType<UsageContent>().FirstOrDefault() is UsageContent usage)
			{
				if (usage.Details?.TotalTokenCount is long count)
				{
					_tokenCount = previousTokenCount + count;
				}
			}

			if (!string.IsNullOrEmpty(update.Text))
			{
				yield return update.Text;
			}
		}
	}

	private ChatOptions BuildOptions(IReadOnlyList<AITool>? extraTools)
	{
		if (extraTools is not { Count: > 0 })
			return _chatOptions;

		var merged = new List<AITool>(_defaultTools ?? []);
		merged.AddRange(extraTools);

		return new ChatOptions
		{
			AllowBackgroundResponses = _chatOptions.AllowBackgroundResponses,
			AllowMultipleToolCalls = _chatOptions.AllowMultipleToolCalls,
			Tools = merged
		};
	}
}
