namespace BMICalculator.Core.Configuration;

public class ApiConfiguration
{
    public string BaseUrl { get; set; } = "https://localhost:7232";// "http://wbac-api.apptest.uz";
    public string? AccessToken { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
}
