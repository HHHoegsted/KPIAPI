using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs
{
    public record RunListItemDto(
        ReportingRunKind Kind,
        string? RunId,
        int? LogicalRunId,
        string? DisplayName,
        DateTime StartTimeUtc,
        DateTime? EndTimeUtc,
        RunOutcome? PhysicalOutcome,
        LogicalRunOutcome? LogicalOutcome,
        int AttemptCount,
        int EventCount,
        int MeasurementCount
    );
}