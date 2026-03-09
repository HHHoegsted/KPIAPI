import { Navigate, Route, Routes, Outlet, Link, useLocation } from "react-router-dom";
import RobotsPage from "./pages/RobotsPage";
import RobotDashboardPage from "./pages/RobotDashboardPage";
import DashboardConfigPage from "./pages/DashboardConfigPage";

// Behold din eksisterende logo-opsætning her
const LOGO_FILE = "HK_LOGO_LOGO_BLÅ_150x45px.png";
const logoSrc = new URL(`./assets/logos/${LOGO_FILE}`, import.meta.url).href;

function Layout() {
  const location = useLocation();
  const isOverview = location.pathname === "/";

  return (
    <div style={{ minHeight: "100vh", background: "var(--bg)" }}>
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

      <main style={{ maxWidth: 1100, margin: "0 auto" }}>
        <Outlet />
      </main>
    </div>
  );
}

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<RobotsPage />} />
        <Route path="/robots/:robotKey" element={<RobotDashboardPage />} />
        <Route path="/robots/:robotKey/config" element={<DashboardConfigPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
