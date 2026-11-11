# Verifica 0.13.2 - repository StagePad-Live

Corretto l’endpoint di ricerca e il percorso consentito degli asset verso
Alessio278/StagePad-Live; link README e guide allineati. Versione EXE 0.13.2.
283 controlli di integrazione rieseguiti e superati, incluso il feed di
aggiornamento simulato con URL del nuovo repository. Publish Windows x64
riuscito. Restano validi i 203 controlli del nucleo/presentazione eseguiti
sulla 0.13.1; logica audio e interfaccia non cambiate in questa versione.

Controllo WPF, driver reali e aggiornamento completo tra EXE su Windows non
eseguiti qui. Prima pubblicazione consigliata come pre-release, poi promozione
a stabile soltanto dopo le prove. Il repository viene popolato dall’utente:
nessuna pubblicazione GitHub è stata effettuata da questa sessione.

# Verifica 0.13.1 - leggibilità delle opzioni volume

Corrette le etichette nere dei due checkbox anti-salto volume nei Setting:
testo bianco con ritorno a capo, nomi espliciti e spiegazione sotto ciascuno,
localizzati in italiano, inglese, spagnolo e francese. Nessuna modifica alla
logica audio, MIDI, pickup o ai dati salvati.

203 controlli del nucleo/presentazione rieseguiti e superati; compilazione e
publish Windows x64 riusciti. Le 283 verifiche di integrazione della 0.13.0
restano la baseline precedente e non sono state ripetute per questa modifica
visiva. Controllo visivo WPF/Windows non eseguito in questo ambiente Linux.

# Verifica 0.13.0 - emergenza e stabilità

486 controlli automatici superati: 203 sul nucleo/presentazione e 283 di integrazione.
Compilazione e publish single-file Windows x64 riusciti senza warning o errori.
Verificata anche la risorsa Win32 del prodotto/versione nell’EXE pubblicato.

Nuovi casi: EMERGENZA con coda audio satura, cursori conservati, rilascio Hold,
ripresa manuale, MIDI e UDP OSC indipendenti senza audio locale; trigger fresco
subito dopo PANIC; disconnessione input mentre è premuto Hold e riconnessione
senza replay; pickup e tre formati encoder relativi; ritardi MIDI/OSC, ordine,
annullamento STOP e indipendenza tra slot OSC; test con conferma dell’invio;
export/import show con audio, riferimenti relativi, nessuna sovrascrittura,
rifiuto traversal e pulizia staging; checksum errato senza alterare l’EXE,
sostituzione atomica e ripristino dei byte precedenti; feed GitHub simulato.
I test MIDI sono simulati, UDP usa loopback reale. Il feed aggiornamenti nei
test è simulato: nessuna release è stata pubblicata o scaricata da GitHub qui.

Non eseguiti in questo ambiente Linux: WPF e scale DPI, API native Windows,
WASAPI, dispositivi MIDI fisici, passaggio tra EXE durante aggiornamento e
rollback reale su Windows. --check-ui-assets include la validazione del
binario pubblicato, logo, editor EMERGENZA, modalità volume e finestra principale;
Build.ps1 lo esegue su Windows. Il workflow release crea una bozza da verificare
prima della pubblicazione. Per questi motivi la build resta beta, con priorità
alla stabilità e checklist pratica in TEST-COMPLETI.md.

# Verifica 0.12.0 - nomi, fader e chiusura

431 controlli superati: 183 sul nucleo/presentazione e 248 di integrazione.
Build/publish single-file Windows x64 completati senza errori o warning.
Restore pulito riuscito con NVorbis 0.10.5: corretto il riferimento 0.11.0
inesistente comparso nel sorgente precedente; il vecchio publish con cache
usava già il codec 0.10.5.

Verificati: cambio automatico titolo con un secondo file; titolo personalizzato
che inizia con Slot; ritorno al nome automatico cancellando il campo; fallback
slot vuoto; lettura dei nomi legacy derivati dal file; LEARN CC separati senza
applicare il primo valore; due volumi indipendenti; cambio destinazione al nuovo
audio in play; tasti solo MIDI che non rubano il fader; fallback se l’ultimo
audio è in pausa; nessun target con tutti gli audio fermi/in pausa; CC zero e 127;
SHOW LOCK protegge assegnazioni ma permette volumi live; salvataggio mappature.

La chiusura finestra segnala il supervisore tramite evento nominato per PID,
senza attendere la coda comandi. Cleanup con scadenza comune dei figli,
terminazione dei superstiti e cache vecchia ripulita in background all’avvio.
Recovery salvato prima della dismissione dei driver. Uscite UI ripetutamente
fallite sono limitate a due riavvii. Figli senza supervisore hanno un’uscita
forzata ritardata. Test eseguito con tre veri sottoprocessi non responsivi:
terminati entro tre secondi, lasciando vivo un discendente esterno. I processi
vengono terminati singolarmente, senza chiudere applicazioni esterne aperte
ad esempio per leggere un log.

Su Windows la riapertura conserva un’istanza visibile e tenta di recuperare
quelle invisibili, controllando prodotto, SID utente e sessione prima di
terminare processi. Istanze giovani hanno un periodo di attesa; finestre
ridotte a icona vengono ripristinate, senza cambiare una finestra massimizzata.
Questa parte Win32 NON è stata eseguita qui (ambiente Linux).

Esteso --check-ui-assets per impostazioni CC, costruzione della finestra
principale e segnale di chiusura con evento isolato di test. Deve essere
eseguito da Build.ps1 su Windows; qui è stata verificata la compilazione.
Restano da verificare driver MIDI/audio fisici, foreground/recovery Win32,
chiusura/riapertura reale e interfaccia alle diverse scale DPI.

# Verifica 0.11.0 - azioni Audio/MIDI/OSC combinate

383 controlli superati: 174 sul nucleo/presentazione e 209 di integrazione.
I test MIDI usano hardware simulato; OSC viene inviato e ricevuto realmente su
UDP loopback. Build/publish single-file Windows x64 completati senza warning o errori.

Nuovi casi verificati:

- Tutte le sette combinazioni Audio/MIDI/OSC, attivate da nota/canale MIDI.
- Due datagrammi OSC per pressione e relativi byte, indirizzi, argomenti e ordine.
- Vettori binari OSC per int32, float32, stringa ASCII, T/F, padding e signed int.
- Più comandi MIDI Note Gate, CC, Program Change e MSC nella stessa pressione.
- Rilascio di entrambe le note Gate; compatibilità degli ownership condivisi preesistenti.
- MIDI disabilitato senza invii; OSC disabilitato senza datagrammi; audio solo se selezionato.
- Audio mancante con invii OSC/MIDI ancora indipendenti.
- MIDI bloccato con audio e OSC operativi.
- Salvataggio e lettura di selezioni e liste; riapertura senza invio automatico OSC.
- STOP ALL annulla OSC in attesa DNS; nuovi invii funzionano senza replay del lotto annullato.
- Risoluzione localhost con ricevitore IPv4; preferenza IPv4 e supporto IP IPv6 esplicito.
- Validazione limiti e tipi, compatibilità JSON precedente con singolo Midi.
- Catalogo nelle quattro lingue e segnaposto delle traduzioni.

Per conservare snapshot e file entro limiti prevedibili: massimo 16 comandi MIDI,
16 OSC, 16 argomenti OSC e 6000 byte di configurazione comandi per slot.
Lettura file progetto limitata a 4 MiB. Invio OSC su worker separato con coda
limitata e scadenza dei messaggi; nessun I/O rete sul callback audio.

Il controllo Windows --check-ui-assets include ora creazione/lettura dell’editor
con tutte e tre le azioni, più comandi MIDI e argomenti OSC. NON eseguito qui:
WPF e driver Windows reali. Non verificati ricevitori remoti, firewall, porte
MIDI fisiche e sincronismo temporale tra protocolli: eseguire la checklist
sui dispositivi di destinazione prima di una serata.

# Verifica 0.10.5 - regressione di avvio logo

La 0.10.4 assegnava Transform direttamente alla geometria restituita da
Geometry.Parse, che WPF rende non modificabile. La 0.10.5 usa Clone prima della
trasformazione e congela solo l’immagine finale. Aggiornato anche il generatore,
con riproducibilità verificata per sorgente C#, SVG, PNG e ICO.

L’icona della finestra usa di nuovo BitmapImage dal PNG incorporato, come nella
0.10.3; mantiene la grafica ripulita della 0.10.4. Intestazione vettoriale corretta.
Build/publish Windows x64 single-file completati senza errori o warning.
Nessuna modifica a motore audio, progetti o impostazioni MIDI.

Aggiunto --check-ui-assets al programma e al Build.ps1 Windows. Verifica creazione
del logo, rasterizzazione a 16/32/64/256, pixel bianchi, PNG incorporato e handle
della finestra. Il controllo avrebbe intercettato la geometria non modificabile
prima della distribuzione. NON eseguito in questo ambiente Linux: richiede WPF
su Windows. Il build locale qui verifica la compilazione, non l’avvio reale.

# Verifica 0.10.4 - testi e logo

141 controlli nucleo/presentazione superati, incluso catalogo completo nelle
quattro lingue e conservazione dei segnaposto di traduzione. Modifica estetica:
nessun cambiamento alla logica audio/MIDI o ai progetti rispetto ai 136 controlli
di integrazione simulata della 0.10.3.

- Canale Pad: etichetta, chiave traduzione e lettura campo aggiornate insieme.
- Trattini lunghi sostituiti con trattini semplici nei testi sorgenti/distribuiti.
- Logo: S bianca senza contorno, trasformazione uniforme e margini più regolari.
- UI/finestre WPF: DrawingImage vettoriale, geometrie congelate e indipendenti dal DPI.
- PNG 1024x1024; ICO con 12 frame 16/20/24/28/32/40/48/64/96/128/192/256.
- Ogni frame ICO campionato direttamente dalla geometria a risoluzione 8x,
  evitando ridimensionamenti multipli. Controllati canale alpha e tutte le misure.
- Ispezionati PNG e anteprima dei frame piccoli su sfondo scuro.

Publish Windows x64 single-file completato. Resta da verificare su Windows la
resa effettiva delle icone nell’app, nella barra e nei collegamenti esistenti.

# Verifica 0.10.3 - show e ripristino

277 controlli superati: 141 sul nucleo/presentazione e 136 di integrazione simulata.
Verificati: nome/percorso dell’ultimo show scritti dal writer di recupero;
riapertura con configurazione recuperata; JSON manuali senza percorso sessione;
file originale assente con avviso; progetto eliminato scollegato; fallback alla
copia di backup e compatibilità con il vecchio recupero senza metadati sessione.
La coppia configurazione/percorso viene scritta atomicamente nello stesso JSON
di recupero, fuori dal thread audio, e svuotata dal writer alla chiusura.

Build/publish single-file Windows x64 riusciti. Template XAML dell’elenco validi
come XML. Il template controlla sfondo e selezione anche quando l’elenco è
disabilitato durante la conferma. Etichetta Show con colori fissi indipendenti
dallo stato audio.

Non verificati qui: caricamento dei template e resa WPF su Windows, transizioni
visive durante conferma/annullamento, temi Windows e cambio focus. Eseguire i casi
manuali della checklist sul sistema Windows di destinazione.

# Verifica 0.10.2 - finestra e gestione progetti

270 controlli superati: 141 sul nucleo/presentazione e 129 di integrazione con hardware simulato.
Nuovi casi: centratura 1250×960 su aree monitor differenti; nome show salvato;
duplicazione senza sovrascrittura; rinomina del progetto aperto e aggiornamento
associazione/nome; SHOW LOCK; protezione recupero automatico; divieto di spostare
il JSON fuori dalla cartella con riferimenti audio relativi; apertura copia;
eliminazione senza rimuovere audio o configurazione caricata.

Publish single-file Windows x64 completato senza errori o warning.
La vista progetti ha scorrimento esterno, elenco con viewport dedicata e azioni
che vanno a capo. Il pannello nome/conferma si apre solo quando serve.

Non eseguibili qui: WPF reale, dimensioni native e centratura sui monitor Windows,
interazione con mouse/trackpad e resa dell’elenco con DPI 100/125/150/200%.
Verificare su Windows la finestra normale e ridotta, nomi lunghi, molti progetti,
conferma/annullamento eliminazione, SHOW LOCK e passaggio tra le quattro lingue.

# Verifica 0.10.1 - icone e posizionamento editor slot

136 controlli sul nucleo/presentazione superati, inclusi sette nuovi casi del
posizionamento laterale: destra, sinistra, bordo inferiore, coordinate negative,
schermo piccolo, nessun lato interamente libero e area di lavoro con barra in alto.
Build/publish Windows x64 riusciti senza warning o errori. Nessuna modifica al
motore audio/MIDI o al salvataggio progetti rispetto ai 114 test di integrazione
superati nella 0.10.0.

- Editor richiesto a 650×950 pixel fisici. Posizione calcolata nello spazio
  schermo dello slot; area monitor ottenuta con MonitorFromRect/GetMonitorInfo.
- Dimensioni WPF convertite con il DPI della finestra, applicate con SetWindowPos;
  limitazione all’area di lavoro quando la dimensione richiesta non entra.
- Campi a colonne proporzionali e testo a capo per le dimensioni ridotte.
- S bianca ingrandita, fondo verde menta e contorno sottile per le misure piccole.
- ICO con frame 16/20/24/28/32/40/48/64/96/128/192/256; campionati i frame piccoli,
  con S bianca presente in oltre il 25% dei pixel totali. Finestre WPF alimentate
  dal PNG ad alta risoluzione invece che dal primo frame piccolo dell’ICO.

Non verificati in questo ambiente Linux: resa effettiva nella barra Windows,
cache dei collegamenti fissati, coordinate native con DPI misti, interazione con
il form e misure fisiche su hardware reale. Sono necessari i controlli Windows
riportati nella checklist.

# Verifica 0.10.0 - progetti integrati, follow e marchio

243 controlli superati: 129 sul nucleo/presentazione e 114 di integrazione simulata.
Build Release e pubblicazione single-file Windows x64 completate senza warning
né errori. Catalogo di 287 testi nelle quattro lingue.

- Scroll minimo per pad fuori vista: orizzontale, verticale, bordi, celle grandi,
  cella già visibile e viewport non ancora disposto. Il callback controlla di nuovo
  ON/OFF, vista corrente e identità/seriale dell’ultimo trigger prima di muoversi.
- Preferenze lingua, follow e cartella progetti conservate insieme; reset cartella
  dinamico per utente Windows, senza nome utente personale incorporato.
- Validazione nomi progetto, estensione JSON, caratteri e nomi Windows riservati,
  tentativi di percorsi e nomi riservati al recupero automatico.
- Apri/Salva usa un controllo interno alla vista principale, senza dialoghi file
  per i progetti. Conferma sovrascrittura nella stessa vista.
- Salvataggio eseguito su worker; la risposta positiva arriva dopo il commit su
  disco. Errori di scrittura/apertura conservano progetto e associazione precedenti.
  Il thread di controllo audio e il callback non eseguono operazioni disco.
- Recupero da cartella project con fallback ai dati recovery preesistenti e ai
  backup; dati precedenti non cancellati. File di recupero esclusi dall’elenco manuale.
- Logo condiviso PNG/SVG/ICO: S scura su verde menta; ICO multi-risoluzione incluso
  nelle risorse Windows. Verificati i collegamenti dei documenti e gli asset del logo.

Non eseguiti in questo ambiente Linux: interazione WPF, file manager integrato,
notifiche durante cambio lingua, trascinamento reale, follow tramite MIDI fisico,
resa icone nella shell e DPI multipli su Windows. I test simulati non certificano
hardware, driver o uno spettacolo di lunga durata.

# Verifica 0.9.2 - Settings e reset colore

Memoria audio, diagnostica in tempo reale, log e licenze spostati dalla sidebar
alla finestra Settings. Il budget resta protetto da SHOW LOCK e salvato nel
progetto; la diagnostica continua ad aggiornarsi anche quando Settings è aperto
in Serata. La finestra usa scorrimento con pulsante di chiusura sempre accessibile.

Default accanto al selettore colore richiama la stessa routine di aggiornamento
della palette: anteprima, ruota, luminosità e colore selezionato vengono aggiornati.
Solo il colore viene ripristinato; la persistenza resta su Applica allo slot.
Controlli e messaggi sono presenti nelle quattro lingue.

Compilazione/publish Windows x64 e controlli del catalogo completati. Nessuna
nuova operazione audio/MIDI. Interazione grafica, riapertura Settings e resa DPI
da verificare su Windows.

# Verifica 0.9.1

Dimensioni iniziali dell’editor slot aggiornate a 700×1000 unità WPF.
Compilazione e pubblicazione Windows x64; verifica visiva da eseguire su Windows.
Modifica di impaginazione: i test funzionali sottostanti restano quelli della 0.9.0.

# Verifica 0.9.0 - Settings, quattro lingue e presentazione MIDI generale

206 controlli superati: 99 sul nucleo/presentazione, 107 di integrazione simulata.
Build Release e pubblicazione self-contained Windows x64 riuscite senza warning
né errori. Catalogo di 254 testi con inglese, spagnolo e francese oltre all’italiano.

- Verificate completezza delle traduzioni e parità dei segnaposto di formato.
- Testati fallback dei nomi personalizzati e conservazione dei percorsi nei messaggi.
- Il blocco MidiCodec è identico byte per byte al sorgente 0.8.0: rimangono i test
  dei vettori Note/MSC e dei profili precedenti. Cambiano terminologia UI e documentazione.
- Settings separato, con quattro lingue e nessun selettore colore generale.
- Controllati collegamenti locali dei README/guide e struttura YAML dei file GitHub.
- CI GitHub preparata ma non ancora eseguita: il repository non è stato pubblicato
  da questo ambiente, in assenza del collegamento all’account.

Non eseguiti su Linux: apertura WPF, cambio lingua interattivo durante playback,
layout in tutte le lingue/DPI, selettori colore su Windows, driver e hardware reali.
Le finestre native e i dettagli degli errori del sistema seguono la lingua di Windows.
Le traduzioni richiedono anche revisione linguistica e visiva nell’uso reale.

Le sezioni successive sono risultati storici delle versioni precedenti.

# Verifica 0.8.0 - fino a 100 slot, griglie 10×10, distribuzione libera

196 controlli superati: 89 sul nucleo/presentazione e 107 di integrazione simulata.
La pubblicazione Windows x64 usa warning trattati come errori.

- Slot 1–100 e validazione delle griglie con massimo 10 righe e 10 colonne.
- Roundtrip JSON di 100 slot; note MIDI predefinite valide per tutti gli indici.
- Riduzione e riattivazione degli slot: nomi, mappature e percorsi audio conservati.
- Mixer: 100 voci operative, zero allocazioni gestite nei 100 blocchi Render
  controllati dopo warmup, PANIC su tutti gli slot.
- Integrazione: caricamento, play, seek, salvataggio e riattivazione dello slot 100;
  snapshot IPC completo; slot nascosti rifiutati; cambio numero bloccato durante
  playback/caricamenti e da SHOW LOCK.
- Rimossi i titoli della barra griglia. Selettore numero slot in Setup.
- Licenza MIT mantenuta e copiata nella radice LICENSE per la pubblicazione.
- Nessuna verifica di autorizzazione, credenziale, endpoint o servizio licenze
  nel codice o nella build. Il componente sperimentale è stato rimosso.

Limiti: nessun collaudo grafico WPF, gesto trackpad, driver WASAPI, controller
fisico o dot2 eseguito in questo ambiente Linux. Le 100 voci simulate non sono
una certificazione della capacità del PC/driver reale per 100 stream simultanei.

Le sezioni successive documentano le verifiche storiche delle versioni precedenti.

# Aggiornamento 0.7.7 - Reset configurazione slot

Build Release Windows x64 completata senza errori o warning.
Il pannello ricostruisce i campi dagli stessi default del progetto, usando
l’indice dello slot per nome e nota OUT predefiniti. La lettura preserva file
e volume originali. Reset e Applica sono disabilitati mentre il salvataggio
è in corso. Non eseguito il test grafico WPF in questo ambiente Linux.

# Aggiornamento 0.7.6 - trackpad e barre moderne

Build Release Windows x64 completata senza errori o warning. 77 controlli
superati, inclusi direzione orizzontale destra/sinistra e delta frazionari.
Verifica strutturale XAML: Track presente, comandi PageLeft/PageRight nel trigger
orizzontale e CommandTarget esplicito per i due pulsanti della pista.
Hook WM_MOUSEHWHEEL registrato alla creazione della finestra e rimosso alla
chiusura; coordinate schermo con segno e conversione DPI tramite PointFromScreen.
Solo la griglia visibile sotto il puntatore riceve il movimento orizzontale.
Non eseguiti su Linux: gesto reale del trackpad e trascinamento/clic WPF.

# Correzione 0.7.5 - barre e titoli

Build Release Windows x64 completata senza errori o warning.
74 controlli superati, inclusi sei controlli del movimento titoli: nome breve,
pausa iniziale, velocità, pausa finale, ritorno e ripetizione.
Verificata la struttura XAML: nessun template personalizzato delle barre;
restano template, comandi e trascinamento nativi, con dimensioni per orientamento.
Il testo Serata viene disegnato con FormattedText in un viewport ritagliato e
aggiornato dal clock UI esistente solo quando la vista è visibile.
Non è stato eseguito il test interattivo WPF su Windows in questo ambiente Linux.

# Correzione 0.7.4 - dimensioni griglia e titoli

Build Release Windows x64 completata senza errori o warning.
Il contenuto dello ScrollViewer riceve dimensioni finite derivate dal viewport,
con righe e colonne a dimensione esplicita uniforme. Il testo non può espandere
la griglia. La barra orizzontale non eredita più una larghezza totale di 7 unità.
Verificare visivamente su Windows: box con nomi fino a 100 caratteri, 4×4, 2×8,
8×2, cambio vista, ridimensionamento e F11. L’ambiente Linux non avvia WPF.

# Aggiornamento 0.7.3 - titoli Serata

Aggiunto viewport ritagliato con animazione WPF solo per i titoli troppo larghi.
Build Release e pubblicazione Windows x64 riuscite, warning trattati come errori.
Questo aggiornamento riguarda solo la presentazione; il nucleo resta quello
coperto dalle 153 verifiche riportate sotto. Animazione e interazione WPF
non avviate su Linux: prova visiva Windows elencata nella checklist.

# Aggiornamento 0.7.2

Build Windows x64 compilata su Linux, con warning trattati come errori.
153 controlli superati: 68 sul nucleo/presentazione e 85 di integrazione simulata.
Il nucleo verifica anche roundtrip delle griglie indipendenti, validazione Setup
e compatibilità con i progetti precedenti. L’integrazione verifica il comando
Setup indipendente, persistenza di entrambe le griglie e SHOW LOCK su entrambe.
Il popup ritardato e il layout WPF con scorrimento richiedono verifica visiva
su Windows: non sono stati avviati in questo ambiente Linux.

I risultati seguenti descrivono la precedente base 0.7.0.

# Verifica della beta integrata 0.7.0

## Ambiente e limiti

Compilazione con .NET SDK 10.0.100 su Linux x64, destinazione Windows x64.
Non sono stati eseguiti avvio WPF, WASAPI, named pipe native Windows, driver MIDI,
Donner fisico o dot2. L'integrazione usa lo stesso coordinatore, codec, writer JSON
e moduli MIDI di produzione, sostituendo hardware e trasporto con simulatori.
Il protocollo viene realmente serializzato/deserializzato anche nei test simulati.

## Controlli eseguiti

**148 verifiche superate:** 66 sul nucleo/presentazione (incluso lo stress) e 82 di integrazione simulata.


- Compilazione Release: 0 errori e 0 warning.
- Pubblicazione single-file self-contained Windows x64, senza trimming,
  native library self-extraction e componenti/licenze incorporati.
- Nucleo audio: play, pausa/ripresa campione esatto, stop, 16 voci, clipping/NaN,
  coda SPSC limitata e ordinata su 200.000 comandi, PANIC con coda satura,
  sorgente in errore isolata, underrun e cursore congelato/ripristinato.
- Test deterministico: **zero allocazioni gestite in 1000 blocchi Render** dopo warm-up.
- Modalità: Hold, Protected, TogglePause, Stop mouse e default master 100%.
- Fade: durata indipendente dal volume dello slot; stop durante il fade in.
- PCM: head in RAM, confine con pagine disco, refill dopo salto e restart immediato.
- Waveform: picchi stereo offline e saturazione della sola visualizzazione.
- MIDI: Note On con velocity zero, channel/data encoding, vettori MSC GO/RESUME
  confrontati con gli esempi del manuale dot2, cue con cultura invariabile.
- JSON: roundtrip, validazione e mappature duplicate rifiutate.
- Integrazione: learn senza trigger, controller rimosso/ricollegato, Hold,
  Protected, Note On/Off, MSC-only e audio+MIDI indipendenti.
- Errore e blocco simulati della chiamata MIDI: audio e PANIC indipendenti.
- Due Gate sulla stessa nota: rilascio corretto dell'ultimo utilizzatore.
- Porta MIDI OUT occupata e routing MIDI modificato durante audio: continuità.
- Show lock centrale; master/trasporti restano utilizzabili.
- Progetti atomici e recovery; JSON corrotto non sostituisce il progetto corrente;
  ripristino senza autoplay; caricamento audio fallito non avvia una vecchia cache.
- Invii precedenti a PANIC rifiutati; vecchi GO non riprodotti in recupero.
- Stress software per 15 secondi, 16 voci: uscita finita, nessuna eccezione sorgente.

Il campionamento delle allocazioni sull'intero ciclo di stress comprende anche
harness e runtime ed è registrato come diagnostica. Il controllo rigoroso delle
allocazioni riguarda il test dedicato del metodo Render, non l'intero processo.
Un controllo preliminare sul ciclo intero non era riproducibile; per questo i due
obiettivi (continuità sotto carico e allocazioni del render) sono verificati separatamente.

Vedi `core-tests.txt`, `integration-tests.txt`, `build.txt`, `publish.txt` per
risultati e conteggi effettivi della build. Lo stress accelerato non equivale a
8 ore reali, a riproduzione da driver o a una misura della latenza pad→suono.

## Prima dell'uso live

Eseguire `TEST-COMPLETI.md` su Windows con hardware definitivo, inclusa la prova
di resistenza, tutti i codec previsti e verifica MSC sulla console. Il watchdog
nativo, l'hot-plug reale e il layout WPF sono compilati ma non convalidati qui.
La build è una candidata per bug hunting, non una garanzia di assenza di glitch.


## GUI 0.6.0

Intervento sui soli componenti di interfaccia, tema XAML, barra del titolo e
visualizzazione della posizione. Motori, protocollo e modello progetto invariati.

- Nuova compilazione WPF Release: 0 errori / 0 warning.
- Pubblicazione Windows x64 self-contained single-file.
- Verificata la presenza della risorsa compilata `ui/theme.baml` nell'assembly.
- La UI riutilizza pennelli congelati e geometria waveform; l'interpolazione del
  cursore resta solo visiva, limitata a 240 ms e si ferma se mancano aggiornamenti.
- Animazioni brevi di opacità nella UI, senza attese nei comandi di riproduzione.
- I 104 controlli sopra si riferiscono al motore invariato validato nella 0.5.0;
  non sono una verifica visiva o di interazione della nuova GUI.

Non eseguiti: avvio e rendering WPF su Windows, interazione con popup/slider,
DPI reali e prova grafica sotto carico. Usare la sezione GUI 0.6.0 della checklist.


## Correzioni 0.6.1

Eseguiti 48 controlli del nucleo e della presentazione, inclusi 9 nuovi controlli
sull'orologio UI: compensazione dell'età del timestamp motore, passaggio simultaneo
dei secondi, durate frazionarie, pausa, limite di estrapolazione, fine, retrigger e ore.
Lo stress opzionale e i 64 test di integrazione restano quelli precedentemente eseguiti;
non sono conteggiati come nuove prove di questa correzione.

Il timestamp usato è TickCount64 del rendering audio, condiviso fra processi Windows.
La previsione è limitata a 500 ms, disabilitata in pausa, errore audio e underrun.
I numeri sono ottenuti da un solo valore intero: trascorso + rimanente = totale
visualizzato. L'aggiornamento della waveform è indipendente dal polling dello stato.

Il cursore rappresenta la posizione del flusso prodotto dal motore, non una misura
hardware del suono già arrivato alle casse. Nessuna latenza fisica è stata misurata.
Layout, hit area e simboli vettoriali della nuova vista Serata compilati; verifica
visiva e interazione mouse/controller su Windows ancora necessarie.

## Verifica 0.7.0

Nuova esecuzione completa: **66 controlli core/presentazione** (incluso stress
software di 15 secondi) e **82 controlli di integrazione**, con hardware simulato.

Aggiunti: seek avanti/indietro, pausa campione esatto, cue da fermo, modalità
Protected, raccordo tra preview PCM e sorgente, PANIC con seek in coda,
assenza di allocazioni nel render dopo seek, conservazione della griglia JSON,
validazione delle celle, 20 colori distinti e roundtrip HSV di 4096 colori RGB.
L'integrazione verifica seek senza messaggi luci in SHOW LOCK, posizione in pausa,
nomi DeviceInfo leggibili, griglia protetta e persistita, seek scartato dopo PANIC,
budget insufficiente senza danni alla sorgente, PCM del seek su vero file di cache,
read-ahead al punto di arrivo e rilascio completo delle riserve RAM.

La ruota colore è una bitmap UI riutilizzata: nessun lavoro del selettore viene
eseguito nel callback audio. I seek in streaming preparano il PCM su Task worker;
il callback scambia riferimenti e legge esclusivamente memoria. Le preview
precedenti restano referenziate fino all'ack del callback, prima del rilascio del budget.
Un errore di preparazione viene mostrato come avviso di operazione, senza invalidare lo slot.

Non eseguiti qui: apertura/interazione WPF della ruota e dei menu, resa DPI,
uscita WASAPI fisica, hot-plug reale, latenza e prova continuativa di ore.
I risultati compilati e simulati non sostituiscono questi collaudi Windows.
