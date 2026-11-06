# Semana 8 — Pipeline, cobertura, documentos formales y cierre

Commits de esta semana: identidad de Alejandro (acordado con el usuario). Es quien consolida, aprueba y mergea.

## 1. Cobertura de código y Quality Gate

El gate estaba en ERROR solo porque la cobertura de código nuevo era 0 %: las pruebas de integración pegan por HTTP a la API en Docker y nada medía qué código ejecutaban. Ahora se mide de verdad (`tests/coverage/run-coverage.sh`):

| Reporte | Cómo se genera |
|---|---|
| `unit.xml` | `dotnet-coverage` envuelve `dotnet test` de las unitarias |
| `integration.xml` y `consumer-integration.xml` | API y consumidor corren en el host bajo `dotnet-coverage`, contra postgres/redis/rabbitmq de Compose, y se ejecutan las 22 pruebas de integración |
| `recovery.xml` | API en el host bajo cobertura y la suite de recuperación completa (26/26) |

`tests/coverage/sonar-scan.sh` importa los cuatro reportes (Sonar une las líneas cubiertas) y `quality-gate.sh` falla si el gate no es OK.

| Medida | Antes | Ahora |
|---|---|---|
| Quality Gate | ERROR (cobertura de código nuevo 0 %) | **OK** |
| Cobertura del backend | no medida | **82,1 %** |
| Bugs / vulnerabilidades / smells | 0 / 0 / 4 | 0 / 0 / 4 |

**Decisión de alcance, para que Alejandro la acepte o la rechace:** la cobertura se calcula sobre el backend. Quedan excluidos de la *cobertura* (no del análisis de bugs y vulnerabilidades) las migraciones de EF (generadas), el frontend y la app móvil (su interfaz se valida con Cypress, axe y Maestro) y los scripts, documentos e infraestructura. El umbral del gate no se tocó. La condición de cobertura de código nuevo no tiene líneas que medir en este análisis, así que no bloquea; el 82,1 % es la cobertura global del backend.

Cambios que salieron del análisis: `allowBackup=false` y `usesCleartextTraffic=false` en el manifiesto Android (el HTTP a `10.0.2.2` sigue permitido por la `network_security_config`; los 8 flujos Maestro lo confirman), las URLs `http://10.0.2.2` marcadas como intencionales (`NOSONAR` con su justificación), eliminación del controlador de plantilla `WeatherForecast`, y 8 pruebas de integración nuevas de clientes, vehículos y conductores (22 en total).

## 2. Jenkinsfile

Pipeline declarativo en la raíz con la secuencia del plan: checkout → dependencias → lint y validaciones (incluye `terraform validate` y `ansible --syntax-check`) → unitarias → SonarQube con cobertura → npm audit → Dependency-Check → SAST → build de imágenes → despliegue QA → pruebas de API → Cypress → recuperación → Maestro (agente `android`) → ZAP → rendimiento → Quality Gate y criterios de salida → aprobación de Alejandro (`input` con `submitter: alejandro`) → despliegue final con respaldo previo de PostgreSQL y Ansible.

- Ningún paso ignora errores y el build falla si falta un reporte obligatorio.
- Secretos solo por credenciales de Jenkins: `sonar-token`, `nvd-api-key`, `fleettrack-qa-env`, `fleettrack-ansible-extra-vars`.
- Parámetros: `FULL_PERFORMANCE` (load/stress/spike a escala completa) y `DEPLOY_FINAL` (solo en `main`).
- Reportes archivados por build, con `reports/metadata.json` (build, commit, fecha, ambiente).

**Qué se verificó y qué no:** la sintaxis Groovy se compiló con las librerías de la imagen `jenkins/jenkins:lts-jdk21`, y cada comando de las etapas se ejecutó a mano en esta máquina (excepto Dependency-Check, bloqueado por la clave de NVD). **El pipeline no se ejecutó en un servidor Jenkins.** Desviaciones declaradas: Semgrep en lugar de Fortify (sin licencia) y xUnit contra el stack desplegado en lugar de `inso` para la colección de Insomnia.

## 3. Documentos formales

| Documento | Archivo |
|---|---|
| Plan de pruebas (20 secciones) | `docs/plan-pruebas.pdf` |
| Arquitectura | `docs/arquitectura.pdf` |
| Matriz de riesgos (14 riesgos) | `docs/matriz-riesgos.xlsx` |
| Casos de prueba con trazabilidad (95 casos, requisitos, defectos) | `docs/casos-prueba.xlsx` |
| Informe final con GO / NO-GO | `docs/informe-final.pdf` |

Las fuentes están en `docs/src` (Markdown y el generador de los Excel). Los casos automatizados se leen de las propias suites, así que no se desincronizan del código. Regenerar: `node docs/build-pdfs.mjs` y `python docs/src/build-xlsx.py`. Los enlaces a Issues se registran como PR de entrega: el equipo trabajó con Pull Requests y no hay Issues enlazables desde aquí.

## 4. Decisión

**NO-GO provisional**, candidato `v0.9.0-rc1` apto en funcionalidad. Faltan cinco evidencias externas (clave de NVD y Dependency-Check, rendimiento a escala completa, usabilidad con 5 personas, Jenkins montado y pipeline ejecutado, aprobación de riesgos residuales). Detalle en el informe final, sección 8.

## 5. Otras mejoras

`.gitattributes` con LF obligatorio para scripts, Jenkinsfile y YAML (git avisaba de conversiones a CRLF).
