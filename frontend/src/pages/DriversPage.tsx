import { useEffect, useState, type FormEvent } from "react";
import { apiFetch, ApiError } from "../api/client";
import { initials } from "../components/Icon";
import { Empty, ListSkeleton, matches, Notice, SearchBox, useNotice } from "../components/ui";

interface Driver {
  id: string;
  fullName: string;
  email: string;
}

export function DriversPage() {
  const [drivers, setDrivers] = useState<Driver[] | null>(null);
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [query, setQuery] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useNotice();

  async function load() {
    setDrivers(await apiFetch<Driver[]>("/api/drivers"));
  }

  useEffect(() => {
    load().catch(() => {
      setDrivers([]);
      setError("No se pudieron cargar los conductores.");
    });
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
      setNotice("Conductor agregado.");
      await load();
    } catch (err) {
      setError(err instanceof ApiError && err.status === 409
        ? "Ya existe un conductor con ese correo."
        : "No se pudo crear el conductor.");
    }
  }

  const visible = (drivers ?? []).filter((d) => matches(query, d.fullName, d.email)).reverse();

  return (
    <>
      <div className="page-head">
        <div>
          <h2>Conductores</h2>
          <p>Quienes pueden iniciar sesion en la app movil y recibir rutas.</p>
        </div>
      </div>

      <div className="split">
        <form className="card form" onSubmit={handleSubmit}>
          <h3>Nuevo conductor</h3>

          <div className="field">
            <label htmlFor="driver-name">Nombre completo</label>
            <input id="driver-name" required value={fullName} onChange={(e) => setFullName(e.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="driver-email">Correo</label>
            <input id="driver-email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          </div>

          <div className="field">
            <label htmlFor="driver-password">Contrasena inicial</label>
            <input id="driver-password" type="password" required minLength={8} aria-describedby="driver-password-hint"
              value={password} onChange={(e) => setPassword(e.target.value)} />
            <small id="driver-password-hint" className="hint">Minimo 8 caracteres, con mayuscula, minuscula, numero y simbolo.</small>
          </div>

          {error && <Notice tone="bad" role="alert">{error}</Notice>}
          {notice && <Notice tone="ok" role="status" fade>{notice}</Notice>}

          <button type="submit">Agregar conductor</button>
        </form>

        <section className="card" aria-label="Lista de conductores">
          <div className="card-head">
            <h3>Conductores registrados</h3>
            <span className="chip brand num">{drivers?.length ?? 0}</span>
          </div>
          <SearchBox label="Buscar por nombre o correo" value={query} onChange={setQuery} />

          {drivers === null ? (
            <ListSkeleton />
          ) : visible.length === 0 ? (
            <Empty icon="drivers" title={query ? "Sin resultados." : "Aun no hay conductores."}
              hint={query ? "Prueba con otro nombre o correo." : "Agrega el primero con el formulario."} />
          ) : (
            <ul className="rows">
              {visible.map((d) => (
                <li key={d.id}>
                  <span className="avatar" aria-hidden="true">{initials(d.fullName)}</span>
                  <div className="main">
                    <strong>{d.fullName}</strong>
                    <span>{d.email}</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </>
  );
}
