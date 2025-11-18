using System.IO;
using System.Net.Sockets;
using Atakmap.Commoncommo.Protobuf.V1;
using CoT.Configuration;
using CoT.Protocol;

namespace CoT.Network;

/// <summary>
/// TCP Stream client for connecting to TAK Server.
/// Handles persistent connections with automatic reconnection.
/// </summary>
public class TakTcpStreamClient : IDisposable
{
	private readonly TakClientConfig _config;
	private readonly TakMessageParser _parser;
	private TcpClient? _tcpClient;
	private NetworkStream? _stream;
	private CancellationTokenSource? _cancellationTokenSource;
	private Task? _receiveTask;
	private Task? _reconnectTask;
	private ConnectionState _connectionState = ConnectionState.Disconnected;
	private bool _isDisposed;

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
	/// Initializes a new instance of the TakTcpStreamClient.
	/// </summary>
	public TakTcpStreamClient(TakClientConfig config)
	{
		_config = config ?? throw new ArgumentNullException(nameof(config));
		_config.Validate();
		_parser = new();
	}

	/// <summary>
	/// Connects to the TAK Server.
	/// </summary>
	public Task ConnectAsync()
	{
		return ConnectAsync(CancellationToken.None);
	}

	/// <summary>
	/// Connects to the TAK Server with a cancellation token.
	/// </summary>
	public async Task ConnectAsync(CancellationToken cancellationToken)
	{
		if (_tcpClient != null && _tcpClient.Connected)
			throw new InvalidOperationException("Client is already connected");

		ConnectionState = ConnectionState.Connecting;

		try
		{
			_tcpClient = new();

			// Set connection timeout
			var connectTask = _tcpClient.ConnectAsync(_config.Host, _config.Port);
			var timeoutTask = Task.Delay(_config.ConnectionTimeout, cancellationToken);

			var completedTask = await Task.WhenAny(connectTask, timeoutTask);

			if (completedTask == timeoutTask)
			{
				_tcpClient?.Close();
				throw new TimeoutException($"Connection to {_config.Host}:{_config.Port} timed out");
			}

			await connectTask; // Ensure any exceptions are thrown

			_stream = _tcpClient.GetStream();

			// Set receive buffer size
			_tcpClient.ReceiveBufferSize = _config.BufferSize;

			// Start receiving
			_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_receiveTask = Task.Run(() => ReceiveLoopAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

			ConnectionState = ConnectionState.Connected;
		}
		catch (Exception ex)
		{
			ConnectionState = ConnectionState.Error;
			OnError(ex);

			// Start auto-reconnect
			if (!_isDisposed && !cancellationToken.IsCancellationRequested)
			{
				_reconnectTask = Task.Run(() => ReconnectLoopAsync(cancellationToken));
			}

			throw;
		}
	}

	/// <summary>
	/// Disconnects from the TAK Server.
	/// </summary>
	public async Task DisconnectAsync()
	{
		// Close the stream first to unblock any pending reads
		_stream?.Close();
		
		if (_cancellationTokenSource != null)
		{
			_cancellationTokenSource.Cancel();

			try
			{
				if (_receiveTask != null)
				{
					// Wait for the receive task with a timeout to prevent hanging
					var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
					var completedTask = await Task.WhenAny(_receiveTask, timeoutTask);
					
					if (completedTask == timeoutTask)
					{
						// Receive task didn't complete in time, but we'll continue cleanup
						OnError(new TimeoutException("Receive task did not complete during disconnect"));
					}
				}
			}
			catch (OperationCanceledException)
			{
				// Expected when cancelling
			}
		}

		_stream?.Dispose();
		_stream = null;

		_tcpClient?.Close();
		_tcpClient?.Dispose();
		_tcpClient = null;

		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = null;

		ConnectionState = ConnectionState.Disconnected;
	}

	/// <summary>
	/// Main receive loop for processing TCP stream messages.
	/// </summary>
	private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && _tcpClient?.Connected == true)
		{
			try
			{
				// Parse TAK protocol header from stream
				Guard.IsNotNull(_stream);
				var header = TakProtocolHeader.ParseFromStream(_stream);

				// Read payload based on header length
				var payload = new byte[header.PayloadLength];
				int totalRead = 0;

				while (totalRead < header.PayloadLength)
				{
					int bytesRead = await _stream.ReadAsync(payload, totalRead, header.PayloadLength - totalRead, cancellationToken);

					if (bytesRead == 0)
						throw new EndOfStreamException("Connection closed by remote host");

					totalRead += bytesRead;
				}

				// Process the message
				ProcessMessage(header, payload);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (ObjectDisposedException)
			{
				break;
			}
			catch (EndOfStreamException)
			{
				// Connection was closed, attempt reconnect
				OnError(new IOException("Connection closed by remote host"));
				if (!_isDisposed)
				{
					ConnectionState = ConnectionState.Reconnecting;
					_reconnectTask = Task.Run(() => ReconnectLoopAsync(CancellationToken.None));
				}
				break;
			}
			catch (IOException ex)
			{
				OnError(ex);
				if (!_isDisposed)
				{
					ConnectionState = ConnectionState.Reconnecting;
					_reconnectTask = Task.Run(() => ReconnectLoopAsync(CancellationToken.None));
				}
				break;
			}
			catch (Exception ex)
			{
				OnError(ex);
			}
		}
	}

	/// <summary>
	/// Automatic reconnection loop.
	/// </summary>
	private async Task ReconnectLoopAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && !_isDisposed)
		{
			try
			{
				ConnectionState = ConnectionState.Reconnecting;

				// Clean up old connection
				_stream?.Dispose();
				_tcpClient?.Close();
				_tcpClient?.Dispose();

				// Wait before reconnecting
				await Task.Delay(_config.ReconnectInterval, cancellationToken);

				// Attempt to reconnect
				await ConnectAsync(CancellationToken.None);

				// If successful, exit reconnect loop
				break;
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				OnError(new Exception("Reconnection failed", ex));
				// Continue trying to reconnect
			}
		}
	}

	/// <summary>
	/// Processes a received message.
	/// </summary>
	private void ProcessMessage(TakProtocolHeader header, byte[] payload)
	{
		try
		{
			// Stream mode always uses protobuf (version 1)
			if (header.Version == 1 || header.IsStreamMode)
			{
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
			else
			{
				OnError(new NotSupportedException($"Unexpected protocol version in stream mode: {header.Version}"));
			}
		}
		catch (Exception ex)
		{
			OnError(ex);
		}
	}

	/// <summary>
	/// Sends a TAK message to the server.
	/// </summary>
	public async Task SendMessageAsync(TakMessage message, CancellationToken cancellationToken = default)
	{
		if (_stream == null || _tcpClient == null || !_tcpClient.Connected)
			throw new InvalidOperationException("Not connected to server");

		try
		{
			var payload = _parser.SerializeToProtobuf(message);

			var header = new TakProtocolHeader
			{
				Version = 1,
				IsStreamMode = true,
				PayloadLength = payload.Length
			};

			var headerBytes = header.ToBytes();

			// Write header and payload
			await _stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
			await _stream.WriteAsync(payload, 0, payload.Length, cancellationToken);
			await _stream.FlushAsync(cancellationToken);
		}
		catch (IOException ex)
		{
			// Connection error during send - trigger reconnection
			OnError(ex);
			if (!_isDisposed)
			{
				ConnectionState = ConnectionState.Reconnecting;
				_reconnectTask = Task.Run(() => ReconnectLoopAsync(CancellationToken.None));
			}
			throw;
		}
		catch (ObjectDisposedException ex)
		{
			// Stream was disposed - likely during disconnect
			OnError(ex);
			throw new InvalidOperationException("Connection was closed", ex);
		}
		catch (OperationCanceledException)
		{
			// Cancellation requested - don't treat as error
			throw;
		}
		catch (Exception ex)
		{
			OnError(ex);
			throw;
		}
	}

	/// <summary>
	/// Sends a CoT event to the server.
	/// </summary>
	public Task SendCotEventAsync(CotEvent cotEvent, CancellationToken cancellationToken = default)
	{
		var takMessage = new TakMessage
		{
			CotEvent = cotEvent,
			CreationTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
		};

		return SendMessageAsync(takMessage, cancellationToken);
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
		if (_isDisposed)
			return;

		_isDisposed = true;

		// Cancel all operations
		_cancellationTokenSource?.Cancel();

		// Synchronously close and dispose resources
		try
		{
			_stream?.Close();
		}
		catch
		{
			// Suppress exceptions during disposal
		}

		_stream?.Dispose();
		_stream = null;

		try
		{
			_tcpClient?.Close();
		}
		catch
		{
			// Suppress exceptions during disposal
		}

		_tcpClient?.Dispose();
		_tcpClient = null;

		_cancellationTokenSource?.Dispose();
		_cancellationTokenSource = null;

		ConnectionState = ConnectionState.Disconnected;
	}
}
