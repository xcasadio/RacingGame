# Plan agent IA — Les modes automation de RGCE ignorent l'entrée réelle

Plan court issu de l'enquête sur le point ouvert O3 de [rgce-race-finished-capture-tasks.md](rgce-race-finished-capture-tasks.md) (2026-10-15).
Les points P1 → P4 sont des **propositions de l'agent, à arbitrer par l'auteur** : aucune modification de code avant son approbation.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche d'`AGENTS.md`.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

Pendant un mode automation de RacingGameCasaEngine (RGCE), aucune entrée réelle (souris, clavier, manette) n'agit sur le jeu : un clic de l'auteur dans une autre application ne renvoie plus au menu pendant `race-finished`, et aucun écran, aucune voiture, aucun bouton ne réagit au joueur. Les validateurs gardent leurs contrôles et pilotent tout eux-mêmes, comme aujourd'hui. O3 est clos avec sa cause.

Ce plan ne livre pas : la correction du même défaut en jeu normal (voir O2 et O3 ci-dessous), ni O4 du plan `race-finished`.

## Cause établie (enquête du 2026-10-15)

Sonde de bloc-notes : copie des sources de `RacingGameCasaEngine` sur le bin du checkout principal (build du 2026-10-15 07:56, celui de la validation de `c887e95`), jamais dans le dépôt. Elle journalise chaque changement des boutons de souris, des touches, des manettes et du focus, avec la position du curseur, le processus de la fenêtre sous le curseur et celui de la fenêtre au premier plan, l'entrée qui ferme le panneau d'arrivée et la pile d'appel de `ReturnToFrontEnd`. Lancements par un helper qui sauvegarde et restaure `display-settings.json` et `front-end-options.json` (SHA-1 identique après chaque lancement), un seul processus de jeu ou d'éditeur à la fois, aucune entrée simulée. 19 lancements de `--capture-ui-screens` le 2026-10-15 entre 08:48 et 09:03 : T1 (contrôle), B1 à B10, H1 à H8. Journaux, résultats et sources de la sonde : dossier `o3/` du bloc-notes de cette session (`logs/`, `results.txt`, `probe-src/`, `probe-src-hold/`).

- **Le panneau d'arrivée se ferme sur n'importe quel clic gauche.** Course finie, `RaceHudScreen.Update` appelle `_returnToMenu` dès que `IsDismissPressed` est vrai (`RacingGameCasaEngine/Screens/RaceHudScreen.cs:59-63`) : Entrée, Espace, Échap, `MouseManager.LeftButtonJustPressed`, ou A/B/X/Back/Start de la manette du joueur (`:98-116`). Aucun test de position ni de focus. `_returnToMenu` est `RaceFrontEndFlow.ReturnToFrontEnd`, qui charge le monde du front-end puis le menu principal (`Bootstrap/RaceFrontEndFlow.cs:65, 104-108`).
- **Le jeu voit les boutons de souris de tout le bureau.**
  - Souris et clavier viennent de `RuntimeContext.WindowInputSource`, créé par CasaEngine en `MonoGameWindowInputSource(() => IsActive, () => Window)` puis mis en cache par image (`CasaEngine/CasaEngine/Framework/Application/CasaEngineGame.cs:444-455`). `InputComponent` le lit par le routeur (`Framework/Input/InputComponent.cs:65-101`), MGUI par `ViewRenderHost` (`Framework/UI/ViewRenderHost.cs:31, 63, 83`). Fichiers identiques entre le pointeur du dépôt `6ed0ef3f7` et `87fc5f01b` du checkout principal (`git diff --stat`).
  - `GetMouseState` renvoie `Mouse.GetState(window)` sans test de focus ; seul le clavier est vidé quand `IsActive` est faux (`Framework/Input/MonoGameWindowInputSource.cs:50-71`).
  - MonoGame DesktopGL 3.8.5.1 (`MonoGame.Framework.dll` du bin, décompilé avec `ilspycmd`) : `Mouse.PlatformGetState(GameWindow)` lit les boutons par `Sdl.Mouse.GetGlobalState` et ignore le résultat de `Sdl.Window.GetWindowFlags`. Un clic gauche dans n'importe quelle application est donc un bouton enfoncé pour le jeu, avec une position relative à la zone client, éventuellement hors de celle-ci.
- **`IsActive` reste vrai sans focus.** `GamePlatform.BeforeInitialize` le met à `true` ; `SdlGamePlatform` ne le change que sur les événements SDL `FocusGained` et `FocusLost` (décompilé). Lancée depuis un processus en arrière-plan, la fenêtre du jeu n'a pas eu le focus dans 18 des 19 lancements de sonde (la fenêtre au premier plan est une autre application du premier au dernier update), et `IsActive` y est resté vrai. Dans H7, elle l'a eu un instant (update 677), puis `IsActive` est passé à faux (update 759) : seul changement de focus des 19 lancements.
- **Clavier et manette.** Le clavier de MonoGame vient des événements SDL `KeyDown` et `KeyUp` de la fenêtre (`Keyboard.PlatformGetState`, `SdlGamePlatform`) : aucune touche journalisée en 19 lancements. Une manette `P0` est apparue connectée dans H6 à H8, sans bouton enfoncé : elle reste une source possible.
- **Un clic réel, hors de la fenêtre du jeu, est vu par le jeu** (lancement B6, journal de sonde) :
  - 08:52:49, update 866, écran `RaceHud`, course en cours : `L=Pressed leftJustPressed=True`, curseur à l'écran (742, 1053), zone client `{X:760 Y:180 Width:1920 Height:1080}`, `insideClient=False`, fenêtre sous le curseur et fenêtre au premier plan : `claude`, `IsActive=True`.
  - Le clic est tombé 2 s avant `RaceHud -> race finished` (08:52:51). Pendant le panneau d'arrivée, ce même `LeftButtonJustPressed` rend `IsDismissPressed` vrai.
- **Retour au menu provoqué par un vrai clic pendant `race-finished`, deux fois.** Série H : la sonde maintient le panneau d'arrivée 20 s au lieu de 1 s, pour qu'un clic ordinaire de l'auteur, qui travaillait dans d'autres fenêtres, ait le temps de tomber dessus. 2 lancements sur 8 ont expiré à `CaptureRaceFinished`, et le journal nomme l'entrée :
  - H7, 08:59:11 : clic gauche dans Discord (fenêtre sous le curseur et au premier plan), hors de la zone client, `IsActive=False` → `RaceHud dismiss by mouse LeftButtonJustPressed` à l'update suivant → `ReturnToFrontEnd` appelé depuis `RaceHudScreen.Update` (`:62`) → chargement du menu principal → expiration à 09:00:50.
  - H8, 09:01:11 : même suite avec un clic dans Firefox, hors de la zone client, `IsActive=True` → expiration à 09:02:53.
  - Les 6 autres lancements de H, sans clic pendant le maintien, finissent à code 0. Série B (10 lancements, maintien normal de 1 s) : 10 à code 0 ; le seul clic journalisé (B6) est tombé pendant la course.
  - L'entrée qui renvoie au menu est donc **un clic gauche réel, n'importe où sur le bureau**, même quand la fenêtre du jeu n'a pas le focus (H7).
- **Fenêtre d'exposition.** `CaptureRaceFinished` attend 250 ms puis 1 s d'état stable (`Bootstrap/UiScreenCaptureValidator.cs:20-21, 102, 232-245, 262-271`) : un clic gauche n'importe où sur le bureau pendant ces ~1,25 s renvoie au menu, puis le validateur expire à 120 s. Lancement U0 du 2026-10-15 (journal de la sonde O2) : `RaceHud -> race finished` à 07:59:28, chargement de `MainMenu.uiscreen` à 07:59:29, expiration à 08:01:12.

## État vérifié du dépôt (2026-10-15)

- Branche `claude/jolly-clarke-c20266` (worktree), avancée sans commit propre sur `remaster` `200a66d` (`git merge --ff-only remaster`). Sous-module `CasaEngine` non initialisé dans le worktree (`git submodule status` : `-6ed0ef3f7`). Le checkout principal (`remaster`) a son sous-module à `87fc5f01b` (autre session), `.serena/` et `log.txt` non suivis : **ne pas les indexer**.
- **Lecteurs d'entrée réelle du jeu**, tous exposés pendant une automation :
  - écrans : `SplashScreen.cs:61-67` et `HelpScreen.cs:114-116` (n'importe quel clic gauche, sans position), `MainMenuScreen.cs:87-122`, `CarSelectionScreen.cs:105-182`, `TrackSelectionScreen.cs:85-151`, `HighscoresScreen.cs:99-152`, `OptionsScreen.cs:153-269`, `RaceHudScreen.cs:98-116` ; boutons MGUI de `PauseScreen.cs:39` ;
  - survol à la position réelle du curseur : `MainMenuScreen.cs:91`, `TrackSelectionScreen.cs:99-116, 177`, `CarSelectionScreen.cs:233`, `HighscoresScreen.cs:207`, `OptionsScreen.cs:353`, `HelpScreen.cs:131` ;
  - jeu : conduite `RacingGame.Gameplay/Components/VehicleInputReader.cs:17-82`, pause sur Échap ou Start/Back `RaceFlowCoordinatorComponent.cs:99-115`, caméra `ChaseCameraRigComponent.cs:171-176`, caméra libre `DebugFreeCameraComponent.cs:60-83`, raccourcis F1, F2, F3, F5 `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs:356-392`.
- **Aucun validateur ne lit l'entrée réelle** : `rg "InputComponent|KeyboardManager|MouseManager|GamePad" RacingGameCasaEngine/Bootstrap/*Validator.cs` ne trouve rien. Ils pilotent le front-end par les méthodes `…ForAutomation` de `RaceFrontEndFlow.cs:120-141` et la voiture par `ForcedControlInput` (`CarProfileAuditValidator.cs:292`, `CarTopSpeedAuditValidator.cs:312`).
- **Point d'entrée possible sans toucher au moteur** :
  - `Program.cs:47-48` crée le `EngineRuntimeContext` avant le jeu ; `WindowInputSource` y est public et modifiable (`CasaEngine/CasaEngine/Framework/Application/EngineRuntimeContext.cs:34`) ;
  - `CasaEngineGame` ne crée la source MonoGame que si elle est nulle (`??=`) et enveloppe toute source dans `FrameCachedWindowInputSource` (`CasaEngineGame.cs:444-450`) ; `WindowInputSnapshot.Empty` est l'état « aucune entrée » du moteur (`Framework/Input/IWindowInputSource.cs:12`) ;
  - la manette passe à part : `InputComponent` est créé dans `CasaEngineGame.Initialize` (`CasaEngineGame.cs:403`) et lit `IGamePadStateProvider` (`InputComponent.cs:45-50, 99`), remplaçable par `SetProviders`.
- Modes automation qui créent le jeu : les 6 de `RaceLaunchOptions` (`Bootstrap/RaceLaunchOptions.cs:5-15`, lus dans `Program.cs:22-29, 67-79`). `--verify-legacy-import-profile` sort avant la création du jeu (`Program.cs:39-43`).
- Pas de projet de test pour RGCE (`RacingGame.slnx`) ; précédent d'ADR pour un comportement des modes automation : ADR-0004 (`docs/decisions/0004-rgce-saves-user-settings-only-from-options.md`).

## Décisions verrouillées

Aucune pour l'instant : P1 → P4 ci-dessous attendent l'arbitrage de l'auteur (O1).

## Points à valider par l'auteur

| Réf | Proposition de l'agent | Alternatives |
|---|---|---|
| P1 | **Une source d'entrée vide pour tout le processus** quand un mode automation tourne : `Program.cs` pose `runtimeContext.WindowInputSource` sur une source RGCE qui renvoie toujours l'état vide, avant de créer le jeu ; le jeu remplace aussi le fournisseur de manette par un fournisseur vide après `base.Initialize()`. Écrans, conduite, pause, raccourcis, boutons MGUI et survol ignorent alors le joueur, sans un test par écran ni changement du sous-module. | **B** : garde dans chaque écran (fermeture et navigation), par un drapeau de `RaceFrontEndState`. 9 fichiers, et ne couvre ni la conduite, ni Échap, ni F1–F5, ni les boutons de la pause, ni le survol ; chaque nouvel écran doit y penser. **C** : correction dans CasaEngine seulement (boutons de souris ignorés quand la fenêtre n'a pas le focus de la souris). Protège aussi le jeu normal, mais change le sous-module et ne protège pas une automation dont la fenêtre reçoit elle-même des entrées. |
| P2 | Les **6 modes** qui créent le jeu : `--smoke-frontend`, `--capture-ui-screens`, `--capture-track-audit`, `--capture-car-profile-audit`, `--capture-car-top-speed-audit`, `--export-track-runtime-scene`. | Seulement `--capture-ui-screens`. |
| P3 | État souris fourni : `WindowInputSnapshot.Empty` du moteur (position (0, 0), aucun bouton). Les captures diront si (0, 0) survole quelque chose. | Une position hors de la zone client. |
| P4 | **ADR-0021 courte**, comme ADR-0004 pour l'autre comportement des modes automation. | Doc XML seulement. |

## Règles d'exécution pour l'agent

- **Branche `claude/jolly-clarke-c20266`** (worktree, partie de `remaster`). Ne jamais committer sur `main` ni `master`, ne jamais pousser ; la fusion dans `remaster` reste une décision de l'auteur.
- **Une seule tâche à la fois**, icône `🚧` puis `✅`, `🧪` ou `⚠️`, note de validation sous la tâche, **un commit par tâche** incluant ce fichier.
- **Un seul processus de jeu ou d'éditeur à la fois**, lancé par un helper qui attend la fin de tout autre processus de jeu ou d'éditeur, sauvegarde et restaure `display-settings.json` et `front-end-options.json` (SHA-1 vérifié) ; aucune entrée clavier ou souris simulée.
- Build : `dotnet build RacingGame.slnx` (sous-module à initialiser dans le worktree avant le premier build). `git add` fichier par fichier.
- Langue : ce plan en français ; code, commits et ADR en anglais.
- Rappel moteur : pas d'allocation dans les chemins chauds (la source vide renvoie un état constant).

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur.
- Sonde de maintien (série H) reconstruite sur le code corrigé, qui lit en plus le bouton gauche brut de MonoGame (`Mouse.GetState(Window)`) : au moins un clic réel pendant le maintien, ordinaire ou volontaire de l'auteur, est journalisé brut, n'atteint pas `MouseManager`, ne ferme pas le panneau, et la capture finit à code 0. Sans clic réel pendant un maintien, la tâche reste 🧪.
- Les 6 modes automation : code 0, aucun avertissement ni erreur nouveaux ; une ligne de journal indique que l'entrée du joueur est ignorée.
- `--capture-ui-screens`, deux lancements, `scripts/UiCaptureCompare` contre `artifacts/race-finished-capture/ref-a/ui` (`race-finished`) et `artifacts/element-naming/verify/ui` (9 autres états) : dans le seuil, ou tout écart expliqué (survol à (0, 0), P3).
- Jeu normal (sans option) : l'entrée réelle marche comme avant (vérification manuelle de l'auteur, 🧪).

---

## Phase 1 — Entrée ignorée pendant l'automation

### ⏳ T1.1 — Source d'entrée vide pendant les modes automation

- Objectif : sous un des modes de P2, le jeu ne reçoit aucune entrée réelle (P1, P3).
- Fichiers : `RacingGameCasaEngine/Bootstrap/RaceLaunchOptions.cs` (propriété qui dit si un mode automation tourne), `RacingGameCasaEngine/Bootstrap/AutomationInputSource.cs` (nouveau : source de fenêtre et fournisseur de manette vides), `RacingGameCasaEngine/Program.cs` (pose la source avant `new RacingGameCasaEngineGame`), `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` (manette vide après `base.Initialize()`, une ligne de journal).
- Étapes :
  1. Initialiser le sous-module du worktree ; build de référence.
  2. Source vide : `IWindowInputSource.GetSnapshot()` renvoie `WindowInputSnapshot.Empty` ; `IGamePadStateProvider.GetState` renvoie l'état par défaut (non connecté). Vérifier dans le code moteur que `FrameCachedWindowInputSource` et le routeur n'exigent rien de plus.
  3. `Program.cs` : si un mode automation tourne, `runtimeContext.WindowInputSource = new AutomationInputSource()` avant de créer le jeu.
  4. `Initialize` du jeu : en mode automation, `InputComponent.SetProviders` avec la source enveloppée et la manette vide ; `Logs.WriteInfo` une fois.
- Validation : build ; sonde de maintien reconstruite sur ce code, avec au moins un clic réel pendant un maintien (validation globale) ; les 6 modes à code 0.
- Commit : `fix(racing-casa): ignore the player input under the automation modes`

### ⏳ T1.2 — Captures contre les références

- Objectif : montrer que les captures ne changent pas, ou expliquer chaque écart.
- Sources : deux lancements de `--capture-ui-screens` copiés dans `artifacts/automation-input/{run-a,run-b}/ui` (ignoré par git), rapports `UiCaptureCompare`.
- Étapes :
  1. Comparer run-a et run-b entre eux, puis aux références de la validation globale.
  2. Écart au-dessus du seuil : chercher s'il vient du survol (P3) ou d'O4 ; ne prendre une nouvelle référence qu'avec sa cause écrite.
- Validation : rapports dans `artifacts/automation-input/compare-*` ; note sous la tâche.
- Commit : `docs(racing-casa): record the captures with the player input ignored` (ce plan seulement, les artefacts ne sont pas suivis).

### ⏳ T1.3 — ADR, clôture d'O3 et vérification

- Objectif : décision enregistrée (P4), O3 clos, vérification indépendante.
- Fichiers : `docs/decisions/0021-…md` et l'index des ADR (si P4), `ai-agent/tasks/rgce-race-finished-capture-tasks.md` (O3 : cause et lien vers ce plan), `ai-agent/README.md`, ce plan.
- Étapes :
  1. ADR-0021 avec le skill `adr`, si P4 est retenu.
  2. O3 : cause (bouton de souris global, `IsActive` sans focus, panneau fermé par tout clic) et correctif.
  3. Passe `verifier` fraîche sur T1.1 et T1.2.
- Validation : verdict CONFIRMED ; docs seulement, pas de build.
- Commit : `docs(racing-casa): record that the automation modes ignore the player input`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Arbitrage de P1 → P4 par l'auteur. | T1.1 → T1.3 |
| O2 | **Hors périmètre, à décider** : en jeu normal aussi, un clic gauche dans une autre application est vu par le jeu. Il ferme le panneau d'arrivée, le Splash et l'aide, qui ne testent pas la position. Correction possible dans CasaEngine (`MonoGameWindowInputSource.GetMouseState` : boutons relâchés quand la fenêtre n'a pas le focus de la souris), à traiter dans le dépôt du moteur. | — |
| O3 | **Hors périmètre** : `IsActive` reste vrai quand la fenêtre n'a jamais eu le focus, donc la suppression du clavier de `MonoGameWindowInputSource` ne se déclenche pas dans ce cas. Sans effet constaté ici (aucune touche journalisée), à signaler avec O2. | — |
| O4 | Lien possible avec O4 du plan `race-finished` (petites variantes de cadence) : les écrans à survol lisent la position réelle du curseur. Non établi ; T1.2 le mesurera. | T1.2 |

## Hors périmètre

- Toute modification de CasaEngine ou de MonoGame (O2, O3).
- Le comportement du jeu normal : l'entrée réelle n'y change pas.
- O4 du plan `race-finished`, sauf ce que T1.2 en mesure.
