# StagePad Live - OSC

Select OSC in slot settings, add a command and enter host/IP, UDP port 1-65535 and a path beginning with `/`. Example: `127.0.0.1`, port `8000`, `/show/go`, integer argument `1`.

Arguments: int32, float32, ASCII string, true or false. Up to 16 arguments per message and 16 commands per slot; command data is bounded. Each command can have its own receiver. Configure matching addresses and types in the receiving application.

Use arrows for order, delays for timing and Test now for one immediate command while stopped. Maximum delay: 30 seconds per command and 60 per list. MIDI and OSC lists run independently; cross-output timing depends on receivers.

A marker can send one immediate OSC command each playback crossing, including loops. Seeking skips output cues. STOP/STOP ALL cancel pending sequences; reconnect does not replay old cues.

OSC uses UDP: the sent counter confirms local transmission, not receipt. Configure firewalls/network interfaces yourself and test the actual receiver. No discovery, acknowledgement or remote equipment safety guarantee is provided.

[README](../README.md)
