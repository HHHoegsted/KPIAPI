using System.ComponentModel.DataAnnotations;
using KPIAPI.Domain.Enums;

namespace KPIAPI.Domain.Entities
{
    public class KpiDefinition
    {
        public int Id { get; set; }
        
        public int RobotId { get; set; }

        public Robot Robot { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Key { get; set; } = "";

        [Required, MaxLength(200)]
        public string Name { get; set; } = "";

        [MaxLength(100)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Unit { get; set; }

        public KpiValueType ValueType { get; set; }

        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public List<KpiMeasurement> KpiMeasurements { get; set; } = new();
    }
}
