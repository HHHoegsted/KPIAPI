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

export type RobotRunsPageSummaryDto = {
    robotKey: string;
    runCount: number;
    eventCount: number;
    firstEventUtc: string | null;
    lastEventUtc: string | null;
};

export type RunOutcome = 1 | 2 | 3 | 4;

export type LogicalRunOutcome = 0 | 1 | 2 | 3 | 4;

export type ReportingRunKind = 1 | 2;

export type RunListItemDto = {
    kind: ReportingRunKind;
    runId: string | null;
    logicalRunId: number | null;
    displayName: string | null;
    startTimeUtc: string;
    endTimeUtc: string | null;
    physicalOutcome: RunOutcome | null;
    logicalOutcome: LogicalRunOutcome | null;
    attemptCount: number;
    eventCount: number;
    measurementCount: number;
};

export type LogicalRunAttemptDto = {
    sortOrder: number;
    runId: string;
    startTimeUtc: string;
    endTimeUtc: string | null;
    lastHeartbeatUtc: string | null;
    outcome: RunOutcome | null;
    errorCode: string | null;
    errorMessage: string | null;
    eventCount: number;
    measurementCount: number;
};

export type LogicalRunDetailsDto = {
    logicalRunId: number;
    robotKey: string;
    displayName: string;
    note: string | null;
    createdUtc: string;
    startTimeUtc: string | null;
    endTimeUtc: string | null;
    outcome: LogicalRunOutcome;
    attemptCount: number;
    eventCount: number;
    measurementCount: number;
    attempts: LogicalRunAttemptDto[];
};

export type RunKpiMeasurementDto = {
    eventId: number;
    eventCreatedUtc: string;
    eventMessage: string | null;

    kpiDefinitionId: number;
    kpiKey: string;
    kpiName: string;
    unit: string | null;
    valueType: number;

    intValue: number | null;
    decimalValue: number | null;
    boolValue: boolean | null;
    durationMs: number | null;
    textValue: string | null;
};

export type EnumValue = {
    value: number;
    name: string;
};

export type EnumResponse = {
    enum: string;
    values: EnumValue[];
};