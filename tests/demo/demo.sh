#!/usr/bin/env bash
# Atajos para la demostracion en vivo. Uso: bash tests/demo/demo.sh <comando>
#   prepare      levanta el stack y SonarQube, crea una ruta para el conductor de prueba
#   abrir        abre en el navegador dashboard, RabbitMQ, SonarQube y Jenkins
#   estado       resumen: servicios, colas, outbox, eventos procesados, posiciones, Redis
#   tracking     envia una ubicacion del conductor (aparece en el dashboard en <= 5 s)
#   evento       crea una ruta nueva e inicia su entrega (genera eventos RouteStarted y DeliveryStarted)
#   redis-off | redis-on          detiene / arranca Redis
#   consumer-off | consumer-on    detiene / arranca el consumidor RabbitMQ
#   broker-off | broker-on        detiene / arranca RabbitMQ
#   falla        rompe una prueba a proposito, la ejecuta y restaura el archivo
#   k6           resumen de los reportes de k6 a escala completa
# Requiere: Git Bash en la raiz del repo, Docker Desktop y .env (no contiene secretos propios).
set -uo pipefail
cd "$(dirname "$0")/../.."
[ -f .env ] && { set -a; . ./.env; set +a; }
source tests/recovery/common.sh

# En tiempo real con rabbitmqctl (la API de gestion de RabbitMQ actualiza sus contadores cada ~5 s).
queue_msgs() { docker compose exec -T rabbitmq rabbitmqctl list_queues name messages 2>/dev/null | tr -d "" | awk -v q="$1" '$1==q {print $2}'; }
open_url() { cmd.exe /c start "" "$1" >/dev/null 2>&1; }
first_delivery() { grep -o '"deliveries":\[{"id":"[^"]*"' | cut -d'"' -f6; }

case "${1:-}" in
  prepare)
    docker compose up -d --wait
    docker start sonarqube >/dev/null 2>&1 && echo "SonarQube arrancando (tarda ~2 min en responder)"
    D=$(new_route 1 | first_delivery)
    echo "Ruta creada para el conductor de prueba (entrega ${D:-?})."
    echo "Dashboard http://localhost:3000 | RabbitMQ http://localhost:15672 | SonarQube http://localhost:9000 | Jenkins http://localhost:8081"
    ;;
  abrir)
    for u in http://localhost:3000 http://localhost:15672 http://localhost:9000/dashboard?id=fleettrack http://localhost:8081/job/fleettrack; do open_url "$u"; sleep 1; done
    ;;
  estado)
    echo "== Servicios"; docker compose ps --format 'table {{.Service}}\t{{.Status}}'
    echo "== RabbitMQ  cola principal: $(queue_msgs fleettrack.events.q) mensajes | cola de fallidos: $(queue_msgs fleettrack.events.dead)"
    echo "== Outbox pendiente de publicar: $(pending_outbox)"
    echo "== Eventos procesados por el consumidor: $(event_count)"
    echo "== Posiciones guardadas: $($PSQL 'select count(*) from "TrackingEvents"')"
    echo "== Redis: $(docker compose exec -T redis redis-cli ping 2>&1 | tr -d '\r' | head -1)"
    ;;
  tracking)
    code=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/api/tracking" -H "Authorization: Bearer $(drv_token)" -H 'Content-Type: application/json' \
      -d "{\"latitude\":14.6$((RANDOM % 90 + 10)),\"longitude\":-90.5$((RANDOM % 90 + 10))}")
    echo "POST /api/tracking -> $code  (mira el dashboard)"
    ;;
  evento)
    D=$(new_route 1 | first_delivery)
    code=$(curl -s -o /dev/null -w '%{http_code}' -X POST "$API/api/deliveries/$D/start" -H "Authorization: Bearer $(drv_token)")
    echo "Entrega $D iniciada -> $code. Eventos nuevos: RouteStarted y DeliveryStarted."
    ;;
  redis-off)    docker compose stop redis;    echo "Redis detenido: recarga el dashboard (aviso de modo degradado)." ;;
  redis-on)     docker compose start redis;   echo "Redis arriba otra vez." ;;
  consumer-off) docker compose stop consumer; echo "Consumidor detenido: ejecuta 'evento' y mira la cola crecer en RabbitMQ." ;;
  consumer-on)  docker compose start consumer; echo "Consumidor arriba: la cola se vacia sola." ;;
  broker-off)   docker compose stop rabbitmq; echo "RabbitMQ detenido: la API sigue; el outbox acumula eventos (ejecuta 'estado')." ;;
  broker-on)    docker compose start rabbitmq; echo "RabbitMQ arriba: el outbox se vacia en segundos (ejecuta 'estado')." ;;
  falla)
    F=backend/tests/FleetTrack.Api.IntegrationTests/ManagementTests.cs
    trap 'git checkout -- "$F" && echo "Archivo restaurado."' EXIT
    sed -i 's/HttpStatusCode.Created, created.StatusCode/HttpStatusCode.OK, created.StatusCode/' "$F"
    echo "Prueba rota a proposito (se espera 201 y ahora se exige 200). Ejecutando..."
    dotnet test backend/tests/FleetTrack.Api.IntegrationTests/FleetTrack.Api.IntegrationTests.csproj 2>&1 | grep -E "Error Message|Assert|Expected|Actual|Con error|Superado|Failed|Erróneas|Correctas" | head -12
    ;;
  k6)
    for t in load stress spike; do
      node -e 'const s=require("./reports/performance/'$t'-completo.json").metrics;const d=s.http_req_duration;
        console.log("'$t'".padEnd(7),"p95="+d["p(95)"].toFixed(1)+" ms","| peticiones="+s.http_reqs.count,"| usuarios max="+s.vus_max.value,"| umbral p95<500 ms:",d["p(95)"]<500?"CUMPLE":"NO CUMPLE")'
    done
    ;;
  *) sed -n 2,17p "$0" | cut -c3- ;;
esac
