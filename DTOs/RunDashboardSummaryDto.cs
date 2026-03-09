namespace KPIAPI.DTOs;

public record RunDashboardSummaryDto(
    string RobotKey,
    string RunId,
    int EventCount,
    int MeasurementCount,
    DateTime? FirstEventUtc,
    DateTime? LastEventUtc,
    List<KpiRollupDto> Kpis,

    // Coverage (null if not configured)
    decimal? CoveragePct,
    int? TotalItems,
    int? HitlItems,
    int? CompletedItems
);