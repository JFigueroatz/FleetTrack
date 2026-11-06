#!/usr/bin/env bash
# Cobertura real del backend para SonarQube (formato dotnet-coverage -> sonar.cs.vscoveragexml.reportsPaths).
#  1) unit.xml         unitarias (dotnet-coverage envuelve `dotnet test`).
#  2) integration.xml  API y consumidor corren en el host (no en Docker) bajo dotnet-coverage, contra postgres/redis/rabbitmq
#                      de docker-compose; se ejecutan las pruebas de integracion xUnit (que pegan por HTTP).
#  3) recovery.xml     la API corre en el host bajo cobertura y se ejecuta la suite de recuperacion (el consumidor queda en Docker,
#                      porque los experimentos lo detienen y arrancan con `docker compose`).
# Cada proceso se apaga con `dotnet-coverage shutdown` para volcar el reporte. Sonar une las lineas cubiertas de los tres.
# Requiere: infraestructura de docker-compose arriba, `dotnet tool install -g dotnet-coverage`, .env con las credenciales.
set -euo pipefail
cd "$(dirname "$0")/../.."
set -a; source .env; set +a
OUT=reports/coverage; mkdir -p $OUT; rm -f $OUT/*.xml $OUT/*.log
FAIL=0

export ConnectionStrings__Postgres="Host=localhost;Port=5433;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
export ConnectionStrings__Redis=localhost:6379 RabbitMQ__Host=localhost RabbitMQ__Username=$RABBITMQ_USER \
       RabbitMQ__Password=$RABBITMQ_PASSWORD Jwt__Secret=$JWT_SECRET ASPNETCORE_URLS=http://0.0.0.0:8080 ASPNETCORE_ENVIRONMENT=Production

# start_host <proyecto> <dll> <sesion> <salida.xml>   (en segundo plano)
start_host() {
  dotnet-coverage collect -f xml -o "$OUT/$4" --session-id "$3" "dotnet backend/src/$1/bin/Debug/net10.0/$2.dll" > "$OUT/$3.log" 2>&1 &
}
# Apaga todas las sesiones indicadas y despues espera (esperar antes de apagar la otra seria un bloqueo).
stop_host() { for s in "$@"; do dotnet-coverage shutdown "$s" || true; done; wait || true; }
wait_api() { for _ in $(seq 1 60); do curl -sf http://localhost:8080/health >/dev/null && return 0; sleep 2; done; echo "API no arranco"; return 1; }

echo "== 1) unitarias"
dotnet-coverage collect -f xml -o $OUT/unit.xml "dotnet test backend/tests/FleetTrack.Api.Tests/FleetTrack.Api.Tests.csproj"

dotnet build backend/src/FleetTrack.Api -c Debug -v q -p:UseSharedCompilation=false
dotnet build backend/src/FleetTrack.Consumer -c Debug -v q -p:UseSharedCompilation=false
# En un espacio de trabajo limpio (Jenkins) las pruebas de integracion no estan compiladas todavia.
dotnet build backend/tests/FleetTrack.Api.IntegrationTests -c Debug -v q -p:UseSharedCompilation=false

echo "== 2) integracion (API + consumidor en el host)"
docker compose stop api consumer
start_host FleetTrack.Api FleetTrack.Api fleetapi integration.xml
start_host FleetTrack.Consumer FleetTrack.Consumer fleetconsumer consumer-integration.xml
wait_api
dotnet test backend/tests/FleetTrack.Api.IntegrationTests/FleetTrack.Api.IntegrationTests.csproj --no-build || FAIL=1
stop_host fleetapi fleetconsumer

echo "== 3) recuperacion (API en el host; consumidor en Docker)"
docker compose up -d consumer
start_host FleetTrack.Api FleetTrack.Api fleetapi recovery.xml
wait_api
bash tests/recovery/run-all.sh | tee $OUT/recovery.txt || FAIL=1
stop_host fleetapi

docker compose up -d --wait api consumer
ls -la $OUT/*.xml
exit $FAIL
