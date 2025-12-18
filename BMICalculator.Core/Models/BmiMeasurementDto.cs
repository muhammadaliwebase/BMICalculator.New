namespace BMICalculator.Core.Models;

public class BmiMeasurementDto
{
    public long Id { get; set; }
    public string TurnstilePersonId { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public decimal Bmi { get; set; }
    public string BmiCategory { get; set; } = string.Empty;
    public DateTime MeasuredAt { get; set; }
    public string? DeviceId { get; set; }
    public string? Notes { get; set; }
    public int StateId { get; set; }
}
