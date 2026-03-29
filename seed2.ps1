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
    if ($roll -le 72) { return 1 }  # Succeeded
    if ($roll -le 86) { return 2 }  # Failed
    if ($roll -le 95) { return 3 }  # Partial
    return 4                        # Canceled
}

function Get-RandomErrorCode {
    $codes = @("PAY001", "BANK502", "AUTH403", "TIMEOUT", "VAL001")
    return $codes | Get-Random
}

function Get-RandomErrorMessage {
    $messages = @(
        "One or more invoices failed validation.",
        "Bank integration returned an unexpected response.",
        "Payment authorization could not be completed.",
        "Timed out while submitting payment batch.",
        "Processing stopped due to missing invoice data."
    )
    return $messages | Get-Random
}

function Get-RandomInvoiceStage {
    @("Collect", "Validate", "Approve", "Pay", "Finalize") | Get-Random
}

function Get-RandomDecimal {
    param(
        [double]$Minimum,
        [double]$Maximum,
        [int]$Decimals = 2
    )

    $value = (Get-Random -Minimum ([int]($Minimum * 100)) -Maximum ([int]($Maximum * 100))) / 100.0
    return [Math]::Round($value, $Decimals)
}

$robots = @(
    "25007-fin-pay-invoices-bot"
)

Write-Host "Seeding invoice robots, runs, events, and KPI measurements against $ApiBase ..." -ForegroundColor Cyan

foreach ($robotKey in $robots) {
    Write-Host "Upserting robot $robotKey" -ForegroundColor Yellow
    Invoke-ApiJson -Method POST -Url "$ApiBase/api/robots/upsert" -Body @{
        key = $robotKey
    } | Out-Null

    for ($i = 1; $i -le $RunsPerRobot; $i++) {
        $daysAgo = Get-Random -Minimum 0 -Maximum 45
        $startUtc = (Get-Date).ToUniversalTime().AddDays(-$daysAgo).AddMinutes(-(Get-Random -Minimum 0 -Maximum 1440))
        $eventCount = Get-Random -Minimum 3 -Maximum 7
        $outcome = Get-RandomOutcome

        $startResponse = Invoke-ApiJson -Method POST -Url "$ApiBase/api/robots/$robotKey/runs/start" -Body @{
            startTimeUtc = $startUtc.ToString("o")
        }

        $runId = $startResponse.runId
        if (-not $runId) {
            throw "Run start did not return a runId for $robotKey"
        }

        Write-Host "  Run $runId started for $robotKey" -ForegroundColor DarkYellow

        $totalInvoices = Get-Random -Minimum 8 -Maximum 40
        $hitlInvoices = Get-Random -Minimum 0 -Maximum ([Math]::Max(1, [Math]::Floor($totalInvoices * 0.20)))
        $processedSoFar = 0
        $amountPaidSoFar = 0.0

        for ($e = 1; $e -le $eventCount; $e++) {
            $createdUtc = $startUtc.AddMinutes($e * (Get-Random -Minimum 1 -Maximum 10))

            if ($e -eq $eventCount) {
                $invoiceChunk = $totalInvoices - $processedSoFar
            }
            else {
                $remaining = $totalInvoices - $processedSoFar
                $invoiceChunk = Get-Random -Minimum 1 -Maximum ([Math]::Max(2, [Math]::Floor($remaining / ([Math]::Max(1, $eventCount - $e + 1))) + 1))
            }

            $processedSoFar += $invoiceChunk
            if ($processedSoFar -gt $totalInvoices) {
                $invoiceChunk -= ($processedSoFar - $totalInvoices)
                $processedSoFar = $totalInvoices
            }

            $amountChunk = 0.0
            for ($n = 1; $n -le $invoiceChunk; $n++) {
                $amountChunk += Get-RandomDecimal -Minimum 250 -Maximum 18500 -Decimals 2
            }
            $amountChunk = [Math]::Round($amountChunk, 2)
            $amountPaidSoFar = [Math]::Round(($amountPaidSoFar + $amountChunk), 2)

            $requiresHitl = (($e -eq $eventCount) -and ($hitlInvoices -gt 0))
            $stage = Get-RandomInvoiceStage
            $eventType = @("Info","Checkpoint","Progress") | Get-Random
            $message = "Processed $invoiceChunk invoice(s) in batch $e of $eventCount"

            $kpis = @(
                @{
                    key = "invoices-paid"
                    name = "Invoices Paid"
                    valueType = 1
                    unit = "invoices"
                    intValue = $invoiceChunk
                    decimalValue = $null
                    boolValue = $null
                    durationMs = $null
                    textValue = $null
                },
                @{
                    key = "amount-paid-dkk"
                    name = "Amount Paid"
                    valueType = 2
                    unit = "DKK"
                    intValue = $null
                    decimalValue = $amountChunk
                    boolValue = $null
                    durationMs = $null
                    textValue = $null
                },
                @{
                    key = "requires-hitl"
                    name = "Requires HITL"
                    valueType = 3
                    unit = $null
                    intValue = $null
                    decimalValue = $null
                    boolValue = $requiresHitl
                    durationMs = $null
                    textValue = $null
                },
                @{
                    key = "payment-stage"
                    name = "Payment Stage"
                    valueType = 5
                    unit = $null
                    intValue = $null
                    decimalValue = $null
                    boolValue = $null
                    durationMs = $null
                    textValue = $stage
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
                    invoicesInBatch = $invoiceChunk
                    amountPaidDkk = $amountChunk
                    amountPaidRunToDateDkk = $amountPaidSoFar
                }
                kpis = $kpis
            } | Out-Null
        }

        $endUtc = $startUtc.AddMinutes((Get-Random -Minimum 5 -Maximum 75))
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