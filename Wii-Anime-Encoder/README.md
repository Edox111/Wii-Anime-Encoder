# Wii Anime Encoder | Scarlet Edition

A Windows desktop tool for preparing subtitled anime for Wii media playback.

Choose video, subtitle and output folders, preview a 10-second clip, then encode a batch. The app burns external subtitles into H.264 MP4 files and names each result automatically.

**Status:** pre-release. Tested locally on Windows; playback compatibility depends on your Wii player and source material. This is a native Windows app, not a GitHub Pages web app.

## Features

- External SSA, ASS and SRT subtitle matching and burn-in.
- Automatic names: `Series - Ep01 - 640x480.mp4` (actual dimensions may differ).
- Maximum 640×480, preserved aspect ratio, up to 30 fps and AAC stereo.
- RF 18–22 in half steps; default **19**. Audio track selection from 1–8.
- Fullscreen 10-second preview and per-episode progress.
- Empty folders on a new user's first launch; saved folders on later launches.
- Local processing with no telemetry or automatic updater.

## Build and run

Requires Windows x64, Windows PowerShell 5.1 and .NET Framework 4.8.
Windows 11 normally includes these components. Windows N may need the Media Feature Pack.

Open PowerShell in this repository and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\setup-handbrake.ps1
.\dist\Wii-Anime-Encoder\Wii-Anime-Scarlet.exe
```

Build creates the app in `dist/Wii-Anime-Encoder`. Setup separately downloads the
official HandBrakeCLI **1.9.2** Windows x64 archive, verifies its pinned SHA-256,
and installs it locally. HandBrake binaries are not included in this repository.
Review scripts before running them; administrator privileges are not required.

## Use

1. Choose the **Video folder**.
2. Keep **Subtitle folder** at its default or choose another location.
3. Choose a separate **Completed videos** folder.
4. Keep RF **19** and audio track **1**, or adjust them for your source.
5. Click **Preview** to check an episode; click **Start encoding** for the batch.

Use matching names such as `Example Show - Ep01.mkv` and
`Example Show - Ep01.ass`. Videos are read directly from the chosen folder.
Missing or ambiguous subtitle matches are skipped. Subtitles become permanent
parts of the output picture.

**Preview:** Escape closes, Space pauses/resumes, R replays. F11 toggles app
fullscreen. **Open output** opens a completed file or lets you choose among
files converted during the current session.

See [the quick guide](README.txt) for troubleshooting, cleanup and settings.

## Development

- `EncoderApp.cs`: WinForms UI and WPF preview player.
- `engine.ps1`: naming, subtitle matching and HandBrake process orchestration.
- `runner.ps1`: isolated conversion entry point.
- `build.ps1`: local build; no network access.
- `setup-handbrake.ps1`: explicit, verified dependency download.
- `tests/smoke.ps1`: first-launch, preferences and default-quality checks.

Run the smoke test after building:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tests\smoke.ps1
```

GitHub Actions builds and runs this test on Windows. It does not test actual Wii
playback or full encoding. Report bugs with Windows version, reproduction steps,
source format and the visible error. Do not attach copyrighted episodes or
private folder information.

## Optional personal theme

The public source package uses the app's dark fallback background and default
Windows icon. For a personal build, place `remilia.jpg` and/or `app.ico` in
`assets/local/` before building. These files are ignored by Git and are not
included in the public package. Clear relevant asset permissions before adding
them to a public repository, screenshots or downloadable release.

## License and credits

Custom app code, scripts and documentation are offered under the [MIT License](LICENSE).
Third-party artwork, characters, fonts and HandBrake are outside that license.
Read [credits and license scope](CREDITS-AND-LICENSES.txt).

Touhou Project / Remilia Scarlet: Team Shanghai Alice (ZUN).
Original custom background credit: UGUME (うぐめ), Metal Luck.
This is an unofficial project with no affiliation or endorsement from Nintendo,
Team Shanghai Alice or HandBrake. No anime footage or subtitles are distributed.

## Publishing

Upload this repository's source files to GitHub; omit `dist/`, local assets,
settings and test media. The included `.gitignore` covers these paths.
Keep this version marked as a pre-release until the remaining app changes are done.

HandBrake is obtained separately by each user. Do not upload your locally
populated `dist/` as a release without reviewing the licenses and corresponding
source obligations of any bundled third-party components. Use footage and music
you are authorized to publish in demonstrations.
