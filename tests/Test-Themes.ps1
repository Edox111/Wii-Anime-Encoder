$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Windows.Forms,System.Drawing
$root=Split-Path $PSScriptRoot -Parent
$assembly=[Reflection.Assembly]::LoadFrom((Join-Path $root 'dist\Wii-Anime-Encoder\WiiAnimeEncoder.exe'))
$type=$assembly.GetType('WiiAnimeEncoder.MainForm'); $flags=[Reflection.BindingFlags]'Instance,NonPublic'
$ctor=$type.GetConstructor($flags,$null,[Type[]]@([string]),$null)
$qa=Join-Path $PSScriptRoot ('qa-themes-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $qa | Out-Null
$prefs=Join-Path $qa 'settings.json'
function Field($form,$name) { $type.GetField($name,$flags).GetValue($form) }
$form=$ctor.Invoke([object[]]@([string]$prefs))
try {
    $form.StartPosition='Manual'; $form.Location=New-Object Drawing.Point(-20000,-20000); $form.ShowInTaskbar=$false
    $form.Show(); [Windows.Forms.Application]::DoEvents()
    $picker=Field $form 'themePicker'
    if ($picker.Items.Count -ne 11 -or $picker.Text -ne 'Midnight') { throw 'Theme count or default incorrect.' }
    $names=@($picker.Items | ForEach-Object { $_.Name })
    if (($names -join "|") -ne ((@($names | Sort-Object)) -join "|")) { throw "Themes are not alphabetical." }
    if ([WiiAnimeEncoder.AppTheme]::Find("unknown").Name -ne "Midnight") { throw "Unknown theme fallback changed." }
    $colors=New-Object 'Collections.Generic.HashSet[int]'
    for ($i=0; $i -lt $picker.Items.Count; $i++) {
        $picker.SelectedIndex=$i; [Windows.Forms.Application]::DoEvents()
        $theme=Field $form 'theme'
        $button=Field $form 'start'
        if ($button.Accent -ne $theme.Accent) { throw 'Button accent does not follow theme.' }
        if ((Field $form 'quality').Value -ne 38 -or (Field $form 'audio').Value -ne 1) { throw 'Theme changed encoding options.' }
        if ((Field $form 'stop').Enabled -or (Field $form 'cancelButton').Enabled) { throw 'Theme enabled an idle job control.' }
        if ($theme.Name -ne "Scarlet") { $null=$colors.Add($button.Accent.ToArgb()) }
        if ($null -eq (Field $form 'themeArtwork')) { throw ('Missing theme photograph: '+$theme.Name) }
        if ($true) {
            $bitmap=New-Object Drawing.Bitmap($form.Width,$form.Height)
            try { $form.DrawToBitmap($bitmap,(New-Object Drawing.Rectangle(0,0,$form.Width,$form.Height))); $bitmap.Save((Join-Path $qa ($theme.Name+'.png'))) } finally { $bitmap.Dispose() }
        }
    }
    if ($colors.Count -ne 10) { throw 'Plain theme accents are not distinct.' }
    $picker.SelectedItem=[WiiAnimeEncoder.AppTheme]::Find('Ocean')
} finally { $form.Dispose() }
$form=$ctor.Invoke([object[]]@([string]$prefs))
try { if ((Field $form 'themePicker').Text -ne 'Ocean') { throw 'Theme preference did not persist.' } } finally { $form.Dispose() }
'PASS: 11 themes, neutral default, distinct plain-theme button accents, unchanged RF/audio, idle controls, persisted theme.'
"Visual samples: $qa"
