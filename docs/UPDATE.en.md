# StagePad Live - portable updates

Close the application before replacing `StagePad.exe` with the executable from a newer stable release. Settings, named shows, recovery and media references stay in `%LOCALAPPDATA%\StagePad` or your chosen project folder. The Windows ZIP includes its runtime.

Settings > Updates checks `Alessio278/StagePad-Live` stable releases. The updater expects a `StagePad.exe` asset and a matching SHA-256 digest or `SHA256.txt`. Installation requires idle audio/sequences, validates product/version and retains a previous executable. Failed startup attempts rollback; Settings can restore an available previous version.

Back up shows and original audio before a major-version change. Show export includes media; copying only JSON does not. Existing development data is migrated without overwriting named shows; archived media remains in `backups\legacy-data` when referenced.

This build is unsigned. SmartScreen reputation is separate from the checksum and update integrity checks. Automatic update checks require network access; ordinary playback does not.

[README](../README.md)
