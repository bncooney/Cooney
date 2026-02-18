using CommunityToolkit.Mvvm.ComponentModel;
using DevChat.Data;
using DevChat.Models;
using Microsoft.EntityFrameworkCore;

namespace DevChat.ViewModels;

public partial class ConversationViewModel(ConversationEntity entity) : ObservableObject
{
	private readonly ConversationEntity _entity = entity;

	public static async Task<ConversationViewModel> CreateNewAsync(IDbContextFactory<ChatDbContext> dbFactory)
	{
		var entity = new ConversationEntity
		{
			Id = Guid.NewGuid(),
			Title = "New Chat",
			CreatedAt = DateTime.UtcNow
		};
		using var db = await dbFactory.CreateDbContextAsync();
		db.Conversations.Add(entity);
		await db.SaveChangesAsync();
		return new ConversationViewModel(entity);
	}

	public Guid Id => _entity.Id;

	public DateTime CreatedAt => _entity.CreatedAt;

	public string Title
	{
		get => _entity.Title;
		set => SetProperty(_entity.Title, value.ReplaceLineEndings(" "), _entity, static (e, v) => e.Title = v);
	}
}
