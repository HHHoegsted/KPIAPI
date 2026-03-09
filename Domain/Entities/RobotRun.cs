using System.ComponentModel.DataAnnotations;
using KPIAPI.Domain.Enums;

namespace KPIAPI.Domain.Entities
{
    public class RobotRun
    {
        public int Id { get; set; }

        public int RobotId { get; set; }

        [Required, MaxLength(200)]
        public string RunId { get; set; } = "";

        public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;

        public DateTime? EndTimeUtc { get; set; }

        public DateTime? LastHeartbeatUtc { get; set; }

        public RunOutcome? Outcome { get; set; }

        [MaxLength(100)]
        public string? ErrorCode { get; set; }

        [MaxLength(200)]
        public string? ErrorMessage { get; set; }

        //Navigation properties
        
        public List<RunEvent> Events { get; set; } = new();

        public Robot Robot { get; set; } = null!;
    }
}
