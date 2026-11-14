# StagePad Live - MIDI

Usa un controller Note On/Off e seleziona MIDI IN in Setup. LEARN dello slot consuma la pressione di apprendimento. Un tasto non può appartenere a più slot né coincidere con l’inserimento marker.

## Messaggi in uscita

- Note Pulse: Note Off automatico dopo la durata configurata.
- Note Gate: Note Off al rilascio; la perdita dell’ingresso rilascia le note tenute.
- Control Change: canale 1-16, controller/valore 0-127.
- Program Change: canale 1-16, programma 0-127, base zero.
- MSC: SysEx GO, STOP e RESUME implementati; configura device ID, command format, cue e indirizzamento secondo il receiver.

Fino a 16 messaggi per slot, tutti sulla porta MIDI OUT selezionata. Frecce per l’ordine; massimo 30 secondi per ritardo e 60 per lista. Test ora invia un singolo comando immediato a riproduzione ferma. Per altri software può servire una porta MIDI virtuale separata.

## Volumi e marker

Nelle impostazioni puoi assegnare CC separati a master e traccia in play, con fader assoluti, encoder relativi e anti-salto. La traccia controllata è l’ultimo audio avviato ancora in play; selezionare uno slot non reindirizza il fader. La stessa coppia canale/controller non può controllare entrambi.

LEARN inserimento marker usa un tasto Note On/Off libero e agisce solo in Setup sulla traccia selezionata in play. I marker possono inviare MIDI immediato Pulse/CC/Program/MSC, senza Gate né ripetizione dei cue saltati.

Non sono implementati clock MIDI e MIDI thru automatico. Nessun profilo hardware è certificato: verifica messaggi e receiver. Il reconnect non ripete cue precedenti.

[Guida](README.it.md)
