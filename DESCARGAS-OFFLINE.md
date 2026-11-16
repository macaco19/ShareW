# Preparar las descargas para compilar shareW aqui

Necesitamos el SDK **10.0.404 para Linux x64** y todos los paquetes NuGet del proyecto, incluidos los paquetes de referencia y runtime de Windows. Las DLL del portable no sustituyen a los paquetes de compilación: faltan generadores de código y tareas de XAML, entre otros componentes.

## Opción automática desde un Windows x64 con Internet

1. Descarga y extrae `shareW-0.1.0-preparacion-offline.zip` en una carpeta local con varios GB de espacio temporal, por ejemplo `C:\shareW-preparacion`.
2. Abre PowerShell en la carpeta que contiene `Preparar-Dependencias.ps1` y la carpeta `src`.
3. Ejecuta:

   ```powershell
   .\Preparar-Dependencias.ps1
   ```

4. Espera a que termine. Descarga y verifica el SDK de Linux; también descarga y extrae temporalmente el SDK de Windows para resolver los paquetes NuGet. No instala el SDK ni compila shareW. La restauración puede ejecutar tareas de MSBuild de los proyectos y paquetes, como cualquier `dotnet restore`.
5. Trae al directorio de esta sesión los dos archivos que genera dentro de `offline-inputs`:

   - `dotnet-sdk-10.0.404-linux-x64.tar.gz`
   - `shareW-nuget-offline.zip`

Con ellos se intentará la restauración local y la compilación cruzada para Windows x64 en Linux. Sigue pendiente comprobar si todas las tareas de compilación de esta versión funcionan en este entorno. Las pruebas de captura, DPI y atajos se harán después en Windows.

Si falla, comparte `offline-inputs/restore-download.log` y el mensaje de error. El script no cambia políticas de ejecución: si tu equipo no permite ejecutarlo, podemos preparar las descargas mediante pasos manuales según el mensaje que muestre.

El preparador desactiva `RestoreEnablePackagePruning` únicamente en su comando de restauración. .NET 10 necesita datos de los paquetes de referencia para esta optimización; el preparador usa una raíz vacía para forzar su descarga, lo que causaba `NETSDK1226`. Recopilar sin esa optimización conserva las dependencias para la compilación posterior. No cambia los proyectos ni sus funciones.

## Descarga directa del SDK de Linux

Si prefieres empezar trayendo solo el SDK, descarga este archivo sin instalarlo:

https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.404/dotnet-sdk-10.0.404-linux-x64.tar.gz

SHA-512 publicado por Microsoft:

```text
577d0eb17fec33d029104c5c63a1ab0accc3e28776274abef2218dbac5af18d2109b5406d26b31d76f5c245215e3ba0926a94c20246c672c41847c9e1eb1dc50
```

El SDK solo no basta para compilar sin Internet; aún necesitaremos los paquetes NuGet. El script automatiza esa segunda parte para evitar una lista manual incompleta de dependencias transitivas.

Referencias oficiales:

- SDK, archivos y hashes: https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- Restauración de dependencias: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore
- Compilación para Windows desde Linux: https://learn.microsoft.com/en-us/dotnet/core/tools/sdk-errors/netsdk1100
