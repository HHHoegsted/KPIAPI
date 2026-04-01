using KPIAPI.Data;
using KPIAPI.Domain;
using KPIAPI.Domain.Constants;
using KPIAPI.Domain.Entities;
using KPIAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KPIAPI.Services
{
    public class RunsService
    {
        private readonly AppDbContext _db;

        public RunsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<object> StartAsync(string robotKey, StartRunRequest? request)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return new { Error = "Robot key is required." };

            if (!RobotKey.TryParse(robotKey, out var parts))
                return new { Error = "Robot key must match: yynnn-ccc-display-name-of-robot." };

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

            return new
            {
                run.Id,
                run.RunId,
                run.StartTimeUtc
            };
        }

        public async Task<string?> CompleteAsync(string robotKey, string runId, CompleteRunRequest request)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return "Robot key is required.";

            if (string.IsNullOrWhiteSpace(runId))
                return "Run ID is required.";

            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return $"Robot with key '{robotKey}' not found.";

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RunId == runId && r.RobotId == robot.Id);
            if (run == null)
                return $"Run with ID '{runId}' for robot '{robotKey}' not found.";

            if (run.Outcome != null)
                return $"Run with ID '{runId}' for robot '{robotKey}' has already been completed.";

            run.Outcome = request.Outcome;
            run.EndTimeUtc = request.EndTimeUtc?.ToUniversalTime() ?? DateTime.UtcNow;
            run.ErrorCode = request.ErrorCode;
            run.ErrorMessage = request.ErrorMessage;

            await _db.SaveChangesAsync();

            return null;
        }

        public async Task<string?> HeartbeatAsync(string robotKey, string runId, RunHeartbeatRequest? request)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return "Robot key is required.";

            if (string.IsNullOrWhiteSpace(runId))
                return "Run ID is required.";

            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return $"Robot with key '{robotKey}' not found.";

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RunId == runId && r.RobotId == robot.Id);
            if (run == null)
                return $"Run with ID '{runId}' for robot '{robotKey}' not found.";

            if (run.Outcome != null)
                return null;

            var atUtc = request?.AtUtc?.ToUniversalTime() ?? DateTime.UtcNow;

            if (run.LastHeartbeatUtc == null || atUtc > run.LastHeartbeatUtc.Value)
                run.LastHeartbeatUtc = atUtc;

            await _db.SaveChangesAsync();
            return null;
        }

        public async Task<List<RunKpiMeasurementDto>> GetAllKpisForRunAsync(
            string robotKey,
            string runId,
            bool developerMode = false)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
                return new List<RunKpiMeasurementDto>();

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null) return new List<RunKpiMeasurementDto>();

            var run = await _db.RobotRuns.FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);
            if (run == null) return new List<RunKpiMeasurementDto>();

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

            return result;
        }

        public async Task<List<RunListItemDto>> ListRunsForRobotAsync(
            string robotKey,
            DateTime? fromUtc,
            int limit,
            string sort,
            bool developerMode = false)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            limit = Math.Clamp(limit, 1, 2000);
            sort = (sort ?? "desc").Trim().ToLowerInvariant();

            if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
                return new List<RunListItemDto>();

            var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return new List<RunListItemDto>();

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
                return new List<RunListItemDto>();

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
                Outcome: r.Outcome,
                EventCount: eventCounts.TryGetValue(r.Id, out var ec) ? ec : 0,
                MeasurementCount: measurementCounts.TryGetValue(r.Id, out var mc) ? mc : 0
            )).ToList();

            return result;
        }

        public async Task<RunDetailsDto?> GetRunAsync(string robotKey, string runId, bool developerMode = false)
        {
            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
                return null;

            var robot = await _db.Robots.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Key == robotKey);

            if (robot == null)
                return null;

            var run = await _db.RobotRuns.AsNoTracking()
                .FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);

            if (run == null)
                return null;

            var eventCount = await _db.RunEvents.AsNoTracking()
                .CountAsync(e => e.RobotRunId == run.Id);

            var measurementCount = await _db.KpiMeasurements.AsNoTracking()
                .CountAsync(m => m.RunEvent.RobotRunId == run.Id);

            return new RunDetailsDto(
                RunId: run.RunId,
                StartTimeUtc: run.StartTimeUtc,
                EndTimeUtc: run.EndTimeUtc,
                LastHeartbeatUtc: run.LastHeartbeatUtc,
                Outcome: run.Outcome,
                ErrorCode: run.ErrorCode,
                ErrorMessage: run.ErrorMessage,
                EventCount: eventCount,
                MeasurementCount: measurementCount
            );
        }

        public async Task<string?> DeleteAsync(string robotKey, string runId, bool developerMode = false)
        {
            if (string.IsNullOrWhiteSpace(robotKey))
                return "Robot key is required.";

            if (string.IsNullOrWhiteSpace(runId))
                return "Run ID is required.";

            robotKey = robotKey.Trim().ToLowerInvariant();
            runId = runId.Trim();

            if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
                return "Run deletion is only available in developer mode.";

            var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
            if (robot == null)
                return $"Robot with key '{robotKey}' not found.";

            var run = await _db.RobotRuns
                .FirstOrDefaultAsync(r => r.RobotId == robot.Id && r.RunId == runId);

            if (run == null)
                return $"Run with ID '{runId}' for robot '{robotKey}' not found.";

            _db.RobotRuns.Remove(run);
            await _db.SaveChangesAsync();

            return null;
        }
    }
}