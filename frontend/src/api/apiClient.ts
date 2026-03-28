import type {
    EnumResponse,
    KpiDefinition,
    RobotListItem,
    RobotRunsPageSummaryDto,
    RunKpiMeasurementDto,
    RunListItemDto,
} from "./types";

const API_BASE = "";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const res = await fetch(`${API_BASE}${path}`, {
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

    listKpiDefinitions: (robotKey: string, activeOnly: boolean) =>
        request<KpiDefinition[]>(
            `/api/robots/${encodeURIComponent(robotKey)}/kpi-definitions?activeOnly=${activeOnly}`
        ),

    getKpiValueTypeEnum: () => request<EnumResponse>(`/api/meta/enums/kpi-value-type`),
};