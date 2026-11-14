# Contributing to StagePad Live

Reports and contributions in Italian, English, Spanish and French are welcome.

- Include app/Windows versions, reproduction steps, expected/actual behavior, screen scaling and relevant device/driver/receiver information.
- Remove personal file paths and unrelated content from logs/screenshots.
- Keep code changes focused and decoding, disk/network I/O and blocking work out of the audio callback.
- Run `Build.ps1` on Windows before submitting. It includes core, simulated integration and native WPF checks.
- Test UI changes at 100%, 150% and 200% DPI in all four languages.
- Translations are in `StagePad.App/Localization/strings.json`. Preserve placeholders, protocol fields and user-entered text.
- Use the ASCII hyphen `-` in maintained text. Do not add long dash punctuation.
- Do not claim device profiles, supported platforms, certifications or test outcomes without evidence.

Contributions use the repository's MIT License. Preserve third-party notices. See [Windows checklist](docs/TEST-COMPLETI.md) and [verification](docs/VERIFICA.md).
