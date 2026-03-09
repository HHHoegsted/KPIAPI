using Microsoft.AspNetCore.Mvc;
using KPIAPI.Domain.Enums;

namespace KPIAPI.Controllers;

[ApiController]
[Route("api/meta")]
public class MetaController : ControllerBase
{
    [HttpGet("enums/kpi-value-type")]
    public ActionResult GetKpiValueTypeEnum()
    {
        var values = Enum.GetValues<KpiValueType>()
            .Select(v => new
            {
                value = (int)v,
                name = v.ToString()
            })
            .OrderBy(x => x.value)
            .ToList();

        return Ok(new
        {
            @enum = nameof(KpiValueType),
            values
        });
    }


    [HttpGet("enums/run-outcome")]
    public ActionResult GetRunOutcomeEnum()
    {
        var values = Enum.GetValues<RunOutcome>()
            .Select(v => new
            {
                value = (int)v,
                name = v.ToString()
            })
            .OrderBy(x => x.value)
            .ToList();

        return Ok(new
        {
            @enum = nameof(RunOutcome),
            values
        });
    }
}
