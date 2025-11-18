using System.Net;
using System.Net.Sockets;
using Atakmap.Commoncommo.Protobuf.V1;
using CoT.Protocol;

namespace CoT.Test.TestInfrastructure;

/// <summary>
/// Simulated UDP multicast sender for testing UDP mesh clients.
/// </summary>
public class SimulatedUdpMulticast : IDisposable
{
	private readonly UdpClient _udpClient;
	private readonly IPEndPoint _multicastEndpoint;
	private readonly TakMessageParser _parser = new();

	public string MulticastAddress { get; }
	public int Port { get; }

	public SimulatedUdpMulticast(string multicastAddress = "239.2.3.1", int port = 6969)
	{
		MulticastAddress = multicastAddress;
		Port = port;
		_multicastEndpoint = new IPEndPoint(IPAddress.Parse(multicastAddress), port);
		_udpClient = new UdpClient();
	}

	public async Task SendMessageAsync(TakMessage message, bool useProtobuf = true)
	{
		byte[] payload;
		byte version;

		if (useProtobuf)
		{
			payload = _parser.SerializeToProtobuf(message);
			version = 1;
		}
		else
		{
			payload = _parser.SerializeToXml(message.CotEvent!);
			version = 0;
		}

		// Create TAK protocol header (mesh mode)
		var header = new TakProtocolHeader
		{
			Version = version,
			IsStreamMode = false
		};

		var headerBytes = header.ToBytes();
		var packet = new byte[headerBytes.Length + payload.Length];
		Array.Copy(headerBytes, 0, packet, 0, headerBytes.Length);
		Array.Copy(payload, 0, packet, headerBytes.Length, payload.Length);

		await _udpClient.SendAsync(packet, _multicastEndpoint);
	}

	public async Task SendCotEventAsync(CotEvent cotEvent, bool useProtobuf = true)
	{
		var message = new TakMessage
		{
			CotEvent = cotEvent,
			CreationTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};

		await SendMessageAsync(message, useProtobuf);
	}

	public void Dispose()
	{
		_udpClient?.Dispose();
	}
}
