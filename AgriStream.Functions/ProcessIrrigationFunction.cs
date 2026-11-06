using System.Text.Json;
using AgriStream.Functions.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class ProcessIrrigationFunction(ILogger<ProcessIrrigationFunction> logger)
{
    private const string UpdateSql =
        "UPDATE Devices SET Status = @status, LastCommand = @command, UpdatedAt = @updatedAt WHERE DeviceId = @deviceId";

    private const string InsertSql =
        "INSERT INTO Devices (DeviceId, Name, FieldName, Status, LastCommand, UpdatedAt) " +
        "VALUES (@deviceId, @deviceId, @fieldName, @status, @command, @updatedAt)";

    // Picks up queued commands and records the device state in the master registry table.
    [Function("ProcessIrrigationCommand")]
    public async Task Run(
        [QueueTrigger(StorageHelper.IrrigationQueueName, Connection = "AzureWebJobsStorage")] string message)
    {
        var command = JsonSerializer.Deserialize<IrrigationCommand>(message, JsonSerializerOptions.Web)
            ?? throw new InvalidOperationException("The queue message could not be read.");

        var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString")
            ?? throw new InvalidOperationException("SqlConnectionString is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var status = $"Irrigating ({command.DurationMinutes} min)";
        var summary = $"{command.CommandId} by {command.RequestedBy}";

        var updated = await ExecuteAsync(connection, UpdateSql, command, status, summary);
        if (updated == 0)
        {
            await ExecuteAsync(connection, InsertSql, command, status, summary);
        }

        logger.LogInformation("Device {DeviceId} set to '{Status}' by command {CommandId}.",
            command.DeviceId, status, command.CommandId);
    }

    private static async Task<int> ExecuteAsync(
        SqlConnection connection, string sql, IrrigationCommand command, string status, string summary)
    {
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@deviceId", command.DeviceId);
        cmd.Parameters.AddWithValue("@fieldName", command.FieldName);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@command", summary);
        cmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow);
        return await cmd.ExecuteNonQueryAsync();
    }
}
