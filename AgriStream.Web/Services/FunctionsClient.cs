using System.Net.Http.Json;
using AgriStream.Web.Models;

namespace AgriStream.Web.Services;

// Calls the HTTP-triggered functions that sit behind the dashboard.
public class FunctionsClient(HttpClient http)
{
    public async Task<IReadOnlyList<TelemetryReading>> GetTelemetryAsync(string? fieldId, int top = 20)
    {
        var url = $"api/GetTelemetry?top={top}";
        if (!string.IsNullOrWhiteSpace(fieldId))
        {
            url += $"&fieldId={Uri.EscapeDataString(fieldId)}";
        }

        return await http.GetFromJsonAsync<List<TelemetryReading>>(url) ?? [];
    }

    public async Task<string> TriggerIrrigationAsync(IrrigationRequest request)
    {
        var response = await http.PostAsJsonAsync("api/TriggerIrrigation", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<IrrigationResult>();
        return result?.CommandId ?? string.Empty;
    }
}
