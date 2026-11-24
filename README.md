<h1><img src="StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

Created by [@Alessio278](https://github.com/Alessio278)


Use StagePad Live to launch audio tracks and send MIDI and OSC cues from a Windows PC. Prepare the pads in Setup, then switch to Live for the performance.

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE) ![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](README.md) - [Italiano](docs/README.it.md) - [Español](docs/README.es.md) - [Français](docs/README.fr.md)

## What's New

Each release lists the changes since the previous stable version. [Read the latest release notes](https://github.com/Alessio278/StagePad-Live/releases/latest)

## Contents

- [Quick start](#quick-start)
- [Features](#features)
- [Audio playback](#audio-playback)
- [Markers and loops](#markers-and-loops)
- [MIDI and OSC](#midi-and-osc)
- [Shows and saving](#shows-and-saving)
- [Screenshots](#screenshots)
- [Windows SmartScreen](#windows-smartscreen)
- [Build and test](#build-and-test)
- [License and contributions](#license-and-contributions)

## Quick start

1. Extract **StagePad-Live-1.4-Windows-Preview.zip** and run **StagePad-1.4.exe**.
2. In Setup, choose the audio device and MIDI ports. Start with a 40 ms audio buffer and run the audio test.
3. Load audio, configure slot actions and use LEARN to assign MIDI pads. Choose the interface language from the gear icon.
4. Save the show, switch to Live and enable SHOW LOCK. F11 toggles full screen; STOP ALL stops audio and cancels pending commands.

Open a copy of your 1.3 show before testing. Version 1.4 reads older shows and saves in schema 5, including when auto-save runs. Keep the original for use with 1.3.

## Features

| Area | Details |
| --- | --- |
| Pads | 1-100 slots; independent Setup and Live grids, up to 10 x 10 |
| Audio | One-shot, Hold, protected and toggle-pause playback; fades; slot and master volumes |
| Actions | Audio, MIDI and OSC combined, Global pause, or exclusive + Marker |
| Control | MIDI LEARN, separate master/track CC assignments, relative encoders and soft takeover |
| Shows | New, Open, Save, Save as, rename, duplicate, delete, auto-save and export with audio |
| Interface | Italian, English, Spanish and French; colored pads, scrolling titles and waveform player |

### Slot launch options

- **Block other audio pads.** While this slot plays, tracks already running continue. Pressing another audio pad discards the entire cue, including its MIDI and OSC commands.
- **Stop other audio slots on start.** The new track starts immediately. Each outgoing track uses its own fade-out.
- **Hold for three seconds.** Use this on ordinary audio, MIDI and OSC pads as well as Global pause. Release early to cancel. For MIDI, use a momentary pad that sends Note Off.

## Audio playback

Tracks up to 25 minutes can play from RAM when the memory budget allows. Otherwise StagePad reads ahead from disk. In the player, gold marks elapsed audio and light gray marks the loop.

[Audio engine and memory](docs/AUDIO.md)

## Markers and loops

Track boundaries are set in **Manage markers > Track start and end**. Choose a Marker for either boundary, or keep **File start / File end**. Stop the Slot before applying a cut. Markers used as boundaries cannot be moved or deleted until the full track is restored.

Use **+ Marker** in the player to add at the cursor. During playback it inserts without opening the editor. **Manage markers** edits position, label, color and MIDI/OSC triggers. Double-click the waveform or a marker to seek.

A slot can use the exclusive **+ Marker** action; an additional MIDI key can be learned in **Settings > Quick marker insertion**. Both capture on the selected playing track in Setup, with SHOW LOCK off.

Choose two existing markers and press **Apply loop**. Playback starts at the first marker; **Remove loop** clears the interval. Stop the track before changing the loop. During loops the player frame pulses slowly in red; outside loops, marker crossings flash seven times in the marker color. Marker output commands continue on each loop pass.

<a href="docs/screenshots/en/03-player-marker.png"><img src="docs/screenshots/en/03-player-marker.png" width="1100" alt="Waveform Player with colored Markers"></a>

## MIDI and OSC

Each slot supports up to 16 MIDI and 16 OSC commands, with order and delays. The two lists run independently. MIDI supports Note, CC, Program Change and MSC; OSC uses UDP with editable host, port, address and typed arguments. A local send count does not confirm receipt.

[MIDI](docs/MIDI.en.md) - [OSC](docs/OSC.en.md) - [Update](docs/UPDATE.en.md)

## Shows and saving

Shows open and save inside the app. Auto-save is on by default, writes in the background and retains the previous file as a `.bak`. Startup restores the last show without starting playback.

The default show folder is `%LOCALAPPDATA%\StagePad\project`; change it in Settings. A JSON show references its audio files. Use **Export show with audio** to move a complete show to another PC.

<a href="docs/screenshots/en/13-shows.png"><img src="docs/screenshots/en/13-shows.png" width="1100" alt="Built-in Show browser"></a>

## Screenshots

These screenshots show version 1.1 with demo data. Select an image to view it at full size.

<a href="docs/screenshots/en/02-live.png"><img src="docs/screenshots/en/02-live.png" width="1100" alt="Live: Audio and MIDI/OSC Pads"></a>

<table>
<tr>
<td align="center" width="50%"><strong>Audio Slot</strong><br><a href="docs/screenshots/en/06-slot-audio.png"><img src="docs/screenshots/en/previews/06-slot-audio.png" width="360" alt="Audio Slot"></a></td>
<td align="center" width="50%"><strong>Marker editor</strong><br><a href="docs/screenshots/en/05-markers.png"><img src="docs/screenshots/en/previews/05-markers.png" width="360" alt="Marker editor"></a></td>
</tr>
<tr>
<td align="center" width="50%"><strong>Emergency protection</strong><br><a href="docs/screenshots/en/09-emergency.png"><img src="docs/screenshots/en/previews/09-emergency.png" width="360" alt="Emergency protection"></a></td>
<td align="center" width="50%"><strong>Settings</strong><br><a href="docs/screenshots/en/11-settings.png"><img src="docs/screenshots/en/previews/11-settings.png" width="360" alt="Settings"></a></td>
</tr>
</table>

[Open the full gallery: 13 screenshots](docs/screenshots/en/README.md)

## Windows SmartScreen

The executable is unsigned, so Windows may show “Windows protected your PC” or “Unknown publisher”. Download from the official Releases page and compare SHA-256 with `SHA256.txt`. If you trust the file, Windows may offer **More info > Run anyway**; managed PCs can restrict this option.

## Build and test

Requires Windows 10/11 x64 and .NET SDK 10 for building. The portable download includes its runtime. WAV and OGG use included decoders; other formats depend on Windows codecs.

```powershell
.\Build.ps1
```

The script runs core and simulated integration tests, publishes the app, runs native WPF checks and creates the Windows ZIP. Complete the [testing checklist](docs/TESTING.md) with your equipment before a show.

## License and contributions

StagePad Live is distributed under the [MIT License](LICENSE). Third-party notices are in [licenses](licenses/) and in Settings. See [CONTRIBUTING.md](CONTRIBUTING.md), [SECURITY.md](SECURITY.md) and the [changelog](CHANGELOG.md).
