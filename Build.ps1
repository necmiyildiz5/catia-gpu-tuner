$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$out=Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Force $out | Out-Null
$sources=Get-ChildItem "$PSScriptRoot/src/*.cs" | Select-Object -ExpandProperty FullName
& $compiler /nologo /target:winexe /platform:x64 /optimize+ "/win32manifest:$PSScriptRoot/App.manifest" "/win32icon:$PSScriptRoot/assets/CATIA-GPU-Tuner.ico" "/out:$out/CATIA-GPU-Tuner.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Management.dll /r:System.Web.Extensions.dll $sources
if($LASTEXITCODE -ne 0){throw 'Build failed'}
Get-FileHash "$out/CATIA-GPU-Tuner.exe" | Format-List
