# Diseño de la interfaz (web y móvil)

Rediseño de las dos interfaces de FleetTrack con un mismo lenguaje visual. Es una **pantalla de trabajo** (el supervisor y el conductor vienen a hacer una tarea), así que el diseño prioriza claridad, estados visibles y controles conocidos sobre el adorno.

## Dirección

- **Marca:** índigo `#512BD4`, el mismo que ya usaba la app móvil, para que web y móvil se reconozcan como un solo producto.
- **Estructura web:** barra lateral índigo con la navegación y el usuario; contenido claro con tarjetas. En pantallas angostas la barra pasa a ser una cabecera con la navegación deslizable.
- **Estructura móvil:** encabezado índigo con el conductor y el estado de la ruta; una tarjeta por entrega con su estado y las cuatro acciones.
- **Tema claro y oscuro** automáticos según el sistema, en web y móvil.
- **Un solo movimiento firma:** el dashboard muestra a cada conductor como *En vivo*, *Reciente* o *Inactivo* según cuándo reportó, con un punto que late en los que están en vivo, una barra que cuenta los 5 s hasta la próxima actualización y una pequeña gráfica vectorial con las posiciones relativas (sin mapa base: no pretende ser un mapa).

## Sistema

| Elemento | Web | Móvil |
|---|---|---|
| Colores | tokens CSS en `frontend/src/index.css` | `mobile/Resources/Styles/Colors.xaml` |
| Componentes | `.card`, `.chip`, `.notice`, `.rows`, `.stops`… | `Components.xaml` (`Card`, `FieldBox`, `PrimaryAction`, `QuietAction`, `DangerAction`, `Banner*`) |
| Iconos | SVG propios en `frontend/src/components/Icon.tsx`, un solo trazo | sin iconos (solo texto) |
| Tipografía | pila del sistema, escala de 1,125 | Open Sans (ya incluida) |
| Estados | vacío con explicación, carga con esqueleto, error y éxito con aviso | avisos de color por estado, indicador de carga solo mientras trabaja |

Estados de entrega: *Pending* gris, *InProgress* ámbar, *Completed* verde, *Failed* rojo. El color nunca va solo: siempre acompaña a un texto.

## Dinamismo

Web: indicador de conexión y «Actualizado hace N s», resalte de la fila cuando llega una posición nueva, búsqueda en las listas de clientes, vehículos y conductores, contador de registros, aviso de éxito que se retira solo, la lista muestra primero lo último que agregaste y movimiento sutil al entrar a cada pantalla (solo desplazamiento; respeta «reducir movimiento»).

Móvil: deslizar hacia abajo para actualizar (reenvía lo pendiente y vuelve a consultar la ruta), avisos que aparecen solo cuando tienen texto y se limpian solos, y estado de cada entrega con su color.

## Accesibilidad

- Contraste verificado con axe-core (WCAG 2 A y AA más buenas prácticas) en **tema claro y oscuro**, sin violaciones en las pantallas revisadas.
- Enlace «Saltar al contenido», regiones `aside`, `nav` y `main`, foco visible, controles con etiqueta y errores con `role="alert"`.
- `lang="es"` y título de página con nombre.

Hallazgos de axe durante el rediseño, ya corregidos: en tema oscuro el botón principal tenía un contraste de 4,09:1 y las etiquetas y avatares de marca de 3,18:1 (ahora 5,86:1 y 7,31:1); y la animación de entrada bajaba la opacidad del texto, lo que podía dar fallos intermitentes si se medía a mitad de animación (ahora solo se desplaza).

## Lo que no cambió, a propósito

Los textos, IDs y botones que usan las pruebas se conservaron: etiquetas de los formularios, nombres del menú, mensajes de error y de éxito, y en el móvil «Correo», «Contrasena», «Ingresar», «Iniciar», «Evidencia», «Finalizar», «Fallida» y los avisos. Por eso las 15 pruebas de Cypress y los 8 flujos de Maestro siguen pasando.

## Limitaciones conocidas

- El texto de la interfaz sigue sin acentos (convención que ya tenía el proyecto y de la que dependen las pruebas).
- La barra de estado de Android conserva el morado de marca (`#512BD4`) y no el índigo del encabezado; es una diferencia mínima de tono.
- El móvil se verificó en un emulador Android 15 y no en un teléfono físico.
- Las posiciones relativas del dashboard no son un mapa: muestran distancias entre conductores.
