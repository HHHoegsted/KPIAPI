using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KPIAPI.Data;
using KPIAPI.Domain.Entities;
using KPIAPI.Domain.Enums;
using KPIAPI.DTOs;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/robots/{robotKey}/runs/{runId}/events")]
public class RunEventsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RunEventsController(AppDbContext db) => _db = db;


    /*
        Record a run event for a specific robot run, validating and persisting KPI measurements.
        Creates missing KPI definitions on-the-fly (per robot) and updates the run heartbeat.

        Args:
            robotKey (string): Route robot key; normalized via Trim().ToLowerInvariant().
            runId (string): Route run identifier; Trim() applied.
            request (RecordRunEventRequest): Event payload. Must include at least one KPI in `Kpis`.
                Optional `CreatedUtc` (defaults to now UTC) and `Message`.

        Returns:
            Task<ActionResult>:
                - 201 Created with { Id } and a Location header pointing to GetEvent
                - 400 BadRequest if route params are missing, no KPIs are provided, KPI fields are invalid
                    (missing Key/Name, ValueType mismatch, or value not matching ValueType)
                - 404 NotFound if the robot or run does not exist
    */
    [HttpPost]
    public async Task<ActionResult> RecordEvent(
    [FromRoute] string robotKey,
    [FromRoute] string runId,
    [FromBody] RecordRunEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(robotKey))
            return BadRequest("Robot key is required");
        if (string.IsNullOrWhiteSpace(runId))
            return BadRequest("Run ID is required");
        if (request?.Kpis == null || request.Kpis.Count == 0)
            return BadRequest("At least one KPI must be provided");

        robotKey = robotKey.Trim().ToLowerInvariant();
        runId = runId.Trim();

        var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
        if (robot == null)
            return NotFound($"Robot '{robotKey}' not found");

        var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);
        if (run == null)
            return NotFound($"Run '{runId}' not found for robot '{robotKey}'");

        var createdUtc = request.CreatedUtc?.ToUniversalTime() ?? DateTime.UtcNow;

        if (run.LastHeartbeatUtc == null || createdUtc > run.LastHeartbeatUtc.Value)
            run.LastHeartbeatUtc = createdUtc;

        var requestedKeys = request.Kpis
            .Where(k => !string.IsNullOrWhiteSpace(k.Key))
            .Select(k => k.Key.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var existingDefinitions = await _db.KpiDefinitions
            .Where(d => d.RobotId == robot.Id && requestedKeys.Contains(d.Key))
            .ToListAsync();

        var defByKey = existingDefinitions.ToDictionary(d => d.Key, StringComparer.OrdinalIgnoreCase);

        var runEvent = new RunEvent
        {
            RobotRunId = run.Id,
            CreatedUtc = createdUtc,
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim()
        };

        var newDefinitions = new List<KpiDefinition>();

        foreach (var kpi in request.Kpis)
        {
            if (string.IsNullOrWhiteSpace(kpi.Key))
                return BadRequest("KPI Key is required");

            if (string.IsNullOrWhiteSpace(kpi.Name))
                return BadRequest($"KPI '{kpi.Key}': Name is required.");

            var key = kpi.Key.Trim().ToLowerInvariant();

            if (!defByKey.TryGetValue(key, out var definition))
            {
                definition = new KpiDefinition
                {
                    RobotId = robot.Id,
                    Key = key,
                    Name = kpi.Name.Trim(),
                    Unit = string.IsNullOrWhiteSpace(kpi.Unit) ? null : kpi.Unit.Trim(),
                    ValueType = kpi.ValueType,
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow
                };

                defByKey[key] = definition;
                newDefinitions.Add(definition);
            }
            else
            {
                if (definition.ValueType != kpi.ValueType)
                {
                    return BadRequest(
                        $"KPI '{key}': ValueType mismatch. Existing={definition.ValueType}, Provided={kpi.ValueType}");
                }
            }

            if (!IsValidValue(kpi))
                return BadRequest($"KPI '{key}': value does not match ValueType");

            runEvent.KpiMeasurements.Add(new KpiMeasurement
            {
                KpiDefinition = definition,
                RecordedUtc = createdUtc,
                ValueType = kpi.ValueType,
                IntValue = kpi.IntValue,
                DecimalValue = kpi.DecimalValue,
                BoolValue = kpi.BoolValue,
                DurationMs = kpi.DurationMs,
                TextValue = kpi.TextValue
            });
        }

        if (newDefinitions.Count > 0)
            _db.KpiDefinitions.AddRange(newDefinitions);

        _db.RunEvents.Add(runEvent);

        await _db.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetEvent),
            new { robotKey, runId, eventId = runEvent.Id },
            new { runEvent.Id });
    }


    [HttpGet("{eventId:int}")]
    public async Task<ActionResult> GetEvent([FromRoute] string robotKey, [FromRoute] string runId, [FromRoute] int eventId)
    {
        robotKey = robotKey.Trim().ToLowerInvariant();
        runId = runId.Trim();

        var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
        if (robot == null) return NotFound();

        var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);
        if (run == null) return NotFound();

        var ev = await _db.RunEvents
            .Include(e => e.KpiMeasurements)
                .ThenInclude(m => m.KpiDefinition)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.RobotRunId == run.Id);

        if (ev == null) return NotFound();

        return Ok(ev);
    }

    private static bool IsValidValue(KPIDTO kpi)
    {
        int setCount =
            (kpi.IntValue != null ? 1 : 0) +
            (kpi.DecimalValue != null ? 1 : 0) +
            (kpi.BoolValue != null ? 1 : 0) +
            (kpi.DurationMs != null ? 1 : 0) +
            (!string.IsNullOrWhiteSpace(kpi.TextValue) ? 1 : 0);

        if (setCount != 1) return false;

        return kpi.ValueType switch
        {
            KpiValueType.Integer => kpi.IntValue != null,
            KpiValueType.Decimal => kpi.DecimalValue != null,
            KpiValueType.Boolean => kpi.BoolValue != null,
            KpiValueType.DurationMs => kpi.DurationMs != null,
            KpiValueType.Text => !string.IsNullOrWhiteSpace(kpi.TextValue),
            _ => false
        };
    }
}

