using System.ComponentModel.DataAnnotations;

namespace KPIAPI.Domain.Entities;

public class RunEvent
{
    public int Id { get; set; }

    public int RobotRunId { get; set; }
    public RobotRun RobotRun { get; set; } = null!;

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(2000)]
    public string? Message { get; set; }

    public string EventType { get; set; } = "Info";
    public string? CorrelationKey { get; set; }
    public string? PayloadJson { get; set; }

    public List<KpiMeasurement> KpiMeasurements { get; set; } = new();
}

