<#
One-command Windows Unity Alpha build and REAL smoke test.
Run in an interactive Windows 11 desktop session with Unity 6.3 LTS activated:
  powershell -NoProfile -File .\Tools\build-alpha-windows.ps1
Fail closed: no EXE or release claim when prerequisites/tests are absent.
#>
param(
    [string]$UnityPath = $env:UNITY_EDITOR_PATH,
    [switch]$SkipInteractiveSmoke
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builds = Join-Path $project 'Builds'
New-Item -ItemType Directory -Force -Path $builds | Out-Null
$evidence = Join-Path $builds 'stage16-alpha-evidence.json'
$smokeMark = Join-Path $builds 'player-smoke-success.json'
if (Test-Path $evidence) { Remove-Item $evidence -Force }
if (Test-Path $smokeMark) { Remove-Item $smokeMark -Force }
$phase = 'prerequisites'
$started = [DateTime]::UtcNow.ToString('o')
try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw '原生 Windows 构建必须在 Windows 系统执行。当前机器不能直接生成 Unity Windows EXE。'
    }
    if (-not $UnityPath) {
        $installed = @(Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\6000.3.*\Editor\Unity.exe' -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending)
        if ($installed.Count -gt 0) { $UnityPath = $installed[0].FullName }
    }
    if (-not $UnityPath -or -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
        throw '没有找到已安装的 Unity 6.3 LTS Editor。安装并激活 Unity 6.3 LTS（附 Windows Build Support），或设置 UNITY_EDITOR_PATH。'
    }
    if (-not $UnityPath.ToLowerInvariant().Contains('6000.3.') -and -not $env:UNITY_EDITOR_PATH) {
        throw "Editor 路径未匹配 6000.3.x：$UnityPath"
    }
    $editorDir = Split-Path $UnityPath -Parent
    $windowsSupport = Join-Path $editorDir 'Data\PlaybackEngines\WindowsStandaloneSupport'
    if (-not (Test-Path $windowsSupport)) {
        throw "Unity 缺少 Windows Standalone Build Support：$windowsSupport。请在 Unity Hub 中补装。"
    }
    if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
        throw '未检测到 Python 3。Windows 构建需要 Python 3.11+ 执行导出数据和原生报告验证。'
    }
    $env:UNITY_EDITOR_PATH = $UnityPath
    Write-Host "Unity Windows Alpha editor: $UnityPath"
    $phase = 'offline-preflight'
    & python (Join-Path $project 'Tools/doctor.py')
    if ($LASTEXITCODE -ne 0) { throw "离线项目诊断失败 ($LASTEXITCODE)" }
    if (Get-Command dotnet -ErrorAction SilentlyContinue) {
        $phase = 'actual-csharp-core-host'
        & dotnet run --project (Join-Path $project 'Tools/CoreHost/Echo0317.CoreHost.csproj') --configuration Release -- $project
        if ($LASTEXITCODE -ne 0) { throw "独立 C# 核心编译/执行失败 ($LASTEXITCODE)" }
    } else {
        Write-Warning '没有 .NET 8 SDK，将跳过独立 C# 核心测试；Unity EditMode/PlayMode 仍必须通过。'
    }
    $phase = 'unity-editmode-playmode'
    & (Join-Path $PSScriptRoot 'test-unity.ps1') -UnityPath $UnityPath
    $phase = 'build-and-native-player-qa'
    # SkipTests is safe here only because test-unity.ps1 just succeeded.
    & (Join-Path $PSScriptRoot 'build-windows.ps1') -UnityPath $UnityPath -SkipTests
    $phase = 'interactive-player-smoke'
    if (-not $SkipInteractiveSmoke) {
        & (Join-Path $PSScriptRoot 'smoke-windows.ps1') -Seconds 15
        if (-not (Test-Path $smokeMark)) { throw 'Windows Player 未写出真正的交互启动成功标记。' }
    } else {
        Write-Warning '没有执行交互式主菜单冒烟检查；不可标记为已通过 Windows 桌面启动验收。'
    }
    $phase = 'verified-evidence'
    if ($SkipInteractiveSmoke) {
        & python (Join-Path $PSScriptRoot 'alpha-evidence.py') --root $project
    } else {
        & python (Join-Path $PSScriptRoot 'alpha-evidence.py') --root $project --require-smoke
    }
    if ($LASTEXITCODE -ne 0) { throw '原生结果验收报告失败' }
    Write-Host 'PASS: Windows native Unity alpha build and compiled-player path QA.'
    if ($SkipInteractiveSmoke) { Write-Host 'NOTICE: interactive launch NOT verified.' }
    else { Write-Host 'PASS: interactive desktop startup smoke test.' }
    Write-Host "Evidence: $evidence"
}
catch {
    $failure = [ordered]@{
        schema = 'echo0317-stage16-alpha-failure-v1'
        result = 'fail'
        phase = $phase
        startedUtc = $started
        endedUtc = [DateTime]::UtcNow.ToString('o')
        error = $_.Exception.Message
        nativePlayerVerified = $false
    }
    $failure | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $evidence -Encoding UTF8
    Write-Error "Windows Alpha failed [$phase]: $($_.Exception.Message)"
    exit 1
}
