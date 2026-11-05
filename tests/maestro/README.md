# Maestro — flujos móviles (D05)

8 flujos que cubren login, ruta, inicio, ubicación, evidencia, desconexión, sincronización y finalización
(sección 9 del plan de trabajo). Cada uno es independiente (`runFlow` encadena los pasos previos necesarios).

## Estado: ejecutados (2026-11-05)

Los 8 flujos se ejecutaron con Maestro 2.12.1 contra el emulador Android 15 (AVD `fleettrack`) y la API real en Docker:
**8/8 en verde**. Al correrlos por primera vez hubo que corregir los propios flujos (la app estaba bien):

- **01**: el teclado en pantalla tapaba el botón "Ingresar" → `hideKeyboard` antes de tocarlo.
- **05**: la cámara del sistema se abre sobre la app; faltaba disparar y confirmar (`shutter_button` y `done_button` de `com.android.camera2`).
- **06/07**: los patrones de texto exigían terminar en "pendiente" pero la app dice "pendiente(s) de sincronizar." (Maestro compara el texto completo) → se añadió `.*`. En 07 el patrón viejo habría hecho pasar el "no visible" sin comprobar nada. Se añadió además una pausa de ~7 s para que el agente apague la red antes de "Finalizar".
- **07/08**: el estado va dentro del texto de la fila ("1. Cliente de Prueba - Completed"), se verifica con `.*Completed`.

Cómo correrlos (emulador abierto, app instalada, `docker compose up -d`):

```bash
MAESTRO=/ruta/a/maestro ADB=/ruta/a/adb bash tests/maestro/run-all.sh          # los 8
bash tests/maestro/run-all.sh 06_desconexion                                    # uno
```

`run-all.sh` crea una ruta de una entrega para cada flujo y, en 06/07, apaga la red del emulador (`adb shell svc`) justo después de "Iniciar"; en 07 la reactiva ~22 s después. Instalación de Maestro: https://maestro.mobi (requiere Java 17+).

## Limitación de selectores

Los botones de `RoutePage` ("Iniciar", "Evidencia", "Finalizar", "Fallida") se repiten una vez por entrega de la
ruta; los flujos asumen una ruta de una sola entrega (ver seed de `SeedData.cs`) para que `tapOn` resuelva al
primer/único botón sin ambigüedad. Con varias entregas haría falta un selector por fila (`index` o un
`AutomationId` por entrega), pendiente para cuando se automatice contra una ruta real de varias paradas.

## Desconexión / reconexión

Maestro no controla el estado de red del dispositivo. Los flujos 06 y 07 asumen que el agente Jenkins con Android
(D05/D06, también pendiente de montar) desconecta y reconecta la red alrededor de esos pasos vía `adb shell svc
wifi|data disable|enable`, no vía comandos de Maestro.
