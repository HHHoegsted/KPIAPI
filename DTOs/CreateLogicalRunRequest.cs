namespace KPIAPI.DTOs;

public record CreateLogicalRunRequest(
    string DisplayName,
    string? Note,
    List<string>? RunIds
);