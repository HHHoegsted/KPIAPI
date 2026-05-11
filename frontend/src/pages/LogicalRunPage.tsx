import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { api } from "../api/apiClient";
import type { LogicalRunDetailsDto, LogicalRunOutcome, RunOutcome } from "../api/types";

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

function logicalOutcomeLabel(outcome: LogicalRunOutcome): string {
    switch (outcome) {
        case 1:
            return "I gang";
        case 2:
            return "Gennemført";
        case 3:
            return "Gennemført efter retry";
        case 4:
            return "Fejlet";
        default:
            return "Ukendt";
    }
}

function physicalOutcomeLabel(outcome: RunOutcome | null): string {
    switch (outcome) {
        case 1:
            return "Gennemført";
        case 2:
            return "Fejlet";
        case 3:
            return "Delvis";
        case 4:
            return "Annulleret";
        default:
            return "I gang";
    }
}

export default function LogicalRunPage() {
    const navigate = useNavigate();
    const { robotKey = "", logicalRunId = "" } = useParams();
    const [isDeveloperMode, setIsDeveloperMode] = useState(
        () => localStorage.getItem("developerMode") === "true"
    );

    const [details, setDetails] = useState<LogicalRunDetailsDto | null>(null);
    const [runIdsInput, setRunIdsInput] = useState("");
    const [loading, setLoading] = useState(false);
    const [mutating, setMutating] = useState(false);
    const [error, setError] = useState<string | null>(null);

    async function load() {
        setLoading(true);
        setError(null);

        try {
            const data = await api.getLogicalRun(robotKey, logicalRunId);
            setDetails(data);
        } catch (e: unknown) {
            setError(toErrorMessage(e));
            setDetails(null);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        load();
    }, [robotKey, logicalRunId]);

    useEffect(() => {
        function handleDeveloperModeChanged() {
            setIsDeveloperMode(localStorage.getItem("developerMode") === "true");
        }

        window.addEventListener("developer-mode-changed", handleDeveloperModeChanged);

        return () => {
            window.removeEventListener("developer-mode-changed", handleDeveloperModeChanged);
        };
    }, []);

    async function handleDeleteLogicalRun() {
        const confirmed = window.confirm(
            `Er du sikker på, at du vil slette denne logiske kørsel?\n\n${details?.displayName ?? logicalRunId}\n\nDe fysiske kørsler slettes ikke.`
        );

        if (!confirmed) return;

        setMutating(true);
        setError(null);

        try {
            await api.deleteLogicalRun(robotKey, logicalRunId);
            navigate(`/robots/${encodeURIComponent(robotKey)}`);
        } catch (e: unknown) {
            setError(toErrorMessage(e));
        } finally {
            setMutating(false);
        }
    }

    async function handleRemoveAttempt(runId: string) {
        const confirmed = window.confirm(`Fjern ${runId} fra den logiske kørsel?`);
        if (!confirmed) return;

        setMutating(true);
        setError(null);

        try {
            await api.removeLogicalRunAttempt(robotKey, logicalRunId, runId);
            await load();
        } catch (e: unknown) {
            setError(toErrorMessage(e));
        } finally {
            setMutating(false);
        }
    }

    async function handleAddAttempts() {
        const runIds = runIdsInput
            .split(/[\n,;]+/)
            .map((value) => value.trim())
            .filter(Boolean);

        if (runIds.length === 0) {
            setError("Angiv mindst ét run id.");
            return;
        }

        setMutating(true);
        setError(null);

        try {
            await api.addLogicalRunAttempts(robotKey, logicalRunId, runIds);
            setRunIdsInput("");
            await load();
        } catch (e: unknown) {
            setError(toErrorMessage(e));
        } finally {
            setMutating(false);
        }
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
                    <h1 style={{ marginBottom: 4 }}>{details?.displayName ?? "Logisk kørsel"}</h1>
                    <div style={{ color: "var(--muted)" }}>{robotKey}</div>
                </div>

                <div style={{ display: "flex", gap: 8 }}>
                    <Link className="btn-link btn-secondary" to={`/robots/${encodeURIComponent(robotKey)}`}>
                        Tilbage
                    </Link>
                    <button onClick={load} disabled={loading || mutating}>
                        Opdater
                    </button>
                    {isDeveloperMode && (
                        <button
                            onClick={handleDeleteLogicalRun}
                            disabled={loading || mutating}
                            style={{ background: "#fff1f0", border: "1px solid #d92d20", color: "#b42318" }}
                        >
                            {mutating ? "Arbejder…" : "Slet logisk kørsel"}
                        </button>
                    )}
                </div>
            </div>

            {loading && <div>Indlæser…</div>}

            {error && (
                <div className="card" style={{ marginBottom: 16 }}>
                    <strong>Fejl:</strong> {error}
                </div>
            )}

            {details && (
                <>
                    <div className="summary-grid" style={{ marginBottom: 16 }}>
                        <div className="card">
                            <div className="card-title">Status</div>
                            <div>{logicalOutcomeLabel(details.outcome)}</div>
                        </div>

                        <div className="card">
                            <div className="card-title">Forsøg</div>
                            <div>{details.attemptCount}</div>
                        </div>

                        <div className="card">
                            <div className="card-title">Behandlede</div>
                            <div>{details.eventCount}</div>
                        </div>

                        <div className="card">
                            <div className="card-title">Målinger</div>
                            <div>{details.measurementCount}</div>
                        </div>

                        <div className="card">
                            <div className="card-title">Første start</div>
                            <div>{fmtLocalDateTimeDk(details.startTimeUtc)}</div>
                        </div>

                        <div className="card">
                            <div className="card-title">Sidste slut</div>
                            <div>{fmtLocalDateTimeDk(details.endTimeUtc)}</div>
                        </div>
                    </div>

                    {details.note && (
                        <div className="card" style={{ marginBottom: 16 }}>
                            <div className="card-title">Note</div>
                            <div>{details.note}</div>
                        </div>
                    )}

                    {isDeveloperMode && (
                        <div className="card" style={{ marginBottom: 16 }}>
                            <div className="card-title">Tilføj fysiske kørsler</div>
                            <div style={{ color: "var(--muted)", marginBottom: 12 }}>
                                Angiv et eller flere run ids, adskilt med komma eller linjeskift.
                            </div>

                            <textarea
                                value={runIdsInput}
                                onChange={(e) => setRunIdsInput(e.target.value)}
                                rows={4}
                                placeholder="runid1&#10;runid2"
                                style={{
                                    width: "100%",
                                    padding: "10px 12px",
                                    border: "1px solid var(--border)",
                                    borderRadius: 6,
                                    resize: "vertical",
                                    marginBottom: 12,
                                }}
                            />

                            <div style={{ display: "flex", gap: 8 }}>
                                <button onClick={handleAddAttempts} disabled={loading || mutating}>
                                    {mutating ? "Arbejder…" : "Tilføj forsøg"}
                                </button>
                                <button
                                    type="button"
                                    className="btn-secondary"
                                    onClick={() => setRunIdsInput("")}
                                    disabled={loading || mutating}
                                >
                                    Ryd
                                </button>
                            </div>
                        </div>
                    )}

                    <table className="robots-table">
                        <thead>
                            <tr>
                                <th>Forsøg</th>
                                <th>Status</th>
                                <th>Start</th>
                                <th>Slut</th>
                                <th>Behandlede</th>
                                <th>Målinger</th>
                                <th>Detaljer</th>
                                {isDeveloperMode && <th>Handling</th>}
                            </tr>
                        </thead>

                        <tbody>
                            {details.attempts.map((attempt) => (
                                <tr key={attempt.runId}>
                                    <td>{attempt.sortOrder}</td>
                                    <td>
                                        <div>{physicalOutcomeLabel(attempt.outcome)}</div>
                                        {attempt.errorMessage && (
                                            <div style={{ color: "var(--muted)", fontSize: "0.92em" }}>
                                                {attempt.errorMessage}
                                            </div>
                                        )}
                                    </td>
                                    <td>{fmtLocalDateTimeDk(attempt.startTimeUtc)}</td>
                                    <td>{fmtLocalDateTimeDk(attempt.endTimeUtc)}</td>
                                    <td>{attempt.eventCount}</td>
                                    <td>{attempt.measurementCount}</td>
                                    <td>
                                        <Link
                                            to={`/robots/${encodeURIComponent(robotKey)}/runs/${encodeURIComponent(attempt.runId)}`}
                                            style={{ color: "var(--link)", textDecoration: "underline" }}
                                        >
                                            Åbn fysisk kørsel
                                        </Link>
                                    </td>
                                    {isDeveloperMode && (
                                        <td>
                                            <button
                                                type="button"
                                                className="btn-secondary"
                                                onClick={() => handleRemoveAttempt(attempt.runId)}
                                                disabled={loading || mutating}
                                            >
                                                Fjern
                                            </button>
                                        </td>
                                    )}
                                </tr>
                            ))}

                            {details.attempts.length === 0 && (
                                <tr>
                                    <td colSpan={isDeveloperMode ? 8 : 7}>Ingen forsøg er tilføjet endnu.</td>
                                </tr>
                            )}
                        </tbody>
                    </table>
                </>
            )}
        </>
    );
}