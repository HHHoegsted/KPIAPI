using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs
{
    public record RunListItemDto(
        string RunId,
        DateTime StartTimeUtc,
        DateTime? EndTimeUtc,
        RunOutcome? Outcome,
        int EventCount,
        int MeasurementCount
    );
}