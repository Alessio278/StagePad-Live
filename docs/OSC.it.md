# OSC / MIDI / Audio - StagePad 0.12.0

Nelle impostazioni dello slot seleziona **Audio**, **MIDI** e/o **OSC** in **Azioni alla pressione**. Sono disponibili tutte e sette le combinazioni. Il pad MIDI associato e il tasto a schermo eseguono la stessa scelta.

Aggiungi fino a **16 comandi MIDI** e **16 comandi OSC** per slot. MIDI supporta Note (Impulso/Gate), MSC, Control Change e Program Change. Tutti i comandi MIDI usano la destinazione selezionata in Setup. Numeri programma e valori CC seguono l’intervallo 0-127 indicato nei campi.

Per ogni comando OSC inserisci host/IP, porta UDP e percorso, poi gli argomenti tipizzati: intero, decimale, testo ASCII, vero o falso. Esempio: host `127.0.0.1`, porta `8000`, percorso `/show/go`, argomento intero `1`. Ogni comando può avere un destinatario diverso. Vedi la [guida OSC](OSC.it.md).

Audio, MIDI e OSC vengono inviati indipendentemente. Un audio mancante non impedisce MIDI/OSC; un’uscita MIDI occupata non blocca audio o OSC. Ogni elenco viene inviato nell’ordine visualizzato; i tempi fra audio, MIDI e UDP dipendono anche dai ricevitori. Il rilascio chiude tutte le note MIDI Gate dello slot. STOP ALL annulla gli OSC ancora in coda e rilascia le note MIDI. La diagnostica in Settings conta i messaggi UDP trasmessi localmente: verifica la risposta sul software ricevente.

Selezioni, comandi, destinazioni e argomenti vengono salvati nel progetto e nel recupero. I progetti precedenti conservano il comportamento MIDI originale. Limiti: 16 argomenti per messaggio OSC e 6000 byte complessivi di configurazione comandi per slot.

OSC format reference: https://opensoundcontrol.stanford.edu/spec-1_0.html

UDP invia ogni comando una sola volta. I messaggi scaduti dopo un secondo in coda o in risoluzione DNS vengono scartati, senza ripetizioni dopo riconnessione. Un messaggio già trasmesso non può essere richiamato da STOP ALL. Il ricevitore deve accettare i tipi di argomento configurati, in particolare T/F.
