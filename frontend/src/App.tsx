import { Navigate, Route, Routes, Outlet, Link, useLocation } from "react-router-dom";
import { useEffect, useRef, useState } from "react";
import RobotsPage from "./pages/RobotsPage";
import RunsPage from "./pages/RunsPage";
import SingleRunPage from "./pages/SingleRunPage";

const LOGO_FILE = "HK_LOGO_LOGO_BLÅ_150x45px.png";
const logoSrc = new URL(`./assets/logos/${LOGO_FILE}`, import.meta.url).href;

const APP_VERSION = "1.0";
const DEV_MODE_STORAGE_KEY = "developerMode";

function getDeveloperMode(): boolean {
    return localStorage.getItem(DEV_MODE_STORAGE_KEY) === "true";
}

function setDeveloperMode(next: boolean) {
    localStorage.setItem(DEV_MODE_STORAGE_KEY, String(next));
    window.dispatchEvent(
        new CustomEvent("developer-mode-changed", {
            detail: { enabled: next },
        })
    );
}

function Layout() {
    const location = useLocation();
    const isOverview = location.pathname === "/";

    const [developerMode, setDeveloperModeState] = useState<boolean>(() => getDeveloperMode());
    const [hint, setHint] = useState<string | null>(null);

    const clickCountRef = useRef(0);
    const resetTimerRef = useRef<number | null>(null);
    const hintTimerRef = useRef<number | null>(null);

    useEffect(() => {
        function handleDeveloperModeChanged(event: Event) {
            const customEvent = event as CustomEvent<{ enabled?: boolean }>;
            const enabled =
                typeof customEvent.detail?.enabled === "boolean"
                    ? customEvent.detail.enabled
                    : getDeveloperMode();

            setDeveloperModeState(enabled);
        }

        window.addEventListener("developer-mode-changed", handleDeveloperModeChanged as EventListener);

        return () => {
            window.removeEventListener(
                "developer-mode-changed",
                handleDeveloperModeChanged as EventListener
            );

            if (resetTimerRef.current != null) {
                window.clearTimeout(resetTimerRef.current);
            }

            if (hintTimerRef.current != null) {
                window.clearTimeout(hintTimerRef.current);
            }
        };
    }, []);

    function showHint(message: string) {
        setHint(message);

        if (hintTimerRef.current != null) {
            window.clearTimeout(hintTimerRef.current);
        }

        hintTimerRef.current = window.setTimeout(() => {
            setHint(null);
        }, 1800);
    }

    function handleVersionClick() {
        clickCountRef.current += 1;

        if (resetTimerRef.current != null) {
            window.clearTimeout(resetTimerRef.current);
        }

        resetTimerRef.current = window.setTimeout(() => {
            clickCountRef.current = 0;
        }, 2000);

        if (clickCountRef.current >= 5) {
            clickCountRef.current = 0;

            if (resetTimerRef.current != null) {
                window.clearTimeout(resetTimerRef.current);
                resetTimerRef.current = null;
            }

            const next = !developerMode;
            setDeveloperMode(next);
            setDeveloperModeState(next);
            showHint(next ? "Developer mode aktiveret" : "Developer mode deaktiveret");
        }
    }

    return (
        <div style={{ minHeight: "100vh", background: "var(--bg)", display: "flex", flexDirection: "column" }}>
            <header
                style={{
                    position: "sticky",
                    top: 0,
                    zIndex: 50,
                    background: "var(--surface)",
                    borderBottom: "1px solid var(--border)",
                }}
            >
                <div
                    style={{
                        maxWidth: 1100,
                        margin: "0 auto",
                        padding: "10px 16px",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "space-between",
                        gap: 16,
                    }}
                >
                    <div style={{ display: "flex", alignItems: "center", gap: 12, minWidth: 0 }}>
                        <img
                            src={logoSrc}
                            alt="Logo"
                            style={{ height: 34, width: "auto", display: "block" }}
                        />
                        <div style={{ fontWeight: 700, color: "var(--primary)", whiteSpace: "nowrap" }}>
                            KPI-dashboard
                        </div>
                    </div>

                    <nav style={{ display: "flex", gap: 12, alignItems: "center" }}>
                        {!isOverview && (
                            <Link to="/" className="btn-link btn-secondary">
                                Oversigt
                            </Link>
                        )}
                    </nav>
                </div>
            </header>

            <main style={{ maxWidth: 1100, margin: "0 auto", flex: 1, width: "100%" }}>
                <Outlet />
            </main>

            <footer
                style={{
                    maxWidth: 1100,
                    width: "100%",
                    margin: "0 auto",
                    padding: "10px 16px 16px",
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    gap: 12,
                    color: "var(--muted)",
                    fontSize: "0.85rem",
                }}
            >
                <div
                    onClick={handleVersionClick}
                    role="button"
                    tabIndex={0}
                    onKeyDown={(e) => {
                        if (e.key === "Enter" || e.key === " ") {
                            e.preventDefault();
                            handleVersionClick();
                        }
                    }}
                    aria-label="Version"
                    style={{
                        userSelect: "none",
                        cursor: "default",
                        opacity: 0.8,
                    }}
                    title={developerMode ? "Developer mode er aktiv" : undefined}
                >
                    Version {APP_VERSION}
                    {developerMode && <span style={{ marginLeft: 8, fontSize: "0.78rem" }}>DEV</span>}
                </div>

                <div style={{ minHeight: 20, fontSize: "0.82rem" }}>{hint ?? ""}</div>
            </footer>
        </div>
    );
}

export default function App() {
    return (
        <Routes>
            <Route element={<Layout />}>
                <Route path="/" element={<RobotsPage />} />
                <Route path="/robots/:robotKey" element={<RunsPage />} />
                <Route path="/robots/:robotKey/runs/:runId" element={<SingleRunPage />} />
                <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
        </Routes>
    );
}