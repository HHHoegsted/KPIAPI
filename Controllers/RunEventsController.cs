using Microsoft.AspNetCore.Mvc;
using KPIAPI.Services;
using KPIAPI.DTOs;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/robots/{robotKey}/runs/{runId}/events")]
public class RunEventsController : ControllerBase
{
    private readonly RunEventsService _runEventsService;

    public RunEventsController(RunEventsService runEventsService)
    {
        _runEventsService = runEventsService;
    }

    [HttpPost]
    public async Task<IActionResult> RecordEvent(
        [FromRoute] string robotKey,
        [FromRoute] string runId,
        [FromBody] RecordRunEventRequest request)
    {
        return await _runEventsService.RecordEventAsync(robotKey, runId, request);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromRoute] string robotKey,
        [FromRoute] string runId,
        [FromQuery] bool developerMode = false,
        [FromQuery] bool debugOnly = false)
    {
        return await _runEventsService.ListEventsAsync(robotKey, runId, developerMode, debugOnly);
    }

    [HttpGet("{eventId:int}")]
    public async Task<IActionResult> GetEvent(
        [FromRoute] string robotKey,
        [FromRoute] string runId,
        [FromRoute] int eventId)
    {
        return await _runEventsService.GetEventAsync(robotKey, runId, eventId);
    }
}