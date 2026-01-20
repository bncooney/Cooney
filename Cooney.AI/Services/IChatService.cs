namespace Cooney.AI.Services;

using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.AI;

public interface IChatService
{
	/// <summary>
	/// Gets the current chat history for this session
	/// </summary>
	IReadOnlyList<ChatMessage> ChatHistory { get; }

	/// <summary>
	/// Sends a message and returns streaming responses
	/// </summary>
	IAsyncEnumerable<ChatResponseUpdate> SendMessageAsync(
		string userMessage,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Clears the chat history for this session
	/// </summary>
	void ClearHistory();

	/// <summary>
	/// Adds a system message to the chat history
	/// </summary>
	void AddSystemMessage(string message);

	/// <summary>
	/// Gets the maximum number of tokens that can be processed in a single context.
	/// </summary>
	int ContextLength { get; }

	/// <summary>
	/// Gets the total number of tokens used in the current conversation.
	/// </summary>
	int TotalTokenCount { get; }
}
