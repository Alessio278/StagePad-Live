# StagePad Live - aggiornamenti portatili

Chiudi l’app prima di sostituire `StagePad.exe` con l’eseguibile di una release stabile più recente. Impostazioni, show, recupero e riferimenti audio restano in `%LOCALAPPDATA%\StagePad` o nella directory scelta. Lo ZIP Windows include il runtime.

Impostazioni > Aggiornamenti controlla le release stabili di `Alessio278/StagePad-Live`. Servono l’asset `StagePad.exe` e SHA-256 corrispondente, oppure `SHA256.txt`. Audio/sequenze devono essere fermi. Prodotto/versione verificati, copia precedente conservata e tentativo di rollback se l’avvio fallisce. Ripristino precedente disponibile nelle impostazioni.

Prima di una versione principale conserva show e audio originali. L’esportazione dello show include i media; il solo JSON no. I dati di sviluppo vengono recuperati senza sovrascrivere show; gli audio archiviati in `backups\legacy-data` devono restare disponibili se referenziati.

Build non firmata. SmartScreen è separato dai controlli di integrità. Serve rete per verificare aggiornamenti, non per la riproduzione ordinaria.

[Guida](README.it.md)
