using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs;

public record KPIDTO(
    string Key,
    string Name,
    KpiValueType ValueType,
    string? Unit,
    long? IntValue,
    decimal? DecimalValue,
    bool? BoolValue,
    long? DurationMs,
    string? TextValue
);

