WII ANIME ENCODER
Quick guide

GET STARTED
1. Extract the entire ZIP into a writable folder on your Windows x64 PC.
2. Double-click Setup-HandBrake.cmd once. It downloads and verifies the official
   HandBrakeCLI 1.9.2 encoder. Internet is needed only for this setup step.
3. After setup succeeds, open WiiAnimeEncoder.exe.
4. Choose Video folder, Subtitle folder and a separate Completed videos folder.
   Subtitle folder initially follows your video selection.
5. Keep RF quality at 19 and Audio track at 1, or adjust them for your files.
6. Click Preview to test an episode, or Start encoding to process the batch.

No compilation or administrator access is required for this compiled package.
Keep all extracted files together. Do not run the app from inside the ZIP.
Requirements: Windows x64, .NET Framework 4.8 and Windows PowerShell 5.1.
Windows N may need the Media Feature Pack for preview playback.
For source builds, follow README.md in the source repository.

PREPARE YOUR EPISODES
Keep videos directly in the chosen folder (subfolders are not scanned).
Each episode needs one matching external .ssa, .ass or .srt subtitle:
  Example Show - Ep01.mkv
  Example Show - Ep01.ass
Use clear episode numbers. Missing or ambiguous subtitle matches are skipped.
Subtitles may also be in direct Subs or Subtitles subfolders.
Subtitles are burned into the picture and cannot be turned off afterward.

SETTINGS AND FILENAMES
RF quality: 18-22 in 0.5 steps; default 19. Lower values mean higher quality/larger files.
Audio track: 1-8. Select a source track that exists in every episode.
Anime title: optional; overrides the automatically detected series name.
The filename preview updates automatically:
  Example Show - Ep01 - <resolution>.mp4
Actual resolution is confirmed after encoding. Output is H.264 MP4, up to
640x480 and 30 fps, with preserved aspect ratio and AAC stereo audio.
Existing output files are never overwritten.

PREVIEW AND RESULTS
Preview: choose an episode to convert and play its first 10 seconds fullscreen.
  Escape closes the preview; Space pauses/resumes; R replays.
  The temporary clip is deleted when you close it.
Start encoding: processes all eligible episodes. Progress appears when encoding
starts and shows the current episode's percentage.
Pause: keeps the current encoder alive and changes to Resume when acknowledged.
Resume: continues that same encode. Keep the app open; pause does not survive
  closing the application, a Windows shutdown, or a crash.
Cancel: asks before discarding unfinished work. Completed videos are retained.
Theme: choose from eleven photographic themes in the top-right dropdown.
  Midnight is the neutral default. The selection is remembered per Windows account.
  Scarlet shows a castle at night; Crimson shows a glowing blood moon.
  Credits lists the included photographs and software components.
  A yellow-star icon identifies the application across all themes.
Open output: opens one completed video, or offers a chooser for several.
  This list covers the current app session; saved videos remain after exit.
F11 toggles the app's fullscreen mode; Escape exits fullscreen.

IF SOMETHING GOES WRONG
No eligible episodes: check episode numbers and matching subtitle filenames.
No audio: check that the selected audio track exists in the source.
Missing encoder: run Setup-HandBrake.cmd and wait for verification to finish.
Output already exists: rename/move the old file or choose another output folder.
Temporary-file error: move the app to a writable folder with enough free space.
Preview unavailable on Windows N: the Media Feature Pack may be needed.

FILES AND CREDITS
Videos are processed locally; this app has no telemetry or automatic updater.
Folder fields start empty for new users and are remembered after normal exit.
Updates retain saved folders for the same Windows account.
Folder preferences: %LOCALAPPDATA%\WiiAnimeEncoder\settings.json
No persistent conversion logs or .completed files are created.
Temporary .work-... folders beside the app are normally removed automatically.
After a crash, remove leftover temporary folders only when the app and encoder
are closed. Keep backups of your original files.

See CREDITS-AND-LICENSES.txt for software credits, asset information,
distribution notes, and the complete HandBrake license notices.

Theme photographs load offline. Credits lists photographers and licenses. Cancelling resets progress to zero and keeps completed videos.
