namespace Cooney.AI.Services;

public class ChatServiceOptions
{
	public const string SectionName = "ChatService";

	public string Endpoint { get; set; } = string.Empty;
	public string Model { get; set; } = string.Empty;
	public string ApiKey { get; set; } = "ignored";
	public ChatOptionsConfig ChatOptions { get; set; } = new();
}

public class ChatOptionsConfig
{
	public bool AllowBackgroundResponses { get; set; } = true;
	public bool AllowMultipleToolCalls { get; set; } = true;
}
