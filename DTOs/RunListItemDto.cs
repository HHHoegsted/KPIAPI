namespace KPIAPI.DTOs
{
    public record RunListItemDto(
        string RunId,
        DateTime StartTimeUtc,
        DateTime? EndTimeUtc,
        int EventCount,
        int MeasurementCount
    );
}
