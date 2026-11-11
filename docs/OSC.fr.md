# OSC / MIDI / Audio - StagePad 0.12.0

Dans les réglages du slot, sélectionnez **Audio**, **MIDI** et/ou **OSC** dans **Actions à l’appui**. Les sept combinaisons sont disponibles. Le pad MIDI associé et le bouton à l’écran exécutent la même sélection.

Ajoutez jusqu’à **16 commandes MIDI** et **16 commandes OSC** par slot. MIDI prend en charge Note (Impulsion/Gate), MSC, Control Change et Program Change. Toutes les commandes utilisent la sortie MIDI choisie dans Préparation ; les champs CC et Program Change indiquent des valeurs 0-127.

Chaque commande OSC possède un hôte/IP, un port UDP, une adresse et des arguments : entier, décimal, texte ASCII, vrai ou faux. Exemple : `127.0.0.1`, port `8000`, adresse `/show/go`, entier `1`. Chaque commande peut cibler un autre récepteur. Consultez le [guide OSC](OSC.fr.md).

Audio, MIDI et OSC sont indépendants : un fichier audio absent n’empêche pas les commandes et une sortie MIDI bloquée ne bloque ni OSC ni audio. Les listes sont envoyées dans l’ordre affiché ; la synchronisation entre supports dépend des récepteurs. Le relâchement ferme toutes les notes Gate. STOP ALL annule OSC en attente et libère les notes MIDI. Les diagnostics comptent les envois UDP locaux ; vérifiez la réaction du récepteur.

Projets et récupération conservent sélections, commandes et destinations. Les anciens projets gardent leur comportement. Limites : 16 arguments par message OSC et 6000 octets de configuration de commandes par slot.

OSC format reference: https://opensoundcontrol.stanford.edu/spec-1_0.html

UDP envoie chaque commande une fois. Les messages de plus d’une seconde en file/DNS sont abandonnés sans répétition à la reconnexion. STOP ALL ne peut rappeler un message déjà envoyé. Le récepteur doit accepter les types configurés, notamment T/F.
