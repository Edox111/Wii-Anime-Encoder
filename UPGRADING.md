# Updating your GitHub repository

This is the final v1.0.0 source update. Uploading source does not publish a GitHub release.

1. Extract the project ZIP.
2. Upload the contents of Wii-Anime-Encoder into the root of your existing
   repository. Keep src/, scripts/ and tests/ as folders.
3. Replace build.ps1, setup-handbrake.ps1 and the documentation.
4. Update .github/workflows/build.yml using GitHub's editor if the browser upload
   does not accept the hidden folder. The included workflow uses checkout@v7.
5. Delete the old root files EncoderApp.cs, engine.ps1 and runner.ps1, and the old
   tests/smoke.ps1. Their replacements are already in src/, scripts/ and tests/.
6. Remove any old nested duplicate Wii-Anime-Encoder directory only after checking
   that it contains no unique files.
7. Commit, then check that Windows build, Smoke test, and Layout and fullscreen pass.

Do not upload dist/, assets/local/, personal settings, or media files.
Keep .gitignore, .gitattributes and .editorconfig at the repository root.

For local use, rebuild and run setup-handbrake.ps1 as described in README.md.
Launch dist/Wii-Anime-Encoder/WiiAnimeEncoder.exe. The saved folder locations
continue to use the same Windows user settings file.

The renamed runtime scripts must stay beside the new executable:
Convert-Episodes.ps1 and Invoke-EncodingJob.ps1. Do not mix executable and script
files from different versions. Extract a review package into its own folder.

Release packaging additions: upload distribution/, package-release.ps1, src/Properties/ and the installer/protocol tests along with all other source changes. The compiled app ZIP is a release attachment, not repository source.
