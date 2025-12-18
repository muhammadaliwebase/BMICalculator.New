namespace BMICalculator.Core.Models;

public class TurnstilePersonDto
{
    public string Id { get; set; } = string.Empty;
    public string? EmployeeNo { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? MidName { get; set; }
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? PhotoBase64 { get; set; }

    public string FullName => $"{LastName} {Name} {MidName}".Trim();
}
