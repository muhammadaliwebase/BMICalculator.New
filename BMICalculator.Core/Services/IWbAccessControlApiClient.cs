using BMICalculator.Core.Models;

namespace BMICalculator.Core.Services;

public interface IWbAccessControlApiClient
{
    /// <summary>
    /// Authenticates with the API and gets access token
    /// </summary>
    Task<bool> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Gets person by turnstile person ID (from FaceID scan)
    /// </summary>
    Task<TurnstilePersonDto?> GetPersonByIdAsync(string turnstilePersonId);

    /// <summary>
    /// Gets the latest BMI measurement for a person
    /// </summary>
    Task<BmiMeasurementDto?> GetLatestBmiByPersonIdAsync(string turnstilePersonId);

    /// <summary>
    /// Gets BMI measurement history for a person
    /// </summary>
    Task<List<BmiMeasurementDto>> GetBmiHistoryByPersonIdAsync(string turnstilePersonId, int? limit = null);

    /// <summary>
    /// Creates a new BMI measurement record
    /// </summary>
    Task<long?> CreateBmiMeasurementAsync(CreateBmiMeasurementDto dto);

    /// <summary>
    /// Gets all persons for FaceID sync
    /// </summary>
    Task<List<TurnstilePersonDto>> GetAllPersonsAsync();
}
