using CoT.Extensions;
using CoT.Test.TestInfrastructure;
using Atakmap.Commoncommo.Protobuf.V1;

namespace CoT.Test.Extensions;

[TestClass]
public class CotEventExtensionsTests
{
	[TestMethod]
	public void IsStale_FutureStaleTime_ReturnsFalse()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent(ttl: TimeSpan.FromMinutes(5));

		// Act
		var isStale = cotEvent.IsStale();

		// Assert
		Assert.IsFalse(isStale);
	}

	[TestMethod]
	public void IsStale_PastStaleTime_ReturnsTrue()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateStaleCotEvent();

		// Act
		var isStale = cotEvent.IsStale();

		// Assert
		Assert.IsTrue(isStale);
	}

	[TestMethod]
	public void IsStale_ZeroStaleTime_ReturnsFalse()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent();
		cotEvent.StaleTime = 0;

		// Act
		var isStale = cotEvent.IsStale();

		// Assert
		Assert.IsFalse(isStale);
	}

	[TestMethod]
	public void GetSendTime_ValidTime_ReturnsCorrectDateTime()
	{
		// Arrange
		var now = DateTimeOffset.UtcNow;
		var cotEvent = new CotEvent
		{
			SendTime = (ulong)now.ToUnixTimeMilliseconds()
		};

		// Act
		var sendTime = cotEvent.GetSendTime();

		// Assert
		Assert.AreEqual(now.ToUnixTimeMilliseconds(), sendTime.ToUnixTimeMilliseconds());
	}

	[TestMethod]
	public void GetTimeUntilStale_FutureTime_ReturnsPositiveDuration()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent(ttl: TimeSpan.FromMinutes(10));

		// Act
		var timeUntilStale = cotEvent.GetTimeUntilStale();

		// Assert
		Assert.IsTrue(timeUntilStale > TimeSpan.Zero);
		Assert.IsTrue(timeUntilStale <= TimeSpan.FromMinutes(10));
	}

	[TestMethod]
	public void GetTimeUntilStale_PastTime_ReturnsZero()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateStaleCotEvent();

		// Act
		var timeUntilStale = cotEvent.GetTimeUntilStale();

		// Assert
		Assert.AreEqual(TimeSpan.Zero, timeUntilStale);
	}

	[TestMethod]
	public void HasValidCoordinates_ValidCoordinates_ReturnsTrue()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent(lat: 37.7749, lon: -122.4194);

		// Act
		var isValid = cotEvent.HasValidCoordinates();

		// Assert
		Assert.IsTrue(isValid);
	}

	[TestMethod]
	[DataRow(-91.0, 0.0)]
	[DataRow(91.0, 0.0)]
	[DataRow(0.0, -181.0)]
	[DataRow(0.0, 181.0)]
	public void HasValidCoordinates_InvalidCoordinates_ReturnsFalse(double lat, double lon)
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent(lat: lat, lon: lon);

		// Act
		var isValid = cotEvent.HasValidCoordinates();

		// Assert
		Assert.IsFalse(isValid);
	}

	[TestMethod]
	public void HasValidHeight_ValidHeight_ReturnsTrue()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent();
		cotEvent.Hae = 150.0;

		// Act
		var isValid = cotEvent.HasValidHeight();

		// Assert
		Assert.IsTrue(isValid);
	}

	[TestMethod]
	public void HasValidHeight_UnknownHeight_ReturnsFalse()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent();
		cotEvent.Hae = 999999.0;

		// Act
		var isValid = cotEvent.HasValidHeight();

		// Assert
		Assert.IsFalse(isValid);
	}

	[TestMethod]
	[DataRow("a-f-G-E-V", "Atom/Friendly/G")]
	[DataRow("a-h-A", "Atom/Hostile/A")]
	[DataRow("a-n-G", "Atom/Neutral/G")]
	[DataRow("a-u-G", "Atom/Unknown/G")]
	public void GetTypeDescription_ValidTypes_ReturnsCorrectDescription(string type, string expected)
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent(type: type);

		// Act
		var description = cotEvent.GetTypeDescription();

		// Assert
		Assert.AreEqual(expected, description);
	}

	[TestMethod]
	public void GetCallsign_WithContact_ReturnsCallsign()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent(callsign: "Alpha-1");

		// Act
		var callsign = cotEvent.GetCallsign();

		// Assert
		Assert.AreEqual("Alpha-1", callsign);
	}

	[TestMethod]
	public void GetCallsign_NoContact_ReturnsEmpty()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent();

		// Act
		var callsign = cotEvent.GetCallsign();

		// Assert
		Assert.AreEqual(string.Empty, callsign);
	}

	[TestMethod]
	public void GetBatteryLevel_WithStatus_ReturnsBattery()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateDetailedCotEvent();

		// Act
		var battery = cotEvent.GetBatteryLevel();

		// Assert
		Assert.IsNotNull(battery);
		Assert.AreEqual(75u, battery.Value);
	}

	[TestMethod]
	public void GetSpeed_WithTrack_ReturnsSpeed()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateDetailedCotEvent();

		// Act
		var speed = cotEvent.GetSpeed();

		// Assert
		Assert.IsNotNull(speed);
		Assert.AreEqual(5.5, speed.Value, 0.001);
	}

	[TestMethod]
	public void GetCourse_WithTrack_ReturnsCourse()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateDetailedCotEvent();

		// Act
		var course = cotEvent.GetCourse();

		// Assert
		Assert.IsNotNull(course);
		Assert.AreEqual(270.0, course.Value, 0.001);
	}

	[TestMethod]
	public void CreatePositionUpdate_ValidParameters_CreatesEvent()
	{
		// Arrange & Act
		var cotEvent = CotEventExtensions.CreatePositionUpdate(
			uid: "TEST-001",
			type: "a-f-G-E-V",
			lat: 37.7749,
			lon: -122.4194,
			hae: 100.0,
			callsign: "Test-Call",
			ttl: TimeSpan.FromMinutes(5));

		// Assert
		Assert.AreEqual("TEST-001", cotEvent.Uid);
		Assert.AreEqual("a-f-G-E-V", cotEvent.Type);
		Assert.AreEqual(37.7749, cotEvent.Lat, 0.000001);
		Assert.AreEqual(-122.4194, cotEvent.Lon, 0.000001);
		Assert.AreEqual(100.0, cotEvent.Hae, 0.001);
		Assert.AreEqual("Test-Call", cotEvent.Detail.Contact.Callsign);
		Assert.IsFalse(cotEvent.IsStale());
	}

	[TestMethod]
	public void ToSummaryString_ValidEvent_CreatesFormattedString()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateTestCotEvent(uid: "ABC-123", callsign: "Bravo-1");

		// Act
		var summary = cotEvent.ToSummaryString();

		// Assert
		Assert.Contains("ABC-123", summary);
		Assert.Contains("Bravo-1", summary);
		Assert.Contains("37.7749", summary);
		Assert.Contains("Stale:", summary);
	}

	[TestMethod]
	public void GetGroupName_WithGroup_ReturnsName()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateDetailedCotEvent();

		// Act
		var groupName = cotEvent.GetGroupName();

		// Assert
		Assert.AreEqual("Team Alpha", groupName);
	}

	[TestMethod]
	public void GetGroupRole_WithGroup_ReturnsRole()
	{
		// Arrange
		var cotEvent = TestDataFactory.CreateDetailedCotEvent();

		// Act
		var role = cotEvent.GetGroupRole();

		// Assert
		Assert.AreEqual("Team Leader", role);
	}
}
