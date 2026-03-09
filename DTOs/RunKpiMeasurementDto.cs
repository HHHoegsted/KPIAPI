using KPIAPI.Domain.Enums;

namespace KPIAPI.DTOs;

public record RunKpiMeasurementDto(
    int EventId,
    DateTime EventCreatedUtc,
    string? EventMessage,

    int KpiDefinitionId,
    string KpiKey,
    string KpiName,
    string? Unit,
    KpiValueType ValueType,

    long? IntValue,
    decimal? DecimalValue,
    bool? BoolValue,
    long? DurationMs,
    string? TextValue
);
