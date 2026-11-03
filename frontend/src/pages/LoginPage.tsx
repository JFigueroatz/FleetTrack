import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError } from "../api/client";
import { useAuth } from "../auth/AuthContext";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await login(email, password);
      navigate("/dashboard");
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401
        ? "Correo o contrasena incorrectos."
        : "No se pudo iniciar sesion. Intenta de nuevo.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <main>
      <h1>FleetTrack</h1>
      <form onSubmit={handleSubmit}>
        <label htmlFor="email">Correo</label>
        <input id="email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />

        <label htmlFor="password">Contrasena</label>
        <input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />

        {error && <p role="alert">{error}</p>}

        <button type="submit" disabled={submitting}>
          {submitting ? "Ingresando..." : "Ingresar"}
        </button>
      </form>
    </main>
  );
}
