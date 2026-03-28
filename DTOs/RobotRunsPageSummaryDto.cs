namespace KPIAPI.DTOs;

public record RobotRunsPageSummaryDto(
    string RobotKey,
    int RunCount,
    int EventCount,
    DateTime? FirstEventUtc,
    DateTime? LastEventUtc
);