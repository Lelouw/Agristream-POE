using System.ComponentModel.DataAnnotations;

namespace AgriStream.Web.Models;

// Master registry of field devices; the queue-triggered function keeps Status up to date.
public class Device
{
    [Key, StringLength(50)]
    public string DeviceId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string FieldName { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Status { get; set; } = "Idle";

    [StringLength(120)]
    public string? LastCommand { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
