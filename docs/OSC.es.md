# StagePad Live - OSC

Activa OSC en el slot y añade host/IP, puerto UDP 1-65535 y ruta que empieza con `/`. Ejemplo: `127.0.0.1`, `8000`, `/show/go`, argumento entero `1`.

Tipos: int32, float32, cadena ASCII, verdadero y falso. Hasta 16 argumentos y 16 comandos por slot, con tamaño limitado. Cada comando puede usar otro receptor. Las rutas y tipos deben coincidir.

Flechas para orden, retrasos para tiempos y prueba individual inmediata en reposo. Máximo 30 segundos por comando y 60 por lista. MIDI y OSC se ejecutan independientemente.

Un marcador puede enviar un OSC inmediato por paso, también en bucles. Saltar no envía cues omitidos. STOP/STOP ALL cancelan secuencias pendientes; reconectar no repite comandos.

UDP: el contador confirma envío local, no recepción. Configura firewall/red y prueba el receptor. Sin descubrimiento, confirmación ni garantía sobre equipos remotos.

[Guía](README.es.md)
