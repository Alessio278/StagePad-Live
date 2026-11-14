# Pubblicare StagePad Live 1.0.0

## Contenuto del repository

Estrai lo ZIP sorgenti nella radice del repository, mantenendo anche `.github` e `.gitignore`. Non caricare EXE, ZIP, `bin`, `obj`, cache o file personali nel codice: i binari appartengono alle Releases.

README principale in inglese; guide IT/ES/FR in `docs`. Logo originale, indice e badge verificabili sono già inclusi. La licenza è MIT; non aggiungere badge npm/OpenSSF o piattaforme non supportate.

Descrizione suggerita (meno di 350 caratteri):

> StagePad Live is an open-source Windows audio launcher and MIDI/OSC cue controller for live shows. Configure up to 100 pads, audio markers and loops, automate MIDI/OSC actions, and manage shows with auto-save. Available in Italian, English, Spanish and French.

Topics: `windows`, `audio`, `midi`, `osc`, `soundboard`, `live-performance`, `wpf`, `dotnet`.

## Percorso semplice sul sito GitHub

1. Carica/committa il contenuto sorgenti su `main`. Conserva i percorsi `.github/workflows/ci.yml`, `.github/workflows/release.yml` e `.github/ISSUE_TEMPLATE/bug_report.yml`.
2. Attendi il workflow **Windows build and tests**. Se fallisce, apri il primo errore e correggilo prima di pubblicare.
3. In **Actions > Publish stable Windows release > Run workflow**, seleziona `main` ed esegui.
4. Il workflow ricompila e controlla Windows/WPF, prepara gli asset e pubblica `v1.0.0` con `draft=false` e `prerelease=false`, come ultima release. Qualsiasi errore richiesto blocca la pubblicazione.
5. Verifica Releases, badge, download e checksum. Completa la checklist hardware sul PC destinato allo show.

In alternativa, il push del tag `v1.0.0` sul commit corretto avvia lo stesso workflow. Il tag deve coincidere con la versione nel csproj. Una release già pubblicata non viene sovrascritta.

## Pubblicazione manuale degli asset già preparati

Dopo il controllo Windows puoi creare una release `v1.0.0`, titolo **StagePad Live 1.0.0**, allegare EXE, ZIP Windows, ZIP sorgenti, SHA256 e RELEASE-CHECKS. Usa le note in `docs/releases/v1.0.0.md`. Lascia disattivato **Set as a pre-release** e imposta **Latest release**.

Il collegamento GitHub e i permessi di scrittura sono necessari solo per caricare/pubblicare dal tuo account. Preparare il pacchetto non modifica il repository remoto.
