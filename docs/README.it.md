<img src="../StagePad.App/Assets/StagePad.png" width="72" alt="StagePad logo">

# StagePad

**Launcher audio e trigger MIDI/OSC open source per Windows.**

**Italiano** · [English](../README.md) · [Español](README.es.md) · [Français](README.fr.md)

Windows x64 · MIT · 1–100 slot · Audio + MIDI + OSC · IT / EN / ES / FR

Prepara tracce audio e messaggi MIDI/OSC in **Setup**, poi passa a **Serata** per
attivarli dalla griglia con mouse o controller MIDI. StagePad è pensato per
musica dal vivo, teatro, prove, installazioni sonore e flussi MIDI compatibili.

Created by **Alessio Natalucci**.

[Scarica per Windows](https://github.com/Alessio278/StagePad-Live/releases) · [Guida MIDI](MIDI.it.md) · [Novità](../CHANGELOG.md) · [Segnala un problema](https://github.com/Alessio278/StagePad-Live/issues)

La configurazione dello slot si apre accanto al relativo pad: a destra se c’è spazio, altrimenti a sinistra. La dimensione iniziale richiesta è **650×950 pixel dello schermo**, ridotta solo se l’area disponibile è più piccola. Etichette e scorrimento si adattano alle finestre più strette e ai DPI elevati.


La finestra principale si apre centrata a **1250×960 pixel dello schermo**, adattandosi agli schermi più piccoli. **Show: nome progetto** precede Audio pronto. L’elenco progetti resta scorrevole nella finestra normale. Seleziona un progetto per **Rinomina**, **Duplica** o **Elimina**. Il nome si inserisce nell’app; eliminare richiede conferma e lascia gli audio sul disco. Rinomina e Duplica non sovrascrivono altri progetti. SHOW LOCK protegge queste azioni. Se elimini il progetto aperto, la configurazione audio resta in memoria: usa Salva per conservarla nuovamente.


All’avvio StagePad ripristina automaticamente l’ultimo show e le modifiche recenti recuperate, mantenendo l’associazione al file salvato. L’etichetta **Show** resta sempre verde accesa. L’elenco conserva sfondo scuro e selezione durante la conferma di eliminazione. Se il file originale manca, la configurazione recuperata resta disponibile con un avviso.

## Nomi automatici, volumi MIDI e aggiornamenti

Lascia vuoto **Nome** nelle impostazioni dello slot per usare automaticamente il nome del file audio, senza estensione. Il titolo cambia quando carichi un’altra traccia. Scrivi un titolo per mantenerlo fisso; cancellalo per ripristinare il nome automatico.

In **⚙ Impostazioni → VOLUMI MIDI** assegna separatamente un canale/CC al **master** e un canale/CC al volume della **traccia corrente in riproduzione**. Usa i due pulsanti **LEARN** e muovi il fader/knob corrispondente, oppure inserisci i valori e premi **Applica assegnazioni MIDI**. Sono supportati CC assoluti 0-127; `-1` disabilita l’assegnazione. Due controlli diversi possono usare lo stesso numero CC su canali diversi. Le assegnazioni vengono salvate nello show.

La traccia controllata è l’ultimo audio avviato che sta ancora suonando. Un tasto solo MIDI/OSC o la selezione di uno slot non sposta il fader audio. Se la traccia più recente è ferma o in pausa, viene controllato l’altro audio avviato più recentemente ancora in play; senza audio in play il fader traccia non modifica slot.

La chiusura invia una richiesta diretta al supervisore e limita l’attesa dei moduli. Una nuova apertura riporta in primo piano un’istanza visibile; per un’istanza invisibile rimasta bloccata tenta il recupero dei soli processi StagePad dello stesso utente e sessione.

**Aggiornamento portatile:** chiudi StagePad e sostituisci solo `StagePad.exe`, poi riaprilo. Progetti, impostazioni, mappature e recupero restano nelle cartelle già configurate. Vedi [istruzioni di aggiornamento](UPDATE.it.md).

## Azioni combinate Audio, MIDI e OSC

Nelle impostazioni dello slot seleziona **Audio**, **MIDI** e/o **OSC** in **Azioni alla pressione**. Sono disponibili tutte e sette le combinazioni. Il pad MIDI associato e il tasto a schermo eseguono la stessa scelta.

Aggiungi fino a **16 comandi MIDI** e **16 comandi OSC** per slot. MIDI supporta Note (Impulso/Gate), MSC, Control Change e Program Change. Tutti i comandi MIDI usano la destinazione selezionata in Setup. Numeri programma e valori CC seguono l’intervallo 0-127 indicato nei campi.

Per ogni comando OSC inserisci host/IP, porta UDP e percorso, poi gli argomenti tipizzati: intero, decimale, testo ASCII, vero o falso. Esempio: host `127.0.0.1`, porta `8000`, percorso `/show/go`, argomento intero `1`. Ogni comando può avere un destinatario diverso. Vedi la [guida OSC](OSC.it.md).

Audio, MIDI e OSC vengono inviati indipendentemente. Un audio mancante non impedisce MIDI/OSC; un’uscita MIDI occupata non blocca audio o OSC. Ogni elenco viene inviato nell’ordine visualizzato; i tempi fra audio, MIDI e UDP dipendono anche dai ricevitori. Il rilascio chiude tutte le note MIDI Gate dello slot. STOP ALL annulla gli OSC ancora in coda e rilascia le note MIDI. La diagnostica in Settings conta i messaggi UDP trasmessi localmente: verifica la risposta sul software ricevente.

Selezioni, comandi, destinazioni e argomenti vengono salvati nel progetto e nel recupero. I progetti precedenti conservano il comportamento MIDI originale. Limiti: 16 argomenti per messaggio OSC e 6000 byte complessivi di configurazione comandi per slot.

## Progetti dentro l’app

**Apri / Salva** mostra una vista progetti interna, senza finestra di scelta file.
Seleziona un progetto per aprirlo oppure inserisci il nome e salva. La conferma
per sovrascrivere un file avviene nella stessa vista. Aggiorna elenco mostra i
progetti JSON della cartella; i file di recupero automatico non compaiono nell’elenco manuale.

In **⚙ Impostazioni → Progetti**, inserisci il percorso completo della cartella
e premi **Applica cartella**. **Default** ripristina `%LOCALAPPDATA%\StagePad\project`
per l’utente Windows corrente, ad esempio
`C:\Users\<utente>\AppData\Local\StagePad\project`. I file esistenti non vengono
spostati cambiando cartella. Il recupero automatico usa ora `project\last-project.json`,
continuando a leggere i dati della vecchia cartella `recovery`, che non vengono eliminati.

In **⚙ Impostazioni → Serata**, **Scorrimento automatico al pad attivato** è
attivabile/disattivabile, ON per impostazione predefinita e salvato sul PC. In
Serata porta in vista un pad appena attivato fuori dall’area visibile, spostandosi
solo quanto serve, anche orizzontalmente. Un pad già completamente visibile non
muove la griglia. Funziona anche da controller MIDI, non prende il focus e non
invia comandi audio/MIDI aggiuntivi. Non agisce in Setup e non insegue continuamente
la riproduzione.

Il logo è la **S bianca più grande su quadrato arrotondato verdino**. LIVE verde è vicino a
STAGEPAD; lo stesso simbolo è usato nelle icone Windows.

## Funzioni

- Da **1 a 100 slot attivi**, una voce audio per slot.
- Griglie Setup e Serata indipendenti, fino a **10×10**, con celle leggibili e scorrimento.
- Play, pausa, stop, volume, fade e master; modalità avvio/retrigger, Hold,
  riproduzione protetta e play/pausa/riprendi.
- MIDI IN Note On/Off con Learn; MIDI OUT Note a impulso o gate e i formati
  MSC GO/STOP/RESUME implementati.
- Forma d’onda con salto tramite doppio clic e tempi trascorso/totale/rimanente.
- Show Lock, Stop All, schermo intero e scorrimento dei titoli lunghi.
- Colori dei singoli slot con fondo soffuso e ruota colore in **Configura slot**.
- Italiano, inglese, spagnolo e francese dalla schermata **⚙ Impostazioni**.
- Progetti JSON con salvataggio e recupero; l’audio resta nei file originali.

Nessun account, attivazione o controllo licenza online. Dopo il download l’app
si usa anche offline. La prima compilazione dei sorgenti richiede Internet per
scaricare le dipendenze.

## Avvio rapido

1. Apri **Releases** e scarica `StagePad.exe` dagli allegati della versione.
2. Chiudi l’eventuale versione precedente e avvia il file. La build Windows x64
   include il runtime: per usarla non serve installare .NET.
3. In **Setup**, scegli l’uscita audio, parti da **40 ms**, premi **Applica** e
   prova **Test audio · −24 dB**.
4. Scegli il numero di slot e **Righe / Colonne**. Carica le tracce e attendi PRONTO.
5. In **Configura**, imposta nome, colore, azione, modalità, fade, mappatura pad e MIDI OUT.
6. Scegli le porte MIDI IN / OUT. Per associare un pad, premi **LEARN** e poi
   il pad fisico: quella pressione serve alla mappatura, non avvia lo slot.
7. Salva il progetto e passa a **Serata**. **F11** attiva lo schermo intero;
   **Esc** ne esce. Show Lock protegge le impostazioni durante l’esecuzione.

La lingua iniziale è l’italiano. Apri **⚙ → Lingua dell’interfaccia** per cambiarla:
si aggiorna subito, anche negli editor aperti, viene salvata sul PC e non richiede
riavvii audio. Memoria audio e Diagnostica sono in Settings, con Apri log e
Licenze; il budget RAM resta salvato nel progetto. I selettori colore restano
soltanto nei singoli slot.

**Reset** ripristina i campi dell’editor; **Applica allo slot** salva le modifiche.
File audio e volume vengono conservati. Riducendo il numero di slot, quelli
successivi vengono nascosti e conservano configurazioni e percorsi.

**Default** accanto al selettore colore ripristina soltanto il colore predefinito dello slot, con anteprima immediata. Salva con **Applica allo slot**.

## Compatibilità

WAV PCM/float e OGG Vorbis hanno decoder inclusi. MP3, FLAC, M4A/AAC e altri
formati Windows dipendono dai codec Media Foundation installati. Supportati file
mono e stereo; non supportati OGG Opus, DRM e audio multicanale.

Il MIDI non è legato a una marca: usa controller Note On/Off e destinazioni che
riconoscano i messaggi configurati. Per collegare altri software può servire una
porta MIDI virtuale installata separatamente. CC, Program Change, MIDI clock e
MIDI thru automatico non sono implementati. Consulta i limiti MSC nella
[guida MIDI](MIDI.it.md).

## Stato beta e limiti

I test automatici coprono il motore e l’integrazione simulata. Interazione WPF,
WASAPI, periferiche MIDI reali, riconnessioni e spettacoli prolungati richiedono
prove sul PC Windows. Cento slot configurabili non garantiscono cento stream
simultanei sul proprio hardware.

- Eseguibile non firmato digitalmente: Windows può mostrare avvisi sull’autore.
- Prepara le tracce prima dello show: RAM e cache disco sono limitate.
- Il mix applica clipping di sicurezza, non un limiter musicale.
- PANIC ferma l’audio e rilascia le Note note al programma; non arresta
  universalmente tutti i dispositivi esterni.
- I progetti non incorporano gli audio: conserva insieme progetti e materiali.
- Finestre di scelta file e dettagli degli errori di sistema seguono la lingua di Windows.

## Compilazione e contributi

Installa **.NET SDK 10 x64**, nella versione indicata da `global.json`, e dalla
cartella principale esegui `Build.ps1` oppure apri `Compila.cmd`. Dopo i test trovi
`artifacts\win-x64\StagePad.exe`.

```powershell
dotnet run --project StagePad.Tests -c Release
dotnet run --project StagePad.IntegrationTests -c Release
```

`--stress` è una diagnostica aggiuntiva, non una prova di durata su hardware.
Segnalazioni, traduzioni e contributi sono benvenuti: [CONTRIBUTING](../CONTRIBUTING.md).
Risultati e checklist: [VERIFICA](VERIFICA.md) e [TEST-COMPLETI](TEST-COMPLETI.md).

Licenza [MIT](../LICENSE). Le licenze dei componenti sono nella cartella
[licenses](../licenses/) e nel pulsante **Licenze** dell’app.

## Slot EMERGENZA e controllo delle sequenze

Nelle impostazioni di uno slot abilita **EMERGENZA: pausa tutte le tracce**. Il pad mette in pausa tutti gli audio in riproduzione o in dissolvenza, compresi quelli protetti, conservando le posizioni. Lo slot non avvia il proprio audio. Seleziona MIDI e/o OSC per inviare i suoi messaggi di emergenza. Prima vengono annullate le sequenze in attesa e rilasciate le note MIDI attive. L’opzione è salvata nello show e funziona da mouse o pad MIDI associato. Usa Play per riprendere una singola traccia in pausa; i normali pad mantengono il comportamento della modalità scelta. STOP ALL continua a fermare e azzerare le posizioni. EMERGENZA agisce sull’audio software; la ricezione dei messaggi esterni va verificata sul dispositivo destinatario.

Usa ↑/↓ per riordinare i comandi MIDI/OSC, **Attesa prima (ms)** per i ritardi e **Test ora** per trasmettere un singolo comando a riproduzione ferma. Il test ignora il ritardo; per le note MIDI usa un Pulse temporizzato. Le due liste di uscita procedono separatamente. Limiti: 30 secondi per comando e 60 secondi per lista. Il rilascio del pad annulla le note Gate future; STOP annulla i comandi futuri dello slot. Mantieni i ritardi di emergenza a zero per l’invio immediato.

Nei Setting i volumi master/traccia supportano CC assoluti, tre codifiche di encoder relativi e **soft takeover**, che attende l’allineamento o attraversamento del volume corrente prima di acquisire un fader assoluto. Passo relativo regolabile. Lo scollegamento dell’ingresso rilascia i pad premuti e la riconnessione non ripete i trigger.

Nel browser progetti **Esporta show con audio** crea un pacchetto `.stagepad.zip` con lo show e i suoi file audio originali. **Importa show** accetta il percorso completo del pacchetto o un pacchetto nella cartella progetti e crea uno show con nome separato senza sovrascrivere. Ferma audio e sequenze prima dell’operazione. Limite dei media originali: 8 GiB.

**Setting → Aggiornamenti** consulta le release stabili di `Alessio278/StagePad-Live`. Installazione solo a riproduzione ferma, dati utente conservati, verifica SHA-256, copia di backup e ripristino automatico se il nuovo avvio non viene confermato. È disponibile anche il ripristino manuale della versione precedente. Per gli aggiornamenti online devono esistere repository e release pubblicate con gli asset richiesti: questo pacchetto sorgenti non li pubblica da solo. Resta disponibile la sostituzione manuale dell’EXE.


Le due opzioni nelle impostazioni volumi MIDI si chiamano **Evita salti di volume del master** e **Evita salti di volume della traccia**. Con fader assoluti attivano il controllo solo quando il fader raggiunge il volume corrente, evitando sbalzi. Nella 0.13.1 hanno testo bianco leggibile e una spiegazione sotto ciascuna.
