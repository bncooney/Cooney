# Cooney.AI

> [!WARNING]
> This library is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A .NET Standard 2.0 library providing AI integration tools and a ComfyUI API client implementing the `Microsoft.Extensions.AI` abstractions. This library includes:

- **ComfyUI Integration**: Queue workflows, monitor execution, and retrieve generated images from a ComfyUI server
- **AI Function Tools**: Pre-built tools (like file reading) that can be used with any `IChatClient` to extend AI model capabilities

## Features

- **Microsoft.Extensions.AI integration** - Implements `IImageGenerator` for standardized AI service integration
- **ComfyUI API client** - Queue workflows, poll for completion, and download results
- **Workflow abstraction** - Define custom workflows or use the built-in Flux workflow
- **Source generation** - Automatically generates strongly-typed workflow classes from JSON files
- **Server management** - Query available models, node types, and system stats
- **Image upload/download** - Upload input images and retrieve generated outputs
- **AI Tools** - Pre-built AI function tools for chat clients (file reading, etc.)

## Prerequisites

This library requires a running ComfyUI instance. You can run ComfyUI locally or connect to a remote server.

**Default server address**: `http://127.0.0.1:8188`

## Quick Start

### Basic Image Generation

```csharp
using Cooney.AI;
using Microsoft.Extensions.AI;

// Create a client (defaults to http://127.0.0.1:8188)
var client = new ComfyUIApiClient();

// Generate an image
var response = await client.GenerateAsync(new ImageGenerationRequest
{
    Prompt = "A beautiful landscape painting of mountains during sunset."
});

// Save the image
var imageData = (byte[])response.RawRepresentation!;
await File.WriteAllBytesAsync("output.png", imageData);
```

### Custom Generation Options

```csharp
using Cooney.AI;
using Microsoft.Extensions.AI;

var client = new ComfyUIApiClient();

// Configure generation options
var options = ComfyUIApiClient.DefaultImageGenerationOptions.Clone();
options.ModelId = "flux1-dev-fp8.safetensors";
options.ImageSize = new(512, 512);
options.AdditionalProperties!["steps"] = 30;
options.AdditionalProperties["cfg_scale"] = 1.5;
options.AdditionalProperties["guidance_scale"] = 4.0;
options.AdditionalProperties["seed"] = 42;

var response = await client.GenerateAsync(
    new ImageGenerationRequest { Prompt = "A cyberpunk cityscape at night" },
    options);

var imageData = (byte[])response.RawRepresentation!;
await File.WriteAllBytesAsync("cyberpunk.png", imageData);
```

## AI Tools

The library provides pre-built AI function tools that can be used with any `IChatClient` from Microsoft.Extensions.AI. These tools enable AI models to perform actions like reading files, with more tools planned for future releases.

### ReadFile Tool

The `ReadFile` tool allows AI models to read file contents with support for large files through chunking.

#### Basic Usage

```csharp
using Cooney.AI.Tools;
using Microsoft.Extensions.AI;

// Create a chat client (example with OllamaSharp)
var chatClient = new ChatClientBuilder(new OllamaApiClient(
    new Uri("http://127.0.0.1:11434/"),
    "gpt-oss:20b"))
    .UseFunctionInvocation()
    .Build();

// Create the ReadFile tool
var readFileTool = new ReadFile();

// Configure chat options with the tool
var chatOptions = new ChatOptions
{
    Tools = [readFileTool]
};

// Ask the AI to analyze a file
List<ChatMessage> chatHistory =
[
    new ChatMessage(ChatRole.System,
        "You are a code analysis assistant. Use the read_file tool to analyze code structure."),
    new ChatMessage(ChatRole.User,
        "Please read the file at 'C:\\MyProject\\Program.cs' and explain what it does.")
];

// Get AI response (the AI will automatically call the read_file tool)
var response = new StringBuilder();
await foreach (ChatResponseUpdate item in chatClient.GetStreamingResponseAsync(
    chatHistory,
    chatOptions))
{
    response.Append(item.Text);
}

Console.WriteLine(response.ToString());
```

#### Chunked Reading for Large Files

The ReadFile tool supports reading large files in chunks using `offset` and `limit` parameters:

```csharp
using Cooney.AI.Tools;
using Microsoft.Extensions.AI;

var readFileTool = new ReadFile();

var chatOptions = new ChatOptions
{
    Tools = [readFileTool]
};

List<ChatMessage> chatHistory =
[
    new ChatMessage(ChatRole.System,
        "You are a file analysis assistant. Use the read_file tool with offset and limit " +
        "parameters to read large files in chunks. If was_truncated is true, read the next chunk."),
    new ChatMessage(ChatRole.User,
        "Read the large log file at 'C:\\Logs\\application.log' and find any error messages.")
];

// The AI will automatically use chunking if needed
await foreach (ChatResponseUpdate item in chatClient.GetStreamingResponseAsync(
    chatHistory,
    chatOptions))
{
    Console.Write(item.Text);
}
```

#### Tool Parameters

The `read_file` function accepts the following parameters:

- **file_path** (required): Absolute path to the file to read
- **offset** (optional): Line number to start reading from (0-based, default: 0)
- **limit** (optional): Maximum number of lines to read (max: 10000)

#### Response Format

The tool returns a JSON response with:

```json
{
    "success": true,
    "content": "file content here...",
    "lines_read": 150,
    "total_lines": 500,
    "was_truncated": true,
    "offset": 0,
    "limit": 150
}
```

If an error occurs:

```json
{
    "success": false,
    "error": "File not found: C:\\path\\to\\file.txt",
    "error_type": "FileNotFound"
}
```

#### Error Types

- `InvalidParameter`: Invalid parameter values
- `FileNotFound`: File does not exist
- `AccessDenied`: Insufficient permissions to read the file
- `IOException`: I/O error during file reading
- `UnexpectedError`: Other unexpected errors

#### Strategy for Large Files

When working with large files, the AI model should:

1. Call `read_file` with a `limit` (e.g., 1000 lines) to get the start of the file
2. Check if `was_truncated` is `true` to determine if the file is large
3. Read subsequent chunks by calling `read_file` again with an `offset` (e.g., offset=1000, limit=1000)
4. Continue until all relevant content is found or the entire file is read

#### Security Features

- Requires absolute file paths (relative paths are rejected)
- Returns structured error messages for permission issues
- Enforces maximum line limit (10,000 lines) to prevent memory issues

#### Example Integration Tests

For complete working examples of the ReadFile tool in action, see the integration tests at [Cooney.AI.Test/Integration/ReadFileToolTests.cs](../Cooney.AI.Test/Integration/ReadFileToolTests.cs), which demonstrate:

- AI models analyzing code structure using the ReadFile tool
- Chunked reading of large files with offset/limit parameters
- Error handling for invalid file paths

### Checking Server Health

```csharp
using Cooney.AI;

var client = new ComfyUIApiClient("http://127.0.0.1:8188");

// Check if server is available
var systemStats = await client.GetSystemStatsAsync();
if (systemStats != null)
{
    Console.WriteLine("ComfyUI server is online");
    Console.WriteLine(systemStats.ToJsonString(
        new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
}
else
{
    Console.WriteLine("ComfyUI server is not available");
}
```

## API Reference

### Tools Namespace

#### ReadFile

AI function tool for reading file contents with chunking support.

```csharp
public class ReadFile : AIFunction
{
    public override string Name => "read_file";

    public override string Description =>
        "Use read_file to read the content of a file. It's designed to handle large files safely. " +
        "By default, it reads from the beginning of the file. " +
        "Use offset (line number) and limit (number of lines) to read specific parts or chunks of a file. " +
        "This is efficient for exploring large files.";
}
```

**Parameters:**
- `file_path` (string, required): The absolute path to the file to read
- `offset` (integer, optional): The line number to start reading from (0-based, default: 0, minimum: 0)
- `limit` (integer, optional): The maximum number of lines to read (minimum: 1, maximum: 10,000)

**Returns:** `JsonObject` with the following structure:

Success response:
```csharp
{
    "success": true,
    "content": "file content...",
    "lines_read": 100,
    "total_lines": 500,
    "was_truncated": true,
    "offset": 0,
    "limit": 100  // Only included if limit was specified
}
```

Error response:
```csharp
{
    "success": false,
    "error": "Error message",
    "error_type": "ErrorType"  // InvalidParameter, FileNotFound, AccessDenied, IOException, UnexpectedError
}
```

**Usage Notes:**
- The tool reads entire file content into memory, then applies offset/limit
- Supports .NET Standard 2.0 compatibility with async file reading
- Validates file paths must be absolute (rejects relative paths)
- Maximum line limit of 10,000 to prevent memory issues
- Returns `was_truncated: true` when more lines are available beyond the limit

### ComfyUIApiClient

Main client class for interacting with a ComfyUI server.

```csharp
public class ComfyUIApiClient : IImageGenerator
{
    public ComfyUIApiClient(string serverAddress = "http://127.0.0.1:8188");

    // Image Generation (IImageGenerator implementation)
    Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    // Server Information
    Task<JsonNode?> GetSystemStatsAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetNodeTypesAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetModelsAsync(CancellationToken cancellationToken = default);

    // Image Management
    Task<(string name, string subfolder, string type)> UploadImageAsync(
        byte[] imageData,
        string filename,
        bool overwrite = true,
        string? subfolder = null,
        CancellationToken cancellationToken = default);
}
```

**Constructor:**
- `serverAddress`: The ComfyUI server URL (default: `http://127.0.0.1:8188`)

**Methods:**

#### `GenerateAsync()`
Generates an image using ComfyUI.

**Parameters:**
- `request`: The image generation request containing the prompt
- `options`: Optional generation options (model, size, steps, etc.)
- `cancellationToken`: Cancellation token

**Returns:** `ImageGenerationResponse` with the generated image in `RawRepresentation`

**Notes:**
- Uses the Flux workflow by default
- Custom workflows can be provided via `options.AdditionalProperties["workflow"]`
- Polls the server every 3 seconds for completion
- Times out after 240 seconds (4 minutes)

#### `GetSystemStatsAsync()`
Retrieves system statistics from the ComfyUI server.

**Returns:** `JsonNode` with system stats, or `null` if the server is unavailable

**Usage:** Health checks and monitoring

#### `GetNodeTypesAsync()`
Gets the list of available node types on the server.

**Returns:** List of node type names (e.g., "KSampler", "CheckpointLoaderSimple")

**Usage:** Discovering available workflow nodes for custom workflow creation

#### `GetModelsAsync()`
Gets all available models across all model folders.

**Returns:** List of model filenames (e.g., "flux1-dev-fp8.safetensors")

**Notes:** Excludes the "custom_nodes" folder

#### `UploadImageAsync()`
Uploads an image to the ComfyUI server for use in workflows.

**Parameters:**
- `imageData`: The image bytes
- `filename`: Desired filename on the server
- `overwrite`: Whether to overwrite existing files (default: true)
- `subfolder`: Optional subfolder path
- `cancellationToken`: Cancellation token

**Returns:** Tuple containing the uploaded file's name, subfolder, and type

**Usage:** Uploading reference images for img2img or controlnet workflows

### DefaultImageGenerationOptions

Default configuration for image generation.

```csharp
public static readonly ImageGenerationOptions DefaultImageGenerationOptions = new()
{
    AdditionalProperties = new()
    {
        { "workflow", null },                   // Custom workflow (null = use default)
        { "steps", 20 },                        // Number of sampling steps
        { "cfg_scale", 1.0 },                   // CFG scale
        { "guidance_scale", 3.5 },              // Guidance scale (Flux-specific)
        { "seed", -1 },                         // Random seed (-1 = random)
    },
    Count = 1,                                  // Number of images to generate
    ImageSize = new(1024, 1024),               // Output image dimensions
    MediaType = "image/png",                    // Output format
    ModelId = "flux1-dev-fp8.safetensors",     // Model checkpoint to use
    ResponseFormat = ImageGenerationResponseFormat.Data,
};
```

### IWorkflow

Interface for defining custom ComfyUI workflows.

```csharp
public interface IWorkflow
{
    string Name { get; }
    JsonNode Build();
}
```

**Implementation:**
- `Name`: A descriptive name for the workflow
- `Build()`: Returns the JSON structure representing the ComfyUI workflow

### Workflow

Basic implementation of `IWorkflow` that wraps a `JsonNode`.

```csharp
public sealed class Workflow(string name, JsonNode jsonNode) : IWorkflow
{
    public string Name { get; }
    public JsonNode Build() => jsonNode;
}
```

## Workflow Source Generation

The library includes a source generator that creates strongly-typed workflow classes from JSON files.

### How It Works

1. Place ComfyUI workflow JSON files in a `Workflows/` folder in your project
2. Add them as `<AdditionalFiles>` in your `.csproj`:
   ```xml
   <ItemGroup>
       <AdditionalFiles Include="Workflows\*.json" />
   </ItemGroup>
   ```
3. The source generator automatically creates C# classes implementing `IWorkflow`

### Generated Workflow Classes

For a workflow file named `MyCustomWorkflow.json`, the generator creates:

```csharp
namespace Cooney.AI.Workflows.Generated
{
    public sealed class MycustomworkflowWorkflow : IWorkflow
    {
        public string Name => "MyCustomWorkflow";

        // Constructor with parameters extracted from the workflow JSON
        public MycustomworkflowWorkflow(/* parameters */)
        {
            // ...
        }

        public JsonNode Build()
        {
            // Returns the workflow JSON with parameter values substituted
        }
    }
}
```

### Using Generated Workflows

```csharp
using Cooney.AI;
using Cooney.AI.Workflows.Generated;
using Microsoft.Extensions.AI;

var client = new ComfyUIApiClient();

// Use a generated workflow
var options = ComfyUIApiClient.DefaultImageGenerationOptions.Clone();
options.AdditionalProperties!["workflow"] = new Text2imageWanWorkflow(
    "reference_image.png",
    "My custom prompt");

var response = await client.GenerateAsync(
    new ImageGenerationRequest { Prompt = "ignored when using custom workflow" },
    options);
```

## Usage Examples

### Example 1: Batch Image Generation

```csharp
using Cooney.AI;
using Microsoft.Extensions.AI;

var client = new ComfyUIApiClient();

var prompts = new[]
{
    "A serene forest landscape",
    "A futuristic space station",
    "An ancient temple ruins"
};

foreach (var (prompt, index) in prompts.Select((p, i) => (p, i)))
{
    Console.WriteLine($"Generating image {index + 1}/{prompts.Length}: {prompt}");

    var response = await client.GenerateAsync(new ImageGenerationRequest
    {
        Prompt = prompt
    });

    var imageData = (byte[])response.RawRepresentation!;
    await File.WriteAllBytesAsync($"output_{index}.png", imageData);
}
```

### Example 2: Uploading and Using Reference Images

```csharp
using Cooney.AI;

var client = new ComfyUIApiClient();

// Upload a reference image
var referenceImage = await File.ReadAllBytesAsync("reference.png");
var (name, subfolder, type) = await client.UploadImageAsync(
    referenceImage,
    "reference.png",
    overwrite: true);

Console.WriteLine($"Uploaded: {name} (subfolder: {subfolder}, type: {type})");

// Use the uploaded image in a custom workflow
// (requires a workflow that accepts input images)
```

### Example 3: Discovering Available Resources

```csharp
using Cooney.AI;

var client = new ComfyUIApiClient();

// Get available models
var models = await client.GetModelsAsync();
Console.WriteLine($"Available models ({models.Count}):");
foreach (var model in models.OrderBy(m => m))
{
    Console.WriteLine($"  - {model}");
}

// Get available node types
var nodeTypes = await client.GetNodeTypesAsync();
Console.WriteLine($"\nAvailable node types ({nodeTypes.Count}):");
foreach (var nodeType in nodeTypes.OrderBy(n => n).Take(10))
{
    Console.WriteLine($"  - {nodeType}");
}
Console.WriteLine($"  ... and {nodeTypes.Count - 10} more");
```

### Example 4: Custom Workflow Definition

```csharp
using Cooney.AI;
using Microsoft.Extensions.AI;
using System.Text.Json.Nodes;

var client = new ComfyUIApiClient();

// Define a custom workflow programmatically
var customWorkflow = new Workflow("Custom Simple Workflow", JsonNode.Parse("""
{
    "1": {
        "inputs": {
            "ckpt_name": "my-model.safetensors"
        },
        "class_type": "CheckpointLoaderSimple"
    },
    "2": {
        "inputs": {
            "text": "my prompt here",
            "clip": ["1", 1]
        },
        "class_type": "CLIPTextEncode"
    }
}
""")!);

var options = ComfyUIApiClient.DefaultImageGenerationOptions.Clone();
options.AdditionalProperties!["workflow"] = customWorkflow;

var response = await client.GenerateAsync(
    new ImageGenerationRequest { Prompt = "ignored" },
    options);
```

### Example 5: Error Handling and Retries

```csharp
using Cooney.AI;
using Microsoft.Extensions.AI;

var client = new ComfyUIApiClient();

const int maxRetries = 3;
var retryDelay = TimeSpan.FromSeconds(5);

for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        Console.WriteLine($"Attempt {attempt}/{maxRetries}");

        var response = await client.GenerateAsync(new ImageGenerationRequest
        {
            Prompt = "A mountain landscape at sunset"
        });

        var imageData = (byte[])response.RawRepresentation!;
        await File.WriteAllBytesAsync("output.png", imageData);

        Console.WriteLine("Success!");
        break;
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Network error: {ex.Message}");
        if (attempt < maxRetries)
        {
            Console.WriteLine($"Retrying in {retryDelay.TotalSeconds} seconds...");
            await Task.Delay(retryDelay);
        }
    }
    catch (TimeoutException ex)
    {
        Console.WriteLine($"Generation timeout: {ex.Message}");
        if (attempt < maxRetries)
        {
            Console.WriteLine($"Retrying in {retryDelay.TotalSeconds} seconds...");
            await Task.Delay(retryDelay);
        }
    }
}
```

## Integration with Microsoft.Extensions.AI

This library implements the `Microsoft.Extensions.AI` abstractions, allowing it to be used interchangeably with other AI services.

```csharp
using Microsoft.Extensions.AI;
using Cooney.AI;

// Use as IImageGenerator
IImageGenerator generator = new ComfyUIApiClient();

var response = await generator.GenerateAsync(new ImageGenerationRequest
{
    Prompt = "A futuristic cityscape"
});
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.AI;
using Cooney.AI;

var services = new ServiceCollection();

// Register as IImageGenerator
services.AddSingleton<IImageGenerator>(sp =>
    new ComfyUIApiClient("http://localhost:8188"));

var serviceProvider = services.BuildServiceProvider();

// Resolve and use
var generator = serviceProvider.GetRequiredService<IImageGenerator>();
var response = await generator.GenerateAsync(new ImageGenerationRequest
{
    Prompt = "A serene lake at dawn"
});
```

## ComfyUI Workflow Structure

ComfyUI workflows are defined as JSON objects where each key is a node ID and each value defines the node's inputs and type.

### Basic Workflow Structure

```json
{
    "1": {
        "inputs": {
            "ckpt_name": "model.safetensors"
        },
        "class_type": "CheckpointLoaderSimple",
        "_meta": {
            "title": "Load Checkpoint"
        }
    },
    "2": {
        "inputs": {
            "text": "prompt text here",
            "clip": ["1", 1]
        },
        "class_type": "CLIPTextEncode",
        "_meta": {
            "title": "Encode Prompt"
        }
    }
}
```

### Node Connections

Nodes reference other nodes' outputs using arrays: `["node_id", output_index]`

Example: `"clip": ["1", 1]` means "use output index 1 from node 1"

### Built-in Flux Workflow

The library includes a default Flux workflow that handles:
- Checkpoint loading
- CLIP text encoding (positive and negative prompts)
- Empty latent image creation
- KSampler for denoising
- VAE decoding
- Image saving

## Performance Considerations

### Polling Behavior

- The client polls the server every **3 seconds** by default
- Maximum wait time is **240 seconds** (4 minutes)
- Polling is inefficient for very short or very long generations

### Recommendations

1. **For production use**: Consider implementing WebSocket support for real-time progress updates
2. **For batch processing**: Queue multiple prompts sequentially rather than in parallel to avoid overloading the server
3. **For long-running workflows**: The current timeout may be insufficient for complex generations

### Resource Management

```csharp
// Dispose the client when done
using var client = new ComfyUIApiClient();

// Or explicitly
var client = new ComfyUIApiClient();
try
{
    // Use client
}
finally
{
    ((IDisposable)client).Dispose();
}
```

## Limitations

### Current Implementation Limitations

1. **No WebSocket support**: The library uses HTTP polling instead of ComfyUI's WebSocket API for progress updates
2. **Single image output**: Only retrieves the first generated image if multiple outputs exist
3. **Fixed polling interval**: Cannot customize the 3-second polling interval
4. **No progress callbacks**: Cannot monitor generation progress in real-time
5. **Basic error handling**: Limited error context from failed workflows

### Known Issues

- Negative prompts may not work with all models/workflows
- The Flux workflow is hardcoded and may not support all Flux model variants
- Upload subfolder handling depends on server configuration

## Troubleshooting

### Server Connection Issues

```csharp
var client = new ComfyUIApiClient("http://127.0.0.1:8188");
var stats = await client.GetSystemStatsAsync();

if (stats == null)
{
    Console.WriteLine("Cannot connect to ComfyUI server");
    Console.WriteLine("Make sure ComfyUI is running on http://127.0.0.1:8188");
}
```

### Generation Timeouts

If generations consistently timeout:
- Check if the server is processing the workflow (check ComfyUI logs)
- Verify the model is loaded and available
- Consider if the workflow is too complex for the 4-minute timeout

### Invalid Workflow Errors

If workflow execution fails:
- Use `GetNodeTypesAsync()` to verify required nodes are available
- Use `GetModelsAsync()` to verify the model exists
- Check ComfyUI server logs for detailed error messages

## References

### ComfyUI
- [ComfyUI GitHub](https://github.com/comfyanonymous/ComfyUI)
- [ComfyUI API Documentation](https://github.com/comfyanonymous/ComfyUI/wiki/API)

### Microsoft.Extensions.AI
- [Microsoft.Extensions.AI NuGet Package](https://www.nuget.org/packages/Microsoft.Extensions.AI.Abstractions/)
- [AI Abstractions Documentation](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.ai)

### Flux Models
- [Flux.1 by Black Forest Labs](https://blackforestlabs.ai/)
