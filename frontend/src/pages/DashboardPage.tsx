import { useEffect, useRef, useState } from "react";
import { apiFetch } from "../api/client";
import { Icon, initials } from "../components/Icon";
import { Empty, ListSkeleton, Notice } from "../components/ui";

interface LastPosition {
  driverId: string;
  driverName: string;
  latitude: number;
  longitude: number;
  recordedAt: string;
}

const POLL_INTERVAL_MS = 5000;
const LIVE_MS = 60_000;
const RECENT_MS = 10 * 60_000;

type Freshness = "live" | "recent" | "idle";

function freshness(ageMs: number): Freshness {
  return ageMs < LIVE_MS ? "live" : ageMs < RECENT_MS ? "recent" : "idle";
}

const FRESH_LABEL: Record<Freshness, string> = { live: "En vivo", recent: "Reciente", idle: "Inactivo" };

function ago(ageMs: number) {
  const s = Math.max(0, Math.round(ageMs / 1000));
  if (s < 5) return "ahora";
  if (s < 60) return `hace ${s} s`;
  if (s < 3600) return `hace ${Math.floor(s / 60)} min`;
  if (s < 86400) return `hace ${Math.floor(s / 3600)} h`;
  return `hace ${Math.floor(s / 86400)} d`;
}

/** Posiciones relativas entre conductores (sin mapa base: es solo geometria de sus coordenadas). */
function PositionPlot({ positions, now }: { positions: LastPosition[]; now: number }) {
  const W = 340, H = 260, PAD = 34;
  const lats = positions.map((p) => p.latitude);
  const lngs = positions.map((p) => p.longitude);
  const [minLat, maxLat, minLng, maxLng] = [Math.min(...lats), Math.max(...lats), Math.min(...lngs), Math.max(...lngs)];
  const spanLat = maxLat - minLat || 1;
  const spanLng = maxLng - minLng || 1;
  const pos = (p: LastPosition) => ({
    x: maxLng === minLng ? W / 2 : PAD + ((p.longitude - minLng) / spanLng) * (W - PAD * 2),
    y: maxLat === minLat ? H / 2 : H - PAD - ((p.latitude - minLat) / spanLat) * (H - PAD * 2),
  });

  return (
    <svg className="plot" viewBox={`0 0 ${W} ${H}`} role="img" aria-label={`Posiciones relativas de ${positions.length} conductor(es)`}>
      {[1, 2, 3].map((i) => <line key={`h${i}`} className="grid" x1="0" x2={W} y1={(H / 4) * i} y2={(H / 4) * i} />)}
      {[1, 2, 3].map((i) => <line key={`v${i}`} className="grid" y1="0" y2={H} x1={(W / 4) * i} x2={(W / 4) * i} />)}
      {positions.map((p) => {
        const { x, y } = pos(p);
        const f = freshness(now - Date.parse(p.recordedAt));
        const label = p.driverName.split(" ")[0];
        return (
          <g key={p.driverId}>
            {f === "live" && <circle className="halo" cx={x} cy={y} r="7" />}
            <circle className={`pt-${f}`} cx={x} cy={y} r="7" />
            <text x={x} y={y + 22} textAnchor="middle">{label}</text>
          </g>
        );
      })}
    </svg>
  );
}

export function DashboardPage() {
  const [positions, setPositions] = useState<LastPosition[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [degraded, setDegraded] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const [now, setNow] = useState(() => Date.now());
  const [updatedAt, setUpdatedAt] = useState<number | null>(null);
  const [cycle, setCycle] = useState(0);
  const [flash, setFlash] = useState<ReadonlySet<string>>(new Set());
  const seen = useRef<Map<string, string>>(new Map());

  useEffect(() => {
    let cancelled = false;
    let flashTimer: ReturnType<typeof setTimeout> | undefined;

    async function load() {
      try {
        const data = await apiFetch<LastPosition[]>("/api/tracking/latest", {}, (r) => {
          if (!cancelled) setDegraded(r.headers.get("X-Redis-Status") === "unavailable");
        });
        if (cancelled) return;

        const changed = new Set<string>();
        if (seen.current.size > 0) {
          for (const p of data) {
            if (seen.current.get(p.driverId) !== p.recordedAt) changed.add(p.driverId);
          }
        }
        seen.current = new Map(data.map((p) => [p.driverId, p.recordedAt]));
        setFlash(changed);
        clearTimeout(flashTimer);
        flashTimer = setTimeout(() => setFlash(new Set()), 1500);

        setPositions(data);
        setError(null);
        setUpdatedAt(Date.now());
        setCycle((c) => c + 1);
      } catch {
        if (!cancelled) {
          setError("No se pudo cargar el tablero de seguimiento.");
        }
      } finally {
        if (!cancelled) setLoaded(true);
      }
    }

    void load();
    const interval = setInterval(load, POLL_INTERVAL_MS);
    const clock = setInterval(() => setNow(Date.now()), 1000);
    return () => {
      cancelled = true;
      clearInterval(interval);
      clearInterval(clock);
      clearTimeout(flashTimer);
    };
  }, []);

  const counts = { live: 0, recent: 0, idle: 0 };
  for (const p of positions) counts[freshness(now - Date.parse(p.recordedAt))]++;

  return (
    <>
      <div className="page-head">
        <div>
          <h2>Dashboard de seguimiento</h2>
          <p>Ultima posicion conocida por conductor (cache Redis, se actualiza cada {POLL_INTERVAL_MS / 1000}s).</p>
        </div>
        <div className="live-status">
          {error
            ? <span className="chip warn"><Icon name="alert" size={14} />Sin conexion</span>
            : <span className="chip ok"><span className="dot live" />Conectado</span>}
          {updatedAt && <span className="num">Actualizado {ago(now - updatedAt)}</span>}
        </div>
      </div>

      <div className="refresh" aria-hidden="true"><span key={cycle} /></div>

      {degraded && (
        <Notice tone="warn" role="status">
          Modo degradado: la cache Redis no esta disponible, las posiciones se leen de PostgreSQL y pueden tardar mas.
        </Notice>
      )}

      {error && <Notice tone="bad" role="alert">{error}</Notice>}

      {!loaded ? (
        <div className="card"><ListSkeleton /></div>
      ) : positions.length === 0 ? (
        <div className="card">
          <Empty icon="signal" title="Sin conductores con ubicacion reportada todavia."
            hint="Cuando un conductor inicie una entrega en la app movil, su posicion aparecera aqui en tiempo real." />
        </div>
      ) : (
        <div className="dash">
          <section className="card" aria-label="Conductores">
            <div className="summary">
              <span className="chip brand num">{positions.length} {positions.length === 1 ? "conductor" : "conductores"}</span>
              {counts.live > 0 && <span className="chip ok num"><span className="dot live" />{counts.live} en vivo</span>}
              {counts.recent > 0 && <span className="chip warn num">{counts.recent} reciente(s)</span>}
              {counts.idle > 0 && <span className="chip idle num">{counts.idle} inactivo(s)</span>}
            </div>

            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Conductor</th>
                    <th>Estado</th>
                    <th>Latitud</th>
                    <th>Longitud</th>
                    <th>Reportado</th>
                  </tr>
                </thead>
                <tbody>
                  {positions.map((p) => {
                    const age = now - Date.parse(p.recordedAt);
                    const f = freshness(age);
                    return (
                      <tr key={p.driverId} className={flash.has(p.driverId) ? "flash" : undefined}>
                        <td>
                          <div className="who-cell">
                            <span className="avatar" aria-hidden="true">{initials(p.driverName)}</span>
                            {p.driverName}
                          </div>
                        </td>
                        <td>
                          <span className={`chip ${f === "live" ? "ok" : f === "recent" ? "warn" : "idle"}`}>
                            <span className={`dot${f === "live" ? " live" : ""}`} />
                            {FRESH_LABEL[f]}
                          </span>
                        </td>
                        <td>{p.latitude.toFixed(5)}</td>
                        <td>{p.longitude.toFixed(5)}</td>
                        <td>
                          <div className="when">
                            {new Date(p.recordedAt).toLocaleTimeString()}
                            <span>{ago(age)}</span>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </section>

          <section className="card" aria-label="Posiciones relativas">
            <div className="card-head"><h3>Posiciones relativas</h3></div>
            <PositionPlot positions={positions} now={now} />
            <p className="plot-note">Distancias relativas entre conductores, sin mapa base. El anillo marca a quienes reportaron hace menos de un minuto.</p>
          </section>
        </div>
      )}
    </>
  );
}
