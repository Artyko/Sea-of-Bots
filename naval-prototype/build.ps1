param([string]$GameDirectory = 'E:\SteamLibrary\steamapps\common\Clone Drone in the Danger Zone')
$ErrorActionPreference = 'Stop'
$taskManaged = Join-Path $GameDirectory 'Clone Drone in the Danger Zone_Data\Managed'
$taskCompiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskOutput = Join-Path $PSScriptRoot 'dist\NavalPrototype'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskReferences = Get-ChildItem -LiteralPath $taskManaged -Filter '*.dll' | ForEach-Object { '/reference:' + $_.FullName }
$taskManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ModInfo.json') -Raw | ConvertFrom-Json
$taskSources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
& $taskCompiler /nologo /noconfig /target:library /nostdlib /optimize+ ('/out:' + (Join-Path $taskOutput $taskManifest.MainDLLFileName)) $taskReferences $taskSources
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ModInfo.json') -Destination $taskOutput
Write-Output "Built: $taskOutput"
