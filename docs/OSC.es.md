# OSC / MIDI / Audio - StagePad 0.12.0

En los ajustes del slot selecciona **Audio**, **MIDI** y/o **OSC** en **Acciones al pulsar**. Están disponibles las siete combinaciones. El pad MIDI asociado y el botón en pantalla ejecutan la misma selección.

Añade hasta **16 comandos MIDI** y **16 comandos OSC** por slot. MIDI admite Note (Pulso/Gate), MSC, Control Change y Program Change. Todos usan la salida MIDI seleccionada en Preparación; los campos CC y Program Change indican valores 0-127.

Cada comando OSC tiene host/IP, puerto UDP, ruta y argumentos: entero, decimal, texto ASCII, verdadero o falso. Ejemplo: `127.0.0.1`, puerto `8000`, ruta `/show/go`, entero `1`. Cada comando puede dirigirse a otro receptor. Consulta la [guía OSC](OSC.es.md).

Audio, MIDI y OSC son independientes: un audio ausente no impide los comandos y una salida MIDI bloqueada no detiene OSC ni audio. Las listas se envían en el orden visible; la sincronización entre medios depende de los receptores. Al soltar el pad se cierran todas las notas Gate. STOP ALL cancela OSC pendiente y libera notas MIDI. Los diagnósticos cuentan envíos UDP locales; comprueba la respuesta en el receptor.

Los proyectos y la recuperación guardan selecciones, comandos y destinos. Se mantiene el comportamiento de los proyectos anteriores. Límites: 16 argumentos por mensaje OSC y 6000 bytes de configuración de comandos por slot.

OSC format reference: https://opensoundcontrol.stanford.edu/spec-1_0.html

UDP envía cada comando una vez. Los mensajes con más de un segundo en cola/DNS se descartan y no se repiten al reconectar. STOP ALL no puede retirar mensajes ya enviados. El receptor debe aceptar los tipos de argumento seleccionados, especialmente T/F.
