param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"

$source = Join-Path $env:APPDATA ".minecraft"
$work = Join-Path $env:TEMP "LeipzigCraft-Pack-Build"
$outDir = Join-Path $PSScriptRoot "..\dist"
$zip = Join-Path $outDir "LeipzigCraft-Pack-$Version.zip"

if (Test-Path $work) {
    Remove-Item $work -Recurse -Force
}

New-Item -ItemType Directory -Force -Path (Join-Path $work "mods") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $work "config") | Out-Null
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Write-Host "Kopiere JAR-Dateien ..." -ForegroundColor Cyan

Get-ChildItem (Join-Path $source "mods") -File -Filter "*.jar" | ForEach-Object {
    Copy-Item $_.FullName (Join-Path $work "mods") -Force
}

if (Test-Path (Join-Path $source "config")) {
    Write-Host "Kopiere Config ..." -ForegroundColor Cyan

    Copy-Item (Join-Path $source "config\*") (Join-Path $work "config") -Recurse -Force

    # Do not ship local identities, player-specific volumes, caches or WorldEdit sessions.
    $removeFiles = @(
        (Join-Path $work "config\voicechat\username-cache.json"),
        (Join-Path $work "config\voicechat\player-volumes.properties"),
        (Join-Path $work "config\voicechat\category-volumes.properties")
    )

    foreach ($file in $removeFiles) {
        if (Test-Path $file) {
            Remove-Item $file -Force
        }
    }

    $removeDirs = @(
        (Join-Path $work "config\worldedit\sessions"),
        (Join-Path $work "config\SeedCrackerX saved structures")
    )

    foreach ($dir in $removeDirs) {
        if (Test-Path $dir) {
            Remove-Item $dir -Recurse -Force
        }
    }
}

if (Test-Path $zip) {
    Remove-Item $zip -Force
}

Write-Host "Erstelle ZIP. Bei 1,3 GB dauert das etwas ..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $work "*") -DestinationPath $zip -CompressionLevel Optimal

$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
$size = (Get-Item $zip).Length

Write-Host ""
Write-Host "Fertig:" -ForegroundColor Green
Write-Host $zip
Write-Host "SHA256: $hash"
Write-Host "Bytes:  $size"
Write-Host ""
Write-Host "Diesen Hash und die Release-URL anschließend in web\launcher\pack.json eintragen."
