$ErrorActionPreference = 'Stop'
$taskManaged = 'E:\SteamLibrary\steamapps\common\Clone Drone in the Danger Zone\Clone Drone in the Danger Zone_Data\Managed'
Add-Type -Path (Join-Path $taskManaged 'Mono.Cecil.dll')
$taskAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $taskManaged 'Assembly-CSharp.dll'))
$taskContracts = @(
    @('FirstPersonMover','onDeath','System.Void',3),
    @('FirstPersonMover','ReparentCameraAndAnimateForDeath','System.Boolean',1),
    @('GameFlowManager','onCharacterKilled','System.Void',1),
    @('GameFlowManager','LoseGameAndShowUI','System.Void',2),
    @('GameFlowManager','RefreshIsLevelWon','System.Void',0),
    @('GameModeManager','ConsciousnessTransferToKillerEnabled','System.Boolean',0),
    @('GameModeManager','ConsciousnessTransferToAlliesEnabled','System.Boolean',0)
)
foreach ($taskContract in $taskContracts) {
    $taskType = $taskAssembly.MainModule.Types | Where-Object Name -eq $taskContract[0]
    $taskMethods = @($taskType.Methods | Where-Object Name -eq $taskContract[1])
    if ($taskMethods.Count -ne 1 -or $taskMethods[0].ReturnType.FullName -ne $taskContract[2] -or $taskMethods[0].Parameters.Count -ne $taskContract[3]) {
        throw ('Native spectator patch contract changed: ' + $taskContract[0] + '.' + $taskContract[1])
    }
}
$taskAssembly.Dispose()
Write-Output 'PASS: seven installed-game death/spectator patch targets exist with expected signatures (metadata check, no gameplay executed).'
