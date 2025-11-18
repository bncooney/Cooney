using System.Net;
using System.Net.Sockets;
using Atakmap.Commoncommo.Protobuf.V1;
using CoT.Protocol;

namespace CoT.Test.TestInfrastructure;

/// <summary>
/// Simulated TAK server for testing TCP stream clients.
/// </summary>
public class SimulatedTakServer : IDisposable
{
	private readonly TcpListener _listener;
	private readonly List<TcpClient> _clients = [];
	private readonly TakMessageParser _parser = new();
	private CancellationTokenSource? _cancellationTokenSource;
	private Task? _acceptTask;
	private bool _isRunning;

	public int Port { get; }
	public List<TakMessage> ReceivedMessages { get; } = [];
	public event EventHandler<TakMessage>? MessageReceived;

	public SimulatedTakServer(int port = 0)
	{
		_listener = new TcpListener(IPAddress.Loopback, port);
		_listener.Start();
		Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
	}

	public void Start()
	{
		if (_isRunning)
			return;

		_isRunning = true;
		_cancellationTokenSource = new CancellationTokenSource();
		_acceptTask = Task.Run(() => AcceptClientsAsync(_cancellationTokenSource.Token));
	}

	public async Task Stop()
	{
		_isRunning = false;
		_cancellationTokenSource?.Cancel();

		if (_acceptTask != null)
		{
			try
			{
				await _acceptTask;
			}
			catch (OperationCanceledException)
			{
				// Expected
			}
		}

		foreach (var client in _clients)
		{
			client.Close();
			client.Dispose();
		}
		_clients.Clear();

		_listener.Stop();
	}

	private async Task AcceptClientsAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && _isRunning)
		{
			try
			{
				var client = await _listener.AcceptTcpClientAsync(cancellationToken);
				_clients.Add(client);
				_ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch
			{
				// Continue accepting
			}
		}
	}

	private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
	{
		var stream = client.GetStream();

		while (!cancellationToken.IsCancellationRequested && client.Connected)
		{
			try
			{
				var header = TakProtocolHeader.ParseFromStream(stream);
				var payload = new byte[header.PayloadLength];
				int totalRead = 0;

				while (totalRead < header.PayloadLength)
				{
					int bytesRead = await stream.ReadAsync(payload.AsMemory(totalRead, header.PayloadLength - totalRead), cancellationToken);
					if (bytesRead == 0)
						break;
					totalRead += bytesRead;
				}

				var message = _parser.ParseProtobuf(payload);
				ReceivedMessages.Add(message);
				MessageReceived?.Invoke(this, message);
			}
			catch
			{
				break;
			}
		}
	}

	public async Task SendMessageAsync(TakMessage message)
	{
		var payload = _parser.SerializeToProtobuf(message);
		var header = new TakProtocolHeader
		{
			Version = 1,
			IsStreamMode = true,
			PayloadLength = payload.Length
		};

		var headerBytes = header.ToBytes();

		foreach (var client in _clients.ToList())
		{
			if (client.Connected)
			{
				try
				{
					var stream = client.GetStream();
					await stream.WriteAsync(headerBytes);
					await stream.WriteAsync(payload);
					await stream.FlushAsync();
				}
				catch
				{
					// Client disconnected
				}
			}
		}
	}

	public void Dispose()
	{
		Stop().GetAwaiter().GetResult();
		_listener.Dispose();
		_cancellationTokenSource?.Dispose();
	}
}
