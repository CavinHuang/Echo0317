# Run the COMPILED Unity player in headless diagnostics mode, not the Editor.
# The player itself replays all four endings and performs a save/load roundtrip.
param([int]$TimeoutSeconds=90)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$exe=Join-Path $root 'Builds/Windows/Echo0317.exe'
if (-not (Test-Path $exe)) { throw '未找到 Unity 编译生成的 Windows EXE。不能运行原生 QA。' }
$builds=Join-Path $root 'Builds'
$report=Join-Path $builds 'native-game-qa.json'
$log=Join-Path $builds 'native-game-qa.log'
foreach ($file in @($report,$log)) {if (Test-Path $file) {Remove-Item $file -Force}}
$arguments=@('-batchmode','-nographics','-echoQa','-echoQaReport',('"'+$report+'"'),'-logFile',('"'+$log+'"'))
$process=Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
try {
    if (-not $process.WaitForExit($TimeoutSeconds*1000)) {
        throw "Unity 原生 QA 超时（${TimeoutSeconds}s），见 $log"
    }
    $process.Refresh()
    if ($process.ExitCode -ne 0) {
        if (Test-Path $log) {Get-Content $log -Tail 100}
        throw "Unity Player 原生自检失败：ExitCode=$($process.ExitCode)"
    }
    if (-not (Test-Path $report)) {throw 'Unity Player 未写出诊断 JSON；不能认定通过'}
    if (-not (Test-Path $log)) {throw 'Unity Player 未写出运行日志；不能认定通过'}
    if (-not ((Get-Content $log -Raw) -match 'ECHO_NATIVE_QA_PASS')) {
        throw '缺少 Unity Player 发出的 ECHO_NATIVE_QA_PASS 标记'
    }
    python (Join-Path $PSScriptRoot 'check-native-qa.py') $report
    if ($LASTEXITCODE -ne 0) {throw '原生 QA JSON 未通过结果验证'}
    Write-Output 'PASS: Compiled Windows Unity player replayed four endings and verified save integrity.'
} finally {
    $process.Refresh()
    if (-not $process.HasExited) {Stop-Process -Id $process.Id -Force}
}
