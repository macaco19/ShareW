# shareW 0.1.3 — versión inicial para pruebas

Aplicación Windows de captura y anotación basada en **ShareX 22.4.0**, revisión `08bfff15e748a945944e609aca7faffbe888c7af`.

**Estado:** compilación cruzada para Windows x64 completada en Linux con SDK .NET 10.0.404. Se ha generado un portable con runtime .NET 10.0.15 incluido. El usuario ha confirmado el funcionamiento de la captura, la interfaz, la copia al confirmar y la bandeja en 0.1.2. La importación desde el portapapeles de 0.1.3 sigue pendiente de comprobar en Windows; ver `VALIDACION.md`.

El paquete `shareW-0.1.3-win-x64-pruebas.zip` contiene el programa. Extrae la carpeta completa y ejecuta `shareW.exe`. El paquete de fuentes se entrega por separado para revisión y reproducción de la compilación.

## Funciones implementadas

- Captura de región con la ventana y la barra originales de 22.4.0, con las anotaciones activadas. Se conservan las 19 herramientas que ofrece esa versión durante la captura.
- Captura de todas las pantallas y de la ventana activa; selección de varias regiones y lupa.
- Editor original con sus 21 herramientas, efectos y ajustes locales. Recortar y cortar se mantienen en el editor, como en el original.
- Editar imágenes del portapapeles sin guardarlas: botón dedicado, Ctrl + V en la ventana principal y acceso desde la bandeja. No requiere una captura previa.
- Copia al portapapeles al confirmar la captura, activada por defecto; vista previa y botones de copia y guardado manual. El editor mantiene PNG, JPEG y WebP.
- Icono de bandeja con Abrir, Capturar región y Salir; cerrar la ventana mantiene activos los atajos por defecto.
- Imágenes fijadas en ventanas, siempre visibles, que se cierran con shareW.
- Atajos Ctrl + Impr Pant, Impr Pant y Alt + Impr Pant, con aviso si otra aplicación los tiene ocupados.

Las capturas de pantalla y ventana quedan disponibles en la vista previa; abre shareW desde la bandeja y pulsa **Editar última imagen** para anotarlas. Para región, las anotaciones están disponibles sobre la pantalla antes de confirmar. La interfaz de 22.4.0 es la referencia; la imagen antigua de 19.0.2 no determina los iconos o su distribución.

## Uso previsto

1. Pulsa **Capturar región**.
2. Selecciona una zona. La captura rápida está desactivada inicialmente para que puedas seguir anotando.
3. Usa la barra y sus opciones. Tab alterna entre selección y anotación, Enter confirma y Esc cancela.
4. Por defecto, Enter copia la imagen y deja shareW en la bandeja; pégala con Ctrl + V. No se guarda automáticamente un archivo.
5. Cerrar la ventana con X la oculta y mantiene los atajos. Usa clic derecho en el icono de bandeja > **Salir de shareW** para terminar el proceso. El icono puede estar en el menú de iconos ocultos de Windows (`^`).

Puedes desactivar **Copiar al confirmar captura** para volver a la vista previa antes de copiar, y **Seguir activo al cerrar la ventana** para que X salga del programa. Si falla la copia, se conserva la imagen y se muestra el error en la ventana.

La última imagen vive en memoria y se sustituye al cargar o capturar otra. Se conservan los ajustes en `%LOCALAPPDATA%\shareW\settings.json`; no se conserva en ese archivo un historial de imágenes recientes. No hay instalación, servicio, ejecución como administrador ni cambio de reglas EPM.

## Compilar en Windows

En un equipo Windows x64 de desarrollo con **SDK de .NET 10** y acceso a las dependencias NuGet, abre PowerShell en esta carpeta y ejecuta:

```powershell
.\Build.ps1
```

El script verifica el inventario y compila únicamente `src/shareW/shareW.csproj` y sus cinco bibliotecas. Genera `artifacts/shareW-0.1.3-win-x64.zip`, con runtime incluido. El dispositivo donde se use el resultado no necesita instalar el SDK ni descargar paquetes.

No modifica políticas de ejecución. Si la organización restringe scripts o compilaciones, debe utilizarse su procedimiento autorizado de construcción. Una compilación correcta debe completarse con las pruebas manuales indicadas antes de distribuir el programa.

Para revisar las fuentes sin compilar, también puedes ejecutar `python scripts/verify-source.py` con Python 3. No requiere paquetes externos.

## Compilación reproducida en Linux

La compilación se ha completado con los archivos aportados y sin fuentes NuGet remotas. `Build-Linux.sh` usa un SDK Linux extraído en `../.build-tools/dotnet` y los paquetes `.nupkg` en `../.build-tools/nuget-feed`:

```bash
bash Build-Linux.sh
```

Puedes cambiar la raíz de herramientas con `SHAREW_BUILD_TOOLS`. El script usa un único proceso de MSBuild y desactiva el servidor de compilación para funcionar en este entorno restringido. Genera los archivos para Windows en `artifacts/shareW-win-x64` y omite los símbolos de depuración nativos grandes que no necesita el programa para ejecutarse.

Para preparar de nuevo las descargas, consulta `DESCARGAS-OFFLINE.md`. `Preparar-Dependencias.ps1` solo descarga y restaura paquetes en el equipo conectado; la compilación puede hacerse después en Linux.

## Capacidades retiradas y alcance

El conjunto de fuentes de `src` excluye `ShareX.UploadersLib`, la aplicación original, integraciones del navegador, herramientas generales, grabación, descargadores y actualizadores. Se retiraron el callback y el comando de subida del editor, su botón y su atajo. Se retiró el transporte HTTP de importación de imágenes y sus controles de URL. También se eliminaron los auxiliares de ping, proxy y ejecución de programas externos. Las funciones compartidas para abrir URLs o lanzar archivos mediante el shell no realizan esas acciones en esta variante.

Las operaciones de archivo del anfitrión y las entradas de imagen del editor rechazan rutas UNC y unidades de red mapeadas. Esto no controla servicios externos de Windows: una carpeta local puede estar sincronizada, el portapapeles puede tener sincronización o redirección RDP y un sistema de archivos puede tener enlaces o puntos de montaje. shareW no cambia esas políticas. La comprobación de APIs en fuentes no constituye una certificación de ausencia de tráfico; el runtime y las dependencias necesitan revisión y pruebas en Windows.

La aplicación es independiente y se identifica como shareW. No está diseñada para eludir EPM ni garantiza que lo acepte. TI debe revisar y aprobar la compilación resultante según su política.

## Licencias y procedencia

shareW deriva de ShareX y conserva sus avisos y código bajo GPL. El código nuevo se ofrece bajo GPL-3.0-or-later. Consulta `LICENSE`, `Licenses/`, `PROVENANCE.json` y `scripts/source-manifest.json`. El manifiesto identifica las fuentes conservadas, modificadas y eliminadas de los cinco proyectos originales. Los hashes permiten detectar diferencias, pero no son una firma de confianza.

Es un proyecto derivado, sin afiliación ni aprobación del equipo de ShareX. El script de compilación no firma el ejecutable.

## Editar una imagen del portapapeles

1. Copia una imagen desde otra aplicación.
2. Pulsa **Editar imagen del portapapeles…** o **Ctrl + V en la ventana principal**. También está disponible en el menú de la bandeja.
3. La imagen se carga como lienzo del editor, conservando sus dimensiones, sin crear un archivo temporal.
4. Usa las mismas herramientas de anotación. Al confirmar se copia el resultado según tu opción de copia automática. Abrir el editor no sustituye por sí mismo el contenido del portapapeles.

**Editar última imagen…** conserva el acceso a la última captura o imagen abierta. **Ctrl + V dentro del editor** mantiene el pegado original de imágenes y anotaciones; el nuevo atajo de la ventana principal no interfiere con él.

La lectura de formatos nativos de imagen usa los auxiliares de ShareX (PNG, bitmap/DIB, según lo que ofrezca Windows). Si el portapapeles contiene una referencia a un archivo de imagen local, se puede abrir también. No se resuelven ni descargan direcciones de Internet. Si no hay imagen legible, se muestra el aviso y se conserva la última imagen. No hay vigilancia continua del portapapeles.

## Actualizar a 0.1.3

Usa **Salir de shareW** desde el icono de bandeja antes de sustituir archivos; cerrar con X lo deja activo. Copia los archivos de `shareW-0.1.3-actualizacion.zip` sobre la carpeta completa de una versión 0.1.x anterior, conservando sus DLL. Los ajustes existentes se mantienen.
