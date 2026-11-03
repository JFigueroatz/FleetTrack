import { useEffect, useState, type FormEvent } from "react";
import { apiFetch, ApiError } from "../api/client";

interface Driver {
  id: string;
  fullName: string;
  email: string;
}

export function DriversPage() {
  const [drivers, setDrivers] = useState<Driver[]>([]);
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);

  async function load() {
    setDrivers(await apiFetch<Driver[]>("/api/drivers"));
  }

  useEffect(() => {
    load().catch(() => setError("No se pudieron cargar los conductores."));
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    try {
      await apiFetch<Driver>("/api/drivers", {
        method: "POST",
        body: JSON.stringify({ fullName, email, password }),
      });
      setFullName("");
      setEmail("");
      setPassword("");
      await load();
    } catch (err) {
      setError(err instanceof ApiError && err.status === 409
        ? "Ya existe un conductor con ese correo."
        : "No se pudo crear el conductor.");
    }
  }

  return (
    <section>
      <h2>Conductores</h2>

      <form onSubmit={handleSubmit}>
        <label htmlFor="driver-name">Nombre completo</label>
        <input id="driver-name" required value={fullName} onChange={(e) => setFullName(e.target.value)} />

        <label htmlFor="driver-email">Correo</label>
        <input id="driver-email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />

        <label htmlFor="driver-password">Contrasena inicial</label>
        <input id="driver-password" type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} />

        {error && <p role="alert">{error}</p>}

        <button type="submit">Agregar conductor</button>
      </form>

      <ul>
        {drivers.map((d) => (
          <li key={d.id}>
            {d.fullName} - {d.email}
          </li>
        ))}
      </ul>
    </section>
  );
}
