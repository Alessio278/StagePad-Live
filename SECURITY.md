# Security policy

The current release line is **StagePad Live 1.x**. Fixes are distributed through this repository's stable releases.

For a potential vulnerability, use GitHub's private **Report a vulnerability** option when available. Do not post secrets, private shows, personal paths or device credentials in public issues. For ordinary bugs use the issue template.

Only download updates from the official repository. The app checks update origin, SHA-256, executable product/version and startup health. These checks do not constitute code signing or hardware certification.

OSC sends to user-configured UDP destinations. Audio decoding is isolated from the audio callback. Neither feature makes untrusted media or external equipment inherently safe. Test your performance setup before public use.
