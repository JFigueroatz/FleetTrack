import http from "k6/http";
import { check } from "k6";

export const API = __ENV.API_URL || "http://localhost:8080";

/** La API exige eventId como GUID valido; k6 no trae un generador de UUID nativo. */
function uuidv4() {
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (c) => {
    const r = Math.trunc(Math.random() * 16); // NOSONAR: id de prueba de carga, no es un valor de seguridad
    return (c === "x" ? r : (r & 0x3) | 0x8).toString(16);
  });
}

/** Login una vez por VU (usuario virtual) y reutiliza el token durante todo el escenario. */
export function loginDriver() {
  const res = http.post(
    `${API}/api/auth/login`,
    // Usuario de prueba publico del seed (SeedData.cs); se puede sobreescribir con DRIVER_PASSWORD.
    JSON.stringify({ email: "conductor.prueba@fleettrack.test", password: __ENV.DRIVER_PASSWORD || "Conductor123!" }), // NOSONAR
    { headers: { "Content-Type": "application/json" } },
  );
  check(res, { "login 200": (r) => r.status === 200 });
  return JSON.parse(res.body).token;
}

/** Un conductor reporta una ubicacion (lo que mide el plan: 1 ubicacion cada 5s por conductor). */
export function sendTracking(token) {
  const res = http.post(
    `${API}/api/tracking`,
    JSON.stringify({
      latitude: 14.6 + (__VU % 100) * 0.0001,
      longitude: -90.5 + (__VU % 100) * 0.0001,
      eventId: uuidv4(),
    }),
    { headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` } },
  );
  check(res, { "tracking 200": (r) => r.status === 200 });
  return res;
}
