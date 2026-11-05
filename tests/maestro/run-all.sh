#!/usr/bin/env bash
# Corre los flujos Maestro con una ruta nueva (1 entrega) por flujo. Uso: bash tests/maestro/run-all.sh [flujo ...]
# Para 06/07 apaga (y en 07 reactiva) la red del emulador via adb justo despues de "Iniciar".
export MAESTRO_CLI_NO_ANALYTICS=1
M="${MAESTRO:-maestro}"
ADB="${ADB:-adb}"
cd "$(dirname "$0")/../.."
source tests/recovery/common.sh

run() { # $1 flujo, $2 = offline-helper (none|off|offon)
  local f=$1 mode=${2:-none} did
  did=$(new_route 1 | grep -o '"deliveries":\[{"id":"[^"]*"' | cut -d'"' -f6)
  echo "=== $f (entrega $did, modo $mode)"
  "$ADB" shell svc wifi enable; "$ADB" shell svc data enable
  if [ "$mode" != none ]; then
    ( for i in $(seq 1 90); do
        [ "$($PSQL "select \"Status\" from \"Deliveries\" where \"Id\"='$did'" | tr -d '\r')" = 1 ] && break; sleep 1
      done
      "$ADB" shell svc wifi disable; "$ADB" shell svc data disable
      if [ "$mode" = offon ]; then sleep 22; "$ADB" shell svc wifi enable; "$ADB" shell svc data enable; fi
    ) &
    HP=$!
  fi
  timeout 240 "$M" test tests/maestro/$f.yaml 2>&1 | grep -E "FAILED|COMPLETED$|Assertion" | grep -vE "^\s+(Launch|Tap|Input|Hide)" | tail -8
  echo "RESULT $f exit=${PIPESTATUS[0]}"
  [ -n "$HP" ] && wait $HP; HP=
  "$ADB" shell svc wifi enable; "$ADB" shell svc data enable; sleep 3
}
[ $# -eq 0 ] && set -- $(cd tests/maestro && ls [0-9]*.yaml | sed 's/\.yaml$//')
for f in "$@"; do
  case $f in
    06_desconexion) run $f off ;;
    07_sincronizacion) run $f offon ;;
    *) run $f none ;;
  esac
done
