using System.IO;
using System.Runtime.CompilerServices;
using Cooney.AI.Services;
using Microsoft.Extensions.AI;

namespace DevChat.Services;

public class StubChatService(string testDbFolder) : IChatService, IDisposable
{
	public long TokenCount => 0;

	public async IAsyncEnumerable<string> GetStreamingResponseAsync(
		IReadOnlyList<ChatMessage> chatMessages,
		IReadOnlyList<AITool>? extraTools = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
		yield return "Test response";
	}

	public void Dispose()
	{
		try
		{ Directory.Delete(testDbFolder, recursive: true); }
		catch { /* best-effort cleanup */ }
	}
}
