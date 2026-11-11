# Checklist aggiuntiva 0.13.0 su Windows

- EMERGENZA con più tracce, Hold, Protected e fade: pausa immediata, cursori conservati, ripresa manuale senza partenza spontanea; MIDI/OSC ricevuti dal software destinatario.
- Ritardi, ordine, Test ora, STOP e PANIC con uscita MIDI bloccata e host OSC irraggiungibile.
- Rimozione USB mentre si tiene un pad Hold/Gate: audio e note rilasciati; riconnessione senza replay.
- Soft takeover, tutti i formati relativi, cambio traccia, volumi GUI e SHOW LOCK.
- Export/import show con audio reali, nomi duplicati, spazio insufficiente, pacchetti corrotti.
- Aggiornamento da release di prova: checksum errato, cartella EXE non scrivibile, chiusura durante download, nuovo avvio fallito, ritorno alla precedente. Controllare che non restino istanze nascoste.
- Build.ps1 deve eseguire --check-ui-assets sulla build Windows. Provare layout e controlli con DPI 100/125/150/200% e finestre normali.
- Sessione lunga con carico massimo e dispositivi reali: monitorare underrun, memoria, log e chiusura/riapertura.

# Checklist Windows - beta integrata 0.7.2

Compila i risultati sul PC, con controller e dispositivo ricevente effettivamente usati.
Questi punti non sono dichiarati già superati su hardware. Mantieni una copia
separata del progetto JSON prima del bug hunting.

## Fase 1 - audio

- [ ] Avvio con doppio clic su Windows 10/11 x64 senza runtime .NET installato.
- [ ] Nuovo progetto: master 100%. Progetto salvato: master ripristinato.
- [ ] Test audio e uscita corretti a buffer 40 ms, poi verifica 20/80 se necessario.
- [ ] WAV mono/stereo a 44,1/48 kHz; MP3, FLAC, OGG Vorbis, M4A/AAC.
- [ ] Tracce da 2 secondi in RAM e da 25 minuti in streaming; partenza pronta dopo il caricamento.
- [ ] Play, pausa, ripresa esatta, fine file, Stop e 16 tracce simultanee.
- [ ] Volume per slot, master, fade in/out; verifica clipping con molti slot.
- [ ] Rimuovi l'audio USB: alert, posizione congelata, retry, ripresa sullo stesso endpoint.
- [ ] Rinomina l'originale dopo PRONTO: playback dalla cache continua.
- [ ] File mancante e corrotto: errore dello slot, altre tracce proseguono; nessun vecchio audio lanciato al posto del file fallito.
- [ ] Carico CPU/disco elevato durante il play e caricamento di nuove tracce: controlla glitch e contatori underrun.

## Fase 2 - controller, learn e modalità

- [ ] Porta IN corretta, ingresso visto senza app concorrenti.
- [ ] LEARN di tutti i 16 pad: nessun lancio durante learn, nessuna mappatura duplicata.
- [ ] Test delle quattro modalità anche con pressioni ravvicinate e Note On velocity zero.
- [ ] Senza interruzione: né ripressione né rilascio né Pausa fermano l'audio; Stop mouse e PANIC sì.
- [ ] Mouse: premi e rilascia fuori dal pad; Hold riceve il rilascio.
- [ ] Scollega controller durante più tracce: tutte continuano e il mouse funziona.
- [ ] Ricollega controller: recupero automatico e nuova pressione accettata.
- [ ] Porta IN occupata: alert e retry; audio invariato.
- [ ] Carico elevato, Note Repeat/aftertouch: nessun crash; disattiva Note Repeat per il comportamento normale dei pad.

## Fase 3 - interfaccia

- [ ] In Serata tutti i 16 pad raggiungibili con scorrimento, mai schiacciati: provare 1×16, 16×1, 2×8, 4×4 e 16×16 a finestra piccola e massimizzata.
- [ ] F11/Esc e scale Windows 100%, 125%, 150%, 200% con monitor previsto per lo show.
- [ ] Tempi grandi leggibili; prova una durata superiore a un'ora.
- [ ] Forma d'onda, posizione e tempi della traccia selezionata/ultima avviata.
- [ ] Nomi lunghi, colori, stati play/pausa/errore e slot solo MIDI senza file audio.
- [ ] Passaggi Setup/Serata durante 16 voci: nessuna interruzione.
- [ ] Termina solo il processo `--ui`: audio prosegue e il supervisore riapre la UI.
- [ ] Blocca la UI in un test controllato: recupero dopo timeout senza fermare l'audio.

## Fase 4 - MIDI OUT e ricevitore compatibile

- [ ] Interfaccia USB–MIDI OUT DIN collegata al MIDI IN del dispositivo ricevente.
- [ ] Tools → MIDI Monitor: canale, nota, velocity, Note On e Note Off corretti.
- [ ] Remote Inputs: executor scelto e funzione appropriata su pressione/rilascio.
- [ ] Impulso temporizzato e Gate, incluso rilascio comune di due slot sulla stessa nota.
- [ ] Solo MIDI non avvia audio; Audio+MIDI esegue entrambe le azioni.
- [ ] Audio mancante/corrotto con azione Both: errore audio, messaggio MIDI comunque inviato.
- [ ] Porta OUT occupata, interfaccia scollegata e simulazione di errore: nessun arresto audio.
- [ ] Ricollega OUT: nessun vecchio GO ripetuto; verifica lo stato del ricevitore.
- [ ] MSC: Device ID e command format, Default Only oppure Exec.Page/Exec Page uguali sul dispositivo ricevente.
- [ ] GO cue esplicita, STOP = pausa, RESUME sulla prima pagina/Main Executor.
- [ ] Verifica sul dispositivo ricevente reale con il firmware usato e, se presente, la sessione master.
- [ ] PANIC rilascia Note note al modulo; non è un blackout universale e non sostituisce lo Stop del dispositivo ricevente.
- [ ] Scollega solo il cavo DIN: osserva che la porta Windows può restare “aperta” senza poter rilevare l'assenza di ACK.

## Fase 5 - progetti e stabilità

- [ ] Salva, chiudi, riapri: 16 slot, nomi, colori, mode, volumi, mappature e routing ripristinati, nessun autoplay.
- [ ] Copia progetto+audio in un'altra cartella mantenendo i percorsi relativi: apri correttamente.
- [ ] JSON danneggiato o schema sconosciuto: avviso, progetto corrente conservato.
- [ ] Backup `.bak` e ripristino automatico dell'ultima configurazione.
- [ ] SHOW LOCK: prova caricamento, clear, editor già aperto, learn, routing, RAM e test output. Tutto rifiutato.
- [ ] Con lock: Play, selezione, volumi, Stop e PANIC restano operativi.
- [ ] PANIC dopo una raffica di pad: nessun vecchio avvio parte in ritardo.
- [ ] Log non scrivibile/disco pieno: avviso senza bloccare l'audio.
- [ ] Termina separatamente `--midi-in` e `--midi-out`: riavvio del solo modulo; audio continuo.
- [ ] Termina `--decode`: fallisce soltanto quel caricamento.
- [ ] Termina `--audio`: interruzione attesa, recupero del progetto senza partenza automatica.
- [ ] Driver bloccato/heartbeat assente: watchdog con backoff; annota modulo riavviato e tempi.

## Resistenza e latenza

- [ ] Almeno 8 ore sul PC definitivo, gruppi di tracce da 25 minuti riavviati prima della fine.
- [ ] Carico abituale 4–8 voci, picchi a 16; altre attività reali del PC.
- [ ] Disconnessioni MIDI ripetute: nessun glitch audio correlato.
- [ ] Disconnessione audio annotata separatamente dalle interruzioni inattese.
- [ ] Zero crash durante uso normale, zero underrun/glitch inattesi e memoria senza crescita continua a slot invariati.
- [ ] Latenza pad→suono misurata sul sistema: riportare buffer, driver, metodo e risultato. Nessuna misura hardware è disponibile da questa consegna.

## Modello per segnalare un bug

- Versione e hash exe:
- Windows / CPU / RAM / scheda e driver:
- Controller e interfaccia MIDI:
- Progetto o slot interessato e formato audio:
- Passaggi esatti per riprodurre:
- Risultato atteso:
- Risultato effettivo, incluso se l'audio ha proseguito:
- Ora dell'evento e log allegati:


## GUI 0.6.0 - verifica visiva e interazione

- [ ] Setup: sidebar scorrevole, selezione audio/MIDI, apri/salva, budget e diagnostica.
- [ ] Tutti i menu a discesa aprono e selezionano correttamente con il tema scuro.
- [ ] Slider master e slot: mouse, trascinamento e tastiera; percentuale master leggibile.
- [ ] Configura slot: campi, menu, testo, errori e pulsante Applica sul tema scuro.
- [ ] Setup e Serata mantengono le griglie scelte e rendono accessibili tutte le celle con scorrimento quando serve.
- [ ] Finestra piccola, massimizzata, F11/Esc, monitor e DPI previsti per lo show.
- [ ] Tre tempi grandi leggibili, anche con durate oltre un'ora.
- [ ] Cursore più fluido durante play; pausa, stop, retrigger e cambio slot riportano
      correttamente la posizione. Nessun avanzamento indefinito a motore irraggiungibile.
- [ ] Feedback alla pressione: Hold riceve il rilascio anche fuori dal pad; PANIC è immediato.
- [ ] SHOW LOCK: sidebar di configurazione disabilitata, trasporti e diagnostica accessibili.
- [ ] Sotto carico grafico la riproduzione audio resta continua.


## Correzioni 0.6.1

- [ ] Trascorso e rimanente cambiano nello stesso aggiornamento per tutta la traccia.
- [ ] File con durata frazionaria: totale arrotondato per eccesso, zero rimanente a fine.
- [ ] Pausa/ripresa, Stop e retrigger non lasciano il contatore sulla posizione precedente.
- [ ] Cambiando slot il player mostra subito i tempi della selezione corretta.
- [ ] Serata: tre pulsanti ▶ Ⅱ ■ uguali e cliccabili in ogni pad, anche ridimensionando.
- [ ] Hold: rilascio del pulsante ▶ o del titolo fuori dalla scheda ferma lo slot.
- [ ] Titoli bianchi in ogni stato; barre colore personalizzate preservate.
- [ ] Nessun effetto sull'audio durante recupero/visualizzazione della forma d'onda.

## Novità 0.7.2 - dispositivi, colore, seek e griglia

- [ ] A tracce ferme, seleziona due uscite audio diverse: nomi leggibili nel menu e nel campo chiuso; Applica, poi Test. Verifica l'uscita fisica.
- [ ] Scollega un'uscita selezionata: alert/retry; la selezione non diventa una stringa DeviceInfo. MIDI assente/occupato non impedisce il test audio.
- [ ] Configura → Colore: apertura della ruota, trascinamento anche oltre il bordo, luminosità 0–100%, colori rapidi, anteprima e Fatto.
- [ ] Applica allo slot e riapri: colore conservato. Chiudi l'editor senza applicare: colore precedente conservato.
- [ ] Prova ruota a DPI 100/150/200%, vicino ai bordi dello schermo; prova frecce nella ruota e tastiera sullo slider.
- [ ] Doppio clic a 25/75% della waveform: cerca avanti/indietro in play, pausa e stop; verifica punto di ripresa, tempi e cursore.
- [ ] Prova RAM e streaming da 25 minuti, inizio/fine file, altre 15 voci attive e carico disco. Solo lo slot selezionato cambia posizione.
- [ ] Ripeti seek con SHOW LOCK, modo Protected e slot audio+MIDI: nessun nuovo messaggio MIDI. Il pad conserva le sue modalità.
- [ ] Ripeti doppi clic ravvicinati; STOP/PANIC durante la preparazione: nessun avvio tardivo, nessuna coda illimitata.
- [ ] Budget RAM pieno o cache non leggibile: seek rifiutato con avviso, playback esistente e slot ancora utilizzabili. File originale mancante/corrotto non blocca gli altri slot.
- [ ] Imposta 2×8, 8×2, 4×4 e 5×4, premi Applica griglia; verifica tutti i 16 slot, ordine e ridimensionamento/F11 con celle leggibili e scorrimento quando serve.
- [ ] 3×5 o meno di 16 celle: avviso e layout precedente conservato. SHOW LOCK blocca Applica griglia.
- [ ] Salva/riapri JSON: righe/colonne conservate; vecchi progetti senza Grid si aprono 4×4.
- [ ] Scollega il pad durante seek e modifica griglia: playback e mouse restano operativi; ricollegamento automatico.

## Interfaccia 0.7.2

- [ ] Griglie Setup e Serata indipendenti: modifica, salva e riapri; vecchi progetti con Setup 4×4.
- [ ] SHOW LOCK disabilita la barra griglia in entrambe le viste.
- [ ] Griglie estreme: controlli interamente visibili nelle celle e ultimo slot raggiungibile con entrambe le barre.
- [ ] Hover fermo 650 ms: popup sotto la forma d’onda; movimento, clic e uscita lo chiudono; nessun popup senza audio.
- [ ] Popup vicino ai bordi dello schermo, monitor multipli e DPI 100/150/200%.

- [ ] Colori chiari/scuri e personalizzati: fondo leggermente sfumato in entrambe le viste, titolo bianco e controlli leggibili.

## Titoli Serata 0.7.3

- [ ] Titolo lungo: lettura completa, pause agli estremi, scorrimento fluido e nessuna sovrapposizione ad altre celle.
- [ ] Titolo breve: fermo; cambio nome/font/dimensioni: scorrimento ricalcolato.
- [ ] Passaggio Setup/Serata, ridimensionamento/F11 e DPI 100/150/200%.
- [ ] Titoli lunghi scorrono anche con animazioni Windows disattivate.

## Regressione layout 0.7.4

- [ ] Nomi fino a 100 caratteri non cambiano larghezza o altezza dei box.
- [ ] Tutte le celle hanno la stessa dimensione: Setup e Serata, 4×4, 2×8, 8×2.
- [ ] Cambio griglia, finestra e F11: nessun box gigante, testo lungo in scorrimento dentro il box.
- [ ] Barra orizzontale accessibile su tutta la larghezza della vista.

## Regressione 0.7.5

- [ ] Griglia 2×8: trascinare barra orizzontale ai due estremi e cliccare sulla pista.
- [ ] Griglia 8×2: trascinare barra verticale ai due estremi e cliccare sulla pista.
- [ ] Sidebar: trascinamento barra verticale e rotella/touchpad.
- [ ] Nome lungo in Serata: testo completo in movimento, pausa iniziale/finale e ritorno; nome breve fermo.
- [ ] Cambiare nome, griglia, finestra e vista durante lo scorrimento: nessun ingrandimento del box.

## Trackpad e barre 0.7.6

- [ ] Griglia 2×8: due dita orizzontali sopra le celle, in entrambe le direzioni e con piccoli movimenti.
- [ ] Il gesto sulla sidebar non sposta la griglia; nessun doppio spostamento.
- [ ] Monitor a sinistra del primario e DPI 100/150/200%.
- [ ] Shift + rotella muove orizzontalmente; rotella normale verticalmente.
- [ ] Barre moderne: trascinamento, clic sulla pista, hover e stato premuto, orizzontale e verticale.

## Reset configurazione 0.7.7

- [ ] Configura uno slot con campi modificati; Reset ripristina nome, colore, play, fade, pad e MIDI/MSC.
- [ ] Dopo Reset, chiudere senza Applica conserva la configurazione precedente.
- [ ] Reset e Applica salvano i default; file audio e volume restano quelli caricati.
- [ ] Nota OUT predefinita 60 + indice slot; pad non associato.
- [ ] SHOW LOCK rifiuta il salvataggio anche con editor già aperto.

## Versione 0.8.0

- [ ] Avvio libero, anche senza connessione Internet; nessuna schermata di attivazione.
- [ ] Barra con sole Righe, Colonne e Applica, senza titoli Griglia Setup/Serata.
- [ ] Numero slot 1, 16, 32, 100 da Setup; griglie entro 10×10 e celle leggibili.
- [ ] Ridurre e poi aumentare gli slot: configurazioni ripristinate, slot nascosti inattivi.
- [ ] Griglie estreme entro 10×10: barre, trackpad e cambio vista.
- [ ] Configura/Reset, audio, MIDI e selezione dello slot 100.
- [ ] Salvare, chiudere e riaprire un progetto con 100 slot.
- [ ] Credito Created by Alessio Natalucci discreto e leggibile accanto al marchio.

## Settings e lingue 0.9.0

- [ ] Ingranaggio apre una sola finestra Settings, con scelta Italiano / English / Español / Français.
- [ ] Ogni lingua aggiorna navigazione, comandi, tooltip, stati, editor slot e palette già aperti.
- [ ] Nomi personalizzati, percorsi audio, valori dei campi e messaggi MIDI non cambiano con la lingua.
- [ ] Riavvio: lingua salvata sul PC ripristinata anche senza rete.
- [ ] Audio in play: cambiare lingua non interrompe o riavvia tracce.
- [ ] Settings non contiene selettori colore; la ruota è disponibile solo in Configura slot.
- [ ] DPI 100/150/200% e titoli lunghi: nessuna sovrapposizione, tutte le lingue.
- [ ] MIDI Note/MSC con ricevitore compatibile: payload e profili precedenti invariati.

## Settings e colore 0.9.2

- [ ] Setup non mostra più Memoria audio o Diagnostica; entrambe sono nella finestra Settings.
- [ ] Aprire, chiudere e riaprire Settings in Setup e in Serata: nessun errore di parent UI e dati aggiornati.
- [ ] Budget valido applicato e salvato nel progetto; budget sotto il minimo riservato rifiutato; SHOW LOCK blocca il controllo.
- [ ] Apri log e Licenze funzionano anche in SHOW LOCK.
- [ ] Default accanto al colore ripristina solo il colore; nome, audio, volume e MIDI restano invariati.
- [ ] Anteprima colore/ruota/luminosità coerenti; Applica salva; chiudere senza Applica scarta il nuovo colore.
- [ ] Quattro lingue e DPI 100/150/200%: Settings scrollabile e controlli leggibili.

## Progetti, follow e logo 0.10.0

- [ ] Apri/Salva usa la vista interna; nessuna finestra nativa di scelta file per progetti.
- [ ] Salva nuovo progetto, conferma/annulla sovrascrittura, file non scrivibile e JSON corrotto: feedback corretto senza perdita dei dati correnti.
- [ ] Cambia cartella da Settings e ripristina Default; al riavvio preferenze conservate.
- [ ] Vecchio recovery letto; nuovi salvataggi automatici in project; file precedenti conservati.
- [ ] Nomi Unicode, nomi Windows riservati e tentativi di percorso: validazione coerente.
- [ ] Serata: pad fuori vista da MIDI, anche ultimo slot e asse orizzontale; pad visibile non muove la griglia.
- [ ] Follow OFF: nessuno scorrimento; riavvio e cambio lingua conservano OFF.
- [ ] Cambia ON/OFF mentre arriva un trigger: nessun callback arretrato sposta la vista dopo OFF.
- [ ] Scorri manualmente durante play senza nuovi trigger: la vista resta dove scelta; Setup non scorre automaticamente.
- [ ] Logo verdino e S scura in alto a sinistra e nelle icone; LIVE vicino a STAGEPAD.
- [ ] Tutte le viste nelle quattro lingue e a DPI 100/150/200%.

## Editor e icone 0.10.1

- [ ] S bianca grande su verdino nella barra Windows, nell’eseguibile e nelle finestre.
- [ ] Editor a destra/sinistra del pad, mai oltre l’area utile del monitor.
- [ ] Dimensione iniziale esterna 650×950 pixel dove l’area schermo lo consente.
- [ ] Monitor secondario a sinistra, scaling 100/125/150/200%, barra in alto e monitor con DPI diversi.
- [ ] Schermo più piccolo: form ridotto con campi leggibili e tutti i comandi raggiungibili con scorrimento.
- [ ] Riaprire editor dopo scorrimento griglia e spostamento finestra principale: ancora riferito al pad corretto.


## Finestra e progetti 0.10.2

- Avvio: 1250×960 pixel centrati, limitati all’area di lavoro; controllare anche DPI misti.
- Apri: elenco utilizzabile a finestra normale, ridotta e schermo intero; oltre 50 file.
- Nomi lunghi e quattro lingue: elenco scorrevole, azioni a capo, percorso con tooltip.
- Rinomina/Duplica: nome valido, annulla, nome già occupato, file rimosso esternamente.
- Elimina: conferma/annulla; audio intatti; progetto aperto resta in memoria e risalvabile.
- SHOW LOCK impedisce apertura e modifiche ai file dalla vista progetti.
- Etichetta Show aggiornata dopo apertura, salvataggio e rinomina; ripristino al riavvio.


## Show e riavvio 0.10.3

- Show sempre verde con audio fermo, in riproduzione, in errore, con SHOW LOCK e in Serata.
- Conferma/annulla eliminazione: sfondo elenco e selezione scuri, nessun colore del tema Windows; verificare anche cambio focus.
- Riavvio dopo Apri, Salva e Rinomina: stesso show, percorso associato e modifiche recuperate.
- Progetto aperto eliminato: configurazione recuperabile, percorso scollegato.
- File originale mancante e copia recente corrotta: recupero disponibile e avviso.


## Testi e icone 0.10.4

- Editor slot: Canale Pad nelle quattro lingue; verifica Applica e limiti 1-16.
- Logo: S bianca pulita in intestazione, finestre, eseguibile e barra Windows; DPI 100/125/150/200%.
- Messaggi audio e intestazione mappatura con trattini semplici.


## Regressione avvio 0.10.5

- Build.ps1 esegue StagePad.exe --check-ui-assets dopo il publish su Windows.
- Avviare l’eseguibile senza argomenti: compare la finestra principale e viene recuperato l’ultimo show.
- Aprire Settings e configurazione slot: icone PNG caricate senza eccezioni.
- Verificare il logo in intestazione e i messaggi Canale Pad/trattini semplici.


## Audio/MIDI/OSC 0.11.0

- Aprire un progetto 0.10.x: conservare associazioni pad, audio e MIDI Note/MSC.
- Editor: selezionare ciascuna delle sette combinazioni; nessuna selezione mostra errore.
- Aggiungere, rimuovere e modificare più comandi MIDI e OSC; passare tra le quattro lingue.
- Disabilitare/riabilitare un’uscita conserva i suoi comandi validi; Reset ripristina i default.
- CC e Program Change: verificare valori 0-127 e canale; verificare anche MSC pagina 1.
- OSC: IP/host, porta, percorso e tipi i/f/s/T/F corrispondenti al ricevitore; provare due destinazioni.
- Pressione del pad MIDI: una esecuzione del lotto; rilascio chiude tutte le Gate e non reinvia OSC.
- Provare tutte le combinazioni anche con mouse e in Serata, mantenendo SHOW LOCK attivo.
- Audio assente o uscita MIDI scollegata: gli altri protocolli continuano.
- STOP ALL: audio fermo, Gate rilasciate e OSC pendente annullato; nessun replay dopo riconnessione.
- Salva, riapri e riavvia: conservare selezioni e comandi senza riprodurli automaticamente.
- Settings: conteggio invii OSC locali ed eventuali errori; confrontare con la ricezione esterna.
- Eseguire Build.ps1 su Windows: include smoke test WPF dell’editor combinato e delle icone.


## Nomi, volumi e aggiornamento 0.12.0

- Nome vuoto: caricare A poi B, deve seguire il file. Nome personalizzato: resta fisso, anche se inizia con Slot. Cancellare il nome ripristina automatico.
- Settings > Volumi MIDI: LEARN master e traccia su due fader/knob CC assoluti diversi. Provare 0/127 e canali differenti; assegnazione duplicata rifiutata.
- Con due tracce in play controllare l’ultima avviata; pausa/stop fa ricadere sull’altra ancora in play. Nessuna modifica a slot solo selezionati o a MIDI/OSC-only.
- SHOW LOCK: volumi live operativi, modifica/LEARN delle assegnazioni bloccati. Salva e riapri: mappature conservate.
- Chiudere mentre MIDI OUT è occupato, mentre si trascina un volume e con Settings/editor aperti. Processi devono uscire dopo pochi secondi.
- Riaprire subito: attendere chiusura precedente e avviare. Se già visibile, attivare la finestra senza doppio audio o cambio fullscreen.
- Recupero legacy: vecchia istanza invisibile bloccata, nuova copia EXE con stesso utente/sessione; verificare il recupero. Nessun altro prodotto/utente/sessione deve essere terminato.
- Simulare arresto supervisore: figli devono uscire; provare poi una nuova apertura.
- Aggiornare solo EXE da un’altra cartella; conservare AppData, cartella progetti personalizzata, ultimo show, colori e assegnazioni.
- Eseguire Build.ps1 su Windows per il controllo WPF e per generare il pacchetto portatile ZIP.
