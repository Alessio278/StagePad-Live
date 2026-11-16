<h1><img src="../StagePad.App/Assets/StagePad.svg" width="58" alt="StagePad Live logo" align="center"> StagePad Live</h1>

**Lanceur audio et contrôleur de cues MIDI/OSC pour les spectacles sous Windows.**

[![Release](https://img.shields.io/github/v/release/Alessio278/StagePad-Live?label=release&color=239f88)](https://github.com/Alessio278/StagePad-Live/releases) [![Windows build](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Alessio278/StagePad-Live/actions/workflows/ci.yml) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](../LICENSE) ![Windows](https://img.shields.io/badge/Windows-10%20%2F%2011%20x64-0078D4) ![.NET](https://img.shields.io/badge/.NET-10%20%7C%20runtime%20included-512BD4?logo=dotnet) ![Languages](https://img.shields.io/badge/languages-IT%20EN%20ES%20FR-6f42c1)

[English](../README.md) - [Italiano](README.it.md) - [Español](README.es.md) - [Français](README.fr.md)

Créé par **Alessio Natalucci**.

## Sommaire

- [Démarrage rapide](#démarrage-rapide)
- [Fonctions](#fonctions)
- [Marqueurs et boucles](#marqueurs-et-boucles)
- [Shows et sauvegarde](#shows-et-sauvegarde)
- [MIDI et OSC](#midi-et-osc)
- [Compilation et tests](#compilation-et-tests)
- [Licence et contributions](#licence-et-contributions)
- [Windows SmartScreen](#windows-smartscreen)

## Démarrage rapide

1. Téléchargez **StagePad-Live-1.1-Windows.zip** depuis [Releases](https://github.com/Alessio278/StagePad-Live/releases), extrayez-le et lancez **StagePad-1.1.exe**. Aucune installation nécessaire.
2. Dans Setup, choisissez le périphérique audio et les ports MIDI. Commencez avec un tampon de 40 ms et lancez le test audio.
3. Chargez les pistes, configurez les actions et assignez les pads avec LEARN. Choisissez la langue depuis la roue dentée.
4. Enregistrez le show, passez en Direct et activez SHOW LOCK. F11 ouvre le plein écran ; STOP ALL arrête l’audio et les commandes en attente.

## Fonctions

| Domaine | Détails |
| --- | --- |
| Pads | 1-100 slots ; grilles Setup et Direct indépendantes, jusqu’à 10 x 10 |
| Audio | One-shot, Hold, protégé et lecture/pause ; fondus ; volumes par slot et master |
| Actions | Audio, MIDI et OSC combinables, urgence avec pause globale ou + Marker exclusif |
| Contrôles | MIDI LEARN, CC séparés pour master/piste, encodeurs relatifs et reprise progressive |
| Shows | Nouveau, Ouvrir, Enregistrer, Enregistrer sous, renommer, dupliquer, supprimer, sauvegarde automatique et export avec audio |
| Interface | Italien, anglais, espagnol et français ; pads colorés, titres défilants et forme d’onde |

### Nouveautés de la 1.1

- Les nouveaux shows commencent sans action sélectionnée. Charger une piste active Audio.
- Les nouvelles commandes MIDI/OSC préremplissent des valeurs modifiables. Changer les valeurs par défaut ne modifie pas les commandes existantes.
- Les volumes affichent des pourcentages et gardent la dernière valeur souris/MIDI lors des déclenchements et enregistrements. Le volume par défaut s’applique aux nouveaux slots ; l’appliquer à tous exige des pistes arrêtées.
- Les pads configurés sont légèrement teintés et s’éclairent à l’activation. Les slots MIDI/OSC sans audio affichent leurs commandes et leur état.
- L’urgence peut exiger un maintien de trois secondes. Utilisez des pads momentanés Note On/Off pour que le relâchement annule l’attente.
- Défilement plus lent à la molette/au pavé tactile, cases alignées et libellé compact +MARKER.

## Marqueurs et boucles

**+ Marker** dans le lecteur ajoute à la position du curseur sans ouvrir l’éditeur pendant la lecture. **Gérer les marqueurs** modifie le temps, le libellé, la couleur et les déclencheurs MIDI/OSC. Double-cliquez sur la forme d’onde ou un marqueur pour vous déplacer.

Un slot peut utiliser l’action exclusive **+ Marker**. Une autre touche MIDI s’assigne dans **Paramètres > Insertion rapide de marqueurs**. Les deux agissent sur la piste sélectionnée en lecture, uniquement dans Setup et sans SHOW LOCK.

Choisissez deux marqueurs existants et cliquez sur **Appliquer la boucle**. La lecture commence au premier ; **Retirer la boucle** supprime l’intervalle. Arrêtez la piste avant de changer la boucle. Dans une boucle, le cadre pulse lentement en rouge ; hors boucle, les marqueurs produisent sept flashs de leur couleur. Les commandes des marqueurs sont envoyées à chaque passage.

## Shows et sauvegarde

Les shows s’ouvrent et s’enregistrent dans l’app. La sauvegarde automatique est active par défaut, travaille en arrière-plan et conserve le fichier précédent en `.bak`. Le démarrage restaure le dernier show sans lancer l’audio.

Le dossier par défaut est `%LOCALAPPDATA%\StagePad\project`, modifiable dans Paramètres. Le JSON référence les pistes ; utilisez **Exporter le show avec audio** pour transférer l’ensemble.

La 1.1 lit les schémas 1, 2 et 3 et enregistre en schéma 3. Gardez des copies avant la mise à jour : la version 1.0 ne lit pas le schéma 3.

## MIDI et OSC

Chaque slot prend en charge jusqu’à 16 commandes MIDI et 16 OSC, avec ordre et délais. Les deux listes avancent indépendamment. MIDI prend en charge Note, CC, Program Change et MSC ; OSC utilise UDP avec hôte, port, adresse et arguments modifiables. Le compteur d’envois ne confirme pas la réception.

[MIDI](MIDI.fr.md) - [OSC](OSC.fr.md) - [Update](UPDATE.fr.md)

## Compilation et tests

La compilation nécessite Windows 10/11 x64 et .NET SDK 10. Le téléchargement portable inclut le runtime. WAV et OGG utilisent les décodeurs inclus ; les autres formats dépendent des codecs Windows.

```powershell
.\Build.ps1
```

Le script exécute les tests core et d’intégration simulée, compile l’app, vérifie WPF sous Windows et crée le ZIP. Complétez la [liste de tests](TESTING.md) avec votre matériel.

## Licence et contributions

StagePad Live est distribué sous [licence MIT](../LICENSE). Les notices des dépendances sont dans [licenses](../licenses/) et dans Paramètres. Consultez [CONTRIBUTING.md](../CONTRIBUTING.md), [SECURITY.md](../SECURITY.md) et l’[historique](../CHANGELOG.md).

## Windows SmartScreen

L’exécutable n’est pas signé : Windows peut afficher “Windows a protégé votre ordinateur” ou “Éditeur inconnu”. Téléchargez depuis les Releases officielles et comparez SHA-256 à `SHA256.txt`. Si vous faites confiance au fichier, Windows peut proposer **Informations complémentaires > Exécuter quand même** ; les PC gérés peuvent bloquer cette option.
