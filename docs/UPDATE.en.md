# Update StagePad without reinstalling

1. Close the old StagePad version and wait a few seconds.
2. Extract the new Windows package and replace the old `StagePad.exe` in your app folder with the new one.
3. Start the new `StagePad.exe`.

No installer or dependency reinstall is needed: the executable contains its runtime.

Projects, settings, mappings and the last show remain in `%LOCALAPPDATA%\StagePad` and your configured project folder. Keep these folders during updates. Original audio files stay in their existing locations.

If an invisible old instance prevents replacing the file, launch the new EXE from another folder. Version 0.12.0 attempts recovery of stuck instances from the same user/session. A visible instance is brought to the foreground instead; close it before updating.

The GitHub package contains source code. To run or update the app, use the Windows package.

In the project browser, **Export show with audio** creates a `.stagepad.zip` package containing a show and its original audio files. **Import show** accepts a full package path or a package in the project folder, and creates a separately named show without overwriting. Stop audio/sequences before importing/exporting. Packages support up to 8 GiB of original media.
