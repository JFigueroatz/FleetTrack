# Validación de la app Android en emulador

Primera ejecución real de la app móvil (hasta aquí solo se había comprobado que compila). Se hizo en un emulador
Android 15 (API 35, Google APIs, x86_64, AVD `fleettrack`, Pixel 6) contra la API real en Docker (`10.0.2.2:8080`),
manejando la app con `adb` (toques, texto, capturas) y comprobando el resultado en PostgreSQL.

## Qué se verificó

| Flujo | Resultado | Cómo se comprobó |
|---|---|---|
| Login del conductor contra la API | OK | Aparece "Mi ruta" con el nombre del conductor |
| Consulta de ruta | OK | Muestra la ruta `Planned` con 2 entregas `Pending` |
| Iniciar entrega | OK | "Entrega iniciada."; ruta y entrega pasan a `InProgress` |
| Envío de ubicación | OK | `TrackingEvents` recibió 37.4219983 / -122.084 (ubicación por defecto del emulador) cada 15 s exactos |
| Finalizar sin red | OK | Con wifi y datos apagados: "Sin conexion: 2 operacion(es) pendiente(s)" y la entrega sigue `InProgress` en el servidor |
| Sincronización al reconectar | OK | A los 4 s la entrega quedó `Completed`; los avisos desaparecieron; **exactamente 1** evento `DeliveryCompleted` |
| Entrega fallida con motivo | OK | Diálogo "Motivo:"; la entrega quedó `Failed` y el evento lleva `"Reason":"cliente ausente"` |
| Evidencia fotográfica | OK (tras la corrección 2) | `DeliveryEvidences` tiene un `image/jpeg` de 74,270 bytes de esa entrega |

El flujo central del plan (operación offline que se sincroniza al reconectar y se completa una sola vez) quedó
demostrado en la app real, no solo simulado por API como en la semana 4.

## Defectos encontrados al ejecutar (y corregidos)

1. **HTTP bloqueado por Android 9+.** Sin una `network_security_config`, la app no podía llamar a `http://10.0.2.2:8080`.
   Corrección agregada en la semana 7 (solo permite `10.0.2.2` y `localhost`); confirmada: el login funciona.
2. **La cámara no abría.** `MediaPicker` fallaba con "`android.media.action.IMAGE_CAPTURE` was not added to the
   `<queries>` element" (visibilidad de paquetes de Android 11+). Se agregó `<queries>` al manifest; confirmado:
   la cámara abre, toma la foto y la evidencia llega al servidor.
3. **Mensaje engañoso.** Al finalizar sin red, la app decía "Entrega finalizada." en verde aunque la operación solo estaba
   encolada. Ahora el mensaje solo se confirma sin reservas si no queda nada pendiente; si quedó encolada dice
   "… (guardada, se enviara al reconectar)". Verificado en pantalla (2026-11-05): "Entrega finalizada. (guardada, se enviara al reconectar)".

## Observaciones sin corregir

- La sesión vive solo en memoria: al reinstalar o reiniciar la app hay que iniciar sesión otra vez (las operaciones
  pendientes sí se conservan en SQLite).
- Con varias entregas, los botones se repiten por fila; los flujos Maestro asumen una sola entrega y habrá que
  seleccionar por fila.
- El estado se muestra en inglés (`InProgress`, `Completed`, `Failed`) porque viene tal cual de la API.

- Tras sincronizar, el mensaje verde "(guardada, se enviara al reconectar)" no se limpia aunque la entrega ya está `Completed` en el servidor; es solo cosmético, queda pendiente.

## Maestro

Los 8 flujos de `tests/maestro` pasan contra el emulador (detalle y correcciones a los flujos en `tests/maestro/README.md`).

## Cómo repetir la prueba

```bash
# 1. Emulador (una sola vez, ya instalado en esta máquina)
"C:/Users/jfigu/AppData/Local/Android/Sdk/emulator/emulator.exe" -avd fleettrack
# 2. Con docker compose up -d y una ruta asignada al conductor de prueba (dashboard o API), instalar la app:
dotnet build mobile -f net10.0-android -t:Install -p:AndroidSdkDirectory="C:\Users\jfigu\AppData\Local\Android\Sdk"
# 3. Permisos (ubicación y cámara) para no ver los diálogos del sistema
adb shell pm grant com.companyname.fleettrack.mobile android.permission.ACCESS_FINE_LOCATION
adb shell pm grant com.companyname.fleettrack.mobile android.permission.CAMERA
# 4. Offline / online
adb shell svc wifi disable; adb shell svc data disable      # apagar red
adb shell svc wifi enable;  adb shell svc data enable       # reconectar
```

## Pendiente

- Probar en un teléfono físico (requiere la IP de la PC en `AppConfig.cs` y la `network_security_config`).
- Limpiar el mensaje de "guardada" tras sincronizar.
