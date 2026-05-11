using KPIAPI.DTOs;
using KPIAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/robots/{robotKey}/logical-runs")]
public class LogicalRunsController : ControllerBase
{
    private readonly LogicalRunsService _logicalRunsService;

    public LogicalRunsController(LogicalRunsService logicalRunsService)
    {
        _logicalRunsService = logicalRunsService;
    }

    [HttpPost]
    public async Task<ActionResult<LogicalRunDetailsDto>> Create(
        [FromRoute] string robotKey,
        [FromBody] CreateLogicalRunRequest request,
        [FromQuery] bool developerMode = false)
    {
        var (result, error) = await _logicalRunsService.CreateAsync(robotKey, request, developerMode);
        if (!string.IsNullOrEmpty(error))
            return BadRequest(error);

        return CreatedAtAction(nameof(Get), new { robotKey, logicalRunId = result!.LogicalRunId }, result);
    }

    [HttpPost("{logicalRunId:int}/attempts")]
    public async Task<ActionResult<LogicalRunDetailsDto>> AddAttempts(
        [FromRoute] string robotKey,
        [FromRoute] int logicalRunId,
        [FromBody] AddLogicalRunAttemptsRequest request,
        [FromQuery] bool developerMode = false)
    {
        var (result, error) = await _logicalRunsService.AddAttemptsAsync(robotKey, logicalRunId, request, developerMode);
        if (!string.IsNullOrEmpty(error))
            return BadRequest(error);

        return Ok(result);
    }

    [HttpGet("{logicalRunId:int}")]
    public async Task<ActionResult<LogicalRunDetailsDto>> Get(
        [FromRoute] string robotKey,
        [FromRoute] int logicalRunId)
    {
        var result = await _logicalRunsService.GetAsync(robotKey, logicalRunId);
        if (result == null)
            return NotFound($"Logical run '{logicalRunId}' not found for robot '{robotKey}'.");

        return Ok(result);
    }

    [HttpGet("{logicalRunId:int}/kpis")]
    public async Task<ActionResult<List<RunKpiMeasurementDto>>> GetAllKpisForLogicalRun(
        [FromRoute] string robotKey,
        [FromRoute] int logicalRunId,
        [FromQuery] bool developerMode = false)
    {
        var result = await _logicalRunsService.GetAllKpisForLogicalRunAsync(robotKey, logicalRunId, developerMode);
        return Ok(result);
    }

    [HttpDelete("{logicalRunId:int}/attempts/{runId}")]
    public async Task<IActionResult> RemoveAttempt(
        [FromRoute] string robotKey,
        [FromRoute] int logicalRunId,
        [FromRoute] string runId,
        [FromQuery] bool developerMode = false)
    {
        var error = await _logicalRunsService.RemoveAttemptAsync(robotKey, logicalRunId, runId, developerMode);
        if (!string.IsNullOrEmpty(error))
            return BadRequest(error);

        return NoContent();
    }

    [HttpDelete("{logicalRunId:int}")]
    public async Task<IActionResult> Delete(
        [FromRoute] string robotKey,
        [FromRoute] int logicalRunId,
        [FromQuery] bool developerMode = false)
    {
        var error = await _logicalRunsService.DeleteAsync(robotKey, logicalRunId, developerMode);
        if (!string.IsNullOrEmpty(error))
            return BadRequest(error);

        return NoContent();
    }
}