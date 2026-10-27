param([switch]$IncludeTheme)
$ErrorActionPreference = 'Stop'
$version = '1.0.0'
$buildArguments = @("-NoProfile","-ExecutionPolicy","Bypass","-File",(Join-Path $PSScriptRoot "build.ps1"))
if (-not $IncludeTheme) { $buildArguments += "-NoTheme" }
& powershell.exe @buildArguments
if ($LASTEXITCODE -ne 0) { throw 'Build failed; no release archive was created.' }
$distribution = Join-Path $PSScriptRoot 'dist\Wii-Anime-Encoder'
$artifacts = Join-Path $PSScriptRoot 'artifacts'
$stagingRoot = Join-Path $artifacts ('stage-' + [guid]::NewGuid().ToString('N'))
$staging = Join-Path $stagingRoot 'Wii-Anime-Encoder'
New-Item -ItemType Directory -Path $staging -Force | Out-Null
try {
    # Explicit whitelist: never ship a locally installed encoder, media or settings.
    $files = @('WiiAnimeEncoder.exe','Convert-Episodes.ps1','Invoke-EncodingJob.ps1',
        'Install-HandBrake.ps1','Setup-HandBrake.cmd','README.txt','LICENSE','CREDITS-AND-LICENSES.txt')
    if ($IncludeTheme) {
        if (-not (Test-Path -LiteralPath (Join-Path $distribution 'remilia.jpg'))) {
            throw 'Theme packaging requested, but assets/local/remilia.jpg is missing.'
        }
        $files += 'remilia.jpg'
    }
    foreach ($file in $files) {
        Copy-Item -LiteralPath (Join-Path $distribution $file) -Destination $staging
    }
    $edition = if ($IncludeTheme) { 'Scarlet' } else { 'Standard' }
    $archive = Join-Path $artifacts ("Wii-Anime-Encoder-$edition-$version-win-x64.zip")
    Compress-Archive -LiteralPath $staging -DestinationPath $archive -Force
    $hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($archive + '.sha256', $hash + '  ' + [IO.Path]::GetFileName($archive) + [Environment]::NewLine)
    Write-Host "Created: $archive"
} finally {
    $resolved = [IO.Path]::GetFullPath($stagingRoot)
    if ([IO.Path]::GetDirectoryName($resolved) -eq [IO.Path]::GetFullPath($artifacts) -and
        [IO.Path]::GetFileName($resolved).StartsWith('stage-')) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
