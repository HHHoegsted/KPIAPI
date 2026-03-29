using KPIAPI.Domain.Enums;

namespace KPIAPI.Services
{
    public class MetaService
    {
        public object GetKpiValueTypeEnum()
        {
            var values = Enum.GetValues<KpiValueType>()
                .Cast<KpiValueType>()
                .Select(v => new
                {
                    value = (int)v,
                    name = v.ToString()
                })
                .OrderBy(x => x.value)
                .ToList();

            return new
            {
                @enum = nameof(KpiValueType),
                values
            };
        }

        public object GetRunOutcomeEnum()
        {
            var values = Enum.GetValues<RunOutcome>()
                .Cast<RunOutcome>()
                .Select(v => new
                {
                    value = (int)v,
                    name = v.ToString()
                })
                .OrderBy(x => x.value)
                .ToList();

            return new
            {
                @enum = nameof(RunOutcome),
                values
            };
        }
    }
}
