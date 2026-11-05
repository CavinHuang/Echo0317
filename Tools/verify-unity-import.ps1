param([string]$UnityPath=$env:UNITY_EDITOR_PATH)
$ErrorActionPreference = 'Stop'
if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    throw 'Unity 6.3 LTS Editor 未安装或 UNITY_EDITOR_PATH 无效。无法执行 C# 编译验证。'
}
$project=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builds=Join-Path $project 'Builds'
New-Item -ItemType Directory -Force -Path $builds | Out-Null
$report=Join-Path $builds 'unity-import-verification.json'
$log=Join-Path $builds 'unity-import.log'
if (Test-Path $report) { Remove-Item $report -Force }
& $UnityPath -batchmode -quit -projectPath $project -executeMethod Echo0317.Editor.EchoBuild.VerifyImport -logFile $log
if ($LASTEXITCODE -ne 0) {
    if (Test-Path $log) { Get-Content $log -Tail 120 }
    throw "Unity 原生导入/编译失败：$LASTEXITCODE"
}
if (-not (Test-Path $report)) {
    throw 'Unity 未生成原生导入验证报告；请检查 unity-import.log，不能判定为成功'
}
$result=Get-Content $report -Raw | ConvertFrom-Json
if (-not $result.nativeEditorLoaded -or $result.nodes -ne 55 -or $result.endings -ne 4 -or
    $result.rules -ne 5 -or $result.mediaTasks -ne 43 -or -not $result.unityVersion.StartsWith('6000.3.')) {
    throw 'Unity 编辑器导入报告中的版本、数据或运行状态不符'
}
Write-Output "PASS Unity imported, runtime compiled and content loaded ($($result.unityVersion))."
