namespace DevChat.Test;

/// <summary>
/// Manages WinAppDriver lifetime once for the entire test assembly,
/// so individual test classes can create and tear down app sessions
/// without restarting the driver between classes.
/// </summary>
[TestClass]
public static class GlobalSetup
{
	[AssemblyInitialize]
	public static void AssemblyInit(TestContext _) => DevChatSession.StartWinAppDriver();

	[AssemblyCleanup]
	public static void AssemblyCleanup() => DevChatSession.StopWinAppDriver();
}
