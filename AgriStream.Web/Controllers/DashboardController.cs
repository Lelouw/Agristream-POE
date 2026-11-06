using AgriStream.Web.Data;
using AgriStream.Web.Models;
using AgriStream.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgriStream.Web.Controllers;

[Authorize]
public class DashboardController(
    AppDbContext db,
    FunctionsClient functions,
    StorageService storage,
    ILogger<DashboardController> logger) : Controller
{
    private static readonly string[] Fields = ["Field-A", "Field-B", "Field-C"];

    public async Task<IActionResult> Index(string? field)
    {
        var model = new DashboardViewModel
        {
            Field = field,
            Fields = Fields,
            Devices = await db.Devices.OrderBy(d => d.DeviceId).ToListAsync()
        };

        try
        {
            model.Readings = await functions.GetTelemetryAsync(field);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "The telemetry function could not be reached.");
            model.Error = "Telemetry is unavailable right now. Please try again shortly.";
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    public async Task<IActionResult> TriggerIrrigation(string deviceId, string fieldName, int durationMinutes)
    {
        var request = new IrrigationRequest
        {
            DeviceId = deviceId,
            FieldName = fieldName,
            DurationMinutes = durationMinutes,
            RequestedBy = User.Identity?.Name ?? string.Empty,
            Reason = "Manual trigger from dashboard"
        };

        try
        {
            var commandId = await functions.TriggerIrrigationAsync(request);
            TempData["Success"] = $"Irrigation command {commandId} queued for {deviceId}.";
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Irrigation command for {DeviceId} failed.", deviceId);
            TempData["Error"] = "The irrigation command could not be queued.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Farmer},{Roles.Admin}")]
    public async Task<IActionResult> SimulateReadings(string field, int count = 10)
    {
        if (!Fields.Contains(field))
        {
            return BadRequest("Unknown field.");
        }

        await storage.AddSimulatedReadingsAsync(field, Math.Clamp(count, 1, 100));
        TempData["Success"] = $"Added sensor readings for {field}.";
        return RedirectToAction(nameof(Index), new { field });
    }
}
