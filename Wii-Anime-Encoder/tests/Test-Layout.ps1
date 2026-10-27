$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$a=[Reflection.Assembly]::LoadFrom("$(Split-Path $PSScriptRoot -Parent)\dist\Wii-Anime-Encoder\WiiAnimeEncoder.exe")
$type=$a.GetType('WiiAnimeEncoder.MainForm');$flags=[Reflection.BindingFlags]'Instance,NonPublic'
$ctor=$type.GetConstructor($flags,$null,[Type[]]@([string]),$null); $f=$ctor.Invoke([object[]]@([string](Join-Path $PSScriptRoot "integration-settings.json")))
$f.StartPosition='Manual';$f.Location=New-Object Drawing.Point(-20000,-20000);$f.ShowInTaskbar=$false
$f.Show();[Windows.Forms.Application]::DoEvents()
function Descendants($p){foreach($ch in $p.Controls){$ch;Descendants $ch}}
foreach($size in @(@(1100,720),@(1300,900),@(1920,1080))){
$f.ClientSize=New-Object Drawing.Size($size[0],$size[1]);$f.PerformLayout();[Windows.Forms.Application]::DoEvents()
foreach($box in @(Descendants $f|Where-Object{$_ -is [Windows.Forms.TextBox] -and $_.Parent -isnot [Windows.Forms.NumericUpDown]})){
foreach($s in $box.Parent.Controls){if($s -ne $box -and $s.Visible -and $s.Bounds.IntersectsWith($box.Bounds)){throw "Overlap: $($s.Text)"}}
}
$b=New-Object Drawing.Bitmap($f.Width,$f.Height)
$f.DrawToBitmap($b,(New-Object Drawing.Rectangle(0,0,$f.Width,$f.Height)))
$b.Dispose()
"PASS resize $($size[0])x$($size[1])"
}
# Hide while exercising monitor-wide bounds so no test window flashes on screen.
$f.Hide()
$before=$f.Bounds
$type.GetMethod('ToggleFullscreen',$flags).Invoke($f,@())|Out-Null
if($f.FormBorderStyle -ne 'None' -or $f.Bounds -ne [Windows.Forms.Screen]::FromControl($f).Bounds){throw 'Fullscreen bounds failed'}
$type.GetMethod('ToggleFullscreen',$flags).Invoke($f,@())|Out-Null
if($f.FormBorderStyle -ne 'Sizable' -or $f.Bounds -ne $before){throw 'Window restoration failed'}
$f.WindowState='Maximized'
$type.GetMethod('ToggleFullscreen',$flags).Invoke($f,@())|Out-Null
$type.GetMethod('ToggleFullscreen',$flags).Invoke($f,@())|Out-Null
if($f.WindowState -ne 'Maximized'){throw 'Maximized restoration failed'}
$keyMethod=$type.GetMethod('ProcessCmdKey',$flags)
$msg=New-Object Windows.Forms.Message
$keyMethod.Invoke($f,@($msg,[Windows.Forms.Keys]::F11))|Out-Null
if(-not $type.GetField('fullscreen',$flags).GetValue($f)){throw 'F11 failed'}
$keyMethod.Invoke($f,@($msg,[Windows.Forms.Keys]::Escape))|Out-Null
if($type.GetField('fullscreen',$flags).GetValue($f)){throw 'Escape failed'}
'PASS fullscreen bounds, restore, F11 and Escape'
$f.Dispose()









