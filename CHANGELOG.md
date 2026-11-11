# Changelog

Development-build history, reconstructed from retained documentation and build delivery records.
Entries describe versions created during development; they do not imply a separate public GitHub release for every build.
Original release dates are omitted where they could not be verified. Reconstruction updated: 2026-11-11.

IT: storico delle build di sviluppo ricostruito dai documenti conservati e dalle consegne. Le voci non implicano pubblicazioni GitHub distinte. Non sono state inventate date, versioni mancanti o release per le sole fasi di progetto.

EN: each recoverable numbered build has its own entry. Unnumbered prototypes are kept separate.
ES: cada build numerada recuperable tiene su propia entrada; los prototipos sin número se indican aparte.
FR: chaque build numérotée retrouvée dispose de sa propre entrée ; les prototypes sans numéro restent séparés.

## 0.13.2 - beta

- IT: aggiornamenti, verifica origine degli asset e link della documentazione allineati al repository Alessio278/StagePad-Live.
- EN: update checks, asset source validation and documentation links aligned with Alessio278/StagePad-Live.
- ES: actualizaciones, validación de archivos y enlaces adaptados a Alessio278/StagePad-Live.
- FR: mises à jour, validation des fichiers et liens alignés sur Alessio278/StagePad-Live.

## 0.13.1 - beta

- IT: rese leggibili le due opzioni anti-salto volume nei Setting; nomi espliciti, testo bianco a capo e spiegazione sotto ogni opzione, nelle quattro lingue. Logica audio/MIDI invariata.
- EN: readable volume pickup options with clear names, white wrapping text and inline help in all four languages. Audio/MIDI behavior unchanged.
- ES: opciones antisaltos legibles, nombres claros, texto blanco y ayuda en cuatro idiomas.
- FR: options de reprise progressive lisibles, noms clairs, texte blanc et aide en quatre langues.

## 0.13.0 - beta

- IT: slot EMERGENZA per pausa globale e invio MIDI/OSC; fix trigger dopo PANIC e rilascio Hold/Gate alla perdita ingresso; soft takeover e encoder relativi; ordine/ritardi/test comandi; pacchetti show con audio; aggiornamento da release GitHub con checksum, backup e rollback.
- EN: global-pause EMERGENCY slots with MIDI/OSC; PANIC retrigger and lost-input release fixes; soft takeover and relative encoders; command order/delays/tests; portable show/media packages; GitHub release updater with checksum, backup and rollback.
- ES: slots EMERGENCIA; correcciones PANIC y desconexión; soft takeover y encoders; orden/retardos/pruebas; paquetes con audio; actualización con checksum y restauración.
- FR: slots URGENCE ; corrections PANIC et déconnexion ; reprise progressive et encodeurs ; ordre/délais/tests ; paquets audio ; mises à jour avec checksum et restauration.

## 0.12.0 - beta

- IT: nomi slot automatici dinamici e nomi personalizzati espliciti; master e traccia in play assegnabili a CC separati con LEARN; chiusura diretta e cleanup limitato dei processi; recupero istanze invisibili e riattivazione di quelle visibili; pacchetto Windows portatile per aggiornare solo l’EXE. Dipendenza NVorbis fissata a 0.10.5 per restore pulito.
- EN: dynamic automatic slot titles; independent master/current-playing-track CC assignments with LEARN; direct bounded shutdown, invisible-instance recovery and visible-window activation; portable Windows update package. Pinned NVorbis 0.10.5 for clean restore.
- ES: títulos automáticos dinámicos; controles CC independientes de máster/pista con LEARN; cierre limitado y recuperación de instancias invisibles; paquete Windows portátil. NVorbis fijado a 0.10.5.
- FR: titres automatiques dynamiques ; CC master/piste indépendants avec LEARN ; fermeture bornée et récupération des instances invisibles ; paquet Windows portable. NVorbis fixé à 0.10.5.

## 0.11.0 - beta

- IT: selettori Audio/MIDI/OSC indipendenti; fino a 16 comandi MIDI e 16 OSC per slot; Note, MSC, CC e Program Change; OSC UDP con destinazioni e argomenti tipizzati; rilascio di tutte le note Gate e annullamento OSC in coda con STOP ALL; compatibilità progetti precedenti.
- EN: independent Audio/MIDI/OSC actions; up to 16 MIDI and 16 OSC commands per slot; Note, MSC, CC and Program Change; UDP OSC destinations and typed arguments; multi-note Gate release and queued OSC cancellation on STOP ALL; legacy project compatibility.
- ES: acciones Audio/MIDI/OSC independientes; hasta 16 comandos de cada salida; Note, MSC, CC y Program Change; OSC UDP con argumentos; liberación Gate múltiple y cancelación OSC; proyectos anteriores compatibles.
- FR: actions Audio/MIDI/OSC indépendantes ; jusqu’à 16 commandes par sortie ; Note, MSC, CC et Program Change ; OSC UDP typé ; libération Gate multiple et annulation OSC ; compatibilité des anciens projets.

## 0.10.5 - beta

- IT: corretto blocco di avvio della 0.10.4: la geometria del logo viene clonata prima della trasformazione. Icona finestra caricata dal PNG ripulito, come nel percorso funzionante della 0.10.3. Aggiunto controllo WPF del logo nel build Windows.
- EN: fixed 0.10.4 startup failure by cloning logo geometry before transformation; restored the working PNG window-icon path. Added Windows WPF asset startup check.
- ES: corregido el fallo de inicio de 0.10.4 al clonar la geometría; icono de ventana PNG y comprobación de recursos WPF en Windows.
- FR: correction du démarrage de 0.10.4 par clonage de la géométrie ; icône PNG de fenêtre et contrôle des ressources WPF sous Windows.

## 0.10.4 - beta

- IT: etichetta Canale Pad abbreviata; trattini semplici nei testi; logo vettoriale con S bianca senza contorno, proporzioni uniformi e icone campionate direttamente per ciascuna dimensione.
- EN: shorter Pad channel label; simple hyphens in text; vector white S logo without outline, uniform proportions and directly sampled icon sizes.
- ES: etiqueta del canal del pad abreviada; guiones simples; logo vectorial con S blanca sin contorno e iconos generados por tamaño.
- FR: libellé du canal du pad raccourci ; tirets simples ; logo vectoriel avec S blanc sans contour et icônes générées par taille.

## 0.10.3 - beta

- IT: etichetta Show sempre verde accesa; sfondo e selezione dell’elenco progetti stabili durante la conferma di eliminazione; ripristino automatico ultimo show con nome, associazione al file e modifiche recuperate.
- EN: always-green Show badge; stable dark project list during delete confirmation; automatic last-show recovery with file association and recent edits.
- ES: etiqueta Show siempre verde; lista oscura estable al confirmar eliminación; restauración automática del último show con archivo asociado y cambios recientes.
- FR: libellé Show toujours vert ; liste sombre stable lors de la suppression ; restauration automatique du dernier show avec fichier associé et modifications récentes.

## 0.10.2 - beta

- IT: finestra iniziale 1250×960 pixel centrata; etichetta Show prima dello stato audio; elenco progetti scorrevole anche senza schermo intero; Rinomina, Duplica ed Elimina integrati, con conferma prima di eliminare.
- EN: centered 1250×960 startup window; Show label before audio status; scrollable project list in normal windows; inline Rename, Duplicate and confirmed Delete.
- ES: ventana inicial centrada de 1250×960 píxeles; etiqueta Show; lista visible sin pantalla completa; Renombrar, Duplicar y Eliminar con confirmación.
- FR: fenêtre initiale centrée de 1250×960 pixels ; libellé Show ; liste accessible sans plein écran ; Renommer, Dupliquer et Supprimer avec confirmation.

## 0.10.1 - beta

- IT: S bianca ingrandita su fondo verdino, icona multi-risoluzione ottimizzata e sorgente PNG ad alta risoluzione per le finestre Windows. Editor slot 650×950 pixel, posizionato accanto al pad con controllo dell’area schermo.
- EN: Larger white S on mint background, optimized multi-size icon and high-resolution PNG window icon. Slot editor opens at 650×950 pixels beside the pad, within the monitor work area.
- ES: S blanca más grande sobre fondo verde menta e iconos de mayor resolución. Editor de 650×950 píxeles junto al pad, dentro del área del monitor.
- FR: S blanc agrandi sur fond vert menthe et icônes mieux résolues. Éditeur 650×950 pixels à côté du pad, dans la zone du moniteur.

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

- IT: Memoria audio e Diagnostica spostate in Settings; Default accanto al selettore colore ripristina solo il colore dello slot.
- EN: Audio memory and Diagnostics moved to Settings; Default beside the color selector restores only the slot color.
- ES: Memoria de audio y Diagnóstico pasan a Ajustes; Predeterminado junto al selector restablece solo el color del slot.
- FR: Mémoire audio et Diagnostic déplacés dans Paramètres ; Par défaut à côté du sélecteur rétablit uniquement la couleur du slot.

## 0.9.1 - beta

- IT: finestra di configurazione slot predefinita 700×1000 unità WPF.
- EN: default slot settings window size is 700×1000 WPF units.
- ES: ventana de configuración del slot de 700×1000 unidades WPF por defecto.
- FR: taille par défaut de la fenêtre de configuration du slot : 700×1000 unités WPF.

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

- 1–100 active slots, independent Setup/Live grids up to 10×10.
- Hidden tail slots retain saved settings and audio paths.
- Engine, MIDI dispatch, seek, project persistence and PANIC extended to slot 100.
- Removed redundant grid headings; added discreet creator credit.
- Open-source MIT distribution without online license verification or activation.

## 0.7.7 - beta

- IT: tasto Reset nella configurazione dello slot; ricarica i valori predefiniti coerenti con l’indice dello slot. File audio e volume vengono conservati; Applica allo slot conferma le modifiche.
- EN: Reset button in slot settings, restoring defaults for that slot index while retaining the audio file and volume. Apply to slot commits the changes.
- ES: botón Reset en los ajustes del slot; restaura los valores predeterminados, conserva audio y volumen y requiere aplicar los cambios.
- FR: bouton Reset dans les réglages du slot ; rétablit les valeurs par défaut, conserve audio et volume et nécessite l’application des changements.

## 0.7.6 - beta

- IT: gestione dello scorrimento orizzontale del trackpad tramite WM_MOUSEHWHEEL sulla griglia sotto il puntatore, con conversione DPI e delta frazionari; barre scure arrotondate integrate nel tema, con pista e comandi distinti per orientamento.
- EN: horizontal trackpad handling through WM_MOUSEHWHEEL for the grid under the pointer, including DPI conversion and fractional deltas; dark rounded scrollbars integrated with the theme and orientation-specific track commands.
- ES: desplazamiento horizontal del trackpad sobre la cuadrícula apuntada, conversión DPI y deltas fraccionarios; barras oscuras redondeadas integradas en el tema.
- FR: défilement horizontal du pavé tactile sur la grille pointée, conversion DPI et deltas fractionnaires ; barres sombres arrondies intégrées au thème.

## 0.7.5 - beta

- IT: ripristinati trascinamento, pista e comandi nativi delle barre di scorrimento. Titoli Serata disegnati in un viewport ritagliato e mossi dal clock UI, con pause agli estremi, ritorno e ripetizione; titoli brevi fermi.
- EN: restored native scrollbar dragging, track and commands. Live titles use clipped drawing and the UI clock, with end pauses, return and repeat; short titles remain still.
- ES: restaurados arrastre, pista y comandos nativos de las barras; títulos largos con dibujo recortado, reloj de interfaz, pausas y repetición.
- FR: restauration du déplacement, de la piste et des commandes natives des barres ; titres longs dessinés dans une zone découpée, avec horloge UI, pauses et répétition.

## 0.7.4 - beta

- IT: corretta l’espansione anomala dei box: dimensioni finite dal viewport, righe e colonne esplicite uniformi e titoli che non allargano la griglia. Corretta la dimensione della barra orizzontale; ulteriori revisioni di titoli e barre seguono nella 0.7.5.
- EN: corrected oversized cells using finite viewport dimensions and explicit uniform rows/columns; titles no longer enlarge the grid. Corrected horizontal scrollbar sizing; further title/scrollbar revisions follow in 0.7.5.
- ES: corregidos los cuadros sobredimensionados mediante filas y columnas uniformes limitadas al área visible; corregido el tamaño de la barra horizontal.
- FR: correction des cases surdimensionnées par des lignes et colonnes uniformes limitées au viewport ; correction de la taille de la barre horizontale.

## 0.7.3 - beta

- IT: prima implementazione dello scorrimento automatico dei titoli troppo lunghi in Serata, con area ritagliata, pause agli estremi e gestione del ridimensionamento. Le segnalazioni successive portano alle correzioni 0.7.4 e 0.7.5.
- EN: initial automatic scrolling for overflowing Live titles, with clipping, end pauses and resize handling. Subsequent reports lead to the fixes in 0.7.4 and 0.7.5.
- ES: primera implementación del desplazamiento automático de títulos largos en Directo; las correcciones posteriores se documentan en 0.7.4 y 0.7.5.
- FR: première implémentation du défilement automatique des titres longs en Direct ; corrections ultérieures dans les versions 0.7.4 et 0.7.5.

## 0.7.2 - beta

- IT: leggero sfondo soffuso derivato dal colore personalizzato dello slot, in Setup e Serata; barra colore piena e titoli bianchi. Conservate griglie indipendenti e indicazione del doppio clic sulla forma d’onda della 0.7.1.
- EN: subtle slot-color background tint in Setup and Live, retaining the solid color strip and white titles. Preserves independent grids and the waveform double-click hint from 0.7.1.
- ES: fondo suave basado en el color del slot en Preparación y Directo, con títulos blancos; conserva cuadrículas independientes y ayuda de doble clic.
- FR: fond légèrement teinté selon la couleur du slot en Préparation et Direct, avec titres blancs ; conserve grilles indépendantes et aide du double-clic.

## 0.7.1 - beta

- IT: popup scuro arrotondato sotto la forma d’onda dopo una breve sosta del cursore, con indicazione del doppio clic per saltare alla posizione. Righe e colonne indipendenti per Setup e Serata, salvate nel progetto e protette da SHOW LOCK. Celle con dimensioni minime e scorrimento quando necessario; Serata resta una griglia.
- EN: delayed rounded dark tooltip below the waveform explaining double-click seeking. Independent project-persisted Setup/Live rows and columns protected by SHOW LOCK. Minimum readable cell sizes and scrolling when needed; Live remains a grid.
- ES: ayuda oscura bajo la forma de onda para el salto por doble clic; filas y columnas independientes guardadas en el proyecto; celdas legibles y desplazamiento.
- FR: aide sombre sous la forme d’onde pour le déplacement par double-clic ; lignes et colonnes indépendantes enregistrées dans le projet ; cases lisibles et défilement.

## 0.7.0 - beta

- IT: selezione uscita audio con nomi leggibili; ruota colore HSV, luminosità, anteprima e 20 colori rapidi. Seek manuale con doppio clic, in play, pausa o da fermo, senza messaggi MIDI. Righe e colonne configurabili e salvate nel progetto. Per audio in streaming, preparazione PCM sul worker e cambio riferimenti nel callback; PANIC annulla i seek pendenti.
- EN: friendly audio-output names; HSV color wheel, brightness, preview and 20 presets. Manual double-click seek while playing, paused or stopped, without MIDI output. Project-persisted configurable grid rows/columns. Streaming seek prepares PCM on a worker; PANIC cancels pending seeks.
- ES: nombres de salida legibles, rueda HSV y 20 colores; salto manual en la forma de onda sin MIDI; cuadrículas guardadas y preparación PCM en segundo plano.
- FR: noms de sortie lisibles, roue HSV et 20 couleurs ; déplacement manuel dans la forme d’onde sans MIDI ; grilles enregistrées et préparation PCM en arrière-plan.

## 0.6.1 - beta

- IT: contatori trascorso, totale e rimanente calcolati dallo stesso riferimento temporale, con compensazione dell’età dello stato ricevuto; lettura waveform separata dal polling. Controlli Play/Pausa/Stop dei pad Serata con area cliccabile adattiva; titoli bianchi e dettagli secondari ridotti nelle finestre piccole.
- EN: elapsed, total and remaining counters share one time reference with state-age compensation; waveform retrieval no longer blocks polling. Adaptive Live pad Play/Pause/Stop hit areas, white titles and reduced secondary detail in small windows.
- ES: tiempos sincronizados desde una referencia común; lectura de forma de onda separada; controles adaptativos y títulos blancos en Directo.
- FR: compteurs synchronisés sur une référence commune ; lecture de forme d’onde séparée ; commandes adaptatives et titres blancs en Direct.

## 0.6.0 - beta

- IT: ridisegno dell’interfaccia con tema scuro, controlli arrotondati e stati distinguibili. Sidebar Setup per progetti, audio, MIDI, memoria e diagnostica; Serata a 16 pad e player compatto con waveform e tempi grandi. Indicatori Audio/MIDI e blocco visivo della configurazione con SHOW LOCK; logica dei motori invariata rispetto alla 0.5.0.
- EN: redesigned dark interface with rounded controls and clearer states. Setup sidebar for projects/audio/MIDI/memory/diagnostics; 16-pad Live view and compact waveform player with large time readouts. Audio/MIDI indicators and visually locked configuration under SHOW LOCK; engine behavior retained from 0.5.0.
- ES: nueva interfaz oscura, barra lateral Setup, vista Directo de 16 pads, reproductor compacto y bloqueo visual de configuración con SHOW LOCK.
- FR: nouvelle interface sombre, barre latérale Setup, vue Direct de 16 pads, lecteur compact et verrouillage visuel de la configuration avec SHOW LOCK.

## 0.5.0 - integrated beta

- IT: prima build integrata documentata delle fasi funzionali 1-5: audio polifonico a 16 slot, MIDI IN con LEARN, modalità OneShot/Hold/Protected/TogglePause, uscita Note/MSC, waveform e tempi grandi, progetti JSON e ripristino. SHOW LOCK, hot-plug/retry, processi separati con recupero e log. Le prove automatiche usano hardware simulato; la build richiede collaudo Windows reale.
- EN: first documented integrated build of functional stages 1-5: 16-slot polyphonic audio, MIDI IN/LEARN, OneShot/Hold/Protected/TogglePause, Note/MSC output, waveform/time displays, JSON projects and recovery. Includes SHOW LOCK, hot-plug/retry, isolated processes, recovery and logs. Automated tests use simulated hardware; real Windows validation remains required.
- ES: primera build integrada documentada de las fases 1-5: audio de 16 slots, MIDI LEARN, cuatro modos, Note/MSC, waveform, proyectos, SHOW LOCK y recuperación de procesos.
- FR: première build intégrée documentée des phases 1-5 : audio 16 slots, MIDI LEARN, quatre modes, Note/MSC, forme d’onde, projets, SHOW LOCK et récupération des processus.

## 0.1.1 - audio/UI development build

- IT: tempi grandi separati; vista Serata 4×4 con 16 pad visibili, dimensionamento dinamico e schermo intero tramite F11. Questa consegna precede l’integrazione documentata della 0.5.0.
- EN: separate large time readouts; 4×4 Live view with 16 visible pads, dynamic sizing and F11 fullscreen. This delivery precedes the documented 0.5.0 integration.
- ES: tiempos grandes separados, vista Directo 4×4 con 16 pads, tamaño dinámico y pantalla completa con F11.
- FR: grands compteurs séparés, vue Direct 4×4 avec 16 pads, dimensionnement dynamique et plein écran F11.

## Initial audio prototype - Phase 1, version not recorded

- IT: prima consegna di collaudo documentata: 16 slot audio, polifonia, Play/Pausa/Stop, volumi, PANIC, uscita selezionabile e caricamento isolato. Non viene attribuito retroattivamente un numero di versione non verificabile.
- EN: first documented test delivery: 16 audio slots, polyphony, Play/Pause/Stop, volumes, PANIC, selectable output and isolated loading. No unverified version number is assigned retrospectively.
- ES: primera entrega de prueba documentada: 16 slots, polifonía, transporte, volúmenes, PANIC, salida seleccionable y carga aislada; número de versión no verificado.
- FR: première livraison de test documentée : 16 slots, polyphonie, transport, volumes, PANIC, sortie sélectionnable et chargement isolé ; numéro de version non vérifié.

## Historical reconstruction notes

- Primary retained technical record: [verification history](docs/VERIFICA.md), including the numbered 0.6.x and 0.7.x updates; the [Windows checklist](docs/TEST-COMPLETI.md) records their intended native validation.
- Early 0.7-series README notes and development delivery records also corroborate the independent grids, color tint, title scrolling and subsequent fixes. The earliest Phase 1 / 0.1.1 / 0.5.0 chronology is supported by the retained development conversation.
- Only recoverable build numbers are listed. Project stages, internal rebuilds, re-sends and test-log filenames are not treated as separate numbered releases without supporting delivery evidence.
- This is a reconstruction of development history, not a GitHub tag, commit or publication audit. No retrospective tags, commits or releases have been created.
- Compilation and simulated checks do not establish that a historical build was validated with physical Windows audio/MIDI hardware. Known user-reported regressions and their fixes remain visible in the numbered entries.
