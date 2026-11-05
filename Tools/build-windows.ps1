param([string]$UnityPath=$env:UNITY_EDITOR_PATH, [switch]$SkipTests, [switch]$Release)
$ErrorActionPreference = 'Stop'
if (-not $UnityPath -or -not (Test-Path $UnityPath)) { throw '请将 UNITY_EDITOR_PATH 设为 Unity 6.3 LTS 的 Unity.exe 路径' }
if ($Release -and $SkipTests) { throw '正式 Release 构建不能使用 -SkipTests 绕过原生测试' }
$project = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builds = Join-Path $project 'Builds'
$folder = Join-Path $builds 'Windows'
$env:ECHO_BUILD_PATH = Join-Path $folder 'Echo0317.exe'
$env:ECHO_RELEASE_PROFILE = if ($Release) {'release'} else {'internal'}
$log = Join-Path $builds 'unity-build.log'
New-Item -ItemType Directory -Force -Path $folder | Out-Null
python (Join-Path $project 'Tests/test_export.py')
if ($LASTEXITCODE -ne 0) { throw '剧情数据验证失败，停止 Windows 构建' }
python (Join-Path $project 'Tests/test_release_assets.py')
if ($LASTEXITCODE -ne 0) { throw '发布素材预检查失败，停止 Windows 构建' }
python (Join-Path $project 'Tests/test_release_gate.py') --profile $env:ECHO_RELEASE_PROFILE
if ($LASTEXITCODE -ne 0) { throw '发行镜头审核门槛未通过，停止 Windows 构建' }
# Stage 15 requires the full 69,497-state soft-lock audit before any
# Windows build, even for an internal Alpha that skips Unity test execution.
python -m unittest discover -s Tests -p 'test_stage15_*.py' -q
if ($LASTEXITCODE -ne 0) { throw 'Stage 15 剧情死路与交互安全验证失败' }
if (-not $SkipTests) { & (Join-Path $PSScriptRoot 'test-unity.ps1') -UnityPath $UnityPath }
# A stale DLL or media file from an earlier build can make an incomplete new
# build look valid. Always package a clean output directory.
if (Test-Path $folder) { Remove-Item $folder -Recurse -Force }
New-Item -ItemType Directory -Force -Path $folder | Out-Null
& $UnityPath -batchmode -quit -nographics -projectPath $project -executeMethod Echo0317.Editor.EchoBuild.BuildWindows -logFile $log
if ($LASTEXITCODE -ne 0) {
    if(Test-Path $log) { Get-Content $log -Tail 100 }
    throw "Unity build failed: $LASTEXITCODE"
}
$dataFolder=Join-Path $folder 'Echo0317_Data'
if (-not (Test-Path $env:ECHO_BUILD_PATH) -or -not (Test-Path $dataFolder)) {
    throw '缺少 EXE 或 Unity _Data 目录，不能发布不完整的 Windows 构建'
}
$files=Get-ChildItem $folder -File -Recurse
if ($files.Count -lt 3) {throw '构建文件数量异常'}
$playerDll = Join-Path $folder 'UnityPlayer.dll'
if (-not (Test-Path $playerDll)) { throw '缺少 UnityPlayer.dll，不能发布不完整的 Unity 游戏' }
$streamingAssets = Join-Path $dataFolder 'StreamingAssets'
foreach($relative in @('Data/story.json','Data/cases.json','Data/shots.json','Data/release-required-shots.json')) {
    if (-not (Test-Path (Join-Path $streamingAssets $relative))) { throw "构建缺少关键资源: $relative" }
}
# Stage 13: this gate runs the COMPILED Windows player, not only Editor tests.
& (Join-Path $PSScriptRoot 'qa-native-windows.ps1')
$profileStamp = Join-Path $folder 'ECHO_BUILD_PROFILE.txt'
$profileText = if($Release) {
    'RELEASE CONTENT GATE PASSED - platform review and device QA still required'
} else {
    'INTERNAL ALPHA - PREVIS / PLACEHOLDER FILMS - NOT FOR PUBLIC RELEASE'
}
Set-Content -Path $profileStamp -Value $profileText -Encoding UTF8
$files=Get-ChildItem $folder -File -Recurse
$manifest=Join-Path $builds 'windows-build-sha256.txt'
$files | ForEach-Object {
    $hash=(Get-FileHash $_.FullName -Algorithm SHA256).Hash
    $relative=$_.FullName.Substring($folder.Length+1).Replace('\','/')
    "$hash  $relative"
} | Set-Content $manifest -Encoding UTF8
$archive=Join-Path $builds ("Echo0317-Windows-x64-" + $env:ECHO_RELEASE_PROFILE + ".zip")
if (Test-Path $archive) { Remove-Item $archive -Force }
Compress-Archive -Path (Join-Path $folder '*') -DestinationPath $archive -CompressionLevel Optimal
Write-Output "Windows x64: $env:ECHO_BUILD_PATH"
Write-Output "Distribution ZIP: $archive"
Write-Output "SHA256 manifest: $manifest"
