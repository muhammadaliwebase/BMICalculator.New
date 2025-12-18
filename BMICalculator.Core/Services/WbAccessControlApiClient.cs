using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BMICalculator.Core.Configuration;
using BMICalculator.Core.Models;

namespace BMICalculator.Core.Services;

public class WbAccessControlApiClient : IWbAccessControlApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ApiConfiguration _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private string? _accessToken;

    public WbAccessControlApiClient(HttpClient httpClient, ApiConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
        _httpClient.BaseAddress = new Uri(config.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        try
        {
            var loginRequest = new { username, password };
            var response = await _httpClient.PostAsJsonAsync("/api/Auth/Login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions);
                if (result?.AccessToken != null)
                {
                    _accessToken = result.AccessToken;
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _accessToken);
                    return true;
                }
            }
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<TurnstilePersonDto?> GetPersonByIdAsync(string turnstilePersonId)
    {
        try
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync($"/api/TurnstilePerson/Get/{turnstilePersonId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TurnstilePersonDto>(_jsonOptions);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<BmiMeasurementDto?> GetLatestBmiByPersonIdAsync(string turnstilePersonId)
    {
        try
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync(
                $"/api/BmiMeasurement/GetLatestByPersonId/{turnstilePersonId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BmiMeasurementDto>(_jsonOptions);
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<BmiMeasurementDto>> GetBmiHistoryByPersonIdAsync(string turnstilePersonId, int? limit = null)
    {
        try
        {
            EnsureAuthenticated();
            var url = $"/api/BmiMeasurement/GetHistoryByPersonId/{turnstilePersonId}";
            if (limit.HasValue)
            {
                url += $"?limit={limit.Value}";
            }

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<BmiMeasurementDto>>(_jsonOptions);
                return result ?? new List<BmiMeasurementDto>();
            }
            return new List<BmiMeasurementDto>();
        }
        catch (Exception)
        {
            return new List<BmiMeasurementDto>();
        }
    }

    public async Task<long?> CreateBmiMeasurementAsync(CreateBmiMeasurementDto dto)
    {
        try
        {
            EnsureAuthenticated();
            var response = await _httpClient.PostAsJsonAsync("/api/BmiMeasurement/Create", dto);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<HaveIdResponse>(_jsonOptions);
                return result?.Id;
            }
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<List<TurnstilePersonDto>> GetAllPersonsAsync()
    {
        try
        {
            EnsureAuthenticated();
            var response = await _httpClient.GetAsync("/api/TurnstilePerson/GetList");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<TurnstilePersonDto>>(_jsonOptions);
                return result ?? new List<TurnstilePersonDto>();
            }
            return new List<TurnstilePersonDto>();
        }
        catch (Exception)
        {
            return new List<TurnstilePersonDto>();
        }
    }

    private void EnsureAuthenticated()
    {
        if (string.IsNullOrEmpty(_accessToken) && !string.IsNullOrEmpty(_config.AccessToken))
        {
            _accessToken = _config.AccessToken;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    private class AuthResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
