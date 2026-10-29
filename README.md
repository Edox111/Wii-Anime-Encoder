# Wii Anime Encoder

This working tree is the unreleased v1.1.0 update, with eleven photo themes and Pause/Resume.

Choose a theme from the top-right dropdown. Midnight is the neutral default;
eleven photographic themes share the same encoding controls. The choice
is saved per Windows account. Scarlet uses a castle under a red night sky; Crimson uses a glowing blood moon.
Use Credits to read the photo and software acknowledgements.

During conversion, **Pause** changes to **Resume** once the worker acknowledges it.
The current encoder stays alive and continues the same output when resumed.
Keep the application open: this is not a checkpoint that survives exit or reboot.
**Cancel** asks for confirmation, discards unfinished work and keeps completed videos.
Closing the application during a job also asks before cancelling it.

A Windows desktop tool for preparing subtitled anime for Wii media playback.

Choose video, subtitle and output folders, preview a 10-second clip, then encode a batch. The app burns external subtitles into H.264 MP4 files and names each result automatically.

**Status:** 1.1.0 development build. The owner confirmed Wii playback; compatibility still depends on your player and source material. This is a native Windows app, not a GitHub Pages web app.

## Features

- External SSA, ASS and SRT subtitle matching and burn-in.
- Automatic names: `Series - Ep01 - 640x480.mp4` (actual dimensions may differ).
- Maximum 640×480, preserved aspect ratio, up to 30 fps and AAC stereo.
- RF 18–22 in half steps; default **19**. Audio track selection from 1–8.
- Fullscreen 10-second preview and per-episode progress.
- Empty folders on a new user's first launch; saved folders on later launches.
- Local processing with no telemetry or automatic updater.

## Compiled application

Extract the compiled ZIP into a writable folder. Double-click
Setup-HandBrake.cmd once, then open WiiAnimeEncoder.exe. No compilation or
administrator access is required. Setup needs internet to download and verify
HandBrake; ordinary conversion runs locally afterward.

The compiled package and GitHub's automatic source ZIP serve different purposes.
The application package includes eleven licensed photographic backgrounds.

## Build and run

Requires Windows x64, Windows PowerShell 5.1 and .NET Framework 4.8.
Windows 11 normally includes these components. Windows N may need the Media Feature Pack.

Open PowerShell in this repository and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\setup-handbrake.ps1
.\dist\Wii-Anime-Encoder\WiiAnimeEncoder.exe
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

- `src/MainForm*.cs`: application lifecycle, layout, rendering and encoding.
- `src/Controls/`: themed controls; `src/PreviewPlayer.cs`: fullscreen playback.
- `scripts/Convert-Episodes.ps1`: naming, subtitle matching and HandBrake process orchestration.
- `scripts/Invoke-EncodingJob.ps1`: isolated conversion entry point.
- `build.ps1`: local build; no network access.
- `setup-handbrake.ps1`: explicit, verified dependency download.
- `tests/Test-Application.ps1`: first-launch, preferences and default-quality checks.

Run the smoke test after building:

```powershell
powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tests\Test-Application.ps1
```

GitHub Actions builds and runs this test on Windows. It does not test actual Wii
playback or full encoding. Report bugs with Windows version, reproduction steps,
source format and the visible error. Do not attach copyrighted episodes or
private folder information.

## Photo themes

All eleven backgrounds are included in assets/themes and copied by build.ps1.
The app uses an embedded transparent yellow-star icon at 16, 32, 48 and 256 px. Source and photographer credits are in
assets/themes/CREDITS.txt. Build with -NoTheme for palette-only local testing.

## License and credits

Custom app code, scripts and documentation are offered under the [MIT License](LICENSE).
Third-party photographs, fonts and HandBrake are outside that license.
Read [credits and license scope](CREDITS-AND-LICENSES.txt).

This is an independent project with no affiliation or endorsement from Nintendo
or HandBrake. No anime footage or subtitles are distributed.

## Publishing

Upload this repository's source files to GitHub; omit `dist/`, local assets,
settings and test media. The included `.gitignore` covers these paths.
Use a new tag v1.1.0 for this release, targeting the final tested commit on main.

HandBrake is obtained separately by each user. Do not upload your locally
populated `dist/` as a release without reviewing the licenses and corresponding
source obligations of any bundled third-party components. Use footage and music
you are authorized to publish in demonstrations.

See [UPGRADING.md](UPGRADING.md) for the renamed files and [OPTIMIZATION-NOTES.md](OPTIMIZATION-NOTES.md) for measurements and validation.

## Creating a release package

Run package-release.ps1 with Windows PowerShell. All eleven photo themes are included.
Packages and SHA-256 files are written to artifacts/.

The packager excludes HandBrake binaries even if installed locally, plus preferences
and test media. Users obtain HandBrake through the included setup launcher.
See RELEASE-CHECKLIST.md for the remaining publication steps.

Theme photos load offline and keep the existing controls and button colors. Photo credits and license links are available in Credits and assets/themes/CREDITS.txt. Scarlet uses a nighttime castle photograph. Cancel resets the percentage to zero; completed videos remain available.
