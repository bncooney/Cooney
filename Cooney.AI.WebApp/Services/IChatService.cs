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
}
