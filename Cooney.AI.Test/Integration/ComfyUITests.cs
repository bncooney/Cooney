using Cooney.AI.ComfyUI;
using Cooney.AI.Workflows.Generated;
using Microsoft.Extensions.AI;
using System.Reflection;

namespace Cooney.AI.Test.Integration;

[TestClass]
public sealed class ComfyUITests
{
	private const string ServerAddress = "http://127.0.0.1:8188";
	private static ComfyUIApiClient _client = null!;
	private static bool _isComfyUIAvailable;

	public TestContext? TestContext { get; set; }

	[ClassInitialize]
	public static async Task ClassSetup(TestContext context)
	{
		_client = new ComfyUIApiClient(ServerAddress);
		var systemStats = await _client.GetSystemStatsAsync(context.CancellationToken);
		_isComfyUIAvailable = systemStats != null;
	}

	[TestInitialize]
	public void TestSetup()
	{
		if (!_isComfyUIAvailable)
			Assert.Inconclusive($"ComfyUI server is not available at {ServerAddress}");
	}

	[TestMethod]
	[Priority(0)]
	public async Task ServerHealthCheck()
	{
		var systemStats = await _client.GetSystemStatsAsync(TestContext!.CancellationToken);

		Assert.IsNotNull(systemStats, "System stats should be available");

		TestContext.WriteLine($"ComfyUI server is available at {ServerAddress}");
		TestContext.WriteLine("System Stats:");
		TestContext.WriteLine(systemStats!.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
	}

	[TestMethod]
	[Priority(1)]
	public async Task ImageGenerationTest()
	{
		var image = await _client.GenerateAsync(new ImageGenerationRequest
		{
			Prompt = "A beautiful landscape painting of mountains during sunset.",
		}, cancellationToken: TestContext!.CancellationToken);

		Assert.IsNotNull(image);
		Assert.IsNotNull(image.RawRepresentation);

		string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
		string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
		string tempDirPath = Path.Combine(Path.GetTempPath(), assemblyName, timestamp);
		Directory.CreateDirectory(tempDirPath);

		string filePath = Path.Combine(tempDirPath, "generated_image.png");
		await File.WriteAllBytesAsync(filePath, (byte[])image.RawRepresentation, TestContext.CancellationToken);

		TestContext.AddResultFile(filePath);
	}

	[TestMethod]
	[Priority(1)]
	public async Task Text2VideoTest()
	{
		var prompt = "White numerals flip over on a blue background.";
		var options = ComfyUIApiClient.DefaultImageGenerationOptions.Clone();
		options.AdditionalProperties!["workflow"] = new Text2imageWanWorkflow("Square_512_512x512.png", prompt);
		options.ImageSize = new(512, 512);

		var video = await _client.GenerateAsync(new ImageGenerationRequest
		{
			Prompt = prompt,
		}, options, cancellationToken: TestContext!.CancellationToken);

		Assert.IsNotNull(video);
		Assert.IsNotNull(video.RawRepresentation);

		string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
		string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
		string tempDirPath = Path.Combine(Path.GetTempPath(), assemblyName, timestamp);
		Directory.CreateDirectory(tempDirPath);

		string filePath = Path.Combine(tempDirPath, "generated_image.png");
		await File.WriteAllBytesAsync(filePath, (byte[])video.RawRepresentation, TestContext.CancellationToken);

		TestContext.AddResultFile(filePath);
	}

	[TestMethod]
	[Priority(1)]
	[DataRow("HD_1280x720.png")]
	[DataRow("Square_512_512x512.png")]
	public async Task ImageUploadTest(string filename)
	{
		// Get the Data folder path relative to the output directory
		string dataFolderPath = Path.Combine(AppContext.BaseDirectory, "Data");
		string imagePath = Path.Combine(dataFolderPath, filename);

		Assert.IsTrue(File.Exists(imagePath), $"Test image file not found: {imagePath}");

		// Read the image data
		byte[] imageData = await File.ReadAllBytesAsync(imagePath, TestContext!.CancellationToken);

		Assert.IsNotNull(imageData);
		Assert.IsNotEmpty(imageData, "Image data should not be empty");

		// Upload the image
		var (name, subfolder, type) = await _client.UploadImageAsync(
			imageData,
			filename,
			overwrite: true,
			subfolder: null,
			cancellationToken: TestContext.CancellationToken);

		// Verify the upload response
		Assert.IsNotNull(name, "Uploaded image name should not be null");
		Assert.IsFalse(string.IsNullOrEmpty(name), "Uploaded image name should not be empty");
		Assert.AreEqual("input", type, "Uploaded image type should be 'input'");

		TestContext.WriteLine($"Successfully uploaded: {filename}");
		TestContext.WriteLine($"Server filename: {name}");
		TestContext.WriteLine($"Subfolder: {subfolder}");
		TestContext.WriteLine($"Type: {type}");
	}

	[TestMethod]
	[Priority(1)]
	public async Task GetNodeTypesTest()
	{
		var nodeTypes = await _client.GetNodeTypesAsync(TestContext!.CancellationToken);

		Assert.IsNotNull(nodeTypes, "Node types list should not be null");
		Assert.IsNotEmpty(nodeTypes, "Node types list should not be empty");

		TestContext.WriteLine($"Retrieved {nodeTypes.Count} node types");
		TestContext.WriteLine("Node Types:");
		foreach (var nodeType in nodeTypes.OrderBy(n => n))
		{
			TestContext.WriteLine($"  - {nodeType}");
		}
	}

	[TestMethod]
	[Priority(1)]
	public async Task GetModelsTest()
	{
		var models = await _client.GetModelsAsync(TestContext!.CancellationToken);

		Assert.IsNotNull(models, "Models list should not be null");
		Assert.IsNotEmpty(models, "Models list should not be empty");

		TestContext.WriteLine($"Retrieved {models.Count} models");
		TestContext.WriteLine("Models:");
		foreach (var model in models.OrderBy(m => m))
		{
			TestContext.WriteLine($"  - {model}");
		}
	}
}
