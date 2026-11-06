namespace AgriStream.Functions.Models;

public class IrrigationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public class IrrigationCommand
{
    public string CommandId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
}
