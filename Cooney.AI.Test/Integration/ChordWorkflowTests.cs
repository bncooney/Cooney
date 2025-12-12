using Cooney.AI.ComfyUI;
using Cooney.AI.Workflows.Generated;
using Microsoft.Extensions.AI;
using System.Reflection;

namespace Cooney.AI.Test.Integration;

[TestClass]
public class ChordWorkflowTests
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
	public async Task ChordWorkflowImageGenerationTest()
	{
		var prompt = "texture of large new broken rocks with moss, top down view, seamless tileable";
		var options = new ComfyUIImageGenerationOptions(
			new ChordSdxlT2iImageToMaterialWorkflow(prompt, Random.Shared.Next().ToString())
		);

		var result = await _client.GenerateAsync(new ImageGenerationRequest
		{
			Prompt = prompt,
		}, options, cancellationToken: TestContext!.CancellationToken);

		Assert.IsNotNull(result);

		string assemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
		string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
		string tempDirPath = Path.Combine(Path.GetTempPath(), assemblyName, timestamp);
		Directory.CreateDirectory(tempDirPath);

		foreach (var image in result.Contents.OfType<DataContent>())
		{
			Assert.IsNotNull(image.Name);

			string imagePath = Path.Combine(tempDirPath, image.Name);
			await File.WriteAllBytesAsync(imagePath, image.Data, TestContext.CancellationToken);
			TestContext.WriteLine($"Generated image saved to: {imagePath}");
		}
	}
}
