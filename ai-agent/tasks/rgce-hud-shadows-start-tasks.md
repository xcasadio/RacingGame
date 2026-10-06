# Plan agent IA — HUD ×2, réglage des ombres et annonce du départ de course (RacingGameCasaEngine)

Plan d'exécution des trois demandes de l'auteur du 2026-10-05, faites après la clôture du chantier des écrans XAML ([rgce-xaml-screens-tasks.md](rgce-xaml-screens-tasks.md)) :
- « O6 : améliore le réglage des ombres » ;
- « Pour le HUD il faut l'agrandir et faire un x2 sur la taille. Il est trop petit » ;
- « Normalement il y a un texte pour annoncer le départ de la course ? ».

Les décisions D1 → D6 ci-dessous ont été arbitrées avec l'auteur le 2026-10-05 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

- Le HUD de course de RacingGameCasaEngine (RGCE) est deux fois plus grand, panneau de fin de course compris.
- Les ombres de course sont mieux réglées : moins de crénelage, sans acné ni ombres décollées, avec un coût mesuré. Seuls les réglages changent, tous les objets gardent leur ombre.
- Le départ de course est annoncé comme dans le jeu legacy : le feu de départ 3D passe au rouge, au jaune puis au vert, avec les sons « Beep » et « Bleep ».

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-05)

Découverte en lecture seule (deux workflows de découverte avec contre-vérification), puis vérifications directes.

- **Branche et historique**
  - Branche `remaster`, HEAD `5c2c73a`, arbre propre hormis `.serena/` et `log.txt`, non suivis, à l'auteur.
  - D'autres sessions écrivent sur `remaster` : `d95bd6f` (graine stable du décor, qui règle O4 de l'ancien plan) et `5c2c73a` (suppression d'`ArcadeCarMovementComponent`). Les worktrees `.claude/worktrees/*` sont les leurs.
  - Références de capture en vigueur : `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots\references-55304bb` (ombres actives, HUD XAML, 1920×1080). Le décor est stable d'un lancement à l'autre depuis `d95bd6f` (`Worlds/LegacyTrackSceneFactory.cs`, `CreateDeterministicTrackRandom`, FNV-1a).
- **HUD**
  - `Content/UI/Screens/RaceHud/RaceHud.xaml` reprend les tailles du HUD construit en code : rectangles d'`ingame.png` à l'échelle 0,5 pour les panneaux et 0,575 pour le compte-tours, marges de 10 px au bord, dans un `OverlayPanel` responsive.
  - Panneaux : Laps 111×80, Times 171×64, TopTimes 141×196, Tacho 197×196.
  - Panneau de fin : largeur 420, padding 24, polices 26/14/14, espacement 8, bordure 2.
  - Boîtes de chiffres de `RaceHudViewModel` : tour (8, 6, 40, 66), vitesse (106, 147, 85, 41), rapport (164, 86, 30, 41). Aiguille 16×107 en marge (103, 12), pivot (111, 112). `UpdateTextSizes` utilise les constantes `TimesPanelHeight=64` et `TopTimesPanelWidth=141`.
  - RGCE dessine son interface pour 1920×1080 (MGUI `UIResponsiveSettings`, défaut HD, `CasaEngine/MGUI/MGUI.Core/UI/Responsive/UIResponsiveSettings.cs:59`) : facteur 0,667 en 1280×720. L'ancien HUD MGUI de RacingGame était conçu pour 1280×720 (`RacingGame.Shared/UI/MGUI/MguiUiHost.cs:22`), avec les mêmes échelles 0,5 et 0,575 (`RacingGame.Shared/UI/MGUI/Views/GameHudView.cs:14-15`). À résolution égale, le HUD de RGCE fait donc ⅔ du HUD legacy.
  - Un ×2 revient à dessiner les panneaux à l'échelle 1,0 et le compte-tours à 1,15 (calcul en `float` avec arrondi bancaire, comme le code d'origine) :
    - Laps 222×160, chiffre du tour (15, 12, 80, 133) ;
    - Times 342×128, textes en (154, 14) et (154, 78), police 38 ;
    - TopTimes 282×392, en-tête 62, nom de piste en y = 10 (police 26), lignes en y = 66 + 66·i, textes en x = 20 et 82 et en y = ligne + 11 (police 30) ;
    - Tacho 394×392, aiguille 32×214 en marge (207, 24) avec pivot (223, 223), vitesse (211, 294, 170, 83), rapport (329, 171, 60, 83).
- **Ombres**
  - Moteur : une seule carte d'ombres orthographique de 2·`MaxDistance` de côté, centrée sur la caméra, qui la suit sans s'aligner sur les texels (`CasaEngine/CasaEngine/Framework/Rendering/Draw/ShadowPass.cs:226-246`), au format `Single` avec repli `Color` (`ShadowPass.cs:15-16, 175-176`). Filtrage PCF 2×2 fixe, coupure nette au bord de la carte, `DepthBias` en profondeur normalisée et `NormalBias` en unités monde le long de la normale (`CasaEngine/CasaEngine/Content/Shaders/Lighting.fxh:129-176`).
  - Réglables par RGCE : `Resolution` (≥ 1, sans plafond dans le moteur), `MaxDistance`, `DepthBias`, `NormalBias` (`Framework/Rendering/Shadows/ShadowSettings.cs`). RGCE ne règle aujourd'hui que `Enabled` ; le reste vaut les défauts : 1024, 100, 0,001 et 0 (`Worlds/RaceWorldFactory.cs`). Un texel couvre donc environ 0,195 unité.
  - Lumière principale `Light.Key` de direction (-0,527 ; -0,574 ; -0,628) (`RaceWorldFactory.cs`).
  - Jeu legacy : carte 1024 floutée, biais 0,00065, distance de lumière virtuelle 171,6 et portée visible 129,25 (`RacingGame.Shared/Shaders/ShadowMapShader.cs:230-236`).
  - Mesures possibles :
    - `--capture-track-audit` capture 21 vues fixes, 7 par piste (`Bootstrap/TrackMigrationCaptureValidator.cs`), dans `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots` ;
    - l'option Show FPS affiche le `DebugOverlay` du moteur (temps Update et Draw au chronomètre, moyenne et maximum sur 10 s, statistiques de rendu) (`RacingGameCasaEngineGame.cs:119, 148`, `CasaEngine/CasaEngine/Framework/Rendering/DebugOverlay.cs:15, 97-98`) ;
    - avec `IsFixedTimeStep = true` (`RacingGameCasaEngineGame.cs:158`), seul le temps Draw mesuré au chronomètre reflète le coût, et seulement côté processeur.
- **Annonce du départ**
  - Jeu legacy, pendant les 3 s de zoom de départ (`RacingGame.Shared/GameLogic/BasePlayer.cs:270-283`, `Landscapes/TrackObjectManager.cs:161-187`) :
    - le feu est rouge au départ (`ResetStartLight`, modèle `StartLight`) ;
    - à 1 s, il reste rouge et joue « Beep » ;
    - à 2 s, il passe au jaune (`StartLight2`) et joue « Beep » ;
    - à 3 s, il passe au vert (`StartLight3`), joue « Bleep », et le joueur prend la main.
    - Aucun texte n'est affiché.
  - Couleurs : le code legacy les nomme « red, yellow, green » pour `StartLight`, `StartLight2` et `StartLight3` (`BasePlayer.cs:279`, `TrackObjectManager.cs:21-23`). Les trois `Content/Models/StartLight*.gltf` de RGCE partagent les mêmes textures (`TLight.png`, `Light.png`) ; la couleur vient de leur géométrie, et le rendu de chacun est contrôlé par la sonde de T2.1.
  - RGCE place une entité statique `Track.Scenery.StartLight3.NNN` (politique `StaticDecoration`) (`Worlds/LegacyTrackSceneFactory.cs:636-643` pour la position, `:197` pour le nom, `:422` pour la politique). Elle ne change jamais.
  - Décompte de RGCE : 3 s, qui démarrent à la configuration de la course (`GameFramework/RaceGameMode.cs:57, 84-102`). La pause est refusée pendant le décompte (`RaceGameMode.cs:117-120`), et la conduite commence à 0 (`Components/RaceFlowCoordinatorComponent.cs:45-50`). `StartBannerSecondsRemaining` (0,85 s) n'est jamais affiché.
  - Historique : le premier HUD de RGCE affichait une ligne de débogage « Countdown: N » (`git show 8146bcb:RacingGameCasaEngine/Screens/RaceHudScreen.cs:106-108`). Elle a été retirée par `8c92c95`, au portage du HUD legacy.
  - `StaticModelComponent.StaticModel` est réglable, mais l'affecter ne reconstruit pas la hiérarchie de rendu, faite par `InitializeWithWorld` (`CasaEngine/CasaEngine/Framework/Scene/Entities/Components/StaticModelComponent.cs:42, 202-238`, fait corrigé par la contre-vérification). La méthode de changement de feu reste à établir (P3).
- **Sons**
  - RGCE ne joue aucun son de jeu : ses options de volume pilotent `SoundEffect.MasterVolume` et `MediaPlayer.Volume` (`RacingGameCasaEngineGame.cs:120-144`).
  - CasaEngine a un système audio : `AudioSystemComponent` créé par `CasaEngineGame` (`CasaEngine/CasaEngine/Framework/Application/CasaEngineGame.cs:63, 366`), `AudioService.PlaySound(SoundAsset)`, mélangeur à bus (`Framework/Audio/AudioService.cs:104-109`, `Mixing/AudioMixer.cs`), assets `.sound` (`CasaEngine/docs/decisions/0002-audio-asset-format-and-editor-scope-v1.md`). Sans périphérique audio, la lecture devient silencieuse (`Backends/MonoGameAudioBackend.cs:102, 241`). `AudioSystemComponent` expose un `MasterVolume` (`CasaEngine/docs/decisions/0040-project-audio-mute-setting.md:9`).
  - Sources : `RacingGame/Content/Audio/Waves/Beep.wav` (1,000 s) et `Bleep.wav` (1,132 s), PCM 16 bits stéréo 44,1 kHz (en-têtes RIFF lus).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Ombres : « réglages seuls ». Tous les objets gardent leur ombre ; seuls `Resolution`, `MaxDistance`, `DepthBias` et `NormalBias` sont réglés, d'après des captures comparatives et une mesure du coût (auteur, 2026-10-05). **Remplacée en partie le 2026-10-06** par D3 de [rgce-start-beeps-shadow-shimmer-tasks.md](rgce-start-beeps-shadow-shimmer-tasks.md) : le sol ne projette plus d'ombre, comme dans l'original, et `NormalBias` passe à 0,1. |
| D2 | HUD : ×2, marges au bord comprises (10 → 20 px), panneau de fin de course compris (largeur 420 → 840, polices doublées) (auteur, 2026-10-05). |
| D3 | Départ : feu legacy animé (rouge, jaune, vert) et sons « Beep » / « Bleep », avec le volume des options ; pas de texte de décompte (auteur, 2026-10-05). |
| D4 | CasaEngine et MGUI (sous-modules) ne sont pas modifiés (règle conservée des chantiers précédents). |
| D5 | Plan dans `ai-agent/tasks/` selon le modèle `ai-agent/plan-template.md`. |
| D6 | Le chantier précédent reste clos ; ses vérifications manuelles 🧪 restent à faire par l'auteur. |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **Branche** : `remaster`, comme le chantier précédent ; jamais de push. Avant chaque commit, vérifier `git log` : d'autres sessions écrivent sur cette branche. |
| P2 | **HUD ×2** : panneaux recalculés aux échelles 1,0 et 1,15 (valeurs dans « État vérifié »), constantes de `UpdateTextSizes` passées à 128 et 282. Marges au bord 20. Panneau de fin : largeur 840, padding 48, bordure 4, espacement 16, polices 52/28/28. L'ombre des textes reste à (1, 1), comme le décalage fixe de `DrawShadowedText`. Les données de conception ne changent pas. |
| P3 | **Feu animé** : une petite sonde, dans le scratchpad sur une copie, choisit entre deux voies sans changer le moteur.<br>- (A) Trois entités de feu au même endroit, rouge, jaune et vert, avec une politique non statique ; une seule visible à la fois.<br>- (B) Une entité dont le modèle est changé puis réinitialisé.<br>On garde la voie qui s'affiche correctement sans avertissement.<br>La logique vit dans `RaceFlowCoordinatorComponent`, qui fait déjà avancer le décompte : rouge à la configuration ; « Beep » à 2 s restantes ; jaune et « Beep » à 1 s ; vert et « Bleep » à 0. Le feu est remis au rouge à chaque nouvelle course. |
| P4 | **Sons** : `Beep.wav` et `Bleep.wav` copiés dans `RacingGameCasaEngine/Content/Audio/` (règle de l'ADR-0002 du dépôt : tout fichier catalogué vit sous `Content`), avec deux assets `.sound` (bus des effets) catalogués et joués par `AudioService.PlaySound`.<br>Le lien entre l'option Sound Volume et ce service est vérifié dans le code du moteur ; s'il n'existe pas, l'option règle aussi le volume du bus des effets.<br>Une trace `Logs.WriteTrace` est écrite à chaque son, pour la validation automatique.<br>Décision consignée en ADR-0003, « RGCE plays its sound effects through CasaEngine's audio system ». |
| P5 | **Ombres, méthode** :<br>- variantes construites sur une copie de HEAD dans le scratchpad (`git archive`, références rendues absolues, comme pour les références de T0.2 de l'ancien plan) ;<br>- pour chaque variante : `--capture-track-audit` (21 vues fixes) et un run avec Show FPS activé le temps de la mesure (fichier d'options sauvegardé puis restauré à l'identique) pour lire le temps Draw ;<br>- grille : `Resolution` ∈ {1024, 2048, 4096} × `MaxDistance` ∈ {60, 100, 150}, puis `DepthBias` ∈ {0,0005 ; 0,001 ; 0,002} et `NormalBias` ∈ {0 ; 0,05 ; 0,1} sur le meilleur couple ;<br>- critères, dans l'ordre : pas d'acné ni d'ombre décollée de la voiture, marches nettement réduites sur la route dans les vues de poursuite, ligne de coupure de la carte invisible dans ces vues, puis le coût Draw le plus bas parmi les variantes qui satisfont ces critères ;<br>- le choix, ses chiffres et ses planches sont notés sous la tâche. |
| P6 | **Validation par tâche** :<br>- `dotnet build RacingGame.slnx` 0 erreur ;<br>- `--smoke-frontend` code 0 sans avertissement ;<br>- `--capture-ui-screens` comparé à `references-55304bb`, avec les écarts attendus nommés par tâche ;<br>- éditeur, `scripts/capture_editor_screen.ps1` sur `RaceHud.uiscreen` pour T1.1 ;<br>- `--capture-track-audit` code 0 pour T2.1, T3.1 et T4.1 ;<br>- nouvelles références notées après T1.1 et T4.1 ;<br>- 🧪 pour ce que seul l'auteur peut juger : taille ressentie, sons entendus, ombres en mouvement. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (P1). Ne jamais committer sur `master`.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Ne jamais pousser.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Aucun projet de test n'existe pour RGCE.
- **Ne jamais modifier** `CasaEngine/` ni `CasaEngine/MGUI/` (D4).
- Si le code est écrit mais qu'une vérification visuelle ou manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** les modifications préexistantes de l'auteur ni celles d'autres sessions : `git add` fichier par fichier ; `.serena/` et `log.txt` ne sont jamais indexés.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- **Sondes** : uniquement dans le scratchpad, sur une copie ; jamais d'envoi de touches ni de mouvements de souris simulés. Un survol par la souris réelle peut fausser une capture : un écart localisé sous le curseur se confirme par un second run.
- **Fichier d'options de l'auteur** (`%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\front-end-options.json`) : sauvegardé avant toute modification de mesure, puis restauré à l'identique (même SHA-1).
- **Budget** : au plus 3 cycles « correction → build ou validation » par tâche. Un 4e échec passe la tâche en ⚠️ Blocked, avec l'erreur et la question dans « Points ouverts », puis arrêt.
- **Retour arrière** : avant commit, suppression des seuls fichiers créés par la tâche et `git restore` des seuls fichiers suivis qu'elle a modifiés ; après commit, `git revert <SHA>` ; jamais de `git checkout`, `git reset --hard`, `git clean` ni `git stash`.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
- `--smoke-frontend`, `--verify-legacy-import-profile`, `--capture-track-audit` : code 0, journaux sans `[Warning]` ni `[Error]`.
- `--capture-ui-screens` contre les dernières références notées : 0,00 sur les états non concernés, écarts expliqués sur les autres.
- `scripts/capture_editor_screen.ps1` sur `RaceHud.uiscreen` : code 0, sans erreur de données de conception.
- Passe `verifier` indépendante sur le résultat final.
- Vérification manuelle par l'auteur :
  - taille du HUD à plusieurs résolutions ;
  - feu et sons au départ des trois pistes ;
  - volume des sons selon l'option ;
  - ombres en course, en mouvement.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan, index et ADR

- Objectif : versionner ce plan, l'ajouter à l'index et consigner la décision audio (P4).
- Fichiers : `ai-agent/tasks/rgce-hud-shadows-start-tasks.md`, `ai-agent/README.md`, `docs/decisions/0003-rgce-sound-effects-through-casaengine-audio.md`, `docs/decisions/README.md`.
- Validation : fichiers présents ; liens de l'index valides.
- Commit : `docs(racing-casa): plan the HUD size, shadow tuning and race start light`
- Note de validation : plan relu par un `plan-verifier` (READY), approuvé par l'auteur avec ses propositions P1 → P6, mode AUTO (2026-10-05). ADR-0003 écrite et indexée ; liens de `ai-agent/README.md` et `docs/decisions/README.md` vérifiés.

## Phase 1 — HUD ×2

### 🧪 T1.1 — HUD de course deux fois plus grand

- Objectif : D2 et P2.
- Fichiers :
  - `RacingGameCasaEngine/Content/UI/Screens/RaceHud/RaceHud.xaml` (tailles, marges, positions, aiguille, panneau de fin) ;
  - `RacingGameCasaEngine.UI/ViewModels/RaceHudViewModel.cs` (boîtes de chiffres, constantes de `UpdateTextSizes`) ;
  - `docs/mgui-gaps-from-rgce-xaml-screens.md` : une note au début de la section D15 indique que ses mesures décrivent le HUD de la migration, doublé ensuite.
- Étapes :
  1. Recalculer chaque valeur aux échelles 1,0 et 1,15 (« État vérifié du dépôt »), les commentaires de `RaceHud.xaml` compris.
  2. Panneau de fin selon P2.
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` code 0, aucun avertissement ;
  - `--capture-ui-screens` contre `references-55304bb` : 0,00 hors états de course ; `race-hud`, `pause` et `race-finished` changent ;
  - mesure sur la capture : panneaux à 222×160, 342×128, 282×392 et 394×392 en 1920×1080 (encre des fonds), marges de 20 px ;
  - planche : chiffres, aiguille et textes à leur place ;
  - nouvelles références notées : le run de ce commit, copié en `references-<SHA>` ;
  - éditeur : `capture_editor_screen.ps1` sur `RaceHud.uiscreen` code 0, ligne d'état sans erreur ;
  - 🧪 taille jugée en jeu par l'auteur, à plusieurs résolutions.
- Commit : `feat(racing-casa): double the race HUD size`

> Validation (2026-10-05) :
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE. `--smoke-frontend` : code 0, journal sans `[Warning]` ni `[Error]`.
> - `--capture-ui-screens` (2 runs, code 0, journaux propres) contre `references-55304bb` :
>   - `race-hud` 8,31/8,04/7,76, `pause` 2,75/2,67/2,58 et `race-finished` 16,85/14,95/12,88 : écarts attendus, HUD doublé ;
>   - autres états à 0,00, sauf un état par run touché par le survol de la vraie souris : `main-menu` 0,28 et `track-selection` 0,03 au 1er run (bouton PLAY en surbrillance), `options` 0,28 au 2e (rangée de boutons du haut) ; chaque fois 0,00 à l'autre run ;
>   - entre les deux runs : `race-hud` et `pause` à 0,00 (rendu stable), `race-finished` 0,11 (correctif ci-dessous).
> - Mesures sur `race-hud` en 1920×1080, échelle 1 : panneau Laps de (20, 20) à (241, 179), bord droit de Times à x = 362 (20 + 342) ; planche : chiffres, aiguille (pivot au centre du cadran) et textes à leur place, ombre des textes à (1, 1).
> - Correctif dans la tâche : les textes du panneau de fin perdaient le bas de leurs jambages (« Victory », « Lap »), défaut déjà visible dans `references-55304bb` et doublé par le ×2 (M8, texte dessiné sous sa boîte). Ils ont maintenant `ClipToBounds="False"`, comme les textes du HUD.
> - Éditeur : `scripts/capture_editor_screen.ps1 -Screen UI/Screens/RaceHud/RaceHud.uiscreen` : code 0, diagnostics sans erreur, ligne d'état « Loaded RaceHud.xaml », HUD doublé avec les données de conception.
> - Nouvelles références : le 2e run (`ui-run-20261005-195808`), dont `ui-options.png` est remplacée par celle du 1er run (même code pour cet écran, sans survol), copié en `references-<SHA>` ; le SHA de ce commit est noté sous T2.1.
> - 🧪 Reste : taille du HUD jugée en jeu par l'auteur, à plusieurs résolutions.

> Retour de l'auteur (2026-10-05) : en bas à gauche, les temps ne sont pas centrés dans leur conteneur ; en haut à droite, le texte non plus.
> - Cause, établie par une découverte en lecture seule :
>   - les textes gardaient les positions du HUD construit en code, ancrées à gauche ;
>   - MGUI dessine les glyphes plus bas que leur boîte de ligne, parce que la hauteur de ligne vient de la police sprite Arial alors que les glyphes dessinés sont ceux de Tahoma ;
>   - mesuré à l'échelle 1 : temps 16 px trop bas et 20 px trop à droite, débordant du panneau ; lignes du top 5 8 px trop bas.
> - Correction (`RaceHud.xaml`) : chaque texte est centré sur la zone vide de sa case, mesurée dans `ingame.png` :
>   - temps : x 113 à 341, à droite de « Current: » ;
>   - nom de piste : corps de l'en-tête ;
>   - lignes : case du rang et case du temps.
>
>   Un padding bas (15 et 16) remonte l'encre au centre de la case. Les temps, dont la ligne est trop haute pour ce procédé, sont placés depuis le haut de leur ligne, dans un panneau prolongé de 10 au-dessus du sprite ; le sprite reste au même endroit.
> - Validation :
>   - `dotnet build` : 0 erreur, aucun avertissement RGCE ;
>   - `--smoke-frontend`, `--capture-track-audit` et `--capture-ui-screens` : code 0, journaux propres ;
>   - écart des captures contre `references-f94ed3e` limité aux textes des deux panneaux ;
>   - encre centrée à 2 px près (chiffres et majuscules) en 1920×1080, 1600×900, 1440×810 et 1280×720, mesuré sur des captures d'une sonde ;
>   - éditeur : `RaceHud.uiscreen` sans erreur.
> - Vérification indépendante (3 angles : pixels, sémantique MGUI, exactitude de la doc) :
>   - centrage CONFIRMED ;
>   - une affirmation fausse de la doc (le DrawOrigin, qui s'annule, a été cité comme cause du décalage) et des tolérances trop précises sont corrigées ;
>   - P4 reportés : padding 14 un peu meilleur que 15 pour le nom de piste selon un modèle ; aperçu de l'éditeur sans les tailles de police d'exécution (E1).
> - Réglages de l'auteur (`display-settings.json`, `front-end-options.json`) restaurés à l'identique après les runs.

## Phase 2 — Annonce du départ

### 🧪 T2.1 — Feu de départ animé

- Objectif : D3, partie visuelle, selon P3.
- Fichiers :
  - `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs` (création du feu selon la voie retenue) ;
  - `RacingGameCasaEngine/Components/RaceFlowCoordinatorComponent.cs` (états du feu d'après le décompte) ;
  - éventuellement un petit composant ou service de feu dans `RacingGameCasaEngine/GameFramework/`.
- Étapes :
  1. Sonde dans le scratchpad : voie (A) puis, si elle échoue, voie (B). Captures pendant le décompte (copie de `UiScreenCaptureValidator` modifiée dans la sonde seulement) à 2,5 s, 1,5 s et 0,5 s restantes.
  2. Implémenter la voie retenue ; trace `Logs.WriteTrace` à chaque changement de feu (rouge, jaune, vert).
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` et `--capture-track-audit` code 0, sans avertissement ;
  - journal : la séquence rouge, jaune, vert apparaît à chaque course, aux bons instants du décompte ;
  - `--capture-ui-screens` contre les références de T1.1 : 0,00 attendu, le feu étant vert après le décompte comme aujourd'hui ;
  - planches de la sonde : rouge, jaune, vert visibles ;
  - 🧪 départ observé par l'auteur.
- Commit : `feat(racing-casa): animate the race start light`

> Validation (2026-10-05) :
> - Références de T1.1 : `references-70fee31` (commit `70fee31`).
> - Voie retenue : (B), sans essayer (A), parce que la lecture du code écarte (A).
>   - (A) entre en conflit avec la vue de debug « circuit seul » : `ApplyRaceWorldVisibilityState` réécrit `IsVisible` de tout le décor (`Bootstrap/RacingGameCasaEngineGame.cs:325-341`) et rallumerait les trois feux.
>   - (B) est prévue par le moteur : `StaticModelComponent.InitializeWithWorld` retire les sous-meshes générés avant de reconstruire (« re-initialize », `StaticModelComponent.cs:226-236`). `StaticModel.Initialize` est idempotent (`StaticModel.cs:66-71`), et le retrait des sous-meshes ne fait que les détacher (`SceneComponent.cs:403-408`). Le rendu est en temps réel par défaut (`RenderView.cs:84`).
> - Code :
>   - le feu est construit rouge (`StartLight`), comme `ResetStartLight` le laissait ; les modèles jaune et vert sont chargés avec la piste ;
>   - `GameFramework/RaceStartLight.cs` change le modèle, avec une trace par couleur ;
>   - `RaceFlowCoordinatorComponent` lui donne la couleur du décompte : rouge à 1 s ou plus, jaune entre 1 s et 0 (exclu), vert à 0.
> - Outil `scripts/TrackPlacementExporter` (hors solution) : sa copie de la fabrique attend maintenant `StartLight`, le modèle construit. L'export de scène se fait pendant le décompte (deux images stables après le chargement, `TrackRuntimeSceneExportValidator.cs:100-122`). `dotnet build scripts/TrackPlacementExporter` : 0 erreur.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
> - `--smoke-frontend`, `--capture-track-audit` (21 captures) et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]`. Dans chaque course, la séquence « Start light: red, yellow, green » apparaît une fois : 1 course pour le smoke, 3 pour l'audit, 1 pour les captures. Dans l'audit, le feu reste rouge 2 s puis jaune 1 s.
> - `--capture-ui-screens` contre `references-70fee31` : états de course à 0,00 (feu vert après le décompte, comme avant). `help` à 0,18 et `track-selection` à 0,03 : écarts localisés sur un bouton, dus au survol de la vraie souris.
> - Sonde (copie dans le scratchpad, validateur de captures modifié dans la copie seulement) : feu rouge à 1,5 s du départ, jaune à 0,5 s, vert après le départ. À 2,5 s, la caméra d'intro, vue de côté, ne montre pas le feu.
> - 🧪 Reste : départ observé en jeu par l'auteur.

### 🧪 T2.2 — Sons « Beep » et « Bleep »

- Objectif : D3, partie sonore, selon P4.
- Fichiers :
  - `RacingGameCasaEngine/Content/Audio/Beep.wav`, `Bleep.wav` (copies) et leurs `.sound` ;
  - `RacingGameCasaEngine/Content/AssetInfos.json` ; `RacingGameCasaEngine.csproj` (copie de `Content\Audio\**`) ;
  - le point de changement de feu de T2.1 ;
  - si nécessaire, le lien de l'option Sound Volume au bus des effets (`Bootstrap/RacingGameCasaEngineGame.cs`).
- Étapes :
  1. Lire le format `.sound` et le chargement des clips dans le moteur (`Framework/Audio/SoundAsset.cs`, `AssetContentManagerAudioClipProvider.cs`).
  2. Établir dans le code du moteur si `SoundEffect.MasterVolume` s'applique aux voix du service audio. Sinon, l'option Sound Volume règle aussi le volume du service audio (`AudioSystemComponent.MasterVolume` ou bus des effets, selon ce que le code montre).
  3. Jouer « Beep » à 2 s et à 1 s restantes, « Bleep » à 0 ; trace à chaque son.
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` et `--capture-track-audit` code 0, sans avertissement, assets chargés ;
  - journal : 2 « Beep » puis 1 « Bleep » par course, et l'état de l'audio (disponible ou non) noté ;
  - 🧪 sons entendus par l'auteur, volume réglé par l'option.
- Commit : `feat(racing-casa): play the start light sounds`

> Validation (2026-10-05) :
> - Option Sound Volume : rien à brancher.
>   - Le backend audio de CasaEngine joue chaque voix par une `SoundEffectInstance` (`CasaEngine/CasaEngine/Framework/Audio/Backends/MonoGameAudioBackend.cs:415, 426`).
>   - MonoGame applique `SoundEffect.MasterVolume` à toutes ces instances : « the master volume scale applied to all SoundEffectInstances » (https://docs.monogame.net/api/Microsoft.Xna.Framework.Audio.SoundEffect.html).
>   - L'option règle déjà `SoundEffect.MasterVolume` (`Bootstrap/RacingGameCasaEngineGame.cs:143`).
> - Contenu :
>   - `Content/Audio/Beep.wav` et `Bleep.wav` : copies de `RacingGame/Content/Audio/Waves/`, même SHA-1 ;
>   - `Beep.sound` et `Bleep.sound` : format des `.sound` de `CasaEngine.Demos`, bus `Sfx` ;
>   - 4 entrées au catalogue (`Sound.Beep`, `Sound.BeepWave`, `Sound.Bleep`, `Sound.BleepWave`), qui en compte maintenant 52 ;
>   - copie de `Content\Audio\*.*` à la sortie du build.
> - Code : `RaceStartLight` suit maintenant les signaux de `ReplaceStartLightObject`. À 2 s du départ : rouge et « Beep » ; à 1 s : jaune et « Beep » ; au départ : vert et « Bleep ». Les sons passent par `AudioService.PlaySound`, et leurs handles sont rendus au détachement du coordinateur.
> - Doc : section « Sound effects » dans `RacingGameCasaEngine/Assets/README.md`.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE ; les 4 fichiers audio sont copiés dans `bin/.../Content/Audio`.
> - `--smoke-frontend`, `--capture-track-audit` et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]`. Chaque course trace « Start light: red, beep played (audio available) », « yellow, beep played », puis « green, bleep played » : 1 course pour le smoke, 3 pour l'audit, 1 pour les captures. Les `.sound` sont chargés au début de la course, les `.wav` au premier son.
> - `--capture-ui-screens` contre `references-70fee31` : états de course à 0,00 ; `help` 0,18 et `track-selection` 0,03, mêmes écarts qu'en T2.1, localisés sous la souris immobile.
> - 🧪 Reste : sons entendus par l'auteur, et volume réglé par l'option Sound Volume.

## Phase 3 — Ombres

### 🧪 T3.1 — Réglage des ombres de course

- Objectif : D1 selon P5.
- Fichiers : `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` (réglages de `world.EnvironmentSettings.Shadows`).
- Étapes :
  1. Sonde : copie de HEAD dans le scratchpad, une construction par variante de la grille P5.
  2. Pour chaque variante : `--capture-track-audit` et lecture du temps Draw avec Show FPS. Planches comparatives des vues de poursuite et de départ, recadrées sur les bords d'ombre.
  3. Choisir selon les critères de P5 ; appliquer dans `RaceWorldFactory.cs`, avec un commentaire qui explique les valeurs.
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` et `--capture-track-audit` code 0, sans avertissement ;
  - planches avant et après, chiffres de coût notés ;
  - `--capture-ui-screens` : seuls les états de course changent ; nouvelles références notées ;
  - fichier d'options de l'auteur restauré à l'identique ;
  - 🧪 ombres en mouvement jugées par l'auteur.
- Commit : `feat(racing-casa): tune the race shadow settings`

> Validation (2026-10-05) :
> - Réglages retenus (`Worlds/RaceWorldFactory.cs`) : `Resolution` 4096, `MaxDistance` 150, `DepthBias` 0,001 (valeur du moteur), `NormalBias` 0,4. Le texel passe de 0,195 à 0,073 unité.
> - Méthode, avec trois écarts à P5 :
>   - une seule construction de la sonde (copie de HEAD dans le scratchpad), qui lit les réglages dans des variables d'environnement, au lieu d'une construction par variante ;
>   - coût mesuré sans plafond dans la sonde : pas fixe et VSync coupés, images comptées 4 s sur la vue de course arrêtée en 1920×1080. Pas d'activation de Show FPS : `front-end-options.json` n'est pas touché ;
>   - grille des biais étendue à `NormalBias` 0,2 ; 0,3 ; 0,4 ; 0,5, parce qu'aucune valeur de P5 n'enlevait l'acné.
> - Coût par image (2 passes, bruit ±0,03 ms) :
>   - ombres coupées : 1,40 ms ;
>   - réglages du moteur (1024/100) : 1,60 à 1,62 ms ;
>   - toutes les variantes 1024 à 4096 × 60 à 150 : 1,58 à 1,64 ms ;
>   - réglage retenu : 1,61 ms.
>   Aucune différence mesurable sur cette machine. La carte 4096 prend environ 128 Mo de mémoire vidéo au lieu de 8 ; si l'allocation échoue, le moteur se passe d'ombres (`ShadowPass.cs:184-196`).
> - Planches (vue de course, vues d'audit au cadrage stable `Advanced-*` et `Expert-*`) :
>   - marches de l'ombre du rail et de la voiture nettement réduites dès 2048, et plus encore à 4096. Une distance de 60 perd les ombres lointaines, et à 100 la limite de la carte les tronque, comme avant ; 150 les garde ;
>   - acné des pentes en lumière rasante présente à `NormalBias` ≤ 0,2 quel que soit `DepthBias` (0,0005 à 0,002), résiduelle à 0,3, absente à 0,4 et 0,5 ;
>   - en contrepartie, les ombres portées rétrécissent un peu quand `NormalBias` monte : environ −10 % à 0,3, −17 % à 0,5. La bande d'ombre du rail sur la route est plus fine qu'avant. L'ombre de la voiture reste collée à la voiture à toutes les valeurs.
> - Mesures parasites écartées :
>   - les vues d'audit Beginner changent de cadrage dans certains runs : la caméra de debug de l'audit suit la vraie souris. Deux runs identiques donnent des images identiques ;
>   - les états de menu de `--capture-ui-screens` montrent le survol de la souris immobile (`help` 0,18, `track-selection` 0,03), comme en T2.1.
> - Effet de bord de la sonde, corrigé : forcer la VSync à false dans `ApplyFrontEndOptions` l'a enregistrée dans `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\display-settings.json`, ce que fait chaque lancement de course (`RacingGameCasaEngineGame.cs:139-140`). `IsVSyncEnabled` y est remis à true, la valeur d'avant la sonde : la première grille plafonnait à 100 images/s parce que le lancement de course la rallumait. `front-end-options.json` est identique à la sauvegarde du chantier précédent (même SHA-1).
> - `dotnet build RacingGame.slnx` : 0 erreur, 0 avertissement.
> - `--smoke-frontend`, `--verify-legacy-import-profile`, `--capture-track-audit` (21 captures) et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]`.
> - `--capture-ui-screens` contre `references-70fee31` : `race-hud` 2,77/2,97/2,97, `race-finished` 4,47/4,21/3,65 et `pause` 0,87/0,93/0,93, des écarts attendus puisque ce sont les ombres ; les états de menu ne changent pas, hors survol.
> - Nouvelles références : `references-<SHA>`, avec les trois états de course de ce run (`ui-run-20261005-205209`) et les sept états de menu de `references-70fee31`, dont le code n'a pas changé. Le SHA est noté sous T4.1.
> - O1 reste ouvert : la carte suit la caméra sans s'aligner sur les texels. Les texels plus petits rendent le scintillement moins visible, sans le supprimer.
> - 🧪 Reste : ombres en mouvement jugées par l'auteur, dont la finesse de l'ombre du rail.

## Phase 4 — Clôture

### 🧪 T4.1 — Validation globale et rapport de fin

> Validation (2026-10-05) :
> - Références de T3.1 : `references-f94ed3e` (commit `f94ed3e`).
> - **Build** : `dotnet build RacingGame.slnx` 0 erreur ; build non incrémental de `RacingGameCasaEngine` et `RacingGameCasaEngine.UI` sans avertissement dans leurs projets.
> - **Jeu**, sur le code final (T3.1) :
>   - `--smoke-frontend`, `--verify-legacy-import-profile`, `--capture-track-audit` et `--capture-ui-screens` donnent le code 0, sans `[Warning]` ni `[Error]` ;
>   - chaque course trace la séquence rouge/« Beep », jaune/« Beep », vert/« Bleep ».
> - **Éditeur** : `capture_editor_screen.ps1` sur `RaceHud.uiscreen` code 0 (T1.1). L'écran n'a pas changé depuis.
> - **Passe `verifier`** indépendante : **CONFIRMED** sur les 5 points (HUD ×2 recalculé valeur par valeur, feu, sons, réglages d'ombres, périmètre et sous-module inchangé). Elle a rejoué le build, l'audit et les captures, et comparé `ui-run-20261005-205754` à `references-f94ed3e` : états de course à 0,00, menus ≤ 0,18 (survol). Pas de P0 à P2. Deux remarques P4, reportées :
>   - les références de comparaison viennent du même commit : elles prouvent la stabilité, la justesse du HUD étant établie par le recalcul ;
>   - une image de plus d'une seconde ferait sauter un signal du feu et son son, comme dans le jeu legacy.
> - **Sous-module** : `git diff --name-only 5c2c73a..f94ed3e` ne liste aucun chemin `CasaEngine/` ; `git -C CasaEngine status --porcelain` vide.
> - **Réglages de l'auteur** : `display-settings.json` (VSync rétablie en T3.1) et `front-end-options.json` sont inchangés pendant la vérification (même SHA-1). Le verifier n'a pas lancé `--smoke-frontend`.
> - 🧪 Reste la vérification manuelle par l'auteur (liste « Validation globale » et notes 🧪 de T1.1 → T3.1).
>
> Rapport de fin :
> - **HUD** : deux fois plus grand, panneau de fin compris (T1.1). Les textes de ce panneau ne perdent plus le bas de leurs lettres.
> - **Départ** : le feu de départ 3D passe du rouge au jaune puis au vert, avec « Beep », « Beep » puis « Bleep », comme dans le jeu legacy (T2.1, T2.2). Les sons passent par le système audio de CasaEngine (ADR-0003) et suivent l'option Sound Volume.
> - **Ombres** : 4096 texels sur 2 × 150 unités, biais normal 0,4 (T3.1). Les marches sont environ 2,7 fois plus fines, l'acné du terrain a disparu et les ombres lointaines sont gardées. Coût sans différence mesurable ; environ 128 Mo de mémoire vidéo au lieu de 8.
> - **Risques** :
>   - mémoire vidéo de la carte 4096 sur une petite carte graphique : sans allocation, pas d'ombres ;
>   - ombres portées un peu plus fines, à cause du biais normal ;
>   - scintillement en mouvement (O1).
> - **Points ouverts pour l'auteur** :
>   - O1 ;
>   - les validateurs réécrivent `display-settings.json` (résolution, VSync) à chaque run : tâche séparée proposée.

- Objectif : validation globale, passe `verifier` indépendante, rapport de fin, index à jour.
- Commit : `docs(racing-casa): close the HUD, shadows and start light plan`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | **Traité le 2026-10-06** par T2.1 de [rgce-start-beeps-shadow-shimmer-tasks.md](rgce-start-beeps-shadow-shimmer-tasks.md) : la carte d'ombres avance par texels entiers (CasaEngine `7a44aba`). La carte d'ombres suit la caméra sans s'aligner sur les texels : son scintillement en mouvement ne se règle pas sans changer le moteur. Il est noté dans le rapport de fin s'il reste visible. | T3.1 |

## Hors périmètre

- Toute modification de CasaEngine ou de MGUI (D4), dont cascades d'ombres, flou de la carte, alignement sur les texels.
- Les ombres sélectives (choix des objets projeteurs), écartées par D1.
- Un texte de décompte dans le HUD, écarté par D3.
- Les autres sons du jeu legacy.
- Push, merge, PR.
