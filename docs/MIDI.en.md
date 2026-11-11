# MIDI with StagePad

In ⚙ Settings > MIDI VOLUMES, assign independent absolute CC controls to master and the currently playing track through channel/CC or LEARN. CC -1 disables a binding. Bindings are saved in the show; track volume follows the newest audio still playing, independently of slot selection.


Since 0.11.0 each slot has independent Audio/MIDI/OSC selectors and a MIDI command list. Note, MSC, Control Change and Program Change are supported. See [combined actions and OSC](OSC.en.md).


[Italiano](MIDI.it.md) · **English** · [Español](MIDI.es.md) · [Français](MIDI.fr.md) · [Home](../README.md)

## Signal path

Connect a MIDI controller to a Windows MIDI input and select it in **MIDI IN**.
Choose the receiving hardware or software port in **Destination · MIDI OUT**.
USB, DIN interfaces and separately installed virtual ports can be used when
exposed as compatible Windows MIDI ports. StagePad does not install a virtual
port or forward every incoming message automatically.

Slots can trigger audio, MIDI, or both. Audio and MIDI output are independent:
a missing audio file does not prevent a configured MIDI message, and a MIDI
output error does not automatically stop audio.

## Input and Learn

- Input accepts Note On/Off; Note On with velocity zero is a release.
- Press **LEARN** on a slot, then press a pad. This maps the channel/note without
  triggering audio or MIDI output on the learning press.
- A channel/note input pair may not be mapped to two slots.
- CC, Program Change, aftertouch, MIDI clock and controller LED feedback are not implemented.
- If a release is lost after disconnecting a controller, Hold may keep playing.
  Stop that slot with the mouse or use PANIC.

## Note output

| Setting | Behavior |
| --- | --- |
| Channel / note / velocity | Channel 1–16, note 0–127, fixed velocity 1–127 |
| Pulse | Note On, then automatic Note Off; default duration 100 ms |
| Gate | Note On on press; Note Off on release, slot Stop or PANIC |

Configure the receiver’s note mapping or MIDI Learn separately. The same note
can mean different things on different receivers. For two gates sharing a note
and channel, the last held gate controls its final release.

## MIDI Show Control (MSC)

This build sends SysEx MSC commands **GO**, **STOP**, and **RESUME**. Configure
Device ID 0–127 and command format **1 General Lighting**, **2 Moving Lights**,
or **127 All** to match the receiver.

The implemented addressing profiles are intentionally limited: **Default**,
**Exec.Page**, and **Exec Page (space)**. The latter two encode the destination
as an executor and page separated by a dot or space. The page is fixed to **1**;
GO uses the entered cue normalized to three decimal places, and addressed
STOP/RESUME use cue **0.000**. The receiver must understand that exact encoding.
These are supported profiles, not a promise of complete MSC interoperability.
Use Note mapping or verify the selected MSC profile in the receiver’s manual.

GO, STOP and RESUME meanings depend on the receiving device. Do not assume they
mean next cue, blackout, or the same operation on every system. Audio pause does
not automatically send MSC STOP; MIDI is triggered according to slot commands.

## Testing and recovery

**Test OUT** sends the selected message without audio. Note output uses an
automatically released pulse; MSC tests send the real configured command.
Show Lock blocks these tests. **Test IN** observes the controller; mapped slots
remain active while observing input.

“Port open” confirms the Windows interface, not receipt or execution. StagePad
has no universal receiver acknowledgment. PANIC releases tracked Notes and
stops audio; it does not send a universal MSC STOP, blackout or external reset.
Queued messages that expire are discarded rather than replayed after a failure.

The MIDI encoding and existing receiver profiles are unchanged by the generic
UI terminology. Test the actual device, firmware and routing before a show.
