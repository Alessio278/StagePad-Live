namespace StagePad.App;
internal static class DemoText
{
    internal static readonly string[] Languages=["it","en","es","fr"];
    private static readonly Dictionary<string,string[]> Text=new()
    {
        ["show"]=["Aurora - Show Demo","Aurora - Demo Show","Aurora - Show de demostración","Aurora - Show de démonstration"],
        ["ambient"]=["Tappeto ambient","Ambient bed","Base ambient","Nappe ambient"],
        ["guest"]=["Ingresso ospite","Guest entrance","Entrada del invitado","Entrée de l’invité"],
        ["percussion"]=["Percussioni","Percussion","Percusión","Percussions"],
        ["transition"]=["Transizione","Transition","Transición","Transition"],
        ["ending"]=["Finale","Ending","Final","Fin"],
        ["entrance"]=["Ingresso","Entrance","Entrada","Entrée"],
        ["opening"]=["Apertura","Opening","Apertura","Ouverture"],
        ["scene"]=["Scena","Scene","Escena","Scène"],
        ["voice"]=["Voce","Voice","Voz","Voix"],
        ["reverb"]=["Riverbero","Reverb","Reverberación","Réverbération"],
        ["change"]=["Cambio scena","Scene change","Cambio de escena","Changement de scène"],
        ["sequence"]=["Sequenza MIDI","MIDI sequence","Secuencia MIDI","Séquence MIDI"],
        ["ready"]=["Pronto per il prossimo Cue","Ready for the next Cue","Listo para el siguiente Cue","Prêt pour le prochain Cue"],
        ["emergency"]=["EMERGENZA","EMERGENCY","EMERGENCIA","URGENCE"],
        ["rehearsal"]=["Prove - Set acustico","Rehearsal - Acoustic set","Ensayo - Set acústico","Répétition - Set acoustique"],
        ["electronic"]=["Live - Set elettronico","Live - Electronic set","Live - Set electrónico","Live - Set électronique"],
        ["simulated"]=["simulato","simulated","simulado","simulé"],
        ["audioReady"]=["Audio pronto","Audio ready","Audio listo","Audio prêt"],
        ["title"]=["StagePad Live - Screenshot dimostrativi","StagePad Live - Demo screenshots","StagePad Live - Screenshots de demostración","StagePad Live - Screenshots de démonstration"],
        ["notice"]=["Finestre originali con dati dimostrativi. Nessun dispositivo collegato e nessun Audio riprodotto. Le immagini mostrano l’area interna delle finestre, senza la barra del sistema operativo.","Original windows with demo data. No devices connected and no Audio played. Images show the window content without the operating system title bar.","Ventanas originales con datos de demostración. No hay dispositivos conectados ni reproducción de Audio. Las imágenes muestran el contenido de las ventanas sin la barra del sistema operativo.","Fenêtres originales avec des données de démonstration. Aucun appareil connecté et aucune lecture Audio. Les images montrent le contenu des fenêtres sans la barre du système d’exploitation."],
        ["readme"]=["13 screenshot in italiano: interfaccia, nomi delle tracce, Show, etichette dei Marker e descrizioni. I termini tecnici universali restano in inglese. Apri index.html per la galleria.","13 screenshots in English: interface, track names, Shows, Marker labels and descriptions. Universal technical terms remain in English. Open index.html for the gallery.","13 screenshots en español: interfaz, nombres de pistas, Shows, etiquetas de Marker y descripciones. Los términos técnicos universales se mantienen en inglés. Abre index.html para ver la galería.","13 screenshots en français : interface, noms des pistes, Shows, libellés des Marker et descriptions. Les termes techniques universels restent en anglais. Ouvrez index.html pour consulter la galerie."],
        ["01-setup.png"]=["Setup - tracce, comandi e colori","Setup - tracks, commands and colors","Setup - pistas, comandos y colores","Setup - pistes, commandes et couleurs"],
        ["02-live.png"]=["Live - Pad configurati e riscontro dei comandi","Live - configured Pads and command feedback","Live - Pads configurados y respuesta de los comandos","Live - Pads configurés et retour des commandes"],
        ["03-player-marker.png"]=["Player - forma d’onda e Marker colorati","Player - waveform and colored Markers","Player - forma de onda y Markers de colores","Player - forme d’onde et Markers colorés"],
        ["04-player-loop.png"]=["Player - Loop tra due Marker","Player - Loop between two Markers","Player - Loop entre dos Markers","Player - Loop entre deux Markers"],
        ["05-markers.png"]=["Marker - etichetta, colore e Loop","Markers - label, color and Loop","Markers - etiqueta, color y Loop","Markers - libellé, couleur et Loop"],
        ["06-slot-audio.png"]=["Slot Audio - configurazione della traccia","Audio Slot - track configuration","Slot Audio - configuración de la pista","Slot Audio - configuration de la piste"],
        ["07-slot-midi.png"]=["Slot MIDI - sequenza e attese","MIDI Slot - sequence and delays","Slot MIDI - secuencia y esperas","Slot MIDI - séquence et délais"],
        ["08-slot-osc.png"]=["Slot OSC - comandi e attese","OSC Slot - commands and delays","Slot OSC - comandos y esperas","Slot OSC - commandes et délais"],
        ["09-emergency.png"]=["Emergenza - protezione con pressione di tre secondi","Emergency - three-second hold protection","Emergencia - protección con pulsación de tres segundos","Urgence - protection par maintien de trois secondes"],
        ["10-slot-marker.png"]=["Slot + Marker - azione esclusiva","+ Marker Slot - exclusive action","Slot + Marker - acción exclusiva","Slot + Marker - action exclusive"],
        ["11-settings.png"]=["Settings - lingua e destinazioni predefinite","Settings - language and default destinations","Settings - idioma y destinos predeterminados","Settings - langue et destinations par défaut"],
        ["12-midi-volumes.png"]=["Volumi MIDI - Master e traccia con LEARN","MIDI volumes - Master and track with LEARN","Volúmenes MIDI - Master y pista con LEARN","Volumes MIDI - Master et piste avec LEARN"],
        ["13-shows.png"]=["Show - elenco dei progetti dimostrativi","Shows - demo project list","Shows - lista de proyectos de demostración","Shows - liste des projets de démonstration"]
    };
    internal static string Get(string language,string key)
    {
        int index=Array.IndexOf(Languages,language);
        if(index<0)throw new ArgumentException("Unsupported language.",nameof(language));
        return Text[key][index];
    }
}
