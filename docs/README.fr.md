<img src="../StagePad.App/Assets/StagePad.png" width="72" alt="StagePad logo">

# StagePad

**Lanceur audio et déclencheur MIDI/OSC open source pour Windows.**

[Italiano](README.it.md) · [English](../README.md) · [Español](README.es.md) · **Français**

Windows x64 · MIT · 1–100 slots · Audio + MIDI + OSC · IT / EN / ES / FR

Préparez les pistes et messages MIDI/OSC en mode **Préparation**, puis passez en
mode **Direct** pour les déclencher à la souris ou avec un contrôleur MIDI.
StagePad s’adresse à la musique sur scène, au théâtre, aux répétitions, aux
installations sonores et aux usages MIDI compatibles.

Created by **Alessio Natalucci**.

[Télécharger pour Windows](https://github.com/Alessio278/StagePad-Live/releases) · [Guide MIDI](MIDI.fr.md) · [Historique](../CHANGELOG.md) · [Signaler un problème](https://github.com/Alessio278/StagePad-Live/issues)

La configuration du slot s’ouvre à côté du pad : à droite si l’espace le permet, sinon à gauche. La taille initiale demandée est **650×950 pixels écran**, réduite uniquement si la zone de travail du moniteur est plus petite. Les libellés et le défilement s’adaptent aux fenêtres étroites et aux DPI élevés.


La fenêtre principale s’ouvre centrée à **1250×960 pixels**, en s’adaptant aux petits écrans. **Show: nom du projet** précède l’état audio. La liste défile aussi dans une fenêtre normale. Sélectionnez un projet pour **Renommer**, **Dupliquer** ou **Supprimer**. La suppression demande confirmation et conserve les fichiers audio. Renommer et Dupliquer n’écrasent aucun autre projet. SHOW LOCK protège ces actions. Après suppression du projet ouvert, sa configuration reste en mémoire et peut être enregistrée à nouveau.


Au démarrage, StagePad restaure automatiquement le dernier show et les modifications récentes récupérées, en conservant le fichier associé. Le libellé **Show** reste vert. La liste conserve son fond sombre lors de la confirmation de suppression. Si le fichier original manque, la configuration récupérée reste disponible avec un avertissement.

## Noms automatiques, volumes MIDI et mises à jour

Laissez **Nom** vide dans les réglages du slot pour utiliser le nom du fichier audio sans extension. Charger une autre piste actualise le titre. Saisissez un titre pour le conserver ; effacez-le pour revenir au nom automatique.

Dans **⚙ Paramètres → VOLUMES MIDI**, affectez des canaux/CC indépendants au **master** et au volume de la **piste actuellement en lecture**. Utilisez chaque bouton **LEARN** puis déplacez son fader/knob, ou saisissez les valeurs et choisissez **Appliquer les affectations MIDI**. CC absolus 0-127 ; `-1` désactive l’affectation. Un même CC sur des canaux différents est accepté. Les affectations sont enregistrées dans le show.

La piste contrôlée est l’audio lancé le plus récemment et toujours en lecture. Les boutons MIDI/OSC seuls et la sélection de slots ne changent pas la cible. Si la piste s’arrête ou passe en pause, la suivante la plus récente encore en lecture est contrôlée. Sans audio en lecture, aucun slot n’est modifié.

La fermeture avertit directement le superviseur et limite l’attente des modules. Relancer active une instance visible ; une instance invisible bloquée peut être récupérée, uniquement pour StagePad du même utilisateur et de la même session Windows.

**Mise à jour portable :** fermez StagePad, remplacez seulement `StagePad.exe`, puis relancez-le. Projets, réglages, affectations et récupération sont conservés. Consultez [les instructions](UPDATE.fr.md).

## Actions combinées Audio, MIDI et OSC

Dans les réglages du slot, sélectionnez **Audio**, **MIDI** et/ou **OSC** dans **Actions à l’appui**. Les sept combinaisons sont disponibles. Le pad MIDI associé et le bouton à l’écran exécutent la même sélection.

Ajoutez jusqu’à **16 commandes MIDI** et **16 commandes OSC** par slot. MIDI prend en charge Note (Impulsion/Gate), MSC, Control Change et Program Change. Toutes les commandes utilisent la sortie MIDI choisie dans Préparation ; les champs CC et Program Change indiquent des valeurs 0-127.

Chaque commande OSC possède un hôte/IP, un port UDP, une adresse et des arguments : entier, décimal, texte ASCII, vrai ou faux. Exemple : `127.0.0.1`, port `8000`, adresse `/show/go`, entier `1`. Chaque commande peut cibler un autre récepteur. Consultez le [guide OSC](OSC.fr.md).

Audio, MIDI et OSC sont indépendants : un fichier audio absent n’empêche pas les commandes et une sortie MIDI bloquée ne bloque ni OSC ni audio. Les listes sont envoyées dans l’ordre affiché ; la synchronisation entre supports dépend des récepteurs. Le relâchement ferme toutes les notes Gate. STOP ALL annule OSC en attente et libère les notes MIDI. Les diagnostics comptent les envois UDP locaux ; vérifiez la réaction du récepteur.

Projets et récupération conservent sélections, commandes et destinations. Les anciens projets gardent leur comportement. Limites : 16 arguments par message OSC et 6000 octets de configuration de commandes par slot.

## Projets dans l’application

**Ouvrir / Enregistrer** affiche une vue interne, sans boîte de sélection de fichiers.
Sélectionnez un projet pour l’ouvrir ou saisissez un nom et enregistrez. La
confirmation de remplacement apparaît dans cette même vue. Actualiser la liste
montre les projets JSON ; les fichiers de récupération automatique sont exclus de la liste manuelle.

Dans **⚙ Paramètres → Projets**, saisissez le chemin complet puis cliquez sur
**Appliquer le dossier**. **Par défaut** rétablit `%LOCALAPPDATA%\StagePad\project`
pour l’utilisateur Windows actuel. Changer de dossier ne déplace pas les fichiers
existants. La récupération utilise désormais `project\last-project.json` et lit
toujours les anciens fichiers de `recovery`, qui sont conservés. Gardez ensemble
les projets et leurs fichiers audio.

Dans **⚙ Paramètres → Direct**, **Défilement automatique vers le pad déclenché**
s’active ou se désactive ; il est actif par défaut et mémorisé sur ce PC. Un pad
nouvellement déclenché hors champ est révélé avec le déplacement minimum,
horizontalement ou verticalement. Les pads entièrement visibles ne déplacent pas
la grille. Cela fonctionne aussi avec MIDI, sans prendre le focus ni envoyer de
commandes supplémentaires. Aucun effet en Préparation ni suivi continu de la lecture.

Le logo est le **S blanc agrandi sur un carré vert menthe arrondi**. LIVE vert se trouve
juste à côté de STAGEPAD ; le même symbole sert aux icônes Windows.

## Fonctions

- **1 à 100 slots actifs**, une voix audio par slot.
- Grilles indépendantes Préparation et Direct, jusqu’à **10×10**, avec cellules
  lisibles et défilement.
- Lecture, pause, arrêt, volume, fondus et master ; modes déclenchement/redémarrage,
  maintien, lecture protégée et lire/pause/reprendre.
- MIDI IN Note On/Off avec Learn ; MIDI OUT par impulsion ou gate et formats
  MSC GO/STOP/RESUME implémentés.
- Forme d’onde avec positionnement par double clic et durées écoulée/totale/restante.
- Verrou, Stop All, plein écran et défilement des titres longs.
- Couleurs par slot, fonds doux et roue chromatique dans **Configurer le slot**.
- Italien, anglais, espagnol et français depuis **⚙ Paramètres**.
- Projets JSON avec enregistrement et récupération ; fichiers audio séparés.

Aucun compte, activation ou contrôle de licence en ligne. L’application peut
fonctionner hors ligne après téléchargement. La première compilation nécessite
Internet pour récupérer les dépendances.

## Prise en main

1. Ouvrez **Releases** et téléchargez `StagePad.exe` parmi les fichiers de la version.
2. Fermez l’ancienne version éventuelle, puis lancez le fichier. La distribution
   Windows x64 contient le runtime : .NET n’est pas nécessaire pour l’utilisation.
3. En **Préparation**, choisissez la sortie audio, commencez à **40 ms**, cliquez
   sur **Appliquer** et utilisez **Test audio · −24 dB**.
4. Réglez le nombre de slots et les **Lignes / Colonnes**. Chargez les pistes et attendez PRÊT.
5. Dans **Configurer**, réglez nom, couleur, action, mode, fondus, pad et MIDI OUT.
6. Sélectionnez MIDI IN / OUT. Cliquez sur **LEARN**, puis appuyez sur le pad
   physique ; cet appui ne déclenche pas le slot.
7. Enregistrez le projet et passez en **Direct**. **F11** active le plein écran,
   **Esc** permet d’en sortir. Activez le verrou pour protéger les paramètres.

La langue initiale est l’italien. Ouvrez **⚙ → Lingua dell’interfaccia** et
choisissez **Français**. Le changement est immédiat, y compris dans les éditeurs
ouverts, est enregistré sur ce PC et ne redémarre pas l’audio. Les couleurs
restent configurables uniquement dans chaque slot. Mémoire audio et Diagnostic
sont dans Paramètres, avec Ouvrir les journaux et Licences ; le budget RAM reste
enregistré dans le projet.

**Réinitialiser** rétablit les valeurs de l’éditeur ; **Appliquer au slot** les
enregistre. L’audio chargé et le volume sont conservés. Réduire le nombre de slots
masque les derniers sans supprimer leurs réglages ni leurs chemins audio.

**Par défaut** à côté du sélecteur de couleur rétablit uniquement la couleur du slot, avec aperçu immédiat. Enregistrez avec **Appliquer au slot**.

## Compatibilité

WAV PCM/float et OGG Vorbis disposent de décodeurs inclus. MP3, FLAC, M4A/AAC et
les autres formats Windows dépendent des codecs Media Foundation installés.
L’audio mono et stéréo est accepté ; pas OGG Opus, DRM ou audio multicanal.

MIDI n’est pas lié à une marque : utilisez un contrôleur Note On/Off et un
destinataire qui reconnaît les messages configurés. Un port MIDI virtuel installé
séparément peut être nécessaire pour relier d’autres logiciels. CC, Program
Change, MIDI clock et MIDI thru automatique ne sont pas implémentés. Consultez
les limites MSC dans le [guide MIDI](MIDI.fr.md).

## Bêta et limites

Les tests automatiques couvrent le moteur et l’intégration simulée. L’interface
WPF, WASAPI, le matériel MIDI réel, les reconnexions et les longues sessions
doivent être testés sur votre PC Windows. Cent slots ne garantissent pas cent
flux simultanés sur n’importe quel matériel.

- L’exécutable n’est pas signé numériquement ; Windows peut afficher des avertissements.
- Préparez les pistes avant le spectacle : RAM et cache disque sont limités.
- Le mixage applique un écrêtage de sécurité, pas un limiteur musical.
- PANIC arrête l’audio et libère les notes MIDI suivies ; il ne réinitialise ni
  n’arrête universellement tous les équipements externes.
- Les projets n’incluent pas l’audio : conservez ensemble projet et fichiers.
- Les boîtes de dialogue de fichiers et erreurs système suivent la langue de Windows.

## Compilation et contributions

Installez **.NET SDK 10 x64**, selon `global.json`, puis exécutez `Build.ps1` ou
ouvrez `Compila.cmd` depuis la racine. Après les tests, l’exécutable se trouve dans
`artifacts\win-x64\StagePad.exe`.

```powershell
dotnet run --project StagePad.Tests -c Release
dotnet run --project StagePad.IntegrationTests -c Release
```

`--stress` est un diagnostic supplémentaire, pas une certification du matériel.
Rapports, traductions et contributions sont bienvenus : [CONTRIBUTING](../CONTRIBUTING.md).
Consultez [VERIFICA](VERIFICA.md) et [TEST-COMPLETI](TEST-COMPLETI.md) pour les essais.

Licence [MIT](../LICENSE). Avis des composants dans [licenses](../licenses/) et
dans le bouton **Licences** de l’application.

## Slots URGENCE et séquences

Activez **URGENCE : mettre toutes les pistes en pause** dans les réglages d’un slot. Son pad met tous les audios actifs en pause, y compris les pistes protégées, conserve les positions et ne joue pas son propre fichier. Sélectionnez MIDI et/ou OSC pour ses messages. Les séquences en attente sont annulées et les notes MIDI actives libérées avant ses propres commandes. Fonctionne à la souris ou par pad MIDI et se conserve dans le show. Play reprend une piste ; les pads conservent leur mode normal. STOP ALL arrête et remet les positions à zéro. Vérifiez la réception sur les équipements externes.

↑/↓ réordonnent les commandes ; **Attente avant (ms)** ajoute des délais ; **Tester maintenant** transmet une commande à l’arrêt, sans délai et avec Pulse pour les notes MIDI. Les listes MIDI/OSC sont indépendantes. Maximum : 30 secondes par commande et 60 par liste. Le relâchement annule les futures notes Gate ; STOP annule les futures commandes du slot. Utilisez un délai nul pour une urgence immédiate.

Les volumes master/piste acceptent CC absolus, trois formats d’encodeurs relatifs, pas réglable et reprise progressive pour éviter les sauts. La déconnexion libère les pads maintenus ; la reconnexion ne rejoue pas les déclenchements.

**Exporter le show avec audio** crée un `.stagepad.zip` avec les audios originaux ; **Importer un show** accepte un chemin complet ou un paquet du dossier projets, sous un nouveau nom sans écrasement. Arrêtez audio/séquences avant cette opération. Limite : 8 GiB de médias.

**Paramètres → Mises à jour** consulte les versions stables de `Alessio278/StagePad-Live`. Installation uniquement à l’arrêt, données conservées, SHA-256 vérifié et ancienne version sauvegardée. Un échec de démarrage déclenche la restauration ; un retour manuel est aussi disponible. Le dépôt et ses versions doivent être publiés avec les fichiers requis. Ce paquet ne les publie pas automatiquement. Le remplacement manuel de l’EXE reste possible.


Les options sont **Éviter les sauts de volume master** et **Éviter les sauts de volume de la piste**. Avec des faders absolus, le contrôle commence lorsque le fader atteint le volume actuel. La version 0.13.1 ajoute des libellés blancs lisibles et une aide sous chaque option.
