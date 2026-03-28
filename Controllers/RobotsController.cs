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



        /*
            Get summary metrics for a single robot within an optional UTC time window.

            Args:
                robotKey (string): Robot key from the route. Trimmed and normalized to lowercase before lookup.
                fromUtc (DateTime?, optional): Inclusive lower bound for the time window in UTC. When Kind is
                    Unspecified, it is treated as UTC.
                toUtc (DateTime?, optional): Inclusive upper bound for the time window in UTC. When Kind is
                    Unspecified, it is treated as UTC.

            Returns:
                Task<ActionResult<RobotRunsPageSummaryDto>>:
                    200 OK with a RobotRunsPageSummaryDto containing:
                        - RobotKey
                        - RunCount
                        - EventCount
                        - FirstEventUtc
                        - LastEventUtc
                    404 Not Found when the robot does not exist.

            Notes:
                - RunCount is based on RobotRuns.StartTimeUtc.
                - EventCount, FirstEventUtc, and LastEventUtc are based on RunEvents.CreatedUtc.
                - The time filters are applied independently to runs and events using their respective timestamps.
        */

        [HttpGet("{robotKey}/summary")]
        public async Task<ActionResult<RobotRunsPageSummaryDto>> GetRobotSummary(
            [FromRoute] string robotKey,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null)
            {
                robotKey = robotKey.Trim().ToLowerInvariant();

                var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
                if (robot == null)
                    return NotFound($"Robot '{robotKey}' not found");

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
                    .Select(e => new { e.CreatedUtc })
                    .ToListAsync();

                var eventCount = eventFacts.Count;
                var firstEventUtc = eventCount == 0 ? null : eventFacts.Min(x => (DateTime?)x.CreatedUtc);
                var lastEventUtc = eventCount == 0 ? null : eventFacts.Max(x => (DateTime?)x.CreatedUtc);

                return Ok(new RobotRunsPageSummaryDto(
                    RobotKey: robotKey,
                    RunCount: runCount,
                    EventCount: eventCount,
                    FirstEventUtc: firstEventUtc,
                    LastEventUtc: lastEventUtc
                ));
        }
    }
}
