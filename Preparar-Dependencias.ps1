# Prepares download inputs for the offline Linux build. Does not compile shareW.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if ($env:OS -ne 'Windows_NT') { throw 'Ejecuta este preparador en un Windows x64 con Internet.' }
if (![Environment]::Is64BitOperatingSystem) { throw 'Se necesita Windows de 64 bits.' }

& "$PSScriptRoot/scripts/Verify-Source.ps1"
$sdkVersion = '10.0.404'
$work = Join-Path $PSScriptRoot '.offline-preparation'
$delivery = Join-Path $PSScriptRoot 'offline-inputs'
New-Item -ItemType Directory -Force -Path $work, $delivery | Out-Null

# These URLs and SHA-512 values are from Microsoft's .NET 10 release metadata.
$linuxName = "dotnet-sdk-$sdkVersion-linux-x64.tar.gz"
$windowsName = "dotnet-sdk-$sdkVersion-win-x64.zip"
$linuxHash = '577d0eb17fec33d029104c5c63a1ab0accc3e28776274abef2218dbac5af18d2109b5406d26b31d76f5c245215e3ba0926a94c20246c672c41847c9e1eb1dc50'
$windowsHash = '330ddd2665b43cd2e428216a0d8ea1ac7bb19b5434fdc530e62010dc3b52823a620bf95489d628e1ed6f5122401793ee4887cca4a019122361a1231497f632ee'

function Get-VerifiedSdk([string]$Name, [string]$Destination, [string]$Hash) {
    if (!(Test-Path $Destination -PathType Leaf)) {
        Write-Host "Descargando $Name ..."
        $previousProgress = $ProgressPreference
        try {
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -UseBasicParsing -Uri "https://builds.dotnet.microsoft.com/dotnet/Sdk/$sdkVersion/$Name" -OutFile "$Destination.partial"
            if ((Get-FileHash "$Destination.partial" -Algorithm SHA512).Hash.ToLowerInvariant() -ne $Hash) {
                throw "SHA-512 incorrecto: $Name. No se utilizara esta descarga."
            }
            Move-Item "$Destination.partial" $Destination
        }
        finally { $ProgressPreference = $previousProgress }
    }
    if ((Get-FileHash $Destination -Algorithm SHA512).Hash.ToLowerInvariant() -ne $Hash) {
        throw "El archivo existente no coincide con Microsoft: $Destination"
    }
    Write-Host "SHA-512 verificado: $Name"
}

function Invoke-LoggedRestore([string]$Executable, [string[]]$CommandArguments, [string]$LogPath) {
    # Windows PowerShell 5.1 wraps redirected native stderr in ErrorRecord objects.
    # Keep native exit-code handling separate from PowerShell's terminating errors.
    # Preferences here are function-local; the caller retains Stop.
    "shareW dependency restore started at $([DateTime]::UtcNow.ToString('o'))" |
        Set-Content -LiteralPath $LogPath -Encoding UTF8 -ErrorAction Stop
    $ErrorActionPreference = 'Continue'
    $PSNativeCommandUseErrorActionPreference = $false
    $global:LASTEXITCODE = $null
    & $Executable @CommandArguments 2>&1 |
        ForEach-Object {
            $line = $_.ToString()
            Add-Content -LiteralPath $LogPath -Value $line -Encoding UTF8 -ErrorAction Stop
            Write-Host $line
        }
    $nativeExitCode = $LASTEXITCODE
    if ($null -eq $nativeExitCode) { $nativeExitCode = -1 }
    "Native exit code: $nativeExitCode" | Add-Content -LiteralPath $LogPath -Encoding UTF8 -ErrorAction Stop
    return [int]$nativeExitCode
}

$linuxPath = Join-Path $delivery $linuxName
$windowsPath = Join-Path $work $windowsName
Get-VerifiedSdk $linuxName $linuxPath $linuxHash
Get-VerifiedSdk $windowsName $windowsPath $windowsHash

$sdkFolder = Join-Path $work 'sdk-win'
$dotnet = Join-Path $sdkFolder 'dotnet.exe'
if (!(Test-Path $dotnet -PathType Leaf)) {
    Write-Host 'Extrayendo el SDK temporal de Windows. No se instala en el sistema.'
    Expand-Archive -LiteralPath $windowsPath -DestinationPath $sdkFolder -Force
}

$cliHome = Join-Path $work 'cli-home'
$cache = Join-Path $work 'nuget-cache'
$emptyPacks = Join-Path $work 'empty-targeting-packs'
New-Item -ItemType Directory -Force -Path $cliHome, $cache, $emptyPacks | Out-Null
$config = Join-Path $work 'NuGet.Download.Config'
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
'@ | Set-Content -LiteralPath $config -Encoding UTF8

$environmentNames = @('DOTNET_ROOT', 'DOTNET_CLI_HOME', 'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE', 'DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE', 'DOTNET_GENERATE_ASPNET_CERTIFICATE')
$savedEnvironment = @{}
foreach ($name in $environmentNames) { $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
try {
    [Environment]::SetEnvironmentVariable('DOTNET_ROOT', $sdkFolder, 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_CLI_HOME', $cliHome, 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_CLI_TELEMETRY_OPTOUT', '1', 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_SKIP_FIRST_TIME_EXPERIENCE', '1', 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE', 'true', 'Process')
    [Environment]::SetEnvironmentVariable('DOTNET_GENERATE_ASPNET_CERTIFICATE', 'false', 'Process')
    $actualSdk = & $dotnet --version
    if ($LASTEXITCODE -ne 0 -or $actualSdk -ne $sdkVersion) { throw 'No se pudo iniciar el SDK temporal esperado.' }

    Write-Host 'Descargando paquetes de shareW y los paquetes de referencia/runtime Windows...'
    # The empty targeting-pack root forces NuGet to download the reference packs
    # that otherwise would only be present inside the Windows SDK installation.
    # .NET 10 also reads package-pruning metadata from that root. Disable pruning
    # for this download-only restore so a fresh empty root does not cause NETSDK1226.
    # This keeps a superset of dependencies available for the later offline build.
    $restoreArguments = @('restore', 'src/shareW/shareW.csproj', '-r', 'win-x64', '--packages', $cache, '--configfile', $config, '--force', '--no-http-cache', '-p:Platform=x64', '-p:EnableWindowsTargeting=true', '-p:SelfContained=true', '-p:RestoreEnablePackagePruning=false', "-p:NetCoreTargetingPackRoot=$emptyPacks")
    $restoreLog = Join-Path $delivery 'restore-download.log'
    $restoreExitCode = Invoke-LoggedRestore -Executable $dotnet -CommandArguments $restoreArguments -LogPath $restoreLog
    if ($restoreExitCode -ne 0) {
        throw "La restauracion fallo (codigo $restoreExitCode). Comparte $restoreLog para ver el error real."
    }
}
finally {
    foreach ($name in $environmentNames) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process') }
}

$packageFiles = @(Get-ChildItem -LiteralPath $cache -Recurse -File -Filter '*.nupkg')
if ($packageFiles.Count -eq 0) { throw 'La restauracion no ha dejado paquetes .nupkg.' }
$mandatory = @('microsoft.windowsdesktop.app.ref.', 'microsoft.netcore.app.ref.', 'microsoft.netcore.app.runtime.win-x64.', 'microsoft.windowsdesktop.app.runtime.win-x64.', 'microsoft.netcore.app.host.win-x64.')
foreach ($prefix in $mandatory) {
    if (!@($packageFiles | Where-Object { $_.Name.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase) }).Count) {
        throw "Falta un paquete necesario para Linux: $prefix. Comparte el log de restauracion."
    }
}

# Use a flat local NuGet feed: copy package archives, not extracted caches or obj files.
$staging = Join-Path $work ('feed-' + [Guid]::NewGuid().ToString('N'))
$feed = Join-Path $staging 'nuget-feed'
New-Item -ItemType Directory -Path $feed | Out-Null
foreach ($file in $packageFiles) { Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $feed $file.Name) }
Get-ChildItem -LiteralPath $feed -File | Sort-Object Name | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
} | Set-Content (Join-Path $staging 'SHA256SUMS.txt') -Encoding UTF8
@{
    sdk_version = $sdkVersion
    linux_sdk_file = $linuxName
    linux_sdk_sha512 = $linuxHash
    windows_sdk_sha512 = $windowsHash
    package_count = $packageFiles.Count
    prepared_at_utc = [DateTime]::UtcNow.ToString('o')
    operation = 'NuGet restore only; no shareW compilation'
} | ConvertTo-Json | Set-Content (Join-Path $staging 'BUNDLE-INFO.json') -Encoding UTF8

$packagesZip = Join-Path $delivery 'shareW-nuget-offline.zip'
if (Test-Path $packagesZip) { throw "Ya existe $packagesZip. Muevelo antes de repetir la preparacion." }
Compress-Archive -Path "$staging/*" -DestinationPath $packagesZip
Write-Host ''
Write-Host 'Preparado. Trae a la sesion estos DOS archivos de offline-inputs:'
Write-Host $linuxPath
Write-Host $packagesZip
Write-Host "Paquetes incluidos: $($packageFiles.Count)"
Write-Host 'No necesitas enviar el SDK de Windows, las carpetas de cache ni los archivos obj.'
