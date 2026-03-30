namespace KPIAPI.DTOs
{
    public record RunEventDto(
        int EventId,
        DateTime CreatedUtc,
        string? Message,
        string EventType,
        string? CorrelationKey,
        string? PayloadJson,
        bool IsDebug,
        List<RunKpiMeasurementDto> Kpis
    );
}
