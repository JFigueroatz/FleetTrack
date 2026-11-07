# Semana 7 — Correcciones, regresión y candidato a release

Commits de esta semana: identidad de Andrea (reasignación acordada). Alejandro no commitea; sigue siendo quien aprueba y mergea.

## 1. Correcciones de los hallazgos de la semana 6

| Hallazgo | Corrección |
|---|---|
| Sonar: value-types sin `required` en records de entrada (`TrackingRequest`, `CreateRouteRequest`, `CreateRouteDeliveryItem`, `VehicleRequest`) | `[property: JsonRequired]`: un campo omitido ahora devuelve 400 en vez de asumir `0`/`Guid.Empty` en silencio. |
| Sonar: `app.Run()` / `host.Run()` | `await app.RunAsync()` en API y consumer. |
| Sonar bugs: promesas sin esperar (`LoginPage`, `DashboardPage`) | `void navigate(...)` / `void load()`. |
| Sonar: contenedores corrían como root (API, consumer, web) | API y consumer con `USER $APP_UID` (carpeta `/app/evidence` con permiso para ese usuario). Web ahora usa `nginxinc/nginx-unprivileged` (puerto interno 8080; el host sigue en 3000). |
| Sonar: Terraform sin identidad administrada / IP pública | `identity { type = "SystemAssigned" }` en la VM. La IP pública se **acepta** a propósito (QA debe ser alcanzable) y queda anotada con `# NOSONAR` y su justificación. |
| ZAP web (9 avisos): anti-clickjacking, `X-Content-Type-Options`, CSP, Permissions-Policy, `Server`, COEP | Headers agregados en `frontend/nginx.conf` y `server_tokens off`. |
| ZAP API (1 aviso): `Cross-Origin-Resource-Policy` | Middleware en la API (`nosniff` + CORP `same-origin`, válido porque el dashboard usa CORS). |
| Sonar sobre mis scripts de la semana 6 (k6, spec axe) | PRNG/clave de prueba marcados con justificación; el spec de accesibilidad ahora tiene aserciones explícitas. |

## 2. Resultados antes / después (herramientas ejecutadas de verdad)

| Medida | Semana 6 | Semana 7 |
|---|---|---|
| Sonar: bugs | 2 | **0** |
| Sonar: vulnerabilidades | 5 | **0** |
| Sonar: code smells | 14 | **4** (logging, nivel INFO) |
| Sonar: violaciones en código nuevo | — | **0** |
| ZAP web: avisos | 9 | **3** (comentarios dentro de librerías, assets estáticos cacheables, aviso informativo) |
| ZAP API: avisos | 1 | **0** (120 PASS) |
| Semgrep (backend, frontend, mobile) | 0 | 0 |
| Contenedores como root | 3 | **0** |

Reportes nuevos en `reports/` (carpeta ignorada por git: existen solo en la máquina que los generó): `reports/zap/*-semana7.*`, `reports/sonar/*-semana7.json`.

## 3. Regresión completa (después de las correcciones)

| Suite | Resultado |
|---|---|
| xUnit unitarias | 6/6 |
| xUnit integración (contra Docker real) | 14/14 |
| Recuperación (`tests/recovery/run-all.sh`) | 26/26 |
| Cypress (contra el dashboard servido por nginx, puerto 3000) | 15/15 |

Hallazgo de la regresión: el experimento E2 de recuperación fallaba si corría justo después de las pruebas de integración o de k6, porque dejaban en Redis una posición más reciente y el guard de orden (correctamente) no la pisaba. No era un defecto del producto sino falta de aislamiento entre suites: el experimento ahora limpia su clave de cache al empezar.

## 4. Estado del candidato a release

**Apto como candidato de funcionalidad. No apto todavía para un GO final.** Lo que impide el GO:

| Criterio del plan | Estado |
|---|---|
| Quality Gate de SonarQube | **ERROR**: única condición fallida, cobertura de código nuevo 0% (umbral 80%). No se mide cobertura de las pruebas de integración (corren por HTTP contra Docker). Se mantiene el umbral: el plan prohíbe cambiarlo para ocultar un fallo. |
| Dependency-Check | Bloqueado: falta API key de NVD. Mientras tanto `npm audit` y `dotnet list package --vulnerable` dan 0. |
| Usabilidad con 5 participantes | Pendiente: requiere personas reales (protocolo listo). |
| k6 Load/Stress/Spike a escala completa | Pendiente: solo corridas cortas (0% error). |
| App Android | Compila pero nunca se ejecutó en emulador/dispositivo; los 8 flujos Maestro no se ejecutaron. |
| Terraform / Ansible | Código listo, sin ejecutar (Azure sin confirmar). |
| Pipeline Jenkins | No existe todavía (semana 8). |

Sugerencia de etiqueta para el candidato: `v0.9.0-rc1` (la crea Alejandro; no se creó ninguna etiqueta en esta sesión).

## 5. Para la semana 8

Jenkinsfile y pipeline, resolver la cobertura (medir cobertura del API durante las pruebas de integración, o decidir con el docente cómo se evalúa), ejecutar lo pendiente de la tabla anterior, documentos formales (plan de pruebas de 20 secciones, matriz de riesgos, casos de prueba, informe final) y la decisión GO/NO-GO.
