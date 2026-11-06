# Guía para cerrar las evidencias pendientes

Lo que falta para pasar de NO-GO a GO y quién lo hace. Todo se corre desde la raíz del repositorio, en Git Bash.

## 1. Clave de NVD y Dependency-Check

1. Pide la clave (gratis, llega por correo): https://nvd.nist.gov/developers/request-an-api-key
2. Corre el análisis (la primera vez descarga la base de datos y tarda; después usa la caché del volumen):

```bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd -W):/src" -v "$(pwd -W)/reports/dependency-check:/report" \
  -v fleettrack-depcheck-data:/usr/share/dependency-check/data owasp/dependency-check:latest \
  --scan /src --nvdApiKey <TU_CLAVE> --format HTML --format JSON --out /report --failOnCVSS 9 \
  --exclude "**/node_modules/**" --exclude "**/bin/**" --exclude "**/obj/**"
```

3. Revisa `reports/dependency-check/dependency-check-report.html`. Cada hallazgo se corrige (actualizar el paquete), se acepta con justificación o se registra como riesgo.
4. Para que Jenkins también la use, arráncalo con la clave: `NVD_API_KEY=<TU_CLAVE> bash infrastructure/jenkins/start-jenkins.sh`.

## 2. Sesión de usabilidad (5 personas)

1. Levanta el stack (`docker compose up -d`) y deja el dashboard en http://localhost:3000 con el supervisor de prueba (`supervisor@fleettrack.test`).
2. Sigue `docs/usabilidad/protocolo-usabilidad.md`: 7 tareas por persona, sin ayudarles, y mide si cada tarea se completa.
3. Anota una fila por persona y tarea en `docs/usabilidad/plantilla-resultados.csv` (éxito sí/no, tiempo, dificultades).
4. Criterio de éxito: al menos 80 % de los intentos completados. Con el CSV lleno se calcula el porcentaje y se actualiza el informe.

## 3. Jenkins local

```bash
docker start sonarqube                                   # SonarQube debe estar arriba
SONAR_ADMIN_PASSWORD='<contraseña de Sonar, ver docs/guia-validacion.md>' REPO_BRANCH='*/main' bash infrastructure/jenkins/start-jenkins.sh
```

Abre http://localhost:8081, usuario `alejandro`; la contraseña está en `~/jenkins-fleettrack/admin-password.txt`. El job `fleettrack` se crea solo (JCasC). Parámetros: `FULL_PERFORMANCE` para load/stress/spike completos y `DEPLOY_FINAL` (solo en `main`, pide la aprobación de Alejandro).

- Controlador y agentes son este mismo equipo (etiquetas `qa` y `android`), por eso necesita Docker Desktop, el emulador y Maestro.
- Un build completo tarda cerca de una hora (cobertura, Cypress, recuperación, Maestro, ZAP).
- El job clona el repositorio local; para probar un commit sin PR usa `REPO_BRANCH='*/alejandro'`.

## 4. Cierre

Con las secciones 1 a 3 hechas y el pipeline en verde: actualizar `docs/src/informe-final.md` (criterios de salida), regenerar con `node docs/build-pdfs.mjs` y `python docs/src/build-xlsx.py`, y Alejandro crea la etiqueta `v0.9.0-rc1` y autoriza el despliegue.
