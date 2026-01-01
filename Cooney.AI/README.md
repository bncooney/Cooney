# Cooney.AI

> [!WARNING]
> This library is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A .NET Standard 2.0 library providing AI abstractions and tools using `Microsoft.Extensions.AI`. This library offers extensible AI functionality for building intelligent applications.

## Features

- **Microsoft.Extensions.AI Integration** - Built on the standard Microsoft AI abstractions
- **AI Function Tools** - Pre-built tools that extend AI model capabilities
- **Extensible Architecture** - Easy to add new AI providers and tools
- **Cross-Platform** - Works with .NET Standard 2.0 and above

## Installation

Add the package reference to your project:

```bash
dotnet add package Cooney.AI
```

Or via Package Manager:

```powershell
Install-Package Cooney.AI
```

## AI Tools

### ReadFile Tool

The `ReadFile` tool allows AI chat clients to read file contents with support for large files through chunking.

#### Usage with Chat Clients

```csharp
using Cooney.AI.Tools;
using Microsoft.Extensions.AI;

// Create your chat client (e.g., using Ollama, Azure OpenAI, etc.)
IChatClient chatClient = ...; // Your chat client implementation

// Create the ReadFile tool
var readFileTool = new ReadFile();

// Create chat options with the tool
var options = new ChatOptions
{
    Tools = new[] { AIFunctionFactory.Create(readFileTool) }
};

// Chat with the AI, which can now read files
var response = await chatClient.CompleteAsync(
    "What's in the file /path/to/myfile.txt?",
    options
);
```

#### ReadFile Parameters

- **file_path** (required): The absolute path to the file to read
- **offset** (optional): The line number to start reading from (0-based, default: 0)
- **limit** (optional): Maximum number of lines to read (default: read entire file, max: 10000)

#### ReadFile Features

- **Large File Support**: Automatically handles large files through chunking
- **Security**: Validates file paths and requires absolute paths
- **Error Handling**: Provides detailed error messages for access denied, file not found, etc.
- **Truncation Detection**: Indicates when output was truncated due to size limits

#### ReadFile Response Format

The tool returns a JSON object with:

```json
{
  "success": true,
  "content": "file contents here...",
  "lines_read": 100,
  "total_lines": 250,
  "was_truncated": true,
  "offset": 0,
  "limit": 100
}
```

For errors:

```json
{
  "success": false,
  "error": "error description",
  "error_type": "FileNotFound"
}
```

## Extending Cooney.AI

This library is designed to be extended with additional AI providers and tools. Future additions may include:

- Additional AI function tools
- Image generation clients
- Audio processing tools
- And more...

## Requirements

- .NET Standard 2.0 or higher
- Microsoft.Extensions.AI 10.1.1 or higher

## License

MIT License - see LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## Repository

https://github.com/bncooney/Cooney
