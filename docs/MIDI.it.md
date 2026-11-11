# MIDI con StagePad

In ⚙ Impostazioni > VOLUMI MIDI puoi assegnare due CC assoluti indipendenti al master e alla traccia attualmente in play, tramite canale/CC oppure LEARN. CC -1 disabilita una mappatura. Le assegnazioni sono salvate nello show; la traccia segue l’ultimo audio avviato ancora in riproduzione, anche se selezioni un altro slot.


Dalla 0.11.0 ogni slot ha tre selettori indipendenti Audio/MIDI/OSC e una lista di comandi MIDI. Sono disponibili Note, MSC, Control Change e Program Change. Vedi [azioni combinate e OSC](OSC.it.md).


**Italiano** · [English](MIDI.en.md) · [Español](MIDI.es.md) · [Français](MIDI.fr.md) · [Pagina iniziale](README.it.md)

## Collegamento

Collega un controller a una porta MIDI Windows e selezionalo in **MIDI IN**.
Scegli il dispositivo o software ricevente in **Destinazione · MIDI OUT**.
USB, interfacce DIN e porte virtuali installate separatamente sono utilizzabili
se disponibili come porte Windows compatibili. StagePad non installa porte
virtuali e non inoltra indiscriminatamente i messaggi ricevuti.

Ogni slot può attivare audio, MIDI o entrambi. Le due uscite sono indipendenti:
un audio mancante non impedisce il messaggio MIDI e un errore MIDI non ferma
automaticamente la riproduzione audio.

## Ingresso e Learn

- Accettati Note On/Off; Note On con velocity zero equivale a rilascio.
- Premi **LEARN**, poi il pad: associa canale/nota senza attivare audio o MIDI
  durante quella pressione di apprendimento.
- La stessa coppia canale/nota non può essere associata a due slot.
- Non implementati CC, Program Change, aftertouch, MIDI clock e feedback LED.
- Se si perde il rilascio scollegando un controller, Hold può continuare:
  usa Stop sullo slot o PANIC.

## Uscita Note

| Impostazione | Comportamento |
| --- | --- |
| Canale / nota / velocity | Canale 1–16, nota 0–127, velocity fissa 1–127 |
| Impulso | Note On seguito da Note Off automatico; durata predefinita 100 ms |
| Gate | Note On alla pressione; Note Off al rilascio, Stop dello slot o PANIC |

Configura separatamente la mappatura Note o MIDI Learn del ricevitore: la stessa
nota può avere significati diversi su dispositivi diversi. Con due gate sulla
stessa nota/canale, il rilascio finale avviene dopo l’ultimo gate mantenuto.

## MIDI Show Control (MSC)

Questa versione invia SysEx MSC **GO**, **STOP** e **RESUME**. Imposta Device ID
0–127 e command format **1 General Lighting**, **2 Moving Lights** o **127 All**
in modo coerente con il ricevitore.

I profili implementati hanno limiti precisi: **Predefinito / Default**,
**Exec.Page** ed **Exec Page (spazio)**. Gli ultimi due codificano executor e
pagina separati da punto o spazio. La pagina è fissata a **1**; GO normalizza la
cue a tre decimali e STOP/RESUME indirizzati inviano cue **0.000**. Il ricevitore
deve riconoscere esattamente questa codifica. Non si tratta di compatibilità
MSC universale: usa le Note o verifica il profilo MSC nel manuale del dispositivo.

Il significato di GO, STOP e RESUME dipende dal ricevitore: non presumere che
corrispondano ovunque a prossima cue o blackout. La pausa audio non invia
automaticamente MSC STOP; il MIDI segue i comandi dello slot.

## Test e recupero

**Test OUT** invia il messaggio selezionato senza audio: le Note usano un impulso
con rilascio automatico; MSC invia il comando reale configurato. Show Lock blocca
i test. **Test IN** osserva il controller; gli slot già mappati restano attivi.

“Porta aperta” conferma l’interfaccia Windows, non ricezione o esecuzione.
Non esiste un riscontro universale dal ricevitore. PANIC rilascia le Note note
al programma e ferma l’audio; non invia MSC STOP, blackout o reset universali.
I messaggi scaduti vengono scartati, non ripetuti al termine di un guasto.

La codifica MIDI e i profili esistenti non sono stati modificati dalla nuova
terminologia generale. Prova dispositivo, firmware e collegamenti reali prima
dello spettacolo.
