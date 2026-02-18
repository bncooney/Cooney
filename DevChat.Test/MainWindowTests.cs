using OpenQA.Selenium;
using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace DevChat.Test;

[TestClass]
public class MainWindowTests
{
	private static WindowsDriver<WindowsElement> Driver => DevChatSession.Driver;

	[ClassInitialize]
	public static void ClassInit(TestContext _) => DevChatSession.Setup("--test");

	[ClassCleanup]
	public static void ClassCleanup() => DevChatSession.TearDown();

	[TestInitialize]
	public void ResetAppState()
	{
		//// Delete all conversations so each test starts from a clean slate.
		//var list = Driver.FindElementByAccessibilityId("ConversationList");
		//while (true)
		//{
		//	var items = list.FindElementsByClassName("ListBoxItem");
		//	if (items.Count == 0)
		//		break;

		//	new Actions(Driver).ContextClick(items[0]).Perform();
		//	Driver.FindElementByAccessibilityId("DeleteChat").Click();
		//}

		//// Ensure nav pane is open.
		//Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);
		//var navButtons = Driver.FindElementsByAccessibilityId("NewChat");
		//Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
		//if (navButtons.Count == 0)
		//	ActivateButton(Driver.FindElementByAccessibilityId("ToggleNavPane"));
	}

	// ------------------------------------------------------------------
	//  Helpers
	// ------------------------------------------------------------------

	/// <summary>
	/// Activates a WPF button via the Space key. WinAppDriver's <c>.Click()</c>
	/// sends a simulated mouse click that does not reliably invoke WPF commands;
	/// the Space key triggers <c>ButtonBase.OnClick</c> directly.
	/// </summary>
	private static void ActivateButton(WindowsElement button) => button.SendKeys(" ");

	private static readonly TimeSpan DefaultWait = TimeSpan.FromSeconds(5);

	/// <summary>
	/// Creates a new chat and sends a message, returning the input element
	/// for further interaction. Waits until the input is cleared (meaning
	/// the send cycle has started) before returning.
	/// </summary>
	private static WindowsElement CreateChatAndSend(string message)
	{
		ActivateButton(Driver.FindElementByAccessibilityId("NewChat"));
		var input = Driver.FindElementByAccessibilityId("MessageInput");
		input.SendKeys(message);
		input.SendKeys(Keys.Enter);
		new WebDriverWait(Driver, DefaultWait).Until(_ => input.Text == string.Empty);
		return input;
	}

	// ------------------------------------------------------------------
	//  Launch & window basics
	// ------------------------------------------------------------------

	[TestMethod]
	public void Window_IsInitialised_ByDefault()
	{
		var newChatButton = Driver.FindElementByAccessibilityId("NewChat");
		Assert.IsTrue(newChatButton.Displayed);
	}

	// ------------------------------------------------------------------
	//  New chat
	// ------------------------------------------------------------------

	[TestMethod]
	public void NewChat_ShowsChatView()
	{
		ActivateButton(Driver.FindElementByAccessibilityId("NewChat"));

		var input = Driver.FindElementByAccessibilityId("MessageInput");
		Assert.IsTrue(input.Displayed, "Chat view should be shown after creating a new chat.");

		var list = Driver.FindElementByAccessibilityId("ConversationList");
		var items = list.FindElementsByClassName("ListBoxItem");
		Assert.IsNotEmpty(items, "New conversation should appear in the list.");
	}

	// ------------------------------------------------------------------
	//  Sending messages
	// ------------------------------------------------------------------

	[TestMethod]
	public void SendMessage_ShowsResponseAndClearsInput()
	{
		var input = CreateChatAndSend("Hello from test");

		// Input box should be cleared after send (confirmed by helper).
		Assert.AreEqual(string.Empty, input.Text,
			"Message input should be cleared after sending.");

		// Each message renders a FlowDocumentScrollViewer.
		// After one exchange: user message + assistant response = 2 viewers.
		new WebDriverWait(Driver, DefaultWait).Until(
			d => d.FindElements(By.ClassName("FlowDocumentScrollViewer")).Count >= 2);
	}

	[TestMethod]
	public void SendMessage_PreservesNewLines_InChatBubble()
	{
		ActivateButton(Driver.FindElementByAccessibilityId("NewChat"));
		var input = Driver.FindElementByAccessibilityId("MessageInput");

		// Type a multiline message using Shift+Enter for the line break.
		input.SendKeys("Line one" + Keys.Shift + Keys.Enter + Keys.Shift + "Line two");
		input.SendKeys(Keys.Enter);

		new WebDriverWait(Driver, DefaultWait).Until(_ => input.Text == string.Empty);

		// Wait for the user message bubble to render, then verify both lines
		// appear and are NOT collapsed into a single "Line one Line two" string
		// (which is what happens when Markdown soft-breaks are used).
		var viewers = new WebDriverWait(Driver, DefaultWait).Until(
			d =>
			{
				var v = d.FindElements(By.ClassName("FlowDocumentScrollViewer"));
				return v.Count >= 1 ? v : null;
			});

		var userBubbleText = viewers?[0].Text ?? string.Empty;
		Assert.Contains("Line one", userBubbleText,
			$"User bubble should contain 'Line one', got '{userBubbleText}'.");
		Assert.Contains("Line two", userBubbleText,
			$"User bubble should contain 'Line two', got '{userBubbleText}'.");
		Assert.DoesNotContain("Line one Line two", userBubbleText,
			$"Lines should be separated by a line break, not a space. Got '{userBubbleText}'.");

		// The conversation title in the nav pane should be single-line.
		var list = Driver.FindElementByAccessibilityId("ConversationList");
		var firstItem = list.FindElementsByClassName("ListBoxItem")[0];
		var titleBlock = firstItem.FindElementByClassName("TextBlock");
		Assert.IsFalse(titleBlock.Text.Contains('\n'),
			$"Title should not contain newlines, got '{titleBlock.Text}'.");
	}

	// ------------------------------------------------------------------
	//  Navigation pane
	// ------------------------------------------------------------------

	[TestMethod]
	public void ToggleNavPane_HidesAndShowsNavigation()
	{
		var toggle = Driver.FindElementByAccessibilityId("ToggleNavPane");

		// Nav pane starts open.
		Assert.IsTrue(Driver.FindElementByAccessibilityId("NewChat").Displayed);

		// Collapse it.
		ActivateButton(toggle);

		// Collapsed elements are removed from the automation tree entirely,
		// so use a short timeout with FindElements (plural).
		Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromMilliseconds(500);
		try
		{
			var hidden = Driver.FindElementsByAccessibilityId("NewChat");
			Assert.IsEmpty(hidden, "Nav pane should be hidden after toggle.");
		}
		finally
		{
			// Always re-open so subsequent tests aren't affected.
			ActivateButton(toggle);
			Driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(5);
		}

		Assert.IsTrue(Driver.FindElementByAccessibilityId("NewChat").Displayed,
			"Nav pane should be visible again after second toggle.");
	}

	// ------------------------------------------------------------------
	//  Settings
	// ------------------------------------------------------------------

	[TestMethod]
	public void SettingsButton_ShowsSettingsView()
	{
		ActivateButton(Driver.FindElementByAccessibilityId("Settings"));

		var header = Driver.FindElementByAccessibilityId("SettingsHeader");
		Assert.IsTrue(header.Displayed);
		Assert.AreEqual("Settings", header.Text);

		var dbPath = Driver.FindElementByAccessibilityId("DatabasePath");
		Assert.IsTrue(dbPath.Displayed);
		Assert.EndsWith("devchat.db", dbPath.Text,
			$"Expected DB path ending with 'devchat.db', got '{dbPath.Text}'.");
	}

	// ------------------------------------------------------------------
	//  Conversation switching
	// ------------------------------------------------------------------

	[TestMethod]
	public void SwitchConversation_ShowsPreviousMessages()
	{
		// Create first chat with a known message.
		CreateChatAndSend("Alpha message");

		// Create second chat with a different message.
		CreateChatAndSend("Beta message");

		// Switch back to the first chat (second in list, since newest is first).
		var list = Driver.FindElementByAccessibilityId("ConversationList");
		var items = list.FindElementsByClassName("ListBoxItem");
		items[1].Click();

		// First chat's title should still be visible in the page after switching.
		new WebDriverWait(Driver, DefaultWait).Until(
			d => d.PageSource.Contains("Alpha message"));
	}

	// ------------------------------------------------------------------
	//  Delete
	// ------------------------------------------------------------------

	[TestMethod]
	public void DeleteChat_RemovesConversation_WhenContextMenuClicked()
	{
		// Create a new chat so there's something to delete.
		var newChatButton = Driver.FindElementByAccessibilityId("NewChat");
		ActivateButton(newChatButton);

		var list = Driver.FindElementByAccessibilityId("ConversationList");
		var items = list.FindElementsByClassName("ListBoxItem");
		Assert.IsNotEmpty(items, "Expected at least one conversation.");

		var countBefore = items.Count;

		// Right-click the first item to open the context menu.
		var target = items[0];
		new Actions(Driver).ContextClick(target).Perform();

		var deleteItem = Driver.FindElementByAccessibilityId("DeleteChat");
		deleteItem.Click();

		// Verify the item was removed.
		var itemsAfter = list.FindElementsByClassName("ListBoxItem");
		Assert.HasCount(countBefore - 1, itemsAfter,
			"Conversation was not removed from the list.");
	}

}
