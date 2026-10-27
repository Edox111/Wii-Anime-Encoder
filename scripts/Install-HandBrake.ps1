param([string]$ApplicationDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
if (-not [Environment]::Is64BitOperatingSystem) { throw 'Windows x64 is required.' }
$targetDirectory = [IO.Path]::GetFullPath($ApplicationDirectory)
if (-not (Test-Path -LiteralPath (Join-Path $targetDirectory 'WiiAnimeEncoder.exe'))) {
    throw 'Extract the complete application ZIP before running setup.'
}
$binary = Join-Path $targetDirectory 'HandBrakeCLI.exe'
$binaryHash = '658509a4ab1b4b96d2b812cb23cc1c9fc2ff0fd93b65da143a781429b2b87202'
if ((Test-Path -LiteralPath $binary) -and
    (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -eq $binaryHash) {
    Write-Host 'HandBrakeCLI 1.9.2 is already installed and verified.'
    return
}
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$temporary = Join-Path $temporaryRoot ('wii-encoder-setup-' + [guid]::NewGuid().ToString('N'))
$stagedBinary = Join-Path $targetDirectory ('.handbrake-' + [guid]::NewGuid().ToString('N') + '.tmp')
New-Item -ItemType Directory -Path $temporary | Out-Null
try {
    Write-Host 'Downloading official HandBrakeCLI 1.9.2 from GitHub...'
    $archive = Join-Path $temporary 'HandBrakeCLI.zip'
    $uri = 'https://github.com/HandBrake/HandBrake/releases/download/1.9.2/HandBrakeCLI-1.9.2-win-x86_64.zip'
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri $uri -OutFile $archive -UseBasicParsing -TimeoutSec 180
    $archiveHash = 'cc875eda177a4105b99664136719c893db989a4456c4d3a9fc9e8d8742018413'
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $archiveHash) {
        throw 'Download verification failed. The existing installation was not changed.'
    }
    Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $temporary 'unpacked')
    $binaries = @(Get-ChildItem -LiteralPath (Join-Path $temporary 'unpacked') -Filter HandBrakeCLI.exe -Recurse)
    if ($binaries.Count -ne 1 -or
        (Get-FileHash -LiteralPath $binaries[0].FullName -Algorithm SHA256).Hash -ne $binaryHash) {
        throw 'The downloaded encoder did not match the verified release.'
    }
    Copy-Item -LiteralPath $binaries[0].FullName -Destination $stagedBinary
    # Stage beside the destination so replacement stays on the same filesystem.
    if (Test-Path -LiteralPath $binary) {
        [IO.File]::Replace($stagedBinary, $binary, $null)
    } else {
        [IO.File]::Move($stagedBinary, $binary)
    }
    Write-Host 'Setup complete. Open WiiAnimeEncoder.exe.'
} finally {
    if (Test-Path -LiteralPath $stagedBinary) { Remove-Item -LiteralPath $stagedBinary -Force }
    if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($temporary)) -eq $temporaryRoot -and
        [IO.Path]::GetFileName($temporary).StartsWith('wii-encoder-setup-')) {
        Remove-Item -LiteralPath $temporary -Recurse -Force
    }
}
