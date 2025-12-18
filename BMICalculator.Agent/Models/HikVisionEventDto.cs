using System.Text.Json.Serialization;

namespace BMICalculator.Agent.Models;

/// <summary>
/// HikVision FaceID event data structure
/// </summary>
public class HikVisionEventDto
{
    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("dateTime")]
    public DateTime? DateTime { get; set; }

    [JsonPropertyName("activePostCount")]
    public int? ActivePostCount { get; set; }

    [JsonPropertyName("eventType")]
    public string? EventType { get; set; }

    [JsonPropertyName("eventState")]
    public string? EventState { get; set; }

    [JsonPropertyName("eventDescription")]
    public string? EventDescription { get; set; }

    [JsonPropertyName("AccessControllerEvent")]
    public AccessControllerEventDto? AccessControllerEvent { get; set; }
}

public class AccessControllerEventDto
{
    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("majorEventType")]
    public int? MajorEventType { get; set; }

    [JsonPropertyName("subEventType")]
    public int? SubEventType { get; set; }

    [JsonPropertyName("employeeNoString")]
    public string? EmployeeNoString { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("cardReaderNo")]
    public int? CardReaderNo { get; set; }

    [JsonPropertyName("doorNo")]
    public int? DoorNo { get; set; }

    [JsonPropertyName("verifyNo")]
    public int? VerifyNo { get; set; }

    [JsonPropertyName("currentVerifyMode")]
    public string? CurrentVerifyMode { get; set; }

    [JsonPropertyName("serialNo")]
    public int? SerialNo { get; set; }

    [JsonPropertyName("userType")]
    public string? UserType { get; set; }

    [JsonPropertyName("currentEvent")]
    public bool? CurrentEvent { get; set; }

    [JsonPropertyName("pictureURL")]
    public string? PictureUrl { get; set; }
}
