# Semana 4 — Mensajería, idempotencia y recuperación

## Diseño

```
API (cambio de negocio + OutboxMessages en la MISMA transacción)
  └─ OutboxPublisher (BackgroundService, en orden, con confirmación del broker)
        └─ RabbitMQ  exchange fleettrack.events (topic) ─► cola fleettrack.events.q
                                                              └─ FleetTrack.Consumer
                                                                   ├─ EventLogs (PK = MessageId → idempotencia)
                                                                   ├─ Redis: última posición, solo si el evento es más nuevo
                                                                   └─ fallo ×3 con espera creciente → cola fleettrack.events.dead
```

| Pieza | Decisión |
|---|---|
| Eventos | `LocationUpdated`, `RouteStarted`, `DeliveryStarted`, `DeliveryCompleted`, `DeliveryFailed` (`FleetTrack.Domain/Events`). Solo se emiten en una transición real: un reenvío no genera evento. |
| Outbox | Tabla `OutboxMessages`. Si RabbitMQ cae, la API sigue respondiendo y los eventos quedan pendientes; se publican al volver. Entrega *al-menos-una-vez*. |
| Idempotencia de API | `start/complete/fail` bajo bloqueo de la fila de la ruta (`SELECT … FOR UPDATE`) → dos peticiones concurrentes se serializan y solo la primera emite evento. `POST /api/tracking` acepta `eventId` (dedupe por PK) y `recordedAt`. La evidencia reenviada devuelve 200 sin duplicar. |
| Idempotencia del consumidor | `EventLogs.Id` = `MessageId` del outbox. Una repetición se detecta antes de aplicar efectos. |
| Eventos atrasados | La cache de última posición nunca se reemplaza por un `RecordedAt` anterior (API y consumidor). Todos los eventos se registran en `EventLogs` aunque lleguen tarde. |
| Fallos | 3 reintentos (0,5 s, 1 s, 2 s) re-encolando con header `x-retry`; luego `fleettrack.events.dead` con el header `x-error`. |
| Redis caído | La API (`POST /tracking`) y el consumidor siguen; `GET /tracking/latest` lee PostgreSQL y responde `X-Redis-Status: unavailable`; el dashboard muestra un aviso de modo degradado. |
| Móvil | Toda operación (iniciar/finalizar/fallida/evidencia/ubicación) se encola en SQLite (`PendingOperation`) y `SyncService` la reenvía **en orden**; se detiene ante un fallo de red y descarta lo que el servidor rechaza con 4xx definitivo (excepto 401/408/429). Se vacía al reconectar, al abrir la pantalla y cada 15 s. |

## Experimentos de recuperación

`bash tests/recovery/run-all.sh` (requiere `docker compose up -d --build` con `.env`). Resultado de la última corrida: **26 PASS, 0 FAIL**.

| # | Experimento | Verificado |
|---|---|---|
| E1 | Finalización duplicada y concurrente | 10 `complete` simultáneos → 1 solo `DeliveryCompleted`; reenvío posterior 200; completar una fallida → 409 |
| E2 | Ubicaciones repetidas y fuera de orden | mismo `eventId` ×3 → 1 fila; una ubicación de hace 30 min enviada después no reemplaza a la reciente; coordenadas inválidas → 400 |
| E3 | Consumidor: repetidos, atrasados, envenenados | 3 copias del mismo `MessageId` → 1 registro; evento atrasado no toca la cache; JSON inválido termina en la cola de fallidos |
| E4 | Caída de Redis | tracking 200, dashboard 200 con `X-Redis-Status`, dato leído de PostgreSQL |
| E5 | Caída del consumidor | la API sigue; al volver se procesa lo acumulado |
| E6 | Caída del broker | la API responde; el outbox acumula; al volver se vacía y el consumidor registra todo |
| E7 | Reconexión de Android | la cola se reenvía dos veces (corte a mitad de sincronización): 1 entrega completada, 2 ubicaciones, 1 evidencia |

## Límites conocidos

- **E7 está simulado a nivel de API**: reproduce exactamente las llamadas que hace `SyncService`, pero la app Android nunca se ejecutó en emulador/dispositivo (solo compila). Falta la prueba manual real con modo avión.
- Los reintentos del consumidor esperan en línea (procesa de a un mensaje); suficiente para esta carga, a cambiar por colas de retraso si crece.
- El outbox no se purga; a partir de la semana 5 conviene un job de limpieza.
- Un mensaje en la cola de fallidos no tiene reproceso automático (es manual desde la consola de RabbitMQ).
- La sesión del móvil sigue en memoria: tras reiniciar la app hay que volver a iniciar sesión (las operaciones pendientes se conservan).
