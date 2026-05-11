namespace KPIAPI.Domain.Entities
{
    public class LogicalRunAttempt
    {
        public int Id { get; set; }

        public int LogicalRunId { get; set; }

        public int RobotRunId { get; set; }

        public int SortOrder { get; set; }

        public DateTime AddedUtc { get; set; } = DateTime.UtcNow;

        public LogicalRun LogicalRun { get; set; } = null!;

        public RobotRun RobotRun { get; set; } = null!;
    }
}