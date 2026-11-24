# Audio path and engine decision for 1.3

Media Foundation decodes MP3, NAudio converts to stereo and resamples to 48 kHz. Import measures the resulting float samples before building waveform peaks. If the maximum absolute sample exceeds 1, the offline pass divides every sample by that peak. This preserves dynamics; it cannot repair a clipped recording or a peak already truncated by a decoder. It measures sample peaks, not intersample true peaks.

At steady state one voice multiplies PCM by Slot gain and then Master gain. Both range from 0 to 1. Fades, seek ramps and loop edges can reduce amplitude. The final sample clamp can distort an overloaded mix. The 1.2 code had no indication that it had clamped. No fixed boost was identified to explain a single track sounding louder than VLC. A measured comparison needs matching Windows sessions, player processing, and source material.

The import worker prepares resident PCM or the head and initial read-ahead pages before installation. A short source read during playback leaves silence for the missing part of the block and advances only by available frames. Starvations count deficient reads and are not hardware underrun measurements. WASAPI uses shared mode; the endpoint and Windows mixer remain part of the actual result.

| Engine | MP3, gain and buffering | Integration with live cues and distribution |
| --- | --- | --- |
| NAudio / WASAPI | Offline Media Foundation decode, StagePad float mixer and explicit buffers. Shared endpoint latency is configured in the Show. | Existing sample-frame marker, trim, loop and multi-slot behavior can be exercised directly. Keep for 1.3; existing dependency notices apply. |
| LibVLC | Decoder and player supply their own buffering and volume. PCM callbacks would need an adapter to the existing mixer. | Multiple independent players would need timing coordination. Native plugins and LGPL distribution obligations increase packaging work. Benchmark seek latency before a migration. |
| BASS | Native streams, decode channels, volume and buffers could support a shared mixer. | Port scheduling and validate precise seeks, loop boundaries, multiple channels and restart behavior. Commercial uses may require a separate licence. |
| miniaudio | Native MP3 decode, resource management and mixing could replace multiple layers. | Requires C interop and rebuilding timing, recovery and callback ownership. Permissive distribution. Measure end-to-end latency and long playback before considering adoption. |

No comparative hardware benchmark was performed. None of the alternatives alone establishes the cause of the reported incident. See [LibVLC](https://www.videolan.org/vlc/libvlc.html), [BASS](https://www.un4seen.com/doc/bass/bass.html) and [miniaudio](https://miniaud.io/).

The maximum reported WASAPI callback gap is per output opening. Delays above the larger of 100 ms and three configured latency intervals are logged from the device worker. This is an observation of callback timing. Endpoint underruns require separate measurements on Windows.
