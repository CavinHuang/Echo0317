# Manual, interactive Windows 11 smoke run after a successful Unity build.
# Must run in a real desktop user session (not a headless CI service).
param([int]$Seconds=12)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$exe=Join-Path $root 'Builds/Windows/Echo0317.exe'
if (-not (Test-Path $exe)) { throw '没有经过 Unity 编译的 Echo0317.exe，无法运行本机冒烟检查' }
$log=Join-Path $root 'Builds/player-smoke.log'
if (Test-Path $log) {Remove-Item $log -Force}
$oldStamp=Join-Path $root 'Builds/player-smoke-success.json'
if (Test-Path $oldStamp) {Remove-Item $oldStamp -Force}
$process=Start-Process -FilePath $exe -ArgumentList @('-logFile',('"'+$log+'"')) -PassThru
try {
    Start-Sleep -Seconds $Seconds
    $process.Refresh()
    if ($process.HasExited) {throw "游戏启动后过早退出。ExitCode=$($process.ExitCode)"}
    if (-not (Test-Path $log)) {throw '未生成 Player.log；不能判定启动成功'}
    $contents=Get-Content $log -Raw
    if ($contents -match 'NullReferenceException|TypeLoadException|MissingMethodException|Failed to initialize') {
        throw 'Player.log 出现可能致命的异常，请先检查日志'
    }
    if ($contents -notmatch 'ECHO_GAME_READY') {
        throw '游戏进程尚未报告主菜单与资源初始化完成；不能认定启动成功'
    }
    # Only the live GUI process can create this success stamp; remove old stamps
    # before starting a new run so stale evidence can never pass.
    $stamp=Join-Path $root 'Builds/player-smoke-success.json'
    $result=[ordered]@{
        schema='echo0317-interactive-smoke-v1'
        result='pass'
        checkedAtUtc=[DateTime]::UtcNow.ToString('o')
        exe=$exe
        log=$log
        marker='ECHO_GAME_READY'
    }
    $result | ConvertTo-Json | Set-Content -Path $stamp -Encoding UTF8
    Write-Output 'PASS: game process alive, menu initialized and log scan clean.'
    Write-Output 'NOTE: this does not replace manual UI, video/audio or four-ending QA.'
} finally {
    $process.Refresh()
    if (-not $process.HasExited) {Stop-Process -Id $process.Id -Force}
}
