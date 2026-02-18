using System.Diagnostics;
using System.Net.Http;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace DevChat.Test;

/// <summary>
/// Manages WinAppDriver and the application session for UI tests.
/// WinAppDriver must be installed: https://github.com/microsoft/WinAppDriver/releases
/// Developer Mode must be enabled in Windows Settings.
/// </summary>
public static class DevChatSession
{
	private const string WinAppDriverUrl = "http://127.0.0.1:4723";
	private const string WinAppDriverPath =
		@"C:\Program Files (x86)\Windows Application Driver\WinAppDriver.exe";

	private static Process? _winAppDriverProcess;

	public static WindowsDriver<WindowsElement> Driver { get; private set; } = null!;
	private static WindowsDriver<WindowsElement>? _desktopSession;

	public static void Setup(string? appArguments = null)
	{
		// A root Desktop session lets us locate windows by title after HWND changes.
		var desktopOptions = new AppiumOptions();
		desktopOptions.AddAdditionalCapability("app", "Root");
		desktopOptions.AddAdditionalCapability("deviceName", "WindowsPC");
		desktopOptions.AddAdditionalCapability("platformName", "Windows");
		_desktopSession = new WindowsDriver<WindowsElement>(
			new Uri(WinAppDriverUrl), desktopOptions);

		var appPath = FindAppExecutable();
		var options = new AppiumOptions();
		options.AddAdditionalCapability("app", appPath);
		options.AddAdditionalCapability("deviceName", "WindowsPC");
		options.AddAdditionalCapability("platformName", "Windows");

		if (appArguments is not null)
			options.AddAdditionalCapability("appArguments", appArguments);

		Driver = new WindowsDriver<WindowsElement>(new Uri(WinAppDriverUrl), options);
		Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
	}

	/// <summary>
	/// Re-attaches the Driver to the DevChat window after a <c>WindowStyle</c> change.
	/// WPF recreates the underlying Win32 window (new HWND) when the style changes,
	/// which invalidates the existing session. We use the Desktop session to find the
	/// window by title and create a fresh session scoped to the new handle.
	/// </summary>
	public static void ReattachToAppWindow()
	{
		var appWindow = _desktopSession!.FindElementByName("DevChat");
		var nativeHandle = appWindow.GetAttribute("NativeWindowHandle");
		var hexHandle = int.Parse(nativeHandle).ToString("x");

		var options = new AppiumOptions();
		options.AddAdditionalCapability("appTopLevelWindow", hexHandle);
		options.AddAdditionalCapability("deviceName", "WindowsPC");
		options.AddAdditionalCapability("platformName", "Windows");

		// Don't Quit() the old driver — it was created with the "app" capability
		// and quitting it would terminate the application process.
		Driver = new WindowsDriver<WindowsElement>(new Uri(WinAppDriverUrl), options);
		Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
	}

	public static void TearDown()
	{
		try
		{ Driver?.Quit(); }
		catch { /* may be stale */ }
		Driver = null!;

		try
		{ _desktopSession?.Quit(); }
		catch { }
		_desktopSession = null;

		// Ensure the app is closed even if the session quit didn't terminate it.
		foreach (var proc in Process.GetProcessesByName("DevChat"))
		{
			try
			{ proc.Kill(); proc.Dispose(); }
			catch { }
		}
	}

	public static void StartWinAppDriver()
	{
		if (Process.GetProcessesByName("WinAppDriver").Length > 0)
			return;

		if (!File.Exists(WinAppDriverPath))
		{
			throw new FileNotFoundException(
				"WinAppDriver is not installed. Download it from " +
				"https://github.com/microsoft/WinAppDriver/releases and enable Developer Mode.");
		}

		_winAppDriverProcess = Process.Start(new ProcessStartInfo
		{
			FileName = WinAppDriverPath,
			UseShellExecute = true,
		});

		// Poll until WinAppDriver is accepting HTTP connections.
		using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
		for (var i = 0; i < 30; i++)
		{
			try
			{
				http.GetAsync($"{WinAppDriverUrl}/status").GetAwaiter().GetResult();
				return;
			}
			catch { Thread.Sleep(100); }
		}

		throw new TimeoutException("WinAppDriver did not start within 3 seconds.");
	}

	public static void StopWinAppDriver()
	{
		if (_winAppDriverProcess is { HasExited: false })
		{
			_winAppDriverProcess.Kill();
			_winAppDriverProcess.Dispose();
			_winAppDriverProcess = null;
		}
	}

	private static string FindAppExecutable()
	{
		var solutionDir = FindSolutionDirectory();

		// Try common build output paths in order of likelihood.
		string[] candidates =
		[
			Path.Combine(solutionDir, "DevChat", "bin", "Debug", "net10.0-windows", "DevChat.exe"),
			Path.Combine(solutionDir, "DevChat", "bin", "x64", "Debug", "net10.0-windows", "DevChat.exe"),
			Path.Combine(solutionDir, "DevChat", "bin", "Release", "net10.0-windows", "DevChat.exe"),
			Path.Combine(solutionDir, "DevChat", "bin", "x64", "Release", "net10.0-windows", "DevChat.exe"),
		];

		foreach (var candidate in candidates)
		{
			if (File.Exists(candidate))
				return candidate;
		}

		throw new FileNotFoundException(
			"DevChat.exe not found. Build the DevChat project first.\n" +
			$"Searched in:\n  {string.Join("\n  ", candidates)}");
	}

	internal static string FindSolutionDirectory()
	{
		var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
		while (dir is not null)
		{
			if (dir.GetFiles("*.slnx").Length > 0 || dir.GetFiles("*.sln").Length > 0)
				return dir.FullName;
			dir = dir.Parent;
		}

		throw new DirectoryNotFoundException(
			"Could not locate the solution directory. " +
			$"Started searching from: {AppDomain.CurrentDomain.BaseDirectory}");
	}
}
