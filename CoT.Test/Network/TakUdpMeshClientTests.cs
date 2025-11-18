using CoT.Configuration;
using CoT.Network;
using CoT.Test.TestInfrastructure;
using Atakmap.Commoncommo.Protobuf.V1;

namespace CoT.Test.Network;

[TestClass]
public class TakUdpMeshClientTests
{
	private SimulatedUdpMulticast _multicastSender = null!;
	private TakUdpMeshClient _client = null!;

	[TestInitialize]
	public void Setup()
	{
		_multicastSender = new SimulatedUdpMulticast();
	}

	[TestCleanup]
	public void Cleanup()
	{
		_client?.Dispose();
		_multicastSender?.Dispose();
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task StartAsync_ValidConfig_ConnectsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig
		{
			Host = "239.2.3.1",
			Port = 6969,
			Protocol = TakProtocol.UDP_Mesh
		};
		_client = new TakUdpMeshClient(config);

		// Act
		await _client.StartAsync();

		// Assert
		Assert.AreEqual(ConnectionState.Connected, _client.ConnectionState);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ReceiveMessage_ProtobufFormat_RaisesEvent()
	{
		// Arrange
		var config = new TakClientConfig();
		_client = new TakUdpMeshClient(config);

		CotEvent? receivedEvent = null;
		var eventReceived = new ManualResetEventSlim(false);

		_client.CotEventReceived += (sender, cotEvent) =>
		{
			receivedEvent = cotEvent;
			eventReceived.Set();
		};

		await _client.StartAsync();

		// Act
		var testEvent = TestDataFactory.CreateTestCotEvent(uid: "UDP-TEST-001");
		await _multicastSender.SendCotEventAsync(testEvent, useProtobuf: true);

		// Assert
		Assert.IsTrue(eventReceived.Wait(TimeSpan.FromSeconds(5)), "Event not received within timeout");
		Assert.IsNotNull(receivedEvent);
		Assert.AreEqual("UDP-TEST-001", receivedEvent.Uid);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ReceiveMessage_XmlFormat_RaisesEvent()
	{
		// Arrange
		var config = new TakClientConfig
		{
			SupportXmlFallback = true
		};
		_client = new TakUdpMeshClient(config);

		CotEvent? receivedEvent = null;
		var eventReceived = new ManualResetEventSlim(false);

		_client.CotEventReceived += (sender, cotEvent) =>
		{
			receivedEvent = cotEvent;
			eventReceived.Set();
		};

		await _client.StartAsync();

		// Act
		var testEvent = TestDataFactory.CreateTestCotEvent(uid: "XML-TEST-001");
		await _multicastSender.SendCotEventAsync(testEvent, useProtobuf: false);

		// Assert
		Assert.IsTrue(eventReceived.Wait(TimeSpan.FromSeconds(5)), "Event not received within timeout");
		Assert.IsNotNull(receivedEvent);
		Assert.AreEqual("XML-TEST-001", receivedEvent.Uid);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ReceiveMessage_StaleMessageWithFilter_NotRaised()
	{
		// Arrange
		var config = new TakClientConfig
		{
			FilterStaleMessages = true
		};
		_client = new TakUdpMeshClient(config);

		var eventReceived = new ManualResetEventSlim(false);
		_client.CotEventReceived += (sender, cotEvent) => eventReceived.Set();

		await _client.StartAsync();

		// Act
		var staleEvent = TestDataFactory.CreateStaleCotEvent();
		await _multicastSender.SendCotEventAsync(staleEvent, useProtobuf: true);

		// Assert
		Assert.IsFalse(eventReceived.Wait(TimeSpan.FromSeconds(2)), "Stale event should not be received");
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ReceiveMessage_StaleMessageWithoutFilter_Raised()
	{
		// Arrange
		var config = new TakClientConfig
		{
			FilterStaleMessages = false
		};
		_client = new TakUdpMeshClient(config);

		CotEvent? receivedEvent = null;
		var eventReceived = new ManualResetEventSlim(false);

		_client.CotEventReceived += (sender, cotEvent) =>
		{
			receivedEvent = cotEvent;
			eventReceived.Set();
		};

		await _client.StartAsync();

		// Act
		var staleEvent = TestDataFactory.CreateStaleCotEvent();
		await _multicastSender.SendCotEventAsync(staleEvent, useProtobuf: true);

		// Assert
		Assert.IsTrue(eventReceived.Wait(TimeSpan.FromSeconds(5)), "Event should be received even if stale");
		Assert.IsNotNull(receivedEvent);
	}

	[TestMethod]
	[Timeout(15000)]
	public async Task ReceiveMultipleMessages_AllReceived()
	{
		// Arrange
		var config = new TakClientConfig();
		_client = new TakUdpMeshClient(config);

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

		await _client.StartAsync();

		// Act
		for (int i = 0; i < 3; i++)
		{
			var testEvent = TestDataFactory.CreateTestCotEvent(uid: $"MULTI-{i}");
			await _multicastSender.SendCotEventAsync(testEvent, useProtobuf: true);
			await Task.Delay(100); // Small delay between sends
		}

		// Assert
		Assert.IsTrue(countdown.Wait(TimeSpan.FromSeconds(10)), "Not all events received");
		Assert.AreEqual(3, receivedEvents.Count);
		Assert.IsTrue(receivedEvents.Any(e => e.Uid == "MULTI-0"));
		Assert.IsTrue(receivedEvents.Any(e => e.Uid == "MULTI-1"));
		Assert.IsTrue(receivedEvents.Any(e => e.Uid == "MULTI-2"));
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ConnectionStateChanged_EventRaised()
	{
		// Arrange
		var config = new TakClientConfig();
		_client = new TakUdpMeshClient(config);

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
		await _client.StartAsync();
		await _client.StopAsync();

		// Assert
		Assert.IsTrue(stateChanged.Wait(TimeSpan.FromSeconds(5)), "Did not receive all expected state changes");
		Assert.IsTrue(states.Contains(ConnectionState.Connecting));
		Assert.IsTrue(states.Contains(ConnectionState.Connected));
		Assert.IsTrue(states.Contains(ConnectionState.Disconnected));
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task StopAsync_WhileRunning_StopsSuccessfully()
	{
		// Arrange
		var config = new TakClientConfig();
		_client = new TakUdpMeshClient(config);
		await _client.StartAsync();

		// Act
		await _client.StopAsync();

		// Assert
		Assert.AreEqual(ConnectionState.Disconnected, _client.ConnectionState);
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task StartAsync_AlreadyRunning_ThrowsException()
	{
		// Arrange
		var config = new TakClientConfig();
		_client = new TakUdpMeshClient(config);
		await _client.StartAsync();

		// Act & Assert
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.StartAsync());
	}

	[TestMethod]
	[Timeout(10000)]
	public async Task ErrorOccurred_InvalidMessage_RaisesEvent()
	{
		// Arrange
		var config = new TakClientConfig();
		_client = new TakUdpMeshClient(config);

		Exception? receivedException = null;
		var errorReceived = new ManualResetEventSlim(false);

		_client.ErrorOccurred += (sender, ex) =>
		{
			receivedException = ex;
			errorReceived.Set();
		};

		await _client.StartAsync();

		// Act - Send invalid packet (too small)
		var sender = new SimulatedUdpMulticast();
		var udpClient = new System.Net.Sockets.UdpClient();
		await udpClient.SendAsync(new byte[] { 0x01, 0x02 }, new System.Net.IPEndPoint(System.Net.IPAddress.Parse("239.2.3.1"), 6969));

		// Assert
		Assert.IsTrue(errorReceived.Wait(TimeSpan.FromSeconds(5)), "Error event not raised");
		Assert.IsNotNull(receivedException);
	}
}
