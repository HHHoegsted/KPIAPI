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

    public async Task<ReportingRunSlice> BuildPageAsync(
        Robot robot,
        DateTime? fromUtc,
        string sort,
        int limit,
        int offset)
    {
        var reportingRows = await BuildReportingRowsAsync(robot, fromUtc, null);

        reportingRows = sort == "asc"
            ? reportingRows.OrderBy(r => r.StartTimeUtc).ToList()
            : reportingRows.OrderByDescending(r => r.StartTimeUtc).ToList();

        var totalRunCount = reportingRows.Count;

        var pageRows = reportingRows
            .Skip(offset)
            .Take(limit)
            .ToList();

        if (pageRows.Count == 0)
            return new ReportingRunSlice(new List<RunListItemDto>(), totalRunCount, 0, null, null);

        var selectedRunDbIds = pageRows
            .SelectMany(r => r.PhysicalRunIds)
            .Distinct()
            .ToList();

        var eventFacts = await _db.RunEvents
            .AsNoTracking()
            .Where(e => selectedRunDbIds.Contains(e.RobotRunId))
            .GroupBy(e => e.RobotRunId)
            .Select(g => new
            {
                RunDbId = g.Key,
                Count = g.Count(),
                FirstEventUtc = (DateTime?)g.Min(e => e.CreatedUtc),
                LastEventUtc = (DateTime?)g.Max(e => e.CreatedUtc)
            })
            .ToDictionaryAsync(
                x => x.RunDbId,
                x => new EventStat(x.Count, x.FirstEventUtc, x.LastEventUtc));

        var measurementCounts = await _db.KpiMeasurements
            .AsNoTracking()
            .Where(m => selectedRunDbIds.Contains(m.RunEvent.RobotRunId))
            .GroupBy(m => m.RunEvent.RobotRunId)
            .Select(g => new
            {
                RunDbId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.RunDbId, x => x.Count);

        var items = pageRows
            .Select(row => new RunListItemDto(
                Kind: row.Kind,
                RunId: row.RunId,
                LogicalRunId: row.LogicalRunId,
                DisplayName: row.DisplayName,
                StartTimeUtc: row.StartTimeUtc,
                EndTimeUtc: row.EndTimeUtc,
                PhysicalOutcome: row.PhysicalOutcome,
                LogicalOutcome: row.LogicalOutcome,
                AttemptCount: row.AttemptCount,
                EventCount: row.PhysicalRunIds.Sum(id => eventFacts.GetValueOrDefault(id)?.Count ?? 0),
                MeasurementCount: row.PhysicalRunIds.Sum(id => measurementCounts.GetValueOrDefault(id))
            ))
            .ToList();

        var selectedEventStats = selectedRunDbIds
            .Select(id => eventFacts.GetValueOrDefault(id))
            .Where(stat => stat != null)
            .Cast<EventStat>()
            .ToList();

        var eventCount = selectedEventStats.Sum(stat => stat.Count);

        var firstEventUtc = selectedEventStats
            .Where(stat => stat.FirstEventUtc != null)
            .Select(stat => stat.FirstEventUtc)
            .DefaultIfEmpty(null)
            .Min();

        var lastEventUtc = selectedEventStats
            .Where(stat => stat.LastEventUtc != null)
            .Select(stat => stat.LastEventUtc)
            .DefaultIfEmpty(null)
            .Max();

        return new ReportingRunSlice(
            items,
            totalRunCount,
            eventCount,
            firstEventUtc,
            lastEventUtc
        );
    }

    public async Task<ReportingRunSlice> BuildSummaryAsync(
        Robot robot,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var reportingRows = await BuildReportingRowsAsync(robot, fromUtc, toUtc);

        if (reportingRows.Count == 0)
            return new ReportingRunSlice(new List<RunListItemDto>(), 0, 0, null, null);

        var includedRunDbIds = reportingRows
            .SelectMany(r => r.PhysicalRunIds)
            .Distinct()
            .ToList();

        var eventQuery = _db.RunEvents
            .AsNoTracking()
            .Where(e => includedRunDbIds.Contains(e.RobotRunId));

        var eventCount = await eventQuery.CountAsync();

        DateTime? firstEventUtc = null;
        DateTime? lastEventUtc = null;

        if (eventCount > 0)
        {
            firstEventUtc = await eventQuery
                .Select(e => (DateTime?)e.CreatedUtc)
                .MinAsync();

            lastEventUtc = await eventQuery
                .Select(e => (DateTime?)e.CreatedUtc)
                .MaxAsync();
        }

        return new ReportingRunSlice(
            new List<RunListItemDto>(),
            reportingRows.Count,
            eventCount,
            firstEventUtc,
            lastEventUtc
        );
    }

    private async Task<List<ReportingRowFact>> BuildReportingRowsAsync(
        Robot robot,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var from = NormalizeUtc(fromUtc);
        var to = NormalizeUtc(toUtc);

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
            return new List<ReportingRowFact>();

        var runFactsById = runFacts.ToDictionary(r => r.Id);

        var logicalRuns = await _db.LogicalRuns
            .AsNoTracking()
            .Where(lr => lr.RobotId == robot.Id)
            .Select(lr => new LogicalRunFact(
                lr.Id,
                lr.DisplayName
            ))
            .ToListAsync();

        var logicalRunIds = logicalRuns
            .Select(lr => lr.Id)
            .ToList();

        var attemptFacts = logicalRunIds.Count == 0
            ? new List<LogicalRunAttemptFact>()
            : await _db.LogicalRunAttempts
                .AsNoTracking()
                .Where(a => logicalRunIds.Contains(a.LogicalRunId))
                .Select(a => new LogicalRunAttemptFact(
                    a.LogicalRunId,
                    a.RobotRunId,
                    a.SortOrder
                ))
                .ToListAsync();

        var attemptsByLogicalRunId = attemptFacts
            .GroupBy(a => a.LogicalRunId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(a => a.SortOrder).ToList());

        var allGroupedRunIds = attemptFacts
            .Select(a => a.RobotRunId)
            .ToHashSet();

        var reportingRows = new List<ReportingRowFact>();

        foreach (var logicalRun in logicalRuns)
        {
            if (!attemptsByLogicalRunId.TryGetValue(logicalRun.Id, out var logicalRunAttempts))
                continue;

            var attempts = logicalRunAttempts
                .Select(a => runFactsById.GetValueOrDefault(a.RobotRunId))
                .Where(r => r != null)
                .Cast<PhysicalRunFact>()
                .ToList();

            if (attempts.Count == 0)
                continue;

            var startTimeUtc = attempts.Min(a => a.StartTimeUtc);

            if (!MatchesWindow(startTimeUtc, from, to))
                continue;

            var endTimeUtc = attempts.Any(a => a.EndTimeUtc == null)
                ? null
                : attempts.Max(a => a.EndTimeUtc);

            reportingRows.Add(new ReportingRowFact(
                Kind: ReportingRunKind.Logical,
                RunId: null,
                LogicalRunId: logicalRun.Id,
                DisplayName: logicalRun.DisplayName,
                StartTimeUtc: startTimeUtc,
                EndTimeUtc: endTimeUtc,
                PhysicalOutcome: null,
                LogicalOutcome: CalculateLogicalOutcome(attempts),
                AttemptCount: attempts.Count,
                PhysicalRunIds: attempts.Select(a => a.Id).ToList()
            ));
        }

        foreach (var run in runFacts.Where(r => !allGroupedRunIds.Contains(r.Id)))
        {
            if (!MatchesWindow(run.StartTimeUtc, from, to))
                continue;

            reportingRows.Add(new ReportingRowFact(
                Kind: ReportingRunKind.Physical,
                RunId: run.RunId,
                LogicalRunId: null,
                DisplayName: null,
                StartTimeUtc: run.StartTimeUtc,
                EndTimeUtc: run.EndTimeUtc,
                PhysicalOutcome: run.Outcome,
                LogicalOutcome: null,
                AttemptCount: 1,
                PhysicalRunIds: new List<int> { run.Id }
            ));
        }

        return reportingRows;
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

    private static LogicalRunOutcome CalculateLogicalOutcome(IEnumerable<PhysicalRunFact> attempts)
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

    private sealed record LogicalRunFact(
        int Id,
        string DisplayName
    );

    private sealed record LogicalRunAttemptFact(
        int LogicalRunId,
        int RobotRunId,
        int SortOrder
    );

    private sealed record ReportingRowFact(
        ReportingRunKind Kind,
        string? RunId,
        int? LogicalRunId,
        string? DisplayName,
        DateTime StartTimeUtc,
        DateTime? EndTimeUtc,
        RunOutcome? PhysicalOutcome,
        LogicalRunOutcome? LogicalOutcome,
        int AttemptCount,
        List<int> PhysicalRunIds
    );

    private sealed record EventStat(
        int Count,
        DateTime? FirstEventUtc,
        DateTime? LastEventUtc
    );
}

public record ReportingRunSlice(
    List<RunListItemDto> Items,
    int RunCount,
    int EventCount,
    DateTime? FirstEventUtc,
    DateTime? LastEventUtc
);