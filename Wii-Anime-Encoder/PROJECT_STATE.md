# Current state

Pre-release optimization candidate; no public release has been created.
Native Windows x64 WinForms UI with WPF preview playback.
The executable is WiiAnimeEncoder.exe; the window remains Scarlet Edition.
RF 19, folder persistence, automatic names, previews, batch conversion and
session output selection are preserved.

Source lives in src/ (partial MainForm, reusable controls, PreviewPlayer and
Program). Runtime scripts live in scripts/. build.ps1 copies runtime scripts
beside the executable. Private theme assets remain in ignored assets/local/.

Validation:
- Build and isolated preference/progress tests passed locally.
- Minimum/windowed layout and fullscreen restoration passed.
- Real two-episode batch conversion passed.
- Automatic naming, real 10-second preview and preview cleanup passed.
- Cancellation cleanup and output chooser passed.
- Original GitHub workflow passed per owner; updated workflow needs its first run.
- New version still needs hands-on Wii playback verification.

Local themed resize benchmark: latest paired run was 3683 ms before and 2429 ms
after for 35 resize/repaint steps (about 34% lower elapsed time).
This is not a monitor refresh-rate or playback benchmark.

Next: owner reviews optimization candidate, then remaining release preparation.
Halloween remains planned. Asset permissions remain the owner's task.

Slider follow-up: removed full-control focus rectangle; selected-value labels fill their columns. All drag steps and keyboard steps verified at minimum and standard window sizes.
