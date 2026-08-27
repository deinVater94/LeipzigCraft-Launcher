param(
    [Parameter(Mandatory=$true)]
    [string]$ManifestPath,

    [Parameter(Mandatory=$true)]
    [string]$PrivateKeyPath,

    [string]$SignaturePath = ""
)

$ErrorActionPreference = "Stop"

$manifest = (Resolve-Path $ManifestPath -ErrorAction Stop).Path
$privateKey = (Resolve-Path $PrivateKeyPath -ErrorAction Stop).Path

if ([string]::IsNullOrWhiteSpace($SignaturePath)) {
    $SignaturePath = Join-Path (Split-Path $manifest -Parent) "pack.sig"
}

$pem = [IO.File]::ReadAllText($privateKey)
$data = [IO.File]::ReadAllBytes($manifest)

$rsa = [Security.Cryptography.RSA]::Create()

try {
    $rsa.ImportFromPem($pem)

    $signature = $rsa.SignData(
        $data,
        [Security.Cryptography.HashAlgorithmName]::SHA256,
        [Security.Cryptography.RSASignaturePadding]::Pkcs1
    )

    [IO.File]::WriteAllText(
        $SignaturePath,
        [Convert]::ToBase64String($signature),
        [Text.Encoding]::ASCII
    )
}
finally {
    $rsa.Dispose()
}

Write-Host ""
Write-Host "Signatur erstellt:" -ForegroundColor Green
Write-Host $SignaturePath
Write-Host ""
Write-Host "pack.json und pack.sig immer gemeinsam veröffentlichen." -ForegroundColor Yellow
