import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api/apiClient";
import type { EnumResponse, RobotDashboardSummaryDto } from "../api/types";

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

function fmtNum(n: number | null | undefined) {
  if (n === null || n === undefined) return "—";
  if (Number.isNaN(n)) return "—";
  return new Intl.NumberFormat().format(n);
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

/**
 * robotKey format: yynnn-ccc-display-name-of-robot
 * Example: 25007-fin-invoice-paybot  ->  "Invoice Paybot (FIN)"
 */
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
      // keep digits as-is, make short alpha tokens uppercase (OCR/RPA/etc.)
      if (/^[a-zA-Z]{1,3}$/.test(w)) return w.toUpperCase();
      // Capitalize first letter, keep rest as-is
      return w.charAt(0).toUpperCase() + w.slice(1);
    });

  const displayName = words.join(" ").trim();
  if (!displayName) return null;

  return `${displayName} (${centerCode})`;
}

export default function RobotDashboardPage() {
  const { robotKey = "" } = useParams();

  const pageTitle = useMemo(
    () => inferTitleFromRobotKey(robotKey) ?? "Robot-dashboard",
    [robotKey]
  );

  const [daysBack, setDaysBack] = useState<number>(7);
  const [data, setData] = useState<RobotDashboardSummaryDto | null>(null);
  const [kpiValueTypeEnum, setKpiValueTypeEnum] = useState<EnumResponse | null>(null);

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { fromIso, toIso } = useMemo(() => {
    const to = new Date();
    const from = new Date(Date.now() - daysBack * 24 * 60 * 60 * 1000);
    return { fromIso: from.toISOString(), toIso: to.toISOString() };
  }, [daysBack]);

  const valueTypeName = useMemo(() => {
    const map = new Map<number, string>();
    (kpiValueTypeEnum?.values ?? []).forEach((v) => map.set(v.value, v.name));
    return (v: number) => map.get(v) ?? String(v);
  }, [kpiValueTypeEnum]);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      const [dash, enumResp] = await Promise.all([
        api.getRobotDashboard(robotKey, fromIso, toIso),
        api.getKpiValueTypeEnum(),
      ]);
      setData(dash);
      setKpiValueTypeEnum(enumResp);
    } catch (e: unknown) {
      setError(toErrorMessage(e));
      setData(null);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [robotKey, fromIso, toIso]);

  return (
    <div style={{ padding: 16, maxWidth: 1100, margin: "0 auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
        <h1 style={{ marginBottom: 8 }}>{pageTitle}</h1>
      </div>

      <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
        <label htmlFor="daysBack" style={{ display: "flex", gap: 8, alignItems: "center" }}>
          Dage tilbage:
          <input
            id="daysBack"
            type="number"
            min={1}
            max={365}
            value={daysBack}
            onChange={(e) => setDaysBack(Number(e.target.value))}
            style={{ width: 90 }}
          />
        </label>

        <button onClick={load} disabled={loading}>
          Opdater
        </button>

        <Link to={`/robots/${encodeURIComponent(robotKey)}/config`} className="btn-link">
          Redigér konfiguration
        </Link>

        {loading && <span>Indlæser…</span>}
      </div>

      {error && (
        <div style={{ padding: 12, border: "1px solid #c00", marginBottom: 12 }}>
          <strong>Fejl:</strong> {error}
        </div>
      )}

      {data && (
        <>
          {/* Uses your index.css classes: summary-grid, card, card-title, kv */}
          <div className="summary-grid" style={{ marginBottom: 16 }}>
            <div className="card">
              <div className="card-title">Periode</div>
              <dl className="kv">
                <dt>Fra</dt>
                <dd>{fmtLocalDk(data.fromUtc)}</dd>
                <dt>Til</dt>
                <dd>{fmtLocalDk(data.toUtc)}</dd>
              </dl>
            </div>

            <div className="card">
              <div className="card-title">Tællinger</div>
              <dl className="kv">
                <dt>Kørsler</dt>
                <dd>{fmtNum(data.runCount)}</dd>
                <dt>Hændelser</dt>
                <dd>{fmtNum(data.eventCount)}</dd>
                <dt>Målinger</dt>
                <dd>{fmtNum(data.measurementCount)}</dd>
              </dl>
            </div>

            <div className="card">
              <div className="card-title">Dækning (hvis konfigureret)</div>
              <dl className="kv">
                <dt>Dæknings%</dt>
                <dd>
                  {data.coveragePct === null ? "—" : `${Number(data.coveragePct).toFixed(2)}%`}
                </dd>
                <dt>Total antal</dt>
                <dd>{fmtNum(data.totalItems)}</dd>
                <dt>HITL-antal</dt>
                <dd>{fmtNum(data.hitlItems)}</dd>
                <dt>Fuldført</dt>
                <dd>{fmtNum(data.completedItems)}</dd>
              </dl>
            </div>
          </div>

          <div style={{ marginBottom: 8, color: "var(--muted)" }}>
            Første hændelse: {fmtLocalDk(data.firstEventUtc)} • Seneste hændelse: {fmtLocalDk(data.lastEventUtc)}
          </div>

          <h2 style={{ marginTop: 20, marginBottom: 8 }}>KPI-opsummering</h2>
          <table width="100%" cellPadding={8} style={{ borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ textAlign: "left", borderBottom: "1px solid var(--border)" }}>
                <th>Nøgle</th>
                <th>Navn</th>
                <th>Type</th>
                <th>Antal</th>
                <th>Senest registreret</th>
                <th>Sum</th>
                <th>Gns.</th>
                <th>Min</th>
                <th>Max</th>
              </tr>
            </thead>
            <tbody>
              {data.kpis.map((k) => (
                <tr key={k.key} style={{ borderBottom: "1px solid var(--border)" }}>
                  <td style={{ fontFamily: "monospace" }}>{k.key}</td>
                  <td>{k.name}</td>
                  <td>{valueTypeName(k.valueType)}</td>
                  <td>{fmtNum(k.count)}</td>
                  <td>{fmtLocalDk(k.lastRecordedUtc)}</td>
                  <td>{fmtNum(k.sum)}</td>
                  <td>{fmtNum(k.avg)}</td>
                  <td>{fmtNum(k.min)}</td>
                  <td>{fmtNum(k.max)}</td>
                </tr>
              ))}

              {data.kpis.length === 0 && (
                <tr>
                  <td colSpan={9} style={{ padding: 12 }}>
                    Ingen KPI&apos;er i denne periode.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </>
      )}
    </div>
  );
}
