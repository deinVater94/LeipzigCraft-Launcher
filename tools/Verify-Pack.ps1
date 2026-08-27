param(
    [Parameter(Mandatory=$true)]
    [string]$ManifestPath,

    [Parameter(Mandatory=$true)]
    [string]$SignaturePath
)

$ErrorActionPreference = "Stop"

$primaryPem = @'
-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEA3UJhSIhgsSxrhEmM/j1B
QmgaD2LNdScpQyI54LeTMXajJjCcfHH/sVWO8XuVoJGM3/cqjKK4ZvYOA+XBF9bq
MWHZUGy9Nq6XsJMy50225kMNxSDMiK66qc5zh1rg8U6a170N23rncvPoEe3nsCz4
g/SRz+pEWW26hdKOTHO9m8tj9IO8x4RwsEzqCEeMe8jRqNS5iIVaH1Ot1SG3Y2iD
/dX9SK6U4+Rz0KNrM77iqAFMqVt2f84tWwi/Yj+CykBR6mOZmxm4w/CIgnIaJObw
k1O3DqP82cZMVlzJPEZuL3E5pHKwxTXSz5HpGG86WP1MIRJAvg/2Zo8WNejOJIev
/YXOMTpi3ppzO8FNYk0QQz9sVV74gBtzIrMVLI5IFY3IE2vG6OcYP5NMF8fUqgKy
IRDhO45KXWO8TvGWL8eMqntOyknlQ/WpOL5Mt53SqvJc7fftNb2cgMYrnjIGnGzh
6NEiw1MkD5Qa8zbF17aIyzPI9VRY1VgvDKfxoMWwj0tzAgMBAAE=
-----END PUBLIC KEY-----
'@

$recoveryPem = @'
-----BEGIN PUBLIC KEY-----
MIIBojANBgkqhkiG9w0BAQEFAAOCAY8AMIIBigKCAYEAomqpeuFH359j2B6y48pl
4avKL5LTEXEbE1V89MydJrObgr0db5cVArmDMPUUp1b0sFgucik3vX657Wo4ibK4
qvbeHmgB482u067SraMyjNtbsCkV5T56YuMD4eEbs2OqhU4oxT96N/61D//paK3Q
ts3ttznhdy9YMlUaPP2IwaViotSTqnopRmlKz8/ZwN8SPZP5dV3V4spGodUsiJTH
YQDG1qpdk00qGO9dEiIIOhI76Uwixovj6hdCJ2wRPqDUi99OJa8PP0yrCqJN/Hrz
UuSXwMNkVoAINw9vkHaLKbVEaOq1vqExZ9X6TbuP9xaeiKu/LX1m1HvtaglM4V4k
YDrU4P1t7BP26Z29BbOvayiGWEgQHa7KQkBp+LgAG8BD+GwV7eGGfausgY/sALg4
tq+DNV5/KYpQizf+4BgNUILF3EK2IBVYKzVoZ819FyS98FLCYhWvucl8QTgZ2J9Z
lNrnVfQDk9ok9LSxnLYvh4DwojDQBCVqr5X2MSVsWH0ZAgMBAAE=
-----END PUBLIC KEY-----
'@

$data = [IO.File]::ReadAllBytes(
    (Resolve-Path $ManifestPath -ErrorAction Stop).Path)

$signatureText = [IO.File]::ReadAllText(
    (Resolve-Path $SignaturePath -ErrorAction Stop).Path).Trim()

try {
    $signature = [Convert]::FromBase64String($signatureText)
}
catch {
    throw "pack.sig ist kein gültiges Base64."
}

function Test-Key {
    param(
        [string]$Pem,
        [byte[]]$Data,
        [byte[]]$Signature
    )

    $rsa = [Security.Cryptography.RSA]::Create()

    try {
        $rsa.ImportFromPem($Pem)

        return $rsa.VerifyData(
            $Data,
            $Signature,
            [Security.Cryptography.HashAlgorithmName]::SHA256,
            [Security.Cryptography.RSASignaturePadding]::Pkcs1
        )
    }
    finally {
        $rsa.Dispose()
    }
}

if (Test-Key -Pem $primaryPem -Data $data -Signature $signature) {
    Write-Host "GÜLTIG - Primary Key" -ForegroundColor Green
    exit 0
}

if (Test-Key -Pem $recoveryPem -Data $data -Signature $signature) {
    Write-Host "GÜLTIG - Recovery Key" -ForegroundColor Green
    exit 0
}

Write-Host "UNGÜLTIGE SIGNATUR" -ForegroundColor Red
exit 1
