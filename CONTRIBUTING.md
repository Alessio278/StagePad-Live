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

## Publish a stable Windows release

1. Prepare the changes on a branch. Increase the major.minor Version in `StagePad.App/StagePad.App.csproj` and write `docs/releases/vX.Y.md` for that version. Describe the user-visible changes since the preceding stable release.
2. Merge into `main`. Actions compares the versions before and after the push. A numeric increase starts the release job, which runs the existing Windows checks before publishing. Commits that retain the version run the regular Windows CI.
3. After GitHub publishes the release, Actions dispatches **Windows build and tests** on its tag, including the 25-minute audio check. A failure in these additional checks is reported on that workflow. The release is already public at this point.

Each main README links to the latest stable release under **What's New**. The release notes file supplies that page when the workflow publishes it. Keep past notes in `docs/releases`.

The initial creation of main has no previous version to compare. Use the manual release workflow on main for that case or to publish an already committed version. Pushing a tag alone does not start publication. Pull requests and manual Windows CI runs remain available.
