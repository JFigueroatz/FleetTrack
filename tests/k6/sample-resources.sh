#!/usr/bin/env bash
# Mide los recursos del sistema mientras corre una prueba de carga: CPU y memoria de cada contenedor, mensajes en las
# colas de RabbitMQ, memoria y clientes de Redis y conexiones activas de PostgreSQL. Una fila por muestra.
# Uso: bash tests/k6/sample-resources.sh <salida.csv> [intervalo_s] [muestras]
set -uo pipefail
cd "$(dirname "$0")/../.."
OUT=${1:?uso: sample-resources.sh salida.csv [intervalo_s] [muestras]}
EVERY=${2:-15}; N=${3:-80}
[ -f .env ] && { set -a; . ./.env; set +a; }
echo "hora_utc,api_cpu_pct,api_mem_mb,postgres_cpu_pct,rabbitmq_cpu_pct,redis_cpu_pct,consumer_cpu_pct,cola_principal,cola_fallidos,redis_mem_mb,redis_clientes,pg_conexiones" > "$OUT"
num() { echo "${1:-}" | tr -d '%\r' | sed 's/MiB.*//; s/GiB.*//' | awk '{print $1}'; }
for _ in $(seq 1 "$N"); do
  stats=$(docker stats --no-stream --format '{{.Name}}|{{.CPUPerc}}|{{.MemUsage}}' 2>/dev/null)
  get() { echo "$stats" | grep "fleettrack-$1-1" | cut -d'|' -f"$2"; }
  # API de gestion (ligera): rabbitmqctl arranca una VM de Erlang por muestra e infla la CPU medida de RabbitMQ.
  qm() { curl -s -u "${RABBITMQ_USER:-fleettrack}:${RABBITMQ_PASSWORD:-dev_rmq_pass}" "http://localhost:15672/api/queues/%2F/$1" | grep -o '"messages":[0-9]*' | head -1 | cut -d: -f2; }
  rmem=$(docker compose exec -T redis redis-cli info memory 2>/dev/null | tr -d '\r' | sed -n 's/^used_memory:\([0-9]*\)/\1/p')
  rcli=$(docker compose exec -T redis redis-cli info clients 2>/dev/null | tr -d '\r' | sed -n 's/^connected_clients:\([0-9]*\)/\1/p')
  pgc=$(docker compose exec -T postgres psql -U "${POSTGRES_USER:-fleettrack}" -d "${POSTGRES_DB:-fleettrack}" -tA -c "select count(*) from pg_stat_activity where datname='${POSTGRES_DB:-fleettrack}'" 2>/dev/null | tr -d '\r')
  echo "$(date -u +%T),$(num "$(get api 2)"),$(num "$(get api 3)"),$(num "$(get postgres 2)"),$(num "$(get rabbitmq 2)"),$(num "$(get redis 2)"),$(num "$(get consumer 2)"),$(qm fleettrack.events.q),$(qm fleettrack.events.dead),$(awk -v b="${rmem:-0}" 'BEGIN{printf "%.1f", b/1048576}'),${rcli:-},${pgc:-}" >> "$OUT"
  sleep "$EVERY"
done
