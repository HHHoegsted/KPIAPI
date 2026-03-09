using System.ComponentModel.DataAnnotations;

namespace KPIAPI.Domain.Entities
{
    public class Robot
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Key { get; set; } = "";

        [Required, MaxLength(200)]
        public string DisplayName { get; set; } = "";

        [Required, MaxLength(10)]
        public string CenterCode { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public List<RobotRun> Runs { get; set; } = new();
        public List<KpiDefinition> KpiDefinitions { get; set; } = new();
    }
}
