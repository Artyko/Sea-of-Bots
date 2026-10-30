param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$TestType,
    [Parameter(Mandatory=$true)][string]$ManagedDirectory
)
$ErrorActionPreference = 'Stop'
$env:NAVAL_GAME_MANAGED = $ManagedDirectory
# Load the colocated game assemblies before the transient test library. A
# PowerShell scriptblock used as AssemblyResolve recursively enters the engine
# for Unity-heavy tests and can overflow the stack.
foreach ($taskDependency in Get-ChildItem -LiteralPath $ManagedDirectory -Filter '*.dll' -File) {
    try { [Reflection.Assembly]::LoadFrom($taskDependency.FullName) | Out-Null } catch { }
}
[Environment]::ExitCode = 0
$taskLoaded = [Reflection.Assembly]::LoadFrom($AssemblyPath)
$taskType = $taskLoaded.GetType($TestType, $true)
$taskFlags = [Reflection.BindingFlags]::Static -bor [Reflection.BindingFlags]::Public -bor [Reflection.BindingFlags]::NonPublic
$taskEntry = $taskType.GetMethod('Main', $taskFlags)
if ($null -eq $taskEntry) { throw "Test has no static Main method: $TestType" }
$taskEntry.Invoke($null, @()) | Out-Null
exit [Environment]::ExitCode
