using CoT.Configuration;
using CoT.Network;
using CoT.Test.TestInfrastructure;
using Atakmap.Commoncommo.Protobuf.V1;

namespace CoT.Test.Network;

[TestClass]
public class TakTcpStreamClientTests
{
	private SimulatedTakServer _server = null!;
	private TakTcpStreamClient _client = null!;

	[TestInitialize]
	public void Setup()
	{
		_server = new SimulatedTakServer();
		_server.Start();
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		_client?.Dispose();
		if (_server != null)
			await _server.Stop();
		_server?.Dispose();
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ConnectAsync_ValidServer_ConnectsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);

		// Act
		await _client.ConnectAsync();

		// Assert
		Assert.AreEqual(ConnectionState.Connected, _client.ConnectionState);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task SendMessageAsync_ValidMessage_ServerReceives()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);
		await _client.ConnectAsync();

		var messageReceived = new ManualResetEventSlim(false);
		TakMessage? receivedMessage = null;

		_server.MessageReceived += (sender, message) =>
		{
			receivedMessage = message;
			messageReceived.Set();
		};

		// Act
		var testMessage = TestDataFactory.CreateTestTakMessage();
		await _client.SendMessageAsync(testMessage);

		// Assert
		Assert.IsTrue(messageReceived.Wait(TimeSpan.FromSeconds(5)), "Server did not receive message");
		Assert.IsNotNull(receivedMessage);
		Assert.AreEqual(testMessage.CotEvent.Uid, receivedMessage.CotEvent.Uid);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task SendCotEventAsync_ValidEvent_ServerReceives()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);
		await _client.ConnectAsync();

		var messageReceived = new ManualResetEventSlim(false);
		TakMessage? receivedMessage = null;

		_server.MessageReceived += (sender, message) =>
		{
			receivedMessage = message;
			messageReceived.Set();
		};

		// Act
		var testEvent = TestDataFactory.CreateTestCotEvent(uid: "TCP-TEST-001");
		await _client.SendCotEventAsync(testEvent);

		// Assert
		Assert.IsTrue(messageReceived.Wait(TimeSpan.FromSeconds(5)), "Server did not receive event");
		Assert.IsNotNull(receivedMessage);
		Assert.IsNotNull(receivedMessage.CotEvent);
		Assert.AreEqual("TCP-TEST-001", receivedMessage.CotEvent.Uid);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ReceiveMessage_FromServer_RaisesEvent()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);

		CotEvent? receivedEvent = null;
		var eventReceived = new ManualResetEventSlim(false);

		_client.CotEventReceived += (sender, cotEvent) =>
		{
			receivedEvent = cotEvent;
			eventReceived.Set();
		};

		await _client.ConnectAsync();

		// Act
		var testMessage = TestDataFactory.CreateTestTakMessage(
			TestDataFactory.CreateTestCotEvent(uid: "SERVER-SENT-001"));
		await _server.SendMessageAsync(testMessage);

		// Assert
		Assert.IsTrue(eventReceived.Wait(TimeSpan.FromSeconds(5)), "Client did not receive event");
		Assert.IsNotNull(receivedEvent);
		Assert.AreEqual("SERVER-SENT-001", receivedEvent.Uid);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ReceiveMessage_StaleWithFilter_NotRaised()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream,
			FilterStaleMessages = true
		};
		_client = new TakTcpStreamClient(config);

		var eventReceived = new ManualResetEventSlim(false);
		_client.CotEventReceived += (sender, cotEvent) => eventReceived.Set();

		await _client.ConnectAsync();

		// Act
		var staleMessage = TestDataFactory.CreateTestTakMessage(
			TestDataFactory.CreateStaleCotEvent());
		await _server.SendMessageAsync(staleMessage);

		// Assert
		Assert.IsFalse(eventReceived.Wait(TimeSpan.FromSeconds(2)), "Stale event should not be received");
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task DisconnectAsync_WhileConnected_DisconnectsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);
		await _client.ConnectAsync();

		// Act
		await _client.DisconnectAsync();

		// Assert
		Assert.AreEqual(ConnectionState.Disconnected, _client.ConnectionState);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ConnectionStateChanged_EventsRaised()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);

		var states = new List<ConnectionState>();
		var stateChanged = new ManualResetEventSlim(false);
		var expectedStateCount = 3;

		_client.ConnectionStateChanged += (sender, state) =>
		{
			lock (states)
			{
				states.Add(state);
				if (states.Count >= expectedStateCount)
					stateChanged.Set();
			}
		};

		// Act
		await _client.ConnectAsync();
		await _client.DisconnectAsync();

		// Assert
		Assert.IsTrue(stateChanged.Wait(TimeSpan.FromSeconds(5)), "Did not receive all expected state changes");
		Assert.IsTrue(states.Contains(ConnectionState.Connecting));
		Assert.IsTrue(states.Contains(ConnectionState.Connected));
		Assert.IsTrue(states.Contains(ConnectionState.Disconnected));
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ConnectAsync_AlreadyConnected_ThrowsException()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);
		await _client.ConnectAsync();

		// Act & Assert
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.ConnectAsync());
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ConnectAsync_InvalidServer_RaisesError()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = 9999, // Invalid port
			Protocol = TakProtocol.TCP_Stream,
			ConnectionTimeout = TimeSpan.FromSeconds(2)
		};
		_client = new TakTcpStreamClient(config);

		Exception? receivedException = null;
		var errorReceived = new ManualResetEventSlim(false);

		_client.ErrorOccurred += (sender, ex) =>
		{
			receivedException = ex;
			errorReceived.Set();
		};

		// Act
		try
		{
			await _client.ConnectAsync();
		}
		catch
		{
			// Expected
		}

		// Assert - Error event should be raised
		Assert.IsTrue(errorReceived.Wait(TimeSpan.FromSeconds(5)), "Error event not raised");
		Assert.IsNotNull(receivedException);
	}

	[TestMethod]
	[Timeout(15000)]
	public async Task SendMultipleMessages_AllReceived()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);
		await _client.ConnectAsync();

		var countdown = new CountdownEvent(3);

		_server.MessageReceived += (sender, message) => countdown.Signal();

		// Act
		for (int i = 0; i < 3; i++)
		{
			var testEvent = TestDataFactory.CreateTestCotEvent(uid: $"MULTI-TCP-{i}");
			await _client.SendCotEventAsync(testEvent);
		}

		// Assert
		Assert.IsTrue(countdown.Wait(TimeSpan.FromSeconds(10)), "Not all messages received by server");
		Assert.AreEqual(3, _server.ReceivedMessages.Count);
	}

	[TestMethod]
	[Timeout(15000)]
	public async Task ReceiveMultipleMessages_AllReceived()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);

		var receivedEvents = new List<CotEvent>();
		var countdown = new CountdownEvent(3);

		_client.CotEventReceived += (sender, cotEvent) =>
		{
			lock (receivedEvents)
			{
				receivedEvents.Add(cotEvent);
			}
			countdown.Signal();
		};

		await _client.ConnectAsync();

		// Act
		for (int i = 0; i < 3; i++)
		{
			var testMessage = TestDataFactory.CreateTestTakMessage(
				TestDataFactory.CreateTestCotEvent(uid: $"SERVER-MULTI-{i}"));
			await _server.SendMessageAsync(testMessage);
			await Task.Delay(50);
		}

		// Assert
		Assert.IsTrue(countdown.Wait(TimeSpan.FromSeconds(10)), "Not all messages received by client");
		Assert.AreEqual(3, receivedEvents.Count);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task SendMessageAsync_NotConnected_ThrowsException()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_client = new TakTcpStreamClient(config);

		// Act & Assert
		var testMessage = TestDataFactory.CreateTestTakMessage();
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.SendMessageAsync(testMessage));
	}
}
