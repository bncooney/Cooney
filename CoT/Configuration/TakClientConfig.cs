namespace CoT.Configuration;

/// <summary>
/// Configuration for TAK protocol clients.
/// </summary>
public class TakClientConfig
{
	/// <summary>
	/// Host address (IP or hostname)
	/// Default: 239.2.3.1 (standard TAK multicast group)
	/// </summary>
	public string Host { get; init; } = "239.2.3.1";

	/// <summary>
	/// Port number
	/// Default: 6969 (standard TAK port)
	/// </summary>
	public int Port { get; init; } = 6969;

	/// <summary>
	/// Protocol type (UDP Mesh or TCP Stream)
	/// </summary>
	public TakProtocol Protocol { get; init; } = TakProtocol.UDP_Mesh;

	/// <summary>
	/// Whether to support fallback to XML parsing (Version 0)
	/// </summary>
	public bool SupportXmlFallback { get; init; } = true;

	/// <summary>
	/// Buffer size for receiving messages (bytes)
	/// </summary>
	public int BufferSize { get; init; } = 8192;

	/// <summary>
	/// Reconnection interval for TCP connections
	/// </summary>
	public TimeSpan ReconnectInterval { get; init; } = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Connection timeout for TCP connections
	/// </summary>
	public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Whether to automatically validate and filter stale messages
	/// </summary>
	public bool FilterStaleMessages { get; init; } = true;

	/// <summary>
	/// Local interface address to bind to (null = any interface)
	/// </summary>
	public string? LocalAddress { get; init; } = null;

	/// <summary>
	/// Validates the configuration.
	/// </summary>
	public void Validate()
	{
		if (string.IsNullOrWhiteSpace(Host))
			throw new InvalidOperationException("Host cannot be null or empty");

		if (Port <= 0 || Port > 65535)
			throw new InvalidOperationException($"Invalid port number: {Port}");

		if (BufferSize <= 0)
			throw new InvalidOperationException($"Invalid buffer size: {BufferSize}");

		if (ReconnectInterval <= TimeSpan.Zero)
			throw new InvalidOperationException("ReconnectInterval must be positive");

		if (ConnectionTimeout <= TimeSpan.Zero)
			throw new InvalidOperationException("ConnectionTimeout must be positive");
	}
}
