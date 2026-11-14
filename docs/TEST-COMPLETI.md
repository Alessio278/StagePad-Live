# StagePad Live - Windows acceptance checklist

Record Windows version, app version, audio driver/device, MIDI devices, OSC receiver and DPI. Use copies of shows for update testing.

- [ ] Launch/close/relaunch; no orphaned audio/UI processes or unexpected playback.
- [ ] Empty selected slot: no white cursor in the player. Loaded waveform reaches both timeline edges and redraws after resize.
- [ ] With horizontal and vertical scrollbars visible, their shared corner stays dark. Drag both thumbs; use mouse wheel, Shift-wheel and two-finger horizontal trackpad scrolling.
- [ ] Setup/Live at 100%, 150%, 200% DPI, narrow window and full screen; all four languages; long labels and marker labels remain usable.
- [ ] Add/edit/delete markers; double-click seek; seven visible fast flashes over 1.82 seconds; reduced-motion behavior.
- [ ] Apply/remove a loop between existing markers without a checkbox; Play starts at loop start; only slow red loop feedback on initial Play, interior markers and wraps; no colored cues until the loop is removed; pause/resume/stop, track-end boundary, low-RAM rejection, Show Lock and reload.
- [ ] Position field follows cursor movements while the editor is open; typing with keyboard focus is not overwritten. + Marker adds immediately and refreshes the list with a loop applied.
- [ ] During play + Marker does not open a window; loop wraps show only slow red; no red leaks between colored pulses. Rounded checkbox appearance, mouse and keyboard toggling, disabled states and focus.
- [ ] Marker MIDI/OSC once per crossing and loop pass; no skipped-cue replay on seek; automatic MIDI Note Off.
- [ ] Marker capture LEARN: learning press consumed, repeated hold suppressed, only selected playing audio in Setup; no capture in Live, paused, loading or locked.
- [ ] New/Open/Save/Save as, overwrite/dirty confirmations, rename, duplicate, confirmed delete, import/export with media, scrollable project list.
- [ ] Auto-save for named/unnamed shows, during audio, disable option, .bak, error/retry and closing immediately after an edit.
- [ ] Default project path is `%LOCALAPPDATA%\StagePad\project`; legacy migration preserves files and referenced media.
- [ ] One-shot, Hold, protected, toggle-pause, emergency pause and STOP ALL with real MIDI. Disconnect/reconnect without stuck notes or replay.
- [ ] Master/track CC, soft takeover and relative encoders; track selection does not redirect the volume binding.
- [ ] Real OSC receiver confirms address/types; sender count alone is insufficient.
- [ ] Prepare all media and run the intended show length; inspect underruns, CPU/RAM, audio continuity and shutdown.
- [ ] Update and available rollback while idle; source/version/checksum verified; original shows and media remain available.

Do not mark an item passed without actually checking it. Unchecked items are not implied by the release's stable flag.
