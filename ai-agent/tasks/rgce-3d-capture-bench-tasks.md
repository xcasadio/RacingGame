# Plan agent IA — Banc de capture 3D original / conversion

Plan d'exécution du banc de capture 3D approuvé par l'auteur le 2026-11-14 (conversation de cadrage, pas de fichier d'analyse).
Les décisions D1 → D6 ci-dessous ont été arbitrées avec l'auteur le 2026-11-14 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Produire, pour les trois circuits, des paires d'images « original / conversion » prises avec la même caméra, la même projection et la même taille, puis un rapport d'écart par paire. Le banc sert de preuve aux corrections de conversion qui suivront (position, rotation, échelle, effets de texture).

## État vérifié du dépôt (2026-11-14)

- RacingGame : branche `remaster`, HEAD `7686c0a` (copie de l'original ajoutée telle quelle). `CasaEngine` (sous-module modifié), `.codegraph/`, `.serena/`, `MGUI/` et `log.txt` sont à l'auteur : jamais indexés.
- Original : `RacingGameOriginal/Desktop/Program.cs` ignore ses arguments ; `Core/GameLogic/ScreenshotCapturer.cs` ne capture rien ; `Core/Helpers/RandomHelper.cs:34` sème par l'horloge ; `Core/Graphics/BaseGame.cs:1389-1401` réduit `Model.MaxViewDistance` selon les images par seconde ; `Core/Shaders/ShadowMapShader.cs:251` centre la carte d'ombres sur la voiture.
- Conversion : `--capture-track-audit` (`Bootstrap/TrackMigrationCaptureValidator.cs`) calcule ses vues depuis les entités converties, sans taille imposée ; `Worlds/LegacyTrackSceneFactory.cs:977` convertit (x, y, z) → (x, z, −y) à l'échelle 1.
- Bureau de ce PC : 1920×1080, donc une fenêtre 1920×1080 ne tient pas (`BaseGame.FitWindowedSize` la réduit).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | La référence est la copie `RacingGameOriginal/` (Desktop seulement) ; ses modifications sont versionnées ici. |
| D2 | L'original calcule les vues et les exporte dans `views.json` ; la conversion les rejoue (ADR-0015). |
| D3 | 5 vues par circuit : départ, 20 %, 50 %, 80 % du tracé, vue d'ensemble. Côté original, chaque vue en rendu complet et sans post-traitement. |
| D4 | Seule la caméra de capture de la conversion est alignée (FOV 90°, near 0.5, far 1750) ; la caméra de course n'est pas touchée. |
| D5 | Mode AUTO, un commit par tâche sur `remaster`, aucun push. |
| D6 | Le sous-module `CasaEngine` est hors périmètre. |

## Règles d'exécution pour l'agent

- **Branche `remaster`**, sans push. Ne jamais committer sur `master`.
- **Une seule tâche à la fois**, icône mise à jour, validation notée sous la tâche, **un commit par tâche** incluant ce fichier.
- Messages de commit en anglais, format `type(area): summary`.
- **Ne jamais indexer** les fichiers de l'auteur : `git add` fichier par fichier.
- Sans l'argument de capture, les deux jeux se comportent comme avant.
- Langue : ce plan en français ; code, commits et ADR en anglais.

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Validation globale

- `dotnet build RacingGameOriginal/Desktop/RacingGame.csproj -c Debug` et `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` : 0 erreur.
- Les deux commandes de capture produisent 15 vues de mêmes noms et de même taille ; le rapport couvre les 15 paires.

---

## Phase 1 — Banc

### ✅ T1 — Capture déterministe dans l'original

> Validation (2026-11-14) : build 0 avertissement, 0 erreur. Deux lancements `--capture-track-audit --seed 1` en 1280×720 : 30 PNG et `views.json` identiques au bit près (md5, 31/31). Avant le gel des éoliennes, seules leurs pales différaient (écart moyen ≤ 0,24). Lancé sans argument, le jeu démarre normalement et n'écrit aucune capture. La voiture est épinglée au point de vue à chaque image.

- Objectif : `RacingGame.exe --capture-track-audit [--seed N] [--out <dir>] [--size WxH]` saute les menus, capture 5 vues × 3 circuits × 2 variantes en PNG, écrit `views.json`, puis quitte.
- Fichiers : `RacingGameOriginal/Desktop/Program.cs`, `RacingGameOriginal/Core/Helpers/CaptureOptions.cs` (nouveau), `Core/Helpers/RandomHelper.cs`, `Core/GameSettings.cs`, `Core/Graphics/BaseGame.cs`, `Core/Graphics/Model.cs`, `Core/RacingGameManager.cs`, `Core/GameScreens/TrackAuditCaptureScreen.cs` (nouveau), `docs/decisions/0015-*.md`.
- Validation : build 0 erreur ; deux lancements avec la même graine donnent 30 PNG identiques (ou un écart mesuré et noté) ; sans argument le jeu démarre sur son écran de chargement.
- Commit : `feat(original): add a deterministic track audit capture mode`

### ✅ T2 — Rejouer les vues dans la conversion

> Validation (2026-11-14) : build 0 erreur. `--capture-reference-views <views.json> --reference-capture-out <dir>` écrit 15 PNG 1280×720 ; deux lancements : écart 0,00 sur les 15 vues. Alignement journalisé : la grille de départ de la conversion est à 0,00 m de la voiture de l'original convertie sur les trois circuits ; les points de passage sont à 0,00 à 3,68 m (Expert 50 %). Sur la vue de départ, lignes de route, banderole et relief se superposent. Retirés pour la capture : HUD, caméras de poursuite et libre, lignes de debug du parcours (`RaceCourseDebugComponent`) ; l'orientation de la caméra est remise à zéro (la caméra de poursuite la laissait inclinée). Préalable : `337a43c` (membre obligatoire du moteur) et `b4374f4` (voiture dessinée seulement sur la grille, O1).

- Objectif : `RacingGameCasaEngine.exe --capture-reference-views <views.json>` capture les mêmes vues, mêmes noms, même taille, avec la projection de l'original.
- Fichiers : `RacingGameCasaEngine/Program.cs`, `Bootstrap/RaceLaunchOptions.cs`, `Bootstrap/RacingGameCasaEngineGame.cs`, `Bootstrap/ReferenceViewCaptureValidator.cs` (nouveau).
- Validation : build 0 erreur ; 15 PNG de la taille inscrite dans `views.json` ; deux lancements identiques (ou écart mesuré et noté).
- Commit : `feat(racing-casa): replay the original's reference views`

### ⏳ T3 — Rapport de comparaison par paire

- Objectif : pour chaque paire, écart moyen par canal, image de différence et montage côte à côte ; un rapport Markdown.
- Fichiers : `scripts/UiCaptureCompare/` (mode paires) ou outil voisin.
- Validation : rapport généré sur les 15 paires ; sur la vue de départ, les bords de route se superposent.
- Commit : `feat(scripts): compare original and converted 3D captures`

### ⏳ T4 — Liste des écarts classée

- Objectif : lire les 15 paires et classer les écarts par gravité ; c'est la base des corrections suivantes.
- Sources : rapport de T3, images.
- Validation : liste relue avec l'auteur.
- Commit : `docs(racing-casa): rank the 3D divergences found by the capture bench`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Réglé (`b4374f4`) : la conversion n'a pas d'API pour déplacer la voiture ; l'original ne la dessine donc que sur la grille de départ (`car.visible`). | T2 |
| O2 | Le décor aléatoire hors route n'existe pas dans la conversion : écart attendu. | T4 |

## Hors périmètre

- Corriger les écarts, y compris la caméra de course de la conversion.
- Le sous-module `CasaEngine`, les plateformes autres que Desktop.
