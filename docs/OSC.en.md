# OSC / MIDI / Audio - StagePad 0.12.0

Open a slot’s settings and select **Audio**, **MIDI** and/or **OSC** under **Actions on press**. Any of the seven non-empty combinations is supported. A mapped MIDI pad or an on-screen pad executes the same selection.

Add up to **16 MIDI commands** and **16 OSC commands** per slot. MIDI supports Note (Pulse/Gate), MSC, Control Change and Program Change. All MIDI commands use the output selected in Setup. Program numbers and CC values use the displayed 0-127 range.

For each OSC command enter a host/IP, UDP port and OSC address, then add typed arguments: integer, float, ASCII string, true or false. Example: host `127.0.0.1`, port `8000`, address `/show/go`, integer `1`. Each command can target a different receiver. See [OSC guide](OSC.en.md).

Audio, MIDI and OSC are dispatched independently. Missing audio does not suppress MIDI/OSC; blocked MIDI does not block OSC or audio. Commands in each output list are sent in list order; timing between audio, MIDI and UDP depends on their receivers. Releasing the pad closes all configured MIDI Gate notes. STOP ALL cancels queued OSC commands and releases MIDI notes. UDP sends are counted in Settings diagnostics; the count confirms local transmission. Test the receiving software to confirm its response.

The selected outputs, command lists, destinations and arguments are saved with the project and recovery. Existing projects retain their original single-MIDI behavior. Slots accept at most 16 arguments per OSC message and a combined command data limit of 6000 bytes.

OSC format reference: https://opensoundcontrol.stanford.edu/spec-1_0.html

UDP sends each command once. Messages older than one second in queue/DNS are dropped and never replayed after reconnection. STOP ALL cannot recall an already transmitted message. The receiver must accept the configured argument types, especially T/F.
