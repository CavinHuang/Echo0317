<#
Validate a genuine Unity Build Automation Windows artifact on a Windows 10/11 PC.
This DOES NOT generate Unity binaries or spoof a license. It consumes the ZIP
actually downloaded from the Unity Cloud dashboard.

Example:
  .\Tools\verify-cloud-player-windows.ps1 -ArtifactZip "$env:USERPROFILE\Downloads\Echo0317-Windows-Internal-Alpha.zip"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactZip,
    [int]$TimeoutSeconds = 180
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builds = Join-Path $root 'Builds'
$destination = Join-Path $builds 'Windows'
$extraction = Join-Path $builds 'CloudDownloadStaging'
$evidence = Join-Path $builds 'unity-cloud-windows-evidence.json'
New-Item -ItemType Directory -Force -Path $builds | Out-Null
$stage = 'extract'
$pass = $false

try {
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
        throw 'The compiled Unity Windows Player QA must run on Windows.'
    }
    $zip = (Resolve-Path -LiteralPath $ArtifactZip -ErrorAction Stop).Path
    if (-not $zip.EndsWith('.zip', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Expected an actual Unity Cloud Build ZIP, not a source archive.'
    }
    foreach ($dir in @($extraction,$destination)) {
        if (Test-Path -LiteralPath $dir) { Remove-Item -LiteralPath $dir -Force -Recurse }
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }
    Expand-Archive -LiteralPath $zip -DestinationPath $extraction -Force

    $stage = 'find-native-player'
    $candidates = @(Get-ChildItem -LiteralPath $extraction -File -Filter '*.exe' -Recurse |
        Where-Object {
            $directory = $_.DirectoryName
            $basename = [IO.Path]::GetFileNameWithoutExtension($_.Name)
            (Test-Path -LiteralPath (Join-Path $directory 'UnityPlayer.dll') -PathType Leaf) -and
            (Test-Path -LiteralPath (Join-Path $directory ($basename + '_Data')) -PathType Container)
        })
    if ($candidates.Count -ne 1) {
        throw "Need exactly one REAL Unity Windows Player (exe + UnityPlayer.dll + matching _Data): found $($candidates.Count)"
    }
    $player = $candidates[0]
    $source = $player.DirectoryName
    $oldBase = [IO.Path]::GetFileNameWithoutExtension($player.Name)
    Copy-Item -Path (Join-Path $source '*') -Destination $destination -Recurse -Force

    # Unity Cloud may use PlayerSettings.productName as its executable name.
    # Normalize BOTH EXE and *_Data together to match our existing native QA.
    $oldExe = Join-Path $destination $player.Name
    $oldData = Join-Path $destination ($oldBase + '_Data')
    if ($player.Name -ne 'Echo0317.exe') {
        Rename-Item -LiteralPath $oldExe -NewName 'Echo0317.exe'
        Rename-Item -LiteralPath $oldData -NewName 'Echo0317_Data'
    }
    $exe = Join-Path $destination 'Echo0317.exe'
    $data = Join-Path $destination 'Echo0317_Data'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $destination 'UnityPlayer.dll') -PathType Leaf) -or
        -not (Test-Path -LiteralPath (Join-Path $data 'StreamingAssets\Data\story.json') -PathType Leaf)) {
        throw 'Downloaded artifact is missing a complete Unity Windows x64 Player.'
    }

    $stage = 'native-compiled-player-qa'
    & (Join-Path $PSScriptRoot 'qa-native-windows.ps1') -TimeoutSeconds $TimeoutSeconds
    if ($LASTEXITCODE -ne 0) { throw "Native QA returned exit code $LASTEXITCODE" }

    $stage = 'verify-package'
    & python (Join-Path $PSScriptRoot 'pack-hosted-alpha.py') --root $root
    if ($LASTEXITCODE -ne 0) { throw "Actual Windows Player/package validation returned $LASTEXITCODE" }
    $pass = $true
    Write-Host 'PASS: Unity Cloud Windows EXE executed, four endings verified, authentic binary packaged.'
    Write-Host ('Windows Alpha ZIP: ' + (Join-Path $builds 'Echo0317-Windows-x64-internal-hosted.zip'))
    Write-Host 'PENDING: interactive video, audio and input GUI smoke on a live desktop.'
}
catch {
    Write-Error "BLOCKED [$stage]: $($_.Exception.Message)"
}
finally {
    $result = [ordered]@{
        schema = 'echo0317-stage28-cloud-player-v1'
        result = $(if ($pass) { 'pass' } else { 'fail' })
        stage = $stage
        compiledWindowsPlayerValidated = $pass
        interactiveDesktopVideoAudioValidated = $false
        timestampUtc = [DateTime]::UtcNow.ToString('o')
    }
    $result | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $evidence -Encoding UTF8
}
if (-not $pass) { exit 1 }
exit 0
