namespace Cooney.LanguageServer.Protocol.Initialize;

/// <summary>
/// The initialize parameters sent from the client to the server.
/// </summary>
public class InitializeParams
{
	/// <summary>
	/// The process Id of the parent process that started the server.
	/// Is null if the process has not been started by another process.
	/// If the parent process is not alive then the server should exit.
	/// </summary>
	public int? ProcessId { get; set; }

	/// <summary>
	/// Information about the client.
	/// </summary>
	public ClientInfo? ClientInfo { get; set; }

	/// <summary>
	/// The locale the client is currently showing the user interface in.
	/// This must not necessarily be the locale of the operating system.
	/// </summary>
	public string? Locale { get; set; }

	/// <summary>
	/// The root path of the workspace. Is null if no folder is open.
	/// Deprecated in favour of rootUri.
	/// </summary>
	public string? RootPath { get; set; }

	/// <summary>
	/// The root URI of the workspace. Is null if no folder is open.
	/// If both rootPath and rootUri are set, rootUri wins.
	/// </summary>
	public string? RootUri { get; set; }

	/// <summary>
	/// The capabilities provided by the client.
	/// </summary>
	public ClientCapabilities? Capabilities { get; set; }

	/// <summary>
	/// User provided initialization options.
	/// </summary>
	public object? InitializationOptions { get; set; }

	/// <summary>
	/// The initial trace setting. If omitted trace is disabled ('off').
	/// </summary>
	public string? Trace { get; set; }
}

/// <summary>
/// Information about the client.
/// </summary>
public class ClientInfo
{
	/// <summary>
	/// The name of the client as defined by the client.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// The client's version as defined by the client.
	/// </summary>
	public string? Version { get; set; }
}
