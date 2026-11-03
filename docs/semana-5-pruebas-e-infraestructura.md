# Semana 5 — Pruebas, Insomnia, Terraform, Ansible y Cypress

## A06/A07 — Alejandro

**Pruebas de integración** (`backend/tests/FleetTrack.Api.IntegrationTests`): 14 pruebas xUnit contra la pila real
(`docker compose up -d`), sin mocks — HTTP a la API, SQL directo a PostgreSQL. Cubren:
- Auth/ownership: login inválido (401), `/drivers/me` sin token (401) / rol equivocado (403) / correcto (200), acceso a ruta ajena, crear vehículo sin rol de supervisor.
- Ciclo de vida de entregas: asignación, `complete` idempotente (incluyendo 10 llamadas concurrentes), `complete` tras `fail` (409), `start` de una entrega ajena.
- Tracking + broker: coordenadas fuera de rango (400), reenvío del mismo `eventId` no duplica la fila, y una ubicación real llega a RabbitMQ y el consumidor la registra en `EventLogs` (round-trip completo API→outbox→broker→consumidor→PostgreSQL).

Resultado: **14/14 PASS**, más las 6 unitarias existentes (`FleetTrack.Api.Tests`) = 20/20.

**Insomnia** (`tests/insomnia/FleetTrack.insomnia.json`): colección exportable con auth, drivers, catálogo, rutas/entregas y tracking — casos positivos, negativos (401/403/400/409) y de autorización por rol/propiedad.

**Terraform** (`infrastructure/terraform/`): ciclo de vida completo (crear/modificar/destruir) de una VM Ubuntu en Azure con red, IP pública y NSG. **No se ejecutó `terraform apply`**: la elegibilidad de Azure for Students sigue pendiente de confirmar (plan, sección 12), y aplicar crea recursos reales con costo potencial — decisión que le corresponde a Alejandro con la suscripción en mano. El código queda listo para cuando se confirme el acceso.

## M05/M06 — Manuel

**docker-compose**: `healthcheck` y `restart: unless-stopped` en los 6 servicios (antes solo 3 tenían healthcheck y solo el consumer reiniciaba). Se corrigieron dos healthchecks que fallaban por resolver `localhost` a IPv6 dentro del contenedor (sin listener ahí): ahora apuntan a `127.0.0.1` explícito. Se agregó `GET /health` en la API (confirma conexión a PostgreSQL) para el healthcheck y el futuro reverse proxy.

**Puerto de PostgreSQL**: se cambió el mapeo del host de `5432` a `5433` — esta máquina tiene un PostgreSQL nativo de Windows (`postgresql-x64-18`) ocupando `5432` en IPv4, lo que hacía que cualquier conexión desde el host (incluidas las pruebas de integración) golpeara el Postgres equivocado y fallara con "password authentication failed", aunque `docker exec` (vía socket Unix, con auth `trust`) nunca lo mostraba. La API sigue usando el hostname interno `postgres` de Docker, no se ve afectada.

**Ansible + reverse proxy** (`ansible/`): playbook `site.yml` con 3 roles (`docker`, `fleettrack_app`, `reverse_proxy` con nginx) que deja una VM nueva corriendo el stack completo. **No se ejecutó contra un host real** (depende de la VM de Terraform); tampoco hay `ansible` instalado en esta máquina para un `--syntax-check` local.

**Cypress** (`frontend/cypress/`): instalado y **ejecutado de verdad** contra el servidor de desarrollo (`npm run dev`) y la API real en Docker. 4 specs, 11 pruebas, **11/11 PASS**:
- `smoke.cy.ts` (3): login carga, supervisor ve el dashboard, navegación a las 4 secciones.
- `flujo-critico-asignar-ruta.cy.ts` (1): crea cliente, vehículo y conductor desde la UI y arma una ruta con ellos.
- `regresion.cy.ts` (3): restricción de supervisor (semana 2), aviso de modo degradado por Redis (semana 4), lista de conductores no se rompe al agregar uno.
- `negativo.cy.ts` (4): login inválido, placa duplicada (409), ruta sin seleccionar conductor (validación HTML bloquea el submit), login con campos vacíos.

## D05 — Andrea

**Maestro** (`tests/maestro/`): 8 flujos (login, ruta, inicio, ubicación, evidencia, desconexión, sincronización, finalización), escritos contra los textos reales de las pantallas de `mobile/`. **No se ejecutaron**: no hay Maestro CLI ni emulador/dispositivo Android en esta máquina (la app móvil, como ya se documentó en semana 4, solo se validó por compilación). Jenkins con agente Android (D05/D06) sigue sin montarse.

## Resumen de lo verificado vs. lo pendiente de ejecución real

| Entregable | Verificado en esta sesión | Pendiente |
|---|---|---|
| Integración xUnit | Sí — 14/14 contra Docker real | — |
| Insomnia | Colección creada; no se corrió dentro de la app Insomnia | Importar y correr en Insomnia/Inso CLI |
| Terraform | Código completo, sin `apply` | Confirmar Azure for Students, `terraform apply` |
| docker-compose (healthchecks/volúmenes) | Sí — los 6 servicios `healthy` | — |
| Ansible + reverse proxy | Código completo, sin ejecutar | VM real (depende de Terraform), `ansible` instalado |
| Cypress | Sí — 11/11 contra el dev server real | Integrar a Jenkins cuando exista pipeline |
| Maestro | Código completo, sin ejecutar | Maestro CLI + emulador/dispositivo Android |

## Límites conocidos

- La prueba de integración "ubicación llega al broker y el consumidor la registra" correlaciona por marca de
  tiempo (`RecordedAt`) porque el evento `LocationUpdated` no lleva el `EventId` de la fila de tracking; es
  suficiente para esta prueba pero no serviría para deduplicar en producción si dos ubicaciones coincidieran al
  segundo exacto.
- Los flujos de Maestro asumen una ruta de una sola entrega (los botones de `RoutePage` se repiten por entrega).
