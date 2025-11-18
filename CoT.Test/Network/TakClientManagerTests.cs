using CoT.Configuration;
using CoT.Network;
using CoT.Test.TestInfrastructure;
using Atakmap.Commoncommo.Protobuf.V1;

namespace CoT.Test.Network;

[TestClass]
public class TakClientManagerTests
{
	private TakClientManager _manager = null!;
	private SimulatedTakServer _server = null!;
	private SimulatedUdpMulticast _multicastSender = null!;

	[TestInitialize]
	public void Setup()
	{
		_manager = new TakClientManager();
		_server = new SimulatedTakServer();
		_server.Start();
		_multicastSender = new SimulatedUdpMulticast();
	}

	[TestCleanup]
	public async Task Cleanup()
	{
		_manager?.Dispose();
		if (_server != null)
			await _server.Stop();
		_server?.Dispose();
		_multicastSender?.Dispose();
	}

	[TestMethod]
	public void AddUdpClient_ValidConfig_AddsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Protocol = TakProtocol.UDP_Mesh
		};

		// Act
		_manager.AddUdpClient("udp-1", config);

		// Assert
		var client = _manager.GetUdpClient("udp-1");
		Assert.IsNotNull(client);
	}

	[TestMethod]
	public void AddTcpClient_ValidConfig_AddsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};

		// Act
		_manager.AddTcpClient("tcp-1", config);

		// Assert
		var client = _manager.GetTcpClient("tcp-1");
		Assert.IsNotNull(client);
	}

	[TestMethod]
	public void AddUdpClient_DuplicateId_ThrowsException()
	{
		// Arrange
		var config = new TakClientConfig();
		_manager.AddUdpClient("udp-1", config);

		// Act & Assert
		var ex = Assert.Throws<InvalidOperationException>(() => _manager.AddUdpClient("udp-1", config));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task StartUdpClientAsync_ValidClient_StartsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig();
		_manager.AddUdpClient("udp-1", config);

		// Act
		await _manager.StartUdpClientAsync("udp-1", TestContext.CancellationToken);

		// Assert
		var client = _manager.GetUdpClient("udp-1");
		Assert.AreEqual(ConnectionState.Connected, client!.ConnectionState);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ConnectTcpClientAsync_ValidClient_ConnectsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_manager.AddTcpClient("tcp-1", config);

		// Act
		await _manager.ConnectTcpClientAsync("tcp-1", TestContext.CancellationToken);

		// Assert
		var client = _manager.GetTcpClient("tcp-1");
		Assert.AreEqual(ConnectionState.Connected, client!.ConnectionState);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CotEventReceived_FromUdpClient_RaisesManagerEvent()
	{
		// Arrange
		var config = new TakClientConfig();
		_manager.AddUdpClient("udp-1", config);

		CotEventReceivedEventArgs? receivedArgs = null;
		var eventReceived = new ManualResetEventSlim(false);

		_manager.CotEventReceived += (sender, args) =>
		{
			receivedArgs = args;
			eventReceived.Set();
		};

		await _manager.StartUdpClientAsync("udp-1", TestContext.CancellationToken);

		// Act
		var testEvent = TestDataFactory.CreateTestCotEvent(uid: "MGR-UDP-001");
		await _multicastSender.SendCotEventAsync(testEvent, useProtobuf: true);

		// Assert
		Assert.IsTrue(eventReceived.Wait(TimeSpan.FromSeconds(5), TestContext.CancellationToken), "Event not received");
		Assert.IsNotNull(receivedArgs);
		Assert.AreEqual("udp-1", receivedArgs.ClientId);
		Assert.AreEqual(TakProtocol.UDP_Mesh, receivedArgs.Protocol);
		Assert.AreEqual("MGR-UDP-001", receivedArgs.CotEvent.Uid);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task CotEventReceived_FromTcpClient_RaisesManagerEvent()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_manager.AddTcpClient("tcp-1", config);

		CotEventReceivedEventArgs? receivedArgs = null;
		var eventReceived = new ManualResetEventSlim(false);

		_manager.CotEventReceived += (sender, args) =>
		{
			receivedArgs = args;
			eventReceived.Set();
		};

		await _manager.ConnectTcpClientAsync("tcp-1", TestContext.CancellationToken);

		// Act
		var testMessage = TestDataFactory.CreateTestTakMessage(
			TestDataFactory.CreateTestCotEvent(uid: "MGR-TCP-001"));
		await _server.SendMessageAsync(testMessage);

		// Assert
		Assert.IsTrue(eventReceived.Wait(TimeSpan.FromSeconds(5), TestContext.CancellationToken), "Event not received");
		Assert.IsNotNull(receivedArgs);
		Assert.AreEqual("tcp-1", receivedArgs.ClientId);
		Assert.AreEqual(TakProtocol.TCP_Stream, receivedArgs.Protocol);
		Assert.AreEqual("MGR-TCP-001", receivedArgs.CotEvent.Uid);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task SendCotEventAsync_ValidClient_ServerReceives()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = _server.Port,
			Protocol = TakProtocol.TCP_Stream
		};
		_manager.AddTcpClient("tcp-1", config);
		
		Exception? clientError = null;
		_manager.ErrorOccurred += (sender, args) =>
		{
			clientError = args.Exception;
		};
		
		await _manager.ConnectTcpClientAsync("tcp-1", TestContext.CancellationToken);

		TakMessage? receivedMessage = null;
		var messageReceived = new ManualResetEventSlim(false);

		_server.MessageReceived += (sender, message) =>
		{
			receivedMessage = message;
			messageReceived.Set();
		};

		// Give the server's receive loop time to start
		await Task.Delay(100, TestContext.CancellationToken);

		// Act
		var testEvent = TestDataFactory.CreateTestCotEvent(uid: "SEND-TEST-001");
		await _manager.SendCotEventAsync("tcp-1", testEvent, TestContext.CancellationToken);

		// Assert
		if (clientError != null)
		{
			Assert.Fail($"Client error occurred: {clientError.Message}\n{clientError.StackTrace}");
		}
		
		Assert.IsTrue(messageReceived.Wait(TimeSpan.FromSeconds(5), TestContext.CancellationToken), "Server did not receive message");
		Assert.IsNotNull(receivedMessage);
		Assert.AreEqual("SEND-TEST-001", receivedMessage.CotEvent.Uid);
	}

	[TestMethod]
	[Timeout(15000, CooperativeCancellation = true)]
	public async Task StartAllUdpClientsAsync_MultipleClients_AllStart()
	{
		// Arrange
		_manager.AddUdpClient("udp-1", new TakClientConfig());
		_manager.AddUdpClient("udp-2", new TakClientConfig());
		_manager.AddUdpClient("udp-3", new TakClientConfig());

		// Act
		await _manager.StartAllUdpClientsAsync(TestContext.CancellationToken);

		// Assert
		Assert.AreEqual(ConnectionState.Connected, _manager.GetUdpClient("udp-1")!.ConnectionState);
		Assert.AreEqual(ConnectionState.Connected, _manager.GetUdpClient("udp-2")!.ConnectionState);
		Assert.AreEqual(ConnectionState.Connected, _manager.GetUdpClient("udp-3")!.ConnectionState);
	}

	[TestMethod]
	[Timeout(15000, CooperativeCancellation = true)]
	public async Task ConnectAllTcpClientsAsync_MultipleClients_AllConnect()
	{
		// Arrange
		var server2 = new SimulatedTakServer();
		var server3 = new SimulatedTakServer();
		server2.Start();
		server3.Start();

		try
		{
			_manager.AddTcpClient("tcp-1", new TakClientConfig { Host = "127.0.0.1", Port = _server.Port });
			_manager.AddTcpClient("tcp-2", new TakClientConfig { Host = "127.0.0.1", Port = server2.Port });
			_manager.AddTcpClient("tcp-3", new TakClientConfig { Host = "127.0.0.1", Port = server3.Port });

			// Act
			await _manager.ConnectAllTcpClientsAsync(TestContext.CancellationToken);

			// Assert
			Assert.AreEqual(ConnectionState.Connected, _manager.GetTcpClient("tcp-1")!.ConnectionState);
			Assert.AreEqual(ConnectionState.Connected, _manager.GetTcpClient("tcp-2")!.ConnectionState);
			Assert.AreEqual(ConnectionState.Connected, _manager.GetTcpClient("tcp-3")!.ConnectionState);
		}
		finally
		{
			await server2.Stop();
			await server3.Stop();
			server2.Dispose();
			server3.Dispose();
		}
	}

	[TestMethod]
	[Timeout(15000, CooperativeCancellation = true)]
	public async Task StopAllClientsAsync_AllClients_AllStop()
	{
		// Arrange
		_manager.AddUdpClient("udp-1", new TakClientConfig());
		_manager.AddTcpClient("tcp-1", new TakClientConfig { Host = "127.0.0.1", Port = _server.Port });

		await _manager.StartUdpClientAsync("udp-1", TestContext.CancellationToken);
		await _manager.ConnectTcpClientAsync("tcp-1", TestContext.CancellationToken);

		// Act
		await _manager.StopAllClientsAsync();

		// Assert
		Assert.AreEqual(ConnectionState.Disconnected, _manager.GetUdpClient("udp-1")!.ConnectionState);
		Assert.AreEqual(ConnectionState.Disconnected, _manager.GetTcpClient("tcp-1")!.ConnectionState);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task RemoveUdpClientAsync_ExistingClient_RemovesSuccessfully()
	{
		// Arrange
		_manager.AddUdpClient("udp-1", new TakClientConfig());
		await _manager.StartUdpClientAsync("udp-1", TestContext.CancellationToken);

		// Act
		await _manager.RemoveUdpClientAsync("udp-1");

		// Assert
		var client = _manager.GetUdpClient("udp-1");
		Assert.IsNull(client);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task RemoveTcpClientAsync_ExistingClient_RemovesSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig { Host = "127.0.0.1", Port = _server.Port };
		_manager.AddTcpClient("tcp-1", config);
		await _manager.ConnectTcpClientAsync("tcp-1", TestContext.CancellationToken);

		// Act
		await _manager.RemoveTcpClientAsync("tcp-1");

		// Assert
		var client = _manager.GetTcpClient("tcp-1");
		Assert.IsNull(client);
	}

	[TestMethod]
	public void GetUdpClientIds_MultipleClients_ReturnsAllIds()
	{
		// Arrange
		_manager.AddUdpClient("udp-1", new TakClientConfig());
		_manager.AddUdpClient("udp-2", new TakClientConfig());

		// Act
		var ids = _manager.GetUdpClientIds().ToList();

		// Assert
		Assert.HasCount(2, ids);
		Assert.Contains("udp-1", ids);
		Assert.Contains("udp-2", ids);
	}

	[TestMethod]
	public void GetTcpClientIds_MultipleClients_ReturnsAllIds()
	{
		// Arrange
		_manager.AddTcpClient("tcp-1", new TakClientConfig { Host = "127.0.0.1", Port = _server.Port });
		_manager.AddTcpClient("tcp-2", new TakClientConfig { Host = "127.0.0.1", Port = _server.Port });

		// Act
		var ids = _manager.GetTcpClientIds().ToList();

		// Assert
		Assert.HasCount(2, ids);
		Assert.Contains("tcp-1", ids);
		Assert.Contains("tcp-2", ids);
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ConnectionStateChanged_MultipleClients_IdentifiesSource()
	{
		// Arrange
		_manager.AddUdpClient("udp-1", new TakClientConfig());
		_manager.AddTcpClient("tcp-1", new TakClientConfig { Host = "127.0.0.1", Port = _server.Port });

		var stateChanges = new List<(string ClientId, ConnectionState State)>();
		var stateChanged = new ManualResetEventSlim(false);

		_manager.ConnectionStateChanged += (sender, args) =>
		{
			lock (stateChanges)
			{
				stateChanges.Add((args.ClientId, args.State));
				// Wait for both clients to be connected
				if (stateChanges.Any(sc => sc.ClientId == "udp-1" && sc.State == ConnectionState.Connected) &&
					stateChanges.Any(sc => sc.ClientId == "tcp-1" && sc.State == ConnectionState.Connected))
				{
					stateChanged.Set();
				}
			}
		};

		// Act
		await _manager.StartUdpClientAsync("udp-1", TestContext.CancellationToken);
		await _manager.ConnectTcpClientAsync("tcp-1", TestContext.CancellationToken);

		// Assert
		Assert.IsTrue(stateChanged.Wait(TimeSpan.FromSeconds(5), TestContext.CancellationToken), "Did not receive all expected state changes");
		Assert.IsTrue(stateChanges.Any(sc => sc.ClientId == "udp-1" && sc.State == ConnectionState.Connected));
		Assert.IsTrue(stateChanges.Any(sc => sc.ClientId == "tcp-1" && sc.State == ConnectionState.Connected));
	}

	[TestMethod]
	[Timeout(10000, CooperativeCancellation = true)]
	public async Task ErrorOccurred_FromClient_RaisesManagerEvent()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "127.0.0.1",
			Port = 9999, // Invalid port
			Protocol = TakProtocol.TCP_Stream,
			ConnectionTimeout = TimeSpan.FromSeconds(2)
		};
		_manager.AddTcpClient("tcp-error", config);

		CoT.Network.ErrorEventArgs? errorArgs = null;
		var errorReceived = new ManualResetEventSlim(false);

		_manager.ErrorOccurred += (sender, args) =>
		{
			errorArgs = args;
			errorReceived.Set();
		};

		// Act
		try
		{
			await _manager.ConnectTcpClientAsync("tcp-error", TestContext.CancellationToken);
		}
		catch
		{
			// Expected
		}

		// Assert
		Assert.IsTrue(errorReceived.Wait(TimeSpan.FromSeconds(5), TestContext.CancellationToken), "Error event not raised");
		Assert.IsNotNull(errorArgs);
		Assert.AreEqual("tcp-error", errorArgs.ClientId);
	}

	public TestContext TestContext { get; set; }
}
