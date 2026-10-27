$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'scripts\Install-HandBrake.ps1') -ApplicationDirectory (Join-Path $PSScriptRoot 'dist\Wii-Anime-Encoder')
