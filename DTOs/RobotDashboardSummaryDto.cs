namespace KPIAPI.DTOs;

public record RobotDashboardSummaryDto(
    string RobotKey,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int RunCount,
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
