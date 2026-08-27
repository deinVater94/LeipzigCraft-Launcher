param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [string]$Owner = "deinVater94",
    [string]$Repo = "LeipzigCraft-Launcher",
    [string]$SourceRoot = (Join-Path $env:APPDATA ".minecraft"),
    [string]$PreviousManifestUrl = "https://leipzigcraft.com/launcher/pack.json",

    [Parameter(Mandatory=$true)]
    [string]$SigningPrivateKey
)

$ErrorActionPreference = "Stop"

$tag = "pack-v$Version"
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$dist = Join-Path $scriptRoot "..\dist-incremental"
$upload = Join-Path $dist "upload"
$temp = Join-Path $env:TEMP "LeipzigCraft-Incremental-Pack"

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }

New-Item -ItemType Directory -Force -Path $dist | Out-Null
New-Item -ItemType Directory -Force -Path $upload | Out-Null
New-Item -ItemType Directory -Force -Path $temp | Out-Null

$previous = $null

try {
    Write-Host "Lade bisherige pack.json ..." -ForegroundColor DarkGray
    $previous = Invoke-RestMethod -Uri $PreviousManifestUrl -UseBasicParsing

    if ($previous.schemaVersion -ne 2) {
        $previous = $null
        Write-Host "Altes Manifest-Format erkannt; beim ersten inkrementellen Release werden alle Mods hochgeladen." -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Keine vorherige inkrementelle Modliste gefunden." -ForegroundColor Yellow
}

function Get-SafeAssetName {
    param([string]$Name, [string]$Hash)

    $safe = [regex]::Replace($Name, '[^A-Za-z0-9._-]', '_')
    $prefix = $Hash.Substring(0, 12).ToLowerInvariant()
    return "$prefix-$safe"
}

function Get-ReleaseUrl {
    param([string]$AssetName)

    $encoded = [Uri]::EscapeDataString($AssetName)
    return "https://github.com/$Owner/$Repo/releases/download/$tag/$encoded"
}

function Find-PreviousFile {
    param([string]$Path, [string]$Hash)

    if ($null -eq $previous -or $null -eq $previous.files) {
        return $null
    }

    return $previous.files |
        Where-Object {
            $_.path -eq $Path -and
            ($_.sha256 -replace '^sha256:', '').ToUpperInvariant() -eq $Hash
        } |
        Select-Object -First 1
}

$modsDir = Join-Path $SourceRoot "mods"

if (-not (Test-Path $modsDir)) {
    throw "Kein mods-Ordner gefunden: $modsDir"
}

$manifestFiles = @()
$newAssets = @()
$mods = Get-ChildItem $modsDir -File -Filter "*.jar" | Sort-Object Name

Write-Host ""
Write-Host "Prüfe $($mods.Count) Mods ..." -ForegroundColor Green

foreach ($mod in $mods) {
    $hash = (Get-FileHash $mod.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    $relativePath = "mods/$($mod.Name)"
    $previousFile = Find-PreviousFile -Path $relativePath -Hash $hash

    if ($null -ne $previousFile) {
        $url = $previousFile.url
        Write-Host "UNCHANGED  $($mod.Name)" -ForegroundColor DarkGray
    }
    else {
        $assetName = Get-SafeAssetName -Name $mod.Name -Hash $hash
        Copy-Item $mod.FullName (Join-Path $upload $assetName) -Force

        $url = Get-ReleaseUrl -AssetName $assetName
        $newAssets += $assetName

        Write-Host "UPLOAD     $($mod.Name)" -ForegroundColor Cyan
    }

    $manifestFiles += [ordered]@{
        path = $relativePath
        url = $url
        sha256 = $hash
        size = $mod.Length
    }
}

$configArchive = $null
$configDir = Join-Path $SourceRoot "config"

if (Test-Path $configDir) {
    Write-Host ""
    Write-Host "Baue Config-Archiv ..." -ForegroundColor Green

    $configWork = Join-Path $temp "config"
    New-Item -ItemType Directory -Force -Path $configWork | Out-Null
    Copy-Item (Join-Path $configDir "*") $configWork -Recurse -Force

    $removeFiles = @(
        (Join-Path $configWork "voicechat\username-cache.json"),
        (Join-Path $configWork "voicechat\player-volumes.properties"),
        (Join-Path $configWork "voicechat\category-volumes.properties")
    )

    foreach ($file in $removeFiles) {
        if (Test-Path $file) { Remove-Item $file -Force }
    }

    $removeDirs = @(
        (Join-Path $configWork "worldedit\sessions"),
        (Join-Path $configWork "SeedCrackerX saved structures")
    )

    foreach ($dir in $removeDirs) {
        if (Test-Path $dir) { Remove-Item $dir -Recurse -Force }
    }

    $rawConfigZip = Join-Path $temp "LeipzigCraft-config.zip"

    Compress-Archive `
        -Path (Join-Path $configWork "*") `
        -DestinationPath $rawConfigZip `
        -CompressionLevel Optimal

    $configHash = (Get-FileHash $rawConfigZip -Algorithm SHA256).Hash.ToUpperInvariant()
    $configInfo = Get-Item $rawConfigZip
    $reuseConfig = $false

    if ($null -ne $previous -and $null -ne $previous.configArchive) {
        $oldHash = ($previous.configArchive.sha256 -replace '^sha256:', '').ToUpperInvariant()

        if ($oldHash -eq $configHash) {
            $reuseConfig = $true
            $configUrl = $previous.configArchive.url
            Write-Host "UNCHANGED  config" -ForegroundColor DarkGray
        }
    }

    if (-not $reuseConfig) {
        $configAssetName = Get-SafeAssetName -Name "LeipzigCraft-config.zip" -Hash $configHash
        Copy-Item $rawConfigZip (Join-Path $upload $configAssetName) -Force

        $configUrl = Get-ReleaseUrl -AssetName $configAssetName
        $newAssets += $configAssetName

        Write-Host "UPLOAD     config" -ForegroundColor Cyan
    }

    $configArchive = [ordered]@{
        url = $configUrl
        sha256 = $configHash
        size = $configInfo.Length
        extractTo = "config"
    }
}

$manifest = [ordered]@{
    schemaVersion = 2
    version = $Version
    minecraft = "1.21"
    fabricLoader = "0.18.4"
    files = $manifestFiles
    configArchive = $configArchive
}

$manifestPath = Join-Path $dist "pack.json"

$manifest |
    ConvertTo-Json -Depth 8 |
    Set-Content $manifestPath -Encoding UTF8

# Sign the EXACT manifest bytes that will be published.
$signaturePath = Join-Path $dist "pack.sig"
$privateKeyPath = (Resolve-Path $SigningPrivateKey -ErrorAction Stop).Path

$pem = [IO.File]::ReadAllText($privateKeyPath)
$manifestBytes = [IO.File]::ReadAllBytes($manifestPath)
$rsa = [Security.Cryptography.RSA]::Create()

try {
    $rsa.ImportFromPem($pem)

    $signatureBytes = $rsa.SignData(
        $manifestBytes,
        [Security.Cryptography.HashAlgorithmName]::SHA256,
        [Security.Cryptography.RSASignaturePadding]::Pkcs1
    )

    [IO.File]::WriteAllText(
        $signaturePath,
        [Convert]::ToBase64String($signatureBytes),
        [Text.Encoding]::ASCII
    )
}
finally {
    $rsa.Dispose()
}

Write-Host "pack.json kryptografisch signiert." -ForegroundColor Green

@"
LeipzigCraft Incremental Pack $Version

1. GitHub -> $Owner/$Repo -> Releases
2. Create new release
3. Tag: $tag
4. Lade ALLE Dateien aus dist-incremental\upload hoch
5. Release veröffentlichen
6. dist-incremental\pack.json im WEBSITE-Repo als launcher/pack.json ersetzen
7. dist-incremental\pack.sig im WEBSITE-Repo als launcher/pack.sig hochladen

Neue Release-Assets: $($newAssets.Count)
Gesamte Mods im Manifest: $($manifestFiles.Count)
"@ | Set-Content (Join-Path $dist "NEXT-STEPS.txt") -Encoding UTF8

$newAssets |
    Set-Content (Join-Path $dist "UPLOAD-ASSETS.txt") -Encoding UTF8

Write-Host ""
Write-Host "FERTIG" -ForegroundColor Green
Write-Host "Manifest: $manifestPath"
Write-Host "Neue Assets: $($newAssets.Count)"
Write-Host "Hinweise: $(Join-Path $dist 'NEXT-STEPS.txt')"
