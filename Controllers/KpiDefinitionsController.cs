using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KPIAPI.Data;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/robots/{robotKey}/kpi-definitions")]
public class KpiDefinitionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public KpiDefinitionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromRoute] string robotKey,
        [FromQuery] bool activeOnly = true)
    {
        robotKey = robotKey.Trim().ToLowerInvariant();

        var robot = await _db.Robots.AsNoTracking().FirstOrDefaultAsync(r => r.Key == robotKey);
        if (robot == null)
            return NotFound($"Robot '{robotKey}' not found");

        var query = _db.KpiDefinitions
            .AsNoTracking()
            .Where(d => d.RobotId == robot.Id);

        if (activeOnly)
            query = query.Where(d => d.IsActive);

        var defs = await query
            .OrderBy(d => d.Key)
            .Select(d => new
            {
                d.Key,
                d.Name,
                d.Unit,
                ValueType = d.ValueType,
                d.IsActive,
                d.CreatedUtc
            })
            .ToListAsync();

        return Ok(defs);
    }
}
