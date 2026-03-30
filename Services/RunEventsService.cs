using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KPIAPI.Data;
using KPIAPI.Domain.Constants;
using KPIAPI.Domain.Entities;
using KPIAPI.Domain.Enums;
using KPIAPI.DTOs;

namespace KPIAPI.Services
{
    public class RunEventsService
    {
        private const string DebugModeKpiKey = "debug_mode";

        private readonly AppDbContext _db;

        public RunEventsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> RecordEventAsync(string robotKey, string runId, RecordRunEventRequest request)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return new BadRequestObjectResult("Robot key is required");
            if (string.IsNullOrWhiteSpace(runId))
                return new BadRequestObjectResult("Run ID is required");
            if (request?.Kpis == null || request.Kpis.Count == 0)
                return new BadRequestObjectResult("At least one KPI must be provided");

            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return new NotFoundObjectResult($"Robot '{robotKey}' not found");

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);
            if (run == null)
                return new NotFoundObjectResult($"Run '{runId}' not found for robot '{robotKey}'");

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
                Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
                EventType = string.IsNullOrWhiteSpace(request.EventType) ? "Info" : request.EventType.Trim(),
                CorrelationKey = string.IsNullOrWhiteSpace(request.CorrelationKey) ? null : request.CorrelationKey.Trim(),
                PayloadJson = request.Payload == null ? null : JsonSerializer.Serialize(request.Payload)
            };

            var newDefinitions = new List<KpiDefinition>();

            foreach (var kpi in request.Kpis)
            {
                if (string.IsNullOrWhiteSpace(kpi.Key))
                    return new BadRequestObjectResult("KPI Key is required");

                if (string.IsNullOrWhiteSpace(kpi.Name))
                    return new BadRequestObjectResult($"KPI '{kpi.Key}': Name is required.");

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
                else if (definition.ValueType != kpi.ValueType)
                {
                    return new BadRequestObjectResult(
                        $"KPI '{key}': ValueType mismatch. Existing={definition.ValueType}, Provided={kpi.ValueType}");
                }

                if (!IsValidValue(kpi))
                    return new BadRequestObjectResult($"KPI '{key}': value does not match ValueType");

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

            return new CreatedAtActionResult(
                "GetEvent",
                "RunEvents",
                new { robotKey, runId, eventId = runEvent.Id },
                new { runEvent.Id });
        }

        public async Task<IActionResult> ListEventsAsync(
            string robotKey,
            string runId,
            bool developerMode = false,
            bool debugOnly = false)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
                return new NotFoundResult();

            var robot = await _db.Robots
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == robotKey);

            if (robot == null)
                return new NotFoundObjectResult($"Robot '{robotKey}' not found");

            var run = await _db.RobotRuns
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);

            if (run == null)
                return new NotFoundObjectResult($"Run '{runId}' not found for robot '{robotKey}'");

            var query = _db.RunEvents
                .AsNoTracking()
                .Include(e => e.KpiMeasurements)
                    .ThenInclude(m => m.KpiDefinition)
                .Where(e => e.RobotRunId == run.Id);

            if (debugOnly)
            {
                query = query.Where(e => e.KpiMeasurements.Any(m =>
                    m.KpiDefinition.Key == DebugModeKpiKey &&
                    m.ValueType == KpiValueType.Boolean &&
                    m.BoolValue == true));
            }
            else if (!developerMode)
            {
                query = query.Where(e => !e.KpiMeasurements.Any(m =>
                    m.KpiDefinition.Key == DebugModeKpiKey &&
                    m.ValueType == KpiValueType.Boolean &&
                    m.BoolValue == true));
            }

            var events = await query
                .OrderBy(e => e.CreatedUtc)
                .ToListAsync();

            var result = events
                .Select(ev => new RunEventDto(
                    EventId: ev.Id,
                    CreatedUtc: ev.CreatedUtc,
                    Message: ev.Message,
                    EventType: ev.EventType,
                    CorrelationKey: ev.CorrelationKey,
                    PayloadJson: ev.PayloadJson,
                    IsDebug: IsDebugEvent(ev),
                    Kpis: ev.KpiMeasurements
                        .OrderBy(m => m.KpiDefinition.Key)
                        .Select(m => new RunKpiMeasurementDto(
                            EventId: ev.Id,
                            EventCreatedUtc: ev.CreatedUtc,
                            EventMessage: ev.Message,
                            KpiDefinitionId: m.KpiDefinitionId,
                            KpiKey: m.KpiDefinition.Key,
                            KpiName: m.KpiDefinition.Name,
                            Unit: m.KpiDefinition.Unit,
                            ValueType: m.ValueType,
                            IntValue: m.IntValue,
                            DecimalValue: m.DecimalValue,
                            BoolValue: m.BoolValue,
                            DurationMs: m.DurationMs,
                            TextValue: m.TextValue
                        ))
                        .ToList()
                ))
                .ToList();

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetEventAsync(string robotKey, string runId, int eventId, bool developerMode = false)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
                return new NotFoundResult();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null) return new NotFoundResult();

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);
            if (run == null) return new NotFoundResult();

            var ev = await _db.RunEvents
                .Include(e => e.KpiMeasurements)
                    .ThenInclude(m => m.KpiDefinition)
                .FirstOrDefaultAsync(e => e.Id == eventId && e.RobotRunId == run.Id);

            if (ev == null) return new NotFoundResult();

            var dto = new RunEventDto(
                EventId: ev.Id,
                CreatedUtc: ev.CreatedUtc,
                Message: ev.Message,
                EventType: ev.EventType,
                CorrelationKey: ev.CorrelationKey,
                PayloadJson: ev.PayloadJson,
                IsDebug: IsDebugEvent(ev),
                Kpis: ev.KpiMeasurements
                    .OrderBy(m => m.KpiDefinition.Key)
                    .Select(m => new RunKpiMeasurementDto(
                        EventId: ev.Id,
                        EventCreatedUtc: ev.CreatedUtc,
                        EventMessage: ev.Message,
                        KpiDefinitionId: m.KpiDefinitionId,
                        KpiKey: m.KpiDefinition.Key,
                        KpiName: m.KpiDefinition.Name,
                        Unit: m.KpiDefinition.Unit,
                        ValueType: m.ValueType,
                        IntValue: m.IntValue,
                        DecimalValue: m.DecimalValue,
                        BoolValue: m.BoolValue,
                        DurationMs: m.DurationMs,
                        TextValue: m.TextValue
                    ))
                    .ToList()
            );

            return new OkObjectResult(dto);
        }

        private static bool IsDebugEvent(RunEvent ev)
        {
            return ev.KpiMeasurements.Any(m =>
                m.KpiDefinition.Key == DebugModeKpiKey &&
                m.ValueType == KpiValueType.Boolean &&
                m.BoolValue == true);
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
}