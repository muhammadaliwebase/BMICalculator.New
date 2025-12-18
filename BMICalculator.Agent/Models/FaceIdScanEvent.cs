namespace BMICalculator.Agent.Models;

/// <summary>
/// Event raised when a person is scanned by FaceID device
/// </summary>
public class FaceIdScanEvent
{
    public string PersonId { get; set; } = string.Empty;
    public string? EmployeeNo { get; set; }
    public string? Name { get; set; }
    public DateTime ScanTime { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceIp { get; set; }
    public byte[]? FaceImage { get; set; }
}
