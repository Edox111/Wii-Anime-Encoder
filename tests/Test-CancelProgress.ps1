$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$root=Split-Path $PSScriptRoot -Parent
$asm=[Reflection.Assembly]::LoadFrom((Join-Path $root 'dist\Wii-Anime-Encoder\WiiAnimeEncoder.exe'))
$type=$asm.GetType('WiiAnimeEncoder.MainForm'); $flags=[Reflection.BindingFlags]'Instance,NonPublic'
$qa=Join-Path $PSScriptRoot ('qa-cancel-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $qa | Out-Null
$form=$type.GetConstructor($flags,$null,[Type[]]@([string]),$null).Invoke([object[]]@([string](Join-Path $qa 'settings.json')))
function Field($name) { return ,($type.GetField($name,$flags).GetValue($form)) }
function SetField($name,$value) { $type.GetField($name,$flags).SetValue($form,$value) }
$work=Join-Path (Join-Path $root 'dist\Wii-Anime-Encoder') ('.work-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work | Out-Null
# Small worker stands in for encoding: exits when the real cancel request arrives.
$script=Join-Path $qa 'worker.ps1'
'param($work); while (-not (Test-Path -LiteralPath (Join-Path $work "cancel.request"))) { Start-Sleep -Milliseconds 50 }' | Set-Content $script
$worker=Start-Process powershell.exe -ArgumentList @('-NoProfile','-File',('"'+$script+'"'),('"'+$work+'"')) -WindowStyle Hidden -PassThru
try {
    SetField 'worker' $worker; SetField 'activeWork' $work
    SetField 'outputClosed' $true; SetField 'errorClosed' $true
    (Field 'percent').Text='47.2%'
    SetField 'paused' $true
    $type.GetMethod('StopWorker',$flags).Invoke($form,@()) | Out-Null
    if ((Field 'percent').Text -ne '0.0%') { throw 'Cancel did not reset immediately.' }
    if (-not (Test-Path (Join-Path $work 'cancel.request'))) { throw 'Cancel request missing.' }
    SetField 'paused' $false
    (Field 'messages').Enqueue('EVENT {"Type":"PROGRESS","Data":98.7}')
    (Field 'messages').Enqueue('EVENT {"Type":"SAVED","Data":"C:\\Example\\completed.mp4"}')
    for ($i=0; $i -lt 10 -and -not (Field 'messages').IsEmpty; $i++) { $type.GetMethod('Drain',$flags).Invoke($form,@()) | Out-Null }
    if ((Field 'percent').Text -ne '0.0%') { throw 'Late progress overwrote cancel reset.' }
    if ((Field 'convertedFiles').Count -ne 1) { throw 'Completed output event lost.' }
    if ((Field 'worker') -ne $null) {
        if (-not $worker.WaitForExit(10000)) { throw 'Test worker did not exit.' }
        $type.GetMethod('Drain',$flags).Invoke($form,@()) | Out-Null
    }
    if ((Field 'percent').Text -ne '0.0%' -or (Field 'status').Text -ne 'Cancelled.') { throw 'Cancel completion state incorrect.' }
    'PASS: immediate reset, late progress ignored, completed output retained, final cancelled state stays zero.'
} finally {
    if ((Field 'worker') -ne $null) { if (-not $worker.HasExited) { $worker.Kill(); $worker.WaitForExit() }; $worker.Dispose(); SetField 'worker' $null }
    $form.Dispose()
}
