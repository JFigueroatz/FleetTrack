import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { RequireSupervisor } from "./auth/RequireSupervisor";
import { BrandMark, Icon, initials, type IconName } from "./components/Icon";
import { LoginPage } from "./pages/LoginPage";
import { CustomersPage } from "./pages/CustomersPage";
import { VehiclesPage } from "./pages/VehiclesPage";
import { DriversPage } from "./pages/DriversPage";
import { AssignRoutePage } from "./pages/AssignRoutePage";
import { DashboardPage } from "./pages/DashboardPage";

const NAV: ReadonlyArray<readonly [string, string, IconName]> = [
  ["/dashboard", "Dashboard", "dashboard"],
  ["/rutas", "Rutas", "route"],
  ["/clientes", "Clientes", "customers"],
  ["/vehiculos", "Vehiculos", "vehicles"],
  ["/conductores", "Conductores", "drivers"],
];

function Layout({ children }: { children: React.ReactNode }) {
  const { token, fullName, roles, logout } = useAuth();

  return (
    <div className={token ? "app" : undefined}>
      <a className="skip-link" href="#main">Saltar al contenido</a>

      {token && (
        <aside className="rail">
          <h1 className="brand"><BrandMark />FleetTrack</h1>

          <nav aria-label="Principal">
            {NAV.map(([to, label, icon]) => (
              <NavLink key={to} to={to}>
                <Icon name={icon} />
                {label}
              </NavLink>
            ))}
          </nav>

          <div className="rail-user">
            <span className="avatar" aria-hidden="true">{initials(fullName ?? "")}</span>
            <div className="who">
              <strong>{fullName}</strong>
              <span>{roles.includes("Supervisor") ? "Supervisor" : roles.join(", ")}</span>
            </div>
            <button type="button" onClick={logout}>
              <Icon name="logout" size={16} />
              Salir
            </button>
          </div>
        </aside>
      )}

      <main id="main" className={token ? "content" : undefined}>{children}</main>
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <Layout>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/dashboard" element={<RequireSupervisor><DashboardPage /></RequireSupervisor>} />
          <Route path="/rutas" element={<RequireSupervisor><AssignRoutePage /></RequireSupervisor>} />
          <Route path="/clientes" element={<RequireSupervisor><CustomersPage /></RequireSupervisor>} />
          <Route path="/vehiculos" element={<RequireSupervisor><VehiclesPage /></RequireSupervisor>} />
          <Route path="/conductores" element={<RequireSupervisor><DriversPage /></RequireSupervisor>} />
          <Route path="*" element={<Navigate to="/login" replace />} />
        </Routes>
      </Layout>
    </AuthProvider>
  );
}

export default App;
