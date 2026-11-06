using System.Text.Json;
using AgriStream.Functions.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class TriggerIrrigationFunction(StorageHelper storage, ILogger<TriggerIrrigationFunction> logger)
{
    private const int MaxDurationMinutes = 120;

    [Function("TriggerIrrigation")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        IrrigationRequest? request;
        try
        {
            request = await JsonSerializer.DeserializeAsync<IrrigationRequest>(req.Body, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return new BadRequestObjectResult("The irrigation request is not in the correct format.");
        }

        if (request is null || string.IsNullOrWhiteSpace(request.DeviceId))
        {
            return new BadRequestObjectResult("A device id is required.");
        }

        if (request.DurationMinutes is < 1 or > MaxDurationMinutes)
        {
            return new BadRequestObjectResult($"Duration must be between 1 and {MaxDurationMinutes} minutes.");
        }

        var command = new IrrigationCommand
        {
            CommandId = $"IRR-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            DeviceId = request.DeviceId.Trim(),
            FieldName = request.FieldName.Trim(),
            DurationMinutes = request.DurationMinutes,
            RequestedBy = string.IsNullOrWhiteSpace(request.RequestedBy) ? "sensor" : request.RequestedBy,
            Reason = request.Reason,
            RequestedAt = DateTimeOffset.UtcNow
        };

        try
        {
            await storage.IrrigationQueue.SendMessageAsync(JsonSerializer.Serialize(command, JsonSerializerOptions.Web));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not queue irrigation command for device {DeviceId}.", command.DeviceId);
            return new ObjectResult("The irrigation command could not be queued.") { StatusCode = StatusCodes.Status500InternalServerError };
        }

        logger.LogInformation("Irrigation command {CommandId} queued for {DeviceId}.", command.CommandId, command.DeviceId);
        return new ObjectResult(new { commandId = command.CommandId, status = "Queued" }) { StatusCode = StatusCodes.Status202Accepted };
    }
}
