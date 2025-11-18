namespace CoT.Configuration;

/// <summary>
/// TAK protocol transport types.
/// </summary>
public enum TakProtocol
{
	/// <summary>
	/// UDP Multicast (Mesh Mode) - typically 239.2.3.1:6969
	/// </summary>
	UDP_Mesh,

	/// <summary>
	/// TCP Stream to TAK Server
	/// </summary>
	TCP_Stream
}
