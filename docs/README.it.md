<h1><img src="../StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

**Launcher audio e controller di cue MIDI/OSC open source per spettacoli dal vivo su Windows.**

[![Release](https://img.shields.io/github/v/release/Alessio278/StagePad-Live?label=release&color=239f88)](https://github.com/Alessio278/StagePad-Live/releases) [![Windows build](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../LICENSE)

[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4)](#requisiti-e-limiti) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Audio MIDI OSC](https://img.shields.io/badge/control-Audio%20%2B%20MIDI%20%2B%20OSC-239f88) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](../README.md) - [Italiano](README.it.md) - [Español](README.es.md) - [Français](README.fr.md)

## Indice

- [Presentazione](#presentazione)
- [Avvio rapido](#avvio-rapido)
- [Funzioni](#funzioni)
- [Marker e loop](#marker-e-loop)
- [Show e auto-save](#show-e-auto-save)
- [MIDI e OSC](#midi-e-osc)
- [Aggiornamenti](#aggiornamenti)
- [Requisiti e limiti](#requisiti-e-limiti)
- [Compilazione e test](#compilazione-e-test)
- [Contributi e licenza](#contributi-e-licenza)

- [Avviso Windows SmartScreen](#avviso-windows-smartscreen)

## Presentazione

Prepara tracce e comandi in **Setup**, poi attivali in **Serata** con mouse o controller MIDI. StagePad Live è pensato per concerti, teatro, prove e installazioni. Non richiede account, attivazione o verifica online della licenza.

Created by **Alessio Natalucci**. Il nome del software è **StagePad Live**; l’eseguibile portatile si chiama `StagePad.exe`.

## Avvio rapido

1. Scarica lo ZIP Windows dalle [Releases](https://github.com/Alessio278/StagePad-Live/releases), estrailo e avvia `StagePad.exe`.
2. Scegli il dispositivo audio in Setup, parti da un buffer di **40 ms** e usa il test audio.
3. Imposta 1-100 slot e griglie fino a **10 x 10**. Carica le tracce e attendi che siano pronte.
4. Configura azioni, modalità, titolo, colore e fade di ogni slot. **LEARN** assegna il pad MIDI senza attivarlo durante l’apprendimento.
5. Usa **Nuovo show**, **Apri**, **Salva** e **Salva con nome**. L’auto-save è attivo per impostazione predefinita.
6. Passa a Serata, attiva **SHOW LOCK** e usa **F11** per lo schermo intero. **STOP ALL** ferma l’audio e annulla le uscite in attesa.

L’ingranaggio apre le impostazioni. Lingue: italiano, inglese, spagnolo e francese, modificabili senza riavviare l’audio.

## Funzioni

| Area | Funzioni |
| --- | --- |
| Pad | 1-100 slot, griglie Setup/Serata indipendenti fino a 10 x 10 |
| Audio | Play, pausa, stop, retrigger, Hold, protezione e play/pausa |
| Mixer | Volume per slot e master, fade e budget RAM |
| Uscite | Audio, MIDI e OSC singolarmente o combinati |
| Player | Forma d’onda, salto con doppio clic, tempi sincronizzati |
| Marker | Posizione, etichetta, colore, trigger MIDI/OSC e loop |
| Show | Browser integrato, Nuovo/Apri/Salva/Salva con nome e gestione |
| Auto-save | Scrittura in background, nomi provvisori, backup e recupero |
| Live | Show Lock, slot emergenza e scorrimento automatico opzionale |
| Interfaccia | Quattro lingue, colori per slot, titoli scorrevoli e barre scure |

Uno slot **EMERGENZA** mette in pausa tutti gli audio preservandone le posizioni e può inviare MIDI/OSC. STOP ALL azzera anche le posizioni. Verifica sempre la risposta dei dispositivi esterni.

## Marker e loop

**+ Marker** inserisce subito un marker nella posizione attuale del cursore, anche con loop applicato. In **Gestisci marker**, il campo Posizione segue gli spostamenti del cursore quando non ha il focus; selezionando un marker esistente mostra inizialmente la sua posizione salvata. Da qui modifichi/elimini; il doppio clic salta al punto. Massimo **32 marker** per traccia, entro il limite dei dati.

Nella gestione scegli due **marker esistenti** e premi **Applica loop**; usa **Rimuovi loop** per eliminarlo. La fine deve seguire l’inizio di almeno 50 ms. Ferma lo slot per applicare/rimuovere il loop; rimuovilo prima di spostare/eliminare gli estremi. Play parte direttamente dal marker iniziale; la pausa conserva la posizione. Gli altri marker restano aggiungibili, modificabili ed eliminabili anche durante il loop. Il tratto è preparato in RAM entro il budget, con rampe di 5 ms sui bordi.

Ogni marker può inviare MIDI, OSC o entrambi a ogni passaggio, anche nel loop. Invio immediato e Note Off automatico. I salti manuali non inviano i cue saltati né quello esattamente alla destinazione.

Nel loop la cornice pulsa lentamente in rosso (ciclo di 1,6 secondi); senza segnali colorati dei marker. Fuori dai loop la cornice è rossa durante il play; un marker produce **7 lampeggi rapidi** di cornice e tempi nel suo colore: 150 ms acceso / 110 ms spento, **1,82 secondi** in totale, con alone più intenso. Con animazioni Windows disattivate il colore resta fisso.

**Impostazioni > Marker MIDI (Setup) > LEARN inserimento marker** assegna un tasto Note On/Off libero per inserire marker sulla traccia selezionata in play. Tenere premuto non crea duplicati. Disattivato in Serata, in pausa/stop/caricamento e con Show Lock. L’assegnazione è salvata nello show.

Durante il play, **+ Marker** inserisce senza aprire la gestione marker. Con loop applicato, dal primo Play e su tutti i marker resta soltanto il rosso lento, anche al ritorno. I comandi MIDI/OSC dei marker restano attivi. Fuori dai loop, durante il segnale colorato il rosso è sospeso per tutti i sette lampeggi, anche negli intervalli spenti, poi viene ripristinato.

## Show e auto-save

**Salva** aggiorna direttamente lo show corrente; **Salva con nome** apre il campo integrato. Apri con doppio clic o Invio sull’elenco. **Altre operazioni** mantiene Rinomina, Duplica, Elimina, Importa ed Esporta. Cambiando show con modifiche non salvate puoi salvare, scartare o annullare. Ferma audio e sequenze prima di aprire o creare un altro show.

Auto-save attivo e disattivabile nelle impostazioni. Dopo circa **1,5 secondi** senza modifiche salva in background, anche durante il play. Gli show senza nome ricevono un nome provvisorio univoco e compaiono nell’elenco. L’asterisco indica modifiche in attesa. Scritture atomiche, copia `.bak`, errore visibile e ritentativo. Alla chiusura viene tentato un salvataggio finale con attesa limitata; resta il recupero di sessione.

Dati: `%LOCALAPPDATA%\StagePad`. Progetti: `%LOCALAPPDATA%\StagePad\project`. La directory è personalizzabile; **Default** ripristina questo percorso. Cambiarla non sposta i file esistenti.

All’avvio viene ripristinato l’ultimo show senza ripartire con l’audio. I vecchi dati di sviluppo vengono recuperati senza sovrascrivere show omonimi. Gli originali in `backups\legacy-data` possono contenere audio ancora referenziati e devono restare disponibili.

Il JSON contiene percorsi audio. **Esporta show con audio** crea un `.stagepad.zip`; Importa crea uno show distinto, senza sovrascrivere. Pacchetti fino a 8 GiB di media originali.

## MIDI e OSC

Seleziona Audio, MIDI e/o OSC nello slot. Le uscite sono indipendenti: un audio mancante non blocca MIDI/OSC.

Fino a **16 comandi MIDI** e **16 OSC** per slot. MIDI: Note Pulse/Gate, Control Change, Program Change e i formati MSC GO/STOP/RESUME implementati. I pad usano Note On/Off; master e traccia in play hanno CC separati con LEARN, encoder relativi e soft takeover. Non sono implementati clock MIDI, MIDI thru e Gate dei marker.

OSC UDP con host/IP, porta, percorso e argomenti tipizzati. Configura separatamente i receiver e le porte MIDI virtuali. Il conteggio inviati non conferma la ricezione. Riordino, ritardi e test singolo sono nelle impostazioni slot. Il reconnect non ripete trigger precedenti.

[Guida MIDI](MIDI.it.md) - [Guida OSC](OSC.it.md).

## Aggiornamenti

**Impostazioni > Aggiornamenti** controlla le release stabili del repository. L’installazione richiede riproduzione ferma, verifica SHA-256 e prodotto/versione, mantiene un backup e tenta il ripristino se l’avvio fallisce. Puoi anche chiudere l’app e sostituire solo `StagePad.exe`, conservando i dati.

La build non è firmata: Windows può mostrare SmartScreen. Non vengono dichiarate certificazioni hardware o di sicurezza. [Guida aggiornamenti](UPDATE.it.md).

## Requisiti e limiti

- App desktop per **Windows 10/11 x64**. Nessuna build desktop macOS/Linux.
- Runtime .NET incluso nello ZIP Windows. Per compilare serve .NET SDK 10 indicato da `global.json`.
- WAV PCM/float e OGG Vorbis con decoder inclusi; MP3, FLAC, M4A/AAC e altri formati dipendono dai codec Media Foundation installati. Mono/stereo; niente DRM, multicanale o OGG Opus.
- Prepara gli audio prima dello show. 100 slot configurabili non garantiscono 100 stream simultanei su ogni PC.
- Il mix usa clipping di protezione, non un limiter musicale.
- Verifica driver, dispositivi, latenza, riconnessioni e durata con il tuo setup.

## Compilazione e test

Su Windows installa .NET SDK 10 x64 ed esegui:

```powershell
.\Build.ps1
```

Lo script verifica il repository, esegue test del nucleo e integrazione simulata, compila l’EXE x64, esegue i controlli **WPF nativi** e prepara ZIP/checksum. Un controllo richiesto fallito blocca il pacchetto.

```powershell
dotnet run --project StagePad.Tests -c Release
dotnet run --project StagePad.IntegrationTests -c Release
```

I test simulati funzionano anche su altre piattaforme SDK: l’app desktop resta Windows. La release GitHub viene pubblicata stabile solo dopo il build Windows riuscito. [Verifica](VERIFICA.md) - [Checklist Windows](TEST-COMPLETI.md) - [Pubblicazione](GITHUB-SETUP.md).

## Contributi e licenza

Sono benvenuti segnalazioni, traduzioni e contributi. [CONTRIBUTING](../CONTRIBUTING.md) - [SECURITY](../SECURITY.md) - [Changelog](../CHANGELOG.md).

Licenza [MIT](../LICENSE). Le licenze delle dipendenze sono in [licenses](../licenses/) e nelle impostazioni dell’app. Software e sorgenti sono liberamente distribuibili secondo le rispettive licenze.

## Avviso Windows SmartScreen

StagePad Live è attualmente distribuito senza un certificato di firma digitale. All’avvio dell’EXE scaricato, Windows può mostrare **“PC protetto da Windows”** e **“Editore sconosciuto”**. Questo avviso, da solo, non è un rilevamento di malware né una garanzia che il file sia sicuro.

Scarica le release soltanto dal [repository ufficiale](https://github.com/Alessio278/StagePad-Live/releases) e confronta lo SHA-256 del file con il `SHA256.txt` pubblicato. Un checksum coincidente conferma che il file corrisponde all’asset pubblicato; non sostituisce una scansione di sicurezza. Se ti fidi della provenienza e scegli di proseguire, Windows può offrire **Ulteriori informazioni > Esegui comunque**. Sui PC gestiti questa opzione può essere bloccata.
