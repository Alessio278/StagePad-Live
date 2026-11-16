# Portable updates

Close StagePad Live before replacing its executable. The Windows ZIP includes the runtime; shows, settings and media remain in their existing folders.

**Settings > Updates** checks stable releases from `Alessio278/StagePad-Live`. It verifies the versioned executable and SHA-256, keeps a previous copy and attempts rollback if startup fails. Audio and command sequences must be stopped before installation. The update keeps the installed filename.

Back up shows and original audio before upgrading. Version 1.1 saves schema 3, which version 1.0 cannot read. Export a show with audio to copy all referenced media; copying JSON alone does not include it.

The executable is unsigned and may trigger SmartScreen. See the [README](../README.md#windows-smartscreen). Ordinary playback works offline; checking or downloading updates needs internet access.
