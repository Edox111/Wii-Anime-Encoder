param([string]$EncoderPath, [string]$Fixture, [switch]$Worker, [string]$Work)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Worker) {
    if (-not (Test-Path -LiteralPath $EncoderPath) -or -not (Test-Path -LiteralPath $Fixture)) { throw 'Supply an official HandBrakeCLI 1.9.2 executable and a synthetic AVI fixture.' }
    $Work = Join-Path $PSScriptRoot ('qa-pause-'+[guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $Work | Out-Null
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = 'powershell.exe'
    $start.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "'+$PSCommandPath+'" -Worker -EncoderPath "'+$EncoderPath+'" -Fixture "'+$Fixture+'" -Work "'+$Work+'"'
    $start.UseShellExecute=$false; $start.CreateNoWindow=$true; $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true
    $p=[Diagnostics.Process]::Start($start)
    $stdout=$p.StandardOutput.ReadToEndAsync(); $stderr=$p.StandardError.ReadToEndAsync()
    if (-not $p.WaitForExit(180000)) { $p.Kill(); throw 'Pause integration test exceeded three minutes.' }
    $stdout.Result; $stderr.Result
    $exit=$p.ExitCode; $p.Dispose()
    if ($exit -ne 0) { throw "Pause test failed; diagnostics retained in $Work" }
    return
}
[Reflection.Assembly]::LoadFrom((Join-Path $root 'dist\Wii-Anime-Encoder\WiiAnimeEncoder.exe')) | Out-Null
$subtitle=Join-Path $Work 'fixture.srt'
[IO.File]::WriteAllText($subtitle, "1`r`n00:00:00,000 --> 00:01:00,000`r`nPause / resume synthetic test`r`n")
function Encode([string]$Name, [bool]$Pause, [bool]$Cancel) {
    $output=Join-Path $Work ($Name+'.mp4')
    $arguments=[string[]]@('-i',$Fixture,'-o',$output,'-e','x264','--encoder-preset','veryslow','-q','19','--encoder-profile','baseline','--encoder-level','3.0','--maxWidth','640','--maxHeight','480','--non-anamorphic','--crop','0:0:0:0','--rate','30','--pfr','--srt-file',$subtitle,'--srt-burn=1','--json','--min-duration','0')
    $encoder=New-Object WiiAnimeEncoder.EncoderSession($EncoderPath,$arguments)
    $log=New-Object 'Collections.Generic.List[string]'
    $pauses=0; $resumes=0; $waiting=$false; $ack=$false; $resumeSent=$false; $pauseAt=[DateTime]::MaxValue; $lastProgress=-1.0; $pausedProgress=-1.0
    $watch=[Diagnostics.Stopwatch]::StartNew()
    try {
        while ($true) {
            if ($watch.Elapsed.TotalSeconds -gt 100) { throw 'Encoder timed out.' }
            $line=$encoder.ReadLine()
            if ($null -ne $line) {
                $log.Add($line)
                if ($line -match '"Progress"\s*:\s*([\d.]+)') { $lastProgress=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture) }
                if ($line -match 'Encoding Paused by user command') { $ack=$true; $pauseAt=[DateTime]::UtcNow; $pausedProgress=$lastProgress; $pauses++; "PAUSED $Name cycle $pauses" }
                if ($line -match '"State"\s*:\s*"WORKING"') {
                    if ($resumeSent) { $resumes++; $resumeSent=$false; $waiting=$false; "RESUMED $Name cycle $resumes" }
                    elseif ($Pause -and -not $waiting -and $pauses -lt 2) { $encoder.Send('p'); $waiting=$true }
                }
            }
            if ($ack -and ([DateTime]::UtcNow-$pauseAt).TotalSeconds -ge 2) {
                if ($Cancel) { $encoder.Send('r'); $encoder.Send('q'); $ack=$false }
                else { $encoder.Send('r'); $ack=$false; $resumeSent=$true }
            }
            if ($null -eq $line) {
                if ($encoder.HasExited -and $encoder.OutputEnded) { break }
                Start-Sleep -Milliseconds 25
            }
        }
        if ($Cancel) { if ($encoder.ExitCode -eq 0 -or $pauses -lt 1) { throw 'Cancellation while paused was not exercised.' } }
        elseif ($encoder.ExitCode -ne 0 -or ($Pause -and ($pauses -ne 2 -or $resumes -ne 2))) { throw "Encode failed or pause cycles missing: exit $($encoder.ExitCode), pauses $pauses, resumes $resumes" }
    } finally { $encoder.Dispose(); [IO.File]::WriteAllLines((Join-Path $Work ($Name+'.log')),$log) }
    return $output
}
function MediaHash([string]$Path) {
    $reader=New-Object IO.BinaryReader([IO.File]::OpenRead($Path))
    try {
        while ($reader.BaseStream.Position+8 -le $reader.BaseStream.Length) {
            $bytes=$reader.ReadBytes(4); [Array]::Reverse($bytes); $size=[BitConverter]::ToUInt32($bytes,0)
            $kind=[Text.Encoding]::ASCII.GetString($reader.ReadBytes(4))
            if ($size -lt 8) { throw 'Unexpected MP4 box.' }
            if ($kind -eq 'mdat') {
                $sha=[Security.Cryptography.SHA256]::Create()
                try { return [BitConverter]::ToString($sha.ComputeHash($reader.ReadBytes($size-8))) } finally { $sha.Dispose() }
            }
            $reader.BaseStream.Position += $size-8
        }
        throw 'No media data found.'
    } finally { $reader.Dispose() }
}
$baseline=@(Encode 'continuous' $false $false)[-1]
$resumed=@(Encode 'paused' $true $false)[-1]
if ((MediaHash $baseline) -ne (MediaHash $resumed)) { throw 'Paused output media bytes differ from uninterrupted output.' }
Encode 'cancelled' $true $true | Out-Null
'PASS: native HandBrake pause/resume twice; encoded media byte-identical to continuous encode; cancel while paused exits.'
