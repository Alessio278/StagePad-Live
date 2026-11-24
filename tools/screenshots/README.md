# Capture the StagePad Live interface

This Windows development tool captures the native interface with an isolated demo show in Italian, English, Spanish and French. It uses sample data and sends no audio, MIDI or OSC output.

1. Install the Windows .NET SDK specified in [global.json](../../global.json).
2. From the repository root, run the build command below. It checks the demo data and creates the portable capture tool in `artifacts/screenshot-demo`.
3. Open `Cattura-Screenshot.cmd` in that folder. Leave the windows untouched until the gallery opens.

```powershell
.\tools\screenshots\Build-Screenshots.ps1
```

Each language folder contains 13 PNG files, a README and a capture report. The top-level HTML index links to all four galleries.

[Portable capture instructions in Italian](SCREENSHOTS-README.txt) · [Documentation galleries](../../docs/screenshots/README.md)
