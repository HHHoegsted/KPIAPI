import type { AggregatedRunKpi } from "./runKpiTypes";

function fmtNumber(value: number | null) {
    if (value == null) return "—";
    return new Intl.NumberFormat("da-DK", {
        maximumFractionDigits: 2,
    }).format(value);
}

function isTimeSavedKpi(kpiKey: string) {
    return kpiKey.trim().toLowerCase() === "time_saved";
}

function fmtTimeSavedDuration(value: number | null) {
    if (value == null) return "—";

    const totalSeconds = Math.max(0, Math.floor(value));
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    return `${hours}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

type Props = {
    kpi: AggregatedRunKpi;
};

export default function NumericKpiValue({ kpi }: Props) {
    if (isTimeSavedKpi(kpi.kpiKey)) {
        return (
            <div>
                <div>Tid sparet: {fmtTimeSavedDuration(kpi.sum)}</div>
            </div>
        );
    }

    return (
        <div>
            <div>Sum: {fmtNumber(kpi.sum)}</div>
            <div>Gns.: {fmtNumber(kpi.avg)}</div>
        </div>
    );
}