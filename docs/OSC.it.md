# StagePad Live - OSC

Seleziona OSC nello slot, aggiungi un comando e indica host/IP, porta UDP 1-65535 e percorso che inizia con `/`. Esempio: `127.0.0.1`, porta `8000`, `/show/go`, argomento intero `1`.

Tipi: int32, float32, stringa ASCII, vero o falso. Fino a 16 argomenti e 16 comandi per slot, con dimensione dati limitata. Ogni comando può avere un receiver diverso. Indirizzi e tipi devono corrispondere al software ricevente.

Frecce per l’ordine, ritardi per la temporizzazione e Test ora per un singolo comando immediato a riproduzione ferma. Massimo 30 secondi per comando e 60 per lista. Le liste MIDI/OSC sono indipendenti; il sincronismo tra uscite dipende dai receiver.

Un marker invia un OSC immediato a ogni passaggio, anche nei loop. I salti non inviano i cue saltati. STOP/STOP ALL annullano sequenze in attesa. Il reconnect non ripete i cue.

UDP non conferma la ricezione: il contatore indica solo invio locale. Configura firewall/interfacce e verifica il receiver. Nessuna scoperta automatica, conferma o garanzia di sicurezza dell’attrezzatura remota.

[Guida](README.it.md)
