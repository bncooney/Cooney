using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Cooney.AI.WindowsDesktop;

/// <summary>
/// Application entry point. Configures dependency injection and shows main window.
/// </summary>
public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var services = new ServiceCollection();

		services.Configure<Services.AIServiceOptions>(options =>
		{
			options.ApiKey = "ignored";
			options.Endpoint = "http://promaxgb10-7d49.local:8000/v1";
			options.Model = "unsloth/gpt-oss-120b-GGUF";
		});

		services.AddSingleton<Services.IChatService, Services.ChatService>();
		services.AddSingleton<ViewModels.ChatViewModel>();
		services.AddSingleton<ViewModels.SettingsViewModel>();
		services.AddSingleton<ViewModels.MainWindowViewModel>();
		services.AddSingleton<Views.MainWindow>();

		var provider = services.BuildServiceProvider();

		var main = provider.GetRequiredService<Views.MainWindow>();
		main.Show();
	}
}
