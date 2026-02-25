using System.IO;
using System.Windows;
using Cooney.AI.Services;
using Cooney.AI.Tools;
using DevChat.Data;
using DevChat.Services;
using DevChat.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DevChat;

public partial class App : Application
{
	private IHost? _host;

	protected override async void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var isTestMode = e.Args.Contains("--test");

		var dbFolder = isTestMode
			? Path.Combine(Path.GetTempPath(), "DevChat.AutoTest", Guid.NewGuid().ToString())
			: ChatDbContext.DatabaseFolder;
		var dbPath = Path.Combine(dbFolder, "devchat.db");

		_host = Host.CreateDefaultBuilder(e.Args)
			.UseContentRoot(AppDomain.CurrentDomain.BaseDirectory)
			.ConfigureServices((ctx, services) =>
			{
				services.Configure<ChatServiceOptions>(
					ctx.Configuration.GetSection(ChatServiceOptions.SectionName));

				Directory.CreateDirectory(dbFolder);
				services.AddDbContextFactory<ChatDbContext>(options =>
					options.UseSqlite($"Data Source={dbPath}"));
				services.AddDbContextFactory<TodoDbContext>(options =>
					options.UseSqlite($"Data Source={TodoDbContext.DatabasePath}"));

				if (isTestMode)
				{
					services.AddSingleton<IChatService>(new StubChatService(dbFolder));
					services.AddSingleton<ITodoService, StubTodoService>();
				}
				else
				{
					services.AddSingleton<IChatService, ChatService>();
					services.AddSingleton<ITodoService, EfTodoService>();
				}

				services.AddSingleton<List<AITool>>([new Calculator()]);

				services.AddSingleton(new SettingsViewModel(dbPath));
				services.AddSingleton<MainViewModel>();
				services.AddTransient<MainWindow>();
			})
			.Build();

		var dbFactory = _host.Services.GetRequiredService<IDbContextFactory<ChatDbContext>>();
		using (var db = await dbFactory.CreateDbContextAsync())
		{
			await db.Database.EnsureCreatedAsync();
		}

		var todoDbFactory = _host.Services.GetRequiredService<IDbContextFactory<TodoDbContext>>();
		using (var db = await todoDbFactory.CreateDbContextAsync())
		{
			await db.Database.EnsureCreatedAsync();
		}

		await _host.StartAsync();

		var mainVm = _host.Services.GetRequiredService<MainViewModel>();
		await mainVm.LoadConversationsAsync();

		var mainWindow = _host.Services.GetRequiredService<MainWindow>();
		mainWindow.Show();
	}

	protected override async void OnExit(ExitEventArgs e)
	{
		if (_host is not null)
		{
			var mainVm = _host.Services.GetRequiredService<MainViewModel>();
			await mainVm.DisposeAsync();

			await _host.StopAsync();
			_host.Dispose();
		}
		base.OnExit(e);
	}
}
