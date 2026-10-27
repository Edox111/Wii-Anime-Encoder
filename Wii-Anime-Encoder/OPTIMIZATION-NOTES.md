# Optimization review

## Changes

- Cached preferred label sizes until their text, font, padding or width changes.
  This avoids repeated GDI text measurement during nested table layouts.
- Painted transparent labels directly from the shared background cache.
- Restricted background copies to the invalidated control rectangle.
- Reused slider tick fonts, text outline offsets and the UI JSON serializer.
- Stopped the status timer when idle; folder/title edits and conversions restart it.
- Limited each status-processing turn to 8 ms / 250 messages. Repeated percentage
  updates are combined into one label update per turn; completion events remain queued.
- Replaced UI-thread process-output waiting with asynchronous end-of-stream signals.
- Protected active preview playback from reentrant temporary-file cleanup.
- Released background bitmaps, tooltips, timer and slider fonts on disposal.
- Replaced repeated batch-array copying with a growing list.
- Split the monolithic form into focused source files and removed retired controls.
- Updated executable/script names, build scripts, tests and documentation together.

## Evidence and limits

With identical artwork, a paired local test of 35 interactive resize/repaint
iterations measured 3683 ms for the original build and 2429 ms for this build:
about 34% less elapsed time. An earlier optimized run measured 2434 ms.
Results vary by machine, graphics stack and desktop load.

The app remains event-driven WinForms; this does not guarantee a specific frame
rate on a high-refresh monitor. HandBrake settings are unchanged, so these changes
do not promise faster video encoding.

Passed: build; fresh preferences and restart; subtitle defaults; RF default;
idle timer; burst-progress ordering and completion events; resizing at 1100x720,
1300x900 and 1920x1080; fullscreen restore; naming; real preview playback and
cleanup; cancellation cleanup; real two-episode batch; session output chooser.

Automated tests do not replace final visual review and Wii playback checks.
