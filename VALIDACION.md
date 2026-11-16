# Validación de shareW

## Comprobaciones realizadas en este entorno

- El SHA-256 del portable aportado coincide con el publicado para 22.4.0.
- El comentario del ZIP de fuentes identifica la revisión `08bfff15e748a945944e609aca7faffbe888c7af`.
- El inventario de fuentes, el grafo de proyectos, el catálogo de herramientas, los recursos XML y los patrones de APIs de red se comprueban con `scripts/verify-source.py`. Consultar su salida en `STATIC-CHECKS.txt`.
- Los 647 archivos C# pasan el análisis de sintaxis con Roslyn 5.0.0.0. Los dos scripts PowerShell pasan su analizador de sintaxis y el verificador PowerShell confirma el inventario.
- Se compiló y ejecutó por separado `LocalFilePolicy` con .NET 6: rechazó rutas vacías, relativas y UNC y aceptó una ruta absoluta local de Linux. Esta prueba no valida unidades mapeadas ni puntos de montaje Windows.
- Se ha verificado el SHA-512 oficial del SDK Linux 10.0.404 y los SHA-256 de los 45 paquetes del feed aportado.
- La restauración local y la publicación Release para Windows x64 han terminado correctamente con .NET 10.0.404, usando `Build-Linux.sh`. El portable incluye los runtimes Microsoft.NETCore.App y Microsoft.WindowsDesktop.App 10.0.15.
- `shareW.exe` es un ejecutable PE32+ GUI x86-64. Están presentes las cinco bibliotecas adaptadas, el runtime y las dependencias de imagen necesarias; se ha comprobado que no se distribuyen los módulos de subida, FTP/SSH, navegador, FFmpeg, herramientas generales o modelos de IA retirados.
- Se han inspeccionado los metadatos de los seis ensamblados construidos: no tienen las referencias de transporte o lanzamiento de procesos prohibidas por la auditoría ni los callbacks/comandos de subida comprobados. Esta inspección no evalúa llamadas indirectas ni el comportamiento interno de dependencias externas.

## Pruebas pendientes en Windows

No marcar esta versión como lista para uso hasta completar estas comprobaciones y registrar el resultado y el hash del ejecutable probado:

- Ejecutar el portable generado y confirmar su carga en Windows. La compilación y revisión del contenido ya se han completado en Linux; no es necesario recompilar para iniciar las pruebas.
- Abrir shareW como usuario normal, capturar, cancelar con Esc y confirmar con Enter sin dejar ventanas superpuestas o imágenes temporales.
- Comprobar cada una de las 19 herramientas de la barra de captura y sus opciones de color, grosor, fuente y estilo. Ver `HERRAMIENTAS.md`.
- Probar mover, cambiar tamaño y eliminar objetos; deshacer y rehacer; varias regiones; captura rápida y lupa.
- Comprobar todas las pantallas y la ventana activa. Probar dos monitores con DPI distintos, un monitor a la izquierda del principal y una sesión RDP si procede.
- Editar y confirmar una captura; cancelar el editor; comprobar que al guardar se usa la imagen compuesta con las anotaciones.
- Guardar PNG y, desde el editor, JPEG y WebP. Sobrescribir una imagen más grande y verificar que el archivo no conserva datos sobrantes.
- Copiar y pegar una imagen en Paint y en una aplicación de oficina, con transparencia y sin ella. Probar portapapeles ocupado y bloqueo temporal del destino.
- Insertar imágenes locales y emojis, pegar anotaciones, usar clonado y borrador inteligente, y fijar una imagen. Comprobar cierre y liberación de memoria tras capturas repetidas.
- Probar rutas UNC y unidades de red mapeadas: deben rechazarse en las rutas de carga y guardado protegidas. Revisar separadamente enlaces, sincronización de carpetas y portapapeles del sistema.
- Confirmar el aviso de atajos ocupados, uso por botones y liberación de atajos al cerrar.
- Confirmar que no aparecen botones de subida/importación URL ni funciona Ctrl+U como subida.
- Observar tráfico del proceso y de sus descendientes al arrancar, capturar, editar, copiar, guardar, fallar y cerrar, con herramientas aprobadas de Windows. Revisar dependencias transitivas. No basar la conclusión únicamente en una prueba sin tráfico.
- Revisar el paquete y su procedencia con TI y tramitar su aprobación EPM. La aplicación no instala excepciones ni cambia controles de seguridad.

## Límites de lo entregado

Las fuentes y XAML se han compilado y se ha generado `shareW.exe`. Las comprobaciones estáticas y de metadatos verifican patrones concretos, no la ausencia de comunicaciones indirectas. No se ha ejecutado el programa en Windows ni observado su tráfico durante el uso. Las pruebas manuales indicadas siguen pendientes.

## Corrección de tema 0.1.1

- El usuario ha informado de que la captura y las herramientas de 0.1.0 funcionan en su Windows, con un problema de contraste en la ventana principal. No constituye una validación exhaustiva de las herramientas.
- La versión 0.1.1 inicializa `ThemeManager` antes de crear la ventana y vincula sus colores al tema activo. La compilación Release para Windows x64 ha terminado correctamente.
- Se ha comprobado en ejecución, sin pantalla, la activación de ShareXDark y la resolución de cinco pinceles del tema: texto y texto secundario #D8DADB, fondo principal #272727, fondo de controles #2E2E2E y borde #1F1F1F.
- Las cinco bibliotecas compartidas, de captura y edición son idénticas byte a byte a las de 0.1.0. La auditoría de metadatos de los seis ensamblados vuelve a pasar.
- No se ha conseguido renderizar la ventana en este entorno: Xvfb no puede crear sus sockets. El aspecto final de 0.1.1 sigue pendiente de comprobar en Windows.

## Copia al confirmar y bandeja en 0.1.2

Implementado: confirmar una captura o continuar desde el editor copia el resultado si la opción está activada. Con bandeja disponible y residencia activada, no reaparece la ventana principal. Esc no copia una nueva imagen; un error abre la ventana y conserva la captura. Sin bandeja disponible se mantiene una ventana accesible.

Cerrar la ventana con X la oculta cuando la residencia está activada. Abrir desde la bandeja no registra de nuevo los atajos. Salir elimina el icono, libera los atajos y cierra las imágenes fijadas. El cierre por apagado o fin de sesión no se cancela.

Comprobado en Linux: compilación Windows x64, auditoría de ensamblados, migración de ajustes antiguos con ambas opciones activadas por defecto, persistencia de opciones desactivadas y conservación de preferencias anteriores. Las cinco bibliotecas de captura/edición no han cambiado respecto a 0.1.1.

Pruebas específicas pendientes en Windows:

- Confirmar con Enter y pegar en Paint y una aplicación de oficina: comprobar que se copia la imagen compuesta y que la ventana principal permanece oculta.
- Cancelar con Esc: comprobar que no sustituye el portapapeles. Repetir con la ventana visible y oculta.
- Cerrar con X, capturar varias veces por atajo y abrir/cerrar la ventana repetidamente: no debe aparecer en la barra de tareas mientras está oculta ni duplicar iconos o registros de atajos.
- Usar Abrir, Capturar región y Salir desde el icono. Después de Salir, verificar que desaparece el proceso y que sus atajos quedan libres.
- Desactivar cada opción y comprobar el flujo de vista previa y de salida mediante X.
- Probar portapapeles ocupado, explorador de Windows reiniciado y cierre de sesión/apagado.

Estas comprobaciones nativas no se han ejecutado aquí; Xvfb no puede crear sockets en el entorno actual.

## Importación de portapapeles 0.1.3

El usuario ha confirmado el comportamiento de copia y bandeja en 0.1.2. En 0.1.3 se añade el acceso directo al editor desde una imagen del portapapeles: botón, Ctrl + V en la ventana principal y opción de bandeja. Funciona sin `_lastImage` previa; el aviso de contenido no legible se produce antes de sustituir la última imagen. La importación solo lee el portapapeles y no guarda archivos ni copia de vuelta hasta una acción explícita de confirmar o copiar. Los archivos copiados desde el Explorador pasan por la política de rutas locales. No se han modificado las bibliotecas del editor ni de captura.

Compilación y auditoría estática realizadas en Linux. Pendiente en Windows:

- Desde un inicio sin captura previa, copiar una imagen de un navegador, Paint, Recortes y una aplicación de oficina; abrirla con el botón y comprobar que es el lienzo correcto y conserva sus dimensiones.
- Repetir mediante Ctrl + V en la ventana principal y mediante la opción de bandeja con la ventana oculta.
- Cancelar la edición y comprobar que la importación no ha sustituido el portapapeles. Confirmar y comprobar que se copia el resultado anotado según la opción configurada.
- Copiar un archivo PNG/JPEG local desde el Explorador y abrirlo desde el portapapeles. Rechazar las referencias a unidades de red.
- Probar portapapeles vacío, texto, contenido no compatible y bloqueo temporal del portapapeles: no debe perderse la última imagen ni bloquearse la aplicación.
- Comprobar Ctrl + V dentro del editor: el pegado original de imágenes/anotaciones y el pegado de texto al editar una anotación deben seguir funcionando.
