
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms
$asm=[Reflection.Assembly]::LoadFrom((Join-Path (Split-Path $PSScriptRoot -Parent) 'dist\Wii-Anime-Encoder\Wii-Anime-Scarlet.exe'))
$type=$asm.GetType('EncoderApp')
$flags=[Reflection.BindingFlags]'Instance,NonPublic'
$ctor=$type.GetConstructor($flags,$null,[Type[]]@([string]),$null)
$prefs=Join-Path $PSScriptRoot ('prefs-qa-'+[guid]::NewGuid().ToString('N')+'\settings.json')
function Field($form,$name){ $type.GetField($name,$flags).GetValue($form) }
function New-TestForm { $f=$ctor.Invoke([object[]]@([string]$prefs)); $f.ShowInTaskbar=$false; $f.StartPosition='Manual'; $f.Location=New-Object Drawing.Point(-20000,-20000); return $f }
$f=New-TestForm
foreach($name in @('video','subtitle','output')){if((Field $f $name).Text -ne ''){throw "First-launch $name not empty"}}
if((Field $f 'quality').Value -ne 38){throw 'RF default not 19'}
(Field $f 'video').Text='C:\Example\Episodes'
if((Field $f 'subtitle').Text -ne 'C:\Example\Episodes'){throw 'Subtitle default failed'}
(Field $f 'subtitle').Text='C:\Example\Subtitles'
(Field $f 'output').Text='C:\Example\Completed'
$f.Show(); $f.Close(); $f.Dispose()
$f=New-TestForm
foreach($pair in @(@('video','C:\Example\Episodes'),@('subtitle','C:\Example\Subtitles'),@('output','C:\Example\Completed'))){
 if((Field $f $pair[0]).Text -ne $pair[1]){throw "Restore failed: $($pair[0])"}
}
$f.Dispose()
Remove-Item -LiteralPath $prefs
Remove-Item -LiteralPath (Split-Path $prefs)
'PASS: empty first launch; RF 19; subtitle follows video; all three folders restored after normal close.'
