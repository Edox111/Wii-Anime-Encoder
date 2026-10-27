$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$root = Split-Path $PSScriptRoot -Parent
$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $root 'dist\Wii-Anime-Encoder\WiiAnimeEncoder.exe'))
$type = $assembly.GetType('WiiAnimeEncoder.MainForm')
$flags = [Reflection.BindingFlags]'Instance,NonPublic'
$constructor = $type.GetConstructor($flags,$null,[Type[]]@([string]),$null)
$form = $constructor.Invoke([object[]]@([string](Join-Path $PSScriptRoot 'absent-slider-settings.json')))
$form.StartPosition = 'Manual'
$form.Location = New-Object Drawing.Point(-20000,-20000)
$form.ShowInTaskbar = $false
try {
    $form.Show()
    foreach ($size in @(@(1050,720),@(1300,900))) {
        $form.Size = New-Object Drawing.Size($size[0],$size[1])
        [Windows.Forms.Application]::DoEvents()
        foreach ($field in @('quality','audio')) {
            $slider = $type.GetField($field,$flags).GetValue($form)
            $sliderType = $slider.GetType()
            $label = @($slider.Parent.Controls | Where-Object { $_ -is [Windows.Forms.Label] })[0]
            for ($value = $slider.Minimum; $value -le $slider.Maximum; $value++) {
                $x = 12 + [int](($slider.Width-24)*($value-$slider.Minimum)/($slider.Maximum-$slider.Minimum))
                $mouse = New-Object Windows.Forms.MouseEventArgs([Windows.Forms.MouseButtons]::Left,1,$x,15,0)
                $sliderType.GetMethod('OnMouseDown',$flags).Invoke($slider,@([Windows.Forms.MouseEventArgs]$mouse)) | Out-Null
                $sliderType.GetMethod('OnMouseMove',$flags).Invoke($slider,@([Windows.Forms.MouseEventArgs]$mouse)) | Out-Null
                [Windows.Forms.Application]::DoEvents()
                if ($slider.Value -ne $value) { throw "Drag failed: $field $value" }
                $expected = if ($field -eq 'quality') { 'RF quality: '+($value/2.0).ToString('0.0',[Globalization.CultureInfo]::InvariantCulture) } else { "Audio track: $value" }
                if ($label.Text -ne $expected) { throw "Selected value not updated: $field" }
                $required = [Windows.Forms.TextRenderer]::MeasureText($label.Text,$label.Font)
                if ($required.Width+4 -gt $label.ClientSize.Width -or $required.Height+2 -gt $label.ClientSize.Height) { throw "Selected value clipped: $field" }
                $bitmap = New-Object Drawing.Bitmap($slider.Width,$slider.Height)
                try { $slider.DrawToBitmap($bitmap,$slider.ClientRectangle) } finally { $bitmap.Dispose() }
                $sliderType.GetMethod('OnMouseUp',$flags).Invoke($slider,@([Windows.Forms.MouseEventArgs]$mouse)) | Out-Null
            }
            $key = New-Object Windows.Forms.KeyEventArgs([Windows.Forms.Keys]::Left)
            $sliderType.GetMethod('OnKeyDown',$flags).Invoke($slider,@([Windows.Forms.KeyEventArgs]$key)) | Out-Null
            if ($slider.Value -ne $slider.Maximum-1) { throw "Keyboard step failed: $field" }
        }
    }
    'PASS: all RF/audio drag steps, visible selected labels, keyboard steps at minimum and standard window sizes.'
} finally {
    $form.Dispose()
}
