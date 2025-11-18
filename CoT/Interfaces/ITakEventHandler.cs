using Atakmap.Commoncommo.Protobuf.V1;
using CoT.Configuration;

namespace CoT.Interfaces;

/// <summary>
/// Event handler interface for TAK message events.
/// </summary>
public interface ITakEventHandler
{
	/// <summary>
	/// Called when a CoT event is received.
	/// </summary>
	void OnCotEventReceived(CotEvent cotEvent);

	/// <summary>
	/// Called when a complete TAK message is received (including control messages).
	/// </summary>
	void OnTakMessageReceived(TakMessage takMessage);

	/// <summary>
	/// Called when an error occurs during message reception or parsing.
	/// </summary>
	void OnError(Exception ex);

	/// <summary>
	/// Called when the connection state changes.
	/// </summary>
	void OnConnectionStateChanged(ConnectionState state);
}
