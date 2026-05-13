namespace KPIAPI.DTOs;

public record RobotRunsPageSummaryDto(
    string RobotKey,
    int RunCount,
    int EventCount,
    long TotalTimeSavedSeconds,
    DateTime? FirstEventUtc,
    DateTime? LastEventUtc
);