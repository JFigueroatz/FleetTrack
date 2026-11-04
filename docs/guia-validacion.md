# Guía para validar FleetTrack por tu cuenta

Todo se corre desde la raíz del repo (`FleetTrack/`). Requisitos ya instalados en esta máquina: Docker Desktop, .NET 10 SDK, Node 24, k6. Credenciales de prueba (del seed, no son reales): supervisor `supervisor@fleettrack.test` / `Supervisor123!`, conductor `conductor.prueba@fleettrack.test` / `Conductor123!`.

## 0. Arrancar

```bash
docker compose up -d --build
docker compose ps          # los 6 servicios en "healthy" (consumer solo "Up")
```
Si falta `.env`: `cp .env.example .env` y completar `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD`, `JWT_SECRET` con cualquier valor de prueba.

## 1. App web (se puede validar completa ahora)

Abre http://localhost:3000 e inicia sesión como supervisor.

| Paso | Qué haces | Qué debe pasar |
|---|---|---|
| 1 | Login con contraseña incorrecta | Mensaje "Correo o contrasena incorrectos.", no navega |
| 2 | Clientes → agrega uno | Aparece en la lista |
| 3 | Vehículos → agrega uno; repite con placa `P-000TEST` | El duplicado muestra "Ya existe un vehiculo con esa placa." |
| 4 | Conductores → agrega uno | Aparece en la lista |
| 5 | Rutas → elige conductor, vehículo y clientes (en orden) → Crear ruta | "Ruta creada y asignada." |
| 6 | Dashboard | Tabla con la última posición por conductor, se refresca cada 5 s |
| 7 | Con `docker compose stop redis` y recargando el dashboard | Aviso "Modo degradado…" y los datos siguen saliendo (de PostgreSQL). Luego `docker compose start redis` |

Para ver posiciones sin la app móvil, simula un conductor con la API:

```bash
source tests/recovery/common.sh
T=$(drv_token)
curl -X POST $API/api/tracking -H "Authorization: Bearer $T" -H 'Content-Type: application/json' -d '{"latitude":14.63,"longitude":-90.51}'
```

## 2. Mensajería y datos

- **RabbitMQ** http://localhost:15672 (usuario `fleettrack`, contraseña en `RABBITMQ_PASSWORD` del `.env`): `fleettrack.events.q` en 0 pendientes; `fleettrack.events.dead` conserva los mensajes envenenados de los experimentos. Connections: 2 (API y consumer).
- **PostgreSQL** (puerto 5433 en el host): `docker compose exec postgres psql -U fleettrack -d fleettrack -c 'select "Type", count(*) from "EventLogs" group by 1'`
- **API**: http://localhost:8080/health responde 200.

## 3. Pruebas automatizadas (con el stack arriba)

```bash
cd backend && dotnet test FleetTrack.slnx                 # 6 unitarias + 14 de integración
bash tests/recovery/run-all.sh                            # 26 verificaciones (concurrencia, Redis/broker/consumer caídos, reenvíos)
cd frontend && CYPRESS_BASE_URL=http://localhost:3000 npx cypress run   # 15 pruebas (smoke, flujo crítico, regresión, negativo, accesibilidad)
cd frontend && CYPRESS_BASE_URL=http://localhost:3000 npx cypress open  # lo mismo, con ventana para verlo en vivo
```
Total esperado: 61 en verde.

## 4. Seguridad y calidad

| Herramienta | Comando | Esperado |
|---|---|---|
| Semgrep | `semgrep --config p/csharp --config p/security-audit --config p/secrets backend/ mobile/` | 0 hallazgos |
| npm audit | `cd frontend && npm audit` | 0 vulnerabilidades |
| NuGet | `cd backend && dotnet list package --vulnerable --include-transitive` | ninguna |
| ZAP | `docker run --rm --network fleettrack_default zaproxy/zap-stable zap-baseline.py -t http://web:8080 -I` (en Git Bash anteponer `MSYS_NO_PATHCONV=1`) | 0 FAIL, 3 WARN aceptados |
| SonarQube | `docker start sonarqube` → http://localhost:9000 (admin / `SonarFleetTrack2026!`, esperar ~2 min) | 0 bugs, 0 vulnerabilidades; gate en ERROR solo por cobertura |

Los reportes ya generados están en `reports/` (ZAP: abre los `.html`).

## 5. Lo que NO se ejecutó y cómo validarlo

### 5.1 App Android (la más importante: nunca corrió fuera de compilación)

**Actualización:** el emulador ya está instalado (AVD `fleettrack`, Android 15) y la app se validó en él; los resultados y los comandos para repetirlo están en `docs/validacion-android-emulador.md`. Para abrirlo: `"C:/Users/jfigu/AppData/Local/Android/Sdk/emulator/emulator.exe" -avd fleettrack`. Lo que sigue describe cómo se llegó ahí o cómo hacerlo en otra máquina; originalmente el SDK solo tenía `adb`, sin emulador ni imágenes. Dos caminos:

**A. Teléfono Android físico (el más rápido).** Activa Opciones de desarrollador → Depuración USB, conéctalo y comprueba `adb devices`. Un teléfono no alcanza `10.0.2.2` (alias solo del emulador): pon la IP de tu PC en `mobile/Services/AppConfig.cs` (`http://<IP-de-tu-PC>:8080`), agrégala también a `mobile/Platforms/Android/Resources/xml/network_security_config.xml`, y permite el puerto 8080 en el firewall de Windows. PC y teléfono deben estar en la misma red.

**B. Emulador.** Instálalo con:
```bash
SDKM="C:/Users/jfigu/AppData/Local/Android/Sdk/cmdline-tools/latest/bin/sdkmanager.bat"
"$SDKM" "emulator" "system-images;android-35;google_apis;x86_64"
"C:/Users/jfigu/AppData/Local/Android/Sdk/cmdline-tools/latest/bin/avdmanager.bat" create avd -n fleettrack -k "system-images;android-35;google_apis;x86_64"
```
(Alternativa con clics: Android Studio → Device Manager. Necesita virtualización activada en BIOS. Luego arráncalo con `emulator -avd fleettrack`, desde `Sdk/emulator`.) Con el emulador, `AppConfig` ya apunta bien a `10.0.2.2`.

**Instalar y probar (ambos caminos):**
```bash
dotnet build mobile -f net10.0-android -t:Run -p:AndroidSdkDirectory="C:\Users\jfigu\AppData\Local\Android\Sdk"
```
Antes, crea una ruta desde el dashboard web para `Conductor de Prueba` (paso 5 de la sección 1). Recorrido en la app:

1. Login con el conductor de prueba → ves el nombre y la ruta.
2. **Iniciar** una entrega → "Entrega iniciada." y el estado cambia a InProgress; en el dashboard el conductor aparece tras ~15 s (la ubicación se envía sola; en emulador fija una ubicación desde Extended controls → Location).
3. **Evidencia** → saca la foto (el emulador ofrece una cámara virtual) → "Evidencia guardada."
4. **Modo avión** (o `adb shell svc wifi disable; adb shell svc data disable`), toca **Finalizar** → aparece "Sin conexion: 1 operacion(es) pendiente(s)…".
5. Reactiva la red → en segundos el aviso desaparece y la entrega queda Completed **una sola vez** (comprueba con `select count(*) from "EventLogs" where "Type"='DeliveryCompleted'`).
6. **Fallida** → pide motivo → la entrega queda Failed.

Aviso honesto: la corrección de HTTP sin cifrar (Android 9+) se compiló pero no se pudo probar en ejecución; si el login falla con un error de "cleartext", es lo primero que hay que revisar.

### 5.2 Maestro (8 flujos móviles)

Necesita lo de 5.1 funcionando. Instala Maestro (https://maestro.mobi, requiere Java 17+; ya tienes JDK 21) y con el dispositivo/emulador conectado:
```bash
maestro test tests/maestro/01_login.yaml      # un flujo
maestro test tests/maestro/                   # los 8
```
Los flujos 06 y 07 (desconexión/sincronización) necesitan que apagues y reactives la red a mano entre ellos (`adb shell svc wifi …`). Los flujos asumen una ruta de una sola entrega. Es probable que haya que ajustar algún texto o tiempo al correrlos por primera vez.

### 5.3 Terraform (Azure)
Sin crear nada: instala Terraform (`winget install Hashicorp.Terraform`) y valida solo la sintaxis:
```bash
cd infrastructure/terraform && terraform init -backend=false && terraform validate
```
Crear recursos reales (`terraform plan/apply`) exige tener la suscripción Azure for Students confirmada y define `TF_VAR_azure_subscription_id` y `allowed_ssh_cidr`. Revisa `terraform plan` antes de cualquier `apply`: crea una VM con costo.

### 5.4 Ansible
No corre en Windows nativo; usa WSL (necesitas una distro Ubuntu: `wsl --install -d Ubuntu`) y `pip install ansible`:
```bash
ansible-playbook --syntax-check -i ansible/inventory.example.ini ansible/site.yml
```
Ejecutarlo de verdad requiere una VM Ubuntu real (la de Terraform, o una local) y pasar los secretos con `-e`.

### 5.5 Dependency-Check
Pide la API key gratis en https://nvd.nist.gov/developers/request-an-api-key y corre:
```bash
MSYS_NO_PATHCONV=1 docker run --rm -v "$(pwd):/src" -v "$(pwd)/reports/dependency-check:/report" owasp/dependency-check:latest --scan /src --nvdApiKey <TU_KEY> --format HTML --out /report --exclude "**/node_modules/**" --exclude "**/bin/**" --exclude "**/obj/**"
```
La primera descarga de la base de datos tarda un buen rato.

### 5.6 k6 a escala completa
```bash
k6 run tests/k6/load.js     # 100 VUs, 10 min; umbrales: p95 < 500 ms, errores < 1 %
k6 run tests/k6/stress.js   # ~12 min, hasta 300 VUs
k6 run tests/k6/spike.js    # ~3 min, salto a 300 VUs
```
Cierra otras aplicaciones pesadas antes: la máquina es también quien genera la carga, y eso distorsiona el resultado.

### 5.7 Insomnia
Importa `tests/insomnia/FleetTrack.insomnia.json` (Application → Import). La colección usa variables que debes llenar a mano en el entorno "Local": `supervisorToken` y `driverToken` (cópialos de la respuesta de las peticiones de login), y `driverId`, `vehicleId`, `customerId`, `deliveryId` (de `GET /api/drivers`, `/vehicles`, `/customers` y de la respuesta de `POST /api/routes`). Las peticiones de evidencia necesitan elegir un archivo de imagen al enviarlas.

### 5.8 Usabilidad
Sigue `docs/usabilidad/protocolo-usabilidad.md` con 5 personas y anota en `docs/usabilidad/plantilla-resultados.csv`.

### 5.9 Jenkins
El `Jenkinsfile` aún no existe (semana 8).
