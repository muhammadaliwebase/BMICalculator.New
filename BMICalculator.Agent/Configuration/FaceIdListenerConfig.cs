namespace BMICalculator.Agent.Configuration;

public class FaceIdListenerConfig
{
    /// <summary>
    /// Port number for HTTP listener (e.g., 8080)
    /// </summary>
    public int ListenPort { get; set; } = 8080;

    /// <summary>
    /// Endpoint path for HikVision callbacks (e.g., "/hikvision/listen")
    /// </summary>
    public string ListenEndpoint { get; set; } = "/hikvision/listen";

    /// <summary>
    /// Device ID to filter events (optional)
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Whether to enable the listener on startup
    /// </summary>
    public bool AutoStart { get; set; } = true;
}
