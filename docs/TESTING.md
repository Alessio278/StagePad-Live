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
