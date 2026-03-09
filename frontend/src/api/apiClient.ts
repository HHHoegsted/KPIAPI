import type {
  EnumResponse,
  KpiDefinition,
  RobotDashboardConfigDto,
  RobotDashboardConfigResponseDto,
  RobotDashboardSummaryDto,
  RobotListItem,
} from "./types";

const API_BASE =  "";

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

  getRobotDashboard: (robotKey: string, fromUtcIso?: string, toUtcIso?: string) => {
    const qs = new URLSearchParams();
    if (fromUtcIso) qs.set("fromUtc", fromUtcIso);
    if (toUtcIso) qs.set("toUtc", toUtcIso);
    const q = qs.toString();

    return request<RobotDashboardSummaryDto>(
      `/api/robots/${encodeURIComponent(robotKey)}/dashboard${q ? `?${q}` : ""}`
    );
  },

  listKpiDefinitions: (robotKey: string, activeOnly: boolean) =>
    request<KpiDefinition[]>(
      `/api/robots/${encodeURIComponent(robotKey)}/kpi-definitions?activeOnly=${activeOnly}`
    ),

  getDashboardConfig: (robotKey: string) =>
    request<RobotDashboardConfigResponseDto>(
      `/api/robots/${encodeURIComponent(robotKey)}/dashboard-config`
    ),

  putDashboardConfig: (robotKey: string, dto: RobotDashboardConfigDto) =>
    request<void>(`/api/robots/${encodeURIComponent(robotKey)}/dashboard-config`, {
      method: "PUT",
      body: JSON.stringify(dto),
    }),

  getKpiValueTypeEnum: () => request<EnumResponse>(`/api/meta/enums/kpi-value-type`),
};
