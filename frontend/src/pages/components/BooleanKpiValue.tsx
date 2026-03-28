import type { AggregatedRunKpi } from "./runKpiTypes";

type Props = {
    kpi: AggregatedRunKpi;
};

function fmtPct(count: number, total: number) {
    if (total <= 0) return "0 %";
    const pct = (count / total) * 100;
    return `${new Intl.NumberFormat("da-DK", {
        maximumFractionDigits: 0,
    }).format(pct)} %`;
}

export default function BooleanKpiValue({ kpi }: Props) {
    const yesCount = kpi.trueCount ?? 0;
    const noCount = kpi.falseCount ?? 0;
    const total = yesCount + noCount;

    return (
        <div>
            <div>Ja: {fmtPct(yesCount, total)} ({yesCount})</div>
            <div>Nej: {fmtPct(noCount, total)} ({noCount})</div>
        </div>
    );
}