param([string]$Config)
$ErrorActionPreference='Stop'
[Console]::OutputEncoding = New-Object Text.UTF8Encoding($false)
$OutputEncoding = [Console]::OutputEncoding
$c=Get-Content -LiteralPath $Config -Raw -Encoding UTF8|ConvertFrom-Json
Remove-Item -LiteralPath $Config -Force
& "$PSScriptRoot\Convert-Episodes.ps1" -SourceFolder $c.Video -SubtitleFolder $c.Subtitle -DestinationFolder $c.Output -EncoderPath "$PSScriptRoot\HandBrakeCLI.exe" -AnimeName $c.Title -Quality $c.Quality -AudioTrack $c.Audio -FirstOnly:([bool]$c.First) -PlanOnly:([bool]$c.Plan) -SelectedFile $c.Selected -WorkFolder $c.Work -PreviewClip:([bool]$c.Preview) -NonInteractive
