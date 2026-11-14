<h1><img src="StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

**Open-source audio launcher and MIDI/OSC cue controller for Windows live shows.**

[![Release](https://img.shields.io/github/v/release/Alessio278/StagePad-Live?label=release&color=239f88)](https://github.com/Alessio278/StagePad-Live/releases) [![Windows build](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4)](#requirements-and-limits) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Audio MIDI OSC](https://img.shields.io/badge/control-Audio%20%2B%20MIDI%20%2B%20OSC-239f88) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](README.md) - [Italiano](docs/README.it.md) - [Español](docs/README.es.md) - [Français](docs/README.fr.md)

## Contents

- [Overview](#overview)
- [Quick start](#quick-start)
- [Features](#features)
- [Markers and loops](#markers-and-loops)
- [Projects and auto-save](#projects-and-auto-save)
- [MIDI and OSC](#midi-and-osc)
- [Updates](#updates)
- [Requirements and limits](#requirements-and-limits)
- [Build and test](#build-and-test)
- [Contributing and license](#contributing-and-license)

- [Windows SmartScreen notice](#windows-smartscreen-notice)

## Overview

Prepare audio and commands in **Setup**, then trigger them in **Live** with the mouse or a MIDI controller. StagePad Live is designed for concerts, theatre, rehearsals and installations. No account, activation or online license verification is required.

Created by **Alessio Natalucci**. Application name: **StagePad Live**. The portable executable is named `StagePad.exe`.

## Quick start

1. Download `StagePad-Live-1.0.0-Windows.zip` from [Releases](https://github.com/Alessio278/StagePad-Live/releases), extract it and launch `StagePad.exe`.
2. In Setup choose the audio device, start with a **40 ms** buffer and run the audio test.
3. Choose 1-100 slots and a grid up to **10 x 10**. Load audio and wait until it is ready.
4. Configure each slot's actions, playback mode, title, color and fades. Use **LEARN** to assign a MIDI pad; the learning press does not trigger playback.
5. Use **New show**, **Open**, **Save** or **Save as**. Auto-save is enabled by default.
6. Switch to Live, enable **SHOW LOCK** and use **F11** for full screen. **STOP ALL** stops audio and cancels pending output actions.

The default language is Italian. Use the gear icon to select **Italian / English / Spanish / French** without restarting audio.

## Features

| Area | Included |
| --- | --- |
| Pads | 1-100 slots; separate Setup and Live grids, up to 10 x 10 |
| Audio | Play, pause, stop, one-shot/retrigger, Hold, protected and toggle-pause modes |
| Mixer | Per-slot gain and master volume, fades and bounded memory budget |
| Control | Audio, MIDI and OSC independently or in any combination |
| Player | Waveform, double-click seek, elapsed/total/remaining time |
| Markers | Editable time, label and color; MIDI/OSC triggers; marker-to-marker loop |
| Shows | In-app browser, New/Open/Save/Save as, rename, duplicate and confirmed delete |
| Auto-save | Background writes, provisional names, previous-file backup and session recovery |
| Performance | Show Lock, emergency pause slots and optional scroll-to-triggered-pad |
| Interface | Four languages, per-slot colors, scrolling long titles and dark scrollbars |

An **EMERGENCY** slot pauses all playing/fading tracks, preserving positions, and can transmit its own MIDI/OSC messages. STOP ALL also resets positions. These controls act on the software; verify the response of external receivers.

## Markers and loops

**+ Marker** immediately inserts a marker at the current cursor position, including with a loop applied. In **Manage markers**, the Position field follows cursor movements unless the field has keyboard focus; selecting an existing marker initially displays its saved position. Edit or delete there; double-click a marker to seek. Up to **32 markers** per track, with a bounded metadata size.

In Manage markers, choose two **existing** markers and press **Apply loop**; use **Remove loop** to clear it. End must follow start by at least 50 ms. Stop the slot before applying/removing the loop, and remove it before moving/deleting its endpoints. Play starts directly at the loop start marker; pause resumes from its retained position. Other markers can still be added, edited or deleted while looping. The interval is prepared in RAM within the configured budget; 5 ms edge ramps reduce clicks. Labels, colors and triggers remain editable.

Each marker can send one immediate MIDI command, one OSC command, or both on playback crossing, including each loop pass. MIDI notes use automatic Note Off. Manual seeking does not send skipped cues or the cue exactly at the destination.

A playing track has a soft red player frame. Inside an active loop, the red frame pulses slowly (1.6-second cycle); no colored marker flashes are shown. Outside loops, crossing a marker produces **7 fast flashes** of its color on the frame and time readouts: 150 ms on / 110 ms off, **1.82 seconds** total, with a stronger halo. With Windows animations disabled the cue stays steady instead.

**Settings > MIDI markers (Setup) > LEARN marker insertion** assigns an unused Note On/Off button to create markers on the selected playing track. Holding it does not create duplicates. Capture is disabled in Live, while paused/stopped/loading and with Show Lock. Assignments are saved in the show.

While playing, **+ Marker** inserts silently without opening the editor. An enabled loop shows only the slow red pulse from initial Play onward, including all marker crossings and wraps. Marker MIDI/OSC commands still run. Outside loops, during a colored cue, the red frame is suppressed for the entire seven-flash sequence, including dark gaps, then restored.

## Projects and auto-save

**Save** updates the current show directly. **Save as** opens the integrated name field. Double-click a row or press Enter to open a show. **More actions** contains Rename, Duplicate, Delete, Import and Export; functions are retained. Switching shows with unsaved changes offers Save / Discard / Cancel. Stop audio and sequences before creating or opening another show.

Auto-save is ON by default and can be disabled in Settings. After about **1.5 seconds** without edits it writes the latest document in the background, including while audio plays. An unnamed show gets a unique provisional filename and appears in the list. An asterisk indicates pending changes. Writes are atomic, a previous `.bak` is retained, and failed writes keep the document dirty for retry. Graceful close attempts a bounded final save; session recovery remains available.

Default data directory: `%LOCALAPPDATA%\StagePad`. Default show directory: `%LOCALAPPDATA%\StagePad\project`. Settings can choose another project directory; **Default** restores this location. Changing it does not move existing files.

Startup restores the last show and recent recovered edits without automatically starting playback. Old development data is migrated once without replacing existing named shows. Archived source data, including any referenced internal audio, stays in `backups\legacy-data` and must remain available to recovered shows.

A JSON show normally references its audio files. **Export show with audio** creates a `.stagepad.zip`; Import creates a separately named show without overwriting. Packages support up to 8 GiB of original media.

## MIDI and OSC

Select **Audio**, **MIDI** and/or **OSC** in slot settings. Audio, MIDI and OSC are dispatched independently. A missing audio file does not prevent the selected MIDI/OSC output.

Each slot supports up to **16 MIDI commands** and **16 OSC commands**. MIDI includes Note Pulse/Gate, Control Change, Program Change and the implemented MSC GO/STOP/RESUME formats. Pad input is Note On/Off; master and current-playing-track volume can use separate CC assignments with LEARN, absolute faders, relative encoders and soft takeover. MIDI clock, MIDI thru and per-marker Gate are not implemented.

OSC uses UDP with per-command host/IP, port, address and typed arguments. Receivers and virtual MIDI ports are configured separately. Local send counts confirm transmission, not reception. Delays, reordering and one-command tests are available in slot settings. Reconnect does not replay previous triggers.

See [MIDI guide](docs/MIDI.en.md) and [OSC guide](docs/OSC.en.md).

## Updates

**Settings > Updates** reads stable releases from this repository. Installation is allowed while idle, verifies SHA-256 and the Windows executable's product/version, retains a backup and attempts rollback if startup fails. You can also close the app and replace only `StagePad.exe`; user data stays in AppData.

An unsigned build may display Windows SmartScreen's unknown-publisher prompt. The project does not claim a signing certificate or hardware certification. See [update guide](docs/UPDATE.en.md).

## Requirements and limits

- Desktop application: **Windows 10/11 x64**. macOS and Linux desktop builds are not provided.
- The portable Windows build includes its .NET runtime. Building needs **.NET SDK 10** selected by `global.json`.
- WAV PCM/float and OGG Vorbis use included decoders. MP3, FLAC, M4A/AAC and other formats depend on installed Windows Media Foundation codecs. Mono/stereo are supported; DRM, multichannel and OGG Opus are not.
- Prepare audio before a show. Cache and RAM are bounded. 100 configured slots do not guarantee 100 simultaneous streams on a particular PC.
- Mix summing uses safety clipping, not a musical limiter.
- Real-device latency, drivers, reconnection and long shows require testing with your own equipment. No hardware-support or security-certification badge is claimed.

## Build and test

On Windows, install .NET SDK 10 x64 and run:

```powershell
.\Build.ps1
```

The script validates the repository, runs core and simulated integration tests, publishes the self-contained x64 EXE, runs **native WPF rendering/startup checks**, then creates the ZIP and checksums. It fails before packaging if a required check fails.

```powershell
dotnet run --project StagePad.Tests -c Release
dotnet run --project StagePad.IntegrationTests -c Release
```

Core/simulated tests can run on other SDK-supported platforms; this does not imply desktop support outside Windows. The GitHub release workflow publishes a **stable** release only after the Windows build passes. See [verification](docs/VERIFICA.md), [Windows checklist](docs/TEST-COMPLETI.md) and [publishing guide](docs/GITHUB-SETUP.md).

## Contributing and license

Reports, translations and focused changes are welcome. Read [CONTRIBUTING](CONTRIBUTING.md), [SECURITY](SECURITY.md) and the [changelog](CHANGELOG.md).

StagePad Live uses the [MIT License](LICENSE). Dependency notices are preserved in [licenses](licenses/) and available from Settings > Licenses. Source and executable are freely distributable under their respective license terms.

## Windows SmartScreen notice

StagePad Live is currently distributed without a digital code-signing certificate. Windows may display **“Windows protected your PC”** and **“Unknown publisher”** when launching the downloaded EXE. This warning alone is not a malware detection or a guarantee that a file is safe.

Download releases only from the [official repository](https://github.com/Alessio278/StagePad-Live/releases) and compare the file’s SHA-256 with the published `SHA256.txt`. A matching checksum confirms that the file matches the published asset; it does not replace a security scan. If you trust the source and choose to continue, Windows may offer **More info > Run anyway**. Managed PCs may prevent this option.
