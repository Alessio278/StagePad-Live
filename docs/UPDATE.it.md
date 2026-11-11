# Aggiornare StagePad senza reinstallare

1. Chiudi la vecchia versione di StagePad e attendi qualche secondo.
2. Estrai il nuovo pacchetto Windows e copia `StagePad.exe` nella cartella in cui tieni l’app, sostituendo il vecchio EXE.
3. Avvia il nuovo `StagePad.exe`.

Non serve un installer né reinstallare dipendenze: l’EXE include il runtime necessario.

Progetti, impostazioni, mappature e ultimo show restano salvati in `%LOCALAPPDATA%\StagePad` e nell’eventuale cartella progetti personalizzata. Non cancellare queste cartelle durante l’aggiornamento. I file audio originali restano nei loro percorsi.

Se la vecchia versione è rimasta invisibile in background e impedisce la sostituzione, puoi avviare il nuovo EXE da un’altra cartella: la 0.12.0 tenta di recuperare l’istanza bloccata dello stesso utente/sessione. Un’istanza ancora visibile viene invece riportata in primo piano: chiudila prima di aggiornare.

Il pacchetto GitHub contiene i sorgenti per chi vuole compilare il programma; per usarlo o aggiornarlo basta il pacchetto Windows.

Nel browser progetti **Esporta show con audio** crea un pacchetto `.stagepad.zip` con lo show e i suoi file audio originali. **Importa show** accetta il percorso completo del pacchetto o un pacchetto nella cartella progetti e crea uno show con nome separato senza sovrascrivere. Ferma audio e sequenze prima dell’operazione. Limite dei media originali: 8 GiB.
