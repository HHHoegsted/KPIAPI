import { Fragment, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/apiClient";
import type { RobotListItem } from "../api/types";

function fmtLocalDk(isoUtc: string | null) {
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

export default function RobotsPage() {
  const navigate = useNavigate();

  const [hasDataOnly, setHasDataOnly] = useState(true);
  const [rows, setRows] = useState<RobotListItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const grouped = useMemo(() => {
    const norm = (s: string | null | undefined) => (s ?? "").trim().toLowerCase();

    const sorted = [...rows].sort((a, b) => {
      // 1) center code
      const c = norm(a.centerCode).localeCompare(norm(b.centerCode), "da");
      if (c !== 0) return c;

      // 2) display name
      const d = norm(a.displayName).localeCompare(norm(b.displayName), "da");
      if (d !== 0) return d;

      // 3) key (stable tie-breaker)
      return norm(a.key).localeCompare(norm(b.key), "da");
    });

    const groups: Array<{ centerCode: string; items: RobotListItem[] }> = [];
    for (const r of sorted) {
      const center = (r.centerCode ?? "").trim() || "Ukendt";
      const last = groups[groups.length - 1];
      if (!last || last.centerCode !== center) {
        groups.push({ centerCode: center, items: [r] });
      } else {
        last.items.push(r);
      }
    }
    return groups;
  }, [rows]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const data = await api.listRobots(hasDataOnly);
      setRows(data);
    } catch (e: unknown) {
      setError(toErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [hasDataOnly]);

  function goToDashboard(robotKey: string) {
    navigate(`/robots/${encodeURIComponent(robotKey)}`);
  }

  return (
    <div style={{ padding: 16, maxWidth: 1100, margin: "0 auto" }}>
      <h1 style={{ marginBottom: 8 }}>Robotter</h1>

      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          marginBottom: 12,
        }}
      >
        <label htmlFor="hasDataOnly" style={{ display: "flex", gap: 8, alignItems: "center" }}>
          <input
            id="hasDataOnly"
            type="checkbox"
            checked={hasDataOnly}
            onChange={(e) => setHasDataOnly(e.target.checked)}
          />
          Kun med data
        </label>

        <div style={{ display: "flex", gap: 12, alignItems: "center" }}>
          {loading && <span>Indlæser…</span>}
          <button onClick={load} disabled={loading}>
            Opdater
          </button>
        </div>
      </div>

      {error && (
        <div style={{ padding: 12, border: "1px solid #c00", marginBottom: 12 }}>
          <strong>Fejl:</strong> {error}
        </div>
      )}

      <table className="robots-table" cellPadding={8}>
        <thead>
          <tr>
            <th>Robot</th>
            <th style={{ width: 220 }}>Sidst set</th>
          </tr>
        </thead>

        <tbody>
          {grouped.map((g) => (
            <Fragment key={g.centerCode}>
              {/* Center header row */}
              <tr>
                <td
                  colSpan={2}
                  style={{
                    background: "var(--brand-100)",
                    color: "var(--primary)",
                    fontWeight: 800,
                    borderBottom: "1px solid var(--border)",
                    paddingTop: 10,
                    paddingBottom: 10,
                  }}
                >
                  {g.centerCode.toUpperCase()}
                  <span style={{ color: "var(--muted)", fontWeight: 700, marginLeft: 10 }}>
                    ({g.items.length})
                  </span>
                </td>
              </tr>

              {/* Robot rows */}
              {g.items.map((r) => (
                <tr
                  key={r.id}
                  className={`robots-row--link${r.isActive ? "" : " robots-row--inactive"}`}
                  onClick={() => goToDashboard(r.key)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter" || e.key === " ") {
                      e.preventDefault();
                      goToDashboard(r.key);
                    }
                  }}
                  tabIndex={0}
                  role="link"
                  aria-label={`Åbn dashboard for ${r.displayName} (${r.centerCode})`}
                >
                  <td className="robots-col--name">{r.displayName}</td>
                  <td className="robots-col--time">{fmtLocalDk(r.lastSeenUtc)}</td>
                </tr>
              ))}
            </Fragment>
          ))}

          {grouped.length === 0 && !loading && (
            <tr>
              <td colSpan={2} style={{ padding: 12 }}>
                Ingen robotter fundet.
              </td>
            </tr>
          )}
        </tbody>
      </table>


    </div>
  );
}
