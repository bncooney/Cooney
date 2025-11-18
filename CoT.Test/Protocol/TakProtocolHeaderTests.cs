using CoT.Protocol;

namespace CoT.Test.Protocol;

[TestClass]
public class TakProtocolHeaderTests
{
	[TestMethod]
	public void Parse_MeshModeHeader_ParsesCorrectly()
	{
		// Arrange - Mesh mode: 0xBF 0x01 0xBF
		byte[] data = [0xBF, 0x01, 0xBF, 0x01, 0x02, 0x03];

		// Act
		var header = TakProtocolHeader.Parse(data, out int bytesRead);

		// Assert
		Assert.AreEqual(1, header.Version);
		Assert.IsFalse(header.IsStreamMode);
		Assert.AreEqual(3, bytesRead);
	}

	[TestMethod]
	public void Parse_StreamModeHeader_ParsesCorrectly()
	{
		// Arrange - Stream mode: 0xBF [varint length]
		// Varint encoding of 127: 0x7F
		byte[] data = [0xBF, 0x7F, 0x01, 0x02, 0x03];

		// Act
		var header = TakProtocolHeader.Parse(data, out int bytesRead);

		// Assert
		Assert.AreEqual(127, header.PayloadLength);
		Assert.IsTrue(header.IsStreamMode);
		Assert.AreEqual(1, header.Version); // Stream mode always version 1
		Assert.AreEqual(2, bytesRead);
	}

	[TestMethod]
	public void Parse_StreamModeWithMultiByteVarint_ParsesCorrectly()
	{
		// Arrange - Stream mode with varint encoding of 300: 0xAC 0x02
		byte[] data = [0xBF, 0xAC, 0x02];

		// Act
		var header = TakProtocolHeader.Parse(data, out int bytesRead);

		// Assert
		Assert.AreEqual(300, header.PayloadLength);
		Assert.IsTrue(header.IsStreamMode);
		Assert.AreEqual(3, bytesRead);
	}

	[TestMethod]
	public void Parse_InvalidMagicByte_ThrowsException()
	{
		// Arrange
		byte[] data = [0xAA, 0x01, 0xBF];

		// Act & Assert
		var ex = Assert.Throws<InvalidDataException>(() => TakProtocolHeader.Parse(data, out _));
	}

	[TestMethod]
	public void Parse_DataTooSmall_ThrowsException()
	{
		// Arrange
		byte[] data = [0xBF];

		// Act & Assert
		var ex = Assert.Throws<ArgumentException>(() => TakProtocolHeader.Parse(data, out _));
	}

	[TestMethod]
	public void ToBytes_MeshMode_GeneratesCorrectHeader()
	{
		// Arrange
		var header = new TakProtocolHeader
		{
			Version = 1,
			IsStreamMode = false
		};

		// Act
		var bytes = header.ToBytes();

		// Assert
		Assert.HasCount(3, bytes);
		Assert.AreEqual(0xBF, bytes[0]);
		Assert.AreEqual(0x01, bytes[1]);
		Assert.AreEqual(0xBF, bytes[2]);
	}

	[TestMethod]
	public void ToBytes_StreamMode_GeneratesCorrectHeader()
	{
		// Arrange
		var header = new TakProtocolHeader
		{
			Version = 1,
			IsStreamMode = true,
			PayloadLength = 127
		};

		// Act
		var bytes = header.ToBytes();

		// Assert
		Assert.HasCount(2, bytes);
		Assert.AreEqual(0xBF, bytes[0]);
		Assert.AreEqual(0x7F, bytes[1]); // Varint for 127
	}

	[TestMethod]
	public void ToBytes_StreamModeWithLargePayload_GeneratesCorrectHeader()
	{
		// Arrange
		var header = new TakProtocolHeader
		{
			Version = 1,
			IsStreamMode = true,
			PayloadLength = 300
		};

		// Act
		var bytes = header.ToBytes();

		// Assert
		Assert.HasCount(3, bytes);
		Assert.AreEqual(0xBF, bytes[0]);
		Assert.AreEqual(0xAC, bytes[1]); // First byte of varint for 300
		Assert.AreEqual(0x02, bytes[2]); // Second byte of varint for 300
	}

	[TestMethod]
	public void ParseFromStream_MeshMode_ParsesCorrectly()
	{
		// Arrange
		byte[] data = [0xBF, 0x01, 0xBF];
		using var stream = new MemoryStream(data);

		// Act
		var header = TakProtocolHeader.ParseFromStream(stream);

		// Assert
		Assert.AreEqual(1, header.Version);
		Assert.IsFalse(header.IsStreamMode);
	}

	[TestMethod]
	public void ParseFromStream_StreamMode_ParsesCorrectly()
	{
		// Arrange
		byte[] data = [0xBF, 0x7F];
		using var stream = new MemoryStream(data);

		// Act
		var header = TakProtocolHeader.ParseFromStream(stream);

		// Assert
		Assert.AreEqual(127, header.PayloadLength);
		Assert.IsTrue(header.IsStreamMode);
	}

	[TestMethod]
	public void RoundTrip_MeshMode_Succeeds()
	{
		// Arrange
		var original = new TakProtocolHeader
		{
			Version = 1,
			IsStreamMode = false
		};

		// Act
		var bytes = original.ToBytes();
		var parsed = TakProtocolHeader.Parse(bytes, out _);

		// Assert
		Assert.AreEqual(original.Version, parsed.Version);
		Assert.AreEqual(original.IsStreamMode, parsed.IsStreamMode);
	}

	[TestMethod]
	public void RoundTrip_StreamMode_Succeeds()
	{
		// Arrange
		var original = new TakProtocolHeader
		{
			Version = 1,
			IsStreamMode = true,
			PayloadLength = 1234
		};

		// Act
		var bytes = original.ToBytes();
		var parsed = TakProtocolHeader.Parse(bytes, out _);

		// Assert
		Assert.AreEqual(original.PayloadLength, parsed.PayloadLength);
		Assert.AreEqual(original.IsStreamMode, parsed.IsStreamMode);
	}
}
