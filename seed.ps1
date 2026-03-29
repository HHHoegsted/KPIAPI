param(
    [string]$ApiBase = "http://localhost:8080",
    [int]$RunsPerRobot = 12
)

$ErrorActionPreference = "Stop"

function Invoke-ApiJson {
    param(
        [Parameter(Mandatory=$true)][ValidateSet("GET","POST","PUT")] [string]$Method,
        [Parameter(Mandatory=$true)][string]$Url,
        [object]$Body = $null
    )

    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 10
        return Invoke-RestMethod -Method $Method -Uri $Url -ContentType "application/json" -Body $json
    }

    return Invoke-RestMethod -Method $Method -Uri $Url
}

function Get-RandomOutcome {
    $roll = Get-Random -Minimum 1 -Maximum 101
    if ($roll -le 65) { return 1 }  # Succeeded
    if ($roll -le 82) { return 2 }  # Failed
    if ($roll -le 94) { return 3 }  # Partial
    return 4                        # Canceled
}

function Get-RandomErrorCode {
    $codes = @("VAL001", "SYS500", "AUTH403", "TIMEOUT", "UPSTREAM")
    return $codes | Get-Random
}

function Get-RandomErrorMessage {
    $messages = @(
        "Validation failed for one or more items.",
        "Unexpected upstream service error.",
        "Timed out while waiting for dependency.",
        "Authentication token expired.",
        "Processing stopped due to business rule."
    )
    return $messages | Get-Random
}

$robots = @(
    "25007-fin-invoice-paybot",
    "25008-hr-onboarding-bot",
    "25009-it-access-review-bot"
)

Write-Host "Seeding robots, runs, events, and KPI measurements against $ApiBase ..." -ForegroundColor Cyan

foreach ($robotKey in $robots) {
    Write-Host "Upserting robot $robotKey" -ForegroundColor Yellow
    Invoke-ApiJson -Method POST -Url "$ApiBase/api/robots/upsert" -Body @{
        key = $robotKey
    } | Out-Null

    for ($i = 1; $i -le $RunsPerRobot; $i++) {
        $daysAgo = Get-Random -Minimum 0 -Maximum 45
        $startUtc = (Get-Date).ToUniversalTime().AddDays(-$daysAgo).AddMinutes(-(Get-Random -Minimum 0 -Maximum 1440))
        $eventCount = Get-Random -Minimum 2 -Maximum 7
        $outcome = Get-RandomOutcome

        $startResponse = Invoke-ApiJson -Method POST -Url "$ApiBase/api/robots/$robotKey/runs/start" -Body @{
            startTimeUtc = $startUtc.ToString("o")
        }

        $runId = $startResponse.runId
        if (-not $runId) {
            throw "Run start did not return a runId for $robotKey"
        }

        Write-Host "  Run $runId started for $robotKey" -ForegroundColor DarkYellow

        $totalItems = Get-Random -Minimum 20 -Maximum 250
        $hitlItems = Get-Random -Minimum 0 -Maximum ([Math]::Max(1, [Math]::Floor($totalItems * 0.35)))
        $processedSoFar = 0

        for ($e = 1; $e -le $eventCount; $e++) {
            $createdUtc = $startUtc.AddMinutes($e * (Get-Random -Minimum 1 -Maximum 8))

            if ($e -eq $eventCount) {
                $processedChunk = $totalItems - $processedSoFar
            }
            else {
                $remaining = $totalItems - $processedSoFar
                $processedChunk = Get-Random -Minimum 1 -Maximum ([Math]::Max(2, [Math]::Floor($remaining / ([Math]::Max(1, $eventCount - $e + 1))) + 1))
            }

            $processedSoFar += $processedChunk
            if ($processedSoFar -gt $totalItems) {
                $processedChunk -= ($processedSoFar - $totalItems)
                $processedSoFar = $totalItems
            }

            $eventType = @("Info","Checkpoint","Progress") | Get-Random
            $message = "Event $e of $eventCount for run $runId"

            $kpis = @(
                @{
                    key = "processed-items"
                    name = "Processed Items"
                    valueType = 1
                    unit = "items"
                    intValue = $processedChunk
                    decimalValue = $null
                    boolValue = $null
                    durationMs = $null
                    textValue = $null
                },
                @{
                    key = "duration-ms"
                    name = "Duration"
                    valueType = 4
                    unit = "ms"
                    intValue = $null
                    decimalValue = $null
                    boolValue = $null
                    durationMs = (Get-Random -Minimum 500 -Maximum 12000)
                    textValue = $null
                },
                @{
                    key = "requires-hitl"
                    name = "Requires HITL"
                    valueType = 3
                    unit = $null
                    intValue = $null
                    decimalValue = $null
                    boolValue = (($e -eq $eventCount) -and ($hitlItems -gt 0))
                    durationMs = $null
                    textValue = $null
                },
                @{
                    key = "stage"
                    name = "Stage"
                    valueType = 5
                    unit = $null
                    intValue = $null
                    decimalValue = $null
                    boolValue = $null
                    durationMs = $null
                    textValue = @("Collect","Validate","Process","Finalize") | Get-Random
                }
            )

            Invoke-ApiJson -Method POST -Url "$ApiBase/api/robots/$robotKey/runs/$runId/events" -Body @{
                createdUtc = $createdUtc.ToString("o")
                message = $message
                eventType = $eventType
                correlationKey = [guid]::NewGuid().ToString("N")
                payload = @{
                    eventNumber = $e
                    totalEvents = $eventCount
                    robotKey = $robotKey
                }
                kpis = $kpis
            } | Out-Null
        }

        $endUtc = $startUtc.AddMinutes((Get-Random -Minimum 3 -Maximum 60))
        $completeBody = @{
            endTimeUtc = $endUtc.ToString("o")
            outcome = $outcome
            errorCode = $null
            errorMessage = $null
        }

        if ($outcome -eq 2 -or $outcome -eq 3 -or $outcome -eq 4) {
            $completeBody.errorCode = Get-RandomErrorCode
            $completeBody.errorMessage = Get-RandomErrorMessage
        }

        Invoke-ApiJson -Method POST -Url "$ApiBase/api/robots/$robotKey/runs/$runId/complete" -Body $completeBody | Out-Null
    }
}

Write-Host ""
Write-Host "Seed complete." -ForegroundColor Green
Write-Host "Check robots list:" -ForegroundColor Cyan
Write-Host "  $ApiBase/api/robots?hasDataOnly=true"
Write-Host "Check one robot run list:" -ForegroundColor Cyan
Write-Host "  $ApiBase/api/robots/$($robots[0])/runs"