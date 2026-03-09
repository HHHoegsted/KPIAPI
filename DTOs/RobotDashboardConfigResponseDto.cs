using KPIAPI.Domain.Entities;

namespace KPIAPI.DTOs
{
    public record RobotDashboardConfigResponseDto(
        string RobotKey,
        RobotDashboardConfigDto? Config
    );
}
