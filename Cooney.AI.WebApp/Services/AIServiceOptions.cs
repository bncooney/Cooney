namespace Cooney.AI.WebApp.Services;

public class AIServiceOptions
{
	public const string SectionName = "AIService";

	public string Endpoint { get; set; } = string.Empty;
	public string Model { get; set; } = string.Empty;
	public string ApiKey { get; set; } = "ignored";
	public ChatOptionsConfig ChatOptions { get; set; } = new();
	public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";
}

public class ChatOptionsConfig
{
	public int MaxOutputTokens { get; set; } = 32000;
	public float Temperature { get; set; } = 0.1f;
	public bool AllowBackgroundResponses { get; set; } = true;
	public bool AllowMultipleToolCalls { get; set; } = true;
}
