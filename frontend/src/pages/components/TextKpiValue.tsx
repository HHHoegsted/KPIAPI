import type { AggregatedRunKpi } from "./runKpiTypes";

type Props = {
    kpi: AggregatedRunKpi;
};

export default function TextKpiValue({ kpi }: Props) {
    if (kpi.topTextValues.length === 0) {
        return <span>—</span>;
    }

    return (
        <div>
            {kpi.topTextValues.map((x) => (
                <div key={x.value}>
                    {x.value}: {x.count}
                </div>
            ))}
        </div>
    );
}