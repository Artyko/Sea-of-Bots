param(
    [Parameter(Mandatory=$true)][string]$Test,
    [string]$GameDirectory = 'E:\SteamLibrary\steamapps\common\Clone Drone in the Danger Zone'
)
$ErrorActionPreference = 'Stop'
$taskManaged = Join-Path $GameDirectory 'Clone Drone in the Danger Zone_Data\Managed'
$taskCompiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$taskTestSource = Join-Path (Join-Path $PSScriptRoot 'tests') ($Test + '.cs')
if (-not (Test-Path -LiteralPath $taskTestSource)) { throw "Unknown test: $Test" }
$taskOutput = Join-Path $PSScriptRoot 'dist\test-run'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskAssembly = Join-Path $taskOutput ($Test + '.dll')
$taskReferences = Get-ChildItem -LiteralPath $taskManaged -Filter '*.dll' | ForEach-Object { '/reference:"' + $_.FullName + '"' }
$taskSources = Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object FullName
$taskResponse = Join-Path $taskOutput ($Test + '.rsp')
$taskArguments = @('/nologo','/codepage:65001','/nostdlib','/target:library',('/out:"' + $taskAssembly + '"'))
$taskArguments += $taskReferences
$taskArguments += $taskSources | ForEach-Object { '"' + $_ + '"' }
$taskArguments += '"' + $taskTestSource + '"'
try {
    [System.IO.File]::WriteAllLines($taskResponse, [string[]]$taskArguments)
    & $taskCompiler /noconfig ('@' + $taskResponse)
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
} finally {
    Remove-Item -LiteralPath $taskResponse -ErrorAction SilentlyContinue
}
$env:NAVAL_GAME_MANAGED = $taskManaged
$taskExit = 0
try {
    $taskPowerShell = Join-Path $PSHOME 'powershell.exe'
    & $taskPowerShell -NoProfile -File (Join-Path $PSScriptRoot 'run-test-assembly.ps1') -AssemblyPath $taskAssembly -TestType $Test -ManagedDirectory $taskManaged
    $taskExit = $LASTEXITCODE
} finally {
    # Tests are loaded by a short-lived PowerShell child from transient
    # libraries. No unsigned test executables are created, avoiding heuristic
    # antivirus execution blocks; the child exit releases the DLL for cleanup.
    for ($taskAttempt = 0; $taskAttempt -lt 5 -and (Test-Path -LiteralPath $taskAssembly); $taskAttempt++) {
        Remove-Item -LiteralPath $taskAssembly -Force -ErrorAction SilentlyContinue
        if (Test-Path -LiteralPath $taskAssembly) { Start-Sleep -Milliseconds 200 }
    }
}
if ($taskExit -ne 0) { throw "$Test failed with exit code $taskExit" }
