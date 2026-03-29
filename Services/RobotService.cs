using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using KPIAPI.DTOs;
using KPIAPI.Data;
using KPIAPI.Domain;
using KPIAPI.Domain.Entities;

public class RobotService
{
    private readonly AppDbContext _db;

    public RobotService(AppDbContext db)
    {
        _db = db;
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

    public async Task<List<object>> ListAsync(bool hasDataOnly)
    {
        var robotsQuery = _db.Robots.AsNoTracking();

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

        return await _db.Robots.AsNoTracking()
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

    public async Task<RobotRunsPageSummaryDto?> GetRobotSummaryAsync(string robotKey, DateTime? fromUtc, DateTime? toUtc)
    {
        robotKey = robotKey.Trim().ToLowerInvariant();

        var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
        if (robot == null)
            return null;

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

        return new RobotRunsPageSummaryDto(
            RobotKey: robotKey,
            RunCount: runCount,
            EventCount: eventCount,
            FirstEventUtc: firstEventUtc,
            LastEventUtc: lastEventUtc
        );
    }
}