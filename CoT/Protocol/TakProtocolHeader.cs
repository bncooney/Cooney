using System.IO;

namespace CoT.Protocol;

/// <summary>
/// Represents the TAK Protocol header for message framing.
/// Header format: 0xBF [version/length] [0xBF for mesh mode]
/// </summary>
public class TakProtocolHeader
{
	/// <summary>
	/// Magic byte that indicates TAK protocol message (0xBF)
	/// </summary>
	public const byte MagicByte = 0xBF;

	/// <summary>
	/// Protocol version (0 = XML, 1 = Protobuf)
	/// </summary>
	public byte Version { get; set; }

	/// <summary>
	/// Payload length in bytes (used in stream mode with varint encoding)
	/// </summary>
	public int PayloadLength { get; set; }

	/// <summary>
	/// Whether this is stream mode (TCP) vs mesh mode (UDP)
	/// </summary>
	public bool IsStreamMode { get; set; }

	/// <summary>
	/// Parses a TAK protocol header from a byte array.
	/// Mesh mode: 0xBF 0x01 0xBF
	/// Stream mode: 0xBF [varint length]
	/// </summary>
	/// <param name="data">Data buffer containing the header</param>
	/// <param name="bytesRead">Number of bytes read from the buffer</param>
	/// <returns>Parsed header</returns>
	public static TakProtocolHeader Parse(byte[] data, out int bytesRead)
	{
		if (data == null || data.Length < 2)
			throw new ArgumentException("Data buffer too small for TAK header", nameof(data));

		if (data[0] != MagicByte)
			throw new InvalidDataException($"Invalid magic byte: expected 0x{MagicByte:X2}, got 0x{data[0]:X2}");

		var header = new TakProtocolHeader();

		// Check if this is mesh mode (second magic byte at position 2)
		if (data.Length >= 3 && data[2] == MagicByte)
		{
			// Mesh mode: 0xBF [version] 0xBF
			header.Version = data[1];
			header.IsStreamMode = false;
			bytesRead = 3;
		}
		else
		{
			// Stream mode: 0xBF [varint length]
			using (var stream = new MemoryStream(data, 1, data.Length - 1))
			{
				header.PayloadLength = ReadVarint(stream);
				header.IsStreamMode = true;
				header.Version = 1; // Stream mode always uses protobuf
				bytesRead = 1 + (int)stream.Position;
			}
		}

		return header;
	}

	/// <summary>
	/// Parses a TAK protocol header from a stream (useful for TCP)
	/// </summary>
	public static TakProtocolHeader ParseFromStream(Stream stream)
	{
		int firstByte = stream.ReadByte();
		if (firstByte == -1)
			throw new EndOfStreamException("Stream ended while reading header");

		if (firstByte != MagicByte)
			throw new InvalidDataException($"Invalid magic byte: expected 0x{MagicByte:X2}, got 0x{firstByte:X2}");

		int secondByte = stream.ReadByte();
		if (secondByte == -1)
			throw new EndOfStreamException("Stream ended while reading header");

		var header = new TakProtocolHeader();

		// Peek ahead to check for mesh mode
		int thirdByte = stream.ReadByte();
		if (thirdByte == MagicByte)
		{
			// Mesh mode: 0xBF [version] 0xBF
			header.Version = (byte)secondByte;
			header.IsStreamMode = false;
		}
		else
		{
			// Stream mode: 0xBF [varint length]
			// We've already read the first byte of the varint
			// Put it back and read the full varint
			var buffer = new byte[10]; // Max varint size
			buffer[0] = (byte)secondByte;

			if (thirdByte != -1)
			{
				buffer[1] = (byte)thirdByte;
			}

			using (var ms = new MemoryStream(buffer))
			{
				header.PayloadLength = ReadVarint(ms);
				header.IsStreamMode = true;
				header.Version = 1; // Stream mode uses protobuf
			}
		}

		return header;
	}

	/// <summary>
	/// Converts the header to bytes for transmission.
	/// </summary>
	public byte[] ToBytes()
	{
		if (IsStreamMode)
		{
			// Stream mode: 0xBF [varint length]
			using (var ms = new MemoryStream())
			{
				ms.WriteByte(MagicByte);
				WriteVarint(ms, PayloadLength);
				return ms.ToArray();
			}
		}
		else
		{
			// Mesh mode: 0xBF [version] 0xBF
			return new byte[] { MagicByte, Version, MagicByte };
		}
	}

	/// <summary>
	/// Reads a varint-encoded integer from a stream.
	/// Protocol Buffers varint encoding: 7 bits per byte, MSB indicates continuation.
	/// </summary>
	private static int ReadVarint(Stream stream)
	{
		int result = 0;
		int shift = 0;
		byte b;

		do
		{
			int readByte = stream.ReadByte();
			if (readByte == -1)
				throw new EndOfStreamException("Stream ended while reading varint");

			b = (byte)readByte;
			result |= (b & 0x7F) << shift;
			shift += 7;

			if (shift > 35) // Prevent overflow (max 5 bytes for int32)
				throw new InvalidDataException("Varint too long");

		} while ((b & 0x80) != 0);

		return result;
	}

	/// <summary>
	/// Writes a varint-encoded integer to a stream.
	/// </summary>
	private static void WriteVarint(Stream stream, int value)
	{
		while (value > 0x7F)
		{
			stream.WriteByte((byte)((value & 0x7F) | 0x80));
			value >>= 7;
		}
		stream.WriteByte((byte)value);
	}
}
