namespace DevChat.Models;

public class ConversationEntity
{
	public Guid Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public List<ChatMessageEntity> Messages { get; set; } = [];
}
