    using Microsoft.AspNetCore.Mvc;
using KPIAPI.Services;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/meta")]
public class MetaController : ControllerBase
{
    private readonly MetaService _metaService;

    public MetaController(MetaService metaService)
    {
        _metaService = metaService;
    }

    [HttpGet("enums/kpi-value-type")]
    public ActionResult GetKpiValueTypeEnum()
    {
        return Ok(_metaService.GetKpiValueTypeEnum());
    }

    [HttpGet("enums/run-outcome")]
    public ActionResult GetRunOutcomeEnum()
    {
        return Ok(_metaService.GetRunOutcomeEnum());
    }
}
