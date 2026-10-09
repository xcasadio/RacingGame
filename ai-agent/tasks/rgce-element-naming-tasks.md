# Plan agent IA — Nommer chaque élément d'écran, de monde et chaque composant de RacingGameCasaEngine

Plan d'exécution de la demande de l'auteur du 2026-10-09 : « Il faut nommer correctement chaque élément dans tout le projet. Par exemple, dans l'image, chaque élément de l'UI peut être nommé. Il faut faire pareil pour chaque asset et élément contenu dans un asset (par exemple un world). » L'image montrait la hiérarchie de `CarSelection.uiscreen` dans l'éditeur, avec des dizaines de lignes « Image » et « Border » sans nom.
La partie moteur (noms donnés par l'éditeur à ce qu'il crée) est le plan `CasaEngine/ai-agent/tasks/editor-default-names-tasks.md`. Ce plan-ci en reprend le résultat par la mise à jour du pointeur (T3.1).
Les décisions D1 → D4 ci-dessous ont été arbitrées avec l'auteur le 2026-10-09 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

- Chaque élément MGUI des 9 écrans porte un nom selon la convention de CasaEngine, unique dans son écran. Les noms déjà référencés par le code sont harmonisés avec elle, avec les chaînes C# qui les cherchent.
- Les 6 composants sans nom du monde de test `Worlds/Test/TrackBeginnerGenerators.world` sont nommés.
- Chaque composant créé par le code de RacingGameCasaEngine porte un nom. Il ne s'affiche plus `Object <guid>` dans l'éditeur ni dans les exports de scène. Il le garde quand son entité est clonée, grâce au correctif du plan CasaEngine (P8), vérifié ici en T3.1.
- Un script versionné vérifie les noms des écrans.
- Le pointeur CasaEngine passe aux noms par défaut de l'éditeur.
- Une ADR est écrite.

## État vérifié du dépôt (2026-10-09)

Branche `remaster`, tête `6bc0b75`. Changements préexistants de l'auteur à ne jamais indexer : `.serena/`, `log.txt` (non suivis).

- **Écrans** (`RacingGameCasaEngine/Content/UI/Screens/*/*.xaml`) :
  - 275 éléments, dont 92 nommés, comptés élément par élément (relevé en lecture seule du 2026-10-09) ;
  - par écran (éléments / nommés) : TrackSelection 19/0, CarSelection 38/7, MainMenu 41/15, Splash 6/0, Help 20/12, Pause 10/2, RaceHud 48/0, Options 51/23, Highscores 42/33 ;
  - types : Image 108, TextBlock 100, Border 24, OverlayPanel 22, Window 9 (racines), Button 7, StackPanel 3, et 2 objets `RenderTransform` (`CarSelection.xaml:65`, `RaceHud.xaml:95`) ;
  - `RenderTransform` n'est pas un élément MGUI : `public class RenderTransform` (`CasaEngine/MGUI/MGUI.Core/UI/XAML/Animation.cs:238`) n'hérite pas de `Element` (`UI/XAML/Element.cs:19`, qui porte `Name` à `:193`). Il reste donc 273 éléments à nommer.
- **Unicité** : MGUI indexe les noms par fenêtre et lève `MGDuplicateElementNameException` sur un doublon (`CasaEngine/MGUI/MGUI.Core/UI/MGWindow.cs:2115-2125`). La comparaison est ordinale (`:1705`). Chaque écran a une seule `Window` racine.
- **Affichage dans l'éditeur** : la hiérarchie montre `Type Nom`, ou le type seul sans nom (`CasaEngine/CasaEngine.Editor/Controls/UIScreenHierarchyPanel.cs:292-294`).
- **Noms cherchés par le code** (`RacingGameCasaEngine/Screens/`) :
  - `PauseScreen.cs:37,39` : `btnResume`, `btnMainMenu` ;
  - `MainMenuScreen.cs:28,69` : `"btn" + EntryNames[i]` ;
  - `HelpScreen.cs:62-64` : `$"txtSection{i}Title"`, `Line0`, `Line1` ;
  - `CarSelectionScreen.cs:32` : tableau `txtColorLabel`, `txtStat0` à `txtStat5`, utilisé à `:75-78` ;
  - `OptionsScreen.cs:85-91` : `txtResolution`, `txtArcade`, `txtSimulation`, `txtLabel`, `txtName`, `txtSoundValue`, `txtMusicValue`, `txtSensitivityValue`. `FindTexts` (`:335-340`) ajoute l'indice au préfixe ;
  - `HighscoresScreen.cs:67-70` : préfixes `txtTab`, `txtRank`, `txtName`, `txtTime`. `FindTexts` est à `:189-194`.
  - Les `face*` et `lbl*` de `MainMenu.xaml` ne sont cherchés par personne.
  - Les `.uiscreen` et `.design.json` ne contiennent ni nom ni chemin d'élément.
  - Aucun projet de test hors du sous-module. `scripts/UiCaptureCompare` et `Bootstrap/UiScreenCaptureValidator.cs` ne dépendent pas des noms.
- **Monde de test** (`RacingGameCasaEngine/Content/Worlds/Test/TrackBeginnerGenerators.world`, écrit à la main dans la forme de l'éditeur, commit `792437b`) :
  - les composants racines des entités `Track.Road.Beginner`, `Track.GuardRail.Left.Beginner`, `Track.GuardRail.Right.Beginner`, `Track.Columns.Beginner`, `Track.Ground.Beginner` et `Light` s'appellent `Object <guid>` (`jq` sur le fichier) ;
  - le composant de `Track.Physics` s'appelle `RaceTrackPhysicsComponent` ;
  - `Worlds/Editor.world` n'a qu'une entité nommée et aucun composant.
- **Entités créées par le code** : toutes nommées. Sites :
  - `RaceWorldFactory.cs:143,166,217,238,260,280` ;
  - `RaceCarEntityFactory.cs:31` ;
  - `LegacyTrackSceneFactory.cs:72,280` ;
  - `CarSelectionCarousel.cs:112,119-120,123`.
- **Composants créés par le code** (`rg "new \w+Component\b"`, constructeurs de copie et `Clone` mis à part). Sans nom :
  - `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` : `:149` CameraLookAt, `:155` ChaseCameraRig, `:156` DebugFreeCamera, `:169` Light (racine des lumières), `:220` PlayerStart, `:248` RaceTrackPhysics, `:253` RaceFlowCoordinator, `:254` RaceCourseDebug, `:263` RaceCheckpointTrigger (racine des checkpoints) ;
  - `RacingGame.Gameplay/Entities/RaceCarEntityFactory.cs:38-42` : VehicleDynamics, LegacyCarVisual, VehicleWheelVisual, DebugCarVisual, RaceCarState ;
  - `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs` : les générateurs racines (`:38`, `:39`, `:43`, `:44`, `:49`), et `:286` le StaticModel de chaque décor ;
  - `RacingGameCasaEngine/UI/CarSelectionCarousel.cs` : `:104` Light, `:124` CameraLookAt, `:331` StaticModel des plateaux et des voitures.
  Déjà nommés par leur rôle :
  - les ancres `RacingCarAnchorComponent` (`RaceCarEntityFactory.cs:48-53,66`) ;
  - `BodyVisual` (`:82-84`) ;
  - les enfants générés (`RaceTrackGeneratorComponent.cs:182-184`).
- **Clones** : un composant cloné perd aujourd'hui son nom, quelle que soit la forme de son `Clone()`.
  - `EntityComponent(EntityComponent other)` (`CasaEngine/CasaEngine/Framework/Scene/Entities/Components/EntityComponent.cs:31-34`) n'appelle pas `ObjectBase(ObjectBase other)`, le seul constructeur qui copie `Name` (`ObjectBase.cs:37-41`). Les constructeurs de copie des composants du jeu passent par lui, par exemple `RaceTrackGeneratorComponent.cs:50`, `RaceTrackPhysicsComponent.cs:27`, `RacingCarAnchorComponent.cs:14`, `VehicleDynamicsComponent.cs:39`.
  - 8 `Clone()` du jeu construisent un objet neuf sans le copier : `ChaseCameraRigComponent`, `DebugCarVisualComponent`, `LegacyCarVisualComponent`, `DebugFreeCameraComponent`, `RaceCarStateComponent`, `RaceFlowCoordinatorComponent`, `RaceCourseDebugComponent`, `VehicleWheelVisualComponent` (`RacingGame.Gameplay/Components/`).
  - Un composant n'est cloné qu'à trois endroits, tous dans CasaEngine : `Entity.cs:171`, `:178` et `SceneComponent.cs:303` (`rg "\.Clone\(\)"`). Le plan CasaEngine (P8, T1.3) y garde le nom, sans toucher aux composants du jeu.
  - Le monde de course est construit par le code et n'est pas cloné. La duplication d'une entité dans l'éditeur et l'instanciation d'une entité depuis un asset clonent (`World.cs:156`, `:163`).
- **Recherches par nom** : celles du code portent sur les **entités** (`PlayerStart`, `Checkpoint.`, `Track.*`, `BootstrapCamera`, `Light.Key`, `PlayerCar`), sauf une sur un composant : `VehicleWheelVisualComponent.cs:101-110` compare le nom d'un composant à `VisualFrameName` (valeur passée à `VehicleDynamicsContracts.cs:171`). Ce plan ne renomme aucune entité.
- **Exports de scène** :
  - `TrackRuntimeSceneExporter` écrit `ComponentPath`, les noms des composants joints par `/` (`Bootstrap/TrackRuntimeSceneExporter.cs:144`, `:222`) ;
  - `compare_race_scene_exports.py` ne met ni le nom d'entité ni `ComponentPath` dans sa clé (docstring `:3`) ;
  - dans `artifacts/o7-track-helpers/after/scene-a.json`, 5 118 des 5 154 `ComponentPath` contiennent un segment `Object <guid>` (`rg -c`).
- **Outils** :
  - `scripts/capture_editor_screen.ps1 -Screen UI/Screens/<Name>/<Name>.uiscreen` capture un écran dans l'éditeur (`RacingGameCasaEngine/Assets/README.md:62`) ;
  - l'éditeur a `--open-asset` et `--save-project` (aller-retour du monde de test, plan éditeur S5, `rgce-editor-project-tasks.md:1100-1125`) ;
  - le jeu a les modes `--capture-ui-screens`, `--export-track-runtime-scene` et `--smoke-frontend` ;
  - `scripts/check_gameplay_type_names.py` est le modèle d'un script de contrôle versionné.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Convention de CasaEngine pour les éléments d'écran (`btn`, `lbl` pour les TextBlock, `img`, `pnl`, …). Les 92 noms existants sont harmonisés, et les chaînes C# des écrans suivent dans le même commit. |
| D2 | Périmètre : les écrans, les mondes (entités et composants) et les composants créés par le code. Les noms des matériaux et des nœuds ou meshes des modèles cuits ne changent pas. |
| D3 | Si un asset est renommé, son id devient l'uuid5 du nouveau nom. Aucun asset n'est renommé dans ce plan (D2), donc aucun id ne change. |
| D4 | L'éditeur donne un nom par défaut à ce qu'il crée (plan CasaEngine). Pas d'avertissement du validateur. |

## Propositions à valider avec le plan

| Réf | Proposition |
|---|---|
| P1 | **Préfixes**, ceux du plan CasaEngine (P1) : `wnd` Window, `pnl` OverlayPanel, StackPanel et Border, `img` Image, `lbl` TextBlock, `btn` Button. |
| P2 | **Forme d'un nom** : préfixe, puis le rôle en PascalCase, puis un indice quand le rôle se répète.<br>- Le rôle vient du dernier segment du sprite (`Ui.Menu.Logo` → `imgLogo`), du chemin lié (`Swatch3.Rect.Margin` → `imgSwatch3`) ou du commentaire qui précède.<br>- Les éléments communs aux écrans de menu ont le même nom partout : `wnd<Écran>`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgHeader`, `imgBackButton`, `imgBackHighlight`, `imgSelectButton`, `imgSelectHighlight`. |
| P3 | **Noms existants** (D1) : `btn*` inchangés ; `txt*` → `lbl*` à rôle égal, sauf trois rôles redondants ou vagues : `txtColorLabel` → `lblCarColor`, `txtLabel0..11` → `lblRow0..11` (Options), `txtName` → `lblPlayerName` (Options). Dans MainMenu, `face<X>` (Border) → `pnl<X>Face` et `lbl<X>` (Image) → `img<X>Label`. 85 noms changent, 7 restent. |
| P4 | **Composants** : le nom de leur type, comme le nom par défaut de l'éditeur (plan CasaEngine P4), par exemple `LightComponent`, `RaceRoadGeneratorComponent`. Dans le code : `Name = nameof(Type)`. Les composants déjà nommés par leur rôle (ancres, `BodyVisual`, enfants générés) ne changent pas. |
| P5 | **Clones** : aucun changement dans les composants du jeu. Le correctif central du plan CasaEngine (P8 : les trois sites de clonage copient le nom) couvre toutes les formes de `Clone()`. La sonde de T3.1 le vérifie sur chaque type nommé par le code. |
| P6 | **Contrôle** : `scripts/check_ui_element_names.py` vérifie trois règles dans les 9 écrans et sort en code 1 sur un écart :<br>- chaque élément a un `Name` (les éléments de propriété `<X.Y>` et `RenderTransform` ne comptent pas) ;<br>- son préfixe est celui de son type (P1) ;<br>- il est unique dans son fichier. |
| P7 | **Références « avant »** prises à la tête actuelle, en T0.1, dans `artifacts/element-naming/before/`. Nommer ne change aucun pixel : chaque capture « après » se compare à ces références. |

### Noms des éléments d'écran (P2, P3)

`=` : nom inchangé ; `←` : ancien nom ; `i` : indice de la colonne.

| Écran | Noms dans l'ordre du document |
|---|---|
| Splash (6) | `wndSplash`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgPressStart` |
| Pause (10) | `wndPause`, `pnlFrame` (Border), `pnlContent` (StackPanel), `lblTitle` (« Paused »), `lblSummary`, `lblHint`, `btnResume` =, `lblResume`, `btnMainMenu` =, `lblMainMenu` |
| MainMenu (41) | `wndMainMenu`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `pnlButtons` (StackPanel). Puis, pour X = Play, Highscores, Options, Help, Quit : `pnl<X>Slot` (OverlayPanel extérieur), `pnl<X>Body` (OverlayPanel intérieur), `btn<X>` =, `pnl<X>Face` ← `face<X>`, `img<X>Glyph`, `pnl<X>Ring`, `img<X>Label` ← `lbl<X>` |
| Help (20) | `wndHelp`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgHeader`, `imgBackButton`, `imgBackHighlight`, `lblSection<i>Title`, `lblSection<i>Line0`, `lblSection<i>Line1` ← `txt…` (i = 0..3) |
| TrackSelection (19) | `wndTrackSelection`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgHeader`, `imgCard<i>`, `imgCard<i>Highlight`, `imgCard<i>Label` (i = 0..2), `imgSelectButton`, `imgSelectHighlight`, `imgBackButton`, `imgBackHighlight` |
| CarSelection (37) | `wndCarSelection`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgHeader`, `imgSwatch0..10`, `imgStatBar0..5`, `imgLeftArrow`, `imgRightArrow`, `imgSelectButton`, `imgSelectHighlight`, `imgBackButton`, `imgBackHighlight`, `imgCar` (image du carrousel), `lblCarColor` ← `txtColorLabel`, `lblStat0..5` ← `txtStat0..5` |
| Options (51) | `wndOptions`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgHeader`, `imgNameField`. Pour X = Fullscreen, VSync, PostEffects, Shadows, ShowFps, Vibration : `img<X>Button` (gris, `Visibility`), `img<X>Highlight` (orange, `HighlightVisibility`). Puis `imgSoundTrack`, `imgSoundHandle`, `imgMusicTrack`, `imgMusicHandle`, `imgSensitivityTrack`, `imgSensitivityHandle`, `imgSelectionArrow`, `imgBackButton`, `imgBackHighlight`, `lblRow0..11` ← `txtLabel0..11`, `lblPlayerName` ← `txtName`, `lblResolution0..4` ← `txtResolution0..4`, `lblArcade`, `lblSimulation`, `lblSoundValue`, `lblMusicValue`, `lblSensitivityValue` (← `txt…`) |
| Highscores (42) | `wndHighscores`, `pnlRoot`, `imgBackground`, `imgLogo`, `pnlBand`, `imgHeader`, `pnlSeparator` (Border), `imgBackButton`, `imgBackHighlight`, `lblTab0..2`, puis par ligne i = 0..9 `lblRank<i>`, `lblName<i>`, `lblTime<i>` (← `txt…`) |
| RaceHud (47) | `wndRaceHud`, `pnlRoot`.<br>Tours : `pnlLaps` (Border), `pnlLapsContent`, `imgLaps`, `imgLapDigit0`.<br>Temps : `pnlTimes`, `pnlTimesContent`, `imgCurrentAndBest`, `lblCurrentLapTime`, `lblBestLapTime`.<br>Piste et cinq meilleurs : `pnlTrack`, `pnlTrackContent`, `imgTrackName`, `lblTrackName`, puis pour i = 0..4 `imgTopRow<i>`, `lblTopRank<i>`, `lblTopTime<i>`.<br>Compte-tours : `pnlTacho`, `pnlTachoContent`, `imgTacho`, `imgNeedle`, `imgSpeedDigit0..2`, `imgGearDigit0`.<br>Fin de course : `pnlGameOver`, `pnlGameOverLines`, `lblGameOverTitle`, `lblGameOverLine0..4`, `lblExitHint` |

Total : 273 noms (6 + 10 + 41 + 20 + 19 + 37 + 51 + 42 + 47).

## Règles d'exécution pour l'agent

- **Branche `remaster`**, celle du chantier de l'éditeur. Ne jamais committer sur `master`.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`. Les commandes de commit commencent par `cd D:/development/repo/RacingGame &&`.
- **Ne jamais pousser.** Le merge sur `master` reste une décision humaine.
- **Ne rien inventer** : sinon ⚠️ Blocked, question dans « Points ouverts », arrêt.
- **Build** `dotnet build RacingGame.slnx` à 0 erreur avant de passer une tâche de code en ✅.
- **Un seul processus du jeu ou de l'éditeur à la fois**, y compris entre agents. Le jeu se lance par l'aide qui sauvegarde et restaure les réglages de l'auteur (`run_rgce.sh` du bloc-notes) ; l'éditeur seulement sur des copies du contenu. Aucun clavier ni souris simulés.
- **Ne jamais indexer** `.serena/` ni `log.txt` : `git add` fichier par fichier.
- **Langue** : ce plan en français ; code, messages de commit, docs de `docs/` et ADR en anglais.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` à 0 erreur.
- `python scripts/check_ui_element_names.py` : code 0, 273 éléments nommés dans 9 écrans.
- `rg -n '"txt|"face' RacingGameCasaEngine/Screens` ne rend rien.
- Comparaisons avec les références de T0.1 :
  - `--capture-ui-screens` puis `scripts/UiCaptureCompare` : les 10 états dans le seuil de 2,0 par canal ;
  - `--smoke-frontend` : code 0, journal sans `[Warning]` ni `[Error]` ;
  - `--export-track-runtime-scene` puis `compare_race_scene_exports.py` : enregistrements identiques, et aucun `ComponentPath` avec `Object <guid>` (`rg -c 'Object [0-9a-f-]{36}'` → 0).
- Le monde de test s'ouvre dans l'éditeur, et son aller-retour par `--save-project` donne les mêmes octets que le fichier committé.
- La sonde de clonage de T3.1 garde le nom de chaque composant cloné.
- Passe `verifier` fraîche à la fin (T3.3).
- Coup d'œil de l'auteur : la hiérarchie de chaque écran dans l'éditeur ne montre plus de ligne sans nom.

---

## Phase 0 — Références

### ✅ T0.1 — Références avant les noms

- Objectif : les références de P7, à la tête `6bc0b75`.
- Fichiers : `artifacts/element-naming/before/` (`ui/`, `ui-run.txt`, `scene.json`, `smoke.txt`) ; ce plan, son entrée dans `ai-agent/README.md`.
- Étapes :
  1. Build du jeu.
  2. Lancer, chaque mode à part, par l'aide de lancement : `--capture-ui-screens`, `--export-track-runtime-scene`, `--smoke-frontend`.
  3. Copier les 10 captures, l'export et les journaux dans `artifacts/element-naming/before/`.
- Validation :
  - les 10 captures sont présentes ;
  - l'export se compare sans écart à `artifacts/o7-track-helpers/after/scene-a.json` (`compare_race_scene_exports.py`). Un écart doit être expliqué par le pointeur `6bc0b75`, sinon ⚠️ Blocked ;
  - les réglages de l'auteur sont restaurés à l'identique.
- Commit : `test(racing-casa): record the references before naming the elements`

> Validation (2026-10-09) :
> - `dotnet build RacingGame.slnx` : 0 erreur. CasaEngine à `7a5320b6a`, le commit du pointeur.
> - `--smoke-frontend`, `--capture-ui-screens` et `--export-track-runtime-scene`, lancés chacun à part : code 0, aucun avertissement ni erreur, réglages de l'auteur restaurés à l'identique.
> - 10 captures (`ui-run-20261009-094556`).
> - `scene.json` contre `artifacts/o7-track-helpers/after/scene-a.json` : 0 enregistrement sans partenaire sur les trois pistes (`cmp-scene-vs-o7.txt`). Il contient 5 118 segments `Object <guid>`.
> - Les captures contre celles de O7 sont dans le seuil (`cmp-ui-vs-o7.txt`, au plus 0,89). Ce sont les écarts habituels des zones animées.
> - `artifacts/` est ignoré par git (`.gitignore:45`), comme les références de O7 : les références restent locales, dans `artifacts/element-naming/before/`. Le commit ne contient que ce plan et l'index.

## Phase 1 — Écrans

### ✅ T1.1 — Noms existants harmonisés avec la convention

- Objectif : P3, dans les XAML et dans le code qui les cherche.
- Fichiers :
  - `RacingGameCasaEngine/Content/UI/Screens/{MainMenu,Help,CarSelection,Options,Highscores}/*.xaml` ;
  - `RacingGameCasaEngine/Screens/{HelpScreen,CarSelectionScreen,OptionsScreen,HighscoresScreen}.cs`.
- Étapes :
  1. Renommer les 85 noms de la table (colonne `←`).
  2. Mettre à jour les chaînes et préfixes de l'état vérifié : `HelpScreen.cs:62-64`, `CarSelectionScreen.cs:32`, `OptionsScreen.cs:85-91`, `HighscoresScreen.cs:67-70`, ainsi que les commentaires qui citent les anciens noms. `MainMenuScreen` et `PauseScreen` ne changent pas (`btn*`).
- Validation :
  - build ;
  - `rg -n '"txt|"face|txtSection|txtStat|txtTab' RacingGameCasaEngine` ne rend rien hors de `bin/` et `obj/` ;
  - `--smoke-frontend` : code 0, sans avertissement. Un nom cherché et absent y laisse un avertissement ou une erreur de chargement ;
  - `--capture-ui-screens`, puis `UiCaptureCompare` contre T0.1 : 10 états dans le seuil.
- Commit : `refactor(racing-casa): name the referenced screen elements after the CasaEngine convention`

> Validation (2026-10-09) :
> - 85 attributs `Name` changés dans les 5 XAML (`git diff` : 85 lignes), rien d'autre ; 4 écrans C# mis à jour (`CarSelectionScreen`, `HelpScreen`, `HighscoresScreen`, `OptionsScreen`). Aucun commentaire ne citait les anciens noms ; `MainMenuScreen` et `PauseScreen` cherchent `btn*` et ne changent pas.
> - `dotnet build RacingGame.slnx` : 0 erreur. `rg -n '"txt|"face|txtSection|txtStat|txtTab' RacingGameCasaEngine` (hors `bin/` et `obj/`) : rien.
> - `--smoke-frontend` : code 0, aucun avertissement ni erreur, réglages restaurés à l'identique (`artifacts/element-naming/t1.1/smoke.txt`).
> - `--capture-ui-screens` contre `before/ui` : 9 états à 0,00 ; `race-finished` à 13,99 (au-dessus de 2,0), identique à la relance (`t1.1b`). Cause : l'arrière-plan 3D (caméra et ombres), pas l'interface. Le sous-module CasaEngine est maintenant à `6ed0ef3f7`, alors que les références datent de `7a5320b6a`. Preuve : le contrôle `t1.1-ctrl` (mes modifications retirées, même sous-module) donne le même 13,99 contre `before/ui`, et `t1.1-ctrl` contre `t1.1b` donne 0,00 sur les 10 états. Les noms ne changent donc aucun pixel. Les références devraient être refaites au pointeur de T3.1 (voir Points ouverts).
> - Captures locales dans `artifacts/element-naming/{t1.1,t1.1b,t1.1-ctrl}/` (ignoré par git).

### 🧪 T1.2 — Tous les éléments nommés, et le contrôle versionné

- Objectif : les 273 noms de la table et le script de P6.
- Fichiers :
  - les 9 `RacingGameCasaEngine/Content/UI/Screens/*/*.xaml` ;
  - `scripts/check_ui_element_names.py` (nouveau) ;
  - `RacingGameCasaEngine/Assets/README.md` (une ligne sur la règle et le script).
- Étapes :
  1. Écrire le script (P6), sur le modèle de `scripts/check_gameplay_type_names.py` : il liste les éléments sans nom, au mauvais préfixe ou en double, et sort en code 1 s'il y en a.
  2. Le lancer avant les noms : il doit échouer et lister 181 éléments sans nom, ce qui montre qu'il détecte le défaut.
  3. Ajouter un attribut `Name` à chaque élément sans nom, selon la table, sans toucher aux autres attributs ni à la mise en forme.
- Validation :
  - le script : code 0, 273 éléments, 9 écrans. Sur une copie d'un écran où deux noms sont rendus identiques, puis un préfixe faux : code 1 avec la ligne fautive ;
  - `--smoke-frontend` code 0 sans avertissement ; `--capture-ui-screens` et `UiCaptureCompare` contre T0.1 : 10 états dans le seuil ;
  - `scripts/capture_editor_screen.ps1` sur `CarSelection` et `RaceHud`, sur une copie du contenu : capture lue, hiérarchie sans ligne sans nom, diagnostics sans erreur.
- Commit : `feat(racing-casa): name every element of the screens`

> Validation (2026-10-09) :
> - `scripts/check_ui_element_names.py` (nouveau, `expat` comme le moteur d'`xml.etree`, pour avoir la ligne de chaque balise) : avant les noms, code 1, 273 éléments, 181 sans nom, aucune autre faute (c'est ce que prévoyait l'étape 2) ; après, code 0, 9 écrans, 273 éléments, 0 faute.
> - Sur une copie de `Help.xaml` où un nom est rendu identique à un autre et un préfixe faux (`txtSection0Title`) : code 1, deux lignes fautives avec le fichier, la ligne et le type.
> - 181 attributs `Name` insérés juste après le nom de balise (script de scratch, par décalage d'octets, `git diff` : 9 fichiers, 181 lignes). Preuve : pour chacune des 181 lignes changées, retirer `Name="..."` de la nouvelle ligne redonne l'ancienne exactement. Les noms obtenus sont égaux, écran par écran et dans l'ordre, à la table du plan (273 noms ; les 92 noms existants étaient déjà ceux de T1.1).
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement hors du sous-module. `Assets/README.md` : une ligne sur la règle et le script.
> - `--smoke-frontend` : code 0, aucun avertissement ni erreur, réglages restaurés à l'identique (`artifacts/element-naming/t1.2/smoke.txt`).
> - `--capture-ui-screens` contre `before/ui` : 9 états à 0,00 ; `race-finished` à 13,99, identique à la relance (`t1.2b`), même cause que T1.1 (O2 : sous-module à `6ed0ef3f7`, références à `7a5320b6a`). Contre le contrôle `t1.1-ctrl` (même sous-module, sans noms) : les 10 états à 0,00, sur `t1.2` comme sur `t1.2b`. Les noms ne changent aucun pixel.
> - Export de scène `t1.2/scene.json` contre `before/scene.json` : 0 enregistrement sans partenaire, 0 problème de piste.
> - Éditeur sur des copies (contenu et éditeur copiés, `.casaeditor` supprimé) : `capture_editor_screen.ps1` sur `CarSelection` et `RaceHud`, code 0, diagnostics sans erreur. Captures lues : chaque ligne de la hiérarchie affiche `Type nom` (`Window wndCarSelection`, `OverlayPanel pnlRoot`, `Image imgSwatch0`… `imgLeftArrow` ; `Window wndRaceHud`, `Border pnlLaps`, `TextBlock lblCurrentLapTime`… `lblTopTime2`) ; aucune ligne sans nom parmi les lignes visibles. Le panneau ne montre que la première vingtaine de lignes : le reste de la hiérarchie est à voir par l'auteur (O1).
> - Captures locales dans `artifacts/element-naming/{t1.2,t1.2b}/` et sous le dossier de scratch (ignorés par git).

## Phase 2 — Mondes et composants

### ✅ T2.1 — Composants du monde de test nommés

- Objectif : P4 pour les 6 composants racines `Object <guid>` de `TrackBeginnerGenerators.world`.
- Fichiers : `RacingGameCasaEngine/Content/Worlds/Test/TrackBeginnerGenerators.world`.
- Étapes : remplacer seulement la valeur de `name` de ces 6 composants par le nom de leur type :
  - `RaceRoadGeneratorComponent` ;
  - `RaceGuardRailGeneratorComponent` ×2 (une par entité) ;
  - `RaceColumnGeneratorComponent` ;
  - `RaceTerrainGeneratorComponent` ;
  - `LightComponent`.
  Ids et réglages inchangés.
- Validation :
  - `jq` : plus aucun nom `Object ` dans le fichier ;
  - `git diff` : 6 lignes changées ;
  - `--open-asset Worlds/Test/TrackBeginnerGenerators.world` sur une copie : code 0, `Logs:` sans erreur, capture lue ;
  - `--save-project` sur une copie : deux enregistrements donnent les mêmes octets que le fichier committé ;
  - `python scripts/generate_rgce_content_assets.py --check` sans écart.
- Commit : `chore(racing-casa): name the components of the generators test world`

> Validation (2026-10-09) :
> - `jq` : plus aucun nom `Object ` dans `TrackBeginnerGenerators.world` ; `git diff` : 6 lignes changées (les 6 `name` des composants racines, dans l'ordre route, glissières x2, colonnes, terrain, lumière), ids et réglages inchangés, fins de ligne CRLF du fichier conservées.
> - `python scripts/generate_rgce_content_assets.py --check` : 0 fichier non catalogué.
> - Éditeur sur des copies (contenu et éditeur copiés, `.casaeditor` supprimé) : `editor_open.ps1 --open-asset Worlds/Test/TrackBeginnerGenerators.world` code 0, `Logs:` sans erreur (deux avertissements de déchargement de l'assembly de scripts, sans rapport). Capture lue : hiérarchie `World.Test.TrackBeginnerGenerators` avec ses 7 entités, inspecteur de `Track.Road.Beginner` avec `Race Road Generator` et ses modèles ; le monde s'ouvre.
> - `--save-project` deux fois de suite sur la même copie : `cmp` avec le fichier du dépôt identique après chaque enregistrement (mêmes octets).
> - Captures locales sous le dossier de scratch `naming/t2.1/` (ignoré par git).

### ✅ T2.2 — Composants créés par le code nommés

- Objectif : P4.
- Fichiers :
  - `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` ;
  - `RacingGameCasaEngine/UI/CarSelectionCarousel.cs` ;
  - `RacingGame.Gameplay/Entities/RaceCarEntityFactory.cs` ;
  - `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs`.
- Étapes :
  1. Donner `Name = nameof(<Type>)` à chaque composant sans nom de l'état vérifié. Pour le carrousel, dans `AddModelEntity` (`:331`).
  2. Vérifier que `VisualFrameName` ne désigne aucun composant renommé.
- Validation :
  - build ; `rg -n "new \w+Component\b"` relu : chaque création hors `Clone` et constructeur de copie a un nom ;
  - `--export-track-runtime-scene`, puis `compare_race_scene_exports.py` contre T0.1 : enregistrements identiques ;
  - `rg -c 'Object [0-9a-f-]{36}'` sur l'export → 0. Un chemin restant qui vient d'un composant créé par CasaEngine est listé ; s'il vient du code de ce dépôt, la tâche n'est pas finie ;
  - `--capture-ui-screens` et `UiCaptureCompare` contre T0.1 : 10 états dans le seuil (carrousel, HUD) ;
  - `--smoke-frontend` code 0 sans avertissement.
- Commit : `feat(racing-casa): name the components the game creates`

> Validation (2026-10-09) :
> - 23 sites de création de composants nommés `Name = nameof(<Type>)` dans 4 fichiers (corrigé à la clôture : la note disait 25) : `RaceWorldFactory` (caméra, `ChaseCameraRig`, `DebugFreeCamera`, lumières, `PlayerStart`, `RaceTrackPhysics`, `RaceFlowCoordinator`, `RaceCourseDebug`, `RaceCheckpointTrigger`), `RaceCarEntityFactory` (5 composants de la voiture), `LegacyTrackSceneFactory` (5 générateurs, dont les 2 glissières, et le `StaticModel` du décor), `CarSelectionCarousel` (lumière, caméra, `StaticModel` de `AddModelEntity`). Aucune entité ni composant déjà nommé par son rôle n'est touché. `VisualFrameName` vaut `WheelFrontLeft`, `WheelFrontRight`, `WheelBackLeft`, `WheelBackRight` : aucun nom de type, donc aucune collision.
> - `dotnet build RacingGame.slnx` : 0 erreur.
> - `--export-track-runtime-scene` (`artifacts/element-naming/t2.2/scene.json`) : `rg -c 'Object [0-9a-f-]{36}'` rend 0 (aucune correspondance, contre 5 118 segments avant) sur 5 154 `ComponentPath`. `compare_race_scene_exports.py` contre `before/scene.json` : 0 enregistrement sans partenaire, 0 problème de piste.
> - `--capture-ui-screens` : 9 états à 0,00 contre `before/ui` ; `race-finished` à 13,99, la dérive connue de O2 (sous-module à `6ed0ef3f7`). Contre le contrôle `t1.1-ctrl` (même sous-module) : les 10 états à 0,00.
> - `--smoke-frontend` : code 0, aucun avertissement ni erreur ; réglages restaurés à l'identique à chaque lancement.
> - Captures et exports locaux dans `artifacts/element-naming/t2.2/` (ignoré par git).

## Phase 3 — Pointeur, documentation, vérification

### 🧪 T3.1 — Pointeur CasaEngine aux noms par défaut de l'éditeur

- Objectif : RacingGame suit la fin du plan CasaEngine `editor-default-names-tasks.md`.
- Prérequis : T1.1 à T1.4 de ce plan CasaEngine faits.
- Fichiers : `CasaEngine` (pointeur).
- Étapes : pointer le sous-module sur la tête de `chantier/rgce-editor-project` ; build.
- Validation :
  - build ;
  - `--open-asset` du monde de test sur une copie : code 0 ; la capture lue montre les composants sous leur nom d'affichage (P5 du plan CasaEngine) ;
  - `scripts/capture_editor_screen.ps1` sur `MainMenu` : capture lue, sans erreur ;
  - **sonde de clonage** (bloc-notes, non versionnée) : un projet console qui référence `RacingGame.Gameplay` et `CasaEngine`.
    - Pour chaque type de composant nommé par le code, elle crée un composant, lui donne un nom et le pose dans une entité : en racine, ou en enfant de la racine pour un `SceneComponent`, ou dans `Components`. Puis elle clone l'entité et compare le nom de chaque clone.
    - Les types : la liste de l'état vérifié, avec les ancres, les générateurs, `RaceTrackPhysicsComponent`, `VehicleDynamicsComponent`, `RaceGeneratedModelComponent`, et les types du moteur `LightComponent`, `CameraLookAtComponent`, `StaticModelComponent`.
    - Attendu : tous les noms gardés. La même sonde, construite sur l'ancien pointeur, échoue sur chaque type.
    - Un type qui ne se construit pas hors du jeu est listé avec l'erreur. Le test moteur de T1.3 couvre sa forme de `Clone()`.
- Commit : `chore(casaengine): give new editor elements and components default names`

> Validation (2026-10-09) :
> - `git add CasaEngine` : `git diff --cached` ne montre que le pointeur `7a5320b6a` → `6ed0ef3f7` (plus ce plan à l'index du commit). `dotnet build RacingGame.slnx` : 0 erreur.
> - Éditeur sur des copies (`artifacts` non utilisé, copies dans le bloc-notes `naming/t3.1/`) : `--open-asset Worlds/Test/TrackBeginnerGenerators.world` : `OPENED exit=0`. La capture lue montre la hiérarchie des 7 entités et, pour `Track.Road.Beginner`, l'inspecteur avec le composant racine « Race Road Generator », ses deux « Static Model » et leurs « Sub Mesh » sous leur nom d'affichage, sans `Object <guid>`. (Corrigé à la clôture : la copie ouverte est le monde committé par T2.1, dont les composants portent le nom de leur type ; l'inspecteur montre le nom d'affichage seul, car P5 du plan CasaEngine masque un nom égal au type. T3.3 l'a vérifié de nouveau.)
> - `capture_editor_screen.ps1` sur `MainMenu` : `OK`. La capture lue montre la hiérarchie sans ligne sans nom : `Window wndMainMenu`, `OverlayPanel pnlRoot`, `Image imgBackground`, `imgLogo`, `Border pnlBand`, `StackPanel pnlButtons`, puis `pnlPlaySlot`, `pnlPlayBody`, `Button btnPlay`, `pnlPlayFace`, `imgPlayGlyph`, `pnlPlayRing`, `imgPlayLabel`, et la même suite pour Highscores et Options ; l'aperçu du menu s'affiche normalement.
> - Sonde de clonage (`naming/cloneprobe/`, projet console qui référence `RacingGame.Gameplay.csproj` et `CasaEngine.csproj`, non versionnée) : 21 types, tous constructibles hors du jeu : `CameraLookAtComponent`, `ChaseCameraRigComponent`, `DebugFreeCameraComponent`, `LightComponent`, `PlayerStartComponent`, `RaceTrackPhysicsComponent`, `RaceFlowCoordinatorComponent`, `RaceCourseDebugComponent`, `RaceCheckpointTriggerComponent`, `VehicleDynamicsComponent`, `LegacyCarVisualComponent`, `VehicleWheelVisualComponent`, `DebugCarVisualComponent`, `RaceCarStateComponent`, `RaceTerrainGeneratorComponent`, `RaceRoadGeneratorComponent`, `RaceGuardRailGeneratorComponent`, `RaceColumnGeneratorComponent`, `RaceGeneratedModelComponent`, `StaticModelComponent`, `RacingCarAnchorComponent`. Pour chacun : nom donné, pose dans une entité (racine pour un `SceneComponent` ; enfant de la racine pour un `SceneComponent` ; `Components` pour les autres), `entity.Clone()`, comparaison du nom : **les 21 noms sont gardés** (`ALL KEPT`, code 0, `naming/cloneprobe/run-new.txt`).
> - Écart au plan (contrôle négatif) : le plan prévoyait « la même sonde sur l'ancien pointeur ». La sonde appelle plutôt `component.Clone()` directement sur chaque type (le chemin sans le correctif T1.3, c'est-à-dire l'ancien comportement de chaque site de clonage) : **les 21 noms sont perdus** (`Object <guid>`), ce qui montre que la sonde détecte le défaut.
> - Reste (O1) : coup d'œil de l'auteur sur la hiérarchie dans l'éditeur.

### ✅ T3.2 — ADR et documentation

- Objectif : D1 à D4 et P1 à P6 écrits.
- Fichiers :
  - `docs/decisions/0020-rgce-names-every-element-and-component.md` (nouveau, skill `adr`) ;
  - `docs/decisions/README.md` ;
  - `ai-agent/README.md` ;
  - ce plan.
- Étapes :
  1. ADR-0020 : contexte (hiérarchie sans noms, `Object <guid>` dans les exports), décisions, conséquences (noms cherchés par le code, script de contrôle, références de scène désormais lisibles).
  2. Lignes des index.
- Validation : liens vérifiés par `rg` ; relecture.
- Commit : `docs(adr): record the naming of the RacingGameCasaEngine elements`

> Validation (2026-10-09) :
> - `docs/decisions/0020-rgce-names-every-element-and-component.md` écrite d'après `template.md` (statut Accepted, date 2026-10-09, source : ce plan et les décisions de l'auteur, plus l'ADR-0075 de CasaEngine pour le côté éditeur) : contexte, D1 à D4, P1 à P7, conséquences. Lignes ajoutées à l'index de `docs/decisions/README.md` et à `ai-agent/README.md` (ce qui reste : O1, T3.3, O2).
> - Liens vérifiés par `rg` : le chemin de l'ADR-0075 de CasaEngine, le fichier de l'ADR, la ligne d'index et le plan cités existent. Docs seulement : pas de build.


### ✅ T3.3 — Vérification indépendante

- Objectif : une passe `verifier` fraîche sur tout le plan, contre la validation globale.
- Étapes : lui donner ce plan, la plage de commits, les références de T0.1. Le jeu et l'éditeur sont lancés par un seul agent à la fois.
- Validation : verdict CONFIRMED, ou constats traités selon leur priorité (P0 à P2 corrigés, P3 et P4 reportés ici).
- Commit : `docs(racing-casa): close the element naming plan` (plan et index seulement).

> Validation (2026-10-09) : workflow avec un vérificateur frais, seul à lancer le jeu et l'éditeur, et deux relectures en lecture seule (écrans ; composants, monde, pointeur et docs).
> - **Vérificateur : CONFIRMED.**
>   - build 0 erreur ; `check_ui_element_names.py` : 9 écrans, 273 éléments, 0 faute ;
>   - les 273 noms recalculés par un analyseur indépendant égalent la table, dans l'ordre ;
>   - chaque nom cherché par le code (littéraux, préfixes et plages d'indices des view models) existe dans son XAML, et `rg` ne trouve plus d'ancien nom ;
>   - `--smoke-frontend` : code 0, sans avertissement ;
>   - export : 0 enregistrement sans partenaire, et 0 segment `Object <guid>` contre 5 118 avant. Les 15 chemins qui contiennent encore `Object0x` sont des nœuds de modèles (D2) ;
>   - monde de test : ouvert dans l'éditeur, et deux `--save-project` donnent les octets du dépôt ;
>   - sonde de clonage : 21 types gardent leur nom. La même sonde construite sur CasaEngine `7a5320b6a` échoue (32 noms perdus) ;
>   - pointeur `6ed0ef3f7` committé.
> - **Captures** : 9 états à 0,00 contre `before/ui`. `race-finished` est à 13,99 (arrière-plan 3D seulement).
>   - Contrôles du vérificateur : moteur ancien contre moteur nouveau, et dll et contenu d'avant les noms contre ceux d'après, donnent 0,00 sur les 10 états. Les noms et le correctif moteur ne changent donc aucun pixel.
>   - Contrôle de la session principale (10:51) : la configuration exacte de T0.1 (`CasaEngine.dll` de `7a5320b6a`, dll et contenu de `4f5377e`) donne aussi 13,99 contre `before/ui`.
>   - La cause donnée par les notes de T1.1, T1.2 et T2.2 (le pointeur) est donc réfutée. C'est la référence `race-finished` de 09:46, identique à celle de O7, qui ne se reproduit plus depuis 10:13 (O2).
> - **Relectures** : aucun P0 à P2.
>   - Notes de T2.2 (compte) et de T3.1 (monde de test) : corrigées ci-dessus.
>   - Reportés : O3 (le script ne vérifie pas qu'il couvre bien 9 écrans, P4) et O4 (deux phrases inexactes de l'ADR-0020, question à l'auteur).
> - Hors plan : une autre session a committé `d4e8f51de` dans le sous-module (enveloppe `StackPanel`, O2 du plan CasaEngine). Le pointeur de RacingGame reste à `6ed0ef3f7`, et `git status` montre le sous-module modifié.

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Coup d'œil de l'auteur sur la hiérarchie des écrans et des composants dans l'éditeur. | T1.2, T3.1 |
| O2 | L'état `race-finished` des captures s'écarte de 13,99 de la référence de T0.1 (arrière-plan 3D : caméra et décor décalés, interface identique). L'écart est le même quelle que soit la version des dll du jeu, du contenu et du moteur, y compris la configuration exacte de T0.1 relancée à 10:51. Tous les lancements depuis 10:13 sont identiques entre eux (0,00), et la référence de 09:46 est identique à celle de O7. Cause hors de ce plan, non identifiée : à chercher à part, puis refaire la référence `race-finished`. | T1.1, T1.2, T2.2, T3.3 |
| O3 | Avis P4 reporté : `check_ui_element_names.py` ne vérifie pas qu'il couvre 9 écrans. Un écran déplacé hors du motif de fichiers passerait sans bruit. | T1.2 |
| O4 | Question à l'auteur. L'ADR-0020 contient deux phrases inexactes, signalées par les relectures :<br>- le contexte (ligne 11) dit que le code cherche les noms `face*`, ce qu'aucun code ne faisait ;<br>- les conséquences (ligne 36) attribuent l'écart `race-finished` au pointeur, ce que les contrôles réfutent (O2).<br>Une ADR ne se réécrit pas : faut-il corriger celle-ci, écrite le jour même et jamais utilisée depuis, ou laisser l'erreur et la noter dans une nouvelle ADR ? | T3.2 |

## Hors périmètre

- Les noms des matériaux (`Material.Hotel01.39`…), des nœuds et des meshes des modèles cuits (`$dummy_root`, `glas [39]`…), et donc leurs ids (D2, D3).
- Les noms d'entités : ils sont tous donnés, et le code les cherche (`RaceWorldNames`).
- Les noms des sprites, sons, polices et autres assets du catalogue : déjà de la forme `Type.Rôle`, aucun n'est renommé.
- Le nom par défaut `Object <guid>` du moteur pour les objets créés par d'autres jeux (plan CasaEngine, hors périmètre).
