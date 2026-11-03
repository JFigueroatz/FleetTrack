import { useEffect, useState } from "react";
import { apiFetch } from "../api/client";

interface LastPosition {
  driverId: string;
  driverName: string;
  latitude: number;
  longitude: number;
  recordedAt: string;
}

const POLL_INTERVAL_MS = 5000;

export function DashboardPage() {
  const [positions, setPositions] = useState<LastPosition[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [degraded, setDegraded] = useState(false);

  useEffect(() => {
    let cancelled = false;

    async function load() {
      try {
        const data = await apiFetch<LastPosition[]>("/api/tracking/latest", {}, (r) => {
          if (!cancelled) setDegraded(r.headers.get("X-Redis-Status") === "unavailable");
        });
        if (!cancelled) {
          setPositions(data);
          setError(null);
        }
      } catch {
        if (!cancelled) {
          setError("No se pudo cargar el tablero de seguimiento.");
        }
      }
    }

    load();
    const interval = setInterval(load, POLL_INTERVAL_MS);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  return (
    <section>
      <h2>Dashboard de seguimiento</h2>
      <p>Ultima posicion conocida por conductor (cache Redis, se actualiza cada {POLL_INTERVAL_MS / 1000}s).</p>

      {degraded && (
        <p role="status">
          Modo degradado: la cache Redis no esta disponible, las posiciones se leen de PostgreSQL y pueden tardar mas.
        </p>
      )}

      {error && <p role="alert">{error}</p>}

      {positions.length === 0 ? (
        <p>Sin conductores con ubicacion reportada todavia.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Conductor</th>
              <th>Latitud</th>
              <th>Longitud</th>
              <th>Reportado</th>
            </tr>
          </thead>
          <tbody>
            {positions.map((p) => (
              <tr key={p.driverId}>
                <td>{p.driverName}</td>
                <td>{p.latitude.toFixed(5)}</td>
                <td>{p.longitude.toFixed(5)}</td>
                <td>{new Date(p.recordedAt).toLocaleTimeString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
