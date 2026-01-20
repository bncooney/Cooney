namespace Cooney.AI.WindowsDesktop.Models;

/// <summary>
/// Represents the sender of a chat message.
/// </summary>
public enum Sender
{
	User,
	Assistant
}

/// <summary>
/// Pure data model representing a chat message.
/// Contains no UI or presentation logic.
/// </summary>
public class ChatMessage
{
	public Sender From { get; }
	public string Text { get; private set; }
	public DateTime Timestamp { get; }

	public ChatMessage(Sender from, string text)
	{
		From = from;
		Text = text;
		Timestamp = DateTime.UtcNow;
	}

	/// <summary>
	/// Appends text to the message (used for streaming responses).
	/// </summary>
	public void AppendText(string chunk)
	{
		Text += chunk;
	}
}
