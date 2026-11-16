[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content (Join-Path $PSScriptRoot 'source-manifest.json') -Raw | ConvertFrom-Json
$expected = @{}
foreach ($entry in $manifest.files) {
    $expected[$entry.path] = $true
    $path = Join-Path $root $entry.path
    if (!(Test-Path $path -PathType Leaf)) { throw "Falta el archivo: $($entry.path)" }
    $actual = (Get-FileHash $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $entry.sha256) { throw "El archivo ha cambiado: $($entry.path). Revise el cambio y actualice el manifiesto despues de auditarlo." }
}
Get-ChildItem (Join-Path $root 'src') -Recurse -File | Where-Object {
    $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
} | ForEach-Object {
    $relative = $_.FullName.Substring($root.Length + 1).Replace('\', '/')
    if (!$expected.ContainsKey($relative)) { throw "Archivo no inventariado: $relative" }
}
Write-Host "Inventario verificado: $($manifest.files.Count) archivos. Esto no verifica el comportamiento en ejecucion."
