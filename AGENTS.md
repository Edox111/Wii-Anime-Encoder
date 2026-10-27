# Project rules

- Native Windows WinForms app with a WPF preview player; do not replace with a web app.
- Build: powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\build.ps1
- Check: powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -File .\tests\smoke.ps1
- Preserve RF 18-22 in 0.5 steps, default 19; audio tracks 1-8.
- New users have empty folders; settings persist per Windows account.
- Preserve existing layout and encoding behavior unless requested.
- Keep source in one folder; do not create versioned copies for routine changes.
- Do not commit dist, artwork without permission, personal settings or media fixtures.
- Run focused checks. Package a release only when requested.
- Tests must isolate preferences; never alter the user's AppData settings.
- Update PROJECT_STATE.md when behavior or release status changes.
