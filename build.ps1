param([switch]$NoTheme)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -ne 'Desktop') {
    $arguments = @("-NoProfile","-ExecutionPolicy","Bypass","-File",$PSCommandPath)
    if ($NoTheme) { $arguments += "-NoTheme" }
    & powershell.exe @arguments
    exit $LASTEXITCODE
}
$out = Join-Path $PSScriptRoot 'dist\Wii-Anime-Encoder'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$exe = Join-Path $out 'WiiAnimeEncoder.exe'
if (Test-Path -LiteralPath $exe) { Remove-Item -LiteralPath $exe }
$wpf = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\WPF"
$refs = @('System.dll','System.Core.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Web.Extensions.dll',"$wpf\PresentationFramework.dll","$wpf\PresentationCore.dll","$wpf\WindowsBase.dll","$wpf\WindowsFormsIntegration.dll",'System.Xaml.dll')
$cp = New-Object CodeDom.Compiler.CompilerParameters
$cp.GenerateExecutable = $true
$cp.OutputAssembly = $exe
$cp.CompilerOptions = '/target:winexe /platform:x64 /optimize+'
$icon = Join-Path $PSScriptRoot 'assets\local\app.ico'
if (-not $NoTheme -and (Test-Path -LiteralPath $icon)) { $cp.CompilerOptions += ' /win32icon:"' + $icon + '"' }
foreach ($ref in $refs) { $null = $cp.ReferencedAssemblies.Add($ref) }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter *.cs -Recurse | Sort-Object FullName | Select-Object -ExpandProperty FullName)
Add-Type -Path $sources -CompilerParameters $cp
foreach ($name in @('README.txt','LICENSE','CREDITS-AND-LICENSES.txt')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $out -Force
}
$art = Join-Path $PSScriptRoot 'assets\local\remilia.jpg'
$targetArt = Join-Path $out 'remilia.jpg'
if (-not $NoTheme -and (Test-Path -LiteralPath $art)) { Copy-Item -LiteralPath $art -Destination $targetArt -Force }
elseif (Test-Path -LiteralPath $targetArt) { Remove-Item -LiteralPath $targetArt }
Write-Host "Built: $exe"

Copy-Item -Path (Join-Path $PSScriptRoot 'scripts\*.ps1') -Destination $out -Force

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "distribution\Setup-HandBrake.cmd") -Destination $out -Force
