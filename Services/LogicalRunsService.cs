using KPIAPI.Data;
using KPIAPI.Domain.Constants;
using KPIAPI.Domain.Entities;
using KPIAPI.Domain.Enums;
using KPIAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KPIAPI.Services;

public class LogicalRunsService
{
    private readonly AppDbContext _db;

    public LogicalRunsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<(LogicalRunDetailsDto? Result, string? Error)> CreateAsync(
        string robotKey,
        CreateLogicalRunRequest request,
        bool developerMode)
    {
        var robot = await GetRobotAsync(robotKey);
        if (robot == null)
            return (null, $"Robot '{robotKey?.Trim().ToLowerInvariant()}' not found.");

        var error = ValidateDeveloperMode(developerMode);
        if (error != null)
            return (null, error);

        if (request == null)
            return (null, "Request body is required.");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return (null, "DisplayName is required.");

        var logicalRun = new LogicalRun
        {
            RobotId = robot.Id,
            DisplayName = request.DisplayName.Trim(),
            Note = NormalizeOptional(request.Note),
            CreatedUtc = DateTime.UtcNow
        };

        _db.LogicalRuns.Add(logicalRun);
        await _db.SaveChangesAsync();

        if (request.RunIds is { Count: > 0 })
        {
            error = await AddAttemptsInternalAsync(robot, logicalRun, request.RunIds);
            if (error != null)
                return (null, error);
        }

        var result = await GetAsync(robot.Key, logicalRun.Id);
        return result == null
            ? (null, "Logical run could not be loaded after creation.")
            : (result, null);
    }

    public async Task<(LogicalRunDetailsDto? Result, string? Error)> AddAttemptsAsync(
        string robotKey,
        int logicalRunId,
        AddLogicalRunAttemptsRequest request,
        bool developerMode)
    {
        var error = ValidateDeveloperMode(developerMode);
        if (error != null)
            return (null, error);

        if (request?.RunIds == null || request.RunIds.Count == 0)
            return (null, "At least one runId is required.");

        var robot = await GetRobotAsync(robotKey);
        if (robot == null)
            return (null, $"Robot '{robotKey?.Trim().ToLowerInvariant()}' not found.");

        var logicalRun = await _db.LogicalRuns
            .Include(lr => lr.Attempts)
            .FirstOrDefaultAsync(lr => lr.Id == logicalRunId && lr.RobotId == robot.Id);

        if (logicalRun == null)
            return (null, $"Logical run '{logicalRunId}' not found for robot '{robot.Key}'.");

        error = await AddAttemptsInternalAsync(robot, logicalRun, request.RunIds);
        if (error != null)
            return (null, error);

        var result = await GetAsync(robot.Key, logicalRun.Id);
        return result == null
            ? (null, "Logical run could not be loaded after adding attempts.")
            : (result, null);
    }

    public async Task<LogicalRunDetailsDto?> GetAsync(string robotKey, int logicalRunId)
    {
        robotKey = NormalizeRobotKey(robotKey);

        if (robotKey == SystemRobotKeys.DebugOnlyRobotKey)
        {
            var robotExists = await _db.Robots.AsNoTracking().AnyAsync(r => r.Key == robotKey);
            if (!robotExists)
                return null;
        }

        var logicalRun = await _db.LogicalRuns
            .AsNoTracking()
            .Where(lr => lr.Id == logicalRunId && lr.Robot.Key == robotKey)
            .Select(lr => new
            {
                lr.Id,
                RobotKey = lr.Robot.Key,
                lr.DisplayName,
                lr.Note,
                lr.CreatedUtc,
                Attempts = lr.Attempts
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new
                    {
                        a.SortOrder,
                        a.RobotRun.RunId,
                        a.RobotRun.StartTimeUtc,
                        a.RobotRun.EndTimeUtc,
                        a.RobotRun.LastHeartbeatUtc,
                        a.RobotRun.Outcome,
                        a.RobotRun.ErrorCode,
                        a.RobotRun.ErrorMessage,
                        EventCount = a.RobotRun.Events.Count,
                        MeasurementCount = a.RobotRun.Events.SelectMany(e => e.KpiMeasurements).Count()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (logicalRun == null)
            return null;

        var attempts = logicalRun.Attempts
            .Select(a => new LogicalRunAttemptDto(
                SortOrder: a.SortOrder,
                RunId: a.RunId,
                StartTimeUtc: a.StartTimeUtc,
                EndTimeUtc: a.EndTimeUtc,
                LastHeartbeatUtc: a.LastHeartbeatUtc,
                Outcome: a.Outcome,
                ErrorCode: a.ErrorCode,
                ErrorMessage: a.ErrorMessage,
                EventCount: a.EventCount,
                MeasurementCount: a.MeasurementCount
            ))
            .ToList();

        return new LogicalRunDetailsDto(
            LogicalRunId: logicalRun.Id,
            RobotKey: logicalRun.RobotKey,
            DisplayName: logicalRun.DisplayName,
            Note: logicalRun.Note,
            CreatedUtc: logicalRun.CreatedUtc,
            StartTimeUtc: attempts.Count == 0 ? null : attempts.Min(a => a.StartTimeUtc),
            EndTimeUtc: CalculateEndTime(attempts),
            Outcome: CalculateOutcome(attempts),
            AttemptCount: attempts.Count,
            EventCount: attempts.Sum(a => a.EventCount),
            MeasurementCount: attempts.Sum(a => a.MeasurementCount),
            Attempts: attempts
        );
    }

    public async Task<List<RunKpiMeasurementDto>> GetAllKpisForLogicalRunAsync(
        string robotKey,
        int logicalRunId,
        bool developerMode = false)
    {
        robotKey = NormalizeRobotKey(robotKey);

        if (!developerMode && robotKey == SystemRobotKeys.DebugOnlyRobotKey)
            return new List<RunKpiMeasurementDto>();

        var robot = await _db.Robots
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == robotKey);

        if (robot == null)
            return new List<RunKpiMeasurementDto>();

        var logicalRunExists = await _db.LogicalRuns
            .AsNoTracking()
            .AnyAsync(lr => lr.Id == logicalRunId && lr.RobotId == robot.Id);

        if (!logicalRunExists)
            return new List<RunKpiMeasurementDto>();

        var runDbIds = await _db.LogicalRunAttempts
            .AsNoTracking()
            .Where(a => a.LogicalRunId == logicalRunId)
            .Select(a => a.RobotRunId)
            .ToListAsync();

        if (runDbIds.Count == 0)
            return new List<RunKpiMeasurementDto>();

        return await _db.RunEvents
            .AsNoTracking()
            .Where(e => runDbIds.Contains(e.RobotRunId))
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
    }

    public async Task<string?> RemoveAttemptAsync(
        string robotKey,
        int logicalRunId,
        string runId,
        bool developerMode)
    {
        var error = ValidateDeveloperMode(developerMode);
        if (error != null)
            return error;

        var robot = await GetRobotAsync(robotKey);
        if (robot == null)
            return $"Robot '{robotKey?.Trim().ToLowerInvariant()}' not found.";

        var normalizedRunId = NormalizeRunId(runId);
        if (string.IsNullOrWhiteSpace(normalizedRunId))
            return "Run ID is required.";

        var attempt = await _db.LogicalRunAttempts
            .Include(a => a.LogicalRun)
            .Include(a => a.RobotRun)
            .FirstOrDefaultAsync(a =>
                a.LogicalRunId == logicalRunId &&
                a.LogicalRun.RobotId == robot.Id &&
                a.RobotRun.RunId == normalizedRunId);

        if (attempt == null)
            return $"Run '{normalizedRunId}' is not part of logical run '{logicalRunId}'.";

        _db.LogicalRunAttempts.Remove(attempt);
        await _db.SaveChangesAsync();
        await NormalizeSortOrderAsync(logicalRunId);

        return null;
    }

    public async Task<string?> DeleteAsync(string robotKey, int logicalRunId, bool developerMode)
    {
        var error = ValidateDeveloperMode(developerMode);
        if (error != null)
            return error;

        var robot = await GetRobotAsync(robotKey);
        if (robot == null)
            return $"Robot '{robotKey?.Trim().ToLowerInvariant()}' not found.";

        var logicalRun = await _db.LogicalRuns
            .FirstOrDefaultAsync(lr => lr.Id == logicalRunId && lr.RobotId == robot.Id);

        if (logicalRun == null)
            return $"Logical run '{logicalRunId}' not found for robot '{robot.Key}'.";

        _db.LogicalRuns.Remove(logicalRun);
        await _db.SaveChangesAsync();
        return null;
    }

    private async Task<string?> AddAttemptsInternalAsync(Robot robot, LogicalRun logicalRun, IEnumerable<string> runIds)
    {
        var normalizedRunIds = runIds
            .Select(NormalizeRunId)
            .Where(runId => !string.IsNullOrWhiteSpace(runId))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedRunIds.Count == 0)
            return "At least one valid runId is required.";

        var runs = await _db.RobotRuns
            .Where(r => r.RobotId == robot.Id && normalizedRunIds.Contains(r.RunId))
            .OrderBy(r => r.StartTimeUtc)
            .ToListAsync();

        if (runs.Count != normalizedRunIds.Count)
        {
            var foundRunIds = runs.Select(r => r.RunId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingRunIds = normalizedRunIds.Where(runId => !foundRunIds.Contains(runId));
            return $"Runs not found for robot '{robot.Key}': {string.Join(", ", missingRunIds)}";
        }

        var conflictingRunIds = await _db.LogicalRunAttempts
            .Where(a => normalizedRunIds.Contains(a.RobotRun.RunId) && a.RobotRun.RobotId == robot.Id && a.LogicalRunId != logicalRun.Id)
            .Select(a => a.RobotRun.RunId)
            .Distinct()
            .ToListAsync();

        if (conflictingRunIds.Count > 0)
            return $"Runs already belong to another logical run: {string.Join(", ", conflictingRunIds)}";

        var existingRunIds = await _db.LogicalRunAttempts
            .Where(a => a.LogicalRunId == logicalRun.Id)
            .Select(a => a.RobotRun.RunId)
            .ToListAsync();

        var existingRunIdSet = existingRunIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var runsToAdd = runs.Where(r => !existingRunIdSet.Contains(r.RunId)).ToList();

        if (runsToAdd.Count == 0)
            return null;

        var nextSortOrder = await _db.LogicalRunAttempts
            .Where(a => a.LogicalRunId == logicalRun.Id)
            .Select(a => (int?)a.SortOrder)
            .MaxAsync() ?? 0;

        foreach (var run in runsToAdd)
        {
            nextSortOrder += 1;

            _db.LogicalRunAttempts.Add(new LogicalRunAttempt
            {
                LogicalRunId = logicalRun.Id,
                RobotRunId = run.Id,
                SortOrder = nextSortOrder,
                AddedUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return null;
    }

    private async Task NormalizeSortOrderAsync(int logicalRunId)
    {
        var attempts = await _db.LogicalRunAttempts
            .Where(a => a.LogicalRunId == logicalRunId)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.Id)
            .ToListAsync();

        for (var index = 0; index < attempts.Count; index += 1)
            attempts[index].SortOrder = index + 1;

        await _db.SaveChangesAsync();
    }

    private async Task<Robot?> GetRobotAsync(string robotKey)
    {
        var normalizedRobotKey = NormalizeRobotKey(robotKey);
        if (string.IsNullOrWhiteSpace(normalizedRobotKey))
            return null;

        return await _db.Robots.FirstOrDefaultAsync(r => r.Key == normalizedRobotKey);
    }

    private static string? ValidateDeveloperMode(bool developerMode)
    {
        return developerMode ? null : "Logical run changes are only available in developer mode.";
    }

    private static string NormalizeRobotKey(string robotKey)
    {
        return robotKey?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    private static string NormalizeRunId(string runId)
    {
        return runId?.Trim() ?? string.Empty;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime? CalculateEndTime(IEnumerable<LogicalRunAttemptDto> attempts)
    {
        var attemptList = attempts.ToList();
        return attemptList.Any(a => a.EndTimeUtc == null)
            ? null
            : attemptList.Max(a => a.EndTimeUtc);
    }

    private static LogicalRunOutcome CalculateOutcome(IEnumerable<LogicalRunAttemptDto> attempts)
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

        return attemptList.All(a => a.Outcome != null)
            ? LogicalRunOutcome.Failed
            : LogicalRunOutcome.Unknown;
    }
}