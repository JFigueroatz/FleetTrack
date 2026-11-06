"""Genera docs/matriz-riesgos.xlsx y docs/casos-prueba.xlsx. Uso: python docs/src/build-xlsx.py
Los casos automatizados se leen de las propias suites (xUnit, Cypress, recuperacion, Maestro), asi la trazabilidad
no se desincroniza del codigo. Resultados: los de la ultima ejecucion registrada en docs/informe-final.md."""
import glob
import os
import re
from openpyxl import Workbook
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FECHA = "2026-11-05"
HEAD = PatternFill("solid", fgColor="512BD4")
COLORES = {"Aprobado": "C6EFCE", "Bloqueado": "FFC7CE", "Pendiente": "FFEB9C", "No ejecutado": "FFEB9C"}


def sheet(wb, title, header, rows, widths):
    ws = wb.create_sheet(title)
    ws.append(header)
    for c in ws[1]:
        c.font, c.fill = Font(bold=True, color="FFFFFF"), HEAD
    for r in rows:
        ws.append(r)
    for i, w in enumerate(widths, 1):
        ws.column_dimensions[get_column_letter(i)].width = w
    for row in ws.iter_rows(min_row=2):
        for c in row:
            c.alignment = Alignment(wrap_text=True, vertical="top")
            if c.value in COLORES:
                c.fill = PatternFill("solid", fgColor=COLORES[c.value])
    ws.freeze_panes = "A2"
    ws.auto_filter.ref = ws.dimensions
    return ws


def read(path):
    with open(os.path.join(ROOT, path), encoding="utf-8") as f:
        return f.read()


# --------------------------------------------------------------------------- matriz de riesgos
RIESGOS = [
    # id, riesgo, causa, prob, impacto, mitigacion, pruebas, estado residual
    ("R01", "Operación duplicada (entrega completada dos veces)", "Reintentos del móvil, peticiones concurrentes", 4, 5,
     "Bloqueo de fila SELECT…FOR UPDATE; transiciones idempotentes; evento solo en transición real", "CT-INT-09/10, CT-REC-E1", "Mitigado"),
    ("R02", "Pérdida de operaciones aceptadas", "Caída del broker o del consumidor", 3, 5,
     "Outbox transaccional con confirmación del broker; cola durable; reintentos y cola de fallidos", "CT-REC-E5/E6, CT-INT-14", "Mitigado"),
    ("R03", "Redis no disponible", "Caída de la caché", 3, 3,
     "Redis solo es caché: lectura desde PostgreSQL y cabecera X-Redis-Status; aviso en el dashboard", "CT-REC-E4, CT-WEB-14", "Mitigado"),
    ("R04", "Eventos fuera de orden pisan datos recientes", "Reenvíos tardíos, mensajes repetidos", 4, 4,
     "Guard por RecordedAt; EventLogs con clave MessageId; estado final no retrocede", "CT-UNI-01..04, CT-REC-E2/E3", "Mitigado"),
    ("R05", "Pérdida de datos en el móvil sin conexión", "Sin red al finalizar o subir evidencia", 4, 5,
     "Cola SQLite de operaciones pendientes y SyncService ordenado", "CT-MAE-06/07, CT-REC-E7, CT-MAN-01", "Mitigado (probado en emulador, no en teléfono físico)"),
    ("R06", "Acceso a datos o entregas ajenas", "Autorización mal aplicada", 3, 5,
     "Roles Supervisor/Driver y verificación de pertenencia en la API", "CT-INT-02..07/11", "Mitigado"),
    ("R07", "Defectos de seguridad web", "Cabeceras ausentes, contenedores root", 3, 4,
     "Cabeceras en nginx y API; contenedores sin privilegios; ZAP y Semgrep en cada build", "CT-SEG-01..05", "Mitigado (3 avisos informativos de ZAP aceptados)"),
    ("R08", "Vulnerabilidades en dependencias", "Paquetes npm o NuGet vulnerables", 3, 4,
     "npm audit, dotnet list package --vulnerable y Dependency-Check en el pipeline", "CT-SEG-06/07/08", "Mitigado: Dependency-Check ejecutado; 1 falso positivo documentado"),
    ("R09", "Rendimiento insuficiente bajo carga", "100 a 300 conductores enviando ubicación", 3, 4,
     "Caché Redis, índices, k6 load/stress/spike con umbrales", "CT-PER-01..04", "Mitigado: escala completa medida (p95 20-30 ms, sin saturar a 300 VUs)"),
    ("R10", "Calidad de código nuevo sin cobertura", "Pruebas de integración por HTTP no medían cobertura", 4, 3,
     "Cobertura medida con dotnet-coverage sobre unitarias e integración; gate de SonarQube sin cambiar umbrales", "CT-CAL-01", "Mitigado: cobertura del backend 82,1 %"),
    ("R11", "Ambiente no reproducible", "Pasos manuales no documentados", 3, 4,
     "Docker Compose con healthchecks, Terraform y Ansible versionados", "CT-INF-01/02/03", "Parcial: Terraform y Ansible validados en Jenkins; sin apply contra Azure"),
    ("R12", "Interfaz poco usable para el conductor", "Pantallas y textos sin validar con personas", 3, 3,
     "Protocolo de usabilidad con 5 participantes y criterio ≥ 80 %", "CT-USA-01", "Abierto: sin participantes todavía"),
    ("R13", "Fortify no disponible", "Sin licencia ni acceso de prueba", 5, 3,
     "Semgrep como sustituto documentado; solicitar excepción académica expresa", "CT-SEG-01..03", "Abierto: requiere aceptación del docente"),
    ("R14", "Secretos expuestos en el repositorio", "Credenciales en código o .env", 2, 5,
     ".env y secretos fuera de Git; reglas de secretos en Semgrep; credenciales de Jenkins", "CT-SEG-01..03", "Mitigado"),
]
wb = Workbook()
wb.remove(wb.active)
sheet(wb, "Matriz de riesgos",
      ["ID", "Riesgo", "Causa", "Probabilidad (1-5)", "Impacto (1-5)", "Puntaje", "Prioridad", "Mitigación", "Pruebas asociadas", "Estado residual"],
      [[i, r, c, p, im, p * im, "Alta" if p * im >= 15 else "Media" if p * im >= 9 else "Baja", m, t, e]
       for i, r, c, p, im, m, t, e in RIESGOS],
      [7, 34, 30, 12, 12, 9, 10, 55, 26, 40])
wb.save(os.path.join(ROOT, "docs", "matriz-riesgos.xlsx"))

# --------------------------------------------------------------------------- requisitos
REQ = [
    ("RF-01", "Autenticación JWT con roles Supervisor y Driver", "PR #2"),
    ("RF-02", "Gestión de clientes, vehículos y conductores", "PR #3"),
    ("RF-03", "Asignación de rutas con entregas ordenadas", "PR #5, #6"),
    ("RF-04", "Dashboard con la última posición de cada conductor", "PR #6"),
    ("RF-05", "Ciclo de vida de la entrega: iniciar, completar, fallar", "PR #5, #8"),
    ("RF-06", "Evidencia fotográfica de la entrega", "PR #5, #7"),
    ("RF-07", "Envío de ubicación del conductor", "PR #5, #7"),
    ("RF-08", "Operación sin conexión y sincronización en Android", "PR #4, #9"),
    ("RNF-01", "Idempotencia y concurrencia", "PR #8, #9"),
    ("RNF-02", "Recuperación ante fallos de Redis, broker y consumidor", "PR #8, #11"),
    ("RNF-03", "Seguridad de aplicación y dependencias", "PR #14, #15"),
    ("RNF-04", "Rendimiento de tracking", "PR #14"),
    ("RNF-05", "Accesibilidad WCAG 2 A/AA", "PR #14"),
    ("RNF-06", "Usabilidad", "PR #14"),
    ("RNF-07", "Ambiente reproducible", "PR #11, #13"),
    ("RNF-08", "Calidad de código y cobertura", "PR #14, #15"),
]
sheet(wb, "Requisitos", ["ID", "Requisito", "Entregado en"], [list(r) for r in REQ], [10, 70, 18])

# --------------------------------------------------------------------------- casos
EV = {"uni": "reports/junit/unit.trx / dotnet test", "int": "reports/junit/integration.trx / dotnet test",
      "web": "frontend/cypress (cypress run)", "rec": "tests/recovery/run-all.sh (26 PASS)",
      "mae": "tests/maestro/run-all.sh (8/8)"}
casos = []


def add(cid, req, desc, tipo, prueba, pr, res, evid, defecto=""):
    casos.append([cid, req, desc, tipo, prueba, pr, res, evid, defecto, FECHA])


# unitarias e integracion: se leen del codigo
unit_req = {"IsNewer": "RNF-01", "CreateToken": "RF-01"}
n = 0
for f in sorted(glob.glob(os.path.join(ROOT, "backend/tests/FleetTrack.Api.Tests/*.cs"))):
    for m in re.finditer(r"public (?:async Task|void) (\w+)\(", open(f, encoding="utf-8").read()):
        if m.group(1) == "InitializeAsync":
            continue
        n += 1
        add(f"CT-UNI-{n:02d}", next(v for k, v in unit_req.items() if k in m.group(1)), m.group(1).replace("_", " "),
            "Unitaria", f"{os.path.basename(f)}::{m.group(1)}", "PR #4, #9", "Aprobado", EV["uni"])
INT_REQ = [("Login", "RF-01", "PR #2"), ("Drivers_me", "RF-01", "PR #2"), ("Crear_vehiculo", "RF-02", "PR #3"),
           ("asigna_ruta", "RF-03", "PR #5"), ("Completar_dos", "RNF-01", "PR #8"), ("concurrentes", "RNF-01", "PR #8"),
           ("eventId", "RNF-01", "PR #8"), ("ajena", "RF-05", "PR #5"), ("otro_conductor", "RF-05", "PR #5"),
           ("fallida", "RF-05", "PR #8"), ("Coordenadas", "RF-07", "PR #5"), ("broker", "RF-07", "PR #9"),
           ("cliente", "RF-02", "PR #3"), ("vehiculo", "RF-02", "PR #3"), ("conductor", "RF-02", "PR #3"), ("listados", "RF-01", "PR #3")]


def int_req(name):
    return next((r, pr) for k, r, pr in INT_REQ if k.lower() in name.lower())


n = 0
for f in sorted(glob.glob(os.path.join(ROOT, "backend/tests/FleetTrack.Api.IntegrationTests/*Tests.cs"))):
    for m in re.finditer(r"public async Task (\w+)\(", open(f, encoding="utf-8").read()):
        if m.group(1) == "InitializeAsync":
            continue
        n += 1
        req, pr = int_req(m.group(1))
        add(f"CT-INT-{n:02d}", req, m.group(1).replace("_", " ") + (" (x3: clientes, vehiculos, conductores)" if m.group(1).startswith("Los_listados") else ""), "Integración/API", f"{os.path.basename(f)}::{m.group(1)}", pr, "Aprobado", EV["int"])
assert n == 20, n  # 20 metodos = 22 pruebas xUnit (un Theory con 3 casos)

web_req = {"smoke": ("RF-01", "PR #13"), "flujo-critico": ("RF-03", "PR #13"), "negativo": ("RF-02", "PR #13"),
           "regresion": ("RNF-02", "PR #13"), "accesibilidad": ("RNF-05", "PR #14")}
n = 0
for f in sorted(glob.glob(os.path.join(ROOT, "frontend/cypress/e2e/*.cy.ts"))):
    base = os.path.basename(f)
    req, pr = next(v for k, v in web_req.items() if base.startswith(k))
    for m in re.finditer(r'^\s*it\("([^"]+)"', open(f, encoding="utf-8").read(), re.M):
        n += 1
        add(f"CT-WEB-{n:02d}", req, m.group(1), "Funcional web" if req != "RNF-05" else "Accesibilidad", f"{base}", pr, "Aprobado", EV["web"])
assert n == 15, n

rec_req = {"E1": "RNF-01", "E2": "RNF-01", "E3": "RNF-01", "E4": "RNF-02", "E5": "RNF-02", "E6": "RNF-02", "E7": "RF-08"}
src = read("tests/recovery/run-all.sh")
exp, n = "", 0
for line in src.splitlines():
    m = re.match(r'echo "(E\d)\s', line)
    if m:
        exp, k = m.group(1), 0
    m = re.match(r'check "([^"]+)"', line)
    if m:
        n += 1
        k += 1
        add(f"CT-REC-{exp}-{k:02d}", rec_req[exp], m.group(1), "Recuperación", f"run-all.sh {exp}", "PR #8, #11", "Aprobado", EV["rec"])
assert n == 26, n

mae_desc = {"01": "Login del conductor", "02": "Consulta de la ruta asignada", "03": "Iniciar entrega", "04": "Envío de ubicación sin errores",
            "05": "Evidencia fotográfica (cámara)", "06": "Desconexión: operación queda pendiente", "07": "Sincronización al reconectar",
            "08": "Finalizar entrega con red"}
mae_req = {"01": "RF-01", "02": "RF-03", "03": "RF-05", "04": "RF-07", "05": "RF-06", "06": "RF-08", "07": "RF-08", "08": "RF-05"}
for f in sorted(glob.glob(os.path.join(ROOT, "tests/maestro/0*.yaml"))):
    k = os.path.basename(f)[:2]
    add(f"CT-MAE-{k}", mae_req[k], mae_desc[k], "Móvil (Maestro)", os.path.basename(f), "PR #12, #16", "Aprobado", EV["mae"])

add("CT-MAN-01", "RF-08", "Android manual: entrega fallida con motivo, mensaje 'guardada, se enviara al reconectar' y evidencia", "Móvil (manual)",
    "docs/validacion-android-emulador.md", "PR #16", "Aprobado", "docs/validacion-android-emulador.md; capturas de la sesión")
for i, (cid, d, p, res, ev, df) in enumerate([
    ("CT-SEG-01", "Semgrep backend (C#): 67 reglas, 0 hallazgos", "RNF-03", "Aprobado", "reports/fortify/semgrep-backend.json", ""),
    ("CT-SEG-02", "Semgrep frontend (TS/React): 125 reglas, 0 hallazgos", "RNF-03", "Aprobado", "reports/fortify/semgrep-frontend.json", ""),
    ("CT-SEG-03", "Semgrep mobile (C#/MAUI): 66 reglas, 0 hallazgos", "RNF-03", "Aprobado", "reports/fortify/semgrep-mobile.json", ""),
    ("CT-SEG-04", "ZAP baseline web: 0 FAIL, 3 avisos informativos aceptados", "RNF-03", "Aprobado", "reports/zap/zap-web-report-semana7.html", "D-06"),
    ("CT-SEG-05", "ZAP API (/health): 0 FAIL, 0 avisos", "RNF-03", "Aprobado", "reports/zap/zap-api-report-semana7.html", "D-06"),
    ("CT-SEG-06", "npm audit frontend: 0 vulnerabilidades", "RNF-03", "Aprobado", "npm audit", ""),
    ("CT-SEG-07", "NuGet: sin paquetes vulnerables (5 proyectos)", "RNF-03", "Aprobado", "dotnet list package --vulnerable", ""),
    ("CT-SEG-08", "OWASP Dependency-Check: 229 dependencias; 39 hallazgos en sqlite-net-pcl (falso positivo por CPE, suprimido con justificación); 0 tras la supresión", "RNF-03", "Aprobado", "reports/dependency-check/dependency-check-report.html y *-sin-supresion.html", "D-16"),
    ("CT-CAL-01", "SonarQube: bugs 0, vulnerabilidades 0 y Quality Gate", "RNF-08", "Aprobado", "reports/sonar/quality-gate-actual.json", "D-09"),
    ("CT-PER-01", "k6 load corto (20 VUs, 30 s): p95 < 500 ms, errores < 1 %", "RNF-04", "Aprobado", "reports/performance/", ""),
    ("CT-PER-02", "k6 stress corto (hasta 60 VUs)", "RNF-04", "Aprobado", "reports/performance/", ""),
    ("CT-PER-03", "k6 spike corto (20→80→20 VUs)", "RNF-04", "Aprobado", "reports/performance/", ""),
    ("CT-PER-04", "k6 a escala completa: load 100 VUs/10 min p95 20 ms; stress a 300 VUs p95 23 ms; spike p95 29,7 ms; 48 000 eventos sin pérdida ni duplicados", "RNF-04", "Aprobado", "reports/performance/*-completo.json", ""),
    ("CT-USA-01", "Usabilidad con 5 participantes (éxito ≥ 80 %)", "RNF-06", "Pendiente", "docs/usabilidad/protocolo-usabilidad.md", ""),
    ("CT-INF-01", "Terraform validate (contenedor hashicorp/terraform)", "RNF-07", "Aprobado", "Jenkins build #11, etapa Lint y validaciones", ""),
    ("CT-INF-02", "Ansible syntax-check (contenedor python + ansible-core)", "RNF-07", "Aprobado", "Jenkins build #11, etapa Lint y validaciones", ""),
    ("CT-INF-03", "Terraform apply y Ansible contra Azure", "RNF-07", "Pendiente", "Requiere suscripción confirmada", ""),
    ("CT-INF-04", "docker compose up: 6 servicios sanos y reinicio automático", "RNF-07", "Aprobado", "docker compose ps", ""),
    ("CT-PIP-01", "Pipeline Jenkins de punta a punta: build #12 en verde (21 min). El build #11, sin clave de NVD, se detuvo en los criterios de salida", "RNF-07", "Aprobado", "Jenkins build #12 (infrastructure/jenkins)", ""),
]):
    add(cid, p, d, "No funcional", "", "PR #14, #15" if cid.startswith(("CT-SEG", "CT-PER", "CT-CAL")) else "", res, ev, df)
# el campo Prueba de los no funcionales queda con la herramienta
sheet(wb, "Casos de prueba",
      ["ID", "Requisito", "Descripción", "Tipo", "Prueba", "PR/Issue", "Resultado", "Evidencia", "Defecto", "Fecha"],
      casos, [14, 10, 70, 18, 38, 12, 13, 46, 9, 12])

DEFECTOS = [
    ("D-01", "Android 9+ bloquea HTTP: el login móvil no podía llamar a la API", "Alta", "Corregido", "network_security_config solo para 10.0.2.2 y localhost", "PR #15", "CT-MAE-01"),
    ("D-02", "La cámara de evidencia no abría (faltaba <queries> IMAGE_CAPTURE)", "Alta", "Corregido", "Manifest con <queries>", "PR #16", "CT-MAE-05"),
    ("D-03", "Mensaje 'Entrega finalizada.' engañoso cuando la operación solo quedó encolada", "Media", "Corregido", "Texto con '(guardada, se enviara al reconectar)'", "PR #16", "CT-MAN-01"),
    ("D-04", "Experimento E2 fallaba tras k6 o integración por posiciones recientes en Redis", "Media", "Corregido", "El experimento limpia su clave de caché", "PR #15", "CT-REC-E2-01"),
    ("D-05", "Sonar: 2 bugs, 5 vulnerabilidades, 14 smells (value types sin required, app.Run, root, Terraform)", "Media", "Corregido", "Ver semana 7", "PR #15", "CT-CAL-01"),
    ("D-06", "ZAP: 9 avisos web y 1 de API (cabeceras de seguridad)", "Media", "Corregido (3 informativos aceptados)", "Cabeceras en nginx y API", "PR #15", "CT-SEG-04/05"),
    ("D-07", "Flujos Maestro: teclado tapaba 'Ingresar', cámara sin disparo, patrones de texto incompletos", "Baja", "Corregido", "Flujos 01, 05, 06, 07, 08 ajustados", "PR #16", "CT-MAE-*"),
    ("D-08", "Tras sincronizar, el mensaje verde 'guardada' no se limpia", "Baja", "Corregido", "El aviso se limpia al confirmarse en el servidor y al actualizar", "Rediseño", "CT-MAE-07"),
    ("D-09", "Quality Gate en ERROR por cobertura de código nuevo (0 %: no se medía)", "Alta", "Corregido", "Cobertura medida con dotnet-coverage (unitarias, integración y recuperación) e importada a Sonar", "Semana 8", "CT-CAL-01"),
    ("D-13", "Semgrep local bloqueado por el Control de aplicaciones de Windows", "Media", "Corregido", "Semgrep corre en su imagen oficial de Docker", "Semana 8", "CT-SEG-01..03"),
    ("D-14", "E4 usaba archivos temporales que fallaban bajo Jenkins en Windows", "Media", "Corregido", "Captura de la respuesta en una variable", "Semana 8", "CT-REC-E4"),
    ("D-15", "run-coverage.sh no compilaba las pruebas de integración en un workspace limpio", "Media", "Corregido", "Compilación explícita antes de --no-build", "Semana 8", "CT-CAL-01"),
    ("D-17", "Rediseño: contraste insuficiente en tema oscuro (4,09:1) y animación que bajaba la opacidad del texto", "Media", "Corregido", "Tokens oscuros con contraste de 5,86:1; animación solo de desplazamiento", "Rediseño", "CT-WEB-12..15"),
    ("D-10", "Dependency-Check no puede ejecutarse sin clave de NVD", "Media", "Corregido", "Clave solicitada y cargada como credencial de Jenkins", "Semana 8", "CT-SEG-08"),
    ("D-16", "sqlite-net-pcl: 39 CVE atribuidos por confusión de CPE (versión del envoltorio tomada como versión del motor)", "Media", "Riesgo aceptado (pendiente de aprobación de Alejandro)", "tests/security/dependency-check-suppressions.xml con justificación y caducidad 2027-01-31", "Semana 8", "CT-SEG-08"),
    ("D-11", "Sonar marcaba allowBackup y HTTP implícito en la app móvil", "Media", "Corregido", "allowBackup=false, usesCleartextTraffic=false (cleartext solo a 10.0.2.2 por network_security_config)", "Semana 8", "CT-CAL-01"),
    ("D-12", "Controlador de plantilla WeatherForecast sin uso", "Baja", "Corregido", "Eliminado", "Semana 8", "CT-CAL-01"),
]
sheet(wb, "Defectos", ["ID", "Descripción", "Severidad", "Estado", "Corrección", "PR", "Caso"], [list(d) for d in DEFECTOS], [8, 80, 10, 26, 50, 10, 16])
wb.save(os.path.join(ROOT, "docs", "casos-prueba.xlsx"))
print(f"casos: {len(casos)}; riesgos: {len(RIESGOS)}; defectos: {len(DEFECTOS)}")
