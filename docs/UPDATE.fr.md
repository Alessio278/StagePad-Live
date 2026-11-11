# Mettre à jour StagePad sans réinstallation

1. Fermez l’ancienne version et attendez quelques secondes.
2. Extrayez le nouveau paquet Windows et remplacez l’ancien `StagePad.exe` dans le dossier de l’application.
3. Lancez le nouveau `StagePad.exe`.

L’EXE inclut son runtime : aucun installateur ni réinstallation des dépendances n’est nécessaire.

Projets, réglages, affectations et dernier show restent dans `%LOCALAPPDATA%\StagePad` et le dossier de projets configuré. Conservez ces dossiers. Les fichiers audio d’origine gardent leurs emplacements.

Si une ancienne instance invisible bloque le remplacement, lancez le nouvel EXE depuis un autre dossier. La version 0.12.0 tente de récupérer les instances bloquées du même utilisateur/session. Une instance visible est remise au premier plan : fermez-la avant la mise à jour.

Le paquet GitHub contient les sources ; le paquet Windows suffit pour utiliser ou mettre à jour l’application.

**Exporter le show avec audio** crée un `.stagepad.zip` avec les audios originaux ; **Importer un show** accepte un chemin complet ou un paquet du dossier projets, sous un nouveau nom sans écrasement. Arrêtez audio/séquences avant cette opération. Limite : 8 GiB de médias.
