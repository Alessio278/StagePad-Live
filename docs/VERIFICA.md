# StagePad Live 1.0.0 - verification

## Automated coverage

Core/presentation tests cover mixing, bounded queues, allocation-free render blocks, pause/stop/emergency, seek behavior, marker validation and feedback, looping, marker triggers, MIDI controls, project validation and UI policies.

Simulated integration tests cover serialized commands, fake MIDI/audio devices, UDP loopback, project operations, marker/loop persistence, Setup-only capture, auto-save while playing, failed-write retry, bounded exit and legacy-data migration without replacing named shows.

`Build.ps1` additionally runs the published EXE's `--check-ui-assets` mode on **Windows**. Native checks rasterize the logo, verify editor roundtrips, render the empty player and scrollbar corner, test both scroll offsets, resize the waveform, construct project/marker/update windows and verify close signals. Success produces `artifacts/validation/windows-ui.json`.

The release workflow must pass all these checks before publishing. A locally cross-compiled EXE alone does not establish a native Windows pass. Test logs and generated validation records describe the environment actually used; this document does not predeclare a CI result.

## Practical limits

Simulated tests and WPF smoke checks do not certify physical drivers, real MIDI/OSC receivers, audible transitions or an entire long show. Complete the [Windows checklist](TEST-COMPLETI.md) with the intended equipment. The package is unsigned.

Badges are factual: Windows x64, .NET 10, four interface languages, MIT, release metadata and the live CI status. No npm package, OpenSSF Gold certification, macOS/Linux desktop build or editor-plugin support is claimed.
