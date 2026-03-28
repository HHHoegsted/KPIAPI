import type { AggregatedRunKpi } from "./runKpiTypes";
import BooleanKpiValue from "./BooleanKpiValue";
import NumericKpiValue from "./NumericKpiValue";
import TextKpiValue from "./TextKpiValue";

type Props = {
    kpi: AggregatedRunKpi;
};

export default function KpiValueCell({ kpi }: Props) {
    switch (kpi.valueType) {
        case 1:
        case 2:
        case 4:
            return <NumericKpiValue kpi={kpi} />;
        case 3:
            return <BooleanKpiValue kpi={kpi} />;
        case 5:
            return <TextKpiValue kpi={kpi} />;
        default:
            return <span>—</span>;
    }
}