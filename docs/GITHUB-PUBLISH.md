# Pubblicazione di Alessio278/StagePad-Live

Il pacchetto 0.13.2 allinea aggiornamenti e collegamenti al repository
`Alessio278/StagePad-Live`. Le build precedenti usano il vecchio indirizzo:
installare manualmente l’EXE 0.13.2 per passare al nuovo canale.

## Caricamento iniziale dal browser

1. Estrarre StagePad-0.13.2-GitHub.zip.
2. Nel repository vuoto scegliere uploading an existing file. Caricare i file
   e le cartelle contenuti nello ZIP alla radice, senza la cartella contenitore.
3. Sono 112 file: per il limite di 100 file a invio, caricare prima tutto
   tranne docs (88 file), confermare; poi Add file > Upload files e caricare
   la cartella docs (24 file). Mantenere le cartelle e i relativi percorsi.
4. Alla radice devono comparire README.md, LICENSE, .gitignore, .github,
   StagePad.App, StagePad.Core, progetti di test, docs, tools e licenses.
5. Non caricare EXE, audio personali, cache o cartelle bin/obj nel codice.
6. Attendere la spunta verde del workflow Windows in Actions. Il workflow
   effettua test simulati, publish e controllo WPF sul binario pubblicato.

## Prima release

1. Provare EXE e funzioni audio/MIDI/OSC e chiusura/riapertura su Windows reale.
2. Releases > Create a new release (o Draft a new release).
3. Tag v0.13.2; Target main; titolo StagePad Live 0.13.2 - Beta.
4. Allegare StagePad.exe con questo nome esatto, StagePad-0.13.2-Windows.zip
   e SHA256.txt. EXE e SHA256.txt sono anche nello ZIP Windows.
5. Salvare prima la bozza e controllare gli allegati. Pubblicare inizialmente
   con This is a pre-release selezionato, finché la validazione reale è aperta.
6. Le release beta sono pubbliche e scaricabili, ma il controllo aggiornamenti
   dell’app vede solo release stabili. Dopo le prove, modificare la release,
   togliere pre-release e impostarla come Latest quando appropriato.

## Aggiornamenti successivi

L’app interroga
https://api.github.com/repos/Alessio278/StagePad-Live/releases/latest
ed accetta soltanto asset del percorso di rilascio di questo repository.
Servono StagePad.exe e la sua riga in SHA256.txt, oppure il digest SHA-256
fornito da GitHub. La versione deve superare quella installata. La 0.13.2
installata non propone nuovamente sé stessa.

Al push di un tag v* il workflow release.yml compila/testa e crea una BOZZA,
se per quel tag non esiste già una release. Non sostituisce allegati scelti
manualmente. Solo la pubblicazione manuale rende visibile la release al pubblico;
solo una release stabile entra nel canale di aggiornamento dell’app.

Nessun repository o release è stato caricato da questa sessione.
L’app è MIT, senza account, attivazione o verifiche licenza.
