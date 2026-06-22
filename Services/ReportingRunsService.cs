using KPIAPI.Data;
using KPIAPI.Domain.Entities;
using KPIAPI.Domain.Enums;
using KPIAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KPIAPI.Services;

public class ReportingRunsService
{
    private readonly AppDbContext _db;

    public ReportingRunsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReportingRunSlice> BuildAsync(
        Robot robot,
        DateTime? fromUtc,
        DateTime? toUtc,
        string sort,
        int? limit = null,
        int offset = 0)
    {
        var from = NormalizeUtc(fromUtc);
        var to = NormalizeUtc(toUtc);
        sort = (sort ?? "desc").Trim().ToLowerInvariant();

        var runFacts = await _db.RobotRuns
            .AsNoTracking()
            .Where(r => r.RobotId == robot.Id)
            .Select(r => new PhysicalRunFact(
                r.Id,
                r.RunId,
                r.StartTimeUtc,
                r.EndTimeUtc,
                r.Outcome
            ))
            .ToListAsync();

        if (runFacts.Count == 0)
            return new ReportingRunSlice(new List<RunListItemDto>(), 0, 0, null, null);

        var runDbIds = runFacts.Select(r => r.Id).ToList();

        var eventFacts = await _db.RunEvents
            .AsNoTracking()
            .Where(e => runDbIds.Contains(e.RobotRunId))
            .GroupBy(e => e.RobotRunId)
            .Select(g => new
            {
                RunDbId = g.Key,
                Count = g.Count(),
                FirstEventUtc = (DateTime?)g.Min(x => x.CreatedUtc),
                LastEventUtc = (DateTime?)g.Max(x => x.CreatedUtc)
            })
            .ToDictionaryAsync(
                x => x.RunDbId,
                x => new EventStat(x.Count, x.FirstEventUtc, x.LastEventUtc));

        var measurementCounts = await _db.KpiMeasurements
            .AsNoTracking()
            .Where(m => runDbIds.Contains(m.RunEvent.RobotRunId))
            .GroupBy(m => m.RunEvent.RobotRunId)
            .Select(g => new { RunDbId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RunDbId, x => x.Count);

        var runDetails = runFacts
            .Select(r => new PhysicalRunDetail(
                r.Id,
                r.RunId,
                r.StartTimeUtc,
                r.EndTimeUtc,
                r.Outcome,
                eventFacts.TryGetValue(r.Id, out var eventStat) ? eventStat.Count : 0,
                measurementCounts.TryGetValue(r.Id, out var measurementCount) ? measurementCount : 0,
                eventFacts.TryGetValue(r.Id, out eventStat) ? eventStat.FirstEventUtc : null,
                eventFacts.TryGetValue(r.Id, out eventStat) ? eventStat.LastEventUtc : null
            ))
            .ToDictionary(r => r.Id);

        var logicalRuns = await _db.LogicalRuns
            .AsNoTracking()
            .Where(lr => lr.RobotId == robot.Id)
            .Select(lr => new LogicalRunFact(lr.Id, lr.DisplayName, lr.CreatedUtc))
            .ToListAsync();

        var logicalRunIds = logicalRuns.Select(lr => lr.Id).ToList();
        var attemptFacts = logicalRunIds.Count == 0
            ? new List<LogicalRunAttemptFact>()
            : await _db.LogicalRunAttempts
                .AsNoTracking()
                .Where(a => logicalRunIds.Contains(a.LogicalRunId))
                .Select(a => new LogicalRunAttemptFact(a.LogicalRunId, a.RobotRunId, a.SortOrder))
                .ToListAsync();

        var allGroupedRunIds = attemptFacts
            .Select(a => a.RobotRunId)
            .ToHashSet();

        var reportingRows = new List<RunListItemDto>();
        var includedRunIds = new HashSet<int>();

        foreach (var logicalRun in logicalRuns)
        {
            var attempts = attemptFacts
                .Where(a => a.LogicalRunId == logicalRun.Id)
                .OrderBy(a => a.SortOrder)
                .Select(a => runDetails.GetValueOrDefault(a.RobotRunId))
                .Where(r => r != null)
                .Cast<PhysicalRunDetail>()
                .ToList();

            if (attempts.Count == 0)
                continue;

            var startTimeUtc = attempts.Min(a => a.StartTimeUtc);

            if (!MatchesWindow(startTimeUtc, from, to))
                continue;

            foreach (var attempt in attempts)
                includedRunIds.Add(attempt.Id);

            reportingRows.Add(new RunListItemDto(
                Kind: ReportingRunKind.Logical,
                RunId: null,
                LogicalRunId: logicalRun.Id,
                DisplayName: logicalRun.DisplayName,
                StartTimeUtc: startTimeUtc,
                EndTimeUtc: attempts.Any(a => a.EndTimeUtc == null) ? null : attempts.Max(a => a.EndTimeUtc),
                PhysicalOutcome: null,
                LogicalOutcome: CalculateLogicalOutcome(attempts),
                AttemptCount: attempts.Count,
                EventCount: attempts.Sum(a => a.EventCount),
                MeasurementCount: attempts.Sum(a => a.MeasurementCount)
            ));
        }

        foreach (var run in runDetails.Values.Where(r => !allGroupedRunIds.Contains(r.Id)))
        {
            if (!MatchesWindow(run.StartTimeUtc, from, to))
                continue;

            includedRunIds.Add(run.Id);

            reportingRows.Add(new RunListItemDto(
                Kind: ReportingRunKind.Physical,
                RunId: run.RunId,
                LogicalRunId: null,
                DisplayName: null,
                StartTimeUtc: run.StartTimeUtc,
                EndTimeUtc: run.EndTimeUtc,
                PhysicalOutcome: run.Outcome,
                LogicalOutcome: null,
                AttemptCount: 1,
                EventCount: run.EventCount,
                MeasurementCount: run.MeasurementCount
            ));
        }

        reportingRows = sort == "asc"
            ? reportingRows.OrderBy(r => r.StartTimeUtc).ToList()
            : reportingRows.OrderByDescending(r => r.StartTimeUtc).ToList();

        var totalRunCount = reportingRows.Count;

        if (offset > 0)
            reportingRows = reportingRows.Skip(offset).ToList();

        if (limit is > 0)
            reportingRows = reportingRows.Take(limit.Value).ToList();

        var includedRunDetails = includedRunIds
            .Select(id => runDetails.GetValueOrDefault(id))
            .Where(r => r != null)
            .Cast<PhysicalRunDetail>()
            .ToList();

        var eventCount = includedRunDetails.Sum(r => r.EventCount);
        var firstEventUtc = includedRunDetails
            .Where(r => r.FirstEventUtc != null)
            .Select(r => r.FirstEventUtc)
            .Min();
        var lastEventUtc = includedRunDetails
            .Where(r => r.LastEventUtc != null)
            .Select(r => r.LastEventUtc)
            .Max();

        return new ReportingRunSlice(
            reportingRows,
            totalRunCount,
            eventCount,
            firstEventUtc,
            lastEventUtc
        );
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value == null)
            return null;

        return value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();
    }

    private static bool MatchesWindow(DateTime startTimeUtc, DateTime? fromUtc, DateTime? toUtc)
    {
        if (fromUtc != null && startTimeUtc < fromUtc.Value)
            return false;

        if (toUtc != null && startTimeUtc > toUtc.Value)
            return false;

        return true;
    }

    private static LogicalRunOutcome CalculateLogicalOutcome(IEnumerable<PhysicalRunDetail> attempts)
    {
        var attemptList = attempts.ToList();

        if (attemptList.Count == 0)
            return LogicalRunOutcome.Unknown;

        if (attemptList.Any(a => a.Outcome == null))
            return LogicalRunOutcome.InProgress;

        var hasSucceeded = attemptList.Any(a => a.Outcome == RunOutcome.Succeeded);
        if (hasSucceeded && attemptList.Count > 1)
            return LogicalRunOutcome.SucceededAfterRetry;

        if (hasSucceeded)
            return LogicalRunOutcome.Succeeded;

        return LogicalRunOutcome.Failed;
    }

    private sealed record PhysicalRunFact(
        int Id,
        string RunId,
        DateTime StartTimeUtc,
        DateTime? EndTimeUtc,
        RunOutcome? Outcome
    );

    private sealed record PhysicalRunDetail(
        int Id,
        string RunId,
        DateTime StartTimeUtc,
        DateTime? EndTimeUtc,
        RunOutcome? Outcome,
        int EventCount,
        int MeasurementCount,
        DateTime? FirstEventUtc,
        DateTime? LastEventUtc
    );

    private sealed record LogicalRunFact(int Id, string DisplayName, DateTime CreatedUtc);

    private sealed record LogicalRunAttemptFact(int LogicalRunId, int RobotRunId, int SortOrder);

    private sealed record EventStat(int Count, DateTime? FirstEventUtc, DateTime? LastEventUtc);
}

public record ReportingRunSlice(
    List<RunListItemDto> Items,
    int RunCount,
    int EventCount,
    DateTime? FirstEventUtc,
    DateTime? LastEventUtc
);