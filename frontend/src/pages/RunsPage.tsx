import {
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ResponsiveContainer,
} from "recharts";
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api } from "../api/apiClient";
import type {
    LogicalRunOutcome,
    RobotRunsPageSummaryDto,
    RunListItemDto,
    RunOutcome,
} from "../api/types";

function fmtLocalDateDk(isoUtc: string | null) {
    if (!isoUtc) return "—";

    const d = new Date(isoUtc);
    if (Number.isNaN(d.getTime())) return isoUtc;

    return new Intl.DateTimeFormat("da-DK", {
        timeZone: "Europe/Copenhagen",
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
    }).format(d);
}

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

function fmtLocalTimeDk(isoUtc: string | null) {
    if (!isoUtc) return "—";

    const d = new Date(isoUtc);
    if (Number.isNaN(d.getTime())) return isoUtc;

    return new Intl.DateTimeFormat("da-DK", {
        timeZone: "Europe/Copenhagen",
        hour: "2-digit",
        minute: "2-digit",
    }).format(d);
}

function fmtTimeSavedDuration(totalSecondsValue: number | null) {
    if (totalSecondsValue == null) return "—";

    const totalSeconds = Math.max(0, Math.floor(totalSecondsValue));
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;

    return `${hours}:${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

function defaultLogicalRunName() {
    const now = new Date();
    const day = String(now.getDate()).padStart(2, "0");
    const month = String(now.getMonth() + 1).padStart(2, "0");
    const year = String(now.getFullYear()).slice(-2);

    return `Samlet kørsel ${day}${month}${year}`;
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

function physicalOutcomeClass(outcome: RunOutcome | null) {
    switch (outcome) {
        case 1:
            return "run-row--succeeded";
        case 2:
            return "run-row--failed";
        case 3:
            return "run-row--partial";
        case 4:
            return "run-row--canceled";
        default:
            return "run-row--running";
    }
}

function physicalRunLabel(outcome: RunOutcome | null): string {
    switch (outcome) {
        case 1:
            return "Kørsel";
        case 2:
            return "Fejlet kørsel";
        case 3:
            return "Delvist gennemført";
        case 4:
            return "Annulleret kørsel";
        default:
            return "I gang siden";
    }
}

function logicalOutcomeClass(outcome: LogicalRunOutcome | null) {
    switch (outcome) {
        case 2:
        case 3:
            return "run-row--succeeded";
        case 4:
            return "run-row--failed";
        case 1:
            return "run-row--running";
        default:
            return "run-row--partial";
    }
}

function logicalRunLabel(outcome: LogicalRunOutcome | null): string {
    switch (outcome) {
        case 1:
            return "Logisk kørsel i gang";
        case 2:
            return "Logisk kørsel gennemført";
        case 3:
            return "Gennemført efter retry";
        case 4:
            return "Logisk kørsel fejlet";
        default:
            return "Logisk kørsel";
    }
}

function rowClass(row: RunListItemDto) {
    return row.kind === 2
        ? logicalOutcomeClass(row.logicalOutcome)
        : physicalOutcomeClass(row.physicalOutcome);
}

function rowLabel(row: RunListItemDto): string {
    return row.kind === 2
        ? logicalRunLabel(row.logicalOutcome)
        : physicalRunLabel(row.physicalOutcome);
}

function rowKey(row: RunListItemDto): string {
    return row.kind === 2
        ? `logical-${row.logicalRunId}`
        : `physical-${row.runId}`;
}

export default function RunsPage() {
    const navigate = useNavigate();
    const { robotKey = "" } = useParams();
    const [isDeveloperMode, setIsDeveloperMode] = useState(
        () => localStorage.getItem("developerMode") === "true"
    );

    const pageTitle = useMemo(
        () => inferTitleFromRobotKey(robotKey) ?? "Robot-kørsler",
        [robotKey]
    );

    const [summary, setSummary] = useState<RobotRunsPageSummaryDto | null>(null);
    const [rows, setRows] = useState<RunListItemDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [creatingLogicalRun, setCreatingLogicalRun] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedRunIds, setSelectedRunIds] = useState<string[]>([]);
    const [logicalRunName, setLogicalRunName] = useState(() => defaultLogicalRunName());
    const [logicalRunNote, setLogicalRunNote] = useState("");

    async function load() {
        setLoading(true);
        setError(null);

        try {
            const [summaryData, runData] = await Promise.all([
                api.getRobotSummary(robotKey),
                api.listRuns(robotKey, 200, "desc"),
            ]);

            setSummary(summaryData);
            setRows(runData);
        } catch (e: unknown) {
            setError(toErrorMessage(e));
            setSummary(null);
            setRows([]);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        load();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [robotKey]);

    useEffect(() => {
        function handleDeveloperModeChanged() {
            const next = localStorage.getItem("developerMode") === "true";
            setIsDeveloperMode(next);

            if (!next) {
                setSelectedRunIds([]);
                setLogicalRunName(defaultLogicalRunName());
                setLogicalRunNote("");
            }

            load();
        }

        window.addEventListener("developer-mode-changed", handleDeveloperModeChanged);

        return () => {
            window.removeEventListener("developer-mode-changed", handleDeveloperModeChanged);
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [robotKey]);

    useEffect(() => {
        const availableRunIds = new Set(
            rows
                .filter((row) => row.kind === 1 && row.runId)
                .map((row) => row.runId as string)
        );

        setSelectedRunIds((current) => current.filter((runId) => availableRunIds.has(runId)));
    }, [rows]);

    function goToRow(row: RunListItemDto) {
        if (row.kind === 2 && row.logicalRunId != null) {
            navigate(
                `/robots/${encodeURIComponent(robotKey)}/logical-runs/${encodeURIComponent(String(row.logicalRunId))}`
            );
            return;
        }

        if (row.runId) {
            navigate(`/robots/${encodeURIComponent(robotKey)}/runs/${encodeURIComponent(row.runId)}`);
        }
    }

    function toggleRunSelection(runId: string) {
        setSelectedRunIds((current) =>
            current.includes(runId)
                ? current.filter((value) => value !== runId)
                : [...current, runId]
        );
    }

    async function handleCreateLogicalRun() {
        if (selectedRunIds.length === 0) {
            setError("Vælg mindst én fysisk kørsel først.");
            return;
        }

        if (!logicalRunName.trim()) {
            setError("Angiv et navn til den logiske kørsel.");
            return;
        }

        setCreatingLogicalRun(true);
        setError(null);

        try {
            const created = await api.createLogicalRun(robotKey, {
                displayName: logicalRunName.trim(),
                note: logicalRunNote.trim() || null,
                runIds: selectedRunIds,
            });

            setSelectedRunIds([]);
            setLogicalRunName(defaultLogicalRunName());
            setLogicalRunNote("");

            navigate(
                `/robots/${encodeURIComponent(robotKey)}/logical-runs/${encodeURIComponent(String(created.logicalRunId))}`
            );
        } catch (e: unknown) {
            setError(toErrorMessage(e));
        } finally {
            setCreatingLogicalRun(false);
        }
    }

    // Prepare chart data from rows
    const chartData = useMemo(
        () =>
            rows
                .map((r) => ({
                    date: r.startTimeUtc,
                    antalBehandlede: r.eventCount,
                }))
                .reverse(), // oldest to newest
        [rows]
    );

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
                </div>

                <div style={{ display: "flex", gap: 8 }}>
                    <Link className="btn-link btn-secondary" to="/">
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

            {summary && (
                <div className="summary-grid" style={{ marginBottom: 16 }}>
                    <div className="card">
                        <div className="card-title">Kørsler</div>
                        <div>{summary.runCount}</div>
                    </div>

                    <div className="card">
                        <div className="card-title">Total antal behandlede</div>
                        <div>{summary.eventCount}</div>
                    </div>

                    <div className="card">
                        <div className="card-title">Total tid sparet</div>
                        <div>{fmtTimeSavedDuration(summary.totalTimeSavedSeconds)}</div>
                    </div>

                    <div className="card">
                        <div className="card-title">Senest set</div>
                        <div>{fmtLocalDateTimeDk(summary.lastEventUtc)}</div>
                    </div>
                </div>
            )}

            {isDeveloperMode && selectedRunIds.length > 0 && (
                <div className="card" style={{ marginBottom: 16 }}>
                    <div className="card-title">Opret logisk kørsel</div>
                    <div style={{ color: "var(--muted)", marginBottom: 12 }}>
                        {selectedRunIds.length} fysisk{selectedRunIds.length === 1 ? "" : "e"} kørsel
                        {selectedRunIds.length === 1 ? " er " : "er "}valgt.
                    </div>

                    <div style={{ display: "grid", gap: 12 }}>
                        <label style={{ display: "grid", gap: 6 }}>
                            <span>Navn</span>
                            <input
                                value={logicalRunName}
                                onChange={(e) => setLogicalRunName(e.target.value)}
                                placeholder="Fx Fakturakørsel 2026-05-11"
                                style={{ padding: "10px 12px", border: "1px solid var(--border)", borderRadius: 6 }}
                            />
                        </label>

                        <label style={{ display: "grid", gap: 6 }}>
                            <span>Note</span>
                            <textarea
                                value={logicalRunNote}
                                onChange={(e) => setLogicalRunNote(e.target.value)}
                                rows={3}
                                placeholder="Valgfri note"
                                style={{ padding: "10px 12px", border: "1px solid var(--border)", borderRadius: 6, resize: "vertical" }}
                            />
                        </label>

                        <div style={{ color: "var(--muted)", fontSize: "0.92em" }}>
                            {selectedRunIds.join(", ")}
                        </div>

                        <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                            <button onClick={handleCreateLogicalRun} disabled={creatingLogicalRun || loading}>
                                {creatingLogicalRun ? "Opretter…" : "Opret logisk kørsel"}
                            </button>
                            <button
                                type="button"
                                className="btn-secondary"
                                onClick={() => {
                                    setSelectedRunIds([]);
                                    setLogicalRunName(defaultLogicalRunName());
                                    setLogicalRunNote("");
                                }}
                                disabled={creatingLogicalRun}
                            >
                                Ryd valg
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {chartData.length > 10 && (
                <div style={{ width: "100%", height: 260, marginBottom: 24 }}>
                    <ResponsiveContainer width="100%" height="100%">
                        <LineChart data={chartData} margin={{ top: 16, right: 24, left: 0, bottom: 8 }}>
                            <CartesianGrid strokeDasharray="3 3" />
                            <XAxis
                                dataKey="date"
                                tickFormatter={(date) => fmtLocalDateDk(date)}
                                minTickGap={24}
                            />
                            <YAxis allowDecimals={false} />
                            <Tooltip
                                labelFormatter={(date) => fmtLocalDateTimeDk(date)}
                                formatter={(value) => [value ?? 0, "Antal behandlede"]}
                            />
                            <Line
                                type="monotone"
                                dataKey="antalBehandlede"
                                stroke="#1976d2"
                                strokeWidth={2}
                                dot={{ r: 3 }}
                                activeDot={{ r: 5 }}
                            />
                        </LineChart>
                    </ResponsiveContainer>
                </div>
            )}

            <table className="robots-table">
                <thead>
                    <tr>
                        {isDeveloperMode && <th style={{ width: 52 }}>Vælg</th>}
                        <th>Kørsel</th>
                        <th>Start</th>
                        <th>Slut</th>
                        <th>Forsøg</th>
                        <th>Antal behandlede</th>
                    </tr>
                </thead>

                <tbody>
                    {rows.map((r) => (
                        <tr
                            key={rowKey(r)}
                            className={`${rowClass(r)} robots-row--link`}
                            onClick={() => goToRow(r)}
                            onKeyDown={(e) => {
                                if (e.key === "Enter" || e.key === " ") {
                                    e.preventDefault();
                                    goToRow(r);
                                }
                            }}
                            tabIndex={0}
                            role="link"
                            aria-label={
                                r.kind === 2
                                    ? `Åbn logisk kørsel ${r.displayName ?? r.logicalRunId}`
                                    : `Åbn kørsel ${r.runId}`
                            }
                        >
                            {isDeveloperMode && (
                                <td onClick={(e) => e.stopPropagation()}>
                                    {r.kind === 1 && r.runId ? (
                                        <input
                                            type="checkbox"
                                            checked={selectedRunIds.includes(r.runId)}
                                            onChange={() => toggleRunSelection(r.runId as string)}
                                            aria-label={`Vælg kørsel ${r.runId}`}
                                        />
                                    ) : null}
                                </td>
                            )}
                            <td className="robots-col--name">
                                <div style={{ fontWeight: 700 }}>
                                    {r.kind === 2 ? r.displayName ?? rowLabel(r) : rowLabel(r)}
                                </div>
                                <div style={{ color: "var(--muted)", fontSize: "0.92em" }}>
                                    {r.kind === 2
                                        ? rowLabel(r)
                                        : fmtLocalDateDk(r.startTimeUtc)}
                                </div>
                            </td>
                            <td className="robots-col--time">{fmtLocalTimeDk(r.startTimeUtc)}</td>
                            <td className="robots-col--time">{fmtLocalTimeDk(r.endTimeUtc)}</td>
                            <td>{r.attemptCount}</td>
                            <td>{r.eventCount}</td>
                        </tr>
                    ))}

                    {rows.length === 0 && !loading && (
                        <tr>
                            <td colSpan={isDeveloperMode ? 6 : 5}>Ingen kørsler fundet.</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </>
    );
}