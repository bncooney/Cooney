# CoT - Cursor on Target / TAK Protocol Library

A .NET Standard 2.0 library for subscribing to and publishing Cursor on Target (CoT) messages using the TAK Protocol. Supports both legacy XML (Version 0) and modern Protobuf (Version 1) formats over UDP multicast (Mesh Mode) and TCP streams (Server Mode).

## Overview

The TAK (Tactical Assault Kit) protocol is used for tactical situational awareness, enabling real-time sharing of position, status, and other tactical information across military and emergency response networks.

### Protocol Formats

- **Version 0**: Legacy XML-based CoT messages
- **Version 1**: Modern Protocol Buffers (protobuf) format for efficient binary serialization

### Network Modes

- **UDP Mesh Mode**: Multicast group communication (typically `239.2.3.1:6969`)
- **TCP Stream Mode**: Point-to-point connection to TAK Server

## Features

- Full support for TAK Protocol Version 1 (Protobuf)
- Backward compatibility with XML CoT messages (Version 0)
- UDP multicast client for mesh networking
- TCP stream client with automatic reconnection
- Multi-client manager for coordinating multiple connections
- Extensible message parsing and serialization
- Built-in stale message filtering
- Helper extension methods for common operations

## Installation

Add the library to your project:

```bash
dotnet add reference path/to/CoT/CoT.csproj
```

## Quick Start

### UDP Multicast Client (Mesh Mode)

```csharp
using CoT.Configuration;
using CoT.Network;

// Configure UDP multicast client
var config = new TakClientConfig
{
    Host = "239.2.3.1",  // Standard TAK multicast group
    Port = 6969,          // Standard TAK port
    Protocol = TakProtocol.UDP_Mesh
};

// Create and start client
var client = new TakUdpMeshClient(config);

// Subscribe to CoT events
client.CotEventReceived += (sender, cotEvent) =>
{
    Console.WriteLine($"Received: {cotEvent.Type}");
    Console.WriteLine($"  UID: {cotEvent.Uid}");
    Console.WriteLine($"  Position: {cotEvent.Lat}, {cotEvent.Lon}");
    Console.WriteLine($"  Callsign: {cotEvent.GetCallsign()}");
};

// Handle errors
client.ErrorOccurred += (sender, ex) =>
{
    Console.WriteLine($"Error: {ex.Message}");
};

// Start listening
await client.StartAsync();

// Keep running...
Console.WriteLine("Listening for CoT messages. Press any key to stop.");
Console.ReadKey();

// Clean up
await client.StopAsync();
```

### TCP Stream Client (Server Mode)

```csharp
using CoT.Configuration;
using CoT.Network;

// Configure TCP stream client
var config = new TakClientConfig
{
    Host = "tak-server.example.com",
    Port = 8087,
    Protocol = TakProtocol.TCP_Stream,
    ReconnectInterval = TimeSpan.FromSeconds(5)
};

// Create client
var client = new TakTcpStreamClient(config);

// Subscribe to events
client.CotEventReceived += (sender, cotEvent) =>
{
    Console.WriteLine($"[{cotEvent.GetSendTime():HH:mm:ss}] {cotEvent.ToSummaryString()}");
};

// Monitor connection state
client.ConnectionStateChanged += (sender, state) =>
{
    Console.WriteLine($"Connection state: {state}");
};

// Connect to server
await client.ConnectAsync();

// Send a position update
var myPosition = CotEventExtensions.CreatePositionUpdate(
    uid: "MY-DEVICE-123",
    type: "a-f-G-E-V",  // Friendly Ground Equipment Vehicle
    lat: 37.7749,
    lon: -122.4194,
    hae: 10.0,
    callsign: "Alpha-1",
    ttl: TimeSpan.FromMinutes(5)
);

await client.SendCotEventAsync(myPosition);
```

### Managing Multiple Clients

```csharp
using CoT.Network;

// Create manager
var manager = new TakClientManager();

// Add UDP client for local mesh
manager.AddUdpClient("local-mesh", new TakClientConfig
{
    Host = "239.2.3.1",
    Port = 6969,
    Protocol = TakProtocol.UDP_Mesh
});

// Add TCP client for remote server
manager.AddTcpClient("remote-server", new TakClientConfig
{
    Host = "remote.tak-server.com",
    Port = 8087,
    Protocol = TakProtocol.TCP_Stream
});

// Subscribe to unified events
manager.CotEventReceived += (sender, e) =>
{
    Console.WriteLine($"[{e.ClientId}] {e.CotEvent.ToSummaryString()}");
};

// Start all clients
await manager.StartAllClientsAsync();

// Send message via specific client
await manager.SendCotEventAsync("remote-server", myPosition);
```

## Advanced Usage

### Custom Event Handling

```csharp
client.TakMessageReceived += (sender, takMessage) =>
{
    // Access TAK control information
    if (takMessage.TakControl != null)
    {
        Console.WriteLine($"Protocol version range: {takMessage.TakControl.MinProtoVersion}-{takMessage.TakControl.MaxProtoVersion}");
    }

    // Access CoT event
    if (takMessage.CotEvent != null)
    {
        var cot = takMessage.CotEvent;

        // Check detail fields
        if (cot.Detail?.Contact != null)
        {
            Console.WriteLine($"Contact: {cot.Detail.Contact.Callsign} @ {cot.Detail.Contact.Endpoint}");
        }

        if (cot.Detail?.Track != null)
        {
            Console.WriteLine($"Speed: {cot.Detail.Track.Speed} m/s, Course: {cot.Detail.Track.Course}°");
        }

        if (cot.Detail?.Status != null)
        {
            Console.WriteLine($"Battery: {cot.Detail.Status.Battery}%");
        }
    }
};
```

### Using Extension Methods

```csharp
using CoT.Extensions;

// Check if message is stale
if (cotEvent.IsStale())
{
    Console.WriteLine("Message is stale, ignoring");
    return;
}

// Get formatted timestamps
Console.WriteLine($"Sent: {cotEvent.GetSendTime()}");
Console.WriteLine($"Stale in: {cotEvent.GetTimeUntilStale()}");

// Validate coordinates
if (!cotEvent.HasValidCoordinates())
{
    Console.WriteLine("Invalid coordinates");
    return;
}

// Get friendly type description
Console.WriteLine($"Type: {cotEvent.GetTypeDescription()}");

// Create summary
Console.WriteLine(cotEvent.ToSummaryString());
```

### Filtering Messages

```csharp
// Filter by event type
client.CotEventReceived += (sender, cotEvent) =>
{
    // Only process friendly units
    if (cotEvent.Type.StartsWith("a-f-"))
    {
        ProcessFriendlyUnit(cotEvent);
    }
};

// Geographic filtering
client.CotEventReceived += (sender, cotEvent) =>
{
    if (IsInAreaOfInterest(cotEvent.Lat, cotEvent.Lon))
    {
        ProcessEvent(cotEvent);
    }
};

// Auto-filter stale messages via config
var config = new TakClientConfig
{
    FilterStaleMessages = true  // Automatically ignore stale events
};
```

### XML and Protobuf Interoperability

```csharp
using CoT.Protocol;

var parser = new TakMessageParser();

// Parse protobuf message
var takMessage = parser.ParseProtobuf(protobufBytes);

// Parse XML message (legacy)
var cotEvent = parser.ParseXml(xmlBytes);

// Serialize to protobuf
var protobufBytes = parser.SerializeToProtobuf(takMessage);

// Serialize to XML (legacy)
var xmlBytes = parser.SerializeToXml(cotEvent);
```

## Project Structure

```
CoT/
├── Proto/                      # Protocol Buffer definitions
│   ├── takmessage.proto       # Top-level TAK message
│   ├── cotevent.proto         # CoT event structure
│   ├── detail.proto           # Detail container
│   ├── contact.proto          # Contact information
│   ├── group.proto            # Group membership
│   ├── track.proto            # Movement tracking
│   ├── status.proto           # Status information
│   ├── takv.proto             # TAK version info
│   ├── precisionlocation.proto # Precision location data
│   └── takcontrol.proto       # Protocol control
├── Protocol/
│   ├── TakProtocolHeader.cs   # Header parsing (mesh/stream modes)
│   └── TakMessageParser.cs    # Message serialization/deserialization
├── Network/
│   ├── TakUdpMeshClient.cs    # UDP multicast client
│   ├── TakTcpStreamClient.cs  # TCP stream client
│   └── TakClientManager.cs    # Multi-client coordinator
├── Configuration/
│   ├── TakClientConfig.cs     # Client configuration
│   ├── TakProtocol.cs         # Protocol enum
│   └── ConnectionState.cs     # Connection state enum
├── Interfaces/
│   └── ITakEventHandler.cs    # Event handler interface
└── Extensions/
    └── CotEventExtensions.cs  # Helper extension methods
```

## Protocol Details

### TAK Protocol Header

**Mesh Mode (UDP):**
```
0xBF [version] 0xBF [payload]
```

**Stream Mode (TCP):**
```
0xBF [varint length] [payload]
```

### CoT Event Type Codes

Event types follow the format: `a-attitude-function-...`

**Attitudes:**
- `f` - Friendly
- `h` - Hostile
- `n` - Neutral
- `u` - Unknown
- `p` - Pending
- `a` - Assumed Friend
- `s` - Suspect

**Example Types:**
- `a-f-G-E-V` - Friendly Ground Equipment Vehicle
- `a-f-G-U-C` - Friendly Ground Unit Combat
- `a-h-A` - Hostile Aircraft
- `a-u-G` - Unknown Ground

### Timestamps

All timestamps are in **milliseconds since Unix epoch (1970-01-01 00:00:00 UTC)**.

## Configuration Options

```csharp
var config = new TakClientConfig
{
    Host = "239.2.3.1",                          // Server/multicast address
    Port = 6969,                                  // Port number
    Protocol = TakProtocol.UDP_Mesh,             // UDP_Mesh or TCP_Stream
    SupportXmlFallback = true,                   // Support legacy XML
    BufferSize = 8192,                           // Receive buffer size
    ReconnectInterval = TimeSpan.FromSeconds(5), // TCP reconnect interval
    ConnectionTimeout = TimeSpan.FromSeconds(30),// TCP connection timeout
    FilterStaleMessages = true,                  // Auto-filter stale events
    LocalAddress = null                          // Local bind address (null = any)
};
```

## Standards and References

- **TAK Protocol**: Based on AndroidTacticalAssaultKit-CIV protocol definitions
- **CoT Standard**: Cursor on Target XML schema (MITRE Corporation)
- **Protocol Buffers**: Google Protocol Buffers v3
- **MIL-STD-2525**: Military symbology standard (for event type codes)

## License

This implementation is based on publicly available TAK protocol specifications from the Department of Defense AndroidTacticalAssaultKit-CIV project.

## Contributing

Contributions are welcome! Please ensure all changes maintain compatibility with the official TAK protocol specifications.

## See Also

- [AndroidTacticalAssaultKit-CIV](https://github.com/deptofdefense/AndroidTacticalAssaultKit-CIV)
- [TAK Product Center](https://github.com/TAK-Product-Center)
- [MITRE CoT Documentation](https://www.mitre.org/sites/default/files/pdf/09_4937.pdf)
