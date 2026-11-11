# MIDI con StagePad

En ⚙ Ajustes > VOLÚMENES MIDI asigna CC absolutos independientes a máster y pista en reproducción mediante canal/CC o LEARN. CC -1 desactiva la asignación. Se guardan en el show; el volumen de pista sigue el audio más reciente aún en reproducción, independientemente del slot seleccionado.


Desde 0.11.0 cada slot tiene selectores independientes Audio/MIDI/OSC y una lista de comandos MIDI: Note, MSC, Control Change y Program Change. Consulta [acciones combinadas y OSC](OSC.es.md).


[Italiano](MIDI.it.md) · [English](MIDI.en.md) · **Español** · [Français](MIDI.fr.md) · [Inicio](README.es.md)

## Conexión

Conecta un controlador a una entrada MIDI Windows y selecciónalo en **MIDI IN**.
Elige el dispositivo o software receptor en **Destino · MIDI OUT**. Se pueden
usar USB, interfaces DIN y puertos virtuales instalados por separado si aparecen
como puertos Windows compatibles. StagePad no instala puertos virtuales ni
reenvía automáticamente todos los mensajes recibidos.

Cada slot puede activar audio, MIDI o ambos. Las salidas son independientes:
un archivo de audio ausente no impide el mensaje MIDI y un error de MIDI no
detiene automáticamente el audio.

## Entrada y Learn

- Acepta Note On/Off; Note On con velocidad cero equivale a soltar.
- Pulsa **LEARN** y después el pad: asigna canal/nota sin activar audio o MIDI
  durante esa pulsación de aprendizaje.
- Una misma pareja canal/nota no puede asignarse a dos slots.
- No se implementan CC, Program Change, aftertouch, MIDI clock ni respuesta LED.
- Si se pierde la liberación al desconectar un controlador, Hold puede seguir
  sonando: usa Stop en el slot o PANIC.

## Salida Note

| Ajuste | Comportamiento |
| --- | --- |
| Canal / nota / velocidad | Canal 1–16, nota 0–127, velocidad fija 1–127 |
| Pulso | Note On seguido de Note Off automático; duración inicial 100 ms |
| Gate | Note On al pulsar; Note Off al soltar, detener el slot o usar PANIC |

Configura por separado la asignación de notas o MIDI Learn del receptor. Una
misma nota puede significar cosas distintas en equipos distintos. Si dos gates
comparten nota/canal, la liberación final espera al último gate mantenido.

## MIDI Show Control (MSC)

Esta versión envía SysEx MSC **GO**, **STOP** y **RESUME**. Ajusta Device ID 0–127
y command format **1 General Lighting**, **2 Moving Lights** o **127 All** según
el receptor.

Los perfiles implementados tienen límites concretos: **Predeterminado**,
**Exec.Page** y **Exec Page (espacio)**. Los dos últimos codifican executor y
página separados por punto o espacio. La página es siempre **1**; GO normaliza
la cue a tres decimales y STOP/RESUME direccionados envían la cue **0.000**.
El receptor debe reconocer exactamente esta codificación. No se garantiza
interoperabilidad MSC universal: usa notas o comprueba el perfil en su manual.

El significado de GO, STOP y RESUME depende del receptor. No presupongas que
significan siguiente cue o blackout en cualquier sistema. Pausar el audio no
envía MSC STOP automáticamente; MIDI sigue los comandos del slot.

## Pruebas y recuperación

**Probar OUT** envía el mensaje seleccionado sin audio. Las notas usan un pulso
con liberación automática; MSC envía el comando real configurado. El bloqueo
impide estas pruebas. **Probar IN** observa el controlador; los slots asignados
siguen activos.

“Puerto abierto” confirma la interfaz Windows, no la recepción o ejecución.
No hay confirmación universal del receptor. PANIC libera las notas registradas
y detiene el audio; no envía MSC STOP, blackout ni reset universales. Los mensajes
caducados se descartan, no se repiten después de un fallo.

La codificación MIDI y los perfiles existentes se conservan con la terminología
general. Prueba el dispositivo, firmware y conexiones reales antes del espectáculo.
