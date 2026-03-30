import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api } from "../api/apiClient";
import type { RobotRunsPageSummaryDto, RunListItemDto } from "../api/types";

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

function outcomeClass(outcome: number | null) {
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

function runLabel(outcome: number | null): string {
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

export default function RunsPage() {
    const navigate = useNavigate();
    const { robotKey = "" } = useParams();

    const pageTitle = useMemo(
        () => inferTitleFromRobotKey(robotKey) ?? "Robot-kørsler",
        [robotKey]
    );

    const [summary, setSummary] = useState<RobotRunsPageSummaryDto | null>(null);
    const [rows, setRows] = useState<RunListItemDto[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

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
    }, [robotKey]);

    useEffect(() => {
        function handleDeveloperModeChanged() {
            load();
        }

        window.addEventListener("developer-mode-changed", handleDeveloperModeChanged);

        return () => {
            window.removeEventListener("developer-mode-changed", handleDeveloperModeChanged);
        };
    }, [robotKey]);

    function goToRun(runId: string) {
        navigate(`/robots/${encodeURIComponent(robotKey)}/runs/${encodeURIComponent(runId)}`);
    }

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
                        <div className="card-title">Først behandlet</div>
                        <div>{fmtLocalDateTimeDk(summary.firstEventUtc)}</div>
                    </div>

                    <div className="card">
                        <div className="card-title">Senest behandlet</div>
                        <div>{fmtLocalDateTimeDk(summary.lastEventUtc)}</div>
                    </div>
                </div>
            )}

            <table className="robots-table">
                <thead>
                    <tr>
                        <th>Status</th>
                        <th>Start</th>
                        <th>Slut</th>
                        <th>Antal behandlede</th>
                    </tr>
                </thead>

                <tbody>
                    {rows.map((r) => (
                        <tr
                            key={r.runId}
                            className={`${outcomeClass(r.outcome)} robots-row--link`}
                            onClick={() => goToRun(r.runId)}
                            onKeyDown={(e) => {
                                if (e.key === "Enter" || e.key === " ") {
                                    e.preventDefault();
                                    goToRun(r.runId);
                                }
                            }}
                            tabIndex={0}
                            role="link"
                            aria-label={`Åbn kørsel ${r.runId}`}
                        >
                            <td className="robots-col--name">
                                <div style={{ fontWeight: 700 }}>{runLabel(r.outcome)}</div>
                                <div style={{ color: "var(--muted)", fontSize: "0.92em" }}>
                                    {fmtLocalDateDk(r.startTimeUtc)}
                                </div>
                            </td>
                            <td className="robots-col--time">{fmtLocalTimeDk(r.startTimeUtc)}</td>
                            <td className="robots-col--time">{fmtLocalTimeDk(r.endTimeUtc)}</td>
                            <td>{r.eventCount}</td>
                        </tr>
                    ))}

                    {rows.length === 0 && !loading && (
                        <tr>
                            <td colSpan={4}>Ingen kørsler fundet.</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </>
    );
}