using System.Globalization;
using Cooney.AutoTest;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.UI;

namespace DevMap.AutoTest;

[TestClass]
public class ZoomTests
{
	private static WindowsDriver<WindowsElement> Driver => AppSession.Driver;

	[ClassInitialize]
	public static void ClassInit(TestContext _) => AppSession.Setup();

	[ClassCleanup]
	public static void ClassCleanup() => AppSession.TearDown();

	// ------------------------------------------------------------------
	//  Helpers
	// ------------------------------------------------------------------

	private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(5);

	private static void ClickViewMenuItem(string automationId)
	{
		Driver.FindElementByName("View").Click();
		Driver.FindElementByAccessibilityId(automationId).Click();
	}

	private static void ClickPoiMenuItem(string automationId)
	{
		Driver.FindElementByName("POI").Click();
		Driver.FindElementByAccessibilityId(automationId).Click();
	}

	private static double GetZoomLevel()
	{
		var element = Driver.FindElementByAccessibilityId("ZoomLevel");
		if (double.TryParse(element.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double zoom))
			return zoom;
		return -1;
	}

	private static double WaitForZoom(double expectedZoom, double tolerance = 0.5)
	{
		var result = new WebDriverWait(Driver, DefaultWait).Until(_ =>
		{
			var current = GetZoomLevel();
			if (current < 0)
				return null;
			if (Math.Abs(current - expectedZoom) <= tolerance)
				return new[] { current };
			return null;
		});

		Assert.IsNotNull(result, $"Zoom did not reach expected level {expectedZoom}.");
		return result![0];
	}

	// ------------------------------------------------------------------
	//  Preset zoom tests
	// ------------------------------------------------------------------

	[TestMethod]
	public void ZoomToCity_SetsHighZoomLevel()
	{
		ClickViewMenuItem("ZoomToCity");
		var zoom = WaitForZoom(10.0);
		Assert.AreEqual(10.0, zoom, 0.5, $"Zoom should be ~10 (city), got {zoom}.");
	}

	[TestMethod]
	public void ZoomToRegion_SetsMidZoomLevel()
	{
		ClickViewMenuItem("ZoomToRegion");
		var zoom = WaitForZoom(6.0);
		Assert.AreEqual(6.0, zoom, 0.5, $"Zoom should be ~6 (region), got {zoom}.");
	}

	[TestMethod]
	public void ZoomToWorld_SetsLowZoomLevel()
	{
		ClickViewMenuItem("ZoomToWorld");
		var zoom = WaitForZoom(3.0);
		Assert.AreEqual(3.0, zoom, 0.5, $"Zoom should be ~3 (world), got {zoom}.");
	}

	// ------------------------------------------------------------------
	//  Incremental zoom tests
	// ------------------------------------------------------------------

	[TestMethod]
	public void ZoomIn_IncreasesZoomLevel()
	{
		// Start from a known zoom level
		ClickViewMenuItem("ZoomToRegion");
		WaitForZoom(6.0);
		var before = GetZoomLevel();

		ClickViewMenuItem("ZoomIn");
		Thread.Sleep(200); // Allow frame update

		var after = GetZoomLevel();
		Assert.IsGreaterThan(before, after, $"Zoom should increase after ZoomIn. Before: {before}, After: {after}.");
	}

	[TestMethod]
	public void ZoomOut_DecreasesZoomLevel()
	{
		// Start from a known zoom level
		ClickViewMenuItem("ZoomToRegion");
		WaitForZoom(6.0);
		var before = GetZoomLevel();

		ClickViewMenuItem("ZoomOut");
		Thread.Sleep(200); // Allow frame update

		var after = GetZoomLevel();
		Assert.IsLessThan(before, after, $"Zoom should decrease after ZoomOut. Before: {before}, After: {after}.");
	}

	// ------------------------------------------------------------------
	//  Combined POI + zoom test
	// ------------------------------------------------------------------

	[TestMethod]
	public void ZoomToCity_AtPoi_ShowsDetailedPosition()
	{
		ClickPoiMenuItem("GoToParis");
		ClickViewMenuItem("ZoomToCity");

		var zoom = WaitForZoom(10.0);
		Assert.AreEqual(10.0, zoom, 0.5, $"Zoom should be ~10, got {zoom}.");

		// Verify we're still at Paris
		var posElement = Driver.FindElementByAccessibilityId("CameraPosition");
		Assert.Contains("N", posElement.Text, $"Should show northern hemisphere, got '{posElement.Text}'.");
		Assert.Contains("E", posElement.Text, $"Should show eastern hemisphere, got '{posElement.Text}'.");
	}
}
