$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ('wii-installer-test-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testRoot | Out-Null
try {
    [IO.File]::WriteAllText((Join-Path $testRoot 'WiiAnimeEncoder.exe'),'fixture')
    $existing = Join-Path $testRoot 'HandBrakeCLI.exe'
    [IO.File]::WriteAllText($existing,'existing encoder must survive a failed download')
    $before = (Get-FileHash -LiteralPath $existing).Hash
    function Invoke-WebRequest {
        param($Uri,$OutFile,[switch]$UseBasicParsing,$TimeoutSec)
        [IO.File]::WriteAllText($OutFile,'corrupt download')
    }
    $failed = $false
    try { & (Join-Path $root 'scripts\Install-HandBrake.ps1') -ApplicationDirectory $testRoot }
    catch {
        if ($_.Exception.Message -notlike 'Download verification failed*') { throw }
        $failed = $true
    }
    if (-not $failed) { throw 'Corrupt download was accepted.' }
    if ((Get-FileHash -LiteralPath $existing).Hash -ne $before) { throw 'Existing encoder changed after failed verification.' }
    if (@(Get-ChildItem -LiteralPath $testRoot -Filter '.handbrake-*.tmp' -Force).Count) { throw 'Staged file was left behind.' }
    'PASS: corrupt downloads are rejected without replacing the existing encoder.'
} finally {
    $resolved = [IO.Path]::GetFullPath($testRoot)
    $temp = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
    if ([IO.Path]::GetDirectoryName($resolved) -eq $temp -and [IO.Path]::GetFileName($resolved).StartsWith('wii-installer-test-')) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
