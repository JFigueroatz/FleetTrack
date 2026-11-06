#!/usr/bin/env bash
# Consulta el Quality Gate de SonarQube y falla (exit 1) si no es OK. Guarda el detalle en reports/sonar/.
# Requiere SONAR_TOKEN y que el analisis haya terminado (`sonarscanner end`).
set -euo pipefail
cd "$(dirname "$0")/../.."
HOST=${SONAR_HOST_URL:-http://localhost:9000}
AUTH=(-u "${SONAR_TOKEN:?define SONAR_TOKEN}:")
mkdir -p reports/sonar
# El servidor procesa el analisis de forma asincrona: espera a que la tarea termine.
for i in $(seq 1 60); do
  S=$(curl -sf "${AUTH[@]}" "$HOST/api/ce/component?component=fleettrack" | grep -o '"queue":\[[^]]*\]' || true)
  [ "$S" = '"queue":[]' ] && break; sleep 5
done
curl -sf "${AUTH[@]}" "$HOST/api/qualitygates/project_status?projectKey=fleettrack" -o reports/sonar/quality-gate-actual.json
node -e '
const g=require("./reports/sonar/quality-gate-actual.json").projectStatus;
console.log("Quality Gate:",g.status);
for(const c of g.conditions) console.log(" -",c.metricKey,c.status,"valor",c.actualValue,"umbral",c.errorThreshold);
process.exit(g.status==="OK"?0:1)'
