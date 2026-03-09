namespace KPIAPI.DTOs;

public record RecordRunEventRequest(
    DateTime? CreatedUtc,
    string? Message,
    string? EventType,
    string? CorrelationKey,
    object? Payload,
    List<KPIDTO> Kpis
);
