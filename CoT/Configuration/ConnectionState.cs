namespace CoT.Configuration;

/// <summary>
/// Represents the connection state of a TAK client.
/// </summary>
public enum ConnectionState
{
	/// <summary>
	/// Client is disconnected
	/// </summary>
	Disconnected,

	/// <summary>
	/// Client is attempting to connect
	/// </summary>
	Connecting,

	/// <summary>
	/// Client is connected and ready to receive messages
	/// </summary>
	Connected,

	/// <summary>
	/// Client encountered an error
	/// </summary>
	Error,

	/// <summary>
	/// Client is reconnecting after a disconnect
	/// </summary>
	Reconnecting
}
