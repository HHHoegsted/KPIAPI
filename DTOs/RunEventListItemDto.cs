namespace KPIAPI.DTOs
{
    public record RunEventListItemDto(
        int EventId,
        DateTime CreatedUtc,
        string? Message,
        string EventType,
        bool IsDebug
    );
}
