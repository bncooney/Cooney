using Atakmap.Commoncommo.Protobuf.V1;

namespace CoT.Test.TestInfrastructure;

/// <summary>
/// Factory for creating test CoT messages.
/// </summary>
public static class TestDataFactory
{
	public static CotEvent CreateTestCotEvent(
		string uid = "TEST-UID-001",
		string type = "a-f-G-E-V",
		double lat = 37.7749,
		double lon = -122.4194,
		string? callsign = null,
		TimeSpan? ttl = null)
	{
		var now = DateTimeOffset.UtcNow;
		var staleTime = ttl.HasValue ? now.Add(ttl.Value) : now.AddMinutes(5);

		var cotEvent = new CotEvent
		{
			Uid = uid,
			Type = type,
			How = "m-g",
			Lat = lat,
			Lon = lon,
			Hae = 10.0,
			Ce = 999999.0,
			Le = 999999.0,
			SendTime = (ulong)now.ToUnixTimeMilliseconds(),
			StartTime = (ulong)now.ToUnixTimeMilliseconds(),
			StaleTime = (ulong)staleTime.ToUnixTimeMilliseconds()
		};

		if (!string.IsNullOrEmpty(callsign))
		{
			cotEvent.Detail = new Detail
			{
				Contact = new Contact
				{
					Callsign = callsign,
					Endpoint = "tcp://192.168.1.1:8087"
				}
			};
		}

		return cotEvent;
	}

	public static TakMessage CreateTestTakMessage(CotEvent? cotEvent = null)
	{
		var now = DateTimeOffset.UtcNow;

		return new TakMessage
		{
			TakControl = new TakControl
			{
				MinProtoVersion = 1,
				MaxProtoVersion = 1
			},
			CotEvent = cotEvent ?? CreateTestCotEvent(),
			CreationTime = (ulong)now.ToUnixTimeMilliseconds(),
			SubmissionTime = (ulong)now.ToUnixTimeMilliseconds()
		};
	}

	public static CotEvent CreateStaleCotEvent()
	{
		var pastTime = DateTimeOffset.UtcNow.AddMinutes(-10);

		return new CotEvent
		{
			Uid = "STALE-UID-001",
			Type = "a-f-G-E-V",
			How = "m-g",
			Lat = 37.7749,
			Lon = -122.4194,
			Hae = 10.0,
			Ce = 999999.0,
			Le = 999999.0,
			SendTime = (ulong)pastTime.ToUnixTimeMilliseconds(),
			StartTime = (ulong)pastTime.ToUnixTimeMilliseconds(),
			StaleTime = (ulong)pastTime.AddMinutes(1).ToUnixTimeMilliseconds() // Stale 9 minutes ago
		};
	}

	public static CotEvent CreateDetailedCotEvent()
	{
		var cotEvent = CreateTestCotEvent(callsign: "Alpha-1");

		cotEvent.Detail = new Detail
		{
			Contact = new Contact
			{
				Callsign = "Alpha-1",
				Endpoint = "tcp://192.168.1.100:8087"
			},
			Group = new Group
			{
				Name = "Team Alpha",
				Role = "Team Leader"
			},
			Track = new Track
			{
				Speed = 5.5,
				Course = 270.0
			},
			Status = new Status
			{
				Battery = 75
			},
			Takv = new Takv
			{
				Device = "Samsung S20",
				Platform = "ATAK",
				Os = "Android 12",
				Version = "4.5.1"
			},
			PrecisionLocation = new PrecisionLocation
			{
				Geopointsrc = "GPS",
				Altsrc = "GPS"
			}
		};

		return cotEvent;
	}
}
