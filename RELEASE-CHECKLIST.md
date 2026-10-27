# Release candidate checklist

Completed: compiled application, verified dependency setup, clean extraction, version metadata, Unicode protocol, regression checks, license notices, and explicit package file whitelist. Owner confirmed Wii playback.

Before publishing:

- Receive artwork permission and apply any requested attribution.
- Pass the updated GitHub workflow on the final source revision.
- Change 1.0.0-rc.1 to 1.0.0 in AssemblyInfo.cs and package-release.ps1.
- Rebuild and regenerate the SHA-256 checksum.
- Attach the compiled ZIP and checksum to the release draft.
- Publish only when the owner requests it.

The automatic GitHub source ZIP is not the compiled application. Use the ZIP generated in artifacts/ as the application download.
