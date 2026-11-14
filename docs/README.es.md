<h1><img src="../StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

**Lanzador de audio y controlador de cues MIDI/OSC de código abierto para espectáculos en Windows.**

[![Release](https://img.shields.io/github/v/release/Alessio278/StagePad-Live?label=release&color=239f88)](https://github.com/Alessio278/StagePad-Live/releases) [![Windows build](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../LICENSE)

[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4)](#requisitos-y-límites) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Audio MIDI OSC](https://img.shields.io/badge/control-Audio%20%2B%20MIDI%20%2B%20OSC-239f88) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](../README.md) - [Italiano](README.it.md) - [Español](README.es.md) - [Français](README.fr.md)

## Contenido

- [Presentación](#presentación)
- [Inicio rápido](#inicio-rápido)
- [Funciones](#funciones)
- [Marcadores y bucles](#marcadores-y-bucles)
- [Shows y guardado automático](#shows-y-guardado-automático)
- [MIDI y OSC](#midi-y-osc)
- [Actualizaciones](#actualizaciones)
- [Requisitos y límites](#requisitos-y-límites)
- [Compilación y pruebas](#compilación-y-pruebas)
- [Contribuir y licencia](#contribuir-y-licencia)

- [Aviso de Windows SmartScreen](#aviso-de-windows-smartscreen)

## Presentación

Prepara audio y comandos en **Preparación** y actívalos en **Directo** con ratón o controlador MIDI. Para conciertos, teatro, ensayos e instalaciones. No requiere cuenta, activación ni comprobación de licencia online.

Created by **Alessio Natalucci**. Producto: **StagePad Live**. Ejecutable portátil: `StagePad.exe`.

## Inicio rápido

1. Descarga y extrae el ZIP Windows de [Releases](https://github.com/Alessio278/StagePad-Live/releases). Ejecuta `StagePad.exe`.
2. Selecciona la salida de audio; empieza con **40 ms** y realiza la prueba.
3. Configura 1-100 slots y cuadrículas de hasta **10 x 10**. Carga las pistas y espera.
4. Ajusta acciones, modo, título, color y fades. **LEARN** asigna un pad sin dispararlo.
5. Usa Nuevo show, Abrir, Guardar o Guardar como. El guardado automático está activado.
6. Cambia a Directo y activa SHOW LOCK. **F11**: pantalla completa. STOP ALL detiene y cancela salidas pendientes.

El engranaje permite elegir italiano, inglés, español o francés sin reiniciar el audio.

## Funciones

| Área | Funciones |
| --- | --- |
| Pads | 1-100 slots y dos cuadrículas independientes hasta 10 x 10 |
| Audio | Play, pausa, stop, retrigger, Hold, modo protegido y alternar pausa |
| Mezcla | Volúmenes de slot y máster, fades y presupuesto RAM |
| Salidas | Audio, MIDI y OSC independientes o combinados |
| Reproductor | Forma de onda, salto con doble clic y tiempos sincronizados |
| Marcadores | Tiempo, etiqueta, color, MIDI/OSC y bucle |
| Shows | Navegador integrado, Nuevo/Abrir/Guardar, renombrar, duplicar, eliminar |
| Guardado | Automático en segundo plano, copia anterior y recuperación |
| Directo | Show Lock, emergencia y desplazamiento automático opcional |
| Interfaz | Cuatro idiomas, colores por slot y títulos desplazables |

Un slot de EMERGENCIA pausa todos los audios conservando posiciones y puede enviar MIDI/OSC. STOP ALL también restablece posiciones. Comprueba la respuesta de los receptores externos.

## Marcadores y bucles

**+ Marcador** inserta inmediatamente en la posición actual del cursor, también con un bucle aplicado. En **Gestionar marcadores**, el campo Posición sigue los movimientos del cursor si no tiene el foco; seleccionar un marcador existente muestra inicialmente su posición guardada. Doble clic para saltar. Máximo **32 marcadores** y datos limitados por pista.

Selecciona dos **marcadores existentes** para el bucle, con fin al menos 50 ms después del inicio. Usa **Aplicar bucle** / **Quitar bucle**, sin casilla. Detén el slot para aplicar o quitar el bucle; quítalo antes de mover/eliminar los extremos. Play comienza directamente en el marcador inicial; pausa conserva la posición. Los demás marcadores siguen siendo editables durante el bucle. Intervalo preparado en RAM dentro del presupuesto; rampas de 5 ms en los bordes.

Cada marcador puede enviar MIDI, OSC o ambos, también en cada vuelta. Envío inmediato y Note Off automático. Los saltos manuales no envían cues omitidos ni el de la posición de destino.

En el bucle, el marco rojo pulsa lentamente (ciclo de 1,6 segundos); sin destellos de colores. Fuera del bucle, el marco es rojo durante la reproducción; al pasar un marcador, marco y tiempos emiten **7 destellos rápidos** de su color: 150 ms encendido / 110 ms apagado, **1,82 segundos** en total, con un halo más intenso. Sin animaciones de Windows, color fijo.

LEARN de inserción en Ajustes permite usar un botón Note On/Off libre para crear marcadores en la pista seleccionada en reproducción. No actúa en Directo, pausa, stop, carga ni Show Lock. Mantener pulsado no crea duplicados. Asignación guardada en el show.

Durante la reproducción, **+ Marcador** inserta sin abrir el editor. Con un bucle aplicado, desde el primer Play y al cruzar cualquier marcador solo aparece el pulso rojo lento, también al reiniciar el bucle. Los comandos MIDI/OSC siguen activos. Fuera del bucle, el rojo se suspende durante toda la secuencia de siete destellos, incluso en los intervalos apagados, y vuelve al terminar.

## Shows y guardado automático

Guardar actualiza el show; Guardar como permite otra copia. Doble clic o Intro para abrir. **Más acciones** conserva renombrar, duplicar, eliminar, importar y exportar. Antes de cambiar con modificaciones pendientes puedes guardar, descartar o cancelar. Detén audio y secuencias antes de abrir o crear otro show.

Guardado automático activado y configurable: escribe en segundo plano tras unos **1,5 segundos** sin cambios, también durante reproducción. Los shows sin nombre reciben un nombre provisional único. El asterisco indica cambios pendientes. Escrituras atómicas, copia `.bak`, reintentos tras errores y guardado final con espera limitada al cerrar; se mantiene la recuperación.

Datos: `%LOCALAPPDATA%\StagePad`. Shows: `%LOCALAPPDATA%\StagePad\project`. Directorio configurable en Ajustes; Default restaura la ruta. Cambiarla no mueve archivos.

Al iniciar se recupera el último show sin arrancar audio. Migración de datos antiguos sin sobrescribir shows. `backups\legacy-data` puede contener audio referenciado: debe conservarse.

JSON referencia archivos de audio. Exportar con audio crea `.stagepad.zip`; Importar crea un show independiente. Hasta 8 GiB de medios originales.

## MIDI y OSC

Audio, MIDI y OSC se eligen por slot y se envían independientemente. Un audio ausente no bloquea otras salidas.

Hasta **16 comandos MIDI** y **16 OSC** por slot. Note Pulse/Gate, CC, Program Change y los formatos MSC implementados. Pads Note On/Off; volúmenes máster/pista mediante CC separados con LEARN, encoders relativos y soft takeover. Sin reloj MIDI, MIDI thru ni Gate por marcador.

OSC UDP con host, puerto, ruta y argumentos tipados. Puertos virtuales y receptores se configuran aparte. El contador de envíos no confirma recepción. Orden, retrasos y pruebas individuales en ajustes de slot. Una reconexión no repite comandos.

[Guía MIDI](MIDI.es.md) - [Guía OSC](OSC.es.md).

## Actualizaciones

Ajustes > Actualizaciones consulta releases estables. Instalación en reposo con comprobación SHA-256, producto/versión, copia y restauración si falla el arranque. También puedes cerrar y sustituir `StagePad.exe` sin perder datos.

Build sin firma: Windows puede mostrar SmartScreen. Sin certificaciones de hardware o seguridad declaradas. [Guía](UPDATE.es.md).

## Requisitos y límites

- Escritorio **Windows 10/11 x64**, sin builds macOS/Linux.
- Runtime incluido; para compilar, .NET SDK 10 de `global.json`.
- WAV y OGG Vorbis incluidos; otros formatos como MP3/FLAC/AAC dependen de Media Foundation. Mono/estéreo; sin DRM, multicanal u OGG Opus.
- Prepara el audio. 100 slots no garantizan 100 streams simultáneos.
- Clipping de protección, no limitador musical.
- Prueba drivers, latencia, reconexiones y duración con tus equipos.

## Compilación y pruebas

En Windows, instala .NET SDK 10 x64 y ejecuta:

```powershell
.\Build.ps1
```

Verifica el repositorio, ejecuta pruebas de núcleo e integración simulada, compila, comprueba **WPF nativo** y genera ZIP/checksums. Los fallos bloquean el paquete y la release estable.

[Verificación](VERIFICA.md) - [Checklist Windows](TEST-COMPLETI.md) - [Publicación](GITHUB-SETUP.md). Las pruebas simuladas en otras plataformas no implican soporte de escritorio.

## Contribuir y licencia

Se aceptan informes, traducciones y cambios. [CONTRIBUTING](../CONTRIBUTING.md), [SECURITY](../SECURITY.md), [Changelog](../CHANGELOG.md).

Licencia [MIT](../LICENSE). Avisos de dependencias en [licenses](../licenses/) y en la app. Software y código son distribuibles según sus licencias.

## Aviso de Windows SmartScreen

StagePad Live se distribuye actualmente sin certificado de firma digital. Al iniciar el EXE descargado, Windows puede mostrar **«Windows protegió su PC»** y **«Editor desconocido»**. Esta advertencia por sí sola no es una detección de malware ni garantiza que el archivo sea seguro.

Descarga las versiones solo del [repositorio oficial](https://github.com/Alessio278/StagePad-Live/releases) y compara el SHA-256 del archivo con el `SHA256.txt` publicado. Una coincidencia confirma que el archivo corresponde al publicado; no sustituye un análisis de seguridad. Si confías en su procedencia y decides continuar, Windows puede ofrecer **Más información > Ejecutar de todas formas**. En equipos administrados esta opción puede estar bloqueada.
