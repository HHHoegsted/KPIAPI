using System.ComponentModel.DataAnnotations;

namespace KPIAPI.Domain.Entities
{
    public class LogicalRun
    {
        public int Id { get; set; }

        public int RobotId { get; set; }

        [Required, MaxLength(200)]
        public string DisplayName { get; set; } = "";

        [MaxLength(2000)]
        public string? Note { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public Robot Robot { get; set; } = null!;

        public List<LogicalRunAttempt> Attempts { get; set; } = new();
    }
}