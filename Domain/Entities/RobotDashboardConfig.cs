using System.ComponentModel.DataAnnotations;

namespace KPIAPI.Domain.Entities;

public class RobotDashboardConfig
{
    public int Id { get; set; }

    [Required]
    public int RobotId { get; set; }
    public Robot Robot { get; set; } = null!;

    [MaxLength(128)]
    public string? TotalItemsKpiKey { get; set; }

    [MaxLength(128)]
    public string? HitlItemsKpiKey { get; set; }

    public CoverageKpiAggregation TotalItemsAggregation { get; set; } = CoverageKpiAggregation.Sum;
    public CoverageKpiAggregation HitlItemsAggregation { get; set; } = CoverageKpiAggregation.Sum;

    [MaxLength(128)]
    public string? FilterKpiKey { get; set; }

    [MaxLength(128)]
    public string? FilterKpiTextEquals { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
}

public enum CoverageKpiAggregation
{
    Sum = 0,
    TrueCount = 1
}

