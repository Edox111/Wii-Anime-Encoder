# Windows PowerShell 5.1 / PowerShell 7. No administrator access needed.
[CmdletBinding()]
param(
    [string]$SourceFolder,
    [string]$SubtitleFolder,
    [string]$DestinationFolder,
    [string]$EncoderPath,
    [string]$AnimeName,
    [ValidateRange(1,51)][double]$Quality = 19,
    [ValidateRange(1,99)][int]$AudioTrack = 1,
    [switch]$AllowMissingSubtitles,
    [switch]$FirstOnly,
    [switch]$PlanOnly,
    [string]$SelectedFile, [string]$WorkFolder, [switch]$PreviewClip, [switch]$NonInteractive
)
$ErrorActionPreference = 'Stop'
$configFile = Join-Path $PSScriptRoot 'settings.json'

function Read-Choice([string]$Prompt, [string[]]$Choices, [string]$Default) {
    do {
        $answer = (Read-Host "$Prompt [$Default]").Trim()
        if (-not $answer) { return $Default }
    } until ($answer -in $Choices)
    return $answer
}
function Pick-Path([switch]$Folder) {
    Add-Type -AssemblyName System.Windows.Forms
    if ($Folder) {
        $dialog = New-Object System.Windows.Forms.FolderBrowserDialog
        $dialog.Description = 'Choose the folder containing original episodes and subtitles'
    } else {
        $dialog = New-Object System.Windows.Forms.OpenFileDialog
        $dialog.Title = 'Select your existing HandBrakeCLI.exe'
        $dialog.Filter = 'HandBrake command-line application|HandBrakeCLI.exe'
    }
    try {
        if ($dialog.ShowDialog() -ne 'OK') { throw 'Selection cancelled.' }
        if ($Folder) { return $dialog.SelectedPath }
        return $dialog.FileName
    } finally { $dialog.Dispose() }
}
function Get-SubtitleCandidates($Video) {
    $folders = @($SubtitleFolder)
    foreach ($name in @('Subs','Subtitles')) {
        $folder = Join-Path $SubtitleFolder $name
        if (Test-Path -LiteralPath $folder -PathType Container) { $folders += $folder }
    }
    $files = @(foreach ($folder in $folders) {
        Get-ChildItem -LiteralPath $folder -File | Where-Object {
            $_.Extension -in '.ssa','.ass','.srt'
        }
    })
    # Literal comparisons: brackets and '+' in names are not wildcards.
    $exact = @($files | Where-Object { $_.BaseName -eq $Video.BaseName })
    if ($exact.Count -gt 0) { return $exact }
    $englishNames = @('.en','.eng','.english') | ForEach-Object { $Video.BaseName + $_ }
    return @($files | Where-Object { $_.BaseName -in $englishNames })
}
function Clean-AnimeTitle([string]$Text) {
    $text = [regex]::Replace($Text, '\[[^\]]*\]|\((?:19|20)\d{2}\)', ' ')
    $text = $text -replace '_',' '
    $text = $text -replace '(?<=\p{L})\.(?=\p{L})',' '
    $text = $text -replace '(?i)(?:\s*[-.]?\s+)(?:TV|Season\s*\d+|S\d{1,2})\s*$',''
    $text = $text -replace '(?i)\b(?:\d{3,4}p|\d{3,4}x\d{3,4}|WEB[- .]?DL|WEBRip|BluRay|BDRip|DVDRip|HEVC|AVC|x26[45]|h\.?26[45]|10bit|8bit|AAC|FLAC)\b.*$',''
    $text = [regex]::Replace($text, '[<>:"/\\|?*\x00-\x1F]', ' ')
    return [regex]::Replace($text, '\s+', ' ').Trim().Trim(' ','.','-','_')
}
function Get-EpisodeLabel($Video) {
    $clean = $Video.BaseName -replace '_',' '
    # Preserve a numeric bracket when it is plausibly an episode, not a year.
    $clean = [regex]::Replace($clean, '\[(\d{1,3}(?:\.\d+)?)\]', ' Ep$1 ')
    $clean = [regex]::Replace($clean, '\[[^\]]*\]', ' ').Trim()
    $clean = $clean -replace '(?<=\p{L})\.(?=\p{L})',' '
    $clean = $clean -replace '(?i)\s*\((?:19|20)\d{2}\)\s*',' '
    $clean = $clean -replace '(?i)\b(?:\d{3,4}p|\d{3,4}x\d{3,4}|WEB[- .]?DL|WEBRip|BluRay|BDRip|DVDRip|HEVC|AVC|x26[45]|h\.?26[45]|10bit|8bit|AAC|FLAC)\b.*$',''
    $clean = $clean.Trim().TrimEnd(' ','.','-')
    # Do not silently label a multi-episode file as only its first episode.
    if ($clean -match '(?i)(?:\bEp(?:isode)?\s*|\s-\s)\d{1,4}\s*[-~]\s*\d{1,4}\b') {
        throw "Multiple episode numbers in '$($Video.Name)'; use separate episode files or name this output manually."
    }
    $patterns = @(
        '(?i)^(?<title>.*?)\s*[-. ]*\bS\d{1,2}[ .-]*E(?<episode>\d{1,4}(?:\.\d+)?)(?:v\d+)?(?!\d)',
        '(?i)^(?<title>.*?)\s*[-. ]*\b(?:EPISODE|EP|E)[ .-]*(?<episode>\d{1,4}(?:\.\d+)?)(?:v\d+)?(?!\d)',
        '(?i)^(?<title>.+?)\s+-\s+(?<episode>\d{1,4}(?:\.\d+)?)(?:v\d+)?(?:\s|$|[-])',
        '(?i)^(?<title>.*?)\s*(?:#)(?<episode>\d{1,4}(?:\.\d+)?)(?:\s|$)',
        '(?i)^(?<episode>\d{1,4}(?:\.\d+)?)(?:v\d+)?(?:\s*-\s+.*)?$',
        '(?i)^(?<title>.+?)\s+(?<episode>\d{1,4}(?:\.\d+)?)(?:v\d+)?$'
    )
    $match = $null
    foreach ($pattern in $patterns) {
        $candidate = [regex]::Match($clean,$pattern)
        if ($candidate.Success) { $match=$candidate; break }
    }
    if ($null -eq $match) {
        throw "Cannot identify an episode in '$($Video.Name)'. Use 'Anime Name - Ep05' (and the same subtitle basename)."
    }
    $title = Clean-AnimeTitle $match.Groups['title'].Value
    if ($AnimeName) { $title = Clean-AnimeTitle $AnimeName }
    elseif (-not $title) {
        $title = Clean-AnimeTitle $Video.Directory.Name
        if ($title -match '^(?i:Season\s*\d+|S\d+|Videos?|Episodes?|Subs|Subtitles)$') {
            if ($Video.Directory.Parent) { $title=Clean-AnimeTitle $Video.Directory.Parent.Name }
        }
    }
    if (-not $title -or $title -match '^(?i:Downloads?|Videos?|Episodes?|Season\s*\d+|S\d+)$') {
        throw "Anime title unclear for '$($Video.Name)'. Fill in the Anime title field."
    }
    if ($title.Length -gt 120) { $title=$title.Substring(0,120).TrimEnd() }
    $parts = $match.Groups['episode'].Value.Split('.')
    $episode = ([int]$parts[0]).ToString('D2')
    if ($parts.Length -gt 1) { $episode += '.' + $parts[1] }
    return "$title - Ep$episode"
}
function Get-NameHash([string]$Name) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash(
            [Text.Encoding]::UTF8.GetBytes($Name.ToLowerInvariant())))).Replace('-','').Substring(0,16)
    } finally { $sha.Dispose() }
}
function Read-BE32($Reader) {
    $bytes = $Reader.ReadBytes(4)
    if ($bytes.Length -ne 4) { throw 'Incomplete MP4 header.' }
    if ([BitConverter]::IsLittleEndian) { [array]::Reverse($bytes) }
    return [BitConverter]::ToUInt32($bytes,0)
}
function Find-Mp4Size($Reader, [long]$End, [int]$Depth = 0) {
    if ($Depth -gt 4) { throw 'Unexpected MP4 nesting.' }
    $stream = $Reader.BaseStream
    while ($stream.Position + 8 -le $End) {
        $start = $stream.Position
        [long]$size = Read-BE32 $Reader
        $type = [Text.Encoding]::ASCII.GetString($Reader.ReadBytes(4))
        [long]$header = 8
        if ($size -eq 1) {
            [long]$high = Read-BE32 $Reader
            [long]$low = Read-BE32 $Reader
            if ($high -gt 2147483647) { throw 'MP4 box too large.' }
            $size = $high * 4294967296L + $low
            $header = 16
        } elseif ($size -eq 0) { $size = $End - $start }
        if ($size -lt $header -or $size -gt ($End - $start)) { throw 'Invalid MP4 box size.' }
        $boxEnd = $start + $size
        if ($type -eq 'tkhd' -and $size -ge ($header + 8)) {
            $stream.Position = $boxEnd - 8
            $width = [int][math]::Round((Read-BE32 $Reader) / 65536.0)
            $height = [int][math]::Round((Read-BE32 $Reader) / 65536.0)
            if ($width -gt 0 -and $height -gt 0) { return "$($width)x$height" }
        } elseif ($type -in 'moov','trak') {
            $result = Find-Mp4Size $Reader $boxEnd ($Depth + 1)
            if ($result) { return $result }
        }
        $stream.Position = $boxEnd
    }
    return $null
}
function Get-Mp4Size([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $reader = New-Object IO.BinaryReader($stream)
    try {
        $resolution = Find-Mp4Size $reader $stream.Length
        if (-not $resolution) { throw 'Cannot read video dimensions from the encoded MP4.' }
        return $resolution
    } finally { $reader.Dispose() }
}
function Get-EncodeArguments($Job, [string]$TemporaryOutput) {
    # Explicit settings, independent of GUI presets.
    $arguments = @(
        '--input', $Job.Video.FullName, '--output', $TemporaryOutput,
        '--format', 'av_mp4', '--encoder', 'x264',
        '--encoder-profile', 'baseline', '--encoder-level', '3.0',
        '--encoder-preset', 'fast', '--quality', $Quality.ToString('0.0', [Globalization.CultureInfo]::InvariantCulture),
        '--maxWidth', '640', '--maxHeight', '480',
        '--non-anamorphic', '--modulus', '2', '--crop', '0:0:0:0',
        '--rate', '30', '--pfr',
        '--audio', [string]$AudioTrack,
        '--aencoder', 'av_aac', '--ab', '160', '--mixdown', 'stereo', '--arate', '48'
    )
    if ($null -ne $Job.Subtitle) {
        if ($Job.Subtitle.Extension -eq '.srt') {
            $arguments += @('--srt-file', $Job.Subtitle.FullName,
                '--srt-codeset', 'UTF-8', '--srt-lang', 'eng', '--srt-burn=1')
        } else {
            $arguments += @('--ssa-file', $Job.Subtitle.FullName,
                '--ssa-lang', 'eng', '--ssa-burn=1')
        }
    }
    return $arguments
}
function Emit([string]$Type, $Data) {
    Write-Host ('EVENT ' + (@{Type=$Type;Data=$Data}|ConvertTo-Json -Compress -Depth 5))
}
try {
    $SourceFolder=(Get-Item -LiteralPath $SourceFolder -ErrorAction Stop).FullName
    if($SelectedFile){$videos=@(Get-Item -LiteralPath $SelectedFile -ErrorAction Stop)}
    else{$videos=@(Get-ChildItem -LiteralPath $SourceFolder -File | Where-Object {
        $_.Extension -in '.mkv','.mp4','.avi','.m4v','.mov','.mpg','.mpeg','.wmv','.ts','.m2ts'
    } | Sort-Object Name)}
    if(-not $videos.Count){throw 'No supported videos found in the video folder.'}
    $jobs=@()
    foreach($video in $videos){
        try{
            $label=Get-EpisodeLabel $video
            if($PlanOnly){Emit 'NAME' ($label+' - <resolution>.mp4');return}
            $subs=@(Get-SubtitleCandidates $video)
            if($subs.Count -ne 1){throw "Expected one matching subtitle for $($video.Name); found $($subs.Count)."}
            $jobs+= [pscustomobject]@{Video=$video;Subtitle=$subs[0];Label=$label}
        }catch{
            if($SelectedFile){throw}
            Emit 'SKIP' $_.Exception.Message
        }
    }
    if($PlanOnly){throw 'Could not identify an episode number in the selected folder.'}
    if($FirstOnly -or $PreviewClip){$jobs=@($jobs|Select-Object -First 1)}
    if(-not $jobs.Count){throw 'No eligible episodes. Check episode filenames and matching subtitle files.'}
    if(-not (Test-Path -LiteralPath $EncoderPath -PathType Leaf)){throw 'Bundled HandBrakeCLI.exe is missing. Extract the complete app ZIP.'}
    New-Item -ItemType Directory -Path $WorkFolder -Force|Out-Null
    if(-not $PreviewClip){New-Item -ItemType Directory -Path $DestinationFolder -Force|Out-Null}
    $failed=0;$done=0;$index=0
    foreach($job in $jobs){
        $index++
        Emit 'JOB' @{Name=$job.Video.Name;Index=$index;Total=$jobs.Count}
        $temporary=Join-Path $WorkFolder ('clip-'+[guid]::NewGuid().ToString('N')+'.mp4')
        try{
            $arguments=@(Get-EncodeArguments $job $temporary)
            $arguments+=@('--json','--min-duration','0')
            if($PreviewClip){$arguments+=@('--start-at','seconds:0','--stop-at','seconds:10')}
            $previousPreference=$ErrorActionPreference
            $tail=New-Object 'Collections.Generic.Queue[string]'
            $state=''
            try{
                $ErrorActionPreference='Continue'
                & $EncoderPath @arguments 2>&1 | ForEach-Object {
                    $line=$_.ToString()
                    if($tail.Count -ge 12){$null=$tail.Dequeue()};$tail.Enqueue($line)
                    if($line -match '"State"\s*:\s*"([^"]+)"'){$state=$Matches[1]}
                    if($state -eq 'WORKING' -and $line -match '"Progress"\s*:\s*([\d.]+)'){
                        $percent=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture)*100
                        Emit 'PROGRESS' ([Math]::Min(99.9,$percent))
                    }
                }
                $exitCode=$LASTEXITCODE
            }finally{$ErrorActionPreference=$previousPreference}
            if($exitCode -ne 0 -or -not(Test-Path -LiteralPath $temporary)){throw ("HandBrake failed. "+($tail.ToArray() -join ' '))}
            $resolution=Get-Mp4Size $temporary
            Emit 'NAME' ($job.Label+' - '+$resolution+'.mp4')
            if($PreviewClip){Emit 'CLIP' $temporary}
            else{
                $finalPath=Join-Path $DestinationFolder ($job.Label+' - '+$resolution+'.mp4')
                if(Test-Path -LiteralPath $finalPath){throw "Output already exists; it was not overwritten: $finalPath"}
                Move-Item -LiteralPath $temporary -Destination $finalPath
                Emit 'SAVED' ([IO.Path]::GetFullPath($finalPath))
            }
            $done++
            Emit 'PROGRESS' 100
        }catch{
            $failed++;Emit 'ERROR' $_.Exception.Message
            if(Test-Path -LiteralPath $temporary){Remove-Item -LiteralPath $temporary -Force}
        }
    }
    Emit 'DONE' @{Successful=$done;Failed=$failed}
    if($failed){exit 1}
}catch{Emit 'ERROR' $_.Exception.Message;exit 1}
