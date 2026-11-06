param(
    [Parameter(Mandatory = $true)]
    [string]$Url,

    [int]$Requests = 200,

    [int]$BatchSize = 50
)

# Fires concurrent GET requests at the deployed site and reports status codes and timings.
Add-Type -AssemblyName System.Net.Http

$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(60)

$results = New-Object System.Collections.Generic.List[object]
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

for ($sent = 0; $sent -lt $Requests; $sent += $BatchSize) {
    $count = [Math]::Min($BatchSize, $Requests - $sent)
    $tasks = New-Object 'System.Collections.Generic.List[System.Threading.Tasks.Task]'
    $timers = @()

    for ($i = 0; $i -lt $count; $i++) {
        $timers += [System.Diagnostics.Stopwatch]::StartNew()
        $tasks.Add($client.GetAsync($Url))
    }

    try {
        [System.Threading.Tasks.Task]::WaitAll($tasks.ToArray())
    }
    catch {
        # Failed requests are counted below.
    }

    for ($i = 0; $i -lt $count; $i++) {
        $task = $tasks[$i]
        $status = if ($task.Status -eq 'RanToCompletion') { [int]$task.Result.StatusCode } else { 0 }
        $results.Add([pscustomobject]@{ Status = $status; Ms = $timers[$i].ElapsedMilliseconds })
    }
}

$stopwatch.Stop()
$client.Dispose()

$ok = ($results | Where-Object { $_.Status -ge 200 -and $_.Status -lt 400 }).Count
$times = $results | ForEach-Object { $_.Ms } | Sort-Object

Write-Host "Target:        $Url"
Write-Host "Requests:      $($results.Count) in $([Math]::Round($stopwatch.Elapsed.TotalSeconds, 1)) s"
Write-Host "Succeeded:     $ok"
Write-Host "Failed:        $($results.Count - $ok)"
Write-Host "Median (ms):   $($times[[int]($times.Count / 2)])"
Write-Host "95th pct (ms): $($times[[Math]::Min($times.Count - 1, [int]($times.Count * 0.95))])"
Write-Host "Max (ms):      $($times[-1])"
$results | Group-Object Status | Sort-Object Name | ForEach-Object { Write-Host "HTTP $($_.Name): $($_.Count)" }
