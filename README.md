# AgriStream IoT

MVC management application and serverless pipeline for AgriStream IoT (soil sensor telemetry, drone imagery, irrigation control).

## Solution layout

| Project | Purpose |
|---|---|
| `AgriStream.Web` | ASP.NET Core MVC app hosted on Azure App Service |
| `AgriStream.Functions` | Four Azure Functions (isolated worker, .NET 10) |
| `scripts/load-test.ps1` | Concurrent request script used for the load test |

## Azure services

| Task | Service | Used for |
|---|---|---|
| a | App Service | Hosts `AgriStream.Web` |
| b | Azure SQL Database | `Users` (username, password hash, role) and `Devices` (master registry) |
| c | Blob Storage | Container `drone-images` |
| c | Table Storage | `Telemetry` (sensor readings) and `ImageAudit` |
| d | Queue Storage | `irrigation-commands` |

## Functions

| Function | Trigger | Behaviour |
|---|---|---|
| `ProcessDroneImage` | Blob (`drone-images/{name}`) | Writes an audit row to the `ImageAudit` table |
| `GetTelemetry` | HTTP GET | Returns the latest readings from the `Telemetry` table |
| `TriggerIrrigation` | HTTP POST | Validates the request and queues an irrigation command |
| `ProcessIrrigationCommand` | Queue (`irrigation-commands`) | Updates the device status in the SQL `Devices` table |

## Roles

| Role | Access |
|---|---|
| Farmer | Dashboard, trigger irrigation, simulate readings, upload images |
| Agronomist | Dashboard (read only), upload images, image audit |
| Admin | Everything. Seeded as `admin`; set `SeedAdminPassword` in configuration to change the default password |

## Configuration

Web (`appsettings.json` or App Service settings):

- `ConnectionStrings:DefaultConnection` - Azure SQL connection string
- `Storage:ConnectionString` - storage account connection string
- `Functions:BaseUrl` - Function App URL, for example `https://<app>.azurewebsites.net/`
- `Functions:Key` - function key, sent as `x-functions-key`
- `SeedAdminPassword` - initial admin password

Functions (`local.settings.json` or Function App settings, see `local.settings.sample.json`):

- `AzureWebJobsStorage` - storage account connection string
- `SqlConnectionString` - Azure SQL connection string

Secrets are not committed: `local.settings.json` and `appsettings.Development.json` are git-ignored.

## Run locally

1. Start Azurite and use SQL Server LocalDB (or point the connection strings at Azure).
2. `cd AgriStream.Functions` then `func start`.
3. `cd AgriStream.Web` then `dotnet run`. The database tables, storage containers and seed data are created on startup.
4. Sign in as `admin`, add sensor readings on the dashboard, upload an image and trigger irrigation.

## Load test

```powershell
.\scripts\load-test.ps1 -Url "https://<app>.azurewebsites.net/" -Requests 300 -BatchSize 100
```
