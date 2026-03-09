using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs;

public record CompleteRunRequest(
    DateTime? EndTimeUtc,
    RunOutcome Outcome,
    string? ErrorCode,
    string? ErrorMessage
);

