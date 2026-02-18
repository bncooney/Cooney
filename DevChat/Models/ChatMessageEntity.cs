namespace DevChat.Models;

public class ChatMessageEntity
{
	public int Id { get; set; }
	public Guid ConversationId { get; set; }
	public string Role { get; set; } = string.Empty;
	public string Content { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public ConversationEntity Conversation { get; set; } = null!;
}
