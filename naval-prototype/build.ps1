param([string]$GameDirectory = 'E:\SteamLibrary\steamapps\common\Clone Drone in the Danger Zone')
$ErrorActionPreference = 'Stop'
$taskManaged = Join-Path $GameDirectory 'Clone Drone in the Danger Zone_Data\Managed'
$taskCompiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskOutput = Join-Path $PSScriptRoot 'dist\NavalPrototype'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskReferences = Get-ChildItem -LiteralPath $taskManaged -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$taskManifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'ModInfo.json') -Raw | ConvertFrom-Json
$taskSources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
# A response file avoids Windows' command-length limit as the mod grows.
$taskResponse = Join-Path $taskOutput 'compile.rsp'
$taskArguments = @('/nologo','/target:library','/nostdlib','/optimize+',('/out:"' + (Join-Path $taskOutput $taskManifest.MainDLLFileName) + '"'))
$taskArguments += $taskReferences
$taskArguments += $taskSources | ForEach-Object { '"' + $_ + '"' }
try {
    [System.IO.File]::WriteAllLines($taskResponse, [string[]]$taskArguments)
    & $taskCompiler /noconfig ('@' + $taskResponse)
    if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
} finally {
    Remove-Item -LiteralPath $taskResponse -ErrorAction SilentlyContinue
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ModInfo.json') -Destination $taskOutput
Write-Output "Built: $taskOutput"
