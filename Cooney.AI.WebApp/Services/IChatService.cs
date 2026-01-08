using Microsoft.Extensions.AI;

namespace Cooney.AI.WebApp.Services;

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
	/// <remarks>This value determines the largest input or output that can be handled at once. Exceeding this limit
	/// may result in errors or truncated results.</remarks>
	/// <returns>The maximum context length, in tokens, supported by the implementation.</returns>
	int ContextLength { get; }

	/// <summary>
	/// Gets the total number of tokens used in the current conversation.
	/// </summary>
	int TotalTokenCount { get; }
}
