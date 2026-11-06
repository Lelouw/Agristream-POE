using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AgriStream.Functions;

public class DroneImageFunction(StorageHelper storage, ILogger<DroneImageFunction> logger)
{
    // Runs when a drone image lands in the drone-images container and records an audit row.
    [Function("ProcessDroneImage")]
    public async Task Run(
        [BlobTrigger("drone-images/{name}", Connection = "AzureWebJobsStorage")] BlobClient blob,
        string name)
    {
        var properties = (await blob.GetPropertiesAsync()).Value;
        properties.Metadata.TryGetValue("uploadedBy", out var uploadedBy);

        var processedAt = DateTimeOffset.UtcNow;
        var audit = new TableEntity("DroneImage", StorageHelper.NewestFirstRowKey(processedAt))
        {
            { "FileName", name },
            { "SizeBytes", properties.ContentLength },
            { "ContentType", properties.ContentType },
            { "UploadedBy", uploadedBy ?? "unknown" },
            { "Status", "Processed" },
            { "ProcessedAt", processedAt }
        };

        await storage.ImageAudit.AddEntityAsync(audit);
        logger.LogInformation("Drone image {Name} ({Size} bytes) audited.", name, properties.ContentLength);
    }
}
