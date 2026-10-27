$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$out = Join-Path $PSScriptRoot 'dist\Wii-Anime-Encoder'
if (-not (Test-Path -LiteralPath (Join-Path $out 'Wii-Anime-Scarlet.exe'))) {
    throw 'Run build.ps1 first.'
}
$temp = Join-Path ([IO.Path]::GetTempPath()) ('wii-encoder-download-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    $zip = Join-Path $temp 'HandBrakeCLI.zip'
    $uri = 'https://github.com/HandBrake/HandBrake/releases/download/1.9.2/HandBrakeCLI-1.9.2-win-x86_64.zip'
    Invoke-WebRequest -Uri $uri -OutFile $zip -UseBasicParsing
    $expected = 'cc875eda177a4105b99664136719c893db989a4456c4d3a9fc9e8d8742018413'
    if ((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash -ne $expected) {
        throw 'HandBrake download checksum mismatch. Nothing was installed.'
    }
    Expand-Archive -LiteralPath $zip -DestinationPath (Join-Path $temp 'unpacked')
    $bins = @(Get-ChildItem -LiteralPath (Join-Path $temp 'unpacked') -Filter HandBrakeCLI.exe -Recurse)
    if ($bins.Count -ne 1) { throw 'Unexpected HandBrake archive layout.' }
    Copy-Item -LiteralPath $bins[0].FullName -Destination (Join-Path $out 'HandBrakeCLI.exe') -Force
    Write-Host 'Official HandBrakeCLI 1.9.2 installed locally. License: GPLv2; see CREDITS-AND-LICENSES.txt.'
} finally {
    # Only this script-created GUID directory under the system temporary folder.
    if ((Split-Path $temp -Parent) -eq [IO.Path]::GetTempPath().TrimEnd('\')) {
        Remove-Item -LiteralPath $temp -Recurse -Force
    }
}
