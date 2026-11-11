<img src="../StagePad.App/Assets/StagePad.png" width="72" alt="StagePad logo">

# StagePad

**Lanzador de audio y disparador MIDI/OSC de código abierto para Windows.**

[Italiano](README.it.md) · [English](../README.md) · **Español** · [Français](README.fr.md)

Windows x64 · MIT · 1–100 slots · Audio + MIDI + OSC · IT / EN / ES / FR

Prepara pistas y mensajes MIDI/OSC en **Preparación** y pasa a **Directo** para
activarlos desde una cuadrícula con el ratón o un controlador MIDI. StagePad
está pensado para música en vivo, teatro, ensayos, instalaciones sonoras y
flujos de trabajo MIDI compatibles.

Created by **Alessio Natalucci**.

[Descargar para Windows](https://github.com/Alessio278/StagePad-Live/releases) · [Guía MIDI](MIDI.es.md) · [Cambios](../CHANGELOG.md) · [Informar de un problema](https://github.com/Alessio278/StagePad-Live/issues)

La configuración del slot se abre junto a su pad: a la derecha si cabe, o a la izquierda. El tamaño inicial solicitado es **650×950 píxeles de pantalla**, reducido solo si el área disponible del monitor es menor. Las etiquetas y el desplazamiento se adaptan a ventanas estrechas y DPI altos.


La ventana principal se abre centrada a **1250×960 píxeles**, ajustándose a pantallas pequeñas. **Show: nombre del proyecto** precede al estado del audio. La lista se desplaza también en una ventana normal. Selecciona un proyecto para **Renombrar**, **Duplicar** o **Eliminar**. La eliminación requiere confirmación y conserva los archivos de audio. Renombrar y Duplicar no sobrescriben otros proyectos. SHOW LOCK protege estas acciones. Al eliminar el proyecto abierto, su configuración sigue en memoria y puede guardarse otra vez.


Al iniciar, StagePad restaura automáticamente el último show y los cambios recientes recuperados, manteniendo su archivo asociado. La etiqueta **Show** permanece verde. La lista conserva el fondo oscuro durante la confirmación de eliminación. Si falta el archivo original, se recupera la configuración y se muestra un aviso.

## Nombres automáticos, volúmenes MIDI y actualizaciones

Deja **Nombre** vacío en los ajustes del slot para seguir el nombre del archivo de audio sin extensión. Al cargar otra pista se actualiza el título. Escribe un título para mantenerlo fijo y bórralo para recuperar el nombre automático.

En **⚙ Ajustes → VOLÚMENES MIDI** asigna canal/CC separados al **máster** y al volumen de la **pista actual en reproducción**. Usa cada botón **LEARN** y mueve su fader/knob, o introduce valores y pulsa **Aplicar asignaciones MIDI**. Se admiten CC absolutos 0-127; `-1` desactiva la asignación. El mismo CC en canales distintos está permitido. Las asignaciones se guardan en el show.

Se controla el audio iniciado más recientemente que sigue reproduciéndose. Los botones solo MIDI/OSC y la selección de slots no cambian el destino del fader. Si la pista se detiene o pausa, se controla la siguiente más reciente aún en reproducción. Sin audio activo no se modifica ningún slot.

El cierre avisa directamente al supervisor y limita la espera de los módulos. Al abrir otra vez se activa una instancia visible; una instancia invisible bloqueada puede recuperarse, limitada a StagePad del mismo usuario y sesión de Windows.

**Actualización portátil:** cierra StagePad, sustituye solo `StagePad.exe` y vuelve a abrirlo. Se conservan proyectos, ajustes, asignaciones y recuperación. Consulta [las instrucciones](UPDATE.es.md).

## Acciones combinadas Audio, MIDI y OSC

En los ajustes del slot selecciona **Audio**, **MIDI** y/o **OSC** en **Acciones al pulsar**. Están disponibles las siete combinaciones. El pad MIDI asociado y el botón en pantalla ejecutan la misma selección.

Añade hasta **16 comandos MIDI** y **16 comandos OSC** por slot. MIDI admite Note (Pulso/Gate), MSC, Control Change y Program Change. Todos usan la salida MIDI seleccionada en Preparación; los campos CC y Program Change indican valores 0-127.

Cada comando OSC tiene host/IP, puerto UDP, ruta y argumentos: entero, decimal, texto ASCII, verdadero o falso. Ejemplo: `127.0.0.1`, puerto `8000`, ruta `/show/go`, entero `1`. Cada comando puede dirigirse a otro receptor. Consulta la [guía OSC](OSC.es.md).

Audio, MIDI y OSC son independientes: un audio ausente no impide los comandos y una salida MIDI bloqueada no detiene OSC ni audio. Las listas se envían en el orden visible; la sincronización entre medios depende de los receptores. Al soltar el pad se cierran todas las notas Gate. STOP ALL cancela OSC pendiente y libera notas MIDI. Los diagnósticos cuentan envíos UDP locales; comprueba la respuesta en el receptor.

Los proyectos y la recuperación guardan selecciones, comandos y destinos. Se mantiene el comportamiento de los proyectos anteriores. Límites: 16 argumentos por mensaje OSC y 6000 bytes de configuración de comandos por slot.

## Proyectos dentro de la aplicación

**Abrir / Guardar** muestra una vista interna, sin diálogo de selección de archivos.
Selecciona un proyecto para abrirlo o escribe un nombre y guarda. La confirmación
para sobrescribir se muestra en la misma vista. Actualizar lista muestra los
proyectos JSON; los archivos de recuperación automática no aparecen en la lista manual.

En **⚙ Ajustes → Proyectos**, introduce la ruta completa y pulsa **Aplicar carpeta**.
**Predeterminado** restaura `%LOCALAPPDATA%\StagePad\project` para el usuario de
Windows actual. Cambiar la carpeta no mueve los archivos existentes. La recuperación
usa ahora `project\last-project.json` y sigue leyendo los datos antiguos de
`recovery`, que se conservan. Mantén juntos los proyectos y sus archivos de audio.

En **⚙ Ajustes → Directo**, **Desplazamiento automático al pad activado** se puede
activar/desactivar; está activado por defecto y se recuerda en este PC. Un pad
recién activado fuera de la vista se muestra con el movimiento mínimo necesario,
horizontal o vertical. Los pads completamente visibles no mueven la cuadrícula.
Funciona también con MIDI, sin tomar el foco ni enviar comandos adicionales.
No actúa en Preparación ni sigue continuamente la reproducción.

El logo es la **S blanca más grande sobre un cuadrado verde menta redondeado**. LIVE verde
queda junto a STAGEPAD; el mismo símbolo se utiliza en los iconos de Windows.

## Funciones

- **1–100 slots activos**, una voz de audio por slot.
- Cuadrículas independientes de Preparación y Directo, hasta **10×10**, con
  celdas legibles y desplazamiento.
- Reproducción, pausa, parada, volumen, fundidos y master; modos de inicio/reinicio,
  Hold, reproducción protegida y reproducir/pausar/reanudar.
- MIDI IN Note On/Off con Learn; MIDI OUT con pulsos o gate y los formatos
  MSC GO/STOP/RESUME implementados.
- Forma de onda con salto mediante doble clic y tiempos transcurrido/total/restante.
- Bloqueo, Stop All, pantalla completa y títulos largos con desplazamiento.
- Colores por slot, fondos suaves y rueda de color dentro de **Configurar slot**.
- Italiano, inglés, español y francés desde **⚙ Ajustes**.
- Proyectos JSON con guardado y recuperación; el audio permanece en sus archivos.

Sin cuenta, activación ni comprobación de licencia en línea. La aplicación
funciona sin conexión después de descargarla. La primera compilación necesita
Internet para obtener las dependencias.

## Inicio rápido

1. Abre **Releases** y descarga `StagePad.exe` de los archivos de la versión.
2. Cierra cualquier versión anterior y ejecuta el archivo. La distribución para
   Windows x64 incluye el runtime: no necesitas instalar .NET para utilizarla.
3. En **Preparación**, elige la salida de audio, empieza con **40 ms**, pulsa
   **Aplicar** y utiliza **Prueba de audio · −24 dB**.
4. Elige el número de slots y **Filas / Columnas**. Carga las pistas y espera a LISTO.
5. En **Configurar**, ajusta nombre, color, acción, modo, fundidos, pad y MIDI OUT.
6. Selecciona MIDI IN / OUT. Pulsa **LEARN** y después el pad físico para asignarlo;
   esa pulsación no activa el slot.
7. Guarda el proyecto y pasa a **Directo**. **F11** activa pantalla completa y
   **Esc** permite salir. Activa el bloqueo para proteger la configuración.

El idioma inicial es italiano. Abre **⚙ → Lingua dell’interfaccia** y elige
**Español**. El cambio es inmediato, también en los editores abiertos, se guarda
en este PC y no reinicia el audio. Memoria de audio y Diagnóstico están en Ajustes,
con Abrir registros y Licencias; el presupuesto de RAM se guarda en el proyecto.
Los colores se configuran solo en cada slot.

**Restablecer** recupera los valores del editor; **Aplicar al slot** los guarda.
El audio cargado y el volumen se conservan. Reducir los slots oculta los últimos
sin borrar sus ajustes ni las rutas de audio.

**Predeterminado** junto al selector de color restablece solo el color del slot, con vista previa inmediata. Guarda con **Aplicar al slot**.

## Compatibilidad

WAV PCM/float y OGG Vorbis tienen decodificadores incluidos. MP3, FLAC, M4A/AAC y
otros formatos de Windows dependen de los códecs Media Foundation instalados.
Se admite audio mono y estéreo; no OGG Opus, DRM ni audio multicanal.

MIDI no está vinculado a una marca: utiliza controladores Note On/Off y destinos
que reconozcan los mensajes configurados. Para conectar otros programas puede
ser necesario instalar un puerto MIDI virtual. No se implementan CC, Program
Change, MIDI clock ni MIDI thru automático. Consulta los límites MSC en la
[guía MIDI](MIDI.es.md).

## Beta y límites

Los tests automáticos cubren el motor y la integración simulada. La interfaz WPF,
WASAPI, los dispositivos MIDI reales, las reconexiones y las sesiones largas
requieren pruebas en tu PC Windows. Cien slots no garantizan cien streams
simultáneos con cualquier equipo.

- El ejecutable no tiene firma digital; Windows puede mostrar avisos del editor.
- Prepara las pistas antes del espectáculo: la RAM y la caché de disco son limitadas.
- La mezcla aplica recorte de seguridad, no un limitador musical.
- PANIC detiene el audio y libera las notas MIDI registradas; no reinicia ni
  detiene universalmente los equipos externos.
- Los proyectos no incluyen el audio: conserva juntos el proyecto y sus archivos.
- Los diálogos de archivos y los errores del sistema siguen el idioma de Windows.

## Compilar y contribuir

Instala **.NET SDK 10 x64**, según `global.json`, y ejecuta `Build.ps1` o abre
`Compila.cmd` desde la carpeta principal. Tras los tests, encontrarás
`artifacts\win-x64\StagePad.exe`.

```powershell
dotnet run --project StagePad.Tests -c Release
dotnet run --project StagePad.IntegrationTests -c Release
```

`--stress` es una prueba diagnóstica adicional, no una certificación del equipo.
Se aceptan informes, traducciones y contribuciones: [CONTRIBUTING](../CONTRIBUTING.md).
Consulta [VERIFICA](VERIFICA.md) y [TEST-COMPLETI](TEST-COMPLETI.md) para las pruebas.

Licencia [MIT](../LICENSE). Avisos de terceros en [licenses](../licenses/) y en
el botón **Licencias** de la aplicación.

## Slots EMERGENCIA y secuencias

Activa **EMERGENCIA: pausar todas las pistas** en los ajustes de un slot. Su pad pausa todos los audios activos, incluidos los protegidos, conservando las posiciones y sin reproducir su propio archivo. Selecciona MIDI y/o OSC para enviar sus mensajes. Cancela las secuencias pendientes y libera las notas MIDI activas antes de enviar sus propios comandos. Funciona con ratón o pad MIDI y se guarda en el show. Usa Play para reanudar una pista; los pads conservan su modo normal. STOP ALL detiene y reinicia las posiciones. Comprueba la recepción en los dispositivos externos.

↑/↓ reordenan comandos; **Espera previa (ms)** añade retardos; **Probar ahora** transmite un comando con la reproducción detenida, sin retardo y con Pulse para notas MIDI. Las listas MIDI/OSC son independientes. Máximo: 30 segundos por comando y 60 por lista. Soltar cancela notas Gate futuras; STOP cancela los comandos futuros del slot. Usa retardo cero para emergencias inmediatas.

Los volúmenes master/pista admiten CC absolutos, tres formatos de encoder relativo, paso ajustable y soft takeover para evitar saltos. La pérdida de entrada libera pads pulsados; la reconexión no repite disparos.

**Exportar show con audio** crea `.stagepad.zip` con audio original; **Importar show** acepta una ruta completa o un paquete en la carpeta de proyectos. Importa con otro nombre sin sobrescribir. Detén audio/secuencias antes. Límite: 8 GiB de medios.

**Ajustes → Actualizaciones** consulta versiones estables de `Alessio278/StagePad-Live`. Instala solo en reposo, conserva datos, verifica SHA-256 y guarda la versión anterior. Si falla el arranque, restaura la copia anterior; también hay restauración manual. Requiere repositorio y versiones publicados con los archivos necesarios. Este paquete no publica versiones por sí solo. Sigue disponible la sustitución manual del EXE.


Las opciones son **Evitar saltos de volumen del master** y **Evitar saltos de volumen de la pista**. Con faders absolutos, el control comienza al alcanzar el volumen actual. La versión 0.13.1 añade etiquetas blancas legibles y ayuda debajo.
