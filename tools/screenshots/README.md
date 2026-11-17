# Screenshot generator

Development tool for native StagePad Live screenshots in Italian, English,
Spanish and French. The demo uses isolated sample data and does not open
Audio/MIDI devices or send OSC.

## Build and capture

On Windows with the .NET SDK from the repository global.json:

```powershell
.\tools\screenshots\Build-Screenshots.ps1
```

The build runs the demo data checks and prepares `artifacts/screenshot-demo`
at the repository root. Open `Cattura-Screenshot.cmd` in that output folder.
It creates four localized folders and a gallery. See the included
`SCREENSHOTS-README.txt` for capture details.

The two projects and all capture scripts are kept here. They are separate
from the main application build and are not required to run StagePad Live.
Documentation images remain in `docs/screenshots`.
