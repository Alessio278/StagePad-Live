# MIDI avec StagePad

Dans ⚙ Paramètres > VOLUMES MIDI, affectez des CC absolus indépendants au master et à la piste en lecture via canal/CC ou LEARN. CC -1 désactive une affectation. Elles sont enregistrées dans le show ; la piste suit l’audio le plus récent encore en lecture, indépendamment du slot sélectionné.


Depuis 0.11.0 chaque slot possède des sélecteurs Audio/MIDI/OSC indépendants et une liste MIDI : Note, MSC, Control Change et Program Change. Consultez [actions combinées et OSC](OSC.fr.md).


[Italiano](MIDI.it.md) · [English](MIDI.en.md) · [Español](MIDI.es.md) · **Français** · [Accueil](README.fr.md)

## Connexion

Reliez un contrôleur à une entrée MIDI Windows et sélectionnez-le dans **MIDI IN**.
Choisissez le périphérique ou logiciel récepteur dans **Destination · MIDI OUT**.
USB, interfaces DIN et ports virtuels installés séparément sont utilisables
lorsqu’ils apparaissent comme ports Windows compatibles. StagePad n’installe pas
de port virtuel et ne transmet pas automatiquement tous les messages reçus.

Chaque slot peut déclencher audio, MIDI ou les deux. Les sorties sont indépendantes :
un fichier audio manquant n’empêche pas le message MIDI, et une erreur MIDI
n’arrête pas automatiquement l’audio.

## Entrée et Learn

- Accepte Note On/Off ; Note On avec vélocité zéro correspond au relâchement.
- Cliquez sur **LEARN**, puis appuyez sur le pad : le canal et la note sont associés
  sans déclencher audio ou MIDI pendant cet appui d’apprentissage.
- Une même paire canal/note ne peut pas être affectée à deux slots.
- CC, Program Change, aftertouch, MIDI clock et retour LED ne sont pas implémentés.
- Si un relâchement est perdu après déconnexion, Hold peut continuer : utilisez
  Stop sur le slot ou PANIC.

## Sortie Note

| Paramètre | Comportement |
| --- | --- |
| Canal / note / vélocité | Canal 1–16, note 0–127, vélocité fixe 1–127 |
| Impulsion | Note On puis Note Off automatique ; durée initiale 100 ms |
| Gate | Note On à l’appui ; Note Off au relâchement, Stop du slot ou PANIC |

Configurez séparément l’affectation des notes ou MIDI Learn du récepteur. Une
même note peut avoir des effets différents selon le matériel. Si deux gates
partagent note/canal, le relâchement final attend le dernier gate maintenu.

## MIDI Show Control (MSC)

Cette version envoie les SysEx MSC **GO**, **STOP** et **RESUME**. Réglez Device ID
0–127 et command format **1 General Lighting**, **2 Moving Lights** ou **127 All**
selon le récepteur.

Les profils implémentés ont des limites précises : **Par défaut**, **Exec.Page**
et **Exec Page (espace)**. Les deux derniers codent executor et page séparés par
un point ou un espace. La page reste **1** ; GO normalise la cue à trois décimales
et STOP/RESUME adressés envoient la cue **0.000**. Le récepteur doit reconnaître
exactement ce codage. L’interopérabilité MSC n’est pas universelle : utilisez
les notes ou vérifiez le profil dans le manuel du récepteur.

Le sens de GO, STOP et RESUME dépend du récepteur. Ne supposez pas qu’ils
signifient partout cue suivante ou blackout. La pause audio n’envoie pas
MSC STOP automatiquement ; MIDI suit les commandes du slot.

## Tests et récupération

**Tester OUT** envoie le message sélectionné sans audio : les notes utilisent une
impulsion avec relâchement automatique ; MSC envoie la commande réelle configurée.
Le verrou bloque ces tests. **Tester IN** observe le contrôleur ; les slots
associés restent actifs.

« Port ouvert » confirme l’interface Windows, pas la réception ou l’exécution.
Il n’existe pas d’accusé de réception universel. PANIC libère les notes suivies
et arrête l’audio ; il n’envoie pas de MSC STOP, blackout ou reset universel.
Les messages expirés sont ignorés plutôt que rejoués après une panne.

Le codage MIDI et les profils existants sont conservés avec la terminologie
générale. Testez périphérique, firmware et connexions réelles avant le spectacle.
