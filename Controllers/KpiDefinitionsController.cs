using Microsoft.AspNetCore.Mvc;
using KPIAPI.Services;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/robots/{robotKey}/kpi-definitions")]
public class KpiDefinitionsController : ControllerBase
{
    private readonly KpiDefinitionsService _kpiDefinitionsService;

    public KpiDefinitionsController(KpiDefinitionsService kpiDefinitionsService)
    {
        _kpiDefinitionsService = kpiDefinitionsService;
    }

    [HttpGet]
    public async Task<ActionResult> List(
        [FromRoute] string robotKey,
        [FromQuery] bool activeOnly = true)
    {
        var defs = await _kpiDefinitionsService.ListAsync(robotKey, activeOnly);
        if (defs == null)
            return NotFound($"Robot '{robotKey?.Trim().ToLowerInvariant()}' not found");
        return Ok(defs);
    }
}
