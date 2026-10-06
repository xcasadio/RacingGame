# Plan agent IA — Sélection de piste au look du jeu XNA d'origine (RacingGameCasaEngine)

Plan d'exécution de la demande de l'auteur du 2026-10-06, faite avec une capture vidéo du jeu d'origine (`images/7.webp`) : « on fait la même chose pour l'écran de sélection de la course », après l'écran titre et la sélection de voiture (plan `rgce-title-car-selection-xna-look-tasks.md`, ADR-0009).

Les décisions D1 → D6 ci-dessous ont été arbitrées avec l'auteur le 2026-10-06 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

La sélection de piste de RGCE devient celle de l'original. Elle montre :
- le fond des menus, le logo qui rebondit et la bande sombre ;
- l'en-tête « SELECT TRACK » ;
- les trois cartes de piste : la sélectionnée grandit en 0,5 s et porte le contour orange avec son libellé dessous, les autres sont atténuées ;
- les boutons A SELECT et B BACK de l'original.

Les entrées et les sons sont ceux de l'original, et la piste proposée par défaut est Advanced.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-06)

Découverte en lecture seule (workflow de 3 agents, dont un contrôle croisé adversarial des faits porteurs), faits porteurs revérifiés directement.

- **Branche** : `remaster`, HEAD `c6e0a94`.
  - Le pointeur du sous-module `CasaEngine` est modifié dans l'arbre de travail (`dd91efe`), hors de ce travail, et n'est jamais indexé.
  - `.serena/` et `log.txt`, non suivis, sont à l'auteur.
- **La capture et le code d'origine concordent.** Bandeau, trois cartes, contour, libellé, bande, A et B tombent sur les formules de `git show 4f840a3^:RacingGame.Shared/GameScreens/TrackSelection.cs`, à 1 unité près, une fois la capture recalée :
  - image 1920×1440 en 4:3, `x_cap = 1,875·x − 12,5`, `y_cap = 2,25·y − 11,3` ;
  - recalage confirmé par corrélation du bandeau, de la carte 3 et de A/B.

  La capture est prise pendant l'animation d'ouverture : cartes à t ≈ 0,12 / 0,86 / 0. Ses autres écarts ne sont pas de la mise en page :
  - post-effet (teinte chaude, halo sur la carte choisie) ;
  - pastille web, curseur et bandeau YouTube.
- **Écran d'origine** (`TrackSelection.cs` de `4f840a3^`, et `UIRenderer.cs`, `BaseGame.cs`, `Input.cs`, `RacingGameManager.cs` de la même révision) :
  - **Ordre de dessin** (`:164-223`) :
    1. `RenderMenuBackground` (fond à 0,85, logo qui rebondit) ;
    2. `RenderBlackBar(220, 280)`, texel (0,0,0,205) à 0,85, soit une opacité de 0,683, dessinée après le logo ;
    3. en-tête `HeaderSelectTrackGfxRect` (0, 312, 512, 100) de `headers.png` par `RenderOnScreenRelative1600(10, 18)` ;
    4. les trois cartes ;
    5. `RenderBottomButtons(false)`.

    Aucun texte (pas de GameFont), aucun meilleur temps.
  - **Cartes** : `TrackButtonBeginner/Advanced/Expert` = `buttons.png` (0|212|424, 480, 212, 352) (`UIRenderer.cs:45-47`).
    - Hauteur active `132·352/212` = 219, inactive `108·352/212` = 179 (divisions entières). Largeur au rapport du sprite en pixels (`CalcRectangleCenteredWithGivenHeight`).
    - `totalWidth = active.W + 2·inactive.W + 2·XToRes(32)`, départ `XToRes(512) − totalWidth/2`, `yPos = YToRes(258)`.
    - Chaque carte vaut `Interpolate(active, inactive, t)` (`MainMenu.cs:223-236`), placée en `(x, yPos − (h − inactive.H)/2)`. La suivante commence à `x + w + XToRes(32)`.
  - **Atténuation** : la carte non sélectionnée est teintée (192,192,192,192) sur une texture en alpha prémultiplié (`Content.mgcb:1607`), soit le sprite entier à une opacité de 0,753.
  - **Contour** : `TrackButtonSelectionGfxRect` (636, 480, 212, 352) est dessiné sur la carte sélectionnée, dans le même rectangle (`:203-205`, `UIRenderer.cs:48`).
  - **Libellé**, sous la carte sélectionnée seulement (`:207-215`) : sprite `TrackText*` (0|212|424, 834, 212, 24), au rectangle `(x carte, bas + YToRes(5), largeur carte, hauteur·24/352 entier)`.
  - **Animation** :
    - `currentButtonSizes = {1, 0, 0}` par instance (`:152`), qui avancent de `MoveFactorPerSecond·2` par image (0,5 s d'un bout à l'autre) ;
    - `selectedButton` est statique et vaut 1, Advanced (`:123`) ;
    - chaque ouverture anime donc de la carte 1 vers la piste retenue.
  - **Entrées** (`:38-115`) :
    - gauche (flèche, croix, stick au-delà de 0,75) → (n+2) % 3 ; droite → (n+1) % 3 ; avec `Sound.ButtonClick` ;
    - le survol d'une carte la sélectionne une fois la souris bougée ;
    - un clic sur une carte la sélectionne et lance la course ;
    - A, Espace ou un clic sur A lancent la course (`ScreenClick`, `RacingGameManager.cs:420-427`) ;
    - Échap, B, Back ou un clic sur B reviennent (`ScreenBack`, `:493-513`) ;
    - `Input.MouseInBox` joue `Highlight` quand la souris entre dans une carte ou dans A/B ;
    - ni Entrée, ni haut ou bas.
  - **Son ButtonClick** : cue → `menu_buttonclick`, volume −300 centièmes de dB, soit 0,708 (`RacingGame/Content/Audio/RacingGame.xap:1109-1110, 2384-2398`). Options et Highscores de l'original l'utilisent aussi.
  - **En 1920×1080, au repos, Advanced sélectionnée** :
    - cartes (607, 435, 182×302), (849, 401, 223×370), (1132, 435, 182×302) ; libellé (849, 779, 223×25) ;
    - bande (0, 371, 1920×472) ; en-tête (12, 16, 614×90) ;
    - A (1396, 951, 187×81), B (1639, 951, 187×81).
- **RGCE aujourd'hui** :
  - `Content/UI/Screens/TrackSelection/TrackSelection.xaml` est en pixels fixes de 1280×720 : bande rgba(0,0,0,132) à 248, hauteur 315 ; trois boutons MGUI avec cadre et libellé `TextBlock` ; aucun en-tête ; A/B de 136×59.
  - `Screens/TrackSelectionScreen.cs` repose sur le focus MGUI. Il n'a pas de son, et `RaceFrontEndFlow.cs:60` ne lui passe pas le jeu.
  - `RaceFrontEndState.SelectedTrackIndex` vaut 0 par défaut (`RaceFrontEndState.cs:9`) et n'est pas enregistré.
  - `UiScreenCaptureValidator.cs:80` et `CarProfileAuditValidator.cs:110` le fixent à 0. `CarTopSpeedAuditValidator`, `TrackMigrationCaptureValidator` et `TrackRuntimeSceneExportValidator` fixent le leur. `FrontEndNavigationSmokeValidator` ne le fixe pas.
  - `LegacyMenuUiTheme.ApplySpriteButtonState` n'est plus utilisé que par cet écran (`TrackSelectionScreen.cs:90-91`).
  - **Déjà disponibles** :
    - les sprites `Ui.Track.Beginner/Advanced/Expert`, `Ui.Button.Select/Back/Highlight`, `Ui.Menu.Background/Logo`, et la texture `Ui.Title.HeadersTexture` (`scripts/generate_rgce_ui_assets.py`) ;
    - `RaceScreenRectViewModel` ;
    - les formules d'en-tête et de A/B de `RaceCarSelectionViewModel` ;
    - l'interpolation de taille de `RaceMainMenuViewModel` ;
    - `MenuSounds` (Highlight, ScreenClick, ScreenBack) ;
    - `RaceFrontEndState.PinScreenAnimations`.
  - **Manquent** : les sprites du contour (636, 480), des trois libellés (…, 834) et de l'en-tête (0, 312) ; le son ButtonClick.
  - `buttons.png` et `headers.png` de RGCE sont identiques pixel à pixel à ceux de RacingGame.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | **Animation d'ouverture comme l'original** : chaque ouverture part de la carte 1 en grand et fait grandir la piste retenue en 0,5 s (auteur, 2026-10-06). |
| D2 | **Advanced par défaut**, comme l'original ; les tests automatiques gardent leur choix explicite de Beginner (auteur, 2026-10-06). |
| D3 | **Son ButtonClick** ajouté, au volume de l'original, pour changer de piste au clavier et à la manette (auteur, 2026-10-06). |
| D4 | **Après la course, retour au menu principal** comme aujourd'hui ; l'écart avec l'original (retour à la sélection de piste) est noté dans l'ADR (auteur, 2026-10-06). |
| D5 | Mêmes règles que le chantier précédent : la capture fait foi si elle diverge du code (ici, elles concordent) ; post-effet, scène 3D derrière les menus, curseur et pastille web hors périmètre (ADR-0009). |
| D6 | CasaEngine et MGUI ne sont pas modifiés ; un manque est documenté (`docs/mgui-gaps-from-rgce-xaml-screens.md`). Plan dans `ai-agent/tasks/` selon `ai-agent/plan-template.md`. |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **Branche** `remaster`, sans push. Le pointeur `CasaEngine` modifié n'est jamais indexé. |
| P2 | **Mise en page** par un view model, chaque image, en pixels d'écran, avec les formules d'origine (arrondi au pair), comme la sélection de voiture :<br>- bande (0, 220, 1024, 280) rgba(0,0,0,174) ;<br>- en-tête `RenderOnScreenRelative1600(10, 18)` ;<br>- cartes, contour et libellé tels que l'original les calcule, rangée comprise : départ calculé au repos, d'où le glissement des cartes pendant l'animation ;<br>- A/B comme la sélection de voiture, grossis et cerclés d'orange au survol.<br>La carte non sélectionnée est une seule image à l'opacité 0,753. Pas de texte. |
| P3 | **Animation** (D1) : taille de chaque carte de 0 à 1 à 2 par seconde ; tailles {1, 0, 0} à chaque ouverture ; libellé et contour suivent la carte sélectionnée dès la première image. Sous capture (`PinScreenAnimations`), les cartes sont au repos. |
| P4 | **Entrées et sons** de l'original :<br>- gauche → (n+2) % 3, droite → (n+1) % 3, avec ButtonClick ;<br>- survol d'une carte → sélection, une fois la souris bougée depuis la dernière touche (`MouseManager.HasMoved`, toutes directions comme le menu principal) ;<br>- clic sur une carte → sélection et départ ;<br>- A, Espace, **Entrée** (comme la sélection de voiture) ou clic sur A → ScreenClick puis course ; Échap, B, Back ou clic sur B → ScreenBack puis sélection de voiture ;<br>- Highlight quand la souris entre dans une carte ou un bouton A/B, une fois par entrée ;<br>- stick à 0,5, sans répétition ; garde de première image ; aucun bouton ne prend le focus MGUI. |
| P5 | **Piste par défaut** (D2) : `RaceFrontEndState.SelectedTrackIndex` vaut 1. `FrontEndNavigationSmokeValidator` fixe explicitement 0, pour que sa course reste Beginner ; les autres validateurs fixent déjà la leur. |
| P6 | **Son** (D3) : `menu_buttonclick.wav` copié, `.sound` au volume 0,708, deux entrées de catalogue, `MenuSound.ButtonClick`, par la voie de T1.3 du chantier précédent. |
| P7 | **ADR-0010**, « RacingGameCasaEngine's track selection reproduces the original XNA screen » : D1 à D5 et P2 à P6. L'ADR-0001 cède sa parité d'états visuels pour cet écran. Le retour au menu principal après la course (D4) est un écart connu. `LegacyMenuUiTheme.ApplySpriteButtonState`, devenu inutilisé, est retiré. |
| P8 | **Validation par tâche** :<br>- `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ;<br>- `--smoke-frontend` et `--capture-ui-screens` code 0 sans avertissement, deux runs identiques ;<br>- rectangles mesurés contre les formules en 1920×1080, 1280×720 et 1024×768 ;<br>- superposition sur la capture de l'auteur (sonde en 1024×768, cartes figées à l'état de la capture) ;<br>- éditeur, `capture_editor_screen.ps1` ;<br>- réglages de l'auteur restaurés à l'identique ;<br>- 🧪 pour l'auteur : animation, entrées, sons. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (P1). Ne jamais committer sur `master`.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Ne jamais pousser.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Aucun projet de test n'existe pour RGCE.
- **Ne jamais modifier** `CasaEngine/` ni `CasaEngine/MGUI/` (D6).
- Si le code est écrit mais qu'une vérification visuelle ou manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** les modifications préexistantes de l'auteur ni celles d'autres sessions : `git add` fichier par fichier ; `.serena/`, `log.txt` et le pointeur `CasaEngine` ne sont jamais indexés.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- **Sondes** : uniquement dans le scratchpad, sur une copie ; jamais d'envoi de touches ni de mouvements de souris simulés.
- **Réglages de l'auteur** (`display-settings.json`, `front-end-options.json`) : sauvegardés avant les runs, restaurés à l'identique après (même SHA-1).
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
- `--smoke-frontend` et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]` ; deux runs de capture identiques.
- Contre `references-c0fdd87` : seul `track-selection` change ; les autres états à 0,00.
- Rectangles mesurés contre les formules en 1920×1080, 1280×720 et 1024×768 ; superposition sur la capture de l'auteur.
- `scripts/capture_editor_screen.ps1` sur `TrackSelection.uiscreen` : code 0, sans erreur.
- Passe de vérification indépendante sur le résultat final.
- Vérification manuelle par l'auteur :
  - animation d'ouverture et de changement ;
  - entrées au clavier, à la manette et à la souris ;
  - sons ;
  - départ de la course sur la bonne piste.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan, index et ADR

- Objectif : versionner ce plan, l'indexer, consigner P7 en ADR-0010.
- Fichiers : ce plan, `ai-agent/README.md`, `docs/decisions/0010-rgce-track-selection-as-original.md`, `docs/decisions/README.md`.
- Validation : fichiers présents, liens valides.
- Commit : `docs(racing-casa): plan the track selection with the original look`

> Validation (2026-10-06) : plan revu par un `plan-verifier` (READY), approuvé par l'auteur en mode AUTO ; ADR-0010 et index écrits.

## Phase 1 — Assets

### ✅ T1.1 — Sprites de la sélection de piste

- Objectif : les sprites qui manquent, catalogués.
- Fichiers : `scripts/generate_rgce_ui_assets.py`, `RacingGameCasaEngine/Content/UI/Sprites/*`, `RacingGameCasaEngine/Content/AssetInfos.json`.
- Étapes :
  1. Ajouter au générateur `Ui.Track.Highlight` (636, 480, 212, 352) et `Ui.Track.LabelBeginner/Advanced/Expert` (0|212|424, 834, 212, 24) de `Ui.Menu.ButtonsTexture`, et `Ui.TrackSelection.Header` (0, 312, 512, 100) de `Ui.Title.HeadersTexture`.
  2. Lancer le générateur deux fois : la seconde exécution ne change rien.
- Validation : chaque sprite remonte à une image existante et tient dans l'image (contrôle par script) ; build ; `--smoke-frontend` code 0.
- Commit : `feat(racing-casa): catalogue the track selection art of the original`

> Validation (2026-10-06) :
> - générateur : `Ui.Track.Highlight` (636, 480, 212, 352), `Ui.Track.LabelBeginner/Advanced/Expert` (0|212|424, 834, 212, 24) et `Ui.TrackSelection.Header` (0, 312, 512, 100) ; 45 sprites, catalogue à 84 entrées ; seconde exécution sans changement ;
> - chaque nouveau sprite remonte à une image existante et tient dans l'image (contrôle par script) ;
> - incident corrigé avant commit : une retouche de commentaire par numéro de ligne avait écrasé l'entrée `Ui.Button.Select`. Elle est rétablie ; le diff du catalogue ne contient que les 5 nouvelles entrées ;
> - `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ; `--smoke-frontend` code 0 sans avertissement ; réglages restaurés à l'identique.

### ✅ T1.2 — Son ButtonClick

- Objectif : P6.
- Fichiers : `RacingGameCasaEngine/Content/Audio/menu_buttonclick.{wav,sound}`, `AssetInfos.json`, `RacingGameCasaEngine/UI/MenuSounds.cs`.
- Validation :
  - build ;
  - `--smoke-frontend` sans avertissement, le `.sound` se charge au démarrage (journal) ;
  - 🧪 écoute par l'auteur.
- Commit : `feat(racing-casa): add the original ButtonClick menu sound`

> Validation (2026-10-06) :
> - `menu_buttonclick.wav` (16 bits, stéréo, 44,1 kHz, 0,44 s) copié. Le `.sound` est au volume 0,708, soit −300 centièmes de dB d'après `RacingGame.xap` (son `menu_buttonclick`, `Volume = -300`). Deux entrées de catalogue (ids uuid5 du nom), bus `Sfx`.
> - `MenuSound.ButtonClick` et `Sound.MenuButtonClick` sont ajoutés à `MenuSounds`.
> - `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ; `--smoke-frontend` code 0 sans avertissement, les quatre `.sound` se chargent au démarrage ; réglages restaurés à l'identique.
> - 🧪 écoute par l'auteur, en T2.1 quand l'écran le joue.

## Phase 2 — Écran

### 🧪 T2.1 — Sélection de piste de l'original

- Objectif : P2 à P5.
- Fichiers :
  - `RacingGameCasaEngine/Content/UI/Screens/TrackSelection/{TrackSelection.xaml,TrackSelection.design.json}` ;
  - `RacingGameCasaEngine.UI/ViewModels/RaceTrackSelectionViewModel.cs` (et un view model de carte si besoin) ;
  - `RacingGameCasaEngine/Screens/TrackSelectionScreen.cs` ;
  - `RacingGameCasaEngine/Bootstrap/{RaceFrontEndFlow.cs,RaceFrontEndState.cs,FrontEndNavigationSmokeValidator.cs}` ;
  - `RacingGameCasaEngine/UI/LegacyMenuUiTheme.cs` (retrait de `ApplySpriteButtonState`).
- Étapes :
  1. View model : formules d'origine en pixels d'écran ; tailles animées {1, 0, 0} à l'ouverture, figées au repos sous capture ; rectangles des cartes, du contour et du libellé de la carte sélectionnée ; A/B avec survol ; tests de position pour la souris sur les rectangles animés.
  2. XAML : fond, logo, bande, en-tête, trois cartes, contour, libellé, A/B et contours, tous `IsHitTestVisible="False"`, sans focus ; données de conception en 1280×720.
  3. Écran : entrées et sons de P4, garde de première image, sortie unique (`_isLeaving`).
  4. Piste par défaut 1 ; validateur de smoke à 0 explicite.
- Validation :
  - build ;
  - `--smoke-frontend` ;
  - `--capture-ui-screens` : seul `track-selection` change, deux runs identiques ;
  - rectangles mesurés contre les formules en 1920×1080, 1280×720 et 1024×768 ;
  - superposition en 1024×768 sur la capture de l'auteur (sonde figeant les tailles à l'état de la capture) ;
  - éditeur ;
  - 🧪 animation, entrées et sons.
- Commit : `feat(racing-casa): draw the track selection like the original`

> Validation (2026-10-06) :
> - **View model** : `RaceTrackSelectionViewModel` et `RaceTrackCardViewModel`, sur les formules de `TrackSelection.Update/Render` :
>   - hauteurs `132·352/212` et `108·352/212` × H/640, largeurs au rapport du sprite, interpolation de `MainMenu.InterpolateRect` ;
>   - départ de la rangée au repos ; libellé `hauteur·24/352` ;
>   - tailles {1, 0, 0} à l'ouverture (`Reset`), avancées de 2 par seconde, au repos sous capture ;
>   - carte non sélectionnée à l'opacité 192/255.
> - **XAML** : fond, logo, bande, en-tête `Ui.TrackSelection.Header`, puis pour chaque carte le sprite, le contour `Ui.Track.Highlight` et le libellé `Ui.Track.Label*` ; A/B et leurs contours. Rien ne capte la souris ni le focus. Données de conception en 1280×720 (Advanced).
> - **Écran** : entrées et sons de P4, garde de première image, sortie unique. Le flux passe le jeu à l'écran. Advanced par défaut (`RaceFrontEndState.SelectedTrackIndex = 1`), Beginner fixé par le smoke. `LegacyMenuUiTheme.ApplySpriteButtonState` est retiré : plus aucun appelant.
> - **Mesures** :
>   - 1920×1080, Beginner (capture automatique) : bande y 371 → 843 ; cartes 2 et 3 en x 891 → 1070 et 1133 → 1312, y 436 → 735 ; contour x 608 → 828, y 402 → 769 ; glyphes « BEGINNER » x 664 → 773, y 784 → 800. Tout est égal aux formules : cartes (607, 401, 223×370), (890, 435, 182×302), (1132, 435, 182×302), libellé (607, 779, 223×25), au pixel transparent du bord des sprites près.
>   - 1280×720 et 1024×768, Advanced (sonde) : bandes y 248 → 563 et 264 → 600, cartes et glyphes du libellé sur les formules.
> - **Superposition** sur la capture de l'auteur : sonde en 1024×768, Advanced, cartes figées aux tailles de la capture (0,12 / 0,86 / 0), recalage `x_cap = 1,875·x − 12,5`, `y_cap = 2,25·y − 11,3` (`ts-overlay.png` du scratchpad). En-tête, cartes, contour, libellé « ADVANCED » et A/B coïncident, sans dédoublement.
> - **Runs** :
>   - deux `--capture-ui-screens` identiques (0,00) ;
>   - contre `references-c0fdd87`, seul `track-selection` change ;
>   - `--smoke-frontend` code 0 sans avertissement ;
>   - build 0 erreur, aucun avertissement dans RGCE ;
>   - réglages restaurés à l'identique.
> - **Éditeur** : `capture_editor_screen.ps1` sur `TrackSelection.uiscreen`, code 0 sans erreur, Advanced sélectionnée selon les données de conception.
> - 🧪 Reste pour l'auteur : l'animation d'ouverture et de changement, chaque entrée (survol, clic sur une carte, gauche et droite, A, Espace, Entrée, Échap, B), les sons (ButtonClick, Highlight, ScreenClick, ScreenBack), le départ sur la bonne piste.

## Phase 3 — Clôture

### 🧪 T3.1 — Validation globale, documentation et rapport de fin

- Objectif :
  - validation globale ;
  - nouvelles références ;
  - `docs/mgui-gaps-from-rgce-xaml-screens.md` (manques rencontrés, compte de sprites de M4) ;
  - passe de vérification indépendante ;
  - rapport de fin et index.
- Validation : liste « Validation globale ».
- Commit : `docs(racing-casa): close the track selection plan`

> Validation globale (2026-10-06) :
> - `dotnet build RacingGame.slnx --no-incremental` : 0 erreur, aucun avertissement dans `RacingGameCasaEngine` ni `RacingGameCasaEngine.UI`.
> - `--smoke-frontend` : code 0, journal sans `[Warning]` ni `[Error]` ; sa course reste sur Beginner (entités `Track.*.Beginner` au journal).
> - `--capture-ui-screens` deux fois de suite (`ui-run-20261006-105305`, `ui-run-20261006-105326`) : 0,00 sur les 10 états.
> - Contre `references-c0fdd87` :
>   - `track-selection` change (37,69 / 33,33 / 28,43) ;
>   - `options` 0,04 : 5 568 pixels sur le bouton 1920x1080. L'écran Options n'a pas changé (`git diff c0fdd87..HEAD` vide) et il se restyle au survol (`OptionsScreen.cs:145`) : la vraie souris était sur ce bouton pendant l'un des runs. Le vérificateur n'a pas cet écart contre `references-c0fdd87`, mais il le retrouve contre `references-5e6d95d` ;
>   - les autres états sont à 0,00.
> - Nouvelles références : `references-5e6d95d` (copie de `ui-run-20261006-105326`).
> - Mesures et superposition : note de T2.1.
> - Éditeur sans erreur : note de T2.1.
> - Documentation : `docs/mgui-gaps-from-rgce-xaml-screens.md`, M1 (rencontré désormais par Options, Help et Highscores) et M4 (45 sprites sur 7 textures, ligne de synthèse comprise) ; aucun nouveau manque rencontré.
> - **Vérification indépendante** (workflow, deux angles) :
>   - fidélité au code d'origine, en lecture seule : CONFIRMED sur les 6 points ;
>   - exécution et régressions : CONFIRMED sur les 5 points.
>
>   Leurs remarques :
>   - P3 : la ligne de synthèse M4 disait encore 34 sprites. Corrigé dans ce commit ;
>   - P4 : le test de position de la souris sur les cartes utilise les rectangles de l'image précédente (une image de retard pendant l'animation). Reporté ;
>   - P4 : entre la validation et le chargement différé de la course, l'écran lit encore les entrées. Deux validations dans la même image pourraient relancer la course ; la sélection de voiture a le même schéma. Reporté ;
>   - P4 : une vraie souris sur A ou B pendant la capture automatique change l'image, comme pour Options. Reporté.
>
> Rapport de fin :
> - **Sélection de piste** :
>   - en-tête « SELECT TRACK », bande et trois cartes aux formules de l'original (au pixel près en 1920×1080, 1280×720 et 1024×768, et sur la capture de l'auteur) ;
>   - la carte choisie grandit en 0,5 s depuis la carte 1 à chaque ouverture, avec son contour orange et son libellé ; les autres sont atténuées ;
>   - A et B avec leur contour au survol ;
>   - entrées et sons de l'original (ButtonClick, Highlight, ScreenClick, ScreenBack), plus Entrée ;
>   - Advanced par défaut.
> - **Assets** : contour, trois libellés, en-tête ; son ButtonClick au volume de l'original.
> - **Décision** : ADR-0010.
> - **Écarts connus** (ADR-0010) :
>   - retour au menu principal après la course (choix de l'auteur) ;
>   - post-effet, scène 3D derrière les menus, curseur et pastille web non reproduits.
> - **Points ouverts pour l'auteur** :
>   - vérifications manuelles (T2.1 et T3.1 🧪, écoute de ButtonClick de T1.2) ;
>   - les trois remarques P4 ci-dessus ;
>   - ButtonClick n'est pas encore branché dans Options et Highscores (hors périmètre).

## Phase 4 — Retour de l'auteur (2026-10-06)

Demande de l'auteur, avec une capture de la carte sélectionnée :
- « il y a des pixels jaunes clair (ou blanc ?) au niveau des arrondis, c'est quoi ? » ;
- « quand le bouton beginner s'agrandit il "pousse" les autres boutons vers la droite or les boutons doivent toujours garder leur position et s'agrandir selon leur centre sans perturber les autres ».

État vérifié (2026-10-06) :
- **Les pixels clairs** viennent de l'alpha non prémultiplié.
  - MGUI dessine ses images en mélange prémultiplié (`BlendState.AlphaBlend` : `DrawSettings.cs:82`, `CasaMonoGameRenderInterop.cs:19-20`).
  - CasaEngine charge les PNG sans les prémultiplier (`Texture2D.FromStream`, `Texture2DLoader.cs:12`), alors que l'original les prémultipliait à la compilation (`Content.mgcb:1607`, `PremultiplyAlpha=True`).
  - Un pixel de bord semi-transparent ajoute donc sa couleur entière au lieu de la pondérer par son alpha. Au coin de la carte, le bord gris du sprite de carte (188, 188, 188, alpha 68) puis le bord orange du contour (248, 152, 0, alpha 68) donnent (255, 255, ~146) ; la capture mesure (255, 255, 162) en (641, 402).
  - Toutes les images d'interface ont ce défaut sur leurs bords semi-transparents : logo, en-têtes, cartes, libellés, boutons, flèches, cases, HUD, page de la police GameFont. Les glyphes noirs du menu principal ne l'ont pas : noir × alpha = noir.
  - Les PNG concernés (`Content/Textures/{background,buttons,ingame,headers,ColorSelection,OptionsScreenWindows}.png`, `Content/UI/Fonts/GameFont.png`) ne servent qu'aux `.texture` de l'interface et à la police (relevé du catalogue). `scripts/MenuIconExtractor` lit `buttons.png` en entrée.
- **Le glissement des cartes** est celui du code d'origine : `xPos += thisRect.Width + gap` (`TrackSelection.cs:79`, `:217`). L'auteur demande des cartes à position fixe, qui grandissent autour de leur centre.

| Réf | Proposition |
|---|---|
| P9 | **Centres fixes** : chaque carte garde le centre qu'elle a dans la rangée au repos avec Advanced sélectionnée (en 1920×1080, x 698, 960,5 et 1223) et grandit autour de lui, en x comme en y (`x = round(centre − w/2)`). Le libellé suit la carte. Écart voulu avec l'original, consigné dans l'**ADR-0011**, qui remplace l'ADR-0010. |
| P10 | **Images d'interface prémultipliées** : un petit outil C# versionné, `scripts/UiTexturePremultiplier` (`System.Drawing`, comme `MenuIconExtractor`), écrit des copies prémultipliées, `rgb·a/255` arrondi et alpha inchangé, dans `Content/UI/Textures/`. Le générateur fait pointer les `.texture` de l'interface vers ces copies, et la page de la police GameFont est prémultipliée. Les PNG d'origine restent inchangés : l'extracteur de glyphes les lit toujours. Consigné dans l'**ADR-0012** et en C5 de `docs/mgui-gaps-from-rgce-xaml-screens.md`. Toutes les captures de menu changent légèrement aux bords ; les intérieurs opaques ne bougent pas. |

### 🧪 T4.1 — Cartes à centre fixe

- Objectif : P9.
- Fichiers : `RacingGameCasaEngine.UI/ViewModels/RaceTrackSelectionViewModel.cs`, `TrackSelection.design.json` si les valeurs changent, `docs/decisions/0011-…`, `0010-…` (statut), `docs/decisions/README.md`.
- Validation :
  - build ;
  - capture en 1920×1080, Beginner sélectionnée : carte 1 en x 586 (223 de large, centre 698) ; carte 2 en x 870 (182 de large, centre 960,5) ; carte 3 en x 1132, comme avec Advanced ;
  - deux runs identiques ;
  - sonde figeant l'animation à mi-course : les centres ne bougent pas ;
  - 🧪 animation par l'auteur.
- Commit : `fix(racing-casa): grow the track cards around fixed centres`

> Validation (2026-10-06) :
> - `RaceTrackSelectionViewModel` calcule les centres de la rangée au repos avec la carte du milieu sélectionnée, et place chaque carte en `round(centre − w/2)`. La hauteur était déjà centrée.
> - Capture en 1920×1080, Beginner sélectionnée, mesurée sur la ligne y 450 (les bords arrondis réduisent les largeurs mesurées) : centres des cartes 697, 960,5 et 1222,5. La carte 3 est en x 1132 → 1313 comme avec Advanced ; avant, Beginner poussait les cartes 2 et 3 vers la droite.
> - Sonde figeant les tailles à mi-animation (0,5 / 0,5 / 0) : centres 697,5, 960,5 et 1222,5, inchangés.
> - Deux runs `--capture-ui-screens` identiques ; `--smoke-frontend` code 0 sans avertissement ; build 0 erreur, aucun avertissement dans RGCE ; réglages restaurés à l'identique.
> - Avec Advanced au repos, la disposition est identique à l'original, et les données de conception ne changent pas.
> - ADR-0011 remplace l'ADR-0010 ; les références du code pointent vers l'ADR-0011.
> - 🧪 Reste pour l'auteur : l'animation.

### ⏳ T4.2 — Images d'interface prémultipliées

- Objectif : P10.
- Fichiers : `scripts/UiTexturePremultiplier/*` (nouveau), `scripts/generate_rgce_ui_assets.py`, `scripts/generate_rgce_gamefont.py`, `RacingGameCasaEngine/Content/UI/Textures/*.png` (nouveaux), `Content/UI/Fonts/GameFont.png`, `AssetInfos.json`, `docs/decisions/0012-…`, `docs/decisions/README.md`, `docs/mgui-gaps-from-rgce-xaml-screens.md`.
- Validation :
  - outil déterministe (seconde exécution identique au bit près) ;
  - les pixels opaques sont identiques à l'original, les semi-transparents valent `rgb·a/255` ;
  - capture : au coin de la carte sélectionnée, plus de pixel plus clair que l'orange du contour, et les intérieurs opaques sont inchangés (comparaison masquée) sur tous les états ;
  - smoke, deux runs identiques, éditeur ;
  - 🧪 rendu par l'auteur.
- Commit : `fix(racing-casa): premultiply the UI images as RacingGame did`

### ⏳ T4.3 — Clôture du retour

- Objectif : nouvelles références, passe de vérification indépendante, rapport, index.
- Commit : `docs(racing-casa): close the track selection feedback`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| — | Aucun à l'écriture du plan. | — |

## Hors périmètre

- Le retour à la sélection de piste après la course (D4).
- Le post-effet de l'original (teinte chaude, halo de la carte choisie), la scène 3D derrière les menus, le curseur de l'original et la pastille web (D5, ADR-0009).
- Les autres écrans (options, aide, meilleurs scores, pause) ; ButtonClick est ajouté mais n'y est pas branché.
- Toute modification de CasaEngine ou de MGUI (D6).
