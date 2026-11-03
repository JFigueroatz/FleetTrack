import { sleep } from "k6";
import { loginDriver, sendTracking } from "./common.js";

// Carga inicial del plan (seccion 9): 100 conductores, una ubicacion cada 5s, durante 10 minutos.
// shortcut: todos los VUs usan el conductor de prueba del seed (un solo driverId) en vez de 100 conductores reales;
// igual ejercita API->Postgres->Redis->RabbitMQ->consumer con el volumen de peticiones real. Si se necesitan
// drivers distintos (p.ej. para separar metricas por conductor), crear 100 conductores de prueba primero via /api/drivers.
// El login se hace una sola vez en setup() para que los thresholds midan solo /api/tracking.
export const options = {
  scenarios: {
    load: {
      executor: "constant-vus",
      vus: Number(__ENV.K6_VUS) || 100,
      duration: __ENV.K6_DURATION || "10m",
    },
  },
  thresholds: {
    http_req_duration: ["p(95)<500"], // p95 de tracking <500ms
    http_req_failed: ["rate<0.01"], // errores <1%
  },
};

export function setup() {
  return { token: loginDriver() };
}

export default function (data) {
  sendTracking(data.token);
  sleep(5);
}
