import { Link, useParams } from "react-router-dom";
import { useEffect, useMemo, useState } from "react";
import { api } from "../api/apiClient";
import type { RunKpiMeasurementDto } from "../api/types";
import KpiValueCell from "./components/KpiValueCell";
import type { AggregatedRunKpi } from "./components/runKpiTypes";

function fmtLocalDateTimeDk(isoUtc: string | null) {
    if (!isoUtc) return "—";

    const d = new Date(isoUtc);
    if (Number.isNaN(d.getTime())) return isoUtc;

    return new Intl.DateTimeFormat("da-DK", {
        timeZone: "Europe/Copenhagen",
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: "2-digit",
        minute: "2-digit",
    }).format(d);
}

function toErrorMessage(e: unknown): string {
    if (e instanceof Error) return e.message;
    if (typeof e === "string") return e;

    try {
        return JSON.stringify(e);
    } catch {
        return "Ukendt fejl";
    }
}

function inferTitleFromRobotKey(robotKey: string): string | null {
    const parts = robotKey.split("-").filter((p) => p.length > 0);
    if (parts.length < 3) return null;

    const centerCode = parts[1].toUpperCase();
    const rawNameParts = parts.slice(2);

    const words = rawNameParts
        .join(" ")
        .split(/[\s-_]+/)
        .filter(Boolean)
        .map((w) => {
            if (/^[a-zA-Z]{1,3}$/.test(w)) return w.toUpperCase();
            return w.charAt(0).toUpperCase() + w.slice(1);
        });

    const displayName = words.join(" ").trim();
    if (!displayName) return null;

    return `${displayName} (${centerCode})`;
}

function aggregateRunKpis(rows: RunKpiMeasurementDto[]): AggregatedRunKpi[] {
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

export default function SingleRunPage() {
    const { robotKey = "", runId = "" } = useParams();

    const pageTitle = useMemo(() => {
        const robotTitle = inferTitleFromRobotKey(robotKey) ?? robotKey;
        return `${robotTitle} · Kørsel`;
    }, [robotKey]);

    const [rows, setRows] = useState<RunKpiMeasurementDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function load() {
        setLoading(true);
        setError(null);

        try {
            const data = await api.getRunKpis(robotKey, runId);
            setRows(data);
        } catch (e: unknown) {
            setError(toErrorMessage(e));
            setRows([]);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        load();
    }, [robotKey, runId]);

    const aggregates = useMemo(() => aggregateRunKpis(rows), [rows]);

    const eventCount = useMemo(() => {
        return new Set(rows.map((row) => row.eventId)).size;
    }, [rows]);

    const measurementCount = rows.length;

    const firstEventUtc = useMemo(() => {
        if (rows.length === 0) return null;
        return rows.reduce(
            (min, row) => (row.eventCreatedUtc < min ? row.eventCreatedUtc : min),
            rows[0].eventCreatedUtc
        );
    }, [rows]);

    const lastEventUtc = useMemo(() => {
        if (rows.length === 0) return null;
        return rows.reduce(
            (max, row) => (row.eventCreatedUtc > max ? row.eventCreatedUtc : max),
            rows[0].eventCreatedUtc
        );
    }, [rows]);

    return (
        <>
            <div
                style={{
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "space-between",
                    gap: 12,
                    marginBottom: 16,
                }}
            >
                <div>
                    <h1 style={{ marginBottom: 4 }}>{pageTitle}</h1>
                    <div style={{ color: "var(--muted)" }}>{robotKey}</div>
                    <div style={{ color: "var(--muted)" }}>{runId}</div>
                </div>

                <div style={{ display: "flex", gap: 8 }}>
                    <Link className="btn-link btn-secondary" to={`/robots/${encodeURIComponent(robotKey)}`}>
                        Tilbage
                    </Link>
                    <button onClick={load} disabled={loading}>
                        Opdater
                    </button>
                </div>
            </div>

            {loading && <div>Indlæser…</div>}

            {error && (
                <div className="card" style={{ marginBottom: 16 }}>
                    <strong>Fejl:</strong> {error}
                </div>
            )}

            <div className="summary-grid" style={{ marginBottom: 16 }}>
                <div className="card">
                    <div className="card-title">Behandlede</div>
                    <div>{eventCount}</div>
                </div>

                <div className="card">
                    <div className="card-title">Først behandlet</div>
                    <div>{fmtLocalDateTimeDk(firstEventUtc)}</div>
                </div>

                <div className="card">
                    <div className="card-title">Senest behandlet</div>
                    <div>{fmtLocalDateTimeDk(lastEventUtc)}</div>
                </div>

                <div className="card">
                    <div className="card-title">Målinger</div>
                    <div>{measurementCount}</div>
                </div>
            </div>

            <table className="robots-table">
                <thead>
                    <tr>
                        <th>KPI</th>
                        <th>Værdi</th>
                    </tr>
                </thead>

                <tbody>
                    {aggregates.map((kpi) => (
                        <tr key={kpi.kpiDefinitionId}>
                            <td className="robots-col--name" style={{ verticalAlign: "top" }}>
                                <div style={{ fontWeight: 700 }}>{kpi.kpiName}</div>
                                {kpi.unit && (
                                    <div style={{ color: "var(--muted)", fontSize: "0.92em" }}>
                                        {kpi.unit}
                                    </div>
                                )}
                            </td>


                            <td style={{ maxWidth: 320 }}>
                                <KpiValueCell kpi={kpi} />
                            </td>
                        </tr>
                    ))}

                    {aggregates.length === 0 && !loading && (
                        <tr>
                            <td colSpan={2}>Ingen KPI-målinger fundet for denne kørsel.</td>
                        </tr>
                    )}
                </tbody>
            </table>

            <div style={{ marginTop: 12, color: "var(--muted)", fontSize: "0.92em" }}>
                {aggregates.length} KPI-typer fordelt på {measurementCount} målinger og {eventCount} behandlede.
            </div>
        </>
    );
}