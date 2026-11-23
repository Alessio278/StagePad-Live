# Testing StagePad Live

## Automated checks

Run `Build.ps1` on Windows with .NET SDK 10. It validates repository files, runs core and simulated integration tests, publishes the Windows app, then runs native WPF startup/rendering checks. Packaging stops if a required check fails. Logs are written to `artifacts/validation`.

The test projects can also run separately:

```powershell
dotnet run --project StagePad.Tests -c Release
dotnet run --project StagePad.IntegrationTests -c Release
```

Core tests cover mixing, queues, memory, markers, loops and data validation. Integration tests use simulated audio/MIDI devices and UDP loopback. WPF checks cover window construction, resources, editor roundtrips and rendering. Physical drivers and receiving devices need the manual checks below.

## Windows checklist

Record the app and Windows versions, display scaling, audio driver, MIDI controller and OSC receiver. Use a copy of your show when testing updates.

- [ ] Launch, close and reopen; no background processes or unexpected playback.
- [ ] Setup and Live at 100%, 150% and 200% scaling, normal and full-screen sizes, in all four languages.
- [ ] Slot actions aligned; + Marker on the left; long titles scroll and the compact +MARKER label fits.
- [ ] Wheel, Shift-wheel, trackpad and draggable scrollbars work at a usable speed, including nested lists and horizontal scrolling.
- [ ] Empty pads cannot trigger; loading audio enables Audio. Waveforms fill their width and resize correctly.
- [ ] One-shot, Hold, protected and toggle-pause modes; volume changes from mouse/MIDI survive retrigger, save and reopen.
- [ ] Default volume affects new slots only. Applying to all slots is blocked while tracks are playing or paused.
- [ ] New MIDI/OSC commands preload editable defaults; changing defaults leaves existing commands and imported legacy destinations unchanged.
- [ ] MIDI/OSC-only pad feedback; command order, waits, tests, duplicate/reorder controls and STOP cancellation.
- [ ] Emergency: immediate or three-second hold; early release, lost focus, MIDI disconnect and STOP cancel pending holds.
- [ ] Add, edit and delete markers; double-click seek; seven colored flashes outside loops; reduced-motion setting.
- [ ] + Marker slot is exclusive and captures only on selected playing audio in Setup. External marker LEARN remains separate. Neither captures with SHOW LOCK or in Live.
- [ ] Loop uses two existing markers, starts at the first and shows only slow red feedback. Marker commands still run each pass. Add markers while looping; remove a loop before changing its endpoints.
- [ ] New/Open/Save/Save as, dirty-show confirmation, rename, duplicate, delete and import/export with audio.
- [ ] Auto-save on/off, writes during playback, .bak backup, failed-write retry and closing immediately after an edit.
- [ ] MIDI release/reconnect without stuck notes; independent master/track CC, relative encoders and soft takeover.
- [ ] OSC receiver confirms addresses, types and command timing; the app's send counter alone is insufficient.
- [ ] Optional Note On LED feedback stays off until configured. Test device-specific LED codes and use an output separate from MIDI commands.
- [ ] Open schema-1/2 shows and save schema 3. Keep originals for older app versions.
- [ ] Update and rollback while idle; original shows and audio remain available.
- [ ] Run the intended show duration with the actual equipment; check audio continuity, CPU/RAM and shutdown.

## Version 1.2 - playback boundaries and waveform selection

- Choose start-only, end-only and two-Marker cuts on short and streamed tracks. Confirm no excluded Audio is heard.
- Check Play, MIDI retrigger, pause/resume, Hold release, STOP ALL and emergency pause with a cut.
- Seek before and after the selected interval; confirm the cursor stays inside playback boundaries.
- Apply a Loop inside the cut. Reject a Loop outside it, reversed boundaries and intervals under 50 ms.
- Confirm the light gray Loop waveform is visible at different window sizes. The waveform and Marker labels must remain readable.
- Save, close and reopen the Show. Confirm the cut survives, time displays use the cut duration and the original Audio file is unchanged.
- Restore the full track and confirm normal playback. Used boundary Markers must be protected from moving/deletion until then.
- Run the native WPF checks on Windows before publishing. Keep a 1.1 Show backup: schema 4 is not readable by older versions.

## 1.3 audio acceptance on Windows

1. Run Build.ps1 using the .NET 10 SDK. This runs Core, simulated integration, the actual isolated decoder on a generated 44.1 kHz mono MP3 and an over-range float WAV, plus the native WPF checks. The MP3 fixture is one second of a synthesized 440 Hz sine.
2. Use the same output endpoint, Windows per-app level, spatial/enhancement settings and sample format in StagePad and VLC. Disable VLC ReplayGain and effects. Record each application's WASAPI loopback separately. Compare the same steady section with both controls at 100%, then with either StagePad control at 50% (expected amplitude ratio 0.5, about -6.02 dB after the ramp). Record peak and RMS plus the time of any audible defect. Differences between decoders or clipping before the float conversion can survive import.
3. Load a 23 to 25 minute MP3 into one Slot. Check the audio log for pcm-ready and MemorySource (25 minutes requires about 549 MiB). Play to EOF, vary Master and Slot continuously by mouse and MIDI, then repeat with a 128 MiB Show budget: expect DiskSource. Compare against the same region of the original, including at the reported distortion time. Log output clipping, callback recovery, and the Slot starvation count before any seek or restart.
4. Repeat with active Marker loop, trim, seek, two loud overlapping tracks, and device removal/reconnection. For overlapping tracks keep enough headroom or expect audio-clip reports. Record dropouts separately from over-range samples. The 5-second callback watchdog detects a sustained stall, not individual Windows endpoint underruns.
5. Close and reopen. Confirm the previous owner-tagged PCM files disappear, an unrelated file in the cache survives, the original file hash and Show are intact, and played audio, cursor, loop and Slot rows remain readable at the display scale used on stage. Old GUID-only cache folders cannot identify their owner and are retained.

To exercise the production isolated importer outside WPF, run `dotnet run --project tools/audio-tests -c Release` on a machine with sufficient free space and memory (allow 2 GiB temporary disk and 1.5 GiB free RAM). It generates a 25-minute, over-range, mono float WAV at 44.1 kHz and checks frame count, sample peaks, full residency, the disk fallback and budget release. This fixture uses WAV for portable testing. The Windows build separately decodes the MP3 fixture through Media Foundation.

### Candidate validation record

Linux, .NET SDK 10.0.100, Release configuration. Core: 466 checks passed. Integration: 541 checks passed using simulated hardware. The production importer processed 72,000,000 stereo frames in each of two independent imports from the 25-minute WAV. With an 800 MiB budget the complete track was resident; with 128 MiB it used disk read-ahead. The measured peak in the checked block was 1.0 in both runs. Both reservations returned to zero after disposal. The separate 25-minute mixer simulation exercised continuous rendering and repeated volume changes faster than wall-clock time.

The Windows-targeted app and screenshot tool compiled with zero warnings/errors; demo data checks passed in four languages. Media Foundation MP3 decoding, native WPF rendering, physical MIDI, long wall-clock WASAPI playback and VLC level matching remain Windows acceptance items.
