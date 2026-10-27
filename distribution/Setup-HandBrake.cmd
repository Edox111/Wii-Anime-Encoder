@echo off
setlocal
rem Use Windows PowerShell modules even when launched from PowerShell 7.
set "PSModulePath=%SystemRoot%\System32\WindowsPowerShell\v1.0\Modules;%ProgramFiles%\WindowsPowerShell\Modules"
title Wii Anime Encoder - Dependency Setup
echo Wii Anime Encoder - HandBrake setup
echo.
echo This downloads the official HandBrake encoder and checks its integrity.
echo An internet connection is required. Administrator access is not needed.
echo.
"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-HandBrake.ps1"
if errorlevel 1 (
    echo.
    echo Setup did not finish. Read the error above and try again.
    echo If a file is in use, close the encoder before retrying.
    pause
    exit /b 1
)
echo.
echo You can now open WiiAnimeEncoder.exe.
pause
exit /b 0
