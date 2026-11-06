import type { ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { Notice } from "../components/ui";
import { useAuth } from "./AuthContext";

export function RequireSupervisor({ children }: { children: ReactNode }) {
  const { token, roles } = useAuth();

  if (!token) {
    return <Navigate to="/login" replace />;
  }

  if (!roles.includes("Supervisor")) {
    return <Notice tone="bad" role="alert">No tienes permiso de supervisor para ver esta pagina.</Notice>;
  }

  return <>{children}</>;
}
