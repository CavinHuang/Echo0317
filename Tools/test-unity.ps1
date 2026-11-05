param([string]$UnityPath=$env:UNITY_EDITOR_PATH)
$ErrorActionPreference='Stop'
if (-not $UnityPath -or -not (Test-Path $UnityPath)) {throw '必须提供有效的 Unity 6.3 LTS Editor 路径'}
$project=(Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builds=Join-Path $project 'Builds'
New-Item -ItemType Directory -Force -Path $builds | Out-Null
& (Join-Path $PSScriptRoot 'verify-unity-import.ps1') -UnityPath $UnityPath
foreach($kind in @('EditMode','PlayMode')) {
    $results=Join-Path $builds ("$kind-results.xml")
    $log=Join-Path $builds ("$kind-test.log")
    if (Test-Path $results) { Remove-Item $results -Force }
    & $UnityPath -batchmode -quit -projectPath $project -runTests -testPlatform $kind -testResults $results -logFile $log
    if($LASTEXITCODE -ne 0) {
        if(Test-Path $log) { Get-Content $log -Tail 100 }
        throw "Unity $kind tests failed: $LASTEXITCODE"
    }
    if (-not (Test-Path $results)) { throw "Unity $kind tests returned without XML results" }
    [xml]$xml=Get-Content $results -Raw
    $summary=$xml.'test-run'
    if (-not $summary) { throw "Unity $kind results missing test-run element" }
    $minimum = if ($kind -eq 'EditMode') { 5 } else { 2 }
    if ([int]$summary.failed -gt 0 -or [int]$summary.passed -lt $minimum) {
        throw "Unity $kind results contain failures or no tests (passed=$($summary.passed), failed=$($summary.failed))"
    }
    Write-Output "$kind: $($summary.passed) passed; $($summary.failed) failed; results=$results"
}
