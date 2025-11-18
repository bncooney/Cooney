using Microsoft.Extensions.AI;
using System.Reflection;

namespace AI.Test.Integration;

[TestClass]
public sealed class ComfyUITests
{
	public TestContext? TestContext { get; set; }

	[TestMethod]
	public async Task ImageGenerationTest()
	{
		IImageGenerator client = new ComfyUIApiClient("http://127.0.0.1:8188");

		var image = await client.GenerateAsync(new ImageGenerationRequest
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
}
