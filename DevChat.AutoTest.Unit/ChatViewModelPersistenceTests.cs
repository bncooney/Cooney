using Cooney.AI.Services;
using DevChat.Data;
using DevChat.Services;
using DevChat.ViewModels;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DevChat.AutoTest.Unit;

[TestClass]
public class ChatViewModelPersistenceTests
{
	private SqliteConnection _connection = null!;
	private IDbContextFactory<ChatDbContext> _dbFactory = null!;

	[TestInitialize]
	public async Task Setup()
	{
		_connection = new SqliteConnection("Data Source=:memory:");
		_connection.Open();

		var options = new DbContextOptionsBuilder<ChatDbContext>()
			.UseSqlite(_connection)
			.Options;

		await using var db = new ChatDbContext(options);
		await db.Database.EnsureCreatedAsync(TestContext.CancellationToken);

		_dbFactory = new TestDbContextFactory(options);
	}

	[TestCleanup]
	public void Cleanup() => _connection.Dispose();

	[TestMethod]
	public async Task SendFirstMessage_PersistsTitleToDatabase()
	{
		var conversation = await ConversationViewModel.CreateNewAsync(_dbFactory);
		var vm = new ChatViewModel(new StubChatService(""), _dbFactory, new StubTodoService(), conversation)
		{
			UserInput = "Hello world"
		};
		await vm.SendCommand.ExecuteAsync(null);

		await using var db = await _dbFactory.CreateDbContextAsync(TestContext.CancellationToken);
		var entity = await db.Conversations.FindAsync([conversation.Id], TestContext.CancellationToken);
		Assert.AreEqual("Hello world", entity!.Title);
	}

	[TestMethod]
	public async Task SendFirstMessage_TruncatesLongTitle()
	{
		var conversation = await ConversationViewModel.CreateNewAsync(_dbFactory);
		var vm = new ChatViewModel(new StubChatService(""), _dbFactory, new StubTodoService(), conversation)
		{
			// 65 chars — well over the 40-char limit
			UserInput = "This is a very long message that exceeds forty chars by a lot!!"
		};
		await vm.SendCommand.ExecuteAsync(null);

		await using var db = await _dbFactory.CreateDbContextAsync(TestContext.CancellationToken);
		var entity = await db.Conversations.FindAsync([conversation.Id], TestContext.CancellationToken);
		Assert.AreEqual("This is a very long message that exceeds...", entity!.Title);
	}

	[TestMethod]
	public async Task SendFirstMessage_CollapsesNewlinesInTitle()
	{
		var conversation = await ConversationViewModel.CreateNewAsync(_dbFactory);
		var vm = new ChatViewModel(new StubChatService(""), _dbFactory, new StubTodoService(), conversation)
		{
			UserInput = "Line one\nLine two"
		};
		await vm.SendCommand.ExecuteAsync(null);

		await using var db = await _dbFactory.CreateDbContextAsync(TestContext.CancellationToken);
		var entity = await db.Conversations.FindAsync([conversation.Id], TestContext.CancellationToken);
		Assert.AreEqual("Line one Line two", entity!.Title);
	}

	private sealed class TestDbContextFactory(DbContextOptions<ChatDbContext> options)
		: IDbContextFactory<ChatDbContext>
	{
		public ChatDbContext CreateDbContext() => new(options);

		public Task<ChatDbContext> CreateDbContextAsync(CancellationToken _ = default)
			=> Task.FromResult(new ChatDbContext(options));
	}

	public TestContext TestContext { get; set; } = null!;
}
