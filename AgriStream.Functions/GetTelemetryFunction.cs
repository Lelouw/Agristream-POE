using Azure.Data.Tables;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class GetTelemetryFunction(StorageHelper storage, ILogger<GetTelemetryFunction> logger)
{
    private const int DefaultTop = 20;
    private const int MaxTop = 100;

    [Function("GetTelemetry")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        string? fieldId = req.Query["fieldId"];
        var top = int.TryParse(req.Query["top"], out var requested)
            ? Math.Clamp(requested, 1, MaxTop)
            : DefaultTop;

        try
        {
            var filter = string.IsNullOrWhiteSpace(fieldId)
                ? null
                : TableClient.CreateQueryFilter($"PartitionKey eq {fieldId}");

            // Rows are stored newest first inside a field, so a single field needs no sorting.
            var rows = new List<TableEntity>();
            await foreach (var row in storage.Telemetry.QueryAsync<TableEntity>(filter, maxPerPage: top))
            {
                rows.Add(row);
                if (rows.Count >= (filter is null ? MaxTop * 2 : top)) break;
            }

            var readings = rows
                .Select(r => new
                {
                    fieldId = r.PartitionKey,
                    sensorId = r.GetString("SensorId"),
                    soilMoisture = r.GetDouble("SoilMoisture"),
                    temperatureC = r.GetDouble("TemperatureC"),
                    ph = r.GetDouble("Ph"),
                    readingAt = r.GetDateTimeOffset("ReadingAt")
                })
                .OrderByDescending(r => r.readingAt)
                .Take(top)
                .ToList();

            return new OkObjectResult(readings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not read telemetry for field {FieldId}.", fieldId);
            return new ObjectResult("Telemetry could not be retrieved.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
    }
}
