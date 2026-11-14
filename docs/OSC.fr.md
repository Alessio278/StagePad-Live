# StagePad Live - OSC

Activez OSC dans le slot et ajoutez hôte/IP, port UDP 1-65535 et chemin commençant par `/`. Exemple : `127.0.0.1`, `8000`, `/show/go`, argument entier `1`.

Types : int32, float32, chaîne ASCII, vrai et faux. Jusqu’à 16 arguments et 16 commandes par slot, avec taille limitée. Chaque commande peut viser un autre récepteur. Les chemins et types doivent correspondre.

Flèches pour l’ordre, délais pour les temps et test individuel immédiat au repos. Maximum 30 secondes par commande et 60 par liste. MIDI et OSC s’exécutent indépendamment.

Un marqueur peut envoyer une commande OSC immédiate par passage, y compris en boucle. Un saut ne déclenche pas les cues ignorés. STOP/STOP ALL annulent les séquences en attente ; une reconnexion ne rejoue rien.

UDP : le compteur confirme l’envoi local, pas la réception. Configurez pare-feu/réseau et testez le récepteur. Pas de découverte, accusé de réception ou garantie pour le matériel distant.

[Guide](README.fr.md)
