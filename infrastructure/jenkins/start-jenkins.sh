#!/usr/bin/env bash
# Levanta Jenkins en este equipo (controlador nativo + agentes 'qa' y 'android' = el propio equipo, que ya tiene
# Docker, .NET, Node, k6, Semgrep, el SDK de Android y el emulador). El war y los plugins se sacan de la imagen
# oficial jenkins/jenkins:lts-jdk21 para no depender de otra descarga.
# Uso: bash infrastructure/jenkins/start-jenkins.sh        (queda en primer plano; Ctrl+C lo detiene)
# Requisitos: SonarQube arriba (docker start sonarqube), Docker Desktop, .env del proyecto, Maestro instalado.
set -euo pipefail
cd "$(dirname "$0")/../.."
REPO=$(pwd -W 2>/dev/null || pwd)
export JENKINS_HOME=${JENKINS_HOME:-$HOME/jenkins-fleettrack}
HOME_W=$(cd "$HOME" && (pwd -W 2>/dev/null || pwd))
mkdir -p "$JENKINS_HOME/plugins" "$JENKINS_HOME/war"
IMG=jenkins/jenkins:lts-jdk21
export MSYS_NO_PATHCONV=1

[ -f "$JENKINS_HOME/war/jenkins.war" ] || docker run --rm -v "$HOME_W/jenkins-fleettrack/war:/out" --entrypoint cp $IMG /usr/share/jenkins/jenkins.war /out/
if [ -z "$(ls -A "$JENKINS_HOME/plugins")" ]; then
  docker run --rm -v "$REPO/infrastructure/jenkins/plugins.txt:/plugins.txt:ro" -v "$HOME_W/jenkins-fleettrack/plugins:/out" \
    $IMG jenkins-plugin-cli --plugin-file /plugins.txt --plugin-download-directory /out
fi

# Secretos solo en variables de entorno del proceso (nunca en Git).
[ -f "$JENKINS_HOME/admin-password.txt" ] || head -c 18 /dev/urandom | base64 | tr -d '/+=' > "$JENKINS_HOME/admin-password.txt"
export JENKINS_ADMIN_PASSWORD=$(cat "$JENKINS_HOME/admin-password.txt")
export SONAR_TOKEN=${SONAR_TOKEN:-$(curl -sf -u "admin:${SONAR_ADMIN_PASSWORD:?define SONAR_TOKEN o SONAR_ADMIN_PASSWORD}" -X POST \
  http://localhost:9000/api/user_tokens/generate -d "name=jenkins-$(date +%s)" | sed 's/.*"token":"\([^"]*\)".*/\1/')}
export QA_ENV_PATH="$REPO/.env"
export ANSIBLE_EXTRA_PATH="$JENKINS_HOME/ansible-extra-vars.json"
[ -f "$ANSIBLE_EXTRA_PATH" ] || echo '{"public_url":"http://localhost"}' > "$ANSIBLE_EXTRA_PATH"   # marcador: no se usa sin DEPLOY_FINAL
export REPO_URL=${REPO_URL:-$REPO}
export REPO_BRANCH=${REPO_BRANCH:-*/main}
export CASC_JENKINS_CONFIG="$REPO/infrastructure/jenkins/casc.yaml"

# Herramientas que usan las etapas (Maestro, Android SDK, adb).
SDK=${ANDROID_HOME:-$LOCALAPPDATA/Android/Sdk}
export ANDROID_HOME=$SDK ADB="$SDK/platform-tools/adb.exe" MAESTRO=${MAESTRO:-$HOME/tools/maestro-dl/x/maestro/bin/maestro}
export PATH="$SDK/platform-tools:$SDK/emulator:$(dirname "$MAESTRO"):$PATH"

echo "Jenkins en http://localhost:8081  usuario: alejandro  contrasena: $JENKINS_HOME/admin-password.txt"
exec java -Djenkins.install.runSetupWizard=false -jar "$JENKINS_HOME/war/jenkins.war" --httpPort=8081
