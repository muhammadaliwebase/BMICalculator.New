namespace BMICalculator.Core.Models;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}

public class ApiListResponse<T>
{
    public bool IsSuccess { get; set; }
    public List<T>? Data { get; set; }
    public int TotalCount { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
}

public class HaveIdResponse
{
    public long Id { get; set; }
}
