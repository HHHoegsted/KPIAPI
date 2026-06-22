namespace KPIAPI.DTOs
{
    public record PaginatedRunListDto(
        List<RunListItemDto> Items,
        int TotalCount,
        int Offset,
        int Limit
    );
}