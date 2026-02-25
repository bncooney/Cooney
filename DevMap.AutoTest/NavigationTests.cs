using System.Globalization;
using Cooney.AutoTest;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Support.UI;

namespace DevMap.AutoTest;

[TestClass]
public class NavigationTests
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

	private static void ActivateButton(WindowsElement button) => button.SendKeys(" ");

	/// <summary>
	/// Opens the POI menu and clicks the specified menu item by AutomationId.
	/// </summary>
	private static void ClickPoiMenuItem(string automationId)
	{
		Driver.FindElementByName("POI").Click();
		Driver.FindElementByAccessibilityId(automationId).Click();
	}

	/// <summary>
	/// Parses a coordinate component like "48.8566° N" or "74.0060° W" into a signed double.
	/// </summary>
	private static bool TryParseCoordPart(string s, out double value)
	{
		value = 0;
		s = s.Trim();
		if (s.Length == 0)
			return false;

		double sign = 1;
		char dir = char.ToUpperInvariant(s[^1]);
		if (dir is 'S' or 'W')
			sign = -1;
		if (dir is 'N' or 'S' or 'E' or 'W')
			s = s[..^1].Trim();

		s = s.TrimEnd('\u00b0').Trim();
		if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double raw))
			return false;

		value = raw * sign;
		return true;
	}

	/// <summary>
	/// Waits for the CameraPosition text to settle near the expected coordinates.
	/// Handles format: "48.8566° N, 2.3522° E". Returns parsed [lat, lon].
	/// </summary>
	private static double[] WaitForCameraPosition(double expectedLat, double expectedLon, double tolerance = 1.0)
	{
		var pos = new WebDriverWait(Driver, DefaultWait).Until(_ =>
		{
			var element = Driver.FindElementByAccessibilityId("CameraPosition");
			if (string.IsNullOrWhiteSpace(element.Text))
				return null;

			var parts = element.Text.Split(',', StringSplitOptions.TrimEntries);
			if (parts.Length != 2)
				return null;

			if (!TryParseCoordPart(parts[0], out double lat) ||
				!TryParseCoordPart(parts[1], out double lon))
				return null;

			if (Math.Abs(lat - expectedLat) <= tolerance && Math.Abs(lon - expectedLon) <= tolerance)
				return new[] { lat, lon };

			return null;
		});

		Assert.IsNotNull(pos, $"Camera did not reach expected position ({expectedLat}, {expectedLon}).");
		return pos!;
	}

	// ------------------------------------------------------------------
	//  POI button tests
	// ------------------------------------------------------------------

	[TestMethod]
	public void GoToParis_UpdatesCameraPosition()
	{
		ClickPoiMenuItem("GoToParis");
		var pos = WaitForCameraPosition(48.8566, 2.3522);
		Assert.AreEqual(48.8566, pos[0], 1.0, $"Latitude should be near Paris, got {pos[0]}.");
		Assert.AreEqual(2.3522, pos[1], 1.0, $"Longitude should be near Paris, got {pos[1]}.");
	}

	[TestMethod]
	public void GoToNewYork_UpdatesCameraPosition()
	{
		ClickPoiMenuItem("GoToNewYork");
		var pos = WaitForCameraPosition(40.7128, -74.0060);
		Assert.AreEqual(40.7128, pos[0], 1.0, $"Latitude should be near New York, got {pos[0]}.");
		Assert.AreEqual(-74.0060, pos[1], 1.0, $"Longitude should be near New York, got {pos[1]}.");
	}

	[TestMethod]
	public void GoToSydney_UpdatesCameraPosition()
	{
		ClickPoiMenuItem("GoToSydney");
		var pos = WaitForCameraPosition(-33.8688, 151.2093);
		Assert.AreEqual(-33.8688, pos[0], 1.0, $"Latitude should be near Sydney, got {pos[0]}.");
		Assert.AreEqual(151.2093, pos[1], 1.0, $"Longitude should be near Sydney, got {pos[1]}.");
	}

	// ------------------------------------------------------------------
	//  Manual coordinate input test
	// ------------------------------------------------------------------

	[TestMethod]
	public void NavigateInput_GoesToCoordinates()
	{
		var input = Driver.FindElementByAccessibilityId("NavigateInput");
		input.Clear();
		input.SendKeys("48.8566, 2.3522");

		ActivateButton(Driver.FindElementByAccessibilityId("NavigateGo"));

		var pos = WaitForCameraPosition(48.8566, 2.3522);
		Assert.AreEqual(48.8566, pos[0], 1.0, $"Latitude should be near target, got {pos[0]}.");
		Assert.AreEqual(2.3522, pos[1], 1.0, $"Longitude should be near target, got {pos[1]}.");
	}
}
