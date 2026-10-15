# Plan agent IA — Les modes automation de RGCE ignorent l'entrée réelle

Plan court issu de l'enquête sur le point ouvert O3 de [rgce-race-finished-capture-tasks.md](rgce-race-finished-capture-tasks.md) (2026-10-15).
Les décisions D1 → D4 ci-dessous ont été arbitrées par l'auteur le 2026-10-15 (« P1 à P4 comme recommandé, fais-le ») : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche d'`AGENTS.md`.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

Pendant un mode automation de RacingGameCasaEngine (RGCE), aucune entrée réelle (souris, clavier, manette) n'agit sur le jeu : un clic de l'auteur dans une autre application ne renvoie plus au menu pendant `race-finished`, et aucun écran, aucune voiture, aucun bouton ne réagit au joueur. Les validateurs gardent leurs contrôles et pilotent tout eux-mêmes, comme aujourd'hui. O3 est clos avec sa cause.

Ce plan ne livre pas : la correction du même défaut en jeu normal (voir O2 et O3 ci-dessous), ni O4 du plan `race-finished`.

## Cause établie (enquête du 2026-10-15)

Sonde de bloc-notes : copie des sources de `RacingGameCasaEngine` sur le bin du checkout principal (build du 2026-10-15 07:56, celui de la validation de `c887e95`), jamais dans le dépôt. Elle journalise chaque changement des boutons de souris, des touches, des manettes et du focus, avec la position du curseur, le processus de la fenêtre sous le curseur et celui de la fenêtre au premier plan, l'entrée qui ferme le panneau d'arrivée et la pile d'appel de `ReturnToFrontEnd`. Lancements par un helper qui sauvegarde et restaure `display-settings.json` et `front-end-options.json` (SHA-1 identique après chaque lancement), un seul processus de jeu ou d'éditeur à la fois, aucune entrée simulée. 19 lancements de `--capture-ui-screens` le 2026-10-15 entre 08:48 et 09:03 : T1 (contrôle), B1 à B10, H1 à H8. Journaux, résultats, helpers et sources des sondes, avant et après correctif : `artifacts/automation-input/evidence/` du checkout principal (ignoré par git : `logs/`, `results.txt`, `run_rgce.sh`, `probe/probe-src*`), copie du dossier `o3/` du bloc-notes de cette session.

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
  - écrans : `SplashScreen.cs:61-67` et `HelpScreen.cs:114-116` (n'importe quel clic gauche, sans position), texte tapé lu directement sur `Window.TextInput` par `OptionsScreen.cs:102-110, 130-149` (**trouvé par la revue de T1.1**, hors de la source de fenêtre), `MainMenuScreen.cs:87-122`, `CarSelectionScreen.cs:105-182`, `TrackSelectionScreen.cs:85-151`, `HighscoresScreen.cs:99-152`, `OptionsScreen.cs:153-269`, `RaceHudScreen.cs:98-116` ; boutons MGUI de `PauseScreen.cs:39` ;
  - survol à la position réelle du curseur : `MainMenuScreen.cs:91`, `TrackSelectionScreen.cs:99-116, 177`, `CarSelectionScreen.cs:233`, `HighscoresScreen.cs:207`, `OptionsScreen.cs:353`, `HelpScreen.cs:131` ;
  - jeu : conduite `RacingGame.Gameplay/Components/VehicleInputReader.cs:17-82`, pause sur Échap ou Start/Back `RaceFlowCoordinatorComponent.cs:99-115`, caméra `ChaseCameraRigComponent.cs:171-176`, caméra libre `DebugFreeCameraComponent.cs:60-83`, raccourcis F1, F2, F3, F5 `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs:356-392`.
- **Aucun validateur ne lit l'entrée réelle** : `rg "InputComponent|KeyboardManager|MouseManager|GamePad" RacingGameCasaEngine/Bootstrap/*Validator.cs` ne trouve rien. Ils pilotent le front-end par les méthodes `…ForAutomation` de `RaceFrontEndFlow.cs:120-141` et la voiture par `ForcedControlInput` (`CarProfileAuditValidator.cs:292`, `CarTopSpeedAuditValidator.cs:312`).
- **Point d'entrée possible sans toucher au moteur** :
  - `Program.cs:47-48` crée le `EngineRuntimeContext` avant le jeu ; `WindowInputSource` y est public et modifiable (`CasaEngine/CasaEngine/Framework/Application/EngineRuntimeContext.cs:34`) ;
  - `CasaEngineGame` ne crée la source MonoGame que si elle est nulle (`??=`) et enveloppe toute source dans `FrameCachedWindowInputSource` (`CasaEngineGame.cs:444-450`) ; `WindowInputSnapshot.Empty` est l'état « aucune entrée » du moteur (`Framework/Input/IWindowInputSource.cs:12`) ;
  - la manette passe à part : `InputComponent` est créé dans `CasaEngineGame.Initialize` (`CasaEngineGame.cs:403`) et lit `IGamePadStateProvider` (`InputComponent.cs:45-50, 99`), remplaçable par `SetProviders` ;
  - **trouvé à l'exécution de T1.1** : MGUI lit aussi la manette directement chez MonoGame (`CasaEngine/MGUI/MGUI.Shared/Input/GamePad/GamePadTracker.cs:100`) pour la navigation au focus (`MGUI.Core/UI/Navigation/UIFocusNavigationService.cs:270-295`, appelé par `MGUI.Core/UI/MGDesktop.cs:1582`), sans passer par la source de fenêtre. Le suivi a un interrupteur public `GamePadTracker.Enabled` (`:41`), atteint par `MGDesktop.InputTracker` (`MGUI.Core/UI/MGDesktop.cs:93`). Chaque `UIRoot` est créé par `RuntimeContext.UIViewRuntimeFactory` (`CasaEngineGame.cs:149, 782-787`), que `Program.cs:48` pose déjà : un décorateur RGCE de cette fabrique peut couper ce suivi sans toucher au sous-module (D1 : « boutons MGUI »).
- Modes automation qui créent le jeu : les 6 de `RaceLaunchOptions` (`Bootstrap/RaceLaunchOptions.cs:5-15`, lus dans `Program.cs:22-29, 67-79`). `--verify-legacy-import-profile` sort avant la création du jeu (`Program.cs:39-43`).
- Pas de projet de test pour RGCE (`RacingGame.slnx`) ; précédent d'ADR pour un comportement des modes automation : ADR-0004 (`docs/decisions/0004-rgce-saves-user-settings-only-from-options.md`).

## Décisions verrouillées

Propositions P1 → P4 de l'agent, retenues telles quelles par l'auteur le 2026-10-15.

| Réf | Décision | Alternatives écartées |
|---|---|---|
| D1 | **Une source d'entrée vide pour tout le processus** quand un mode automation tourne : `Program.cs` pose `runtimeContext.WindowInputSource` sur une source RGCE qui renvoie toujours l'état vide, avant de créer le jeu ; le jeu remplace aussi le fournisseur de manette par un fournisseur vide après `base.Initialize()`. Écrans, conduite, pause, raccourcis, boutons MGUI et survol ignorent alors le joueur, sans un test par écran ni changement du sous-module. | **B** : garde dans chaque écran (fermeture et navigation), par un drapeau de `RaceFrontEndState`. 9 fichiers, et ne couvre ni la conduite, ni Échap, ni F1–F5, ni les boutons de la pause, ni le survol ; chaque nouvel écran doit y penser. **C** : correction dans CasaEngine seulement (boutons de souris ignorés quand la fenêtre n'a pas le focus de la souris). Protège aussi le jeu normal, mais change le sous-module et ne protège pas une automation dont la fenêtre reçoit elle-même des entrées. |
| D2 | Les **6 modes** qui créent le jeu : `--smoke-frontend`, `--capture-ui-screens`, `--capture-track-audit`, `--capture-car-profile-audit`, `--capture-car-top-speed-audit`, `--export-track-runtime-scene`. | Seulement `--capture-ui-screens`. |
| D3 | État souris fourni : `WindowInputSnapshot.Empty` du moteur (position (0, 0), aucun bouton). Les captures diront si (0, 0) survole quelque chose. | Une position hors de la zone client. |
| D4 | **ADR-0021 courte**, comme ADR-0004 pour l'autre comportement des modes automation. | Doc XML seulement. |
| D5 | **O5, arbitré par l'auteur le 2026-10-15** (« InactiveSleepTime à zéro et redimensionnement bloqué, fais-le ») : sous `IsAutomation`, `Game.InactiveSleepTime` vaut zéro, pour qu'une perte de focus ne ralentisse plus le lancement, et la fenêtre n'est plus redimensionnable par l'utilisateur. Enregistré dans une ADR-0022 courte qui complète l'ADR-0021. | Consigne de ne pas toucher à la fenêtre pendant un lancement. |

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
- `--capture-ui-screens`, deux lancements, `scripts/UiCaptureCompare` contre `artifacts/race-finished-capture/ref-a/ui` (`race-finished`) et `artifacts/element-naming/verify/ui` (9 autres états) : dans le seuil, ou tout écart expliqué (survol à (0, 0), D3).
- Jeu normal (sans option) : l'entrée réelle marche comme avant (vérification manuelle de l'auteur, 🧪).

---

## Phase 1 — Entrée ignorée pendant l'automation

### ✅ T1.1 — Source d'entrée vide pendant les modes automation

- Objectif : sous un des modes de D2, le jeu ne reçoit aucune entrée réelle (D1, D3).
- Fichiers : `RacingGameCasaEngine/Bootstrap/RaceLaunchOptions.cs` (propriété qui dit si un mode automation tourne), `RacingGameCasaEngine/Bootstrap/AutomationPlayerInput.cs` (nouveau : source de fenêtre, clavier, souris et manette vides ; fabrique d'UI qui coupe le suivi de manette de MGUI), `RacingGameCasaEngine/Program.cs` (pose les deux avant `new RacingGameCasaEngineGame`), `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` (manette vide après `base.Initialize()`, propriété `IgnoresPlayerInput`), `RacingGameCasaEngine/Screens/OptionsScreen.cs` (pas d'abonnement à `Window.TextInput` en mode automation).
- Étapes :
  1. Initialiser le sous-module du worktree ; build de référence.
  2. Source vide : `IWindowInputSource.GetSnapshot()` renvoie `WindowInputSnapshot.Empty` ; `IGamePadStateProvider.GetState` renvoie l'état par défaut (non connecté). Vérifier dans le code moteur que `FrameCachedWindowInputSource` et le routeur n'exigent rien de plus.
  3. Fabrique d'UI : enveloppe la fabrique MGUI et met `Desktop.InputTracker.GamePad.Enabled` à faux sur chaque `UIRoot` créé.
  4. `Program.cs` : si un mode automation tourne, pose la source vide et la fabrique avant de créer le jeu ; `Logs.WriteInfo` une fois.
  5. `Initialize` du jeu : en mode automation, `InputComponent.SetProviders` avec les fournisseurs vides.
  6. `OptionsScreen.Show` : pas d'abonnement à `Window.TextInput` quand le jeu ignore le joueur (constat de la revue).
- Validation : build ; sonde de maintien reconstruite sur ce code, avec au moins un clic réel pendant un maintien (validation globale) ; les 6 modes à code 0.
- Commit : `fix(racing-casa): ignore the player input under the automation modes`

> Validation (2026-10-15) :
> - Sous-modules du worktree initialisés sans réseau, depuis les dépôts du checkout principal (règles `insteadOf` passées en `-c`) : `6ed0ef3f7`, `4e94d7e8`, `9c0da031` ; SHA-1 de `.git/config` inchangé.
> - `dotnet build RacingGame.slnx` : 0 erreur, avant et après le changement. `RacingGameCasaEngine` recompilé seul : 0 avertissement, 0 erreur.
> - Le bin du worktree n'avait pas les 93 fichiers de contenu générés par CasaEngine (shaders `.xnb`, polices, icônes), que `dotnet build` ne produit pas : premier essai en échec au chargement (`ContentLoadException`, `Content/Shaders/DebugPrimitiveColor.xnb`). Copiés depuis le bin du checkout principal, dossier de sortie ignoré par git. Le reste du contenu est identique, aux fins de ligne près (26 fichiers texte).
> - **Revue adverse** en lecture seule (4 angles : contournements, ordre d'initialisation, régression en jeu normal, conformité ; 3 contradicteurs par constat) : un seul constat, trouvé par les 4 angles et confirmé 3 fois sur 3 (P3/P4). `OptionsScreen` lisait le texte tapé directement sur `Window.TextInput`. Corrigé : pas d'abonnement quand `IgnoresPlayerInput` est vrai. Tout le reste a été vérifié sain : ordre de pose dans `Program` et le constructeur, `??=`, aucune autre création de `UIRoot`, aucun autre abonnement aux événements de fenêtre dans RGCE ni dans `RacingGame.Gameplay`, jeu normal inchangé, éditeur non concerné.
> - **Sur le code final** (seul un commentaire XML de `AutomationPlayerInput.cs` a été ajouté ensuite ; le vérificateur a relancé les 6 modes sur la DLL finale, T1.3), par le helper (un seul processus, réglages restaurés à l'identique après chaque lancement, aucune entrée simulée) :
>   - sonde de maintien reconstruite sur ce code, 6 lancements : code 0. 16 vrais clics de l'auteur lus bruts par `Mouse.GetState(Window)`, dont 12 pendant le panneau d'arrivée (lancements 3 à 5), certains avec le curseur dans la zone client. Aucun n'atteint `MouseManager` (`gameLeftJustPressed=True` : 0), aucune fermeture du panneau. `ReturnToFrontEnd` n'est appelé que par le validateur ;
>   - les 6 modes sur le build du worktree, code 0, la ligne `Automation mode: the player input is ignored.` dans chaque journal, 0 avertissement et 0 erreur : `--capture-ui-screens` ×2, `--smoke-frontend` (`Smoke validation completed successfully`), `--capture-track-audit` (30 captures), `--capture-car-profile-audit` (6 scénarios), `--capture-car-top-speed-audit` (3 scénarios), `--export-track-runtime-scene` (3 pistes). Sorties des audits redirigées dans le bloc-notes.
> - Avant la correction d'`OptionsScreen`, une première série sur le code de T1.1 avait déjà donné 8 lancements de maintien à code 0 (7 vrais clics pendant les maintiens, aucun reçu par le jeu) et les 6 modes à code 0.
> - **Manette, preuve au runtime** (FIXHOLD2-2, code de T1.1 avant la correction d'`OptionsScreen`, qui ne touche pas la manette) : pendant le maintien, la manette `P0` de l'auteur, qui jouait à une autre application au premier plan, a envoyé Start deux fois puis B (updates 2108, 2156, 2194). Ce sont des boutons de fermeture du panneau (`RaceHudScreen.cs:112-116`). Le panneau est resté ouvert, et `ReturnToFrontEnd` n'a été appelé que par le validateur.
> - **Clavier, texte tapé et événements adressés à la fenêtre** : couverts par la lecture du code et la revue, sans preuve au runtime. Dans les lancements après correctif, la fenêtre du jeu n'a jamais été au premier plan : aucun clic ne l'a visée, aucune touche ne l'a atteinte.
> - Jeu normal : nouveau code atteint seulement sous `IsAutomation` (lecture et revue) ; la vérification manuelle reste à l'auteur (validation globale).
> - Après les lancements, le build de RGCE seul de 09:42:47 a de nouveau retiré du bin les shaders et les icônes copiés (dossiers modifiés à 09:42:48 ; mécanisme non établi). Fichiers recopiés ensuite. Un build de RGCE peut donc demander de recopier ces 93 fichiers avant de lancer le jeu depuis le worktree.

### ✅ T1.2 — Captures contre les références

- Objectif : montrer que les captures ne changent pas, ou expliquer chaque écart.
- Sources : deux lancements de `--capture-ui-screens` copiés dans `artifacts/automation-input/{run-a,run-b}/ui` (ignoré par git), rapports `UiCaptureCompare`.
- Étapes :
  1. Comparer run-a et run-b entre eux, puis aux références de la validation globale.
  2. Écart au-dessus du seuil : chercher s'il vient du survol (D3) ou d'O4 ; ne prendre une nouvelle référence qu'avec sa cause écrite.
- Validation : rapports dans `artifacts/automation-input/compare-*` ; note sous la tâche.
- Commit : `docs(racing-casa): record the captures with the player input ignored` (ce plan seulement, les artefacts ne sont pas suivis).

> Validation (2026-10-15) :
> - Captures : les deux lancements `--capture-ui-screens` du build final de T1.1 (`ui-run-20261015-093815` et `-093838`), copiés dans `artifacts/automation-input/{run-a,run-b}/ui` du checkout principal (ignoré par git). Rapports `UiCaptureCompare` dans `artifacts/automation-input/compare-*` ; écart par canal R, G, B, seuil 2,0.
> - **run-b** : identique au pixel aux références. Les 9 états valent 0,00 contre `artifacts/element-naming/verify/ui`, et `race-finished` vaut 0,00 contre `artifacts/race-finished-capture/ref-a/ui`. L'état souris vide de D3, position (0, 0), ne survole donc rien.
> - **run-a** : `race-finished` à 0,00 contre ref-a ; 6 des 9 autres états à 0,00 contre `verify/ui`. Les 3 écarts, tous dans le seuil, sont help (0,22 / 0,19 / 0,15), car-selection (0,75 / 0,70 / 0,71) et track-selection (0,27 / 0,23 / 0,18). Leur moyenne RGB (0,19, 0,72 et 0,23) est celle des variantes de cadence déjà relevées par O4 du plan `race-finished`, et le help et la car-selection de run-a sont identiques (0,00) à ceux de ref-a.
> - `race-finished` contre `verify/ui` : 33,14 / 33,53 / 32,99 dans les deux lancements, la pose figée de `c887e95`, attendue.
> - Aucune nouvelle référence : aucun écart ne vient de ce chantier.
> - **O4 ci-dessous** : ces variantes persistent alors qu'aucune entrée n'atteint plus le jeu. Le curseur réel n'en est donc pas la cause.

### ✅ T1.3 — ADR, clôture d'O3 et vérification

- Objectif : décision enregistrée (D4), O3 clos, vérification indépendante.
- Fichiers : `docs/decisions/0021-…md` et l'index des ADR (D4), `ai-agent/tasks/rgce-race-finished-capture-tasks.md` (O3 : cause et lien vers ce plan), `ai-agent/README.md`, ce plan.
- Étapes :
  1. ADR-0021 avec le skill `adr`, (D4).
  2. O3 : cause (bouton de souris global, `IsActive` sans focus, panneau fermé par tout clic) et correctif.
  3. Passe `verifier` fraîche sur T1.1 et T1.2.
- Validation : verdict CONFIRMED ; docs seulement, pas de build.
- Commit : `docs(adr): record that the automation modes ignore the player input` (ADR, skill `adr`), puis `docs(racing-casa): close the automation input plan` (plans et index).

> Validation (2026-10-15) :
> - ADR-0021 (`Accepted`) et sa ligne d'index ; O3 du plan `race-finished` clos avec sa cause et le correctif ; lignes de `ai-agent/README.md` à jour.
> - **Vérificateur frais : CONFIRMED** sur les 5 affirmations (couverture des entrées, 6 modes, jeu normal inchangé, captures, exactitude des notes).
>   - Son premier lancement depuis le bin du worktree a échoué au chargement (`ContentLoadException`) : le build de 09:42:47 avait retiré les shaders copiés (voir la note de T1.1). Il a ensuite tout lancé depuis une copie du bin du worktree complétée des 93 fichiers (DLL finale, même SHA-1). Les 6 modes : code 0, ligne d'entrée ignorée présente, 0 avertissement, 0 erreur, réglages restaurés à l'identique.
>   - Sa capture fraîche (`ui-run-20261015-094901`) : `race-finished` à 0,00 contre ref-a ; les 9 autres états dans le seuil contre `verify/ui`. Écarts par canal : main-menu 0,70 / 0,66 / 0,65, options 0,39 / 0,34 / 0,28, help 0,22 / 0,19 / 0,15, race-hud 1,22 / 1,21 / 1,16 ; les autres à 0,00. Contre ref-a, ce race-hud vaut 0,70 / 0,70 / 0,67, une valeur connue d'O4.
>   - Ses 3 remarques P4 : contenu du bin (fichiers recopiés), race-hud à 1,22 (dans le seuil, variante de cadence, O4), formulation « octets finaux » (reformulée dans la note de T1.1).
> - **Critique de complétude** en lecture seule, 2 contradicteurs par écart :
>   - 6 écarts confirmés par les deux contradicteurs. Deux étaient classés P3 :
>     - l'état de la fenêtre agit encore sur la cadence d'un lancement : reporté en **O5**, hors D1 ;
>     - pas de preuve au runtime pour le clavier : reclassé P4 par les contradicteurs, la note de T1.1 et l'ADR disent maintenant « par lecture du code ».
>   - Quatre étaient des P4 :
>     - preuve manette au runtime non citée : citée (FIXHOLD2-2) ;
>     - preuves seulement dans le bloc-notes : copiées dans `artifacts/automation-input/evidence/` ;
>     - phrases plus larges que les mesures : l'ADR et l'index donnent les chiffres exacts, et `ForcedControlInput` est limité aux audits voiture ;
>     - liste `IsAutomation` tenue à la main : consigne ajoutée à l'ADR.
>   - 2 écarts contestés, 1 voix sur 2 :
>     - la vibration de la manette reste active : notée dans l'ADR ;
>     - T1.1 marquée ✅ sans la vérification manuelle du jeu normal : T1.1 reste ✅, car ses critères n'incluent pas cette vérification, qui reste ouverte dans la validation globale.
>   - 1 écart réfuté.
> - Aucun code touché dans cette tâche : pas de build.

---

## Phase 2 — État de la fenêtre pendant l'automation (O5, D5)

### ✅ T2.1 — Pas de pause d'inactivité ni de redimensionnement sous automation

- Objectif : sous un des modes de D2, la cadence ne dépend plus du focus de la fenêtre, et l'utilisateur ne peut plus redimensionner ni agrandir la fenêtre ; la taille posée par le validateur s'applique toujours.
- Fichiers : `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs`.
- Faits vérifiés :
  - `Game.Tick` dort `InactiveSleepTime` à chaque tick quand `!IsActive` et que le délai vaut au moins 1 ms ; la valeur par défaut est 0,02 s, et le setter public refuse seulement une valeur négative (MonoGame DesktopGL 3.8.5.1 décompilé) ;
  - aucun code du dépôt ni de CasaEngine ne le change, et rien n'écoute `Activated` ou `Deactivated` (`rg`) ;
  - `CasaEngineGame` applique `Window.AllowUserResizing` une seule fois, depuis `RuntimeContext.ProjectSettings.AllowUserResizing` (`CasaEngine/CasaEngine/Framework/Application/CasaEngineGame.cs:482`). RGCE le met à vrai dans son `Initialize`, avant `base.Initialize()` (`RacingGameCasaEngineGame.cs:198`). `SdlGameWindow` le garde au travers des changements de taille (`Sdl.Window.SetResizable`).
- Étapes :
  1. `Initialize` : `AllowUserResizing` vaut `!_ignorePlayerInput`.
  2. Après `base.Initialize()`, sous `_ignorePlayerInput` : `InactiveSleepTime = TimeSpan.Zero`.
  3. Sonde A/B (copie de bloc-notes, jamais dans le dépôt) : fenêtre forcée inactive par réflexion sur `Platform.IsActive` pendant 10 s, updates et draws par seconde, `InactiveSleepTime`, drapeau SDL redimensionnable et style Win32 de la fenêtre. A = code de `d89ddf4`, B = code de cette tâche.
- Validation : build ; sonde A/B (A : moins de draws par seconde pendant l'inactivité et fenêtre redimensionnable ; B : même cadence active ou non, et fenêtre non redimensionnable) ; les 6 modes à code 0, dont `--smoke-frontend`, qui change la résolution par programme ; deux `--capture-ui-screens` contre les références.
- Commit : `fix(racing-casa): keep the automation runs independent of the window state`

> Validation (2026-10-15) :
> - `RacingGameCasaEngine` recompilé : 0 avertissement, 0 erreur. Le bin du worktree a gardé ses 93 fichiers de contenu copiés cette fois.
> - **Sonde A/B**, 2 lancements `--capture-ui-screens` par variante, par le helper. Fenêtre forcée inactive des updates 300 à 900, par réflexion sur `Platform.IsActive` (aucune entrée simulée). Journaux `O5A-*` et `O5B-*`.
>   - A, code de `d89ddf4` : `InactiveSleepTime` à 20 ms. Course au point stable, fenêtre inactive (updates 650 à 880) : 33,8 à 37,7 draws/s pour 59 à 68 updates/s, soit 0,55 à 0,57 draw par update. Exemple : 38,6 draws/s pour 60,3 updates/s à l'update 778. `AllowUserResizing`, le drapeau SDL redimensionnable et le style Win32 (`WS_THICKFRAME`, `WS_MAXIMIZEBOX`) valent vrai tout au long du lancement.
>   - B, code de cette tâche : `InactiveSleepTime` à 0. Même fenêtre inactive : 60,0 draws/s pour 60,0 updates/s (updates 779 et 839), soit 0,86 draw par update en moyenne sur la plage, rattrapage du chargement compris. Les quatre indicateurs de redimensionnement valent faux tout au long du lancement, y compris après la taille de capture 1920×1080 posée par le validateur.
>   - Dans A1 et B1, la fenêtre a aussi perdu le focus d'elle-même vers la 2e et la 3e seconde : la situation que D5 vise.
> - **Les 6 modes** sur le build du worktree (`REPO4-*`) : code 0, ligne d'entrée ignorée présente, 0 avertissement, 0 erreur, réglages restaurés à l'identique. `--smoke-frontend` change la résolution par programme et la vérifie : le redimensionnement par programme marche toujours.
> - **Captures** (`ui-run-20261015-102940` et `-103003`, copiées dans `artifacts/automation-input/{run-c,run-d}/ui`, rapports `compare-*`) : run-c et run-d identiques entre elles (0,00 partout) ; les 9 états à 0,00 contre `verify/ui` dans les deux ; `race-finished` à 0,00 contre ref-a (33,14 / 33,53 / 32,99 contre `verify/ui`, la pose figée attendue).

### ⏳ T2.2 — ADR-0022, clôture d'O5 et vérification

- Objectif : D5 enregistrée, O5 clos, vérification indépendante.
- Fichiers : `docs/decisions/0022-…md`, l'index des ADR et l'ADR-0021 (statut : complétée par l'ADR-0022), `ai-agent/README.md`, ce plan.
- Étapes :
  1. ADR-0022 avec le skill `adr`.
  2. O5 clos.
  3. Revue adverse et passe `verifier` fraîche sur T2.1.
- Validation : verdict CONFIRMED ; docs seulement, pas de build.
- Commit : `docs(adr): record that the automation runs ignore the window state`, puis `docs(racing-casa): close the window state point of the automation input plan`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | ~~Arbitrage de P1 → P4 par l'auteur.~~ Réglé le 2026-10-15 : options recommandées (D1 → D4). | T1.1 → T1.3 |
| O2 | **Hors périmètre, à décider** : en jeu normal aussi, un clic gauche dans une autre application est vu par le jeu. Il ferme le panneau d'arrivée, le Splash et l'aide, qui ne testent pas la position. Correction possible dans CasaEngine (`MonoGameWindowInputSource.GetMouseState` : boutons relâchés quand la fenêtre n'a pas le focus de la souris), à traiter dans le dépôt du moteur. | — |
| O3 | **Hors périmètre** : `IsActive` reste vrai quand la fenêtre n'a jamais eu le focus, donc la suppression du clavier de `MonoGameWindowInputSource` ne se déclenche pas dans ce cas. Sans effet constaté ici (aucune touche journalisée), à signaler avec O2. | — |
| O4 | ~~Lien possible avec O4 du plan `race-finished` : les écrans à survol lisent la position réelle du curseur.~~ Écarté par T1.2 : les mêmes variantes (help, car-selection, track-selection) persistent sans aucune entrée ; le curseur n'en est pas la cause. O4 du plan `race-finished` reste ouvert. La cadence peut encore dépendre de l'état de la fenêtre (O5). | T1.2 |
| O5 | **Arbitré le 2026-10-15 (D5), traité par la phase 2.** Relevé par la critique de complétude de T1.3 : l'état de la fenêtre agit encore sur un lancement d'automation, sans être une entrée au sens de D1. MonoGame dort 20 ms par image quand la fenêtre est inactive (`Game.Tick`, `InactiveSleepTime` à 0,02 s par défaut, décompilé ; aucun code du dépôt ne le change), et le focus change en cours de lancement (FIXHOLD2-6 et -8, `focus change: active=False`). Cela change la cadence, donc peut-être les variantes d'O4. La fenêtre est aussi redimensionnable (`AllowUserResizing = true`, `RacingGameCasaEngineGame.cs:198`). Pistes : `InactiveSleepTime` à zéro et redimensionnement interdit sous `IsAutomation`, ou consigne de ne pas toucher à la fenêtre pendant un lancement. | — |

## Hors périmètre

- Toute modification de CasaEngine ou de MonoGame (O2, O3).
- Le comportement du jeu normal : l'entrée réelle n'y change pas.
- O4 du plan `race-finished`, sauf ce que T1.2 en mesure.
