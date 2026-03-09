using KPIAPI.Domain.Enums;

namespace KPIAPI.Domain.Entities;

public class KpiMeasurement
{
    public int Id { get; set; }

    public int RunEventId { get; set; }
    public RunEvent RunEvent { get; set; } = null!;

    public int KpiDefinitionId { get; set; }
    public KpiDefinition KpiDefinition { get; set; } = null!;

    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    public long? IntValue { get; set; }
    public decimal? DecimalValue { get; set; }
    public bool? BoolValue { get; set; }
    public long? DurationMs { get; set; }
    public string? TextValue { get; set; }

    public KpiValueType ValueType { get; set; }
}


