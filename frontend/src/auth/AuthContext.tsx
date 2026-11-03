import { createContext, useContext, useState, type ReactNode } from "react";
import { apiFetch } from "../api/client";

interface LoginResponse {
  token: string;
  fullName: string;
  roles: string[];
}

interface AuthState {
  token: string | null;
  fullName: string | null;
  roles: string[];
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(localStorage.getItem("fleettrack_token"));
  const [fullName, setFullName] = useState<string | null>(localStorage.getItem("fleettrack_name"));
  const [roles, setRoles] = useState<string[]>(JSON.parse(localStorage.getItem("fleettrack_roles") ?? "[]"));

  async function login(email: string, password: string) {
    const response = await apiFetch<LoginResponse>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    });

    localStorage.setItem("fleettrack_token", response.token);
    localStorage.setItem("fleettrack_name", response.fullName);
    localStorage.setItem("fleettrack_roles", JSON.stringify(response.roles));

    setToken(response.token);
    setFullName(response.fullName);
    setRoles(response.roles);
  }

  function logout() {
    localStorage.removeItem("fleettrack_token");
    localStorage.removeItem("fleettrack_name");
    localStorage.removeItem("fleettrack_roles");
    setToken(null);
    setFullName(null);
    setRoles([]);
  }

  return (
    <AuthContext.Provider value={{ token, fullName, roles, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth debe usarse dentro de AuthProvider.");
  }
  return context;
}
