using KPIAPI.Data;
using KPIAPI.Domain;
using KPIAPI.Domain.Entities;
using KPIAPI.Domain.Enums;
using KPIAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPIAPI.Controllers
{
    [ApiController]
    [Route("api/robots/{robotKey}/runs")]
    public class RunsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RunsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("start")]
        public async Task<ActionResult> Start([FromRoute] string robotKey, [FromBody] StartRunRequest? request)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return BadRequest("Robot key is required.");

            if (!RobotKey.TryParse(robotKey, out var parts))
                return BadRequest("Robot key must match: yynnn-ccc-display-name-of-robot.");

            var key = parts.Key;

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == key);

            if (robot == null)
            {
                robot = new Robot
                {
                    Key = key,
                    CenterCode = parts.CenterCode,
                    DisplayName = parts.DisplayName,
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow
                };

                _db.Robots.Add(robot);
                await _db.SaveChangesAsync();
            }

            var runId = Guid.NewGuid().ToString("N");

            var run = new RobotRun
            {
                RobotId = robot.Id,
                RunId = runId,
                StartTimeUtc = request?.StartTimeUtc?.ToUniversalTime() ?? DateTime.UtcNow
            };

            _db.RobotRuns.Add(run);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                run.Id,
                run.RunId,
                run.StartTimeUtc
            });
        }


        [HttpPost("{runId}/complete")]
        public async Task<ActionResult> Complete([FromRoute] string robotKey, [FromRoute] string runId, [FromBody] CompleteRunRequest request)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return BadRequest("Robot key is required.");

            if (string.IsNullOrWhiteSpace(runId))
                return BadRequest("Run ID is required.");

            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return NotFound($"Robot with key '{robotKey}' not found.");

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RunId == runId && r.RobotId == robot.Id);
            if (run == null)
                return NotFound($"Run with ID '{runId}' for robot '{robotKey}' not found.");

            if (run.Outcome != null)
                return BadRequest($"Run with ID '{runId}' for robot '{robotKey}' has already been completed.");

            run.Outcome = request.Outcome;
            run.EndTimeUtc = request.EndTimeUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            run.ErrorCode = request.ErrorCode;
            run.ErrorMessage = request.ErrorMessage;

            await _db.SaveChangesAsync();

            return NoContent();
        }


        [HttpPost("{runId}/heartbeat")]
        public async Task<ActionResult> Heartbeat(
            [FromRoute] string robotKey,
            [FromRoute] string runId,
            [FromBody] RunHeartbeatRequest? request)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return BadRequest("Robot key is required.");

            if (string.IsNullOrWhiteSpace(runId))
                return BadRequest("Run ID is required.");

            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return NotFound($"Robot with key '{robotKey}' not found.");

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RunId == runId && r.RobotId == robot.Id);
            if (run == null)
                return NotFound($"Run with ID '{runId}' for robot '{robotKey}' not found.");

            // If already completed, treat heartbeat as no-op (idempotent)
            if (run.Outcome != null)
                return NoContent();

            var atUtc = request?.AtUtc?.ToUniversalTime() ?? DateTime.UtcNow;

            // Guard against clock skew: never move backwards
            if (run.LastHeartbeatUtc == null || atUtc > run.LastHeartbeatUtc.Value)
                run.LastHeartbeatUtc = atUtc;

            await _db.SaveChangesAsync();
            return NoContent();
        }


        [HttpGet("{runId}/kpis")]
        public async Task<ActionResult<List<RunKpiMeasurementDto>>> GetAllKpisForRun([FromRoute] string robotKey, [FromRoute] string runId)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null) return NotFound($"Robot '{robotKey}' not found");

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);
            if (run == null) return NotFound($"Run '{runId}' not found for robot '{robotKey}'");

            var result = await _db.RunEvents
                .AsNoTracking()
                .Where(e => e.RobotRunId == run.Id)
                .OrderBy(e => e.CreatedUtc)
                .SelectMany(e => e.KpiMeasurements.Select(m => new RunKpiMeasurementDto(
                    EventId: e.Id,
                    EventCreatedUtc: e.CreatedUtc,
                    EventMessage: e.Message,

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
                )))
                .ToListAsync();

            return Ok(result);
        }


        [HttpGet("{runId}/dashboard")]
        public async Task<ActionResult<RunDashboardSummaryDto>> GetRunDashboard([FromRoute] string robotKey, [FromRoute] string runId)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null) return NotFound($"Robot '{robotKey}' not found");

            var run = await _db.RobotRuns.AsNoTracking().FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);
            if (run == null) return NotFound($"Run '{runId}' not found for robot '{robotKey}'");

            var eventFacts = await _db.RunEvents.AsNoTracking()
                .Where(e => e.RobotRunId == run.Id)
                .Select(e => new { e.Id, e.CreatedUtc })
                .ToListAsync();

            var eventCount = eventFacts.Count;
            var firstEventUtc = eventCount == 0 ? null : eventFacts.Min(e => (DateTime?)e.CreatedUtc);
            var lastEventUtc = eventCount == 0 ? null : eventFacts.Max(e => (DateTime?)e.CreatedUtc);

            var measurements = await _db.KpiMeasurements.AsNoTracking()
                .Where(m => m.RunEvent.RobotRunId == run.Id)
                .Select(m => new
                {
                    m.RecordedUtc,
                    m.ValueType,
                    m.IntValue,
                    m.DecimalValue,
                    m.BoolValue,
                    m.DurationMs,
                    m.TextValue,
                    DefKey = m.KpiDefinition.Key,
                    DefName = m.KpiDefinition.Name,
                    DefUnit = m.KpiDefinition.Unit
                })
                .ToListAsync();

            var measurementCount = measurements.Count;

            var rollups = measurements
                .GroupBy(m => new { m.DefKey, m.DefName, m.DefUnit, m.ValueType })
                .Select(g =>
                {
                    var ordered = g.OrderBy(x => x.RecordedUtc).ToList();
                    DateTime? first = ordered.Count == 0 ? null : ordered.First().RecordedUtc;
                    DateTime? last = ordered.Count == 0 ? null : ordered.Last().RecordedUtc;

                    List<decimal>? numeric = g.Key.ValueType switch
                    {
                        KpiValueType.Integer => g.Where(x => x.IntValue != null).Select(x => (decimal)x.IntValue!.Value).ToList(),
                        KpiValueType.Decimal => g.Where(x => x.DecimalValue != null).Select(x => x.DecimalValue!.Value).ToList(),
                        KpiValueType.DurationMs => g.Where(x => x.DurationMs != null).Select(x => (decimal)x.DurationMs!.Value).ToList(),
                        _ => null
                    };

                    decimal? sum = numeric is { Count: > 0 } ? numeric.Sum() : null;
                    decimal? avg = numeric is { Count: > 0 } ? numeric.Average() : null;
                    decimal? min = numeric is { Count: > 0 } ? numeric.Min() : null;
                    decimal? max = numeric is { Count: > 0 } ? numeric.Max() : null;

                    int? trueCount = null;
                    int? falseCount = null;
                    Dictionary<string, int>? topText = null;

                    if (g.Key.ValueType == KpiValueType.Boolean)
                    {
                        trueCount = g.Count(x => x.BoolValue == true);
                        falseCount = g.Count(x => x.BoolValue == false);
                    }
                    else if (g.Key.ValueType == KpiValueType.Text)
                    {
                        topText = g
                            .Where(x => !string.IsNullOrWhiteSpace(x.TextValue))
                            .GroupBy(x => x.TextValue!.Trim())
                            .OrderByDescending(x => x.Count())
                            .Take(10)
                            .ToDictionary(x => x.Key, x => x.Count());
                    }

                    return new KpiRollupDto(
                        Key: g.Key.DefKey,
                        Name: g.Key.DefName,
                        Unit: g.Key.DefUnit,
                        ValueType: g.Key.ValueType,
                        Count: g.Count(),
                        FirstRecordedUtc: first,
                        LastRecordedUtc: last,
                        Sum: sum,
                        Avg: avg,
                        Min: min,
                        Max: max,
                        TrueCount: trueCount,
                        FalseCount: falseCount,
                        TopTextValues: topText
                    );
                })
                .OrderBy(r => r.Key)
                .ToList();

            var cfg = await _db.RobotDashboardConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.RobotId == robot.Id);

            int? totalItems = null;
            int? hitlItems = null;
            int? completedItems = null;
            decimal? coveragePct = null;

            if (cfg != null &&
                !string.IsNullOrWhiteSpace(cfg.TotalItemsKpiKey) &&
                !string.IsNullOrWhiteSpace(cfg.HitlItemsKpiKey))
            {
                KpiRollupDto? find(string key) =>
                    rollups.FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

                var totalRollup = find(cfg.TotalItemsKpiKey!);
                var hitlRollup = find(cfg.HitlItemsKpiKey!);

                int? readCount(KpiRollupDto? r, CoverageKpiAggregation agg)
                {
                    if (r == null) return null;

                    return agg switch
                    {
                        CoverageKpiAggregation.Sum => r.Sum is null
                            ? null
                            : (int)Math.Round(r.Sum.Value, MidpointRounding.AwayFromZero),
                        CoverageKpiAggregation.TrueCount => r.TrueCount,
                        _ => null
                    };
                }

                totalItems = readCount(totalRollup, cfg.TotalItemsAggregation);
                hitlItems = readCount(hitlRollup, cfg.HitlItemsAggregation);

                if (totalItems != null && hitlItems != null)
                {
                    completedItems = Math.Max(0, totalItems.Value - hitlItems.Value);
                    var denom = totalItems.Value;
                    coveragePct = denom > 0
                        ? Math.Round((decimal)completedItems.Value / denom * 100m, 2)
                        : null;
                }
            }

            var dto = new RunDashboardSummaryDto(
                RobotKey: robotKey,
                RunId: runId,
                EventCount: eventCount,
                MeasurementCount: measurementCount,
                FirstEventUtc: firstEventUtc,
                LastEventUtc: lastEventUtc,
                Kpis: rollups,
                CoveragePct: coveragePct,
                TotalItems: totalItems,
                HitlItems: hitlItems,
                CompletedItems: completedItems
            );

            return Ok(dto);
        }


        [HttpGet("series")]
        public async Task<ActionResult> GetRunSeries(
            [FromRoute] string robotKey,
            [FromQuery] int limit = 50,
            [FromQuery] string sort = "desc")
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            limit = Math.Clamp(limit, 1, 500);
            sort = (sort ?? "desc").Trim().ToLowerInvariant();

            var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return NotFound($"Robot '{robotKey}' not found");

            var cfg = await _db.RobotDashboardConfigs.AsNoTracking()
                .FirstOrDefaultAsync(c => c.RobotId == robot.Id);

            var runsQuery = _db.RobotRuns.AsNoTracking()
                .Where(r => r.RobotId == robot.Id);

            runsQuery = sort == "asc"
                ? runsQuery.OrderBy(r => r.StartTimeUtc)
                : runsQuery.OrderByDescending(r => r.StartTimeUtc);

            var runs = await runsQuery
                .Take(limit)
                .Select(r => new
                {
                    r.Id,
                    r.RunId,
                    r.StartTimeUtc,
                    r.EndTimeUtc,
                    r.Outcome
                })
                .ToListAsync();

            if (runs.Count == 0)
                return Ok(new List<object>());

            var runDbIds = runs.Select(r => r.Id).ToList();

            var eventCounts = await _db.RunEvents.AsNoTracking()
                .Where(e => runDbIds.Contains(e.RobotRunId))
                .GroupBy(e => e.RobotRunId)
                .Select(g => new { RunDbId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RunDbId, x => x.Count);

            var measurementCounts = await _db.KpiMeasurements.AsNoTracking()
                .Where(m => runDbIds.Contains(m.RunEvent.RobotRunId))
                .GroupBy(m => m.RunEvent.RobotRunId)
                .Select(g => new { RunDbId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RunDbId, x => x.Count);

            Dictionary<int, int>? totalByRun = null;
            Dictionary<int, int>? hitlByRun = null;

            if (cfg != null &&
                !string.IsNullOrWhiteSpace(cfg.TotalItemsKpiKey) &&
                !string.IsNullOrWhiteSpace(cfg.HitlItemsKpiKey))
            {
                var totalKey = cfg.TotalItemsKpiKey!;
                var hitlKey = cfg.HitlItemsKpiKey!;

                if (cfg.TotalItemsAggregation == CoverageKpiAggregation.Sum)
                {
                    totalByRun = await _db.KpiMeasurements.AsNoTracking()
                        .Where(m => runDbIds.Contains(m.RunEvent.RobotRunId) && m.KpiDefinition.Key == totalKey)
                        .GroupBy(m => m.RunEvent.RobotRunId)
                        .Select(g => new
                        {
                            RunDbId = g.Key,
                            Sum = g.Sum(x =>
                                x.IntValue != null ? (decimal)x.IntValue.Value :
                                x.DecimalValue != null ? x.DecimalValue.Value :
                                x.DurationMs != null ? (decimal)x.DurationMs.Value :
                                0m)
                        })
                        .ToDictionaryAsync(x => x.RunDbId, x => (int)Math.Round(x.Sum, MidpointRounding.AwayFromZero));
                }
                else
                {
                    totalByRun = await _db.KpiMeasurements.AsNoTracking()
                        .Where(m => runDbIds.Contains(m.RunEvent.RobotRunId) && m.KpiDefinition.Key == totalKey && m.BoolValue == true)
                        .GroupBy(m => m.RunEvent.RobotRunId)
                        .Select(g => new { RunDbId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.RunDbId, x => x.Count);
                }

                if (cfg.HitlItemsAggregation == CoverageKpiAggregation.Sum)
                {
                    hitlByRun = await _db.KpiMeasurements.AsNoTracking()
                        .Where(m => runDbIds.Contains(m.RunEvent.RobotRunId) && m.KpiDefinition.Key == hitlKey)
                        .GroupBy(m => m.RunEvent.RobotRunId)
                        .Select(g => new
                        {
                            RunDbId = g.Key,
                            Sum = g.Sum(x =>
                                x.IntValue != null ? (decimal)x.IntValue.Value :
                                x.DecimalValue != null ? x.DecimalValue.Value :
                                x.DurationMs != null ? (decimal)x.DurationMs.Value :
                                0m)
                        })
                        .ToDictionaryAsync(x => x.RunDbId, x => (int)Math.Round(x.Sum, MidpointRounding.AwayFromZero));
                }
                else
                {
                    hitlByRun = await _db.KpiMeasurements.AsNoTracking()
                        .Where(m => runDbIds.Contains(m.RunEvent.RobotRunId) && m.KpiDefinition.Key == hitlKey && m.BoolValue == true)
                        .GroupBy(m => m.RunEvent.RobotRunId)
                        .Select(g => new { RunDbId = g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.RunDbId, x => x.Count);
                }
            }

            var series = runs.Select(r =>
            {
                int? total = totalByRun != null && totalByRun.TryGetValue(r.Id, out var t) ? t : null;
                int? hitl = hitlByRun != null && hitlByRun.TryGetValue(r.Id, out var h) ? h : null;

                int? completed = null;
                decimal? coverage = null;

                if (total != null && hitl != null)
                {
                    completed = Math.Max(0, total.Value - hitl.Value);
                    coverage = total.Value > 0
                        ? Math.Round((decimal)completed.Value / total.Value * 100m, 2)
                        : null;
                }

                return new
                {
                    r.RunId,
                    r.StartTimeUtc,
                    r.EndTimeUtc,
                    r.Outcome,
                    EventCount = eventCounts.TryGetValue(r.Id, out var ec) ? ec : 0,
                    MeasurementCount = measurementCounts.TryGetValue(r.Id, out var mc) ? mc : 0,
                    CoveragePct = coverage,
                    TotalItems = total,
                    HitlItems = hitl,
                    CompletedItems = completed
                };
            }).ToList();

            return Ok(series);
        }


        [HttpGet]
        public async Task<ActionResult<List<RunListItemDto>>> ListRunsForRobot(
            [FromRoute] string robotKey,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] int limit = 200,
            [FromQuery] string sort = "desc")
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            limit = Math.Clamp(limit, 1, 2000);
            sort = (sort ?? "desc").Trim().ToLowerInvariant();

            var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return NotFound($"Robot '{robotKey}' not found");

            var runsQuery = _db.RobotRuns
                .AsNoTracking()
                .Where(r => r.RobotId == robot.Id);

            if (fromUtc != null)
            {
                var utc = fromUtc.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc)
                    : fromUtc.Value.ToUniversalTime();

                runsQuery = runsQuery.Where(r => r.StartTimeUtc >= utc);
            }

            runsQuery = sort == "asc"
                ? runsQuery.OrderBy(r => r.StartTimeUtc)
                : runsQuery.OrderByDescending(r => r.StartTimeUtc);

            var runs = await runsQuery
                .Take(limit)
                .Select(r => new { r.Id, r.RunId, r.StartTimeUtc, r.EndTimeUtc })
                .ToListAsync();

            if (runs.Count == 0)
                return Ok(new List<RunListItemDto>());

            var runIds = runs.Select(r => r.Id).ToList();

            var eventCounts = await _db.RunEvents.AsNoTracking()
                .Where(e => runIds.Contains(e.RobotRunId))
                .GroupBy(e => e.RobotRunId)
                .Select(g => new { RunDbId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RunDbId, x => x.Count);

            var measurementCounts = await _db.KpiMeasurements.AsNoTracking()
                .Where(m => runIds.Contains(m.RunEvent.RobotRunId))
                .GroupBy(m => m.RunEvent.RobotRunId)
                .Select(g => new { RunDbId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.RunDbId, x => x.Count);

            var result = runs.Select(r => new RunListItemDto(
                RunId: r.RunId,
                StartTimeUtc: r.StartTimeUtc,
                EndTimeUtc: r.EndTimeUtc,
                EventCount: eventCounts.TryGetValue(r.Id, out var ec) ? ec : 0,
                MeasurementCount: measurementCounts.TryGetValue(r.Id, out var mc) ? mc : 0
            )).ToList();

            return Ok(result);
        }
    }
}
