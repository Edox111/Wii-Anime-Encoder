$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\')
$testRoot = Join-Path $temporaryRoot ('wii-protocol-' + [guid]::NewGuid().ToString('N'))
$work = Join-Path $testRoot ('Media ' + [char]0x00e9 + ' & files')
New-Item -ItemType Directory -Path $work -Force | Out-Null
$process = $null
try {
    foreach ($file in @('Convert-Episodes.ps1','Invoke-EncodingJob.ps1')) {
        Copy-Item -LiteralPath (Join-Path $root "scripts\$file") -Destination $work
    }
    $title = [string][char]0x6771 + [char]0x65b9
    [IO.File]::WriteAllText((Join-Path $work 'Example Show - Ep01.mkv'),'fixture')
    $config = @{
        Video=$work; Subtitle=$work; Output=$work; Title=$title; Quality=19;
        Audio=1; First=$false; Plan=$true; Preview=$false; Selected=''; Work=''
    } | ConvertTo-Json
    $configPath = Join-Path $work 'job.json'
    # Match the app's UTF-8-without-BOM configuration writer.
    [IO.File]::WriteAllText($configPath,$config,(New-Object Text.UTF8Encoding($false)))
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = 'powershell.exe'
    $start.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $work 'Invoke-EncodingJob.ps1') + '" -Config "' + $configPath + '"'
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.StandardOutputEncoding = [Text.Encoding]::UTF8
    $process = [Diagnostics.Process]::Start($start)
    $output = $process.StandardOutput.ReadToEnd()
    $process.WaitForExit()
    if ($process.ExitCode -ne 0 -or -not $output.Contains($title + ' - Ep01')) {
        throw "Unicode naming protocol failed: $output"
    }
    if (Test-Path -LiteralPath $configPath) { throw 'Job configuration was not removed.' }
    'PASS: Unicode paths, ampersands, non-ASCII titles and UTF-8 protocol.'
} finally {
    if ($process) { $process.Dispose() }
    $resolved = [IO.Path]::GetFullPath($testRoot)
    if ([IO.Path]::GetDirectoryName($resolved) -eq $temporaryRoot -and [IO.Path]::GetFileName($resolved).StartsWith('wii-protocol-')) {
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
