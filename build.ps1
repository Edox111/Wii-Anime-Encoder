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
$icon = Join-Path $PSScriptRoot 'assets/app.ico'
$cp.CompilerOptions = '/target:winexe /platform:x64 /optimize+ /win32icon:"' + $icon + '"'
$null = $cp.EmbeddedResources.Add($icon)
foreach ($ref in $refs) { $null = $cp.ReferencedAssemblies.Add($ref) }
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter *.cs -Recurse | Sort-Object FullName | Select-Object -ExpandProperty FullName)
Add-Type -Path $sources -CompilerParameters $cp
foreach ($name in @('README.txt','LICENSE','CREDITS-AND-LICENSES.txt')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $out -Force
}
New-Item -ItemType Directory -Path (Join-Path $out "themes") -Force | Out-Null
foreach ($photo in Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot "assets/themes") -Filter *.jpg) {
    $destination = Join-Path $out ("themes/" + $photo.Name)
    if ($NoTheme) { if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination } }
    else { Copy-Item -LiteralPath $photo.FullName -Destination $destination -Force }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "assets/themes/CREDITS.txt") -Destination (Join-Path $out "themes") -Force
Write-Host "Built: $exe"

Copy-Item -Path (Join-Path $PSScriptRoot 'scripts\*.ps1') -Destination $out -Force

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "distribution\Setup-HandBrake.cmd") -Destination $out -Force
