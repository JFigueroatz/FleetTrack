import { Link, Navigate, Route, Routes } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth/AuthContext";
import { RequireSupervisor } from "./auth/RequireSupervisor";
import { LoginPage } from "./pages/LoginPage";
import { CustomersPage } from "./pages/CustomersPage";
import { VehiclesPage } from "./pages/VehiclesPage";
import { DriversPage } from "./pages/DriversPage";

function Layout({ children }: { children: React.ReactNode }) {
  const { token, fullName, logout } = useAuth();

  return (
    <div>
      <header>
        <strong>FleetTrack</strong>
        {token && (
          <nav>
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
