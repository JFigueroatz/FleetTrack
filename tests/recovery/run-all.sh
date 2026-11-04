#!/usr/bin/env bash
# Experimentos de recuperacion (semana 4, seccion 9 del plan). Requiere: docker compose up -d --build (con .env) y bash con curl.
# Uso: bash tests/recovery/run-all.sh
cd "$(dirname "$0")/../.." || exit 1
source tests/recovery/common.sh

ok=0; bad=0
check() { if [ "$2" = "$3" ]; then echo "  PASS  $1"; ok=$((ok+1)); else echo "  FAIL  $1 (esperado '$3', obtenido '$2')"; bad=$((bad+1)); fi; }
wait_eq() { for _ in $(seq 1 ${3:-40}); do [ "$($1)" = "$2" ] && return 0; sleep 1; done; return 1; }
new_uuid() { od -An -x -N16 /dev/urandom | tr -d ' \n' | sed 's/\(........\)\(....\)\(....\)\(....\)\(............\)/\1-\2-\3-\4-\5/'; }
iso() { date -u -d "$1" +%Y-%m-%dT%H:%M:%SZ; }
delivery_ids() { grep -o '"deliveries":.*' | grep -o '"id":"[^"]*"' | cut -d'"' -f4; }
post() { curl -s -o /dev/null -w '%{http_code}' -X POST "$API$1" -H "Authorization: Bearer $T" -H 'Content-Type: application/json' ${2:+-d "$2"}; }
last_lat() { curl -s $API/api/tracking/latest -H "Authorization: Bearer $S" | sed -n 's/.*"latitude":\([-0-9.]*\).*/\1/p' | head -1; }
publish_raw() { # message_id type payload
  curl -s -u $RMQ_AUTH -X POST $RMQ/api/exchanges/%2F/fleettrack.events/publish -H 'Content-Type: application/json' \
    -d "{\"properties\":{\"message_id\":\"$1\",\"type\":\"$2\"},\"routing_key\":\"$2\",\"payload\":\"$3\",\"payload_encoding\":\"string\"}" > /dev/null; }

S=$(sup_token); T=$(drv_token); DRV=$(drv_id)
[ -n "$S" ] && [ -n "$T" ] || { echo "No se pudo autenticar; el stack esta arriba?"; exit 1; }
wait_eq pending_outbox 0 30

echo "E1  Finalizacion duplicada y concurrente"
mapfile -t D < <(new_route 3 | delivery_ids)
base=$(event_count DeliveryCompleted)
post /api/deliveries/${D[0]}/start > /dev/null
for _ in $(seq 1 10); do post /api/deliveries/${D[0]}/complete > /dev/null & done; wait
sleep 4
check "10 complete concurrentes generan 1 solo DeliveryCompleted" "$(( $(event_count DeliveryCompleted) - base ))" "1"
check "reenvio posterior sigue siendo 200" "$(post /api/deliveries/${D[0]}/complete)" "200"
check "completar una entrega fallida -> 409" "$(post /api/deliveries/${D[1]}/fail '{"reason":"cliente ausente"}' >/dev/null; post /api/deliveries/${D[1]}/complete)" "409"
sleep 3
check "DeliveryFailed registrado" "$(event_count DeliveryFailed | awk '{print ($1>=1)}')" "1"

echo "E2  Tracking: reenvio con EventId y ubicaciones fuera de orden"
# Aislamiento: otras suites (integracion xUnit, k6) dejan posiciones recientes en Redis; el guard de orden las respetaria.
docker compose exec -T redis redis-cli DEL "driver:$DRV:lastposition" > /dev/null
E=$(new_uuid); base=$($PSQL "select count(*) from \"TrackingEvents\"")
post /api/tracking "{\"latitude\":14.60,\"longitude\":-90.50,\"eventId\":\"$E\",\"recordedAt\":\"$(iso '-10 min')\"}" > /dev/null
check "mismo EventId reenviado 3 veces -> 200" "$(post /api/tracking "{\"latitude\":14.60,\"longitude\":-90.50,\"eventId\":\"$E\"}")" "200"
post /api/tracking "{\"latitude\":14.60,\"longitude\":-90.50,\"eventId\":\"$E\"}" > /dev/null
check "se guardo una sola fila" "$(( $($PSQL "select count(*) from \"TrackingEvents\"") - base ))" "1"
post /api/tracking "{\"latitude\":14.90,\"longitude\":-90.50,\"recordedAt\":\"$(iso '-1 min')\"}" > /dev/null
post /api/tracking "{\"latitude\":14.10,\"longitude\":-90.50,\"recordedAt\":\"$(iso '-30 min')\"}" > /dev/null   # llega tarde
sleep 3
check "la ubicacion atrasada no reemplaza a la mas reciente" "$(last_lat)" "14.9"
check "coordenadas invalidas -> 400" "$(post /api/tracking '{"latitude":99,"longitude":0}')" "400"

echo "E3  Consumidor: mensajes repetidos, atrasados y envenenados"
M=$(new_uuid); base=$(event_count LocationUpdated)
P="{\\\"DriverId\\\":\\\"$DRV\\\",\\\"DriverName\\\":\\\"x\\\",\\\"Latitude\\\":10.5,\\\"Longitude\\\":-90.0,\\\"RecordedAt\\\":\\\"$(iso '-2 hours')\\\"}"
publish_raw $M LocationUpdated "$P"; publish_raw $M LocationUpdated "$P"; publish_raw $M LocationUpdated "$P"
sleep 4
check "3 copias del mismo MessageId -> 1 registro" "$(( $(event_count LocationUpdated) - base ))" "1"
check "evento atrasado no toco la cache" "$(last_lat)" "14.9"
dead=$(dead_count); publish_raw $(new_uuid) LocationUpdated "no es json"
check "mensaje envenenado llega a la cola de fallidos tras reintentos" "$(wait_eq 'dead_count' $((dead+1)) 30 && echo si)" "si"

echo "E4  Caida de Redis"
docker compose stop redis > /dev/null 2>&1
check "POST /api/tracking responde 200 sin Redis" "$(post /api/tracking "{\"latitude\":14.95,\"longitude\":-90.5,\"recordedAt\":\"$(iso 'now')\"}")" "200"
code=$(curl -s -o /dev/null -w '%{http_code}' -D /tmp/h.txt $API/api/tracking/latest -H "Authorization: Bearer $S")
check "dashboard (GET latest) sigue en 200" "$code" "200"
check "indica modo degradado" "$(grep -ci 'x-redis-status: unavailable' /tmp/h.txt)" "1"
check "el dato sale de PostgreSQL (ultima posicion)" "$(last_lat)" "14.95"
docker compose start redis > /dev/null 2>&1; sleep 5

echo "E5  Caida del consumidor"
docker compose stop consumer > /dev/null 2>&1
base=$(event_count DeliveryStarted)
post /api/deliveries/${D[2]}/start > /dev/null
sleep 3
check "la API sigue aceptando operaciones" "$(post /api/deliveries/${D[2]}/start)" "200"
check "outbox publico igual (el evento espera en la cola)" "$(pending_outbox)" "0"
docker compose start consumer > /dev/null 2>&1
check "al volver, el consumidor procesa lo acumulado" "$(wait_eq 'event_count DeliveryStarted' $((base+1)) 60 && echo si)" "si"

echo "E6  Caida del broker (RabbitMQ)"
mapfile -t D2 < <(new_route 2 | delivery_ids)
docker compose stop rabbitmq > /dev/null 2>&1
base=$(event_count DeliveryStarted)
check "start responde 200 sin broker" "$(post /api/deliveries/${D2[0]}/start)" "200"
post /api/deliveries/${D2[0]}/complete > /dev/null
sleep 4
check "eventos quedan pendientes en el outbox (>=3)" "$(pending_outbox | awk '{print ($1>=3)}')" "1"
docker compose start rabbitmq > /dev/null 2>&1
check "al volver el broker el outbox se vacia" "$(wait_eq pending_outbox 0 90 && echo si)" "si"
check "y el consumidor registra los eventos" "$(wait_eq 'event_count DeliveryStarted' $((base+1)) 60 && echo si)" "si"

echo "E7  Reconexion de Android: cola offline reenviada dos veces"
mapfile -t D3 < <(new_route 1 | delivery_ids)
base=$(event_count DeliveryCompleted); tbase=$($PSQL "select count(*) from \"TrackingEvents\"")
U1=$(new_uuid); U2=$(new_uuid)
for _ in 1 2; do   # la cola se reenvia en orden; el segundo envio simula un reintento tras un corte a mitad de sincronizacion
  post /api/deliveries/${D3[0]}/start > /dev/null
  post /api/tracking "{\"latitude\":14.2,\"longitude\":-90.1,\"eventId\":\"$U1\",\"recordedAt\":\"$(iso '-8 min')\"}" > /dev/null
  post /api/tracking "{\"latitude\":14.3,\"longitude\":-90.1,\"eventId\":\"$U2\",\"recordedAt\":\"$(iso '-7 min')\"}" > /dev/null
  post /api/deliveries/${D3[0]}/complete > /dev/null
done
sleep 4
check "entrega completada una sola vez" "$(( $(event_count DeliveryCompleted) - base ))" "1"
check "2 ubicaciones guardadas, sin duplicados" "$(( $($PSQL "select count(*) from \"TrackingEvents\"") - tbase ))" "2"
printf 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGNgYGD4DwABBAEAfbLI3wAAAABJRU5ErkJggg==' | base64 -d > /tmp/px.png
ev() { curl -s -o /dev/null -w '%{http_code}' -X POST $API/api/deliveries/${D3[0]}/evidence -H "Authorization: Bearer $T" -F "file=@$(cygpath -m /tmp/px.png 2>/dev/null || echo /tmp/px.png);type=image/png"; }
check "evidencia reenviada 2 veces -> 200 y 200" "$(ev)$(ev)" "200200"
check "una sola evidencia guardada" "$($PSQL "select count(*) from \"DeliveryEvidences\" where \"DeliveryId\"='${D3[0]}'")" "1"

echo; echo "Resultado: $ok PASS, $bad FAIL"
[ $bad -eq 0 ]
