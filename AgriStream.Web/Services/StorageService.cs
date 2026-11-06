using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AgriStream.Web.Models;

namespace AgriStream.Web.Services;

public class StorageService
{
    private const string UploadedByKey = "uploadedBy";

    private readonly BlobContainerClient _images;
    private readonly TableClient _telemetry;
    private readonly TableClient _audit;

    public StorageService(IConfiguration configuration)
    {
        var connection = configuration["Storage:ConnectionString"]
            ?? throw new InvalidOperationException("Storage:ConnectionString is not configured.");

        _images = new BlobContainerClient(connection, "drone-images");

        var tables = new TableServiceClient(connection);
        _telemetry = tables.GetTableClient("Telemetry");
        _audit = tables.GetTableClient("ImageAudit");
    }

    public async Task InitAsync()
    {
        await _images.CreateIfNotExistsAsync();
        await _telemetry.CreateIfNotExistsAsync();
        await _audit.CreateIfNotExistsAsync();
    }

    public async Task<string> UploadImageAsync(IFormFile file, string uploadedBy)
    {
        var blobName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType },
            Metadata = new Dictionary<string, string> { [UploadedByKey] = uploadedBy }
        };

        await using var stream = file.OpenReadStream();
        await _images.GetBlobClient(blobName).UploadAsync(stream, options);
        return blobName;
    }

    public async Task<IReadOnlyList<DroneImage>> ListImagesAsync()
    {
        var images = new List<DroneImage>();
        await foreach (var blob in _images.GetBlobsAsync(new GetBlobsOptions { Traits = BlobTraits.Metadata }))
        {
            blob.Metadata.TryGetValue(UploadedByKey, out var uploadedBy);
            images.Add(new DroneImage(
                blob.Name,
                blob.Properties.ContentLength ?? 0,
                blob.Properties.CreatedOn,
                uploadedBy ?? "unknown"));
        }

        return images.OrderByDescending(i => i.UploadedAt).ToList();
    }

    public async Task<(Stream Content, string ContentType)?> OpenImageAsync(string blobName)
    {
        var blob = _images.GetBlobClient(blobName);
        if (!await blob.ExistsAsync())
        {
            return null;
        }

        var download = await blob.DownloadStreamingAsync();
        return (download.Value.Content, download.Value.Details.ContentType);
    }

    // Stands in for the field sensors by writing random readings straight to table storage.
    public async Task AddSimulatedReadingsAsync(string fieldId, int count)
    {
        var random = Random.Shared;
        var now = DateTimeOffset.UtcNow;
        var actions = new List<TableTransactionAction>();

        for (var i = 0; i < count; i++)
        {
            var readingAt = now.AddSeconds(-i * 30);
            var rowKey = $"{DateTime.MaxValue.Ticks - readingAt.UtcTicks:D19}-{Guid.NewGuid():N}";
            actions.Add(new TableTransactionAction(TableTransactionActionType.Add, new TableEntity(fieldId, rowKey)
            {
                { "SensorId", $"SENSOR-{random.Next(1, 6):D2}" },
                { "SoilMoisture", Math.Round(random.NextDouble() * 50 + 10, 1) },
                { "TemperatureC", Math.Round(random.NextDouble() * 23 + 15, 1) },
                { "Ph", Math.Round(random.NextDouble() * 2 + 5.5, 1) },
                { "ReadingAt", readingAt }
            }));
        }

        await _telemetry.SubmitTransactionAsync(actions);
    }

    public async Task<IReadOnlyList<ImageAuditRecord>> GetAuditAsync(int top = 50)
    {
        var records = new List<ImageAuditRecord>();
        await foreach (var row in _audit.QueryAsync<TableEntity>(maxPerPage: top))
        {
            records.Add(new ImageAuditRecord(
                row.GetString("FileName"),
                row.GetInt64("SizeBytes") ?? 0,
                row.GetString("UploadedBy"),
                row.GetString("Status"),
                row.GetDateTimeOffset("ProcessedAt") ?? default));

            if (records.Count >= top)
            {
                break;
            }
        }

        return records;
    }
}
