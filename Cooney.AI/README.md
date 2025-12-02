# Cooney.AI

> [!WARNING]
> This library is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A .NET Standard 2.1 library providing a client for ComfyUI's API, implementing the `Microsoft.Extensions.AI` abstractions for image generation. This library allows you to queue workflows, monitor their execution, and retrieve generated images from a ComfyUI server.

## Features

- **Microsoft.Extensions.AI integration** - Implements `IImageGenerator` for standardized AI service integration
- **ComfyUI API client** - Queue workflows, poll for completion, and download results
- **Workflow abstraction** - Define custom workflows or use the built-in Flux workflow
- **Source generation** - Automatically generates strongly-typed workflow classes from JSON files
- **Server management** - Query available models, node types, and system stats
- **Image upload/download** - Upload input images and retrieve generated outputs

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
