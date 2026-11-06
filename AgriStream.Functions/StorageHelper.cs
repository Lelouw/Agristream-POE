using Azure.Data.Tables;
using Azure.Storage.Queues;

namespace AgriStream.Functions;

public class StorageHelper
{
    public const string IrrigationQueueName = "irrigation-commands";

    public TableClient Telemetry { get; }
    public TableClient ImageAudit { get; }
    public QueueClient IrrigationQueue { get; }

    public StorageHelper()
    {
        var connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
            ?? throw new InvalidOperationException("AzureWebJobsStorage is not configured.");

        var tables = new TableServiceClient(connection);
        Telemetry = tables.GetTableClient("Telemetry");
        ImageAudit = tables.GetTableClient("ImageAudit");
        IrrigationQueue = new QueueClient(connection, IrrigationQueueName);

        Telemetry.CreateIfNotExists();
        ImageAudit.CreateIfNotExists();
        IrrigationQueue.CreateIfNotExists();
    }

    // Newest rows sort first because table storage orders by RowKey ascending.
    public static string NewestFirstRowKey(DateTimeOffset when) =>
        $"{DateTime.MaxValue.Ticks - when.UtcTicks:D19}-{Guid.NewGuid():N}";
}
