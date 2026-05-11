import type {
    EnumResponse,
    KpiDefinition,
    LogicalRunDetailsDto,
    RobotListItem,
    RobotRunsPageSummaryDto,
    RunKpiMeasurementDto,
    RunListItemDto,
} from "./types";

const API_BASE = "";
const DEV_MODE_STORAGE_KEY = "developerMode";

function getDeveloperMode(): boolean {
    return localStorage.getItem(DEV_MODE_STORAGE_KEY) === "true";
}

function withDeveloperMode(path: string): string {
    const developerMode = getDeveloperMode();
    if (!developerMode) return path;

    const separator = path.includes("?") ? "&" : "?";
    return `${path}${separator}developerMode=true`;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(`${API_BASE}${withDeveloperMode(path)}`, {
        headers: {
            "Content-Type": "application/json",
            ...(init?.headers ?? {}),
        },
        ...init,
    });

    if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(text || `HTTP ${res.status} ${res.statusText}`);
    }

    if (res.status === 204) return undefined as T;

    return (await res.json()) as T;
}

export const api = {
    listRobots: (hasDataOnly: boolean) =>
        request<RobotListItem[]>(`/api/robots?hasDataOnly=${hasDataOnly}`),

    getRobotSummary: (robotKey: string, fromUtcIso?: string, toUtcIso?: string) => {
        const qs = new URLSearchParams();
        if (fromUtcIso) qs.set("fromUtc", fromUtcIso);
        if (toUtcIso) qs.set("toUtc", toUtcIso);
        const q = qs.toString();

        return request<RobotRunsPageSummaryDto>(
            `/api/robots/${encodeURIComponent(robotKey)}/summary${q ? `?${q}` : ""}`
        );
    },

    listRuns: (robotKey: string, limit = 200, sort: "asc" | "desc" = "desc") => {
        const qs = new URLSearchParams();
        qs.set("limit", String(limit));
        qs.set("sort", sort);

        return request<RunListItemDto[]>(
            `/api/robots/${encodeURIComponent(robotKey)}/runs?${qs.toString()}`
        );
    },

    getRunKpis: (robotKey: string, runId: string) =>
        request<RunKpiMeasurementDto[]>(
            `/api/robots/${encodeURIComponent(robotKey)}/runs/${encodeURIComponent(runId)}/kpis`
        ),

    getLogicalRun: (robotKey: string, logicalRunId: string) =>
        request<LogicalRunDetailsDto>(
            `/api/robots/${encodeURIComponent(robotKey)}/logical-runs/${encodeURIComponent(logicalRunId)}`
        ),

    getLogicalRunKpis: (robotKey: string, logicalRunId: string) =>
        request<RunKpiMeasurementDto[]>(
            `/api/robots/${encodeURIComponent(robotKey)}/logical-runs/${encodeURIComponent(logicalRunId)}/kpis`
        ),

    createLogicalRun: (robotKey: string, input: { displayName: string; note?: string | null; runIds: string[] }) =>
        request<LogicalRunDetailsDto>(
            `/api/robots/${encodeURIComponent(robotKey)}/logical-runs`,
            {
                method: "POST",
                body: JSON.stringify(input),
            }
        ),

    addLogicalRunAttempts: (robotKey: string, logicalRunId: string, runIds: string[]) =>
        request<LogicalRunDetailsDto>(
            `/api/robots/${encodeURIComponent(robotKey)}/logical-runs/${encodeURIComponent(logicalRunId)}/attempts`,
            {
                method: "POST",
                body: JSON.stringify({ runIds }),
            }
        ),

    deleteLogicalRun: (robotKey: string, logicalRunId: string) =>
        request<void>(
            `/api/robots/${encodeURIComponent(robotKey)}/logical-runs/${encodeURIComponent(logicalRunId)}`,
            { method: "DELETE" }
        ),

    removeLogicalRunAttempt: (robotKey: string, logicalRunId: string, runId: string) =>
        request<void>(
            `/api/robots/${encodeURIComponent(robotKey)}/logical-runs/${encodeURIComponent(logicalRunId)}/attempts/${encodeURIComponent(runId)}`,
            { method: "DELETE" }
        ),

    deleteRun: (robotKey: string, runId: string) =>
        request<void>(
            `/api/robots/${encodeURIComponent(robotKey)}/runs/${encodeURIComponent(runId)}`,
            { method: "DELETE" }
        ),

    listKpiDefinitions: (robotKey: string, activeOnly: boolean) =>
        request<KpiDefinition[]>(
            `/api/robots/${encodeURIComponent(robotKey)}/kpi-definitions?activeOnly=${activeOnly}`
        ),

    getKpiValueTypeEnum: () => request<EnumResponse>(`/api/meta/enums/kpi-value-type`),
};