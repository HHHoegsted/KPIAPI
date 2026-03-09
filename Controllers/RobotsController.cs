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
    [Route("api/robots")]
    public class RobotsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public RobotsController(AppDbContext db)
        {
            _db = db;
        }

        /*
            Create or update ("upsert") a robot by its canonical key, updating center/display name and
            ensuring the robot is marked active.

            Args:
                request (RobotUpsertRequest): Payload containing `Key` (required). Must parse via `RobotKey.TryParse`
                    and match yynnn-ccc-display-name-of-robot (e.g. 25007-fin-invoice-paybot).

            Returns:
                Task<ActionResult>:
                    200 OK with { Id, Key, CenterCode, DisplayName, IsActive }, or 400 BadRequest on validation/parsing failure.

        */
        [HttpPost("upsert")]
        public async Task<ActionResult> Upsert([FromBody] RobotUpsertRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest("Key is required.");
            }

            if (!RobotKey.TryParse(request.Key, out var parts))
                return BadRequest("Key must match: yynnn-ccc-display-name-of-robot (example: 25007-fin-invoice-paybot).");

            var key = parts.Key;
            var centerCode = parts.CenterCode;
            var displayName = parts.DisplayName;

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == key);

            if (robot == null)
            {
                robot = new Robot
                {
                    Key = key,
                    DisplayName = displayName,
                    CenterCode = centerCode,
                    IsActive = true,
                    CreatedUtc = DateTime.UtcNow
                };
                _db.Robots.Add(robot);
            }
            else
            {
                robot.CenterCode = centerCode;
                robot.DisplayName = displayName;
                robot.IsActive = true;
            }

            await _db.SaveChangesAsync();

            return Ok(new { robot.Id, robot.Key, robot.CenterCode, robot.DisplayName, robot.IsActive });
        }


        /*
            Get per-robot dashboard summary for an optional UTC time window: run/event/measurement counts,
            first/last event times, KPI rollups, and (if configured) coverage metrics.

            Args:
                robotKey (string): Route key; normalized via Trim().ToLowerInvariant() before lookup.
                fromUtc (DateTime?, optional): Inclusive start bound. Unspecified Kind is treated as UTC;
                    otherwise converted to UTC. Filters runs/events/measurements by their UTC timestamps.
                toUtc (DateTime?, optional): Inclusive end bound. Same UTC normalization/filtering as fromUtc.

            Returns:
                Task<ActionResult<RobotDashboardSummaryDto>>:
                    200 OK with summary DTO, or 404 NotFound if the robot key does not exist.
        */
        [HttpGet("{robotKey}/dashboard")]
        public async Task<ActionResult<RobotDashboardSummaryDto>> GetRobotDashboard(
            [FromRoute] string robotKey,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();

            var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null) return NotFound($"Robot '{robotKey}' not found");

            DateTime? from = null;
            DateTime? to = null;

            if (fromUtc != null)
            {
                from = fromUtc.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc)
                    : fromUtc.Value.ToUniversalTime();
            }

            if (toUtc != null)
            {
                to = toUtc.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc)
                    : toUtc.Value.ToUniversalTime();
            }

            var runsQuery = _db.RobotRuns.AsNoTracking().Where(r => r.RobotId == robot.Id);

            if (from != null) runsQuery = runsQuery.Where(r => r.StartTimeUtc >= from.Value);
            if (to != null) runsQuery = runsQuery.Where(r => r.StartTimeUtc <= to.Value);

            var runCount = await runsQuery.CountAsync();

            var eventsQuery = _db.RunEvents.AsNoTracking()
                .Where(e => e.RobotRun.RobotId == robot.Id);

            if (from != null) eventsQuery = eventsQuery.Where(e => e.CreatedUtc >= from.Value);
            if (to != null) eventsQuery = eventsQuery.Where(e => e.CreatedUtc <= to.Value);

            var eventFacts = await eventsQuery
                .Select(e => new { e.Id, e.CreatedUtc })
                .ToListAsync();

            var eventCount = eventFacts.Count;
            var firstEventUtc = eventCount == 0 ? null : eventFacts.Min(x => (DateTime?)x.CreatedUtc);
            var lastEventUtc = eventCount == 0 ? null : eventFacts.Max(x => (DateTime?)x.CreatedUtc);

            var measurementsQuery = _db.KpiMeasurements.AsNoTracking()
                .Where(m => m.RunEvent.RobotRun.RobotId == robot.Id);

            if (from != null) measurementsQuery = measurementsQuery.Where(m => m.RecordedUtc >= from.Value);
            if (to != null) measurementsQuery = measurementsQuery.Where(m => m.RecordedUtc <= to.Value);

            var measurements = await measurementsQuery
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

            var dto = new RobotDashboardSummaryDto(
                RobotKey: robotKey,
                FromUtc: from,
                ToUtc: to,
                RunCount: runCount,
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

        /*
            List robots, optionally restricted to robots that have KPI measurement data.

            Args:
                hasDataOnly (bool, optional): Default true. When true, returns only robots that appear in
                    KpiMeasurements and includes LastSeenUtc = max(RecordedUtc). When false, returns all robots
                    with LastSeenUtc = null.

            Returns:
                Task<ActionResult>:
                    200 OK with a list of robot projections (including LastSeenUtc), possibly empty.
        */
        [HttpGet]
        public async Task<ActionResult> List([FromQuery] bool hasDataOnly = true)
        {
            var robotsQuery = _db.Robots.AsNoTracking();

            if (!hasDataOnly)
            {
                var all = await robotsQuery
                    .OrderBy(r => r.Key)
                    .Select(r => new
                    {
                        r.Id,
                        r.Key,
                        r.CenterCode,
                        r.DisplayName,
                        r.IsActive,
                        r.CreatedUtc,
                        LastSeenUtc = (DateTime?)null
                    })
                    .ToListAsync();

                return Ok(all);
            }

            var robotsWithLastSeen = await _db.KpiMeasurements
                .AsNoTracking()
                .GroupBy(m => m.RunEvent.RobotRun.RobotId)
                .Select(g => new
                {
                    RobotId = g.Key,
                    LastSeenUtc = g.Max(x => x.RecordedUtc)
                })
                .ToListAsync();

            if (robotsWithLastSeen.Count == 0)
                return Ok(new List<object>());

            var lastSeenByRobotId = robotsWithLastSeen.ToDictionary(x => x.RobotId, x => x.LastSeenUtc);
            var robotIds = lastSeenByRobotId.Keys.ToList();

            var robots = await _db.Robots.AsNoTracking()
                .Where(r => robotIds.Contains(r.Id))
                .OrderBy(r => r.Key)
                .Select(r => new
                {
                    r.Id,
                    r.Key,
                    r.CenterCode,
                    r.DisplayName,
                    r.IsActive,
                    r.CreatedUtc,
                    LastSeenUtc = lastSeenByRobotId[r.Id]
                })
                .ToListAsync();

            return Ok(robots);
        }
    }
}
