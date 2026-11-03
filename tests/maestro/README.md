# Maestro — flujos móviles (D05)

8 flujos que cubren login, ruta, inicio, ubicación, evidencia, desconexión, sincronización y finalización
(sección 9 del plan de trabajo). Cada uno es independiente (`runFlow` encadena los pasos previos necesarios).

## Estado de esta entrega (semana 5)

Los flujos están escritos contra los textos reales de `mobile/Pages/RoutePage.xaml(.cs)` y `LoginPage.xaml(.cs)`,
pero **no se ejecutaron**: esta máquina no tiene Maestro CLI instalado ni un emulador/dispositivo Android
configurado (la app móvil nunca se probó en emulador — ver `docs/semana-4-mensajeria-y-recuperacion.md`, límites
conocidos). Validarlos requiere:

```bash
# 1. Instalar Maestro CLI (https://maestro.mobi)
curl -Ls "https://get.maestro.mobi" | bash

# 2. Tener un emulador Android corriendo con la app FleetTrack.Mobile instalada y la API accesible en 10.0.2.2:8080
#    (docker compose up -d desde la raiz del repo)

# 3. Ejecutar cada flujo
maestro test tests/maestro/01_login.yaml
# ...o la suite completa
maestro test tests/maestro/
```

## Limitación de selectores

Los botones de `RoutePage` ("Iniciar", "Evidencia", "Finalizar", "Fallida") se repiten una vez por entrega de la
ruta; los flujos asumen una ruta de una sola entrega (ver seed de `SeedData.cs`) para que `tapOn` resuelva al
primer/único botón sin ambigüedad. Con varias entregas haría falta un selector por fila (`index` o un
`AutomationId` por entrega), pendiente para cuando se automatice contra una ruta real de varias paradas.

## Desconexión / reconexión

Maestro no controla el estado de red del dispositivo. Los flujos 06 y 07 asumen que el agente Jenkins con Android
(D05/D06, también pendiente de montar) desconecta y reconecta la red alrededor de esos pasos vía `adb shell svc
wifi|data disable|enable`, no vía comandos de Maestro.
