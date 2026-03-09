using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs
{
    public record KpiRollupDto(
        string Key,
        string Name,
        string? Unit,
        KpiValueType ValueType,
        int Count,
        DateTime? FirstRecordedUtc,
        DateTime? LastRecordedUtc,

        decimal? Sum,
        decimal? Avg,
        decimal? Min,
        decimal? Max,

        int? TrueCount,
        int? FalseCount,
        Dictionary<string, int>? TopTextValues
    );
}
