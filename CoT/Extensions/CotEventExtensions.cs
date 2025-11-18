using Atakmap.Commoncommo.Protobuf.V1;

namespace CoT.Extensions;

/// <summary>
/// Extension methods for working with CoT events.
/// </summary>
public static class CotEventExtensions
{
	extension(CotEvent cotEvent)
	{
		/// <summary>
		/// Checks if the CoT event is stale (past its stale time).
		/// </summary>
		public bool IsStale()
		{
			if (cotEvent.StaleTime == 0)
				return false;

			var staleTime = DateTimeOffset.FromUnixTimeMilliseconds((long)cotEvent.StaleTime);
			return DateTimeOffset.UtcNow > staleTime;
		}

		/// <summary>
		/// Gets the send time as a DateTimeOffset.
		/// </summary>
		public DateTimeOffset GetSendTime()
		{
			return cotEvent.SendTime == 0
				? DateTimeOffset.MinValue
				: DateTimeOffset.FromUnixTimeMilliseconds((long)cotEvent.SendTime);
		}

		/// <summary>
		/// Gets the start time as a DateTimeOffset.
		/// </summary>
		public DateTimeOffset GetStartTime()
		{
			return cotEvent.StartTime == 0
				? DateTimeOffset.MinValue
				: DateTimeOffset.FromUnixTimeMilliseconds((long)cotEvent.StartTime);
		}

		/// <summary>
		/// Gets the stale time as a DateTimeOffset.
		/// </summary>
		public DateTimeOffset GetStaleTime()
		{
			return cotEvent.StaleTime == 0
				? DateTimeOffset.MaxValue
				: DateTimeOffset.FromUnixTimeMilliseconds((long)cotEvent.StaleTime);
		}

		/// <summary>
		/// Gets the time remaining until the event becomes stale.
		/// </summary>
		public TimeSpan GetTimeUntilStale()
		{
			if (cotEvent.StaleTime == 0)
				return TimeSpan.MaxValue;

			var staleTime = DateTimeOffset.FromUnixTimeMilliseconds((long)cotEvent.StaleTime);
			var remaining = staleTime - DateTimeOffset.UtcNow;

			return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
		}

		/// <summary>
		/// Checks if the CoT event has valid geographic coordinates.
		/// </summary>
		public bool HasValidCoordinates()
		{
			return cotEvent.Lat >= -90 && cotEvent.Lat <= 90 &&
				   cotEvent.Lon >= -180 && cotEvent.Lon <= 180;
		}

		/// <summary>
		/// Checks if the height above ellipsoid is valid (not the unknown value).
		/// </summary>
		public bool HasValidHeight()
		{
			return cotEvent.Hae != 999999.0;
		}

		/// <summary>
		/// Checks if circular error is valid (not the unknown value).
		/// </summary>
		public bool HasValidCircularError()
		{
			return cotEvent.Ce != 999999.0;
		}

		/// <summary>
		/// Checks if linear error is valid (not the unknown value).
		/// </summary>
		public bool HasValidLinearError()
		{
			return cotEvent.Le != 999999.0;
		}

		/// <summary>
		/// Gets a formatted string representation of the event type.
		/// CoT type codes follow format: atom-attitude-function-...
		/// </summary>
		public string GetTypeDescription()
		{
			if (string.IsNullOrEmpty(cotEvent.Type))
				return "Unknown";

			var parts = cotEvent.Type.Split('-');
			if (parts.Length < 3)
				return cotEvent.Type;

			var atom = parts[0] == "a" ? "Atom" : "Unknown";
			var attitude = GetAttitudeDescription(parts[1]);
			var function = parts.Length > 2 ? parts[2] : "Unknown";

			return $"{atom}/{attitude}/{function}";
		}

		/// <summary>
		/// Gets the callsign from the contact detail, if available.
		/// </summary>
		public string GetCallsign()
		{
			return cotEvent.Detail?.Contact?.Callsign ?? string.Empty;
		}

		/// <summary>
		/// Gets the endpoint from the contact detail, if available.
		/// </summary>
		public string GetEndpoint()
		{
			return cotEvent.Detail?.Contact?.Endpoint ?? string.Empty;
		}

		/// <summary>
		/// Gets the group name from the group detail, if available.
		/// </summary>
		public string GetGroupName()
		{
			return cotEvent.Detail?.Group?.Name ?? string.Empty;
		}

		/// <summary>
		/// Gets the group role from the group detail, if available.
		/// </summary>
		public string GetGroupRole()
		{
			return cotEvent.Detail?.Group?.Role ?? string.Empty;
		}

		/// <summary>
		/// Gets the battery level from the status detail, if available.
		/// </summary>
		public uint? GetBatteryLevel()
		{
			return cotEvent.Detail?.Status?.Battery;
		}

		/// <summary>
		/// Gets the speed from the track detail, if available.
		/// </summary>
		public double? GetSpeed()
		{
			return cotEvent.Detail?.Track?.Speed;
		}

		/// <summary>
		/// Gets the course from the track detail, if available.
		/// </summary>
		public double? GetCourse()
		{
			return cotEvent.Detail?.Track?.Course;
		}

		/// <summary>
		/// Creates a formatted summary string of the CoT event.
		/// </summary>
		public string ToSummaryString()
		{
			var callsign = cotEvent.GetCallsign();
			var callsignStr = !string.IsNullOrEmpty(callsign) ? $" ({callsign})" : string.Empty;

			return $"[{cotEvent.Type}] {cotEvent.Uid}{callsignStr} @ {cotEvent.Lat:F6}, {cotEvent.Lon:F6} " +
				   $"(Stale: {cotEvent.GetStaleTime():yyyy-MM-dd HH:mm:ss})";
		}
	}

	/// <summary>
	/// Gets the attitude portion of the type code.
	/// </summary>
	private static string GetAttitudeDescription(string attitude)
	{
		switch (attitude)
		{
			case "f":
				return "Friendly";
			case "h":
				return "Hostile";
			case "n":
				return "Neutral";
			case "u":
				return "Unknown";
			case "p":
				return "Pending";
			case "a":
				return "Assumed Friend";
			case "s":
				return "Suspect";
			case "j":
				return "Joker";
			case "k":
				return "Faker";
			default:
				return attitude;
		}
	}

	/// <summary>
	/// Creates a simple position update CoT event.
	/// </summary>
	public static CotEvent CreatePositionUpdate(
		string uid,
		string type,
		double lat,
		double lon,
		double hae = 0,
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
			Hae = hae,
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
					Endpoint = string.Empty
				}
			};
		}

		return cotEvent;
	}
}
