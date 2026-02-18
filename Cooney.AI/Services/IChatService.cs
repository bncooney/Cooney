using Microsoft.Extensions.AI;
using System.Collections.Generic;

namespace Cooney.AI.Services;

public interface IChatService
{
	/// <summary>
	/// Sends a list of messages and returns streaming responses
	/// </summary>
	IAsyncEnumerable<string> GetStreamingResponseAsync(
		IReadOnlyList<ChatMessage> chatMessages,
		IReadOnlyList<AITool>? extraTools = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the total number of tokens used in the current conversation.
	/// </summary>
	long TokenCount { get; }
}
