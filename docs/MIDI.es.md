# StagePad Live - MIDI

Selecciona MIDI IN en Preparación y usa un controlador Note On/Off. LEARN consume la pulsación de aprendizaje. Un botón no puede pertenecer a varios slots ni coincidir con la captura de marcadores.

## Salida

Note Pulse con Note Off automático; Note Gate con Off al soltar; CC y Program Change por canal 1-16 y valores 0-127. Los programas son base cero. MSC incluye los formatos GO/STOP/RESUME implementados: ajusta ID, formato, cue y dirección al receptor.

Hasta 16 mensajes por slot en el MIDI OUT seleccionado. Orden con flechas; retraso máximo 30 segundos por comando y 60 por lista. Prueba individual inmediata en reposo. Puede requerirse un puerto MIDI virtual externo.

## Volúmenes y marcadores

CC separados para máster y pista en reproducción, faders absolutos, encoders relativos y soft takeover. La pista controlada es el último audio iniciado que sigue reproduciéndose. La selección no redirige el fader. No se puede reutilizar la misma pareja canal/controlador para ambos volúmenes.

LEARN de inserción asigna un botón libre y actúa solo en Preparación sobre la pista seleccionada en reproducción. Los marcadores envían Pulse/CC/Program/MSC inmediato, sin Gate ni reproducción de cues omitidos.

Sin reloj MIDI o MIDI thru automático. Verifica mensajes y receptores; no hay perfiles certificados. La reconexión no repite cues.

[Guía](README.es.md)
