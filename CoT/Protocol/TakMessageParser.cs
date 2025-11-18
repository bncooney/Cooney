using System.Text;
using System.Xml.Linq;
using Atakmap.Commoncommo.Protobuf.V1;
using Google.Protobuf;

namespace CoT.Protocol;

/// <summary>
/// Parses and serializes TAK messages in both Protobuf and XML formats.
/// </summary>
public class TakMessageParser
{
	/// <summary>
	/// Parses a protobuf-encoded TAK message.
	/// </summary>
	/// <param name="payload">Protobuf payload bytes</param>
	/// <returns>Parsed TakMessage</returns>
	public TakMessage ParseProtobuf(byte[] payload)
	{
		if (payload == null || payload.Length == 0)
			throw new ArgumentException("Payload cannot be null or empty", nameof(payload));

		try
		{
			return TakMessage.Parser.ParseFrom(payload);
		}
		catch (InvalidProtocolBufferException ex)
		{
			throw new InvalidOperationException("Failed to parse protobuf message", ex);
		}
	}

	/// <summary>
	/// Parses a protobuf-encoded TAK message and extracts the CoT event if present.
	/// </summary>
	public CotEvent ParseProtobufCotEvent(byte[] payload)
	{
		var takMessage = ParseProtobuf(payload);
		return takMessage.CotEvent;
	}

	/// <summary>
	/// Parses an XML-encoded CoT event (legacy Version 0 format).
	/// </summary>
	/// <param name="payload">XML payload bytes</param>
	/// <returns>Parsed CotEvent (as best-effort conversion to protobuf model)</returns>
	public CotEvent ParseXml(byte[] payload)
	{
		if (payload == null || payload.Length == 0)
			throw new ArgumentException("Payload cannot be null or empty", nameof(payload));

		try
		{
			var xmlString = Encoding.UTF8.GetString(payload);
			return ParseXmlString(xmlString);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Failed to parse XML CoT message", ex);
		}
	}

	/// <summary>
	/// Parses an XML-encoded CoT event from a string.
	/// </summary>
	private CotEvent ParseXmlString(string xml)
	{
		var doc = XDocument.Parse(xml);
		var eventElement = doc.Element("event");

		if (eventElement == null)
			throw new InvalidOperationException("XML does not contain an 'event' root element");

		var cotEvent = new CotEvent
		{
			Type = (string)eventElement.Attribute("type") ?? string.Empty,
			Uid = (string)eventElement.Attribute("uid") ?? string.Empty,
			How = (string)eventElement.Attribute("how") ?? string.Empty,
			Access = (string)eventElement.Attribute("access") ?? string.Empty,
			Qos = (string)eventElement.Attribute("qos") ?? string.Empty,
			Opex = (string)eventElement.Attribute("opex") ?? string.Empty,
			Caveat = (string)eventElement.Attribute("caveat") ?? string.Empty,
			ReleaseableTo = (string)eventElement.Attribute("releaseableTo") ?? string.Empty
		};

		// Parse timestamps (ISO 8601 format to milliseconds since epoch)
		cotEvent.SendTime = ParseTimestamp((string)eventElement.Attribute("time"));
		cotEvent.StartTime = ParseTimestamp((string)eventElement.Attribute("start"));
		cotEvent.StaleTime = ParseTimestamp((string)eventElement.Attribute("stale"));

		// Parse point element
		var pointElement = eventElement.Element("point");
		if (pointElement != null)
		{
			cotEvent.Lat = (double?)pointElement.Attribute("lat") ?? 0.0;
			cotEvent.Lon = (double?)pointElement.Attribute("lon") ?? 0.0;
			cotEvent.Hae = (double?)pointElement.Attribute("hae") ?? 999999.0;
			cotEvent.Ce = (double?)pointElement.Attribute("ce") ?? 999999.0;
			cotEvent.Le = (double?)pointElement.Attribute("le") ?? 999999.0;
		}

		// Parse detail element
		var detailElement = eventElement.Element("detail");
		if (detailElement != null)
		{
			cotEvent.Detail = ParseDetailElement(detailElement);
		}

		return cotEvent;
	}

	/// <summary>
	/// Parses the detail element from XML and converts to protobuf Detail message.
	/// </summary>
	private Detail ParseDetailElement(XElement detailElement)
	{
		var detail = new Detail();

		// Parse contact
		var contactElement = detailElement.Element("contact");
		if (contactElement != null)
		{
			detail.Contact = new Contact
			{
				Endpoint = (string)contactElement.Attribute("endpoint") ?? string.Empty,
				Callsign = (string)contactElement.Attribute("callsign") ?? string.Empty
			};
		}

		// Parse group
		var groupElement = detailElement.Element("__group");
		if (groupElement != null)
		{
			detail.Group = new Group
			{
				Name = (string)groupElement.Attribute("name") ?? string.Empty,
				Role = (string)groupElement.Attribute("role") ?? string.Empty
			};
		}

		// Parse precision location
		var precisionLocationElement = detailElement.Element("precisionlocation");
		if (precisionLocationElement != null)
		{
			detail.PrecisionLocation = new PrecisionLocation
			{
				Geopointsrc = (string)precisionLocationElement.Attribute("geopointsrc") ?? string.Empty,
				Altsrc = (string)precisionLocationElement.Attribute("altsrc") ?? string.Empty
			};
		}

		// Parse status
		var statusElement = detailElement.Element("status");
		if (statusElement != null)
		{
			detail.Status = new Status
			{
				Battery = (uint?)statusElement.Attribute("battery") ?? 0
			};
		}

		// Parse takv
		var takvElement = detailElement.Element("takv");
		if (takvElement != null)
		{
			detail.Takv = new Takv
			{
				Device = (string)takvElement.Attribute("device") ?? string.Empty,
				Platform = (string)takvElement.Attribute("platform") ?? string.Empty,
				Os = (string)takvElement.Attribute("os") ?? string.Empty,
				Version = (string)takvElement.Attribute("version") ?? string.Empty
			};
		}

		// Parse track
		var trackElement = detailElement.Element("track");
		if (trackElement != null)
		{
			detail.Track = new Track
			{
				Speed = (double?)trackElement.Attribute("speed") ?? 0.0,
				Course = (double?)trackElement.Attribute("course") ?? 0.0
			};
		}

		// Store remaining XML as xmlDetail
		// Remove parsed elements and store the rest
		var clonedDetail = new XElement(detailElement);
		clonedDetail.Element("contact")?.Remove();
		clonedDetail.Element("__group")?.Remove();
		clonedDetail.Element("precisionlocation")?.Remove();
		clonedDetail.Element("status")?.Remove();
		clonedDetail.Element("takv")?.Remove();
		clonedDetail.Element("track")?.Remove();

		if (clonedDetail.HasElements || clonedDetail.HasAttributes)
		{
			detail.XmlDetail = clonedDetail.ToString(SaveOptions.DisableFormatting);
		}

		return detail;
	}

	/// <summary>
	/// Parses an ISO 8601 timestamp to milliseconds since Unix epoch.
	/// </summary>
	private ulong ParseTimestamp(string timestamp)
	{
		if (string.IsNullOrEmpty(timestamp))
			return 0;

		try
		{
			var dateTime = DateTimeOffset.Parse(timestamp);
			return (ulong)dateTime.ToUnixTimeMilliseconds();
		}
		catch
		{
			return 0;
		}
	}

	/// <summary>
	/// Serializes a TakMessage to protobuf bytes.
	/// </summary>
	public byte[] SerializeToProtobuf(TakMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		return message.ToByteArray();
	}

	/// <summary>
	/// Serializes a CotEvent to XML bytes (legacy format).
	/// </summary>
	public byte[] SerializeToXml(CotEvent cotEvent)
	{
		if (cotEvent == null)
			throw new ArgumentNullException(nameof(cotEvent));

		var eventElement = new XElement("event",
			new XAttribute("version", "2.0"),
			new XAttribute("uid", cotEvent.Uid),
			new XAttribute("type", cotEvent.Type),
			new XAttribute("time", FormatTimestamp(cotEvent.SendTime)),
			new XAttribute("start", FormatTimestamp(cotEvent.StartTime)),
			new XAttribute("stale", FormatTimestamp(cotEvent.StaleTime)),
			new XAttribute("how", cotEvent.How)
		);

		// Add optional attributes
		if (!string.IsNullOrEmpty(cotEvent.Access))
			eventElement.Add(new XAttribute("access", cotEvent.Access));
		if (!string.IsNullOrEmpty(cotEvent.Qos))
			eventElement.Add(new XAttribute("qos", cotEvent.Qos));
		if (!string.IsNullOrEmpty(cotEvent.Opex))
			eventElement.Add(new XAttribute("opex", cotEvent.Opex));

		// Add point element
		eventElement.Add(new XElement("point",
			new XAttribute("lat", cotEvent.Lat),
			new XAttribute("lon", cotEvent.Lon),
			new XAttribute("hae", cotEvent.Hae),
			new XAttribute("ce", cotEvent.Ce),
			new XAttribute("le", cotEvent.Le)
		));

		// Add detail element if present
		if (cotEvent.Detail != null)
		{
			eventElement.Add(SerializeDetailToXml(cotEvent.Detail));
		}

		var xml = eventElement.ToString(SaveOptions.DisableFormatting);
		return Encoding.UTF8.GetBytes(xml);
	}

	/// <summary>
	/// Serializes a Detail message to XML element.
	/// </summary>
	private XElement SerializeDetailToXml(Detail detail)
	{
		var detailElement = new XElement("detail");

		if (detail.Contact != null)
		{
			detailElement.Add(new XElement("contact",
				new XAttribute("endpoint", detail.Contact.Endpoint),
				new XAttribute("callsign", detail.Contact.Callsign)
			));
		}

		if (detail.Group != null)
		{
			detailElement.Add(new XElement("__group",
				new XAttribute("name", detail.Group.Name),
				new XAttribute("role", detail.Group.Role)
			));
		}

		if (detail.PrecisionLocation != null)
		{
			detailElement.Add(new XElement("precisionlocation",
				new XAttribute("geopointsrc", detail.PrecisionLocation.Geopointsrc),
				new XAttribute("altsrc", detail.PrecisionLocation.Altsrc)
			));
		}

		if (detail.Status != null)
		{
			detailElement.Add(new XElement("status",
				new XAttribute("battery", detail.Status.Battery)
			));
		}

		if (detail.Takv != null)
		{
			detailElement.Add(new XElement("takv",
				new XAttribute("device", detail.Takv.Device),
				new XAttribute("platform", detail.Takv.Platform),
				new XAttribute("os", detail.Takv.Os),
				new XAttribute("version", detail.Takv.Version)
			));
		}

		if (detail.Track != null)
		{
			detailElement.Add(new XElement("track",
				new XAttribute("speed", detail.Track.Speed),
				new XAttribute("course", detail.Track.Course)
			));
		}

		// Add xmlDetail if present
		if (!string.IsNullOrEmpty(detail.XmlDetail))
		{
			try
			{
				var xmlDetailElement = XElement.Parse("<root>" + detail.XmlDetail + "</root>");
				detailElement.Add(xmlDetailElement.Elements());
			}
			catch
			{
				// Ignore parsing errors for xmlDetail
			}
		}

		return detailElement;
	}

	/// <summary>
	/// Formats a Unix timestamp (milliseconds since epoch) to ISO 8601 format.
	/// </summary>
	private string FormatTimestamp(ulong millisecondsSinceEpoch)
	{
		if (millisecondsSinceEpoch == 0)
			return DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

		var dateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)millisecondsSinceEpoch);
		return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
	}
}
