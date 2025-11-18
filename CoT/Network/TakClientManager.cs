using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Atakmap.Commoncommo.Protobuf.V1;
using CoT.Configuration;

namespace CoT.Network;

/// <summary>
/// Manages multiple TAK clients (UDP and TCP) with unified event handling.
/// </summary>
public class TakClientManager : IDisposable
{
	private readonly ConcurrentDictionary<string, TakUdpMeshClient> _udpClients;
	private readonly ConcurrentDictionary<string, TakTcpStreamClient> _tcpClients;
	private bool _isDisposed;

	/// <summary>
	/// Event raised when a CoT event is received from any client.
	/// </summary>
	public event EventHandler<CotEventReceivedEventArgs>? CotEventReceived;

	/// <summary>
	/// Event raised when a TAK message is received from any client.
	/// </summary>
	public event EventHandler<TakMessageReceivedEventArgs>? TakMessageReceived;

	/// <summary>
	/// Event raised when an error occurs in any client.
	/// </summary>
	public event EventHandler<ErrorEventArgs>? ErrorOccurred;

	/// <summary>
	/// Event raised when a client's connection state changes.
	/// </summary>
	public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

	/// <summary>
	/// Initializes a new instance of the TakClientManager.
	/// </summary>
	public TakClientManager()
	{
		_udpClients = new();
		_tcpClients = new();
	}

	/// <summary>
	/// Adds a UDP mesh client with the specified identifier and configuration.
	/// </summary>
	public void AddUdpClient(string clientId, TakClientConfig config)
	{
		if (string.IsNullOrWhiteSpace(clientId))
			throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));

		if (_udpClients.ContainsKey(clientId))
			throw new InvalidOperationException($"UDP client with ID '{clientId}' already exists");

		var client = new TakUdpMeshClient(config);
		AttachUdpEventHandlers(client, clientId);

		_udpClients[clientId] = client;
	}

	/// <summary>
	/// Adds a TCP stream client with the specified identifier and configuration.
	/// </summary>
	public void AddTcpClient(string clientId, TakClientConfig config)
	{
		if (string.IsNullOrWhiteSpace(clientId))
			throw new ArgumentException("Client ID cannot be null or empty", nameof(clientId));

		if (_tcpClients.ContainsKey(clientId))
			throw new InvalidOperationException($"TCP client with ID '{clientId}' already exists");

		var client = new TakTcpStreamClient(config);
		AttachTcpEventHandlers(client, clientId);

		_tcpClients[clientId] = client;
	}

	/// <summary>
	/// Starts a specific UDP client.
	/// </summary>
	public Task StartUdpClientAsync(string clientId, CancellationToken cancellationToken = default)
	{
		if (!_udpClients.TryGetValue(clientId, out var client))
			throw new InvalidOperationException($"UDP client with ID '{clientId}' not found");

		return client.StartAsync(cancellationToken);
	}

	/// <summary>
	/// Connects a specific TCP client.
	/// </summary>
	public Task ConnectTcpClientAsync(string clientId, CancellationToken cancellationToken = default)
	{
		if (!_tcpClients.TryGetValue(clientId, out var client))
			throw new InvalidOperationException($"TCP client with ID '{clientId}' not found");

		return client.ConnectAsync(cancellationToken);
	}

	/// <summary>
	/// Starts all UDP clients.
	/// </summary>
	public async Task StartAllUdpClientsAsync(CancellationToken cancellationToken = default)
	{
		var tasks = _udpClients.Values.Select(c => c.StartAsync(cancellationToken));
		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Connects all TCP clients.
	/// </summary>
	public async Task ConnectAllTcpClientsAsync(CancellationToken cancellationToken = default)
	{
		var tasks = _tcpClients.Values.Select(c => c.ConnectAsync(cancellationToken));
		await Task.WhenAll(tasks);
	}

	/// <summary>
	/// Starts all clients (both UDP and TCP).
	/// </summary>
	public async Task StartAllClientsAsync(CancellationToken cancellationToken = default)
	{
		await Task.WhenAll(
			StartAllUdpClientsAsync(cancellationToken),
			ConnectAllTcpClientsAsync(cancellationToken)
		);
	}

	/// <summary>
	/// Stops a specific UDP client.
	/// </summary>
	public Task StopUdpClientAsync(string clientId)
	{
		if (!_udpClients.TryGetValue(clientId, out var client))
			throw new InvalidOperationException($"UDP client with ID '{clientId}' not found");

		return client.StopAsync();
	}

	/// <summary>
	/// Disconnects a specific TCP client.
	/// </summary>
	public Task DisconnectTcpClientAsync(string clientId)
	{
		if (!_tcpClients.TryGetValue(clientId, out var client))
			throw new InvalidOperationException($"TCP client with ID '{clientId}' not found");

		return client.DisconnectAsync();
	}

	/// <summary>
	/// Stops all clients.
	/// </summary>
	public async Task StopAllClientsAsync()
	{
		var udpTasks = _udpClients.Values.Select(c => c.StopAsync());
		var tcpTasks = _tcpClients.Values.Select(c => c.DisconnectAsync());

		await Task.WhenAll(udpTasks.Concat(tcpTasks));
	}

	/// <summary>
	/// Removes a UDP client.
	/// </summary>
	public async Task RemoveUdpClientAsync(string clientId)
	{
		if (_udpClients.TryRemove(clientId, out var client))
		{
			await client.StopAsync();
			client.Dispose();
		}
	}

	/// <summary>
	/// Removes a TCP client.
	/// </summary>
	public async Task RemoveTcpClientAsync(string clientId)
	{
		if (_tcpClients.TryRemove(clientId, out var client))
		{
			await client.DisconnectAsync();
			client.Dispose();
		}
	}

	/// <summary>
	/// Gets a UDP client by ID.
	/// </summary>
	public TakUdpMeshClient? GetUdpClient(string clientId)
	{
		_udpClients.TryGetValue(clientId, out var client);
		return client;
	}

	/// <summary>
	/// Gets a TCP client by ID.
	/// </summary>
	public TakTcpStreamClient? GetTcpClient(string clientId)
	{
		_tcpClients.TryGetValue(clientId, out var client);
		return client;
	}

	/// <summary>
	/// Gets all UDP client IDs.
	/// </summary>
	public IEnumerable<string> GetUdpClientIds()
	{
		return [.. _udpClients.Keys];
	}

	/// <summary>
	/// Gets all TCP client IDs.
	/// </summary>
	public IEnumerable<string> GetTcpClientIds()
	{
		return [.. _tcpClients.Keys];
	}

	/// <summary>
	/// Sends a CoT event to a specific TCP client.
	/// </summary>
	public Task SendCotEventAsync(string clientId, CotEvent cotEvent, CancellationToken cancellationToken = default)
	{
		if (!_tcpClients.TryGetValue(clientId, out var client))
			throw new InvalidOperationException($"TCP client with ID '{clientId}' not found");

		return client.SendCotEventAsync(cotEvent, cancellationToken);
	}

	/// <summary>
	/// Sends a TAK message to a specific TCP client.
	/// </summary>
	public Task SendTakMessageAsync(string clientId, TakMessage message, CancellationToken cancellationToken = default)
	{
		if (!_tcpClients.TryGetValue(clientId, out var client))
			throw new InvalidOperationException($"TCP client with ID '{clientId}' not found");

		return client.SendMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Attaches event handlers to a UDP client.
	/// </summary>
	private void AttachUdpEventHandlers(TakUdpMeshClient client, string clientId)
	{
		client.CotEventReceived += (sender, cotEvent) =>
		{
			CotEventReceived?.Invoke(this, new CotEventReceivedEventArgs(clientId, cotEvent, TakProtocol.UDP_Mesh));
		};

		client.TakMessageReceived += (sender, takMessage) =>
		{
			TakMessageReceived?.Invoke(this, new TakMessageReceivedEventArgs(clientId, takMessage, TakProtocol.UDP_Mesh));
		};

		client.ErrorOccurred += (sender, ex) =>
		{
			ErrorOccurred?.Invoke(this, new ErrorEventArgs(clientId, ex, TakProtocol.UDP_Mesh));
		};

		client.ConnectionStateChanged += (sender, state) =>
		{
			ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(clientId, state, TakProtocol.UDP_Mesh));
		};
	}

	/// <summary>
	/// Attaches event handlers to a TCP client.
	/// </summary>
	private void AttachTcpEventHandlers(TakTcpStreamClient client, string clientId)
	{
		client.CotEventReceived += (sender, cotEvent) =>
		{
			CotEventReceived?.Invoke(this, new CotEventReceivedEventArgs(clientId, cotEvent, TakProtocol.TCP_Stream));
		};

		client.TakMessageReceived += (sender, takMessage) =>
		{
			TakMessageReceived?.Invoke(this, new TakMessageReceivedEventArgs(clientId, takMessage, TakProtocol.TCP_Stream));
		};

		client.ErrorOccurred += (sender, ex) =>
		{
			ErrorOccurred?.Invoke(this, new ErrorEventArgs(clientId, ex, TakProtocol.TCP_Stream));
		};

		client.ConnectionStateChanged += (sender, state) =>
		{
			ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(clientId, state, TakProtocol.TCP_Stream));
		};
	}

	/// <summary>
	/// Disposes all clients and releases resources.
	/// </summary>
	public void Dispose()
	{
		if (_isDisposed)
			return;

		_isDisposed = true;

		StopAllClientsAsync().GetAwaiter().GetResult();

		foreach (var client in _udpClients.Values)
		{
			client.Dispose();
		}

		foreach (var client in _tcpClients.Values)
		{
			client.Dispose();
		}

		_udpClients.Clear();
		_tcpClients.Clear();
	}
}

/// <summary>
/// Event args for CoT events received from a client.
/// </summary>
public class CotEventReceivedEventArgs(string clientId, CotEvent cotEvent, TakProtocol protocol) : EventArgs
{
	public string ClientId { get; init; } = clientId;
	public CotEvent CotEvent { get; init; } = cotEvent;
	public TakProtocol Protocol { get; init; } = protocol;
}

/// <summary>
/// Event args for TAK messages received from a client.
/// </summary>
public class TakMessageReceivedEventArgs(string clientId, TakMessage takMessage, TakProtocol protocol) : EventArgs
{
	public string ClientId { get; init; } = clientId;
	public TakMessage TakMessage { get; init; } = takMessage;
	public TakProtocol Protocol { get; init; } = protocol;
}

/// <summary>
/// Event args for errors from a client.
/// </summary>
public class ErrorEventArgs(string clientId, Exception exception, TakProtocol protocol) : EventArgs
{
	public string ClientId { get; init; } = clientId;
	public Exception Exception { get; init; } = exception;
	public TakProtocol Protocol { get; init; } = protocol;
}

/// <summary>
/// Event args for connection state changes.
/// </summary>
public class ConnectionStateChangedEventArgs(string clientId, ConnectionState state, TakProtocol protocol) : EventArgs
{
	public string ClientId { get; init; } = clientId;
	public ConnectionState State { get; init; } = state;
	public TakProtocol Protocol { get; init; } = protocol;
}
