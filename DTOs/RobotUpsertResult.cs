// DTOs/RobotUpsertResult.cs
namespace KPIAPI.DTOs
{
    public class RobotUpsertResult
    {
        public string? Error { get; set; }
        public int? Id { get; set; }
        public string? Key { get; set; }
        public string? CenterCode { get; set; }
        public string? DisplayName { get; set; }
        public bool? IsActive { get; set; }
    }
}