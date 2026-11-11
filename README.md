<img src="StagePad.App/Assets/StagePad.png" width="72" alt="StagePad logo">

# StagePad

**An open-source audio launcher and MIDI/OSC trigger for Windows.**

[Italiano](docs/README.it.md) · **English** · [Español](docs/README.es.md) · [Français](docs/README.fr.md)

Windows x64 · MIT · 1-100 slots · Audio + MIDI + OSC · IT / EN / ES / FR

StagePad puts audio tracks, MIDI and OSC messages on a grid of playable slots. Prepare
materials in **Setup**, switch to **Live** for performance, and trigger slots with
a mouse or a MIDI controller. Use it for live music, theatre, rehearsals, sound
installations and compatible MIDI workflows.

Created by **Alessio Natalucci**.

[Download for Windows](https://github.com/Alessio278/StagePad-Live/releases) · [MIDI guide](docs/MIDI.en.md) · [Changelog](CHANGELOG.md) · [Report an issue](https://github.com/Alessio278/StagePad-Live/issues)

The slot settings window opens next to its slot: on the right when it fits, otherwise on the left. Its requested opening size is **650×950 screen pixels**, reduced only if the monitor work area is smaller. Labels wrap and the form scrolls on smaller/high-DPI layouts.


The main window opens centered at **1250×960 screen pixels**, fitting smaller work areas. **Show: project name** appears before Audio ready. The project list stays scrollable in a normal window. Select a project to **Rename**, **Duplicate** or **Delete** it. Names are entered within the app; deletion requires confirmation and leaves audio files on disk. Rename and Duplicate never overwrite another project. SHOW LOCK protects these actions. Deleting the open project keeps the current audio setup in memory; use Save to keep it again.


At startup StagePad automatically restores the last show and its recent recovered edits, including its saved-file association. The **Show** badge stays mint green. The project list keeps its dark background and selection during inline delete confirmation. If the original show file is missing, the recovered setup remains available with a warning.

## Automatic names, MIDI volumes and updates

Leave **Name** empty in slot settings to follow the audio filename without its extension. Loading another track updates the title. Enter a custom title to keep it fixed; clear it to restore automatic naming.

In **⚙ Settings → MIDI VOLUMES**, independently assign channel/CC to **master** and **currently playing track** volume. Use each **LEARN** button and move its fader/knob, or enter values and choose **Apply MIDI assignments**. Absolute CC 0-127 and relative encoders are supported; `-1` disables a binding. The same CC number on separate channels is allowed. Assignments are saved in the show.

The controlled track is the most recently started audio still playing. MIDI/OSC-only triggers and slot selection do not redirect the audio fader. If the newest audio stops or pauses, the next most recent still-playing audio is controlled; with no playing audio the track fader changes no slot.

Closing signals the supervisor directly and bounds module shutdown time. Relaunch activates a visible instance; an invisible stuck instance can be recovered, restricted to this product and the same Windows user/session.

**Portable updates:** close StagePad, replace only `StagePad.exe`, then reopen it. Existing project, settings, mapping and recovery directories are preserved. See [update instructions](docs/UPDATE.en.md).

## Combined Audio, MIDI and OSC actions

Open a slot’s settings and select **Audio**, **MIDI** and/or **OSC** under **Actions on press**. Any of the seven non-empty combinations is supported. A mapped MIDI pad or an on-screen pad executes the same selection.

Add up to **16 MIDI commands** and **16 OSC commands** per slot. MIDI supports Note (Pulse/Gate), MSC, Control Change and Program Change. All MIDI commands use the output selected in Setup. Program numbers and CC values use the displayed 0-127 range.

For each OSC command enter a host/IP, UDP port and OSC address, then add typed arguments: integer, float, ASCII string, true or false. Example: host `127.0.0.1`, port `8000`, address `/show/go`, integer `1`. Each command can target a different receiver. See [OSC guide](docs/OSC.en.md).

Audio, MIDI and OSC are dispatched independently. Missing audio does not suppress MIDI/OSC; blocked MIDI does not block OSC or audio. Commands in each output list are sent in list order; timing between audio, MIDI and UDP depends on their receivers. Releasing the pad closes all configured MIDI Gate notes. STOP ALL cancels queued OSC commands and releases MIDI notes. UDP sends are counted in Settings diagnostics; the count confirms local transmission. Test the receiving software to confirm its response.

The selected outputs, command lists, destinations and arguments are saved with the project and recovery. Existing projects retain their original single-MIDI behavior. Slots accept at most 16 arguments per OSC message and a combined command data limit of 6000 bytes.

## Projects inside the app

**Open / Save** now opens an in-app project view instead of a native file dialog.
Select a project to open it, or enter a project name and save. Overwriting an
existing file asks for confirmation in the same view. Refresh lists saved JSON
projects; automatic recovery files are kept separate from the manual list.

In **⚙ Settings → Projects**, enter a full folder path and select **Apply folder**.
**Default** restores `%LOCALAPPDATA%\StagePad\project` for the current Windows
account. Changing the folder does not move existing files. Automatic recovery
now uses `project\last-project.json`; older `recovery` files remain readable and
are retained. Keep project files and their referenced audio together when moving them.

In **⚙ Settings → Live**, **Automatically scroll to the triggered pad** can be
turned ON/OFF. It is ON by default and remembered on this PC. In Live view,
a newly triggered pad outside the viewport is brought into view, horizontally
and/or vertically, with only the required movement. Fully visible pads do not
move the grid. It works with MIDI triggers and does not take keyboard focus or
send extra audio/MIDI commands. It does not affect Setup or continually follow playback.

The app logo is the large white **S on a mint-green rounded square**. The green LIVE
badge sits directly beside STAGEPAD; the same logo is used for Windows icons.

## What it does

| Feature | Details |
| --- | --- |
| Playable slots | 1-100 active slots; one audio voice per slot |
| Two workspaces | Separate Setup and Live grids, up to 10×10; scrollable, readable cells |
| Audio | Play, pause, stop, gain, configurable fades and a master control |
| Playback modes | One shot / retrigger, Hold, protected playback, play-pause-resume |
| MIDI input | Note On/Off, MIDI Learn, independent input routing |
| MIDI output | Note pulses or gates, plus the implemented MSC GO/STOP/RESUME formats |
| Track view | Waveform, double-click seek, elapsed/total/remaining time |
| Live controls | Show Lock, Stop All, full screen, scrolling long titles |
| Appearance | Per-slot colors, soft backgrounds and a color wheel inside slot settings |
| Languages | Italian, English, Spanish and French, selected with the gear icon |
| Projects | JSON save/recovery with audio file paths; no embedded audio |

No account, activation or online license check is required. The application UI
can be used offline after downloading the executable. Building from source needs
Internet access the first time to restore dependencies.

**Default** beside the slot color selector restores only the default color, with an immediate preview. Save it with **Apply to slot**.

## Download and start

1. Open **Releases** and download `StagePad.exe` from the release assets.
2. Close an older copy, then start the executable. The distributed Windows x64
   build is self-contained: end users do not need to install .NET.
3. In **Setup**, choose an audio output, start with a **40 ms** buffer and press
   **Apply**. Use **Audio test · −24 dB** to check the selected destination.
4. Choose the number of slots and adjust **Rows / Columns**. Load tracks and wait
   until they are ready.
5. Open **Configure** on a slot to set its name, color, action, playback mode,
   fades, controller mapping and output MIDI message.
6. Choose MIDI IN / OUT devices as needed. Press **LEARN**, then press the pad
   to map it; that learning press does not trigger the slot.
7. Save the project and switch to **Live**. Use **F11** for full screen and **Esc**
   to leave it. Enable **Show Lock** to protect configuration during performance.

The initial UI language is Italian. Open **⚙ → Lingua dell’interfaccia** to
choose English, Español or Français. Changes apply immediately, including open
slot editors, and are remembered on this PC without restarting audio. **Color
controls remain in individual slot settings**, not in global Settings. Audio
memory budget and live diagnostics are available in Settings, together with
Open logs and Licenses. The memory budget remains saved in the project.

**Reset** in a slot editor restores the editable defaults; use **Apply to slot**
to save. It preserves the loaded audio and slot volume. Reducing the active slot
count hides the tail without discarding its saved settings or audio paths.

## Audio and MIDI compatibility

WAV PCM/float and OGG Vorbis use included decoders. MP3, FLAC, M4A/AAC and other
Windows formats depend on installed Media Foundation codecs. Mono and stereo
files are supported; OGG Opus, DRM and multichannel files are not supported.

MIDI is not tied to one device brand. Use a controller that sends Note On/Off
and an output destination that understands the messages you configure. Other
software may require a separately installed virtual MIDI port. CC, Program
Change, MIDI clock and automatic MIDI thru are **not implemented**. The MSC
addressing modes have specific limits; see the [MIDI guide](docs/MIDI.en.md).

## Emergency slots and sequence controls

Enable **EMERGENCY: pause all tracks** in a slot’s settings. Pressing its pad pauses every playing/fading audio track, including protected tracks, preserving positions. The slot does not play its own audio. Select MIDI and/or OSC to send its emergency messages. Pending output sequences are cancelled and active MIDI notes are released before the slot’s own messages. The setting is saved with the show and works with mouse or mapped MIDI input. Use Play to resume an individual paused track; normal pad modes keep their existing retrigger behavior. STOP ALL still stops and resets positions. Emergency controls pause software audio; they do not guarantee receipt by external equipment.

Use ↑/↓ to reorder MIDI/OSC commands, **Wait before (ms)** to insert delays and **Test now** to transmit one command while playback is stopped. Tests skip the configured delay; MIDI note tests use a timed Pulse. Each output list runs separately. Maximum delay: 30 seconds per command and 60 seconds per list. Releasing a pad cancels future Gate notes; STOP cancels its future commands. Keep emergency delays at zero for immediate transmission.

In Settings, master/track volume mappings support absolute CC, three relative encoder encodings and **soft takeover**, which waits for an absolute fader to reach/cross the current volume. Relative step is configurable. Input loss releases held pads; reconnect never replays triggers.

In the project browser, **Export show with audio** creates a `.stagepad.zip` package containing a show and its original audio files. **Import show** accepts a full package path or a package in the project folder, and creates a separately named show without overwriting. Stop audio/sequences before importing/exporting. Packages support up to 8 GiB of original media.

**Settings → Updates** checks stable releases from `Alessio278/StagePad-Live`. Download/install is allowed only while idle and preserves user data. The download is checked against the release SHA-256, a backup is retained and a failed startup triggers rollback. The previous version can also be restored from Settings. The repository and published release assets must exist for online updates; this source package does not publish them automatically. Manual EXE replacement remains supported.


The MIDI volume options are **Prevent master volume jumps** and **Prevent track volume jumps**. With absolute faders, control begins when the fader reaches the current volume. Version 0.13.1 adds readable white wrapping labels and inline help.

## Beta status and known limits

This is a beta, not a hardware-certified show system. Automated tests cover the
engine and simulated integration. WPF interaction, WASAPI drivers, real MIDI
devices, reconnection and long-duration shows require testing on your Windows PC.
Configuring 100 slots does not guarantee a given PC can stream 100 tracks at once.

- The build is unsigned. Windows may show publisher/reputation prompts.
- Audio preparation uses a RAM budget and disk cache. Prepare files before a show.
- Mix summing uses safety clipping, not a musical limiter.
- PANIC stops audio and releases tracked MIDI notes; it is not a universal command
  to stop or reset external equipment.
- Project files reference audio paths. Keep projects and audio together when moving them.
- Native Windows file dialogs and operating-system error details follow Windows language settings.

## Build from source

Install the **.NET SDK 10 x64** version selected by `global.json`, then run:

```powershell
.\Build.ps1
```

Or double-click `Compila.cmd`. The script runs core and simulated integration
tests, then publishes `artifacts\win-x64\StagePad.exe`.

```powershell
dotnet run --project StagePad.Tests -c Release
dotnet run --project StagePad.IntegrationTests -c Release
```

The optional `--stress` core test is diagnostic, not a real-device endurance test.

## Contribute and license

Bug reports, device compatibility reports, translations and code contributions
are welcome. See [CONTRIBUTING](CONTRIBUTING.md), [verification notes](docs/VERIFICA.md)
and the [Windows checklist](docs/TEST-COMPLETI.md).

StagePad is distributed under the [MIT License](LICENSE). Third-party notices
are in [licenses](licenses/) and available from the app’s **Licenses** button.
