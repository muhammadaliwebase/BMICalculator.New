using BMICalculator.Agent.Models;

namespace BMICalculator.Agent.Services;

public interface IFaceIdListenerService
{
    /// <summary>
    /// Event raised when a person is scanned
    /// </summary>
    event EventHandler<FaceIdScanEvent>? PersonScanned;

    /// <summary>
    /// Indicates if the listener is currently running
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Current listen URL
    /// </summary>
    string? ListenUrl { get; }

    /// <summary>
    /// Starts the HTTP listener
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stops the HTTP listener
    /// </summary>
    Task StopAsync();
}
