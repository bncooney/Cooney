# Cooney.LanguageServer

> [!WARNING]
> This library is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A .NET Standard 2.0 library providing a Language Server Protocol (LSP) client for communicating with language servers. This library enables:

- **Generic LSP Client**: Connect to any language server (Python, TypeScript, C#, Rust, etc.)
- **Multiple Transports**: Support for stdio (most common), TCP, and named pipes
- **Go to Definition**: Navigate to symbol definitions across your codebase
- **Find References**: Locate all references to a symbol
- **Diagnostics**: Receive errors, warnings, and hints from the language server

## Installation

```bash
dotnet add package Cooney.LanguageServer
```

## Quick Start

### Connecting to a Language Server (stdio)

The most common way to connect to a language server is via stdio (standard input/output), where the server runs as a child process:

```csharp
using Cooney.LanguageServer.Client;
using Cooney.LanguageServer.Protocol.Initialize;
using Cooney.LanguageServer.Protocol.Common;

// Create client for Python's Pyright language server
using var client = LanguageServerClient.CreateStdioClient("pyright-langserver", "--stdio");

// Initialize the connection
var initializeParams = new InitializeParams
{
    ProcessId = Environment.ProcessId,
    RootUri = "file:///C:/my-project",
    Capabilities = new ClientCapabilities
    {
        TextDocument = new TextDocumentClientCapabilities
        {
            Definition = new DefinitionCapability { LinkSupport = true },
            References = new ReferencesCapability(),
            PublishDiagnostics = new PublishDiagnosticsCapability
            {
                RelatedInformation = true
            }
        }
    }
};

var result = await client.InitializeAsync(initializeParams);
Console.WriteLine($"Connected to: {result.ServerInfo?.Name} {result.ServerInfo?.Version}");
```

### Go to Definition

Find where a symbol is defined:

```csharp
using Cooney.LanguageServer.Extensions;

var documentUri = UriExtensions.ToFileUri("C:/my-project/main.py");
var textDocument = new TextDocumentIdentifier(documentUri);

// Open the document
var fileContent = File.ReadAllText("C:/my-project/main.py");
await client.DidOpenTextDocumentAsync(textDocument, "python", version: 1, fileContent);

// Get definition at line 10, character 5
var position = new Position(line: 10, character: 5);
var locations = await client.GetDefinitionAsync(textDocument, position);

if (locations != null)
{
    foreach (var location in locations)
    {
        var filePath = UriExtensions.ToFilePath(location.Uri);
        Console.WriteLine($"Defined at {filePath}:{location.Range.Start.Line + 1}");
    }
}
```

### Find References

Locate all references to a symbol:

```csharp
var position = new Position(line: 15, character: 10);
var references = await client.GetReferencesAsync(
    textDocument,
    position,
    includeDeclaration: true);

if (references != null)
{
    Console.WriteLine($"Found {references.Length} reference(s):");
    foreach (var reference in references)
    {
        var filePath = UriExtensions.ToFilePath(reference.Uri);
        Console.WriteLine($"  {filePath}:{reference.Range.Start.Line + 1}");
    }
}
```

### Receiving Diagnostics

Listen for errors, warnings, and hints from the language server:

```csharp
client.DiagnosticsPublished += (sender, diagnostics) =>
{
    var filePath = UriExtensions.ToFilePath(diagnostics.Uri);
    Console.WriteLine($"\nDiagnostics for {filePath}:");

    foreach (var diagnostic in diagnostics.Diagnostics)
    {
        var severity = diagnostic.Severity switch
        {
            DiagnosticSeverity.Error => "ERROR",
            DiagnosticSeverity.Warning => "WARNING",
            DiagnosticSeverity.Information => "INFO",
            DiagnosticSeverity.Hint => "HINT",
            _ => "UNKNOWN"
        };

        var line = diagnostic.Range.Start.Line + 1;
        var col = diagnostic.Range.Start.Character + 1;
        Console.WriteLine($"  [{severity}] Line {line}, Col {col}: {diagnostic.Message}");

        if (diagnostic.Source != null)
            Console.WriteLine($"    Source: {diagnostic.Source}");
    }
};
```

### Connecting via TCP

For language servers running on a network:

```csharp
using var client = LanguageServerClient.CreateTcpClient("localhost", 9090);
var result = await client.InitializeAsync(initializeParams);
```

### Connecting via Named Pipe (Windows)

For local inter-process communication on Windows:

```csharp
using var client = LanguageServerClient.CreateNamedPipeClient("my-language-server-pipe");
var result = await client.InitializeAsync(initializeParams);
```

## Supported Language Servers

This library can connect to any language server that implements the LSP specification. Common examples include:

- **Python**: `pyright-langserver`, `pylsp`
- **TypeScript/JavaScript**: `typescript-language-server`
- **C#**: `OmniSharp`
- **Rust**: `rust-analyzer`
- **Go**: `gopls`
- **Java**: `jdtls`
- **Ruby**: `solargraph`
- **PHP**: `intelephense`

## API Reference

### LanguageServerClient

The main client class for interacting with language servers.

#### Factory Methods

- `CreateStdioClient(string serverExecutablePath, string? arguments = null)` - Creates a client that communicates via stdin/stdout
- `CreateTcpClient(string host, int port)` - Creates a client that communicates via TCP
- `CreateNamedPipeClient(string pipeName)` - Creates a client that communicates via named pipes (Windows)

#### Core Methods

- `InitializeAsync(InitializeParams, CancellationToken)` - Initializes the connection (must be called first)
- `GetDefinitionAsync(TextDocumentIdentifier, Position, CancellationToken)` - Requests symbol definitions
- `GetReferencesAsync(TextDocumentIdentifier, Position, bool, CancellationToken)` - Finds symbol references
- `DidOpenTextDocumentAsync(TextDocumentIdentifier, string, int, string, CancellationToken)` - Notifies server of opened document
- `DidCloseTextDocumentAsync(TextDocumentIdentifier, CancellationToken)` - Notifies server of closed document
- `ShutdownAsync(CancellationToken)` - Gracefully shuts down the server
- `ExitAsync()` - Terminates the server process

#### Events

- `DiagnosticsPublished` - Raised when the server sends diagnostic information

### UriExtensions

Helper methods for converting between file paths and URIs:

- `ToFileUri(string filePath)` - Converts a local file path to a file:// URI
- `ToFilePath(string fileUri)` - Converts a file:// URI to a local file path
- `ToFilePath(Uri uri)` - Extension method on Uri to get local file path

## Connection Lifecycle

The typical workflow for using a language server client:

1. **Create Client** - Use a factory method to create the client with the appropriate transport
2. **Initialize** - Call `InitializeAsync` to establish the connection and exchange capabilities
3. **Open Documents** - Notify the server when documents are opened with `DidOpenTextDocumentAsync`
4. **Make Requests** - Call methods like `GetDefinitionAsync`, `GetReferencesAsync`
5. **Receive Notifications** - Handle `DiagnosticsPublished` events
6. **Close Documents** - Notify the server when documents are closed
7. **Shutdown** - Call `ShutdownAsync` then `ExitAsync` to gracefully terminate
8. **Dispose** - Dispose the client to clean up resources

```csharp
using var client = LanguageServerClient.CreateStdioClient("language-server");
await client.InitializeAsync(initializeParams);

// Use the client...

await client.ShutdownAsync();
await client.ExitAsync();
// Dispose happens automatically with 'using'
```

## Advanced Usage

### Custom Client Capabilities

You can specify which features your client supports:

```csharp
var initializeParams = new InitializeParams
{
    ProcessId = Environment.ProcessId,
    RootUri = "file:///C:/my-project",
    ClientInfo = new ClientInfo
    {
        Name = "MyEditor",
        Version = "1.0.0"
    },
    Capabilities = new ClientCapabilities
    {
        TextDocument = new TextDocumentClientCapabilities
        {
            Definition = new DefinitionCapability
            {
                DynamicRegistration = false,
                LinkSupport = true
            },
            References = new ReferencesCapability
            {
                DynamicRegistration = false
            },
            PublishDiagnostics = new PublishDiagnosticsCapability
            {
                RelatedInformation = true,
                TagSupport = true,
                CodeDescriptionSupport = true
            }
        }
    }
};
```

### Error Handling

The library throws `LanguageServerException` when operations fail:

```csharp
try
{
    var locations = await client.GetDefinitionAsync(textDocument, position);
}
catch (LanguageServerException ex)
{
    Console.WriteLine($"LSP Error: {ex.Message}");
    if (ex.ErrorCode.HasValue)
        Console.WriteLine($"Error Code: {ex.ErrorCode.Value}");
}
```

## Troubleshooting

### Server Process Won't Start

- Verify the executable path is correct and the file exists
- Check that the language server is installed and in your PATH
- Ensure you have permission to execute the server
- Review the server's documentation for required command-line arguments

### No Diagnostics Received

- Ensure you've subscribed to the `DiagnosticsPublished` event before calling `InitializeAsync`
- Verify the document was opened with `DidOpenTextDocumentAsync`
- Some servers only publish diagnostics for files in the workspace root
- Check that the document URI uses the correct file:// scheme

### Definition/References Return Null

- The symbol may not be defined in the current workspace
- The document must be opened before requesting definitions or references
- Ensure the position is on a valid symbol
- Some servers require time to index the workspace after initialization

### Connection Issues

- For stdio: Check that the server executable can be found and executed
- For TCP: Verify the server is running and listening on the specified port
- For named pipes: Ensure the pipe name matches exactly and the server is running

## Requirements

- .NET Standard 2.0 or later
- Depends on:
  - StreamJsonRpc 2.22.23
  - System.Text.Json 10.0.1
  - System.IO.Pipelines 10.0.1
  - Cooney.Common (for validation utilities)

## Testing

The library includes comprehensive unit tests and integration tests.

### Running Unit Tests

```bash
dotnet test --filter "FullyQualifiedName!~Integration"
```

### Running Integration Tests

Integration tests connect to real language servers. To run them, you'll need to install a C# language server:

**Option 1: csharp-ls** (lightweight)
```bash
dotnet tool install --global csharp-ls
```
Note: csharp-ls requires MSBuild to be installed and may have issues in some environments.

**Option 2: OmniSharp** (recommended for integration testing)
Download from [OmniSharp releases](https://github.com/OmniSharp/omnisharp-roslyn/releases)

Then run:
```bash
dotnet test --filter "FullyQualifiedName~Integration"
```

The integration tests will automatically detect available language servers and skip if none are found.

**Note**: Integration tests verify that the LSP client can communicate with real language servers. Even if a test fails due to language server configuration issues (e.g., missing MSBuild), successful JSON-RPC communication indicates the client is working correctly.

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Since this is "vibe coded," feel free to improve it and submit pull requests.

## Related Projects

- [Microsoft Language Server Protocol Specification](https://microsoft.github.io/language-server-protocol/)
- [StreamJsonRpc](https://github.com/microsoft/vs-streamjsonrpc)
- [OmniSharp.Extensions.LanguageServer](https://github.com/OmniSharp/csharp-language-server-protocol)
