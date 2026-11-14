# StagePad Live - MIDI

Use a controller sending Note On/Off. Select its MIDI IN port in Setup. Slot LEARN consumes the learning press. A key cannot be assigned to multiple slots or collide with marker-capture LEARN.

## Output messages

- Note Pulse: automatic Note Off after the configured pulse duration.
- Note Gate: Note Off on pad release; input loss releases held notes.
- Control Change: channel 1-16, controller/value 0-127.
- Program Change: channel 1-16, program 0-127 (zero-based).
- MSC: implemented GO, STOP and RESUME SysEx; configure device ID, command format, cue and addressing to match the receiver.

Up to 16 messages per slot. All use the selected MIDI OUT. Reorder with arrows; delays are up to 30 seconds per message and 60 seconds per list. Test now sends one immediate message while idle. A receiver may need a separately installed virtual MIDI port.

## Volume and marker capture

Settings supports independent master and current-playing-track CC bindings: absolute faders, relative encoders and soft takeover. The track fader controls the most recently started audio still playing; selection alone does not redirect it. The same controller/channel pair cannot control both volumes.

Marker-capture LEARN assigns an unused Note On/Off key. It creates markers only in Setup on the selected playing track. Marker output triggers can use immediate MIDI Pulse/CC/Program/MSC. They never use Gate and do not replay skipped cues after a seek.

MIDI clock and automatic MIDI thru are not implemented. Hardware names are not predefined profiles; verify actual messages and receiver behavior. Reconnect does not replay old cues.

[README](../README.md)
