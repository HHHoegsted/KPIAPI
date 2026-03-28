import type { AggregatedRunKpi } from "./runKpiTypes";

function fmtNumber(value: number | null) {
    if (value == null) return "—";
    return new Intl.NumberFormat("da-DK", {
        maximumFractionDigits: 2,
    }).format(value);
}

type Props = {
    kpi: AggregatedRunKpi;
};

export default function NumericKpiValue({ kpi }: Props) {
    return (
        <div>
            <div>Sum: {fmtNumber(kpi.sum)}</div>
            <div>Gns.: {fmtNumber(kpi.avg)}</div>
        </div>
    );
}