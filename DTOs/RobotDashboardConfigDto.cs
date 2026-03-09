using KPIAPI.Domain.Entities;

namespace KPIAPI.DTOs
{
    public record RobotDashboardConfigDto(
        string? TotalItemsKpiKey,
        string? HitlItemsKpiKey,
        CoverageKpiAggregation TotalItemsAggregation,
        CoverageKpiAggregation HitlItemsAggregation,
        string? FilterKpiKey,
        string? FilterKpiTextEquals
    );

}
