<#
Stage 27: prepare the legitimate Unity Windows 6.3 build path.
No password, serial, license text, GitHub token, or local entitlement is
ever read into this script or written to its diagnostic report.

Personal: sign in and activate once through Unity Hub on this Windows user.
Pro/Enterprise: use the license assigned to this machine/account.

Examples:
  .\Tools\prepare-unity-windows.ps1
  .\Tools\prepare-unity-windows.ps1 -OpenHub
  .\Tools\prepare-unity-windows.ps1 -VerifyEditor
  .\Tools\prepare-unity-windows.ps1 -BuildAlpha
  .\Tools\prepare-unity-windows.ps1 -BuildAlpha -SkipInteractiveSmoke
#>
[CmdletBinding()]
param(
    [string]$UnityPath = $env:UNITY_EDITOR_PATH,
    [switch]$OpenHub,
    [switch]$VerifyEditor,
    [switch]$BuildAlpha,
    [switch]$SkipInteractiveSmoke
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builds = Join-Path $project 'Builds'
New-Item -ItemType Directory -Force -Path $builds | Out-Null
$reportPath = Join-Path $builds 'unity-local-activation-preflight.json'
$report = [ordered]@{
    schema = 'echo0317-stage27-local-activation-v1'
    utc = [DateTime]::UtcNow.ToString('o')
    status = 'blocked'
    projectVersion = '6000.3.2f1'
    platform = 'Windows'
    hubInstalled = $false
    editorInstalled = $false
    editorVersionMatchesProject = $false
    windowsBuildSupportInstalled = $false
    pythonAvailable = $false
    accountActivationVerifiedByEditor = $false
    nativeExeBuilt = $false
    details = @()
}

function Add-Detail([string]$message) {
    $report.details = @($report.details) + @($message)
    Write-Host $message
}

try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw 'Windows Alpha 要求 Windows 10/11 或 Windows Runner。'
    }

    $hubCandidates = @(
        (Join-Path $env:ProgramFiles 'Unity Hub\Unity Hub.exe'),
        (Join-Path $env:ProgramFiles 'Unity\Hub\Unity Hub.exe'),
        (Join-Path $env:LOCALAPPDATA 'Programs\Unity Hub\Unity Hub.exe')
    )
    $unityHub = @($hubCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1)
    $report.hubInstalled = ($unityHub.Count -gt 0)

    if (-not $UnityPath) {
        $installed = @(Get-ChildItem 'C:\Program Files\Unity\Hub\Editor\6000.3.*\Editor\Unity.exe' -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending)
        if ($installed.Count -gt 0) { $UnityPath = $installed[0].FullName }
    }
    $report.editorInstalled = [bool]($UnityPath -and (Test-Path -LiteralPath $UnityPath -PathType Leaf))
    if ($report.editorInstalled) {
        $normalized = $UnityPath.Replace('/', '\')
        $report.editorVersionMatchesProject =
            ($normalized -match '\\6000\.3\.2f1\\Editor\\Unity\.exe$')
        $editorDir = Split-Path $UnityPath -Parent
        $report.windowsBuildSupportInstalled = Test-Path -LiteralPath (
            Join-Path $editorDir 'Data\PlaybackEngines\WindowsStandaloneSupport')
    }
    $report.pythonAvailable = [bool](Get-Command python -ErrorAction SilentlyContinue)
    if (-not $report.hubInstalled) {
        Add-Detail '未找到 Unity Hub。Unity Personal 请先从 https://unity.com/download 安装官方 Hub。'
    }
    if (-not $report.editorInstalled) {
        Add-Detail '未找到 Unity 6000.3.2f1 Editor。请通过 Unity Hub 安装精确版本。'
    } elseif (-not $report.editorVersionMatchesProject) {
        Add-Detail "已找到 Editor，但版本不是项目锁定的 6000.3.2f1：$UnityPath"
    }
    if (-not $report.windowsBuildSupportInstalled) {
        Add-Detail '未找到 Windows Build Support (IL2CPP/Mono)。请在 Unity Hub 的 Editor 模块中添加 Windows Build Support。'
    }
    if (-not $report.pythonAvailable) { Add-Detail '未找到 Python 3.11+。请安装以运行项目验收脚本。' }

    if ($OpenHub) {
        if ($unityHub.Count -eq 0) {
            Start-Process 'https://unity.com/download'
        } else {
            Start-Process -FilePath $unityHub[0]
        }
        Add-Detail 'Unity Personal：请在 Hub 中登录自己的 Unity ID，然后在 Licenses 页面添加免费的 Personal 许可。'
    }

    $ready = $report.editorInstalled -and $report.editorVersionMatchesProject -and
        $report.windowsBuildSupportInstalled -and $report.pythonAvailable
    if (-not $ready) {
        throw '本地 Unity 前置条件尚未满足。请先完成上面的安装检查。'
    }
    $env:UNITY_EDITOR_PATH = $UnityPath
    if ($VerifyEditor -or $BuildAlpha) {
        # This actually launches Unity and exercises the license/Editor.
        # File presence alone is NOT a valid activation check.
        & (Join-Path $PSScriptRoot 'verify-unity-import.ps1') -UnityPath $UnityPath
        if ($LASTEXITCODE -ne 0) { throw "Unity 验证进程失败 ($LASTEXITCODE)" }
        $report.accountActivationVerifiedByEditor = $true
        Add-Detail 'Unity Editor 已实际运行、导入工程并生成了原生验证报告。'
    }

    if ($BuildAlpha) {
        & (Join-Path $PSScriptRoot 'build-alpha-windows.ps1') -UnityPath $UnityPath -SkipInteractiveSmoke:$SkipInteractiveSmoke
        if ($LASTEXITCODE -ne 0) { throw "Windows Alpha 构建进程失败 ($LASTEXITCODE)" }
        $exe = Join-Path $project 'Builds\Windows\Echo0317.exe'
        if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) {
            throw '构建脚本未输出真正的 Windows EXE。'
        }
        $report.nativeExeBuilt = $true
        Add-Detail '真正的 Unity Windows EXE 已通过现有的原生构建和路径自检。'
    }

    $report.status = if ($BuildAlpha) { 'alpha-built' } elseif ($VerifyEditor) {
        'editor-import-verified'
    } else { 'prerequisites-ready-activation-not-tested' }
    Add-Detail "状态：$($report.status)。单纯安装检查不代表许可证已经通过 Unity 激活。"
}
catch {
    Add-Detail "阻塞：$($_.Exception.Message)"
    $report.status = 'blocked'
}
finally {
    # Diagnostics intentionally exclude account names, license payloads and tokens.
    $report | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath $reportPath -Encoding UTF8
    Write-Host "非敏感诊断：$reportPath"
}
if ($report.status -eq 'blocked') { exit 1 }
exit 0
