using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KPIAPI.Data;
using KPIAPI.Domain.Entities;
using KPIAPI.DTOs;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/robots/{robotKey}/dashboard-config")]
public class RobotDashboardConfigController : ControllerBase
{
    private readonly AppDbContext _db;

    public RobotDashboardConfigController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<RobotDashboardConfigResponseDto>> Get([FromRoute] string robotKey)
    {
        robotKey = robotKey.Trim().ToLowerInvariant();

        var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
        if (robot == null)
            return NotFound($"Robot '{robotKey}' not found");

        var cfg = await _db.RobotDashboardConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.RobotId == robot.Id);

        var dto = new RobotDashboardConfigResponseDto(
            RobotKey: robotKey,
            Config: cfg == null
                ? null
                : new RobotDashboardConfigDto(
                    TotalItemsKpiKey: cfg.TotalItemsKpiKey,
                    HitlItemsKpiKey: cfg.HitlItemsKpiKey,
                    TotalItemsAggregation: cfg.TotalItemsAggregation,
                    HitlItemsAggregation: cfg.HitlItemsAggregation,
                    FilterKpiKey: cfg.FilterKpiKey,
                    FilterKpiTextEquals: cfg.FilterKpiTextEquals
                )
        );

        return Ok(dto);
    }

    [HttpPut]
    public async Task<ActionResult> Put([FromRoute] string robotKey, [FromBody] RobotDashboardConfigDto dto)
    {
        robotKey = robotKey.Trim().ToLowerInvariant();

        var robot = await _db.Robots.FirstOrDefaultAsync(r => r.Key == robotKey);
        if (robot == null)
            return NotFound($"Robot '{robotKey}' not found");

        string? normKey(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim().ToLowerInvariant();
        string? normText(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        var cfg = await _db.RobotDashboardConfigs.FirstOrDefaultAsync(c => c.RobotId == robot.Id);

        if (cfg == null)
        {
            cfg = new RobotDashboardConfig
            {
                RobotId = robot.Id,
                CreatedUtc = DateTime.UtcNow
            };
            _db.RobotDashboardConfigs.Add(cfg);
        }

        cfg.TotalItemsKpiKey = normKey(dto.TotalItemsKpiKey);
        cfg.HitlItemsKpiKey = normKey(dto.HitlItemsKpiKey);
        cfg.TotalItemsAggregation = dto.TotalItemsAggregation;
        cfg.HitlItemsAggregation = dto.HitlItemsAggregation;

        cfg.FilterKpiKey = normKey(dto.FilterKpiKey);
        cfg.FilterKpiTextEquals = normText(dto.FilterKpiTextEquals);

        cfg.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}

