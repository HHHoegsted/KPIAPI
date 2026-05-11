import type { RunKpiMeasurementDto } from "../../api/types";
import type { AggregatedRunKpi } from "./runKpiTypes";

export function aggregateRunKpis(rows: RunKpiMeasurementDto[]): AggregatedRunKpi[] {
    const groups = new Map<string, RunKpiMeasurementDto[]>();

    for (const row of rows) {
        const key = `${row.kpiDefinitionId}::${row.kpiKey}`;
        const existing = groups.get(key);
        if (existing) {
            existing.push(row);
        } else {
            groups.set(key, [row]);
        }
    }

    const result: AggregatedRunKpi[] = [];

    for (const [, group] of groups) {
        const first = group[0];

        const base: AggregatedRunKpi = {
            kpiDefinitionId: first.kpiDefinitionId,
            kpiKey: first.kpiKey,
            kpiName: first.kpiName,
            unit: first.unit,
            valueType: first.valueType,
            count: group.length,
            sum: null,
            avg: null,
            trueCount: null,
            falseCount: null,
            topTextValues: [],
        };

        switch (first.valueType) {
            case 1: {
                const values = group
                    .map((x) => x.intValue)
                    .filter((x): x is number => x != null);

                if (values.length > 0) {
                    const sum = values.reduce((a, b) => a + b, 0);
                    base.sum = sum;
                    base.avg = sum / values.length;
                }
                break;
            }

            case 2: {
                const values = group
                    .map((x) => x.decimalValue)
                    .filter((x): x is number => x != null);

                if (values.length > 0) {
                    const sum = values.reduce((a, b) => a + b, 0);
                    base.sum = sum;
                    base.avg = sum / values.length;
                }
                break;
            }

            case 4: {
                const values = group
                    .map((x) => x.durationMs)
                    .filter((x): x is number => x != null);

                if (values.length > 0) {
                    const sum = values.reduce((a, b) => a + b, 0);
                    base.sum = sum;
                    base.avg = sum / values.length;
                }
                break;
            }

            case 3: {
                base.trueCount = group.filter((x) => x.boolValue === true).length;
                base.falseCount = group.filter((x) => x.boolValue === false).length;
                break;
            }

            case 5: {
                const counts = new Map<string, number>();

                for (const row of group) {
                    const value = row.textValue?.trim();
                    if (!value) continue;
                    counts.set(value, (counts.get(value) ?? 0) + 1);
                }

                base.topTextValues = [...counts.entries()]
                    .sort((a, b) => b[1] - a[1])
                    .slice(0, 5)
                    .map(([value, count]) => ({ value, count }));

                break;
            }
        }

        result.push(base);
    }

    return result.sort((a, b) => a.kpiName.localeCompare(b.kpiName, "da"));
}