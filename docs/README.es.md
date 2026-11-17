<h1><img src="../StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

**Lanzador de audio y controlador de cues MIDI/OSC para espectáculos en Windows.**

[![Release](https://img.shields.io/github/v/release/Alessio278/StagePad-Live?label=release&color=239f88)](https://github.com/Alessio278/StagePad-Live/releases) [![Windows build](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../LICENSE) ![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](../README.md) - [Italiano](README.it.md) - [Español](README.es.md) - [Français](README.fr.md)

Creado por **Alessio Natalucci**.

## Índice

- [Inicio rápido](#inicio-rápido)
- [Screenshots](#screenshots)
- [Funciones](#funciones)
- [Marcadores y bucles](#marcadores-y-bucles)
- [Shows y guardado](#shows-y-guardado)
- [MIDI y OSC](#midi-y-osc)
- [Compilación y pruebas](#compilación-y-pruebas)
- [Licencia y contribuciones](#licencia-y-contribuciones)
- [Windows SmartScreen](#windows-smartscreen)

## Inicio rápido

1. Descarga **StagePad-Live-1.1-Windows.zip** desde [Releases](https://github.com/Alessio278/StagePad-Live/releases), extráelo y ejecuta **StagePad-1.1.exe**. No requiere instalación.
2. En Setup elige el dispositivo de audio y los puertos MIDI. Empieza con un búfer de 40 ms y realiza la prueba de audio.
3. Carga las pistas, configura las acciones y asigna pads con LEARN. Selecciona el idioma desde el engranaje.
4. Guarda el show, cambia a Directo y activa SHOW LOCK. F11 abre la pantalla completa; STOP ALL detiene el audio y los comandos pendientes.

## Screenshots

StagePad Live 1.1 con un Show de demostración. Las conexiones a dispositivos y la actividad de los comandos son simuladas.

![Vista Live con Pads Audio y MIDI/OSC](screenshots/es/02-live.png)

<details>
<summary>Vista Setup</summary>

![Vista Setup](screenshots/es/01-setup.png)

</details>

[Ver los 13 screenshots](screenshots/es/README.md)

## Funciones

| Área | Detalles |
| --- | --- |
| Pads | 1-100 slots; rejillas independientes Setup y Directo, hasta 10 x 10 |
| Audio | One-shot, Hold, protegido y reproducción/pausa; fundidos; volumen por slot y máster |
| Acciones | Audio, MIDI y OSC combinables, emergencia con pausa global o + Marker exclusivo |
| Controles | MIDI LEARN, CC separados para máster/pista, encoders relativos y captura suave |
| Shows | Nuevo, Abrir, Guardar, Guardar como, renombrar, duplicar, eliminar, autoguardado y exportación con audio |
| Interfaz | Italiano, inglés, español y francés; pads de colores, títulos desplazables y forma de onda |

### Novedades de 1.1

- Los shows nuevos empiezan sin acciones seleccionadas. Cargar una pista activa Audio.
- Los nuevos comandos MIDI/OSC precargan valores editables. Cambiar los valores predeterminados no modifica comandos existentes.
- Los volúmenes muestran porcentajes y conservan el último valor de ratón/MIDI al volver a activar y guardar. El valor predeterminado se aplica a nuevos slots; aplicarlo a todos exige pistas detenidas.
- Los pads configurados tienen un color tenue que se intensifica al activarlos. Los slots solo MIDI/OSC muestran comandos y estado.
- La emergencia puede exigir tres segundos de pulsación continua. Usa pads momentáneos Note On/Off para que soltar cancele la espera.
- Desplazamiento más lento con rueda/trackpad, casillas alineadas y etiqueta compacta +MARKER.

## Marcadores y bucles

**+ Marker** en el reproductor añade en la posición del cursor sin abrir el editor durante la reproducción. **Gestionar marcadores** permite editar tiempo, etiqueta, color y triggers MIDI/OSC. Haz doble clic en la forma de onda o un marcador para saltar.

Un slot puede usar la acción exclusiva **+ Marker**. Otra tecla MIDI se asigna en **Ajustes > Inserción rápida de marcadores**. Ambos actúan sobre la pista seleccionada en reproducción, solo en Setup y sin SHOW LOCK.

Selecciona dos marcadores existentes y pulsa **Aplicar bucle**. La reproducción empieza en el primero; **Quitar bucle** elimina el intervalo. Detén la pista antes de cambiarlo. En el bucle el marco pulsa lentamente en rojo; fuera, cada marcador produce siete destellos de su color. Los comandos de los marcadores se envían en cada vuelta.

![Player con Markers de colores](screenshots/es/03-player-marker.png)

## Shows y guardado

Los shows se abren y guardan dentro de la app. El autoguardado está activado por defecto, trabaja en segundo plano y conserva el archivo anterior como `.bak`. Al iniciar restaura el último show sin reproducir audio.

La carpeta predeterminada es `%LOCALAPPDATA%\StagePad\project`, modificable en Ajustes. El JSON referencia las pistas; usa **Exportar show con audio** para transferir todo el show.

La 1.1 lee los esquemas 1, 2 y 3 y guarda con esquema 3. Conserva copias antes de actualizar: 1.0 no abre el esquema 3.

## MIDI y OSC

Cada slot admite hasta 16 comandos MIDI y 16 OSC, con orden y esperas. Las dos listas avanzan independientemente. MIDI admite Note, CC, Program Change y MSC; OSC usa UDP con host, puerto, ruta y argumentos editables. El contador de envíos no confirma la recepción.

[MIDI](MIDI.es.md) - [OSC](OSC.es.md) - [Update](UPDATE.es.md)

## Compilación y pruebas

Para compilar necesitas Windows 10/11 x64 y .NET SDK 10. La descarga portátil incluye el runtime. WAV y OGG usan decodificadores incluidos; otros formatos dependen de los códecs de Windows.

```powershell
.\Build.ps1
```

El script ejecuta pruebas core y de integración simulada, compila la app, comprueba WPF en Windows y crea el ZIP. Completa la [lista de pruebas](TESTING.md) con tu equipo.

## Licencia y contribuciones

StagePad Live se distribuye con [licencia MIT](../LICENSE). Los avisos de dependencias están en [licenses](../licenses/) y en Ajustes. Consulta [CONTRIBUTING.md](../CONTRIBUTING.md), [SECURITY.md](../SECURITY.md) y el [historial](../CHANGELOG.md).

## Windows SmartScreen

El ejecutable no está firmado, por lo que Windows puede mostrar “Windows protegió su PC” o “Editor desconocido”. Descarga desde Releases oficiales y compara SHA-256 con `SHA256.txt`. Si confías en el archivo, Windows puede ofrecer **Más información > Ejecutar de todas formas**; los equipos administrados pueden bloquear esta opción.
