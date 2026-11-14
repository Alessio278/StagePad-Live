# StagePad Live - MIDI

Sélectionnez MIDI IN en Préparation et utilisez un contrôleur Note On/Off. LEARN consomme la pression d’apprentissage. Un bouton ne peut appartenir à plusieurs slots ni être réservé à la capture de marqueurs.

## Sortie

Note Pulse avec Note Off automatique ; Note Gate avec Off au relâchement ; CC et Program Change sur canal 1-16, valeurs 0-127. Programmes indexés à zéro. MSC comprend les formats GO/STOP/RESUME implémentés : adaptez ID, format, cue et adressage au récepteur.

Jusqu’à 16 messages par slot sur MIDI OUT sélectionné. Ordre par flèches ; délai maximal de 30 secondes par commande et 60 par liste. Test individuel immédiat au repos. Un port MIDI virtuel externe peut être nécessaire.

## Volumes et marqueurs

CC séparés master/piste en lecture, faders absolus, encodeurs relatifs et reprise progressive. La piste contrôlée est le dernier audio démarré encore en lecture. La sélection ne redirige pas le fader. La même paire canal/contrôleur ne peut contrôler les deux.

LEARN d’insertion affecte un bouton libre, actif uniquement en Préparation sur la piste sélectionnée en lecture. Les marqueurs envoient Pulse/CC/Program/MSC immédiatement, sans Gate ni reprise des cues ignorés.

Pas d’horloge MIDI ni MIDI thru automatique. Vérifiez messages et récepteurs ; aucun profil certifié. La reconnexion ne rejoue pas les cues.

[Guide](README.fr.md)
