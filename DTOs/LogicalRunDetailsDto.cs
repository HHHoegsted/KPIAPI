using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs;

public record LogicalRunDetailsDto(
    int LogicalRunId,
    string RobotKey,
    string DisplayName,
    string? Note,
    DateTime CreatedUtc,
    DateTime? StartTimeUtc,
    DateTime? EndTimeUtc,
    LogicalRunOutcome Outcome,
    int AttemptCount,
    int EventCount,
    int MeasurementCount,
    List<LogicalRunAttemptDto> Attempts
);