export type AggregatedRunKpi = {
    kpiDefinitionId: number;
    kpiKey: string;
    kpiName: string;
    unit: string | null;
    valueType: number;
    count: number;
    sum: number | null;
    avg: number | null;
    trueCount: number | null;
    falseCount: number | null;
    topTextValues: Array<{ value: string; count: number }>;
};