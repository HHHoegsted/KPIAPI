import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api/apiClient";
import type { KpiDefinition, RobotDashboardConfigDto } from "../api/types";

const aggOptions: Array<{ value: 0 | 1; label: string }> = [
  { value: 0, label: "Sum (0)" },
  { value: 1, label: "Antal sande (1)" },
];

function normalizeNullable(s: string): string | null {
  const t = s.trim();
  return t.length ? t : null;
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
      if (/^[a-zA-Z]{1,3}$/.test(w)) return w.toUpperCase();
      return w.charAt(0).toUpperCase() + w.slice(1);
    });

  const displayName = words.join(" ").trim();
  if (!displayName) return null;

  return `${displayName} (${centerCode})`;
}

export default function DashboardConfigPage() {
  const { robotKey = "" } = useParams();

  const pageTitle = useMemo(() => {
    const base = inferTitleFromRobotKey(robotKey) ?? "Robot";
    return `${base} — Konfiguration`;
  }, [robotKey]);

  const [defs, setDefs] = useState<KpiDefinition[]>([]);
  const [config, setConfig] = useState<RobotDashboardConfigDto>({
    totalItemsKpiKey: null,
    hitlItemsKpiKey: null,
    totalItemsAggregation: 0,
    hitlItemsAggregation: 0,
    filterKpiKey: null,
    filterKpiTextEquals: null,
  });

  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const kpiOptions = useMemo(() => {
    return defs
      .slice()
      .sort((a, b) => a.key.localeCompare(b.key))
      .map((d) => ({
        value: d.key,
        label: `${d.key} — ${d.name}${d.unit ? ` (${d.unit})` : ""}${
          d.isActive ? "" : " [inaktiv]"
        }`,
      }));
  }, [defs]);

  async function load() {
    setLoading(true);
    setError(null);
    setNotice(null);

    try {
      const [cfgResp, defsResp] = await Promise.all([
        api.getDashboardConfig(robotKey),
        api.listKpiDefinitions(robotKey, false),
      ]);

      setDefs(defsResp);

      if (cfgResp.config) {
        setConfig(cfgResp.config);
      } else {
        setConfig((c) => ({ ...c }));
      }
    } catch (e: unknown) {
      setError(toErrorMessage(e));
    } finally {
      setLoading(false);
    }
  }

  async function save() {
    setSaving(true);
    setError(null);
    setNotice(null);

    try {
      await api.putDashboardConfig(robotKey, config);
      setNotice("Gemt.");
    } catch (e: unknown) {
      setError(toErrorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [robotKey]);

  return (
    <div style={{ padding: 16, maxWidth: 900, margin: "0 auto" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "baseline" }}>
        <h1 style={{ marginBottom: 8 }}>{pageTitle}</h1>
      </div>

      <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
        <button onClick={load} disabled={loading}>
          Genindlæs
        </button>

        <button onClick={save} disabled={saving || loading}>
          {saving ? "Gemmer…" : "Gem"}
        </button>

        <Link to={`/robots/${encodeURIComponent(robotKey)}`} className="btn-link">
          Dashboard
        </Link>
      </div>

      {error && (
        <div style={{ padding: 12, border: "1px solid #c00", marginBottom: 12 }}>
          <strong>Fejl:</strong> {error}
        </div>
      )}

      {notice && (
        <div style={{ padding: 12, border: "1px solid #0a0", marginBottom: 12 }}>
          {notice}
        </div>
      )}

      <div style={{ border: "1px solid #eee", padding: 12 }}>
        <h2 style={{ marginTop: 0 }}>Indstillinger for dækning</h2>

        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
          <div>
            <div style={{ fontWeight: 600, marginBottom: 6 }}>KPI for total antal</div>
            <select
              value={config.totalItemsKpiKey ?? ""}
              onChange={(e) =>
                setConfig((c) => ({ ...c, totalItemsKpiKey: normalizeNullable(e.target.value) }))
              }
              style={{ width: "100%" }}
            >
              <option value="">(ingen)</option>
              {kpiOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>

            <div style={{ fontWeight: 600, marginTop: 10, marginBottom: 6 }}>
              Aggregering for total antal
            </div>
            <select
              value={config.totalItemsAggregation}
              onChange={(e) =>
                setConfig((c) => ({
                  ...c,
                  totalItemsAggregation: Number(e.target.value) as 0 | 1,
                }))
              }
              style={{ width: "100%" }}
            >
              {aggOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>

          <div>
            <div style={{ fontWeight: 600, marginBottom: 6 }}>KPI for HITL-antal</div>
            <select
              value={config.hitlItemsKpiKey ?? ""}
              onChange={(e) =>
                setConfig((c) => ({ ...c, hitlItemsKpiKey: normalizeNullable(e.target.value) }))
              }
              style={{ width: "100%" }}
            >
              <option value="">(ingen)</option>
              {kpiOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>

            <div style={{ fontWeight: 600, marginTop: 10, marginBottom: 6 }}>
              Aggregering for HITL-antal
            </div>
            <select
              value={config.hitlItemsAggregation}
              onChange={(e) =>
                setConfig((c) => ({
                  ...c,
                  hitlItemsAggregation: Number(e.target.value) as 0 | 1,
                }))
              }
              style={{ width: "100%" }}
            >
              {aggOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        <hr style={{ margin: "16px 0" }} />

        <h2 style={{ marginTop: 0 }}>Valgfrit filter</h2>
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 16 }}>
          <div>
            <div style={{ fontWeight: 600, marginBottom: 6 }}>Filter-KPI-nøgle</div>
            <select
              value={config.filterKpiKey ?? ""}
              onChange={(e) =>
                setConfig((c) => ({ ...c, filterKpiKey: normalizeNullable(e.target.value) }))
              }
              style={{ width: "100%" }}
            >
              <option value="">(ingen)</option>
              {kpiOptions.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
            <div style={{ color: "#666", marginTop: 6 }}>
              Hvis sat, tæller dækning kun elementer hvor denne KPI&apos;s <code>TextValue</code> er
              lig med værdien nedenfor.
            </div>
          </div>

          <div>
            <div style={{ fontWeight: 600, marginBottom: 6 }}>Filtertekst er lig med</div>
            <input
              type="text"
              value={config.filterKpiTextEquals ?? ""}
              onChange={(e) =>
                setConfig((c) => ({
                  ...c,
                  filterKpiTextEquals: normalizeNullable(e.target.value),
                }))
              }
              style={{ width: "100%" }}
              placeholder="fx faktura"
            />
          </div>
        </div>
      </div>
    </div>
  );
}
