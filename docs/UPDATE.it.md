# Aggiornamenti portatili

Chiudi StagePad Live prima di sostituire l’eseguibile. Lo ZIP Windows include il runtime; show, impostazioni e tracce restano nelle cartelle già utilizzate.

**Impostazioni > Aggiornamenti** controlla le release stabili di `Alessio278/StagePad-Live`. Verifica versione e SHA-256 dell’eseguibile, conserva una copia precedente e tenta il ripristino se l’avvio fallisce. Prima dell’installazione ferma audio e sequenze. L’aggiornamento mantiene il nome del file installato.

Conserva copie di show e audio prima di aggiornare. La 1.1 salva con schema 3, non leggibile dalla 1.0. Esporta lo show con audio per trasferire tutte le tracce: il solo JSON contiene riferimenti ai file.

L’eseguibile non è firmato e può attivare SmartScreen: vedi il [README](README.it.md#windows-smartscreen). L’uso ordinario funziona offline; controllo e download degli aggiornamenti richiedono internet.
