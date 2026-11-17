<h1><img src="../StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

**Launcher audio e controller di cue MIDI/OSC per spettacoli dal vivo su Windows.**

[![Release](https://img.shields.io/github/v/release/Alessio278/StagePad-Live?label=release&color=239f88)](https://github.com/Alessio278/StagePad-Live/releases) [![Windows build](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../LICENSE) ![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](../README.md) - [Italiano](README.it.md) - [Español](README.es.md) - [Français](README.fr.md)

Creato da **Alessio Natalucci**.

## Indice

- [Avvio rapido](#avvio-rapido)
- [Screenshot](#screenshot)
- [Funzioni](#funzioni)
- [Marker e loop](#marker-e-loop)
- [Show e salvataggio](#show-e-salvataggio)
- [MIDI e OSC](#midi-e-osc)
- [Compilazione e test](#compilazione-e-test)
- [Licenza e contributi](#licenza-e-contributi)
- [Windows SmartScreen](#windows-smartscreen)

## Avvio rapido

1. Scarica **StagePad-Live-1.1-Windows.zip** da [Releases](https://github.com/Alessio278/StagePad-Live/releases), estrailo e avvia **StagePad-1.1.exe**. Non serve installazione.
2. In Setup scegli dispositivo audio e porte MIDI. Parti da un buffer di 40 ms ed esegui il test audio.
3. Carica le tracce, configura le azioni degli slot e assegna i pad con LEARN. La lingua si sceglie dall’icona ingranaggio.
4. Salva lo show, passa a Serata e attiva SHOW LOCK. F11 apre lo schermo intero; STOP ALL ferma audio e comandi in attesa.

## Screenshot

StagePad Live 1.1 con uno Show dimostrativo. Connessioni ai dispositivi e invio dei comandi sono simulati.

![Vista Live con Pad Audio e MIDI/OSC](screenshots/it/02-live.png)

<details>
<summary>Vista Setup</summary>

![Vista Setup](screenshots/it/01-setup.png)

</details>

[Vedi tutti i 13 screenshot](screenshots/it/README.md)

## Funzioni

| Area | Dettagli |
| --- | --- |
| Pad | 1-100 slot; griglie indipendenti Setup e Serata, fino a 10 x 10 |
| Audio | Modalità One-shot, Hold, protetta e play/pausa; fade; volumi slot e master |
| Azioni | Audio, MIDI e OSC combinabili, emergenza con pausa globale oppure + Marker esclusivo |
| Controlli | MIDI LEARN, CC separati per master/traccia, encoder relativi e anti-salto |
| Show | Nuovo, Apri, Salva, Salva come, rinomina, duplica, elimina, auto-save ed esportazione con audio |
| Interfaccia | Italiano, inglese, spagnolo e francese; pad colorati, titoli scorrevoli e forma d’onda |

### Novità della 1.1

- Gli show nuovi partono senza azioni selezionate. Caricare una traccia abilita Audio.
- I nuovi comandi MIDI/OSC caricano valori predefiniti modificabili. Cambiare i predefiniti non modifica i comandi esistenti.
- I volumi mostrano le percentuali e conservano l’ultimo valore mouse/MIDI alle pressioni successive e nel salvataggio. Il predefinito vale per i nuovi slot; applicarlo a tutti richiede tracce ferme.
- I pad configurati hanno una tinta soffusa e un riscontro più intenso all’attivazione. Gli slot solo MIDI/OSC mostrano comandi e stato di attivazione.
- L’emergenza può richiedere tre secondi di pressione continua. Per annullarla al rilascio usa pad MIDI momentanei Note On/Off.
- Scorrimento rotella/touchpad più lento, checkbox allineati ed etichetta compatta +MARKER.

## Marker e loop

**+ Marker** nel player aggiunge alla posizione del cursore. Durante la riproduzione inserisce senza aprire finestre. **Gestisci marker** modifica posizione, etichetta, colore e trigger MIDI/OSC. Doppio clic su forma d’onda o marker per saltare a quel punto.

Uno slot può avere l’azione esclusiva **+ Marker**; un tasto MIDI aggiuntivo si assegna in **Impostazioni > Inserimento rapido marker**. Entrambi agiscono sulla traccia selezionata in riproduzione, solo in Setup e con SHOW LOCK disattivato.

Scegli due marker esistenti e premi **Applica loop**. La riproduzione parte dal primo marker; **Rimuovi loop** elimina l’intervallo. Ferma la traccia prima di cambiare il loop. Nel loop la cornice pulsa lentamente in rosso; fuori dal loop i marker producono sette lampeggi del proprio colore. I comandi dei marker vengono inviati a ogni passaggio nel loop.

![Player con Marker colorati](screenshots/it/03-player-marker.png)

## Show e salvataggio

Gli show si aprono e salvano nell’app. L’auto-save è attivo per default, scrive in background e conserva il file precedente come `.bak`. All’avvio si ripristina l’ultimo show senza avviare l’audio.

La cartella predefinita è `%LOCALAPPDATA%\StagePad\project`, modificabile nelle impostazioni. Il JSON contiene riferimenti alle tracce: per trasferire uno show completo usa **Esporta show con audio**.

La 1.1 legge gli schemi 1, 2 e 3 e salva con schema 3. Conserva copie prima dell’aggiornamento: la versione 1.0 non apre lo schema 3.

## MIDI e OSC

Ogni slot supporta fino a 16 comandi MIDI e 16 OSC, con ordine e attese. Le due liste avanzano indipendentemente. MIDI supporta Note, CC, Program Change e MSC; OSC usa UDP con host, porta, percorso e argomenti modificabili. Il contatore degli invii non conferma la ricezione.

[MIDI](MIDI.it.md) - [OSC](OSC.it.md) - [Update](UPDATE.it.md)

## Compilazione e test

Per compilare servono Windows 10/11 x64 e .NET SDK 10. Il pacchetto portatile include il runtime. WAV e OGG usano decoder inclusi; gli altri formati dipendono dai codec Windows.

```powershell
.\Build.ps1
```

Lo script esegue test core e di integrazione simulata, compila l’app, verifica WPF su Windows e crea lo ZIP. Completa la [checklist di test](TESTING.md) con l’attrezzatura dello show.

## Licenza e contributi

StagePad Live è distribuito con [licenza MIT](../LICENSE). Le licenze delle dipendenze sono in [licenses](../licenses/) e nelle impostazioni. Per contribuire: [CONTRIBUTING.md](../CONTRIBUTING.md), [SECURITY.md](../SECURITY.md) e [changelog](../CHANGELOG.md).

## Windows SmartScreen

L’eseguibile non è firmato: Windows può mostrare “PC protetto da Windows” o “Editore sconosciuto”. Scarica dalla pagina Releases ufficiale e confronta SHA-256 con `SHA256.txt`. Se ritieni attendibile il file, Windows può offrire **Ulteriori informazioni > Esegui comunque**; sui PC gestiti l’opzione può essere bloccata.
