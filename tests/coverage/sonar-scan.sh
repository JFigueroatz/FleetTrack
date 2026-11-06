#!/usr/bin/env bash
# Analisis SonarQube con cobertura real. Requiere: contenedor `sonarqube` arriba, dotnet-sonarscanner,
# reports/coverage/*.xml generados por run-coverage.sh y SONAR_TOKEN (o SONAR_ADMIN_PASSWORD para crear uno temporal).
# Uso: bash tests/coverage/sonar-scan.sh [version]      (Jenkins pasa el numero de build como version)
set -euo pipefail
export MSYS_NO_PATHCONV=1
cd "$(dirname "$0")/../.."
HOST=${SONAR_HOST_URL:-http://localhost:9000}
if [ -z "${SONAR_TOKEN:-}" ]; then
  SONAR_TOKEN=$(curl -sf -u "admin:${SONAR_ADMIN_PASSWORD:?define SONAR_TOKEN o SONAR_ADMIN_PASSWORD}" -X POST \
    "$HOST/api/user_tokens/generate" -d "name=scan-$(date +%s)" | sed 's/.*"token":"\([^"]*\)".*/\1/')
fi
rm -rf .sonarqube
# Cobertura medida sobre el backend (xUnit). Excluidos y por que: Migrations (codigo generado por EF), frontend y mobile (UI validada
# con Cypress, axe y Maestro, no con cobertura de codigo), tests/docs/infrastructure/ansible (no son codigo de producto).
dotnet sonarscanner begin /k:fleettrack /v:"${1:-local}" /d:sonar.host.url="$HOST" /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.exclusions="**/bin/**,**/obj/**,**/node_modules/**,**/Migrations/**" \
  /d:sonar.coverage.exclusions="**/Migrations/**,frontend/**,mobile/**,tests/**,docs/**,infrastructure/**,ansible/**" \
  /d:sonar.cs.vscoveragexml.reportsPaths="reports/coverage/unit.xml,reports/coverage/integration.xml,reports/coverage/consumer-integration.xml,reports/coverage/recovery.xml"
dotnet build backend/FleetTrack.slnx --no-incremental -v q -p:UseSharedCompilation=false
dotnet build mobile -f net10.0-android --no-incremental -v q -p:UseSharedCompilation=false -p:AndroidSdkDirectory="${ANDROID_HOME:-C:\\Users\\jfigu\\AppData\\Local\\Android\\Sdk}"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
