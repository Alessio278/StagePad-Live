# StagePad roadmap

Implemented through 0.13.0: audio/MIDI/OSC slot combinations, emergency pause,
relative encoders and pickup, ordered delayed commands with individual tests,
portable show/audio packages and release-based updates with rollback.

Priority: stability. Before a public stable release, run the Windows checklist
in TEST-COMPLETI.md, test real device hot-plug and long-duration audio operation.
Linux automated tests cover core logic, simulated MIDI, subprocess cleanup,
archive handling, updater transactions and real loopback UDP. They do not
certify real Windows drivers, WPF/DPI or update handoff between Windows EXEs.

Future candidates: device compatibility reports, signed releases, MIDI clock,
LED feedback, audio redundancy and sample-accurate external synchronization.
