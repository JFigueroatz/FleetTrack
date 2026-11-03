import { Link, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { RequireSupervisor } from "./auth/RequireSupervisor";
import { LoginPage } from "./pages/LoginPage";
import { CustomersPage } from "./pages/CustomersPage";
import { VehiclesPage } from "./pages/VehiclesPage";
import { DriversPage } from "./pages/DriversPage";
import { AssignRoutePage } from "./pages/AssignRoutePage";
import { DashboardPage } from "./pages/DashboardPage";

function Layout({ children }: { children: React.ReactNode }) {
  const { token, fullName, logout } = useAuth();

  return (
    <div>
      <header>
        <strong>FleetTrack</strong>
        {token && (
          <nav>
            <Link to="/dashboard">Dashboard</Link>
            <Link to="/rutas">Rutas</Link>
            <Link to="/clientes">Clientes</Link>
            <Link to="/vehiculos">Vehiculos</Link>
            <Link to="/conductores">Conductores</Link>
            <span>{fullName}</span>
            <button type="button" onClick={logout}>Salir</button>
          </nav>
        )}
      </header>
      {children}
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
