using KPIAPI.Data;
using KPIAPI.Domain;
using KPIAPI.Domain.Entities;
using KPIAPI.Domain.Enums;
using KPIAPI.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace KPIAPI.Controllers
{
    [ApiController]
    [Route("api/robots")]
    public class RobotsController : ControllerBase
    {
        private readonly RobotService _robotService;

        public RobotsController(RobotService robotService)
        {
            _robotService = robotService;
        }

        
        [HttpPost("upsert")]
        public async Task<ActionResult> Upsert([FromBody] RobotUpsertRequest request)
        {
            var result = await _robotService.UpsertAsync(request);
            if (!string.IsNullOrEmpty(result.Error))
                return BadRequest(result.Error);

            return Ok(result);
        }

        
        [HttpGet]
        public async Task<ActionResult> List([FromQuery] bool hasDataOnly = true)
        {
            var robots = await _robotService.ListAsync(hasDataOnly);
            return Ok(robots);
        }

        
        [HttpGet("{robotKey}/summary")]
        public async Task<ActionResult<RobotRunsPageSummaryDto>> GetRobotSummary(
            [FromRoute] string robotKey,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null)
        {
            var summary = await _robotService.GetRobotSummaryAsync(robotKey, fromUtc, toUtc);
            if (summary == null)
                return NotFound($"Robot '{robotKey?.Trim().ToLowerInvariant()}' not found");

            return Ok(summary);
        }
    }
}
