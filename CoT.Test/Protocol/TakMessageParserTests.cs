using CoT.Protocol;
using CoT.Test.TestInfrastructure;
using Atakmap.Commoncommo.Protobuf.V1;

namespace CoT.Test.Protocol;

[TestClass]
public class TakMessageParserTests
{
	private TakMessageParser _parser = null!;

	[TestInitialize]
	public void Setup()
	{
		_parser = new TakMessageParser();
	}

	[TestMethod]
	public void ParseProtobuf_ValidMessage_ParsesCorrectly()
	{
		// Arrange
		var original = TestDataFactory.CreateTestTakMessage();
		var bytes = _parser.SerializeToProtobuf(original);

		// Act
		var parsed = _parser.ParseProtobuf(bytes);

		// Assert
		Assert.IsNotNull(parsed);
		Assert.IsNotNull(parsed.CotEvent);
		Assert.AreEqual(original.CotEvent.Uid, parsed.CotEvent.Uid);
		Assert.AreEqual(original.CotEvent.Type, parsed.CotEvent.Type);
		Assert.AreEqual(original.CotEvent.Lat, parsed.CotEvent.Lat, 0.000001);
		Assert.AreEqual(original.CotEvent.Lon, parsed.CotEvent.Lon, 0.000001);
	}

	[TestMethod]
	public void ParseProtobuf_EmptyPayload_ThrowsException()
	{
		// Act & Assert
		var ex = Assert.Throws<ArgumentException>(() => _parser.ParseProtobuf([]));
	}

	[TestMethod]
	public void ParseProtobuf_InvalidData_ThrowsException()
	{
		// Arrange
		byte[] invalidData = [0x01, 0x02, 0x03, 0x04];

		// Act & Assert
		var ex = Assert.Throws<InvalidOperationException>(() => _parser.ParseProtobuf(invalidData));
	}

	[TestMethod]
	public void SerializeToProtobuf_ValidMessage_Succeeds()
	{
		// Arrange
		var message = TestDataFactory.CreateTestTakMessage();

		// Act
		var bytes = _parser.SerializeToProtobuf(message);

		// Assert
		Assert.IsNotNull(bytes);
		Assert.IsTrue(bytes.Length > 0);
	}

	[TestMethod]
	public void SerializeToXml_ValidCotEvent_CreatesValidXml()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent();

		// Act
		var xmlBytes = _parser.SerializeToXml(cotEvent);
		var xmlString = System.Text.Encoding.UTF8.GetString(xmlBytes);

		// Assert
		Assert.IsTrue(xmlString.Contains("<event"));
		Assert.IsTrue(xmlString.Contains($"uid=\"{cotEvent.Uid}\""));
		Assert.IsTrue(xmlString.Contains($"type=\"{cotEvent.Type}\""));
		Assert.IsTrue(xmlString.Contains("<point"));
		Assert.IsTrue(xmlString.Contains($"lat=\"{cotEvent.Lat}\""));
		Assert.IsTrue(xmlString.Contains($"lon=\"{cotEvent.Lon}\""));
	}

	[TestMethod]
	public void ParseXml_ValidXml_ParsesCorrectly()
	{
		// Arrange
		var original = TestDataFactory.CreateTestCotEvent(callsign: "TestCall");
		var xmlBytes = _parser.SerializeToXml(original);

		// Act
		var parsed = _parser.ParseXml(xmlBytes);

		// Assert
		Assert.IsNotNull(parsed);
		Assert.AreEqual(original.Uid, parsed.Uid);
		Assert.AreEqual(original.Type, parsed.Type);
		Assert.AreEqual(original.Lat, parsed.Lat, 0.000001);
		Assert.AreEqual(original.Lon, parsed.Lon, 0.000001);
	}

	[TestMethod]
	public void RoundTrip_Protobuf_PreservesData()
	{
		// Arrange
		var original = TestDataFactory.CreateDetailedCotEvent();
		var message = new TakMessage { CotEvent = original };

		// Act
		var bytes = _parser.SerializeToProtobuf(message);
		var parsed = _parser.ParseProtobuf(bytes);

		// Assert
		Assert.AreEqual(original.Uid, parsed.CotEvent.Uid);
		Assert.AreEqual(original.Type, parsed.CotEvent.Type);
		Assert.AreEqual(original.Detail.Contact.Callsign, parsed.CotEvent.Detail.Contact.Callsign);
		Assert.AreEqual(original.Detail.Group.Name, parsed.CotEvent.Detail.Group.Name);
		Assert.AreEqual(original.Detail.Track.Speed, parsed.CotEvent.Detail.Track.Speed);
		Assert.AreEqual(original.Detail.Status.Battery, parsed.CotEvent.Detail.Status.Battery);
	}

	[TestMethod]
	public void RoundTrip_Xml_PreservesBasicData()
	{
		// Arrange
		var original = TestDataFactory.CreateTestCotEvent(callsign: "Alpha-1");

		// Act
		var xmlBytes = _parser.SerializeToXml(original);
		var parsed = _parser.ParseXml(xmlBytes);

		// Assert
		Assert.AreEqual(original.Uid, parsed.Uid);
		Assert.AreEqual(original.Type, parsed.Type);
		Assert.AreEqual(original.How, parsed.How);
		Assert.AreEqual(original.Lat, parsed.Lat, 0.000001);
		Assert.AreEqual(original.Lon, parsed.Lon, 0.000001);
		Assert.IsNotNull(parsed.Detail);
		Assert.AreEqual("Alpha-1", parsed.Detail.Contact.Callsign);
	}

	[TestMethod]
	public void ParseXml_WithDetailElements_ParsesCorrectly()
	{
		// Arrange
		var xml = @"
<event version=""2.0"" uid=""TEST-123"" type=""a-f-G-E-V"" time=""2024-01-01T12:00:00Z"" start=""2024-01-01T12:00:00Z"" stale=""2024-01-01T13:00:00Z"" how=""m-g"">
	<point lat=""37.7749"" lon=""-122.4194"" hae=""10.0"" ce=""5.0"" le=""3.0"" />
	<detail>
		<contact endpoint=""tcp://192.168.1.1:8087"" callsign=""Bravo-2"" />
		<__group name=""Team Bravo"" role=""Rifleman"" />
		<track speed=""3.5"" course=""180.0"" />
		<status battery=""85"" />
	</detail>
</event>";
		var xmlBytes = System.Text.Encoding.UTF8.GetBytes(xml);

		// Act
		var parsed = _parser.ParseXml(xmlBytes);

		// Assert
		Assert.AreEqual("TEST-123", parsed.Uid);
		Assert.AreEqual("a-f-G-E-V", parsed.Type);
		Assert.AreEqual(37.7749, parsed.Lat, 0.000001);
		Assert.AreEqual(-122.4194, parsed.Lon, 0.000001);
		Assert.IsNotNull(parsed.Detail);
		Assert.AreEqual("Bravo-2", parsed.Detail.Contact.Callsign);
		Assert.AreEqual("Team Bravo", parsed.Detail.Group.Name);
		Assert.AreEqual(3.5, parsed.Detail.Track.Speed);
		Assert.AreEqual(85u, parsed.Detail.Status.Battery);
	}

	[TestMethod]
	public void ParseProtobufCotEvent_ValidMessage_ExtractsCotEvent()
	{
		// Arrange
		var original = TestDataFactory.CreateTestTakMessage();
		var bytes = _parser.SerializeToProtobuf(original);

		// Act
		var cotEvent = _parser.ParseProtobufCotEvent(bytes);

		// Assert
		Assert.IsNotNull(cotEvent);
		Assert.AreEqual(original.CotEvent.Uid, cotEvent.Uid);
	}
}
