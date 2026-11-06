#!/usr/bin/env bash
# Deja un emulador Android arrancado, con la pantalla encendida, listo para Maestro (agente 'android').
# Variables: ANDROID_HOME (SDK), AVD (por defecto fleettrack).
set -euo pipefail
ADB="${ADB:-adb}"
EMULATOR="${EMULATOR:-${ANDROID_HOME:?define ANDROID_HOME}/emulator/emulator}"
if ! "$ADB" get-state >/dev/null 2>&1; then
  nohup "$EMULATOR" -avd "${AVD:-fleettrack}" -no-snapshot-save -no-audio >/dev/null 2>&1 &
  "$ADB" wait-for-device
fi
for _ in $(seq 1 60); do
  [ "$("$ADB" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" = 1 ] && break
  sleep 5
done
[ "$("$ADB" shell getprop sys.boot_completed | tr -d '\r')" = 1 ] || { echo "El emulador no termino de arrancar"; exit 1; }
"$ADB" shell input keyevent KEYCODE_WAKEUP
"$ADB" shell svc power stayon true
