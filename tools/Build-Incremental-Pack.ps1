param(
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [Parameter(Mandatory=$true)]
    [string]$SigningPrivateKey
)

Write-Host "Bitte deine bisherige Build-Incremental-Pack.ps1 verwenden und dort fabricLoader auf 0.17.3 ändern."
exit 1
