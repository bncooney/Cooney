using Cooney.AI.WebApp.Components;
using Cooney.AI.WebApp.Services;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Cooney.AI.WebApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddRazorComponents()
				.AddInteractiveServerComponents();
			builder.Services.AddFluentUIComponents();

			// Configure AI service
			builder.Services.Configure<AIServiceOptions>(
				builder.Configuration.GetSection(AIServiceOptions.SectionName));

			// Register scoped chat service (per Blazor circuit/user session)
			builder.Services.AddScoped<IChatService, ChatService>();

			var app = builder.Build();

			// Configure the HTTP request pipeline.
			if (!app.Environment.IsDevelopment())
			{
				app.UseExceptionHandler("/Error");
				// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
				app.UseHsts();
			}

			app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
			app.UseHttpsRedirection();

			app.UseAntiforgery();

			app.MapStaticAssets();
			app.MapRazorComponents<App>()
				.AddInteractiveServerRenderMode();

			app.Run();
		}
	}
}
