using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace DevChat.Test;

[TestClass]
public class ChatViewTests
{
	private static WindowsDriver<WindowsElement> Driver => DevChatSession.Driver;

	[ClassInitialize]
	public static void ClassInit(TestContext _) => DevChatSession.Setup("--test");

	[ClassCleanup]
	public static void ClassCleanup() => DevChatSession.TearDown();

	// ------------------------------------------------------------------
	//  Helpers
	// ------------------------------------------------------------------

	private static void ActivateButton(WindowsElement button) => button.SendKeys(" ");

	private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(5);

	private static WindowsElement CreateChatAndSend(string message)
	{
		ActivateButton(Driver.FindElementByAccessibilityId("NewChat"));
		var input = Driver.FindElementByAccessibilityId("MessageInput");
		input.SendKeys(message);
		input.SendKeys(Keys.Enter);
		new WebDriverWait(Driver, DefaultWait).Until(_ => input.Text == string.Empty);
		return input;
	}

	/// <summary>
	/// Waits for the assistant response to render, then returns the
	/// second FlowDocumentScrollViewer (the assistant bubble).
	/// </summary>
	private static WindowsElement WaitForAssistantBubble()
	{
		return new WebDriverWait(Driver, DefaultWait).Until(d =>
		{
			var viewers = d.FindElements(By.ClassName("FlowDocumentScrollViewer"));
			return viewers.Count >= 2 ? (WindowsElement)viewers[1] : null;
		})!;
	}

	// ------------------------------------------------------------------
	//  Raw mode toggle
	// ------------------------------------------------------------------

	[TestMethod]
	public void RawToggle_ShowsRawContent_WhenClicked()
	{
		CreateChatAndSend("Hello");
		WaitForAssistantBubble();

		// The toggle button uses Opacity=0 when not hovered, so it stays in
		// the automation tree. Find the assistant's button (second one) and
		// activate it via Space key (bypasses hit-test).
		var toggleButtons = Driver.FindElementsByAccessibilityId("ToggleRawMode");
		var toggleButton = toggleButtons.Skip(1).First();
		ActivateButton(toggleButton);

		// Only the assistant's RawContent is in the tree (user's stays Collapsed).
		var rawTextBox = new WebDriverWait(Driver, DefaultWait).Until(
			_ =>
			{
				var elements = Driver.FindElementsByAccessibilityId("RawContent");
				var first = elements.FirstOrDefault();
				return first is { Displayed: true } ? first : null;
			});
		Assert.IsNotNull(rawTextBox, "Raw content TextBox should be visible after toggling.");
		Assert.Contains("Test response", rawTextBox!.Text,
			$"Raw content should contain the assistant response, got '{rawTextBox.Text}'.");

		// Button should now read "Rendered" and be fully visible (raw mode trigger).
		Assert.AreEqual("Rendered", toggleButton.Text,
			"Button should show 'Rendered' text in raw mode.");

		// Click again to switch back to rendered mode.
		ActivateButton(toggleButton);

		// FlowDocumentScrollViewer should be visible again.
		new WebDriverWait(Driver, DefaultWait).Until(
			d => d.FindElements(By.ClassName("FlowDocumentScrollViewer")).Count >= 2);
	}
}
