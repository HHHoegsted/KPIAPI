export type RobotListItem = {
    id: number;
    key: string;
    centerCode: string;
    displayName: string;
    isActive: boolean;
    createdUtc: string;
    lastSeenUtc: string | null;
};

export type KpiDefinition = {
    key: string;
    name: string;
    unit: string | null;
    valueType: number;
    isActive: boolean;
    createdUtc: string;
};

export type KpiRollupDto = {
    key: string;
    name: string;
    unit: string | null;
    valueType: number;
    count: number;
    firstRecordedUtc: string | null;
    lastRecordedUtc: string | null;
    sum: number | null;
    avg: number | null;
    min: number | null;
    max: number | null;
    trueCount: number | null;
    falseCount: number | null;
    topTextValues: Record<string, number> | null;
};

export type RobotDashboardSummaryDto = {
    robotKey: string;
    fromUtc: string | null;
    toUtc: string | null;
    runCount: number;
    eventCount: number;
    measurementCount: number;
    firstEventUtc: string | null;
    lastEventUtc: string | null;
    kpis: KpiRollupDto[];
    coveragePct: number | null;
    totalItems: number | null;
    hitlItems: number | null;
    completedItems: number | null;
};

export type RobotRunsPageSummaryDto = {
    robotKey: string;
    runCount: number;
    eventCount: number;
    firstEventUtc: string | null;
    lastEventUtc: string | null;
};

export type RunOutcome = 1 | 2 | 3 | 4;

export type RunListItemDto = {
    runId: string;
    startTimeUtc: string;
    endTimeUtc: string | null;
    outcome: RunOutcome | null;
    eventCount: number;
    measurementCount: number;
};

export type RobotDashboardConfigDto = {
    totalItemsKpiKey: string | null;
    hitlItemsKpiKey: string | null;
    totalItemsAggregation: number;
    hitlItemsAggregation: number;
    filterKpiKey: string | null;
    filterKpiTextEquals: string | null;
};

export type RobotDashboardConfigResponseDto = {
    robotKey: string;
    config: RobotDashboardConfigDto | null;
};

export type EnumValue = {
    value: number;
    name: string;
};

export type EnumResponse = {
    enum: string;
    values: EnumValue[];
};