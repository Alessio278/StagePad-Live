# Contributing to StagePad

Reports and contributions in **Italian, English, Spanish and French** are welcome.

- Describe the StagePad and Windows version, exact steps, expected result and actual result.
- For audio/MIDI problems, include devices, drivers, ports, receiver firmware and relevant settings.
- Keep changes focused. Run `Build.ps1` before submitting a code change on Windows.
- Core and simulated integration tests do not replace testing real devices or the WPF interface.
- Keep decoding, disk I/O, network access and blocking work out of the audio callback.
- Translations live in `StagePad.App/Localization/strings.json`. Preserve placeholders
  such as `{0}` and `{1}`, protocol codes, user-entered text and file paths.
- UI changes should be checked at 100%, 150% and 200% display scaling in all four languages.
- Do not claim support for a MIDI feature or receiver profile that has not been implemented and tested.

Contributions are distributed under the repository’s MIT License. Third-party
code or assets must retain their required license and attribution notices.

## Italiano

Sono benvenuti segnalazioni, traduzioni e contributi. Indica versione, passaggi,
risultato atteso, dispositivi e configurazione. Per tradurre modifica il catalogo
JSON senza alterare i segnaposto. Esegui i test e prova l’interfaccia su Windows.

## Español

Se aceptan informes, traducciones y contribuciones. Indica versión, pasos,
resultado esperado, dispositivos y configuración. Edita el catálogo JSON sin
cambiar los marcadores. Ejecuta los tests y prueba la interfaz en Windows.

## Français

Rapports, traductions et contributions sont bienvenus. Précisez version, étapes,
résultat attendu, périphériques et configuration. Modifiez le catalogue JSON sans
changer les paramètres de format. Exécutez les tests et vérifiez l’interface Windows.
