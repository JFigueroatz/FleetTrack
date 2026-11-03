#!/usr/bin/env bash
# Utilidades compartidas por los experimentos de recuperacion. Requiere el stack de docker-compose levantado.
API=${API:-http://localhost:8080}
RMQ=${RMQ:-http://localhost:15672}
RMQ_AUTH="${RABBITMQ_USER:-fleettrack}:${RABBITMQ_PASSWORD:-dev_rmq_pass}"
PSQL="docker compose exec -T postgres psql -U ${POSTGRES_USER:-fleettrack} -d ${POSTGRES_DB:-fleettrack} -tA -c"

login() { curl -s -X POST $API/api/auth/login -H 'Content-Type: application/json' -d "{\"email\":\"$1\",\"password\":\"$2\"}" | sed 's/.*"token":"\([^"]*\)".*/\1/'; }
sup_token() { login supervisor@fleettrack.test 'Supervisor123!'; }
drv_token() { login conductor.prueba@fleettrack.test 'Conductor123!'; }
json_get() { sed -n "s/.*\"$1\":\"\([^\"]*\)\".*/\1/p" | head -1; }

# Crea una ruta de N entregas para el conductor de prueba; imprime el id de la primera entrega.
new_route() {
  local S=$(sup_token) n=${1:-1} d v c items="" i
  d=$(curl -s $API/api/drivers -H "Authorization: Bearer $S" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
  v=$(curl -s $API/api/vehicles -H "Authorization: Bearer $S" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
  c=$(curl -s $API/api/customers -H "Authorization: Bearer $S" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
  for i in $(seq 1 $n); do items="$items{\"customerId\":\"$c\",\"sequence\":$i},"; done
  curl -s -X POST $API/api/routes -H "Authorization: Bearer $S" -H 'Content-Type: application/json' \
    -d "{\"driverId\":\"$d\",\"vehicleId\":\"$v\",\"deliveries\":[${items%,}]}"
}
drv_id() { curl -s $API/api/drivers/me -H "Authorization: Bearer $(drv_token)" | json_get id; }
pending_outbox() { $PSQL "select count(*) from \"OutboxMessages\" where \"PublishedAt\" is null"; }
event_count() { $PSQL "select count(*) from \"EventLogs\" ${1:+where \"Type\"='$1'}"; }
dead_count() { curl -s -u $RMQ_AUTH $RMQ/api/queues/%2F/fleettrack.events.dead | sed -n 's/.*"messages":\([0-9]*\).*/\1/p' | head -1; }
