param(
    [string] $UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.26f1\Editor\Unity.exe',
    [switch] $BuildFromSceneList
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if (!(Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) { throw 'No se encontró Unity; indicá -UnityEditorPath.' }
if (Get-Process -Name Unity -ErrorAction SilentlyContinue) { throw 'Guardá y cerrá Unity antes de ejecutar la validación batch.' }
$lockPath = Join-Path $projectRoot 'Temp\UnityLockfile'
if (Test-Path -LiteralPath $lockPath) {
    try {
        $lockProbe = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::None)
        $lockProbe.Dispose()
    } catch { throw 'UnityLockfile está en uso o no es accesible; no se inicia otra instancia.' }
    Write-Output 'Lockfile residual sin bloqueo: se conserva y Unity comprobará el acceso al proyecto.'
}
if ($BuildFromSceneList -and (Get-Process -Name SoulslikeCombatPrototype -ErrorAction SilentlyContinue)) { throw 'Cerrá el juego antes de compilar.' }
$method = if ($BuildFromSceneList) { 'PrototypeInputValidation.ValidateAndBuild' } else { 'PrototypeInputValidation.Validate' }
$logName = if ($BuildFromSceneList) { 'ProjectValidationBuild.log' } else { 'ProjectValidation.log' }
$logDirectory = Join-Path $projectRoot 'Logs'
[IO.Directory]::CreateDirectory($logDirectory) | Out-Null
$logPath = Join-Path $logDirectory $logName
$unityArguments = @('-batchmode','-nographics','-projectPath',('"' + $projectRoot + '"'),'-executeMethod',$method,'-logFile',('"' + $logPath + '"'))
$physicsPath = Join-Path $projectRoot 'ProjectSettings\DynamicsManager.asset'
$physicsSnapshot = [IO.File]::ReadAllBytes($physicsPath)
try {
    $unityProcess = Start-Process -FilePath $UnityEditorPath -ArgumentList $unityArguments -WorkingDirectory $projectRoot -WindowStyle Hidden -PassThru -Wait
} finally {
    # La prueba restaura SimulationMode, pero Unity reserializa ajustes antiguos al tocarlos.
    # El editor de prueba ya terminó; conservamos exactamente el archivo previo a la ejecución.
    [IO.File]::WriteAllBytes($physicsPath, $physicsSnapshot)
}
if ($unityProcess.ExitCode -ne 0) { throw ('Unity terminó con código ' + $unityProcess.ExitCode + '. Log: ' + $logPath) }
$logText = [IO.File]::ReadAllText($logPath)
foreach ($marker in @('H1_VALIDATION_SUCCEEDED','H1_VISUAL_VALIDATION_SUCCEEDED','DUEL_VALIDATION_SUCCEEDED','DODGE_VALIDATION_SUCCEEDED','INPUT_VALIDATION_SUCCEEDED','PROJECT_VALIDATION_SUCCEEDED')) {
    if (!$logText.Contains($marker)) { throw ('Falta confirmar ' + $marker + '. Log: ' + $logPath) }
}
if ($BuildFromSceneList -and !$logText.Contains('SCENE_LIST_BUILD_SUCCEEDED')) { throw ('No se confirmó la build. Log: ' + $logPath) }
Write-Output ('Validación aprobada. Log: ' + $logPath)
if ($BuildFromSceneList) { Write-Output ('Ejecutable de prueba: ' + (Join-Path $projectRoot 'Builds\WindowsSceneList\SoulslikeCombatPrototype.exe')) }
