import { sleep } from "k6";
import { loginDriver, sendTracking } from "./common.js";

// Spike: salto brusco de carga para ver como se comporta el sistema ante un pico repentino (p.ej. toda
// la flota reconectando a la vez tras una caida) y si se recupera limpio al bajar.
// Umbrales declarados antes de ejecutar: p95 < 1000 ms durante el salto y errores < 2 %. Los umbrales son mas
// holgados que los de load porque el salto es brusco; lo que se exige es que el sistema no pierda peticiones.
export const options = {
  thresholds: {
    http_req_duration: ["p(95)<1000"],
    http_req_failed: ["rate<0.02"],
  },
  scenarios: {
    spike: {
      executor: "ramping-vus",
      startVUs: 20,
      stages: __ENV.K6_SHORT
        ? [
            { duration: "10s", target: 20 },
            { duration: "10s", target: 80 },
            { duration: "20s", target: 80 },
            { duration: "10s", target: 20 },
            { duration: "15s", target: 20 },
          ]
        : [
            { duration: "30s", target: 20 }, // base
            { duration: "10s", target: 300 }, // salto brusco
            { duration: "1m", target: 300 }, // sostenido en el pico
            { duration: "10s", target: 20 }, // caida brusca
            { duration: "1m", target: 20 }, // recuperacion
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
