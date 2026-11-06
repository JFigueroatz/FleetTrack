import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { ApiError } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import { BrandMark } from "../components/Icon";
import { Notice } from "../components/ui";

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
      void navigate("/dashboard");
    } catch (err) {
      setError(err instanceof ApiError && err.status === 401
        ? "Correo o contrasena incorrectos."
        : "No se pudo iniciar sesion. Intenta de nuevo.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="login">
      <div className="login-art" aria-hidden="true">
        <svg className="route" viewBox="0 0 600 800" preserveAspectRatio="xMidYMid slice" focusable="false">
          <path className="path" d="M-20 720 C 150 620, 70 470, 250 410 S 520 320, 470 170 S 590 70, 650 30" />
          <circle className="stop-halo" cx="250" cy="410" r="9" />
          <circle className="stop" cx="250" cy="410" r="7" />
          <circle className="stop" cx="470" cy="170" r="7" />
          <circle className="stop-halo" cx="470" cy="170" r="9" />
        </svg>
        <div className="brand"><BrandMark />FleetTrack</div>
        <div>
          <p>Rutas, entregas y seguimiento de tu flota en una sola pantalla.</p>
          <small>Para supervisores y conductores.</small>
        </div>
      </div>

      <div className="login-form">
        <form className="login-card" onSubmit={handleSubmit}>
          <div>
            <div className="brand-inline">
              <BrandMark />
              <h1>FleetTrack</h1>
            </div>
            <p className="sub">Inicia sesion para ver y gestionar tu flota.</p>
          </div>

          <div className="field">
            <label htmlFor="email">Correo</label>
            <input id="email" type="email" required autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="password">Contrasena</label>
            <input id="password" type="password" required autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </div>

          {error && <Notice tone="bad" role="alert" fade>{error}</Notice>}

          <button type="submit" disabled={submitting}>
            {submitting ? "Ingresando..." : "Ingresar"}
          </button>
        </form>
      </div>
    </div>
  );
}
