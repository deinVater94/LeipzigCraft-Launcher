$ErrorActionPreference = "Stop"

$source = Join-Path $env:APPDATA ".minecraft"
$target = Join-Path $env:APPDATA "LeipzigCraft\game"

Write-Host ""
Write-Host "LeipzigCraft - aktuelle funktionierende Installation importieren" -ForegroundColor Green
Write-Host ""

if (-not (Test-Path (Join-Path $source "mods"))) {
    throw "Kein mods-Ordner unter $source gefunden."
}

New-Item -ItemType Directory -Force -Path $target | Out-Null

$targetMods = Join-Path $target "mods"
if (Test-Path $targetMods) {
    Remove-Item $targetMods -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $targetMods | Out-Null

Write-Host "Kopiere JAR-Mods ..." -ForegroundColor Cyan

Get-ChildItem (Join-Path $source "mods") -File -Filter "*.jar" | ForEach-Object {
    Copy-Item $_.FullName $targetMods -Force
}

$sourceConfig = Join-Path $source "config"
$targetConfig = Join-Path $target "config"

if (Test-Path $sourceConfig) {
    Write-Host "Kopiere Config ..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $targetConfig | Out-Null
    Copy-Item (Join-Path $sourceConfig "*") $targetConfig -Recurse -Force

    # Remove known personal/cache files before using this as a clean client instance.
    $removeFiles = @(
        (Join-Path $targetConfig "voicechat\username-cache.json"),
        (Join-Path $targetConfig "voicechat\player-volumes.properties"),
        (Join-Path $targetConfig "voicechat\category-volumes.properties")
    )

    foreach ($file in $removeFiles) {
        if (Test-Path $file) {
            Remove-Item $file -Force
        }
    }

    $removeDirs = @(
        (Join-Path $targetConfig "worldedit\sessions"),
        (Join-Path $targetConfig "SeedCrackerX saved structures")
    )

    foreach ($dir in $removeDirs) {
        if (Test-Path $dir) {
            Remove-Item $dir -Recurse -Force
        }
    }
}

$count = (Get-ChildItem $targetMods -Filter "*.jar" -File).Count

Write-Host ""
Write-Host "Fertig: $count Mods nach $target importiert." -ForegroundColor Green
Write-Host "Deine normale .minecraft-Installation wurde nicht verändert." -ForegroundColor DarkGray
Write-Host ""
Read-Host "Enter zum Schließen"
