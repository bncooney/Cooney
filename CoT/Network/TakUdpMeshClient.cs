using System.Net;
using System.Net.Sockets;
using Atakmap.Commoncommo.Protobuf.V1;
using CoT.Configuration;
using CoT.Protocol;

namespace CoT.Network;

/// <summary>
/// UDP Multicast client for receiving TAK messages in Mesh Mode.
/// Listens on multicast group (default 239.2.3.1:6969).
/// </summary>
public class TakUdpMeshClient : IDisposable
{
	private readonly TakClientConfig _config;
	private readonly TakMessageParser _parser;
	private UdpClient? _udpClient;
	private CancellationTokenSource? _cancellationTokenSource;
	private Task? _receiveTask;
	private ConnectionState _connectionState = ConnectionState.Disconnected;

	/// <summary>
	/// Event raised when a CoT event is received.
	/// </summary>
	public event EventHandler<CotEvent>? CotEventReceived;

	/// <summary>
	/// Event raised when a complete TAK message is received.
	/// </summary>
	public event EventHandler<TakMessage>? TakMessageReceived;

	/// <summary>
	/// Event raised when an error occurs.
	/// </summary>
	public event EventHandler<Exception>? ErrorOccurred;

	/// <summary>
	/// Event raised when the connection state changes.
	/// </summary>
	public event EventHandler<ConnectionState>? ConnectionStateChanged;

	/// <summary>
	/// Gets the current connection state.
	/// </summary>
	public ConnectionState ConnectionState
	{
		get => _connectionState;
		private set
		{
			if (_connectionState != value)
			{
				_connectionState = value;
				ConnectionStateChanged?.Invoke(this, value);
			}
		}
	}

	/// <summary>
	/// Initializes a new instance of the TakUdpMeshClient.
	/// </summary>
	public TakUdpMeshClient(TakClientConfig config)
	{
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_config.Validate();
		_parser = new();
	}

	/// <summary>
	/// Starts listening for UDP multicast messages.
	/// </summary>
	public Task StartAsync()
	{
		return StartAsync(CancellationToken.None);
	}

	/// <summary>
	/// Starts listening for UDP multicast messages with a cancellation token.
	/// </summary>
	public Task StartAsync(CancellationToken cancellationToken)
	{
		if (_udpClient != null)
			throw new InvalidOperationException("Client is already running");

		ConnectionState = ConnectionState.Connecting;

		try
		{
			// Create UDP client
			_udpClient = new()
			{
				ExclusiveAddressUse = false
			};

			// Set socket options for multicast
			var localEndPoint = new IPEndPoint(IPAddress.Any, _config.Port);
			_udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_udpClient.Client.Bind(localEndPoint);

			// Join multicast group
			var multicastAddress = IPAddress.Parse(_config.Host);
			_udpClient.JoinMulticastGroup(multicastAddress);

			// Set receive buffer size
			_udpClient.Client.ReceiveBufferSize = _config.BufferSize;

			// Start receiving
			_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

			ConnectionState = ConnectionState.Connected;
		}
		catch (Exception ex)
		{
			ConnectionState = ConnectionState.Error;
			OnError(ex);
			throw;
		}

		return Task.CompletedTask;
	}

	/// <summary>
	/// Stops listening for messages.
	/// </summary>
	public async Task StopAsync()
	{
		if (_cancellationTokenSource != null)
		{
			_cancellationTokenSource.Cancel();

			try
			{
				if (_receiveTask != null)
					await _receiveTask;
			}
			catch (OperationCanceledException)
			{
				// Expected when cancelling
			}
		}

		_udpClient?.Close();
		_udpClient?.Dispose();
		_udpClient = null;

		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = null;

		ConnectionState = ConnectionState.Disconnected;
	}

	/// <summary>
	/// Main receive loop for processing UDP messages.
	/// </summary>
	private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				// Receive UDP datagram
				Guard.IsNotNull(_udpClient);
				var result = await _udpClient.ReceiveAsync();
				var data = result.Buffer;

				if (data.Length < 3)
				{
					OnError(new InvalidOperationException($"Received packet too small: {data.Length} bytes"));
					continue;
				}

				// Process the message
				ProcessMessage(data);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (Exception ex)
			{
				OnError(ex);
			}
		}
	}

	/// <summary>
	/// Processes a received message.
	/// </summary>
	private void ProcessMessage(byte[] data)
	{
		try
		{
			// Parse the TAK protocol header
			var header = TakProtocolHeader.Parse(data, out int headerSize);

			// Extract payload
			var payload = new byte[data.Length - headerSize];
			Array.Copy(data, headerSize, payload, 0, payload.Length);

			// Parse based on version
			if (header.Version == 1)
			{
				// Protobuf format
				var takMessage = _parser.ParseProtobuf(payload);
				OnTakMessageReceived(takMessage);

				if (takMessage.CotEvent != null)
				{
					if (!_config.FilterStaleMessages || !IsStale(takMessage.CotEvent))
					{
						OnCotEventReceived(takMessage.CotEvent);
					}
				}
			}
			else if (header.Version == 0 && _config.SupportXmlFallback)
			{
				// XML format (legacy)
				var cotEvent = _parser.ParseXml(payload);
				if (!_config.FilterStaleMessages || !IsStale(cotEvent))
				{
					OnCotEventReceived(cotEvent);
				}
			}
			else
			{
				OnError(new NotSupportedException($"Unsupported TAK protocol version: {header.Version}"));
			}
		}
		catch (Exception ex)
		{
			OnError(ex);
		}
	}

	/// <summary>
	/// Checks if a CoT event is stale based on its stale time.
	/// </summary>
	private bool IsStale(CotEvent cotEvent)
	{
		if (cotEvent.StaleTime == 0)
			return false;

		var staleTime = DateTimeOffset.FromUnixTimeMilliseconds((long)cotEvent.StaleTime);
		return DateTimeOffset.UtcNow > staleTime;
	}

	/// <summary>
	/// Raises the CotEventReceived event.
	/// </summary>
	private void OnCotEventReceived(CotEvent cotEvent)
	{
		CotEventReceived?.Invoke(this, cotEvent);
	}

	/// <summary>
	/// Raises the TakMessageReceived event.
	/// </summary>
	private void OnTakMessageReceived(TakMessage takMessage)
	{
		TakMessageReceived?.Invoke(this, takMessage);
	}

	/// <summary>
	/// Raises the ErrorOccurred event.
	/// </summary>
	private void OnError(Exception ex)
	{
		ErrorOccurred?.Invoke(this, ex);
	}

	/// <summary>
	/// Disposes the client and releases resources.
	/// </summary>
	public void Dispose()
	{
		// Cancel the operation
		_cancellationTokenSource?.Cancel();

		// Synchronously close and dispose resources
		try
		{
			_udpClient?.Close();
		}
		catch
		{
			// Suppress exceptions during disposal
		}

		_udpClient?.Dispose();
		_udpClient = null;

		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = null;

		ConnectionState = ConnectionState.Disconnected;
	}
}
