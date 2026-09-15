import { formatTimeSavedDuration } from "../../utils/formatTimeSavedDuration";
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

type Props = {
    kpi: AggregatedRunKpi;
};

export default function NumericKpiValue({ kpi }: Props) {
    if (isTimeSavedKpi(kpi.kpiKey)) {
        return (
            <div>
                <div>Tid sparet: {formatTimeSavedDuration(kpi.sum)}</div>
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