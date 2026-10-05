# Plan agent IA — Les modes automation de RacingGameCasaEngine n'écrivent plus les réglages de l'auteur

Plan d'exécution de la demande de l'auteur du 2026-10-05 : les modes automation de RacingGameCasaEngine (RGCE) écrasent les réglages persistés de l'auteur dans `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\display-settings.json`. Le fichier `front-end-options.json` est touché lui aussi. Après une session de validateurs, la résolution du premier avait changé plusieurs fois.

Les décisions D1 → D5 ci-dessous ont été arbitrées avec l'auteur le 2026-10-05 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

- Les modes automation laissent `display-settings.json` et `front-end-options.json` intacts : même SHA-1 et même date de modification avant et après les runs. Ces modes sont `--smoke-frontend`, `--capture-ui-screens`, `--capture-track-audit`, `--capture-car-profile-audit`, `--capture-car-top-speed-audit` et `--export-track-runtime-scene`, soit les 6 validateurs de `Bootstrap/*Validator.cs`.
- Chaque validateur garde ses contrôles. Le smoke vérifie toujours qu'une résolution choisie dans les options est bien appliquée.
- Les réglages ne sont plus enregistrés qu'à la sortie de l'écran Options par son bouton Retour (D1). C'est aussi vrai en jeu normal.
- `--capture-ui-screens` garde un back buffer fenêtré de 1920×1080 sur ses 10 captures, course comprise (D2).

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-05)

Découverte en lecture seule dans la session principale. Les fichiers:ligne renvoient à `remaster` `8f14af9`.

- **Branche et arbre**
  - Le worktree `.claude/worktrees/gracious-kowalevski-b09684` portait la branche `claude/gracious-kowalevski-b09684`, basée sur `master` (`3ca982e`), sans commit propre et sans RGCE. Sur décision de l'auteur (D3), elle a été recalée sur `remaster` `8f14af9` (`git reset --hard remaster`, arbre propre, aucun commit perdu : `git log master..HEAD` était vide).
  - `remaster` est extraite dans le checkout principal `D:\development\repo\RacingGame`, où d'autres sessions committent. Ce chantier n'y touche pas.
  - Le sous-module `CasaEngine` du worktree n'est pas initialisé (`git submodule status` : `-f8629e0`). Le checkout principal a `CasaEngine` en `f8629e0`, `CasaEngine/MGUI` en `d3e0cd1` et `CasaEngine/NvgSharp` en `9c0da03`, tous conformes (`git submodule status --recursive` sans `+` ni `-`).
  - Non suivi : `.serena/`, créé par l'activation de Serena sur le worktree. Il n'est jamais indexé.
- **Qui écrit les deux fichiers**
  - Les chemins sont construits dans `Program.cs:50-52`, sous `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine`. `display-settings.json` est lu au démarrage (`Program.cs:54-65`) et `front-end-options.json` dans le constructeur du jeu (`Bootstrap/RacingGameCasaEngineGame.cs:70`).
  - Une seule écriture : `RacingGameCasaEngineGame.ApplyFrontEndOptions` (`RacingGameCasaEngineGame.cs:124-151`). Elle borne les valeurs, puis applique l'affichage par `ApplyDisplaySettings`, qui ne fait aucune entrée-sortie (`CasaEngine/CasaEngine/Framework/Application/CasaEngineGame.cs:178-225`). Elle enregistre ensuite les deux fichiers (`:140-141`), puis applique les volumes et l'overlay FPS. Les deux fichiers n'ont pas d'autre écrivain : `rg "DisplaySettingsPersistence\.Save|SaveDisplaySettings\("` et `rg FrontEndOptionsPersistence`. Le jeu n'enregistre rien d'autre à la fermeture.
  - Trois appelants :
    - `OptionsScreen.ApplyAndClose`, au bouton Retour (`Screens/OptionsScreen.cs:76, 99-103`) ;
    - `OnWorldLoaded`, à **chaque** chargement de monde : démarrage du front-end, départ et retour de course (`RacingGameCasaEngineGame.cs:169-177`) ;
    - `RaceFrontEndFlow.ApplyOptionsAndReturnToMainMenuForAutomation`, utilisé par le smoke (`Bootstrap/RaceFrontEndFlow.cs:127-132`).
- **Ce que chaque validateur change dans l'état enregistré**
  - `--smoke-frontend` bascule `SelectedResolutionIndex` entre 1280×720 et 1920×1080, puis applique les options. Il vérifie ensuite `GetDisplaySettings()` sur le menu principal (`Bootstrap/FrontEndNavigationSmokeValidator.cs:96-115`). Il démarre et termine aussi une course.
  - `--capture-car-profile-audit` règle `SelectedDrivingMode` sur chaque mode (`Bootstrap/CarProfileAuditValidator.cs:111`). `--capture-car-top-speed-audit` le passe en `Simulation` (`Bootstrap/CarTopSpeedAuditValidator.cs:107`). Les deux enregistrent ce mode au départ de course, par `OnWorldLoaded`.
  - `--capture-ui-screens`, `--capture-track-audit` et `--export-track-runtime-scene` ne règlent que des index de voiture, de couleur et de piste, qui ne sont pas persistés. Ils réécrivent quand même les deux fichiers à chaque chargement de monde.
  - `--verify-legacy-import-profile` sort avant la création du jeu (`Program.cs:39-43`) et n'écrit rien.
- **Capture UI et résolution**
  - `UiScreenCaptureValidator` applique 1920×1080 fenêtré sans l'enregistrer, à sa première mise à jour (`Bootstrap/UiScreenCaptureValidator.cs:81-82`). Il n'attend cette taille qu'avant la capture du Splash (`:109, 250-254`).
  - Au départ de course, `OnWorldLoaded` réapplique la résolution, le plein écran et la VSync de l'état du front-end (`RacingGameCasaEngineGame.cs:130-139`). L'index de résolution de l'état vient de `SyncOptionsState`, appelé une seule fois, dans le constructeur de `RaceFrontEndFlow` (`RaceFrontEndFlow.cs:28`, `RacingGameCasaEngineGame.cs:113-122`). Il est calculé sur `Window.ClientBounds` (`CasaEngineGame.cs:92-104`), avant `Initialize`.
  - On ne sait donc pas, sans lancer le jeu, si les captures `race-hud`, `pause` et `race-finished` sortent en 1920×1080. Le commentaire du validateur le promet (`UiScreenCaptureValidator.cs:11-12`). Il dit aussi que le smoke bascule la résolution enregistrée, ce qui ne sera plus vrai après T1.1.
  - Correspondance index ↔ résolution : `MenuResolutions` et `GetResolutionIndex`, privés (`RacingGameCasaEngineGame.cs:26-32, 430-441`). 1920×1080 est l'index 1.
- **Réglages actuels de l'auteur** (lus le 2026-10-05, non modifiés)
  - `display-settings.json` : 1920×1080, fenêtré, VSync. SHA-1 `18eafcb4c8df587195def3f11aead8a887090e1a`.
  - `front-end-options.json` : `SelectedDrivingMode` vaut `Simulation`. SHA-1 `cdf4b80185913ece1d86d954751a1c492ce09728`.
  - Les deux fichiers datent du 2026-10-05 21:02. Le SHA-1 de référence sera relevé de nouveau juste avant la validation (T2.1).
- **Validation disponible**
  - Aucun projet de test pour RGCE (`RacingGame.slnx`).
  - Les validateurs sortent avec le code 0 en cas de succès et 1 en cas d'échec. Délais : smoke 20 s, capture de pistes 60 s, profils voiture 120 s, vitesse de pointe 180 s, export de scène 45 s, capture UI 120 s.
  - Journal : `racinggame-casaengine-<pid>.log` à côté de l'exécutable (`Program.cs:32-33`), avec les préfixes `[Warning] ` et `[Error] ` (`CasaEngine/CasaEngine/Core/Logging/FileLogger.cs:10-11`).
  - Sorties par défaut : les deux audits voiture écrivent dans `artifacts/` du dépôt (`CarProfileAuditValidator.cs:426-432`, `CarTopSpeedAuditValidator.cs:454-460`). L'export de scène écrit sous `%LOCALAPPDATA%` (`TrackRuntimeSceneExportValidator.cs:174-175`). Les options `--car-profile-audit-file`, `--car-top-speed-audit-file` et `--track-runtime-export-file` les redirigent (`Program.cs:75-77`).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Les réglages ne sont enregistrés qu'au bouton Retour de l'écran Options. `ApplyFrontEndOptions` applique sans enregistrer. Un chargement de monde et le chemin du smoke n'écrivent donc plus rien, en automation comme en jeu normal (auteur, 2026-10-05). |
| D2 | `--capture-ui-screens` place aussi l'état du front-end sur 1920×1080 fenêtré, si bien que la réapplication au départ de course garde la taille de capture (auteur, 2026-10-05). |
| D3 | La branche du worktree, `claude/gracious-kowalevski-b09684`, est recalée sur `remaster` et porte le chantier. Jamais de push (auteur, 2026-10-05). |
| D4 | `CasaEngine/` et ses sous-modules ne sont pas modifiés (règle conservée des chantiers précédents). |
| D5 | Plan dans `ai-agent/tasks/` selon `ai-agent/plan-template.md`. La décision D1 est consignée en ADR-0004 (règle « une décision de persistance prise en discussion devient une ADR »). |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **Découpage de D1** : `ApplyFrontEndOptions` garde les bornes et l'application. Elle applique l'affichage, les volumes et l'overlay, sans entrée-sortie. Les deux écritures passent dans une nouvelle méthode `internal void SaveFrontEndOptions(RaceFrontEndState state)`. Celle-ci enregistre l'affichage courant (`SaveDisplaySettings`) puis les options (`FrontEndOptionsPersistence.Save`), avec les mêmes appels et le même ordre qu'avant. `OptionsScreen.ApplyAndClose` appelle `ApplyFrontEndOptions`, puis `SaveFrontEndOptions`, puis revient au menu. `OnWorldLoaded` et `ApplyOptionsAndReturnToMainMenuForAutomation` ne changent pas : ils appliquent toujours, mais n'écrivent plus. |
| P2 | **Verrou de D2** : `GetResolutionIndex` passe de `private static` à `internal static`. Au démarrage, le validateur applique 1920×1080 comme aujourd'hui. Il règle ensuite l'état sur ces valeurs : `SelectedResolutionIndex = GetResolutionIndex(1920, 1080)`, `IsFullscreen = false`, `EnableVSync = current.IsVSyncEnabled`. La réapplication au départ de course ne change alors plus l'affichage. Le commentaire de classe est mis à jour. |
| P3 | **Sous-module du worktree** : initialisation sans réseau, depuis les dépôts locaux du checkout principal, qui sont aux commits attendus. La commande est `git -c protocol.file.allow=always -c url.<chemin local>.insteadOf=<URL GitHub> … submodule update --init --recursive`, une règle `insteadOf` par sous-module : `CasaEngine`, `MGUI`, `NvgSharp`. La configuration partagée `.git/config` n'est pas modifiée. En cas d'échec, la tâche passe en ⚠️ Blocked avec la question « clone réseau depuis GitHub ? ». |
| P4 | **Validation** : runs séquentiels de l'exécutable du worktree. Les sorties des audits et de l'export vont dans le scratchpad de la session, par les options de fichier, pour garder l'arbre propre. Chaque run doit sortir avec le code 0, sans `[Warning]` ni `[Error]` dans son journal. Le SHA-1 et la date de modification des deux fichiers doivent être identiques avant et après chaque run. La date prouve qu'aucune réécriture n'a eu lieu, même à contenu égal. |
| P5 | **Vérification indépendante** : passe `verifier` sur le résultat final, avant de déclarer le chantier fait (`AGENTS.md`, délégation). Le chemin d'enregistrement du jeu normal (bouton Retour d'Options) n'est exercé par aucun mode automation, et aucune touche ni aucun clic n'est simulé. Il reste donc en 🧪, à vérifier par l'auteur. |

## Règles d'exécution pour l'agent

- **Branche `claude/gracious-kowalevski-b09684`**, dans ce worktree (D3). Ne jamais committer sur `master` ni sur `remaster`. Ne jamais pousser.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Aucun projet de test n'existe pour RGCE.
- **Ne jamais modifier** `CasaEngine/` ni ses sous-modules (D4).
- Si le code est écrit mais qu'une vérification manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** `.serena/` ni les fichiers d'autres sessions : `git add` fichier par fichier.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- **Fichiers de l'auteur** : jamais modifiés ni restaurés par l'agent. Avant tout lancement du jeu, relever leur SHA-1 et leur date. Si un run les change, s'arrêter : passer la tâche en ⚠️ Blocked et donner à l'auteur l'ancien et le nouveau contenu. Aucune restauration n'est faite sans son accord. Jamais d'envoi de touches ni de mouvements de souris simulés.
- **Ordre** : aucun lancement du jeu tant que le code de T1.1 n'est pas en place et buildé. Avant cela, lancer un validateur réécrirait encore les fichiers.
- **Budget** : au plus 3 cycles « correction → build ou validation » par tâche. Un 4e échec passe la tâche en ⚠️ Blocked, avec l'erreur et la question dans « Points ouverts », puis arrêt.
- **Retour arrière** : avant commit, suppression des seuls fichiers créés par la tâche et `git restore` des seuls fichiers suivis qu'elle a modifiés ; après commit, `git revert <SHA>` ; jamais de `git checkout`, `git reset --hard`, `git clean` ni `git stash`.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur.
- Les 6 validateurs, puis `--verify-legacy-import-profile`, lancés un par un : code 0, journal sans `[Warning]` ni `[Error]`.
- SHA-1 et date de modification de `display-settings.json` et `front-end-options.json` identiques avant le premier run, après chaque run et après le dernier.
- Journal du smoke : ligne `Smoke validation: Verified applied resolution on MainMenu`.
- `--capture-ui-screens` : 10 PNG en 1920×1080 dans le dossier `ui-run-…`, captures de course comprises (dimensions lues dans l'en-tête PNG).
- Passe `verifier` indépendante sur le résultat final (P5).
- Vérification manuelle par l'auteur (🧪) :
  - en jeu normal, changer la résolution et un volume dans Options, puis Retour : les deux fichiers sont mis à jour ;
  - relancer le jeu : les réglages sont conservés ;
  - démarrer puis quitter une course : la date des deux fichiers ne change pas.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan, index et ADR

- Objectif : versionner ce plan, l'ajouter à l'index et consigner D1.
- Fichiers : `ai-agent/tasks/rgce-automation-settings-tasks.md`, `ai-agent/README.md`, `docs/decisions/0004-rgce-saves-user-settings-only-from-options.md`, `docs/decisions/README.md`.
- Étapes :
  1. Écrire l'ADR-0004 avec le skill `adr`, selon `docs/decisions/template.md`. Titre : « RacingGameCasaEngine saves the user settings only when the player leaves the Options screen ».
     - Contexte : les écritures de `ApplyFrontEndOptions` à chaque chargement de monde et dans les validateurs.
     - Décision : D1.
     - Conséquences : les validateurs n'écrivent plus. Un chargement de monde n'enregistre plus en jeu normal. Un redimensionnement de fenêtre ou un état modifié hors de l'écran Options n'est plus enregistré au chargement de monde suivant. Aucun mode automation n'exerce plus l'enregistrement.
  2. Ajouter l'ADR à l'index de `docs/decisions/README.md` et le plan au tableau de `ai-agent/README.md`.
- Validation : fichiers présents ; liens des deux index valides.
- Commit : `docs(racing-casa): plan automation runs that leave the user settings untouched`
- Note de validation : plan relu par un `plan-verifier` (READY), approuvé par l'auteur, mode AUTO (2026-10-05). ADR-0004 écrite et indexée ; liens de `ai-agent/README.md` et `docs/decisions/README.md` vérifiés.

### ⏳ T0.2 — Sous-module du worktree et build de départ

- Objectif : pouvoir builder et lancer RGCE depuis ce worktree (P3). Aucun fichier suivi n'est touché, donc pas de commit.
- Étapes :
  1. Initialiser `CasaEngine`, `CasaEngine/MGUI` et `CasaEngine/NvgSharp` depuis les dépôts locaux du checkout principal (P3).
  2. Vérifier `git submodule status --recursive` : `f8629e0`, `d3e0cd1` et `9c0da03`, sans `+` ni `-`.
  3. `dotnet build RacingGame.slnx` sur `8f14af9` + T0.1 : 0 erreur. Relever le chemin de `RacingGameCasaEngine.exe`.
- Validation : sous-modules aux commits attendus ; build 0 erreur. `.git/config` est inchangé : même SHA-1 avant et après.
- Commit : aucun. La note de validation entre dans le commit de T1.1.

---

## Phase 1 — Code

### ⏳ T1.1 — Enregistrer les réglages au seul bouton Retour d'Options

- Objectif : D1, par le découpage P1.
- Fichiers : `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs`, `RacingGameCasaEngine/Screens/OptionsScreen.cs`.
- Étapes :
  1. Dans `ApplyFrontEndOptions`, retirer `SaveDisplaySettings(_displaySettingsFileName)` et `FrontEndOptionsPersistence.Save(_frontEndOptionsFileName, state)`.
  2. Ajouter juste après `internal void SaveFrontEndOptions(RaceFrontEndState state)`, qui fait ces deux appels dans cet ordre, avec un court commentaire de doc : seul l'écran Options l'appelle, et les chargements de monde comme les validateurs appliquent sans enregistrer.
  3. `OptionsScreen.ApplyAndClose` : `_game.ApplyFrontEndOptions(_state); _game.SaveFrontEndOptions(_state); _back();`. Le résumé de la classe passe de « applied by the Back button » à « applied and saved by the Back button ».
  4. `rg "SaveDisplaySettings\(|FrontEndOptionsPersistence\.Save"` dans `RacingGameCasaEngine` : un seul appelant, `SaveFrontEndOptions`, lui-même appelé par `OptionsScreen` seulement.
- Validation :
  - `dotnet build RacingGame.slnx` 0 erreur.
  - Relever le SHA-1 et la date des deux fichiers.
  - `--smoke-frontend` : code 0, journal propre, ligne `Verified applied resolution on MainMenu`.
  - Lancer `--smoke-frontend` une 2e fois : même résultat. Les deux runs partent de la même résolution enregistrée.
  - SHA-1 et dates inchangés.
  - 🧪 pour le chemin d'enregistrement du jeu normal (P5).
- Commit : `fix(racing-casa): save the user settings only from the Options screen`

### ⏳ T1.2 — Garder 1920×1080 pendant toute la capture UI

- Objectif : D2, par le verrou P2.
- Fichiers : `RacingGameCasaEngine/Bootstrap/UiScreenCaptureValidator.cs`, `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` (visibilité de `GetResolutionIndex`).
- Étapes :
  1. `GetResolutionIndex` : `private static` → `internal static`.
  2. Dans le bloc de démarrage du validateur, après `ApplyDisplaySettings(…, persistToProjectSettings: false)`, régler l'état comme décrit en P2.
  3. Commentaire de classe :
     - retirer « (the --smoke-frontend run toggles the saved resolution) » ;
     - dire que l'état du front-end est réglé sur la même taille, pour que la réapplication au départ de course la garde, et que rien n'est enregistré.
- Validation :
  - `dotnet build RacingGame.slnx` 0 erreur.
  - `--capture-ui-screens` : code 0, journal propre, 10 PNG en 1920×1080, dont `ui-race-hud`, `ui-pause` et `ui-race-finished`.
  - SHA-1 et dates des deux fichiers inchangés.
- Commit : `fix(racing-casa): keep the UI capture back buffer at 1920x1080 through the race`

---

## Phase 2 — Validation et clôture

### ⏳ T2.1 — Validation globale, vérification indépendante et clôture

- Objectif : prouver l'objectif sur les 6 validateurs, puis clore le plan.
- Fichiers : ce plan, `ai-agent/README.md`.
- Étapes :
  1. Relever le SHA-1 et la date des deux fichiers.
  2. Lancer un par un :
     - `--smoke-frontend` ;
     - `--capture-ui-screens` ;
     - `--capture-track-audit` ;
     - `--capture-car-profile-audit --car-profile-audit-file <scratchpad>\car-profile-audit.md` ;
     - `--capture-car-top-speed-audit --car-top-speed-audit-file <scratchpad>\car-top-speed-audit.md` ;
     - `--export-track-runtime-scene --track-runtime-export-file <scratchpad>\track-runtime-scene.json` ;
     - `--verify-legacy-import-profile`.
     Après chaque run : code de sortie, `[Warning]` et `[Error]` dans le journal, SHA-1 et date des deux fichiers.
  3. `git status --short` : seul `.serena/` est non suivi, et `artifacts/` n'a pas changé.
  4. Passe `verifier` (P5) avec l'objectif, le diff `8f14af9..HEAD` et les preuves. Les retours sont traités selon leur gravité.
  5. Mettre à jour la ligne du plan dans `ai-agent/README.md` : exécuté, et vérifications manuelles 🧪 restantes.
- Validation : tous les points de « Validation globale », sauf la vérification manuelle de l'auteur.
- Commit : `docs(racing-casa): close the automation settings plan`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | `front-end-options.json` contient `SelectedDrivingMode: Simulation`, la valeur qu'impose `--capture-car-top-speed-audit`. Un ancien plan notait déjà avoir dû remettre `Arcade` après des audits (`docs/racinggame-vehicle-arcade-simulation-plan.md:317`). Était-ce le choix de l'auteur ? Le chantier ne touche pas ce fichier : l'auteur décide s'il remet `Arcade`. | — |

## Hors périmètre

- Une réécriture du mode d'enregistrement (format, emplacement, écriture atomique) ou de `DisplaySettingsPersistence` dans CasaEngine (D4).
- `SyncOptionsState`, appelé avant `Initialize` sur `Window.ClientBounds`, qui peut donner un index de résolution sans rapport avec les réglages enregistrés. Il est seulement signalé, et ne sera documenté que si la validation montre un effet.
- Un garde-fou de taille sur les captures de course, sous la forme d'un contrôle `HasCaptureSize`. D2 verrouille la taille, et la validation la mesure.
- La remise en état de `front-end-options.json` (O1).
- Les vérifications manuelles 🧪 des chantiers précédents.
