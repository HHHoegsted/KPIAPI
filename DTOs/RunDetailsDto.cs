using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs;

public record RunDetailsDto(
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