param([Parameter(Mandatory=$true)][string]$Fixture)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'dist\Wii-Anime-Encoder\WiiAnimeEncoder.exe'))
$type=$assembly.GetType('WiiAnimeEncoder.MainForm'); $flags=[Reflection.BindingFlags]'Instance,NonPublic'
$ctor=$type.GetConstructor($flags,$null,[Type[]]@([string]),$null)
$qa=Join-Path $PSScriptRoot ('qa-controls-'+[guid]::NewGuid().ToString('N'))
$media=Join-Path $qa 'media'; $dest=Join-Path $qa 'output'
New-Item -ItemType Directory -Path $media,$dest | Out-Null
Copy-Item -LiteralPath $Fixture -Destination (Join-Path $media 'Test Pattern - Ep01.avi')
[IO.File]::WriteAllText((Join-Path $media 'Test Pattern - Ep01.srt'),"1`r`n00:00:00,000 --> 00:02:00,000`r`nSynthetic control test`r`n")
function Field($f,$name) { $type.GetField($name,$flags).GetValue($f) }
function Invoke($f,$name,$values=@()) { $type.GetMethod($name,$flags).Invoke($f,$values) | Out-Null }
function PumpUntil($f,$condition,$seconds=45) {
    $watch=[Diagnostics.Stopwatch]::StartNew()
    while (-not (& $condition)) {
        [Windows.Forms.Application]::DoEvents()
        if ($watch.Elapsed.TotalSeconds -gt $seconds) { throw "Timed out: $((Field $f 'status').Text)" }
        Start-Sleep -Milliseconds 30
    }
}
$form=$ctor.Invoke([object[]]@([string](Join-Path $qa 'prefs.json')))
try {
    $form.ShowInTaskbar=$false; $form.StartPosition='Manual'; $form.Location=New-Object Drawing.Point(-20000,-20000)
    (Field $form 'video').Text=$media; (Field $form 'output').Text=$dest
    $form.Show(); [Windows.Forms.Application]::DoEvents()
    Invoke $form 'BeginEncode' @($false)
    Invoke $form 'TogglePause'
    PumpUntil $form { (Field $form 'paused') }
    if ((Field $form 'stop').Text -ne 'Resume' -or @(Get-ChildItem $dest -File).Count -ne 0) { throw 'Queue pause did not hold the encode.' }
    (Field $form 'themePicker').SelectedItem=[WiiAnimeEncoder.AppTheme]::Find('Forest')
    if (-not (Field $form 'paused')) { throw 'Theme change lost paused state.' }
    Invoke $form 'TogglePause'
    PumpUntil $form { -not (Field $form 'paused') }
    PumpUntil $form { (Field $form 'percent').Text -ne '0.0%' -or $null -eq (Field $form 'worker') }
    if ($null -eq (Field $form 'worker')) { throw 'Fixture encoded too quickly to exercise in-episode pause.' }
    Invoke $form 'TogglePause'
    PumpUntil $form { (Field $form 'paused') -or $null -eq (Field $form 'worker') }
    if (-not (Field $form 'paused')) { throw 'In-episode pause was not acknowledged.' }
    Invoke $form 'TogglePause'
    PumpUntil $form { $null -eq (Field $form 'worker') }
    if (@(Get-ChildItem $dest -Filter *.mp4).Count -ne 1 -or (Field $form 'status').Text -ne 'Conversion complete.') { throw 'Resumed batch did not publish one completed file.' }
    if ((Field $form 'stop').Enabled -or (Field $form 'stop').Text -ne 'Pause') { throw 'Pause button did not reset.' }
    'PASS: UI pause before launch, theme switch while paused, resume, in-episode pause/resume, completed output and button reset.'
    Invoke $form 'BeginEncode' @($false)
    Invoke $form 'TogglePause'
    PumpUntil $form { (Field $form 'paused') }
    Invoke $form 'StopWorker'
    PumpUntil $form { $null -eq (Field $form 'worker') }
    if ((Field $form 'status').Text -ne 'Cancelled.' -or @(Get-ChildItem $dest -Filter *.mp4).Count -ne 1) { throw 'Cancel lost completed output.' }
    'PASS: cancelling a paused batch keeps previously completed output.'
} finally {
    if ($null -ne (Field $form 'worker')) { Invoke $form 'StopWorker'; PumpUntil $form { $null -eq (Field $form 'worker') } }
    $form.Dispose()
}
