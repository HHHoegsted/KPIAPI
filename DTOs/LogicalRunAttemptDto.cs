using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs;

public record LogicalRunAttemptDto(
    int SortOrder,
    string RunId,
    DateTime StartTimeUtc,
    DateTime? EndTimeUtc,
    DateTime? LastHeartbeatUtc,
    RunOutcome? Outcome,
    string? ErrorCode,
    string? ErrorMessage,
    int EventCount,
    int MeasurementCount
);