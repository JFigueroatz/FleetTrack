# Guía de presentación y defensa — FleetTrack

Duración: 10 a 15 minutos de exposición + defensa práctica en vivo. Todos los integrantes participan y deben conocer toda la solución.

## 0. Antes de entrar (30 minutos antes)

Levanta todo y deja estas pestañas abiertas. Si algo falla, falla antes y no frente al docente.

```bash
docker compose up -d --wait            # los 6 servicios en healthy
docker start sonarqube                 # http://localhost:9000 (usuario admin; contraseña en docs/guia-validacion.md)
SONAR_ADMIN_PASSWORD='<contraseña de Sonar>' REPO_BRANCH='*/main' bash infrastructure/jenkins/start-jenkins.sh   # http://localhost:8081
"C:/Users/jfigu/AppData/Local/Android/Sdk/emulator/emulator.exe" -avd fleettrack      # emulador con la app instalada
```

| Pestaña | Para qué |
|---|---|
| http://localhost:3000 (supervisor de prueba; credenciales en `docs/guia-validacion.md`) | Dashboard web |
| http://localhost:15672 (usuario y contraseña de `RABBITMQ_*` en `.env`) | Colas, conexiones y cola de fallidos |
| http://localhost:9000/dashboard?id=fleettrack | SonarQube, Quality Gate |
| http://localhost:8081/job/fleettrack | Jenkins, build #11 y sus etapas |
| `reports/zap/zap-web-report-semana7.html` | Hallazgos de ZAP |
| `docs/plan-pruebas.pdf`, `arquitectura.pdf`, `informe-final.pdf`, `matriz-riesgos.xlsx`, `casos-prueba.xlsx` | Documentos formales |
| Una terminal Git Bash en la raíz del repo | Comandos de la defensa |

Antes de abrir la sesión: crea una ruta para el conductor de prueba (dashboard o `source tests/recovery/common.sh; new_route 1`) y deja la app Android con la pantalla encendida.

## 1. Estructura de la exposición (tiempos del curso)

| # | Tiempo | Bloque | Quién | Qué decir y mostrar |
|---|---|---|---|---|
| 1 | 4 min | Problema, arquitectura y riesgos | Alejandro | FleetTrack: logística de rutas, tracking y entregas. Supervisor en el dashboard, conductor en Android con modo sin conexión. Muestra el diagrama de `arquitectura.pdf` (dashboard y app → API .NET → PostgreSQL + outbox → RabbitMQ → consumidor idempotente, Redis como caché). Explica los 3 riesgos mayores: operación duplicada, pérdida de operaciones y caída de Redis/broker. Abre `matriz-riesgos.xlsx` (14 riesgos con probabilidad × impacto). |
| 2 | 3 min | Cloud, IaC y Ansible | Alejandro y Manuel | Terraform (Azure): VM, red, identidad administrada, IP pública aceptada con justificación. Ansible: Docker, despliegue y nginx como reverse proxy. **Di con claridad** que se validaron `terraform validate` y `ansible --syntax-check` (corren dentro del pipeline) y que el `apply` real no se hizo porque Azure for Students no está confirmado. El ambiente reproducible se demuestra con Docker Compose. |
| 3 | 5 min | Pipeline Jenkins y Quality Gates | Alejandro | Abre el build #11: 20 etapas en verde, en el orden del plan. Muestra que la etapa de criterios de salida bloquea si falta un reporte obligatorio y que la aprobación final solo la autoriza Alejandro (`input` con `submitter: alejandro`). SonarQube: Quality Gate OK, 0 bugs, 0 vulnerabilidades, cobertura del backend 82,1 %. |
| 4 | 3 min | API, Redis y RabbitMQ | Andrea | Outbox transaccional, idempotencia en tres niveles (bloqueo de fila, `eventId`, `MessageId`), 3 reintentos y cola de fallidos. Muestra las colas en RabbitMQ. Menciona la colección de Insomnia y las 22 pruebas de integración. |
| 5 | 4 min | Automatización: Cypress, Maestro y seguridad | Manuel y Andrea | Cypress 15/15 (incluye accesibilidad con axe), Maestro 8/8 sobre el emulador, ZAP (0 FAIL, 3 avisos informativos aceptados), Semgrep 0 hallazgos, npm audit y NuGet 0 vulnerabilidades, Dependency-Check. |
| 6 | 3 min | Rendimiento, usabilidad y resultados | Andrea y Manuel | k6 a escala completa: load 100 usuarios/10 min p95 20 ms; stress a 300 usuarios p95 23 ms; spike p95 29,7 ms; umbral p95 < 500 ms y errores < 1 %. 48 000 eventos sin pérdida ni duplicados. Usabilidad: resultados de las 5 personas. |
| 7 | 2 min | Conclusión y GO / NO-GO | Alejandro | Ver sección 6 de esta guía. |

## 2. Todo lo que se hizo, por semana (para contarlo y para responder preguntas)

| Semana | Responsable | Qué se entregó |
|---|---|---|
| 1 | Alejandro, Manuel, Andrea | Estructura del repositorio, plan de trabajo, scaffolds de API, dashboard y app móvil, `docker-compose`, rama protegida y flujo de PR |
| 2 | Alejandro | Identity + JWT con roles Supervisor y Driver, autorización por rol (PR #2) |
| 2-3 | Manuel y Andrea | Login y administración web de clientes, vehículos y conductores (PR #3); app Android con login, ruta simulada y SQLite (PR #4) |
| 3 | Alejandro | Rutas, entregas, evidencia fotográfica y tracking en la API (PR #5) |
| 3 | Manuel y Andrea | Asignación de rutas y dashboard con Redis (PR #6); ruta real, ubicación y evidencia en Android (PR #7) |
| 4 | Alejandro | Outbox transaccional, eventos de dominio y operaciones idempotentes (PR #8) |
| 4 | Andrea y Manuel | Consumidor RabbitMQ idempotente y cola offline en Android (PR #9); aviso de modo degradado y 7 experimentos de recuperación, 26 verificaciones (PR #10) |
| 5 | Alejandro | 14 pruebas de integración, colección Insomnia y Terraform para Azure (PR #11) |
| 5 | Andrea y Manuel | 8 flujos Maestro escritos (PR #12); Ansible + reverse proxy, compose reproducible y Cypress (PR #13) |
| 6 | Manuel | Semgrep, SonarQube, ZAP, axe, k6 y protocolo de usabilidad (PR #14) |
| 7 | Andrea | Correcciones de Sonar y ZAP, regresión completa, guía de validación (PR #15) |
| 7-8 | Andrea | Primera ejecución real de la app Android en emulador, corrección de 3 defectos, los 8 flujos Maestro ejecutados 8/8 (PR #16) |
| 8 | Alejandro | Cobertura real con `dotnet-coverage`, Jenkinsfile, 22 pruebas de integración, documentos formales (PR #17) |
| 8 | Alejandro | Jenkins local ejecutado de punta a punta, k6 a escala completa, resultados en el informe final |

Cifras totales: **77 pruebas automatizadas en verde** (6 unitarias, 22 integración, 26 recuperación, 15 Cypress, 8 Maestro), 14 riesgos, 95 casos de prueba trazables a requisitos, 15 defectos registrados, 17 Pull Requests mergeados.

## 3. Rúbrica de 25 puntos: qué mostrar y cómo está

| Criterio | Pts | Qué mostrar | Estado |
|---|---|---|---|
| Plan de pruebas, arquitectura, riesgos y trazabilidad | 3,0 | `plan-pruebas.pdf` (20 secciones), `arquitectura.pdf`, `matriz-riesgos.xlsx`, `casos-prueba.xlsx` con requisito, PR, prueba, resultado, evidencia y defecto | Completo |
| GitHub: ramas, PR, Issues y versionamiento | 1,0 | Ramas por persona, 17 PR con autoevaluación, historial con identidades distintas, `main` protegida | Falta el tag `v0.9.0-rc1`; se trabajó con PR en vez de Issues (dilo) |
| Cloud + IaC | 2,5 | `infrastructure/terraform`, `terraform validate` dentro del pipeline | Parcial: sin `apply` (Azure sin confirmar) |
| Docker + Ansible + reproducibilidad | 2,0 | `docker compose up -d --wait` con healthchecks, contenedores sin privilegios, roles de Ansible | Docker completo; Ansible validado en sintaxis, sin ejecutar en un host |
| Jenkins CI/CD + Quality Gates | 2,0 | Build #11, `Jenkinsfile`, JCasC, Quality Gate de Sonar | Completo (falta que Dependency-Check lo deje en verde) |
| SonarQube + Fortify + Dependency-Check + npm audit | 2,5 | Sonar OK; Semgrep como sustituto de Fortify; Dependency-Check; npm audit 0 | Fortify sustituido (pedir excepción al docente); Dependency-Check en curso |
| API + Insomnia + Redis + RabbitMQ | 2,0 | 22 pruebas de integración, colección Insomnia, experimentos E4 a E6 | Completo |
| Cypress | 1,5 | 15 pruebas en `frontend/cypress` | Completo |
| Maestro | 1,5 | 8 flujos, `tests/maestro/run-all.sh` | Completo |
| ZAP | 1,5 | Informes web y API de `reports/zap` | Completo |
| Rendimiento | 1,5 | k6 load, stress y spike con umbrales declarados | Completo (a escala del plan) |
| Usabilidad + accesibilidad | 1,5 | axe sobre 4 pantallas sin hallazgos críticos; resultados de 5 personas | Accesibilidad completa; **usabilidad pendiente de personas** |
| Gestión de defectos | 1,0 | Hoja Defectos de `casos-prueba.xlsx` | Completo |
| Exposición y defensa | 2,0 | Esta guía | Depende de ti |

## 4. Defensa práctica: qué te pueden pedir y cómo hacerlo

### 4.1 «Ejecuta un pipeline»
Un build completo dura unos 25 minutos. Lánzalo antes de empezar o al inicio de la exposición y narra las etapas mientras avanzan; muestra el build #11 ya terminado como respaldo.
En Jenkins: job `fleettrack` → **Build Now** (o, si pide parámetros, Build with Parameters). Explica: checkout, dependencias, lint, unitarias, SonarQube con cobertura, npm audit, Dependency-Check, SAST, build de imágenes, despliegue QA, API, Cypress, recuperación, Maestro, ZAP, rendimiento, criterios de salida y aprobación.

### 4.2 «Provoca una prueba fallida»
Rompe una aserción a propósito, muestra que falla y revierte:
```bash
sed -i 's/HttpStatusCode.Created, created.StatusCode/HttpStatusCode.OK, created.StatusCode/' backend/tests/FleetTrack.Api.IntegrationTests/ManagementTests.cs
dotnet test backend/tests/FleetTrack.Api.IntegrationTests/FleetTrack.Api.IntegrationTests.csproj    # falla
git checkout backend/tests/FleetTrack.Api.IntegrationTests/ManagementTests.cs                       # revertir
```
Di qué hace el pipeline en ese caso: la etapa falla, el build queda en rojo y no se aprueba ni se despliega.

### 4.3 «Detén Redis»
```bash
docker compose stop redis
```
En el dashboard recarga: aparece el aviso **«Modo degradado»** y los datos siguen saliendo (de PostgreSQL). Muestra la cabecera:
```bash
source tests/recovery/common.sh; curl -s -i $API/api/tracking/latest -H "Authorization: Bearer $(sup_token)" | grep -i x-redis-status   # unavailable
docker compose start redis
```
Explica: Redis es solo una caché; la API degrada a PostgreSQL y reconstruye la caché al volver.

### 4.4 «Detén el consumidor (o RabbitMQ)»
```bash
docker compose stop consumer
source tests/recovery/common.sh; D=$(new_route 1 | grep -o '"deliveries":\[{"id":"[^"]*"' | cut -d'"' -f6); curl -s -o /dev/null -w "%{http_code}\n" -X POST $API/api/deliveries/$D/start -H "Authorization: Bearer $(drv_token)"   # 200
```
La API sigue respondiendo; en RabbitMQ (pestaña Queues) `fleettrack.events.q` acumula mensajes. Luego:
```bash
docker compose start consumer      # la cola se vacía sola
```
Para el broker: `docker compose stop rabbitmq`, hacer la misma operación (responde 200, el outbox acumula) y `docker compose start rabbitmq` (el outbox se vacía). Toda la batería: `bash tests/recovery/run-all.sh` (26 verificaciones, unos 4 minutos).

### 4.5 «Muestra un hallazgo de ZAP»
Abre `reports/zap/zap-web-report-semana7.html`. Quedan 3 avisos informativos aceptados (comentarios dentro de librerías, assets estáticos cacheables, aviso informativo). Cuenta el antes y el después: en la semana 6 eran 9 avisos web y 1 de API; se corrigieron agregando cabeceras de seguridad en nginx y en la API. Para correrlo en vivo (1 minuto):
```bash
MSYS_NO_PATHCONV=1 docker run --rm --network fleettrack_default -v "$(pwd -W)/reports/zap:/zap/wrk:rw" zaproxy/zap-stable zap-baseline.py -t http://web:8080 -r demo.html -I
```

### 4.6 «Explica el p95»
El percentil 95 es el tiempo por debajo del cual responde el 95 % de las peticiones: el 5 % más lento queda fuera. Sirve más que el promedio porque ignora la mayoría rápida y muestra la cola lenta. Nuestro umbral era p95 < 500 ms. Medimos 20 ms con 100 usuarios, 23 ms con 300 y 29,7 ms en el pico. El máximo fue 675 ms en un caso aislado, y por eso el p95 es la métrica correcta y no el máximo. Muestra `reports/performance/load-completo.json`.

### 4.7 «Recrea la infraestructura»
Ambiente local reproducible (borra los datos de prueba, se vuelven a sembrar):
```bash
docker compose down -v && docker compose up -d --build --wait
```
Nube: `infrastructure/terraform` define VM, red e identidad. Muestra `terraform validate` pasando (contenedor `hashicorp/terraform`) y di sin rodeos que no se ejecutó `apply` porque falta confirmar Azure for Students. Ansible: `ansible-playbook --syntax-check -i ansible/inventory.example.ini ansible/site.yml`.

### 4.8 «Justifica el GO / NO-GO»
Ver sección 6.

## 5. Lo que hay que decir con honestidad (te lo van a preguntar)

- **Fortify:** no hay licencia; se usó **Semgrep** con las mismas categorías (SAST, seguridad, secretos) y 0 hallazgos en backend, frontend y mobile. Pedir al docente una excepción académica expresa.
- **Azure:** Terraform y Ansible están listos y validados en sintaxis, sin `apply`, porque la suscripción no está confirmada.
- **Issues de GitHub:** se trabajó con PR y autoevaluación; los casos de prueba enlazan al PR de entrega.
- **Cobertura:** 82,1 % medida solo sobre el backend (xUnit); frontend y mobile se validan con Cypress, axe y Maestro, no con cobertura de líneas. El umbral del gate no se cambió.
- **Rendimiento:** todos los usuarios virtuales usan el mismo conductor de prueba y k6 corre en la misma máquina que el sistema; es válido para ese equipo.
- **Windows:** el Control de aplicaciones bloqueó a veces binarios recién compilados; no se tocó la seguridad, se movió el espacio de trabajo de Jenkins y Semgrep corre en Docker.
- **Defecto abierto:** el mensaje «guardada» en la app no se limpia tras sincronizar (cosmético).
- **App Android:** probada en emulador, no en teléfono físico.
- **Jenkins:** controlador y agentes son la misma máquina.

## 6. Cierre: decisión GO / NO-GO

Qué decir: «Todas las verificaciones automatizadas pasan (77 de 77), el Quality Gate está aprobado sin cambiar umbrales, no hay defectos críticos ni altos abiertos, y el rendimiento cumple con holgura. Aun así, el plan establece que un reporte obligatorio ausente deja la ejecución incompleta, así que la decisión es **NO-GO provisional** hasta cerrar tres evidencias: Dependency-Check, la usabilidad con 5 personas y la aprobación de los riesgos residuales. El candidato `v0.9.0-rc1` es apto en funcionalidad.»

Si Dependency-Check queda sin hallazgos críticos y la usabilidad llega al 80 %, la decisión pasa a **GO** con la autorización de Alejandro en Jenkins. Tabla de criterios de salida en la sección 8 de `informe-final.pdf`.

## 7. Preguntas probables

| Pregunta | Respuesta corta |
|---|---|
| ¿Por qué un outbox? | El cambio de negocio y el evento se guardan en la misma transacción: si RabbitMQ cae no se pierde nada y se publica después. |
| ¿Cómo evitan duplicados? | Bloqueo de fila en `start/complete/fail`, `eventId` único en tracking y `MessageId` como clave del consumidor. |
| ¿Qué pasa si Redis cae? | Es caché: la API lee PostgreSQL y avisa con `X-Redis-Status`. |
| ¿Qué hace la app sin red? | Guarda cada operación en SQLite y la reenvía en orden al reconectar; probado en emulador con 1 solo evento `DeliveryCompleted`. |
| ¿Por qué no hay más cobertura en frontend/mobile? | La interfaz se valida con Cypress (15), axe y Maestro (8); se declaró en el informe. |
| ¿Qué defecto fue el más importante? | Android bloqueaba HTTP (login imposible) y la cámara no abría; los dos aparecieron solo al ejecutar la app real. |
| ¿Cómo se bloquea un despliegue malo? | Quality Gate, criterios de salida con reportes obligatorios y aprobación de Alejandro. |
| ¿Quién aprueba? | Solo Alejandro, con autoevaluación registrada en sus propios PR. |

## 8. Checklist final (marca antes de entrar)

- [ ] Docker, SonarQube, Jenkins y emulador arriba; 6 servicios en healthy.
- [ ] Ruta creada para el conductor de prueba y app con pantalla encendida.
- [ ] Pestañas abiertas (tabla de la sección 0) y terminal en la raíz del repo.
- [ ] Rama `main` actualizada y PR #18 de esta etapa mergeado (si lo hay).
- [ ] Reporte de Dependency-Check generado y revisado; usabilidad registrada en el CSV.
- [ ] Informe final regenerado con esos dos resultados (`node docs/build-pdfs.mjs` y `python docs/src/build-xlsx.py`).
- [ ] Cada integrante sabe su bloque y puede explicar la arquitectura completa.
