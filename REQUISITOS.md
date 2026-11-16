# shareW

Estado: implementación compilada para Windows x64 desde Linux, con SDK 10.0.404 y dependencias locales. Portable generado. Pruebas de funcionamiento en Windows pendientes.

## Objetivo

Aplicación Windows de capturas y anotación local, reutilizando los componentes de ShareX necesarios para conservar su comportamiento. El nombre solicitado es shareW.

## Referencia obligatoria

La versión base autorizada es ShareX 22.4.0. La imagen de ShareX 19.0.2 solo ilustra la barra durante la captura; no determina la versión ni exige reproducir su interfaz antigua. Se dispone del portable y de `ShareX-22.4.0.zip`, cuyo comentario identifica la revisión `08bfff15e748a945944e609aca7faffbe888c7af`.

La barra sobre la pantalla durante la selección es un requisito, no basta con abrir un editor después de capturar. Se debe inventariar cada botón, menú desplegable, ajuste y atajo del módulo original antes de reducir dependencias. No dar por alcanzada la paridad basándose únicamente en los iconos de la imagen.

## Funciones requeridas

- Conservar todas las herramientas locales de selección, dibujo, anotación, efectos y edición de la barra de referencia.
- Conservar ajustes de color, grosor y fuente, selección y manipulación de objetos, y controles de deshacer/rehacer disponibles en el original.
- Mantener selección de región y captura de pantalla y ventana.
- Guardar el resultado localmente y copiarlo al portapapeles mediante acción explícita.
- Mantener los recursos de interfaz y traducciones necesarios.

## Alcance local

Eliminar los módulos de subida y sus referencias, destinos remotos, integraciones de navegador, llamadas a servicios externos, telemetría si existiese, comprobaciones de actualización y descargas en tiempo de ejecución. Revisar las bibliotecas compartidas: su nombre no garantiza que carezcan de operaciones de red.

No renombrar ni parchear el ejecutable original para eludir EPM. La compilación resultante debe ser revisable y presentarse para aprobación de la organización.

## Estrategia

1. Obtener una revisión fija del código fuente, de ShareX 22.4.0: etiqueta v22.4.0, revisión publicada con prefijo 08bfff1. Obtener también los submódulos fijados por esa revisión, si existen.
2. Leer las instrucciones del repositorio e inventariar el módulo de captura y sus dependencias.
3. Crear una aplicación anfitriona pequeña que reutilice la selección y anotación originales. Verificar si la barra pertenece a captura, al editor o a ambos antes de elegir componentes.
4. Retirar capacidades de red del conjunto de código distribuido; no basta con ocultar botones o configurar DisableUpload.
5. Preservar los avisos, atribuciones y archivos de licencia de los componentes reutilizados.
6. Compilar para Windows y ejecutar la matriz de aceptación; la compilación cruzada en Linux ya ha terminado y el portable se entrega para pruebas.

## Aceptación pendiente

- Catálogo de herramientas y código de captura conservados; verificar su funcionamiento y sus submenús en Windows. Ver `HERRAMIENTAS.md`.
- Probar cada herramienta sobre la pantalla antes de terminar la captura.
- Verificar guardar y copiar la imagen compuesta, cancelación, deshacer y rehacer.
- Probar varias pantallas, coordenadas negativas y escalado DPI mixto.
- Revisar el árbol de dependencias y las rutas que inician procesos o conexiones.
- Comprobar con observación de red que los flujos probados no generan tráfico; esta prueba por sí sola no demuestra ausencia de todas las capacidades de red.
- Entregar fuente identificada por revisión, instrucciones de compilación, licencias y hash del artefacto para revisión de TI.

## Limitaciones del entorno actual

El SDK Linux 10.0.404 y los 45 paquetes NuGet aportados han permitido restaurar y publicar la aplicación para Windows x64 sin fuentes remotas. El entorno sigue siendo Linux: no se ha ejecutado el portable ni validado visualmente la captura en Windows. Los ZIP originales de ShareX se conservan sin modificar. Ver `VALIDACION.md`.
