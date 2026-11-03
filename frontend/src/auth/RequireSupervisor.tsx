import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function RequireSupervisor({ children }: { children: ReactNode }) {
  const { token, roles } = useAuth();

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (!roles.includes("Supervisor")) {
    return <p role="alert">No tienes permiso de supervisor para ver esta pagina.</p>;
  }

  return <>{children}</>;
}
