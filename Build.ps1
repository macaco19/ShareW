# Builds the reviewed source. Does not install software or change security policy.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

& "$PSScriptRoot/scripts/Verify-Source.ps1"

$sdk = & dotnet --version
if ($LASTEXITCODE -ne 0 -or $sdk -notmatch '^10\.') {
    throw 'Se necesita el SDK de .NET 10 en el equipo de compilacion.'
}

$output = Join-Path $PSScriptRoot 'artifacts/shareW-win-x64'
if (Test-Path $output) {
    throw 'La carpeta artifacts/shareW-win-x64 ya existe. Muevela o eliminala antes de compilar de nuevo para evitar mezclar versiones.'
}
& dotnet publish src/shareW/shareW.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -o $output
if ($LASTEXITCODE -ne 0) { throw 'La compilacion ha fallado; no se genera un paquete.' }

$forbidden = Get-ChildItem $output -Recurse -File | Where-Object {
    $_.Name -match 'Uploaders|FluentFTP|Renci\.SshNet|NativeMessaging|ffmpeg|ShareX\.exe|ShareX\.Tools|OnnxRuntime'
}
if ($forbidden) { throw "El resultado contiene componentes no permitidos: $($forbidden.Name -join ', ')" }
if (!(Test-Path (Join-Path $output 'shareW.exe'))) { throw 'No se ha generado shareW.exe.' }
foreach ($symbol in @('libSkiaSharp.pdb', 'libHarfBuzzSharp.pdb')) {
    $symbolPath = Join-Path $output $symbol
    if (Test-Path $symbolPath) { Remove-Item -LiteralPath $symbolPath }
}

Copy-Item LICENSE $output
Copy-Item Licenses $output -Recurse
Copy-Item README.md (Join-Path $output 'README-SOURCE.md')
Copy-Item VALIDACION.md, HERRAMIENTAS.md, PROVENANCE.json $output
Copy-Item scripts/source-manifest.json $output
@{
    application = 'shareW'
    version = '0.1.3'
    sdk = $sdk
    built_at_utc = [DateTime]::UtcNow.ToString('o')
    upstream_commit = '08bfff15e748a945944e609aca7faffbe888c7af'
    manual_windows_validation = 'pending'
} | ConvertTo-Json | Set-Content (Join-Path $output 'BUILD-INFO.json') -Encoding UTF8
Get-ChildItem $output -Recurse -File | Sort-Object FullName | ForEach-Object {
    $relative = $_.FullName.Substring($output.Length + 1).Replace('\', '/')
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $relative
} | Set-Content (Join-Path $output 'SHA256SUMS.txt') -Encoding UTF8

$archive = Join-Path $PSScriptRoot 'artifacts/shareW-0.1.3-win-x64.zip'
Compress-Archive -Path "$output/*" -DestinationPath $archive
Write-Host "Paquete generado: $archive"
Write-Host 'Compilar no sustituye las pruebas de Windows de VALIDACION.md ni la aprobacion de TI.'
