import { sleep } from "k6";
import { loginDriver, sendTracking } from "./common.js";

// Spike: salto brusco de carga para ver como se comporta el sistema ante un pico repentino (p.ej. toda
// la flota reconectando a la vez tras una caida) y si se recupera limpio al bajar.
export const options = {
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
