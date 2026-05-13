using Microsoft.EntityFrameworkCore;
using KPIAPI.DTOs;
using KPIAPI.Data;
using KPIAPI.Domain;
using KPIAPI.Domain.Entities;
using KPIAPI.Domain.Constants;
using KPIAPI.Services;

public class RobotService
{
    private readonly AppDbContext _db;
    private readonly ReportingRunsService _reportingRunsService;

    public RobotService(AppDbContext db, ReportingRunsService reportingRunsService)
    {
        _db = db;
        _reportingRunsService = reportingRunsService;
    }

    public async Task<RobotUpsertResult> UpsertAsync(RobotUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
            return new RobotUpsertResult { Error = "Key is required." };

        if (!RobotKey.TryParse(request.Key, out var parts))
            return new RobotUpsertResult { Error = "Key must match: yynnn-ccc-display-name-of-robot (example: 25007-fin-invoice-paybot)." };

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

        return new RobotUpsertResult
        {
            Id = robot.Id,
            Key = robot.Key,
            CenterCode = robot.CenterCode,
            DisplayName = robot.DisplayName,
            IsActive = robot.IsActive
        };
    }

    public async Task<List<object>> ListAsync(bool hasDataOnly, bool developerMode = false)
    {
        var robotsQuery = _db.Robots.AsNoTracking();

        if (!developerMode)
            robotsQuery = robotsQuery.Where(r => r.Key != SystemRobotKeys.DebugOnlyRobotKey);

        if (!hasDataOnly)
        {
            return await robotsQuery
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
                .Cast<object>()
                .ToListAsync();
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
            return new List<object>();

        var lastSeenByRobotId = robotsWithLastSeen.ToDictionary(x => x.RobotId, x => x.LastSeenUtc);
        var robotIds = lastSeenByRobotId.Keys.ToList();

        return await robotsQuery
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
            .Cast<object>()
            .ToListAsync();
    }

    public async Task<RobotRunsPageSummaryDto?> GetRobotSummaryAsync(
        string robotKey,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool developerMode = false)
    {
        robotKey = robotKey.Trim().ToLowerInvariant();

        if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
            return null;

        var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
        if (robot == null)
            return null;

        var slice = await _reportingRunsService.BuildAsync(robot, fromUtc, toUtc, "desc");
        var timeSavedQuery = _db.KpiMeasurements
            .AsNoTracking()
            .Where(m =>
                m.RunEvent.RobotRun.RobotId == robot.Id &&
                m.KpiDefinition.Key == "time_saved" &&
                m.IntValue != null);

        if (fromUtc.HasValue)
            timeSavedQuery = timeSavedQuery.Where(m => m.RecordedUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            timeSavedQuery = timeSavedQuery.Where(m => m.RecordedUtc <= toUtc.Value);

        var totalTimeSavedSeconds = await timeSavedQuery
            .SumAsync(m => (long?)m.IntValue) ?? 0;

        return new RobotRunsPageSummaryDto(
            RobotKey: robotKey,
            RunCount: slice.RunCount,
            EventCount: slice.EventCount,
            TotalTimeSavedSeconds: totalTimeSavedSeconds,
            FirstEventUtc: slice.FirstEventUtc,
            LastEventUtc: slice.LastEventUtc
        );
    }
}