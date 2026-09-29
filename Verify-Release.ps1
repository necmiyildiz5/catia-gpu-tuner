param(
 [Parameter(Mandatory=$true)][string]$Zip,
 [Parameter(Mandatory=$true)][string]$Sums
)
$ErrorActionPreference='Stop'
if(!(Test-Path -LiteralPath $Zip) -or !(Test-Path -LiteralPath $Sums)){throw 'ZIP or SHA256SUMS file was not found.'}
$expected=((Get-Content -LiteralPath $Sums -Raw).Trim() -split '\s+')[0].ToLowerInvariant()
$actual=(Get-FileHash -LiteralPath $Zip -Algorithm SHA256).Hash.ToLowerInvariant()
if($expected -ne $actual){throw "INTEGRITY CHECK FAILED. Expected $expected but received $actual. Do not run this file."}
Write-Host "Integrity check passed: $actual" -ForegroundColor Green
