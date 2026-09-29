param([Parameter(Mandatory=$true)][string]$Version)
$ErrorActionPreference='Stop'
& "$PSScriptRoot\Build.ps1"
$root=Split-Path $PSScriptRoot -Parent
$stage=Join-Path $env:TEMP "CATIA-GPU-Tuner-$Version"
$zip=Join-Path $root "CATIA-GPU-Tuner-$Version-win-x64.zip"
$sums=Join-Path $root "CATIA-GPU-Tuner-$Version-SHA256SUMS.txt"
Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stage | Out-Null
Copy-Item "$PSScriptRoot\dist\CATIA-GPU-Tuner.exe" "$stage\CATIA-GPU-Tuner.exe"
Compress-Archive -Path "$stage\CATIA-GPU-Tuner.exe" -DestinationPath $zip
$hash=(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
Set-Content -LiteralPath $sums -Value "$hash  $(Split-Path $zip -Leaf)" -NoNewline
Write-Host "Created $zip and $sums"
