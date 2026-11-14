<h1><img src="../StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

**Lanceur audio et contrôleur de cues MIDI/OSC open source pour spectacles sous Windows.**

[![Release](https://img.shields.io/github/v/release/Alessio278/StagePad-Live?label=release&color=239f88)](https://github.com/Alessio278/StagePad-Live/releases) [![Windows build](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../LICENSE)

[![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4)](#configuration-et-limites) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Audio MIDI OSC](https://img.shields.io/badge/control-Audio%20%2B%20MIDI%20%2B%20OSC-239f88) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](../README.md) - [Italiano](README.it.md) - [Español](README.es.md) - [Français](README.fr.md)

## Sommaire

- [Présentation](#présentation)
- [Démarrage rapide](#démarrage-rapide)
- [Fonctions](#fonctions)
- [Marqueurs et boucles](#marqueurs-et-boucles)
- [Shows et enregistrement automatique](#shows-et-enregistrement-automatique)
- [MIDI et OSC](#midi-et-osc)
- [Mises à jour](#mises-à-jour)
- [Configuration et limites](#configuration-et-limites)
- [Compilation et tests](#compilation-et-tests)
- [Contributions et licence](#contributions-et-licence)

- [Avertissement Windows SmartScreen](#avertissement-windows-smartscreen)

## Présentation

Préparez les pistes et commandes en **Préparation**, puis déclenchez-les en **Direct** à la souris ou avec un contrôleur MIDI. Pour concerts, théâtre, répétitions et installations. Aucun compte, activation ou contrôle de licence en ligne.

Created by **Alessio Natalucci**. Produit : **StagePad Live**. Exécutable portable : `StagePad.exe`.

## Démarrage rapide

1. Téléchargez et extrayez le ZIP Windows des [Releases](https://github.com/Alessio278/StagePad-Live/releases). Lancez `StagePad.exe`.
2. Choisissez la sortie audio ; commencez à **40 ms** et effectuez le test audio.
3. Réglez 1-100 slots et des grilles jusqu’à **10 x 10**. Chargez les pistes et attendez.
4. Réglez actions, mode, titre, couleur et fades. **LEARN** affecte un pad sans le déclencher.
5. Nouveau show, Ouvrir, Enregistrer ou Enregistrer sous. L’enregistrement automatique est activé.
6. Passez en Direct et activez SHOW LOCK. **F11** : plein écran. STOP ALL arrête et annule les sorties en attente.

L’engrenage propose italien, anglais, espagnol et français sans redémarrer l’audio.

## Fonctions

| Domaine | Fonctions |
| --- | --- |
| Pads | 1-100 slots et grilles indépendantes jusqu’à 10 x 10 |
| Audio | Lecture, pause, arrêt, retrigger, Hold, protection et pause alternée |
| Mixage | Volume par slot et master, fades et budget RAM |
| Sorties | Audio, MIDI et OSC indépendants ou combinés |
| Lecteur | Forme d’onde, saut par double-clic et temps synchronisés |
| Marqueurs | Position, libellé, couleur, MIDI/OSC et boucle |
| Shows | Navigateur intégré, Nouveau/Ouvrir/Enregistrer et gestion |
| Sauvegarde | Automatique en arrière-plan, copie précédente et récupération |
| Direct | Show Lock, urgence et défilement automatique optionnel |
| Interface | Quatre langues, couleurs par slot et titres défilants |

Un slot URGENCE met les audios en pause en conservant leurs positions et peut envoyer MIDI/OSC. STOP ALL remet aussi les positions à zéro. Vérifiez les récepteurs externes.

## Marqueurs et boucles

**+ Marqueur** insère immédiatement à la position actuelle du curseur, même avec une boucle appliquée. Dans **Gérer les marqueurs**, le champ Position suit les déplacements du curseur s’il n’a pas le focus ; sélectionner un marqueur existant affiche initialement sa position enregistrée. Double-clic pour rejoindre un point. Maximum **32 marqueurs**, avec taille de données limitée.

Choisissez deux **marqueurs existants** pour la boucle ; la fin doit suivre le début d’au moins 50 ms. Utilisez **Appliquer la boucle** / **Supprimer la boucle**, sans case à cocher. Arrêtez le slot pour appliquer/supprimer la boucle ; supprimez-la avant de déplacer/supprimer ses bornes. Play démarre directement au marqueur initial ; la pause conserve la position. Les autres marqueurs restent modifiables pendant la boucle. Plage préparée en RAM dans le budget ; rampes de 5 ms aux bords.

Chaque marqueur peut envoyer MIDI, OSC ou les deux, à chaque tour également. Envoi immédiat et Note Off automatique. Un saut manuel ne déclenche ni les cues ignorés ni celui exactement à la destination.

Dans la boucle, le cadre rouge pulse lentement (cycle de 1,6 seconde) ; sans flashs de couleur. Hors boucle, cadre rouge pendant la lecture ; au passage d’un marqueur, cadre et temps émettent **7 flashs rapides** dans sa couleur : 150 ms allumé / 110 ms éteint, **1,82 seconde** au total, avec un halo plus intense. Couleur fixe si les animations Windows sont désactivées.

Le LEARN d’insertion des paramètres affecte un bouton Note On/Off libre à la création de marqueurs sur la piste sélectionnée en lecture. Inactif en Direct, pause, arrêt, chargement et Show Lock. Un maintien ne crée pas de doublons. Affectation enregistrée dans le show.

Pendant la lecture, **+ Marqueur** insère sans ouvrir l’éditeur. Avec une boucle appliquée, dès le premier Play et sur tous les marqueurs, seul le cadre rouge lent reste, y compris au retour de boucle. Les commandes MIDI/OSC restent actives. Hors boucle, le rouge est suspendu pendant les sept flashs, y compris entre les impulsions, puis revient à la fin.

## Shows et enregistrement automatique

Enregistrer met à jour le show courant ; Enregistrer sous crée une autre copie. Double-clic ou Entrée pour ouvrir. **Autres actions** conserve Renommer, Dupliquer, Supprimer, Importer et Exporter. Avant un changement avec modifications : Enregistrer / Ignorer / Annuler. Arrêtez audio et séquences avant d’ouvrir ou de créer un autre show.

Enregistrement automatique activé et configurable : écriture en arrière-plan après environ **1,5 seconde** sans modification, même en lecture. Nom provisoire unique pour les shows sans nom. L’astérisque signale des changements en attente. Écriture atomique, copie `.bak`, erreurs visibles, nouvel essai et sauvegarde finale avec attente limitée ; récupération de session conservée.

Données : `%LOCALAPPDATA%\StagePad`. Shows : `%LOCALAPPDATA%\StagePad\project`. Dossier configurable ; Default rétablit ce chemin. Le changement ne déplace pas les fichiers.

Le dernier show est restauré sans reprise automatique de lecture. Migration des anciennes données sans écraser les shows. `backups\legacy-data` peut contenir des audios référencés et doit rester disponible.

Le JSON référence les fichiers audio. Exporter avec audio crée `.stagepad.zip` ; Importer crée un show distinct. Jusqu’à 8 GiB de médias originaux.

## MIDI et OSC

Audio, MIDI et OSC se sélectionnent par slot et sont indépendants. Un audio absent ne bloque pas les autres sorties.

Jusqu’à **16 commandes MIDI** et **16 OSC** par slot. Note Pulse/Gate, CC, Program Change et formats MSC implémentés. Pads Note On/Off ; master/piste par CC séparés avec LEARN, encodeurs relatifs et reprise progressive. Pas d’horloge MIDI, MIDI thru ou Gate par marqueur.

OSC UDP : hôte, port, chemin et arguments typés. Configurez séparément ports virtuels et récepteurs. Le compteur d’envois ne confirme pas la réception. Ordre, délais et tests individuels dans les paramètres de slot. Une reconnexion ne rejoue pas les commandes.

[Guide MIDI](MIDI.fr.md) - [Guide OSC](OSC.fr.md).

## Mises à jour

Paramètres > Mises à jour consulte les releases stables. Installation au repos avec SHA-256, vérification produit/version, copie et restauration si le démarrage échoue. Remplacement manuel de `StagePad.exe` possible sans perdre les données.

Build non signée : Windows peut afficher SmartScreen. Aucune certification matérielle ou de sécurité annoncée. [Guide](UPDATE.fr.md).

## Configuration et limites

- Application **Windows 10/11 x64** ; pas de build desktop macOS/Linux.
- Runtime inclus ; compilation avec .NET SDK 10 de `global.json`.
- WAV et OGG Vorbis inclus ; MP3/FLAC/AAC et autres formats selon les codecs Media Foundation. Mono/stéréo ; pas de DRM, multicanal ou OGG Opus.
- Préparez les audios. 100 slots ne garantissent pas 100 flux simultanés.
- Clipping de protection, pas un limiteur musical.
- Testez pilotes, latence, reconnexions et durée avec votre matériel.

## Compilation et tests

Sous Windows, installez .NET SDK 10 x64 et lancez :

```powershell
.\Build.ps1
```

Validation du dépôt, tests du noyau/intégration simulée, compilation, contrôle **WPF natif**, puis ZIP/checksums. Un échec bloque le paquet et la release stable.

[Vérification](VERIFICA.md) - [Checklist Windows](TEST-COMPLETI.md) - [Publication](GITHUB-SETUP.md). Les tests simulés sur d’autres plateformes ne signifient pas un support desktop.

## Contributions et licence

Rapports, traductions et contributions sont bienvenus. [CONTRIBUTING](../CONTRIBUTING.md), [SECURITY](../SECURITY.md), [Changelog](../CHANGELOG.md).

Licence [MIT](../LICENSE). Avis des dépendances dans [licenses](../licenses/) et l’application. Logiciel et code sont distribuables selon leurs licences.

## Avertissement Windows SmartScreen

StagePad Live est actuellement distribué sans certificat de signature numérique. Au lancement de l’EXE téléchargé, Windows peut afficher **« Windows a protégé votre ordinateur »** et **« Éditeur inconnu »**. Cet avertissement seul n’est ni une détection de logiciel malveillant ni une garantie que le fichier est sûr.

Téléchargez les versions uniquement depuis le [dépôt officiel](https://github.com/Alessio278/StagePad-Live/releases) et comparez le SHA-256 du fichier avec le `SHA256.txt` publié. Une correspondance confirme que le fichier correspond à celui publié ; elle ne remplace pas une analyse de sécurité. Si vous faites confiance à sa provenance et choisissez de continuer, Windows peut proposer **Informations complémentaires > Exécuter quand même**. Cette option peut être bloquée sur les ordinateurs administrés.
