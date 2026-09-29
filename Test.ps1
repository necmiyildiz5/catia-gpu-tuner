$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$testRoot=Join-Path ([IO.Path]::GetTempPath()) ('catia-tuner-test-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $testRoot | Out-Null
$sources=@('src/NvDriver.cs','src/Engine.cs','src/SystemCheck.cs','tests/Tests.cs') | ForEach-Object {(Get-Item (Join-Path $PSScriptRoot $_)).FullName}
& $compiler /nologo /target:exe /platform:x64 "/out:$testRoot/tests.exe" /r:System.Web.Extensions.dll $sources
if($LASTEXITCODE -ne 0){throw 'Test compilation failed'}
& "$testRoot/tests.exe" "$testRoot/data"
if($LASTEXITCODE -ne 0){throw 'Tests failed'}
