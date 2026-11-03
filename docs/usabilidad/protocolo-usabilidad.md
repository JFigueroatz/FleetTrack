# Protocolo de prueba de usabilidad — dashboard FleetTrack

Según el plan (M07, sección 9): 5 participantes, tareas definidas, objetivo de éxito ≥80% de intentos, registro de
dificultades. **Esta sesión no se ejecutó** — requiere 5 personas reales, que coordina el equipo. Este documento es
el protocolo listo para usar.

## Participantes

5 personas que no hayan usado FleetTrack antes, idealmente con perfil similar al de un supervisor de logística
(no necesita ser técnico). Registrar solo: código de participante (P1–P5), rol/experiencia previa relevante, fecha.
Nunca nombres completos ni datos de contacto en el reporte (ver `docs/usabilidad/plantilla-resultados.csv`).

## Preparación

1. Ambiente QA con el seed de datos corriendo (`docker compose up -d`), supervisor ya logeado o credenciales a la vista: `supervisor@fleettrack.test` / `Supervisor123!`.
2. Grabar pantalla + audio si el participante autoriza (consentimiento verbal, se registra en la hoja de resultados).
3. El facilitador no ayuda salvo que el participante esté completamente bloqueado 2+ minutos — se anota como fallo igual.

## Guion de introducción (leer igual a los 5)

> "Vas a usar un dashboard de logística como si fueras un supervisor de una empresa de transporte. Te voy a pedir
> que completes algunas tareas pensando en voz alta: qué esperas que pase, qué te confunde. No hay respuestas
> incorrectas — si algo no funciona como esperabas, eso es justo lo que necesito que me digas."

## Tareas (orden fijo)

| # | Tarea | Éxito si... |
|---|---|---|
| T1 | Inicia sesión como supervisor. | Entra al dashboard sin pedir ayuda. |
| T2 | Agrega un cliente nuevo con nombre y dirección. | El cliente aparece en la lista sin error. |
| T3 | Agrega un vehículo nuevo con placa y capacidad. | El vehículo aparece en la lista sin error. |
| T4 | Agrega un conductor nuevo. | El conductor aparece en la lista sin error. |
| T5 | Asigna una ruta al conductor que acabas de crear, con el vehículo y cliente que acabas de crear. | La ruta se crea y el mensaje de confirmación aparece. |
| T6 | Sin que nadie te diga cómo, averigua dónde verías la última ubicación conocida de un conductor. | Llega al dashboard de seguimiento sin ayuda. |
| T7 | Intenta crear un vehículo con una placa que ya existe (dale la placa `P-000TEST`). | Entiende por el mensaje de error que la placa ya existe (no por ensayo y error). |

## Registro por tarea

Por cada tarea y participante: **completó sin ayuda / completó con ayuda / no completó**, tiempo aproximado, y una
cita textual si el participante dijo algo relevante ("no sabía que tenía que...", "pensé que esto hacía..."). Usar
`docs/usabilidad/plantilla-resultados.csv`.

## Cierre

3 preguntas abiertas: ¿qué fue lo más confuso?, ¿qué cambiarías primero?, ¿del 1 al 5 qué tan fácil fue usarlo?

## Criterio de éxito del plan

≥80% de los 25 intentos (5 tareas críticas T1–T5 × 5 participantes; T6–T7 se registran pero no cuentan para el
umbral por ser exploratorias) deben completarse sin ayuda. Por debajo de eso, el hallazgo se documenta como defecto
de usabilidad con la tarea, la cita y una propuesta de corrección, igual que un defecto funcional.
