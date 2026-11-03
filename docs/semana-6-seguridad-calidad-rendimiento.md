# Semana 6 — Seguridad, calidad, accesibilidad, usabilidad y rendimiento

**Cambio de asignación (acordado con el usuario):** la semana 6 completa se commitea como Manuel; la semana 7
(correcciones + regresión) se commiteará como Andrea. Alejandro no commitea en estas dos semanas — sigue siendo
el único que aprueba y mergea.

## SAST — Semgrep como sustituto de Fortify

Fortify requiere licencia (sin acceso confirmado en el plan original). Se sustituyó por **Semgrep** (gratuito),
corrido de verdad contra los 3 proyectos de código:

| Alcance | Reglas | Resultado |
|---|---|---|
| `backend/` (C#) | p/csharp, p/security-audit, p/secrets | 67 reglas, **0 hallazgos** |
| `frontend/src` + `frontend/cypress` (TS/React) | p/typescript, p/react, p/security-audit, p/secrets | 125 reglas, **0 hallazgos** |
| `mobile/` (C#/MAUI) | p/csharp, p/security-audit, p/secrets | 66 reglas, **0 hallazgos** |

Reportes: `reports/fortify/semgrep-*.json`.

## SonarQube + Quality Gate

Instancia Community real (`docker run sonarqube:community`), proyecto `fleettrack`, analizado con
`dotnet-sonarscanner` (begin/build/end) cubriendo backend, frontend y mobile.

- **Quality Gate: OK** (compliant).
- Métricas: 2474 líneas de código, 2 bugs, 5 vulnerabilidades, 14 code smells, 0 security hotspots, 0% duplicación.
- Hallazgos destacados (quedan para corregir en semana 7 — Andrea):
  - `infrastructure/terraform/main.tf`: IP pública sin restricción documentada, falta bloque `identity` (gestión de identidad administrada de Azure).
  - `backend/.../TrackingController.cs`, `RoutesController.cs`, `VehiclesController.cs`: propiedades `double`/value-type en records de entrada sin marcar `required` (riesgo de under-posting silencioso).
  - `backend/.../Program.cs` (API y Consumer): `app.Run()` sin `await` (debería ser `RunAsync`).
  - 3 Dockerfiles corren como root (práctica común pero señalada).
  - 2 `await` faltantes en el frontend (`LoginPage.tsx`, `DashboardPage.tsx`).
  - 3 falsos positivos de Sonar en `accesibilidad.cy.ts` (no reconoce `cy.checkA11y()` como aserción).

Reportes: `reports/sonar/sonarqube-*.json`. Dashboard vivo en `http://localhost:9000/dashboard?id=fleettrack` mientras el contenedor esté corriendo (se detuvo al cerrar la sesión; levantarlo de nuevo con `docker run -d --name sonarqube -p 9000:9000 sonarqube:community`, usuario `admin`).

## OWASP Dependency-Check — bloqueado

Se instaló y corrió la imagen oficial (`owasp/dependency-check`), pero **falla sin una API key de NVD**
(`nvd.nist.gov` exige una desde 2024; sin ella el `Invalid API Key` corta la descarga de la base de datos antes de
analizar nada). Pedir una es gratis e inmediato en https://nvd.nist.gov/developers/request-an-api-key — queda
pendiente de que alguien del equipo la solicite y la pase por `-d:sonar...` o `--nvdApiKey`.

**Cobertura real mientras tanto:** `npm audit` (frontend) y `dotnet list package --vulnerable --include-transitive`
(los 5 proyectos del backend) ya corrieron — **0 vulnerabilidades** en ambos, usando otras fuentes (GitHub
Advisory DB / NuGet feed) que no dependen de la API key de NVD.

## OWASP ZAP (DAST)

Corrido de verdad contra el stack en docker (`zaproxy/zap-stable`, red interna de docker-compose):

- **Web** (`zap-baseline.py`): 59 PASS, **9 WARN** (0 FAIL) — headers de seguridad ausentes: anti-clickjacking,
  `X-Content-Type-Options`, CSP, Permissions-Policy, `Server` expone versión, Cross-Origin-Embedder-Policy. Todo
  corregible agregando headers en nginx (semana 7).
- **API** (`zap-api-scan.py` contra `/health`, sin spec OpenAPI publicado todavía): 119 PASS, **1 WARN** (0 FAIL) —
  `Cross-Origin-Resource-Policy` ausente. Cobertura limitada: sin un OpenAPI/spec servido, ZAP no pudo enumerar el
  resto de endpoints; publicar `/openapi` en el ambiente de pruebas ampliaría el escaneo.

Reportes: `reports/zap/zap-*-report.{html,json}`.

## axe-core (accesibilidad)

`cypress-axe` no soporta Cypress 16 todavía (peer dependency tope en ^15), así que se integró `axe-core`
directamente vía comandos custom (`cy.injectAxe()` / `cy.checkA11y()`, ver `frontend/cypress/support/commands.ts`).
4 pantallas evaluadas contra WCAG 2 A/AA, **0 hallazgos críticos o serios** en las 4: `frontend/cypress/e2e/accesibilidad.cy.ts`, ejecutado de verdad (4/4 PASS).

## Usabilidad (5 participantes)

**No se ejecutó** — necesita personas reales, que el equipo coordina. Protocolo completo listo para usar:
`docs/usabilidad/protocolo-usabilidad.md` (guion, 7 tareas, criterio de éxito ≥80%) y
`docs/usabilidad/plantilla-resultados.csv` para registrar cada sesión.

## k6 — Load, Stress, Spike

Scripts reales en `tests/k6/` contra `POST /api/tracking` (API real en Docker), parametrizados por variables de
entorno para poder correr una versión corta de verificación o la versión completa del plan:

```bash
# Corridas completas del plan (duran varios minutos)
k6 run tests/k6/load.js      # 100 VUs, 10 min, p95<500ms, error<1%
k6 run tests/k6/stress.js    # rampa 100->300 VUs
k6 run tests/k6/spike.js     # salto a 300 VUs

# Version corta de verificacion (usada en esta sesion)
K6_VUS=20 K6_DURATION=30s k6 run tests/k6/load.js
K6_SHORT=1 k6 run tests/k6/stress.js
K6_SHORT=1 k6 run tests/k6/spike.js
```

Resultados de las corridas cortas ejecutadas de verdad en esta sesión (no son los umbrales finales del plan, que
exigen 100/300 VUs y 10+ minutos — eso queda para semana 7/8 con más tiempo de máquina):

| Escenario | VUs | Duración | p95 | Error |
|---|---|---|---|---|
| Load (corto) | 20 | 30s | 67.5ms | 0% |
| Stress (corto) | 20→60 | ~1min | 81.2ms | 0% |
| Spike (corto) | 20→80→20 | ~1min | 20ms | 0% |

shortcut: todos los VUs reportan ubicación como el mismo conductor del seed (un solo `driverId`); igual ejercita
API→PostgreSQL→Redis→RabbitMQ→consumidor con el volumen real de peticiones. Si se necesita separar métricas por
conductor, crear 100 conductores de prueba antes de la corrida completa. Reportes: `reports/performance/*.json`.

## Pendiente para semana 7 (Andrea)

- Corregir los hallazgos de SonarQube (bugs/vulnerabilities MAJOR), agregar headers de seguridad que pide ZAP,
  revisar el hallazgo del Terraform (IP pública).
- Pedir la API key de NVD y correr Dependency-Check de verdad.
- Coordinar y ejecutar la sesión de usabilidad con 5 participantes reales.
- Correr load/stress/spike a duración completa (según disponibilidad de máquina/tiempo).
- Regresión completa: recovery suite (26), integración xUnit (14+6), Cypress (15).
