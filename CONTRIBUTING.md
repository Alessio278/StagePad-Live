# Contributing

Bug reports, translations and pull requests are welcome in Italian, English, Spanish or French.

For a bug report, include the app and Windows versions, steps to reproduce, expected behavior and relevant device information. Remove personal paths and unrelated content from logs or screenshots.

For code changes:

- Keep changes focused. Decoding, file/network I/O and blocking operations belong outside the audio callback.
- Run `Build.ps1` on Windows and use the [manual checklist](docs/TESTING.md) for UI or hardware changes.
- Check UI changes at different display scaling levels and in all four languages.
- Translations are in `StagePad.App/Localization/strings.json`; preserve placeholders and protocol values.
- Preserve third-party license notices and use ordinary hyphens in maintained text.

Contributions are distributed under the repository's MIT License. Report vulnerabilities as described in [SECURITY.md](SECURITY.md).

## Documentation screenshots

The optional [screenshot generator](tools/screenshots/README.md) contains the
demo projects and capture scripts. Generated documentation images are in
`docs/screenshots`.
