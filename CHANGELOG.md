# Changelog

Includes public releases and development builds. Version numbers before 1.1 retain their original format; some early build dates are unavailable.

## [1.4 - Slot launch protection](docs/releases/v1.4.md)

## 1.3 - Release candidate

- Adaptive resident PCM, offline sample peak correction and output clipping diagnostics.
- WASAPI stall recovery, owner-aware startup cleanup and clearer live playback position.
- [Release notes](docs/releases/v1.3.md).

## 1.2 - Unreleased

- Restored the creator credit below the header wordmark.

- Non-destructive track start and end from existing Markers, with full-track restore.
- Trim-aware playback, seeking, time displays and Show schema 4 persistence.
- Light gray waveform inside the Loop and dimmed excluded Audio in the waveform.
- Four localized README pages with clickable square previews and contextual screenshots.

## 1.1 - 2026-11-16

- Compact +MARKER label on marker pads.
- Slot action choices now use an aligned, symmetric grid: Audio/MIDI, OSC/Emergency, and a left-aligned + Marker row. Equal cell widths and uniform spacing replace automatic wrapping.
- Public versions, executable names and release tags use two numbers, such as 1.1 and v1.1.
- The updater supports both new two-part tags and previous three-part release names.
- Consolidates the preceding development builds; project schema stays 3.

## 1.1.1 - development build (2026-11-16)

- Slower wheel/trackpad scrolling across grids and dialogs, preserving high-resolution deltas.
- New MIDI/OSC commands preload editable defaults; changing defaults leaves existing commands unchanged. Legacy linked commands and marker triggers retain their effective destinations.
- Exclusive + Marker slot action for the selected playing track in Setup; external MIDI LEARN remains under Quick marker insertion.
- Contextual MIDI/OSC sequence controls and emergency hold option.
- Per-slot volume percentages; default volume for new slots and shows; explicit apply-to-all while tracks are stopped. Retriggering keeps each slot’s latest mouse/MIDI volume, saved in the show.
- New shows start with no selected slot actions. Loading audio enables Audio. Empty pads remain configurable in Setup and cannot trigger.
- Removed lighting-specific wording from the MIDI output instruction.

## 1.1.0 - development build (2026-11-15)

- Versioned StagePad-1.1.0.exe distribution; legacy update compatibility asset.
- Unified MIDI IN/OUT Connected status.
- Shared show destinations with linked/custom per-command targeting, cumulative wait timeline and duplicate-last command shortcuts.
- Configured-pad tint and stronger activation feedback; command-only player details and per-slot trigger counters.
- Optional continuous three-second emergency hold across mouse/keyboard/MIDI, with early-release/disconnect/STOP/panic cancellation and one activation per hold.
- Optional generic Note On LED feedback on a separate isolated MIDI output.
- Show schema 2 protects new settings from being silently ignored by 1.0; schema-1 import retained.

## 1.0.0 - 2026-11-13

First stable release of StagePad Live.

- Consolidated marker editing, MIDI/OSC cues, marker-to-marker loops and Setup-only marker capture LEARN.
- Integrated New/Open/Save/Save as, complete show management, background auto-save, failed-write recovery and standard AppData storage.
- An applied loop shows only slow red visual feedback from the first Play, suppressing all colored marker flashes while keeping marker MIDI/OSC output.
- Playing + Marker inserts without opening a window; loop wraps suppress boundary flashes; colored cues own the frame for seven pulses over 1.82 seconds before red playback feedback returns.
- Unified dark rounded checkbox template with mint checks, keyboard focus and disabled/indeterminate states.
- Loops start directly at their first marker; slow red feedback remains exclusive while a loop is applied. Explicit Apply/Remove controls replace the checkbox.
- Quick marker insertion commits immediately and refreshes the list with a loop applied; the new-marker time field follows the cursor.
- Marker feedback now produces seven fast synchronized frame/time flashes with a stronger halo, then restores the usual player frame.
- Fixed the cursor on an empty player and the light intersection of horizontal/vertical scrollbars.
- Normalized maintained long dash punctuation to ASCII hyphens; retained all four interface languages.
- Updated the four language guides, README and release documentation.
- Added Windows build, integration and WPF checks to the release workflow.

## 0.14.4 - development build

- Normal StagePad executable/storage; migration from isolated development data without replacing named shows. Simplified project browser, New show, Save/Save as and background auto-save for named/unnamed shows with atomic backup and bounded final save.

## 0.14.3 - development build

- Update window accessible in the development build; waveform source/revision validation, full-width layout and very-short-clip rendering; loop checkbox alignment and marker LEARN spacing.

## 0.14.2 - development build

- Dedicated MIDI LEARN key for inserting markers on selected playing audio in Setup; held-note suppression, Show Lock and Live-mode guards; saved show binding.

## 0.14.1 - development build

- Stronger 1.6-second marker feedback; compact player; loops between existing markers; MIDI/OSC output per marker and loop pass; bounded event queue outside the audio callback.

## 0.14.0 - development build

- Editable marker position, label and color; double-click marker seek; player frame and time feedback; show persistence and isolated evaluation build.

## 0.13.2 - beta

- update checks, asset source validation and documentation links aligned with Alessio278/StagePad-Live.

## 0.13.1 - beta

- readable volume pickup options with clear names, white wrapping text and inline help in all four languages. Audio/MIDI behavior unchanged.

## 0.13.0 - beta

- global-pause EMERGENCY slots with MIDI/OSC; PANIC retrigger and lost-input release fixes; soft takeover and relative encoders; command order/delays/tests; portable show/media packages; GitHub release updater with checksum, backup and rollback.

## 0.12.0 - beta

- dynamic automatic slot titles; independent master/current-playing-track CC assignments with LEARN; direct bounded shutdown, invisible-instance recovery and visible-window activation; portable Windows update package. Pinned NVorbis 0.10.5 for clean restore.

## 0.11.0 - beta

- independent Audio/MIDI/OSC actions; up to 16 MIDI and 16 OSC commands per slot; Note, MSC, CC and Program Change; UDP OSC destinations and typed arguments; multi-note Gate release and queued OSC cancellation on STOP ALL; legacy project compatibility.

## 0.10.5 - beta

- fixed 0.10.4 startup failure by cloning logo geometry before transformation; restored the working PNG window-icon path. Added Windows WPF asset startup check.

## 0.10.4 - beta

- shorter Pad channel label; simple hyphens in text; vector white S logo without outline, uniform proportions and directly sampled icon sizes.

## 0.10.3 - beta

- always-green Show badge; stable dark project list during delete confirmation; automatic last-show recovery with file association and recent edits.

## 0.10.2 - beta

- centered 1250×960 startup window; Show label before audio status; scrollable project list in normal windows; inline Rename, Duplicate and confirmed Delete.

## 0.10.1 - beta

- Larger white S on mint background, optimized multi-size icon and high-resolution PNG window icon. Slot editor opens at 650×950 pixels beside the pad, within the monitor work area.

## 0.10.0 - beta

### Italiano

- Vista Apri/Salva progetto interna, con elenco, nome e conferma di sovrascrittura senza dialoghi esterni.
- Cartella progetti configurabile in Settings, reset a `project` e recupero compatibile con la cartella `recovery`.
- Ritorno automatico al pad attivato fuori vista in Serata, ON/OFF salvato sul PC.
- Logo S su fondo verdino per app/eseguibile e LIVE verde più vicino a STAGEPAD.

### English

- In-app project open/save view, list, naming and inline overwrite confirmation.
- Configurable projects folder, default reset and backward-compatible recovery.
- Optional follow-trigger scrolling in Live, remembered on this PC.
- Mint-green S logo for app/executable and closer green LIVE badge.

### Español

- Vista interna para abrir/guardar proyectos, lista, nombre y confirmación de sobrescritura.
- Carpeta configurable, restablecimiento y recuperación compatible con datos anteriores.
- Desplazamiento opcional al pad activado en Directo, guardado en este PC.
- Logo S sobre verde menta e indicador LIVE verde junto a STAGEPAD.

### Français

- Vue interne d’ouverture/enregistrement, liste, nom et confirmation de remplacement.
- Dossier configurable, retour au dossier par défaut et récupération compatible.
- Défilement optionnel vers le pad déclenché en Direct, mémorisé sur ce PC.
- Logo S sur vert menthe et badge LIVE vert rapproché de STAGEPAD.

## 0.9.2 - beta

- Audio memory and Diagnostics moved to Settings; Default beside the color selector restores only the slot color.

## 0.9.1 - beta

- default slot settings window size is 700×1000 WPF units.

## 0.9.0 - beta

### Italiano

- Schermata Impostazioni dedicata con icona ingranaggio e scelta IT/EN/ES/FR.
- Cambio lingua immediato, salvato sul PC, senza riavviare l’audio.
- Selettore colore soltanto nella configurazione dei singoli slot.
- MIDI presentato in modo generale: controller e destinazioni compatibili,
  con codifica Note/MSC e profili esistenti conservati.
- README e guida MIDI in quattro lingue, istruzioni e struttura per GitHub.

### English

- Dedicated gear-icon Settings window with IT/EN/ES/FR selection.
- Immediate language switching, remembered on this PC without restarting audio.
- Color picker remains only in individual slot configuration.
- Generic MIDI terminology, preserving Note/MSC encoding and existing profiles.
- Four-language documentation and repository preparation.

### Español

- Ventana Ajustes con engranaje y selección IT/EN/ES/FR.
- Cambio inmediato de idioma, guardado en este PC, sin reiniciar el audio.
- Selector de color solo en la configuración de cada slot.
- Terminología MIDI general, conservando la codificación Note/MSC y los perfiles.
- Documentación en cuatro idiomas y preparación del repositorio.

### Français

- Fenêtre Paramètres avec engrenage et sélection IT/EN/ES/FR.
- Changement immédiat de langue, enregistré sur ce PC sans redémarrer l’audio.
- Sélecteur de couleur uniquement dans la configuration de chaque slot.
- Terminologie MIDI générale, codage Note/MSC et profils existants conservés.
- Documentation en quatre langues et préparation du dépôt.

## 0.8.0 - beta

- 1-100 active slots, independent Setup/Live grids up to 10×10.
- Hidden tail slots retain saved settings and audio paths.
- Engine, MIDI dispatch, seek, project persistence and PANIC extended to slot 100.
- Removed redundant grid headings; added discreet creator credit.
- Open-source MIT distribution without online license verification or activation.

## 0.7.7 - beta

- Reset button in slot settings, restoring defaults for that slot index while retaining the audio file and volume. Apply to slot commits the changes.

## 0.7.6 - beta

- horizontal trackpad handling through WM_MOUSEHWHEEL for the grid under the pointer, including DPI conversion and fractional deltas; dark rounded scrollbars integrated with the theme and orientation-specific track commands.

## 0.7.5 - beta

- restored native scrollbar dragging, track and commands. Live titles use clipped drawing and the UI clock, with end pauses, return and repeat; short titles remain still.

## 0.7.4 - beta

- corrected oversized cells using finite viewport dimensions and explicit uniform rows/columns; titles no longer enlarge the grid. Corrected horizontal scrollbar sizing; further title/scrollbar revisions follow in 0.7.5.

## 0.7.3 - beta

- initial automatic scrolling for overflowing Live titles, with clipping, end pauses and resize handling. Subsequent reports lead to the fixes in 0.7.4 and 0.7.5.

## 0.7.2 - beta

- subtle slot-color background tint in Setup and Live, retaining the solid color strip and white titles. Preserves independent grids and the waveform double-click hint from 0.7.1.

## 0.7.1 - beta

- delayed rounded dark tooltip below the waveform explaining double-click seeking. Independent project-persisted Setup/Live rows and columns protected by SHOW LOCK. Minimum readable cell sizes and scrolling when needed; Live remains a grid.

## 0.7.0 - beta

- friendly audio-output names; HSV color wheel, brightness, preview and 20 presets. Manual double-click seek while playing, paused or stopped, without MIDI output. Project-persisted configurable grid rows/columns. Streaming seek prepares PCM on a worker; PANIC cancels pending seeks.

## 0.6.1 - beta

- elapsed, total and remaining counters share one time reference with state-age compensation; waveform retrieval no longer blocks polling. Adaptive Live pad Play/Pause/Stop hit areas, white titles and reduced secondary detail in small windows.

## 0.6.0 - beta

- redesigned dark interface with rounded controls and clearer states. Setup sidebar for projects/audio/MIDI/memory/diagnostics; 16-pad Live view and compact waveform player with large time readouts. Audio/MIDI indicators and visually locked configuration under SHOW LOCK; engine behavior retained from 0.5.0.

## 0.5.0 - integrated beta

First integrated build: 16-slot polyphonic audio, MIDI IN/LEARN, OneShot/Hold/Protected/TogglePause, Note/MSC output, waveform/time displays, JSON projects and recovery. Includes SHOW LOCK, hot-plug/retry, isolated processes, recovery and logs. Automated tests use simulated hardware; real Windows validation remains required.

## 0.1.1 - audio/UI development build

separate large time readouts; 4×4 Live view with 16 visible pads, dynamic sizing and F11 fullscreen. 

## Initial audio prototype

Initial prototype: 16 audio slots, polyphony, Play/Pause/Stop, volumes, PANIC, selectable output and isolated loading.
