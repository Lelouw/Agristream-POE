using System.ComponentModel.DataAnnotations;

namespace AgriStream.Web.Models;

public class LoginViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password)), DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = Roles.Farmer;
}

public record TelemetryReading(
    string FieldId,
    string SensorId,
    double SoilMoisture,
    double TemperatureC,
    double Ph,
    DateTimeOffset ReadingAt);

public class IrrigationRequest
{
    public string DeviceId { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public record IrrigationResult(string CommandId, string Status);

public record DroneImage(string Name, long SizeBytes, DateTimeOffset? UploadedAt, string UploadedBy);

public record ImageAuditRecord(string FileName, long SizeBytes, string UploadedBy, string Status, DateTimeOffset ProcessedAt);

public class DashboardViewModel
{
    public string? Field { get; set; }
    public IReadOnlyList<string> Fields { get; set; } = [];
    public IReadOnlyList<TelemetryReading> Readings { get; set; } = [];
    public IReadOnlyList<Device> Devices { get; set; } = [];
    public string? Error { get; set; }
}
