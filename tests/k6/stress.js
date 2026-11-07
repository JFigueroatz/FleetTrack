import { sleep } from "k6";
import { loginDriver, sendTracking } from "./common.js";

// Stress: sube la carga mas alla de lo normal para encontrar el limite (seccion 9 del plan).
// Rampa de 100 a 300 VUs, sostiene en el pico y baja; el objetivo es observar saturacion/recuperacion,
// no que pase sin errores, asi que los umbrales se declaran pero NO abortan la corrida: el resultado se compara
// contra ellos despues. Umbrales del pico (declarados antes de ejecutar): p95 < 800 ms y errores < 5 %.
export const options = {
  thresholds: {
    http_req_duration: ["p(95)<800"],
    http_req_failed: ["rate<0.05"],
  },
  scenarios: {
    stress: {
      executor: "ramping-vus",
      startVUs: Number(__ENV.K6_START_VUS) || (__ENV.K6_SHORT ? 20 : 100),
      stages: __ENV.K6_SHORT
        ? [
            { duration: "15s", target: 20 },
            { duration: "15s", target: 60 },
            { duration: "20s", target: 60 },
            { duration: "10s", target: 0 },
          ]
        : [
            { duration: "2m", target: 100 }, // calentamiento al nivel de carga normal
            { duration: "3m", target: 300 }, // rampa hasta el pico de stress
            { duration: "5m", target: 300 }, // sostenido en el pico
            { duration: "2m", target: 0 }, // recuperacion
          ],
    },
  },
};

export function setup() {
  return { token: loginDriver() };
}

export default function (data) {
  sendTracking(data.token);
  sleep(5);
}
