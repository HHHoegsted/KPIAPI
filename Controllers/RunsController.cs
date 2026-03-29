using KPIAPI.DTOs;
using KPIAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace KPIAPI.Controllers
{
    [ApiController]
    [Route("api/robots/{robotKey}/runs")]
    public class RunsController : ControllerBase
    {
        private readonly RunsService _runsService;

        public RunsController(RunsService runsService)
        {
            _runsService = runsService;
        }

        [HttpPost("start")]
        public async Task<ActionResult> Start([FromRoute] string robotKey, [FromBody] StartRunRequest? request)
        {
            var result = await _runsService.StartAsync(robotKey, request);
            var errorProp = result?.GetType().GetProperty("Error");
            if (errorProp != null)
            {
                var error = errorProp.GetValue(result) as string;
                if (!string.IsNullOrEmpty(error))
                    return BadRequest(error);
            }
            return Ok(result);
        }

        [HttpPost("{runId}/complete")]
        public async Task<ActionResult> Complete([FromRoute] string robotKey, [FromRoute] string runId, [FromBody] CompleteRunRequest request)
        {
            var error = await _runsService.CompleteAsync(robotKey, runId, request);
            if (!string.IsNullOrEmpty(error))
                return BadRequest(error);
            return NoContent();
        }

        [HttpPost("{runId}/heartbeat")]
        public async Task<ActionResult> Heartbeat([FromRoute] string robotKey, [FromRoute] string runId, [FromBody] RunHeartbeatRequest? request)
        {
            var error = await _runsService.HeartbeatAsync(robotKey, runId, request);
            if (!string.IsNullOrEmpty(error))
                return BadRequest(error);
            return NoContent();
        }

        [HttpGet("{runId}/kpis")]
        public async Task<ActionResult<List<RunKpiMeasurementDto>>> GetAllKpisForRun([FromRoute] string robotKey, [FromRoute] string runId)
        {
            var result = await _runsService.GetAllKpisForRunAsync(robotKey, runId);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<List<RunListItemDto>>> ListRunsForRobot(
            [FromRoute] string robotKey,
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] int limit = 200,
            [FromQuery] string sort = "desc")
        {
            var result = await _runsService.ListRunsForRobotAsync(robotKey, fromUtc, limit, sort);
            return Ok(result);
        }

        [HttpGet("{runId}")]
        public async Task<ActionResult<RunDetailsDto>> GetRun([FromRoute] string robotKey, [FromRoute] string runId)
        {
            var result = await _runsService.GetRunAsync(robotKey, runId);
            if (result == null)
                return NotFound($"Run '{runId}' not found for robot '{robotKey}'");
            return Ok(result);
        }
    }
}
