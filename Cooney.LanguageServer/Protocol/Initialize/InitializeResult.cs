namespace Cooney.LanguageServer.Protocol.Initialize;

/// <summary>
/// The result returned from an initialize request.
/// </summary>
public class InitializeResult
{
	/// <summary>
	/// The capabilities the language server provides.
	/// </summary>
	public ServerCapabilities Capabilities { get; set; } = new();

	/// <summary>
	/// Information about the server.
	/// </summary>
	public ServerInfo? ServerInfo { get; set; }
}

/// <summary>
/// Information about the server.
/// </summary>
public class ServerInfo
{
	/// <summary>
	/// The name of the server as defined by the server.
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// The server's version as defined by the server.
	/// </summary>
	public string? Version { get; set; }
}
