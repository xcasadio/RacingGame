# Plan agent IA — Mise à jour de RacingGame vers CasaEngine f8629e05 / MGUI d3e0cd12

Plan d'exécution du chantier « CasaEngine et MGUI ont été mis à jour, corriger les problèmes de RacingGame ».
Les décisions D1 → D13 ci-dessous ont été arbitrées avec l'auteur les 2026-09-26 et 2026-09-27 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Avant d'écrire le plan** : questions posées en une seule fois (2026-09-26) ; la voie « import `.X` par l'éditeur » a été reposée avec les mesures, puis remplacée par une conversion outillée (2026-09-27).
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

Remettre en état de build et de fonctionnement les projets RacingGame contre le sous-module CasaEngine mis à jour (`295db0c6` → `f8629e05`, 877 commits) et son sous-module MGUI (`6be7a719` → `d3e0cd12`, 449 commits), **sans modifier CasaEngine ni MGUI** :

- le jeu legacy (`RacingGame` + `RacingGame.Shared`) compile et tourne en net9 / DesktopGL 3.8.5.1 avec le MGUI à jour ;
- les 57 modèles `.X` de RacingGame sont convertis en `.gltf` avec leurs textures (PNG) et leurs métadonnées d'effet (`extras`) par un outil versionné dans `scripts/` ;
- `RacingGameCasaEngine` compile et tourne en DesktopGL 3.8.5.1 sur les API actuelles de CasaEngine (entités, possession, `GameplayMode`, transforms, assets) et charge ces `.gltf`.

## État vérifié du dépôt (2026-09-26 / 2026-09-27)

Arbre de travail et Git :

- Branche `remaster`, HEAD `3ed325f update CasaEngine` (auteur, 2026-09-26 17:48) : gitlink `CasaEngine` `295db0c6` → `f8629e05`, seul changement du commit. Commit précédent `f82074e wip` (`RacingGameCasaEngine`) : fait partie de la base à migrer.
- `git status` (2026-09-27) : modifications non commitées de l'auteur ` M RacingGame/RacingGame.csproj` (`net8.0-windows` → `net9.0-windows`), ` M RacingGame.Shared/RacingGame.Shared.csproj` (`net8.0-windows` → `net9.0-windows`), ` M RacingGame.PipelineExtension/RacingGame.PipelineExtension.csproj` (`net8.0` → `net9.0`, `MonoGame.Framework.Content.Pipeline` `3.8.4` → `3.8.*`) — intégrées à T1.1 avec l'accord de l'auteur (D10) ; `?? .serena/`, `?? log.txt` (à ne jamais indexer).
- `RacingGame/Content/Content.mgcb:13` référence encore `..\..\RacingGame.PipelineExtension\bin\Debug\net8.0\RacingGame.PipelineExtension.dll` ; `RacingGame.PipelineExtension/bin/Debug/` contient `net8.0/` (ancienne sortie) et `net9.0/` ; 56 entrées `RacingGameModelProcessor` dans `Content.mgcb`.
- Sonde MGCB (2026-09-27, sorties dans le scratchpad, dépôt inchangé : `git status --porcelain --ignored` identique avant/après) : l'outil `dotnet-mgcb` 3.8.5.1 cible `net8.0` avec `rollForward: Major` (`~/.nuget/packages/dotnet-mgcb/3.8.5.1/tools/net8.0/any/mgcb.runtimeconfig.json`) et tourne sur le runtime .NET 8 installé ; avec l'extension compilée en `net9.0` : échec `Could not load file or assembly 'System.Runtime, Version=9.0.0.0'` / `Failed to create processor 'RacingGameModelProcessor'` ; avec une copie de l'extension en `net8.0` + `MonoGame.Framework.Content.Pipeline 3.8.5.1` : `Models/Building.X` construit en `/platform:DesktopGL` (modèle, `shaders/NormalMapping`, textures).
- Gitlink orphelin : `git ls-files -s` montre `160000 6be7a719… MGUI`, absent de `.gitmodules` (commits `8791a8f remove MGUI` puis `990822b update MGUI`). `RacingGame.slnx` et les csproj référencent `CasaEngine/MGUI/…` uniquement.

Build (`dotnet build RacingGame.slnx`, 2026-09-26) :

- Restore en échec (2026-09-26, avant les modifications de l'auteur), `NU1201` ×8 : `RacingGame.csproj` et `RacingGame.Shared.csproj` ciblaient `net8.0-windows`, les projets MGUI ciblent `net9.0` / `net9.0-windows` (`CasaEngine/Directory.Build.props` : `DotNetVersion` 9.0).
- Sonde (copie dans le scratchpad, net9 + renommage `Brushes.Border_Brushes`→`Brushes.BorderBrushes`, `Brushes.Fill_Brushes`→`Brushes.FillBrushes`) : `RacingGame.Shared` compile, **0 erreur** — sur un graphe MonoGame mixte (`WindowsDX/3.8.5.1` **et** `DesktopGL/3.8.5.1`), car `MGUI.MonoGame.LegacyRenderer.csproj:16` référence DesktopGL sans `PrivateAssets` (déjà le cas avec l'ancien MGUI `6be7a719`). Un état « jeu legacy en WindowsDX seul » n'existe donc pas avec ce MGUI.
- `RacingGameCasaEngine` fait partie de `RacingGame.slnx` (`RacingGame.slnx:13`) : depuis le commit `3ed325f` (bump CasaEngine), `dotnet build RacingGame.slnx` échoue, et ce jusqu'à la fin de la migration de `RacingGameCasaEngine`.
- `RacingGameCasaEngine` : 27 erreurs de déclaration, puis 25 erreurs de corps de méthode une fois les déclarations contournées dans la sonde :
  - `StaticModelImporter` supprimé (CasaEngine `726b2f5a1`, runtime glTF-only, ADR-0019) : `Components/LegacyCarVisualFactory.cs:30,72,84`, `Worlds/LegacyTrackSceneFactory.cs:56,219`, `Bootstrap/LegacyImportProfileVerifier.cs` (8 méthodes) ; chemins `.X` construits en `LegacyCarVisualFactory.cs:59` (`Car.x`) et `LegacyTrackSceneFactory.cs:211` (`{modelName}.X`) ;
  - `Pawn` supprimé (`0c9c55988`), `Pawn.Controller`/`InputEnabled` supprimés (`8710ae092`), setter `Controller.Pawn` privé (`8d5ce0c55`, possession par `Controller.Possess(Entity)`) ;
  - `GameMode` et `World.GameMode` supprimés (`08de2874f`) ; remplaçants : `GameplayMode` (`Framework/Gameplay/GameplayMode.cs:5` : `Start`, `Update`, `Pause`, `Resume`, `Stop`, `EvaluateResult`, hook `OnInitialize`), `GameplayResult` (`Running`, `Success`, `Failure`, `Cancelled`), `World.SetGameplayMode` (`World.cs:170`), `World.GameplayModeRunner` (`World.cs:71`) ;
  - `SceneComponent.Coordinates` renommé `LocalTransform` (`472b70775`), `CopyLocalTransformFrom` (`SceneComponent.cs:410`) ;
  - `AssetContentManager.Load` supprimé ; remplaçants `Acquire<T>(Guid)` (`AssetContentManager.cs:67`) et `LoadCopy<T>(Guid)` (`:98`) ;
  - `CasaEngine.Framework.Application.Components.DebugTools` passé dans `CasaEngine.Editor` (`9d73f9460`) ; aucun type utilisé par `RacingGameCasaEngine` (sonde : retrait du `using` sans nouvelle erreur).
- Vérifié (relecteur indépendant) : l'ancien `GameMode.StartMatch/EndMatch` ne faisait que changer `MatchState` (handlers vides) ; RacingGame ne lit ni `MatchState` ni `GameStateChanged`. Le drapeau `InputEnabled` du véhicule est toujours écrit avec `PlayerController.IsInputEnable` (`RuntimeRaceWorldBinder.cs:63,75`, `RaceFlowCoordinatorComponent.cs:50-51`, `RaceRuntimeUiCoordinator.cs:128,133`). `World.GetPlayerController(Entity)` est public (`World.cs:959`).

Backend MonoGame :

- Ancien CasaEngine : `MonoGame.Framework.WindowsDX 3.8.2.1105` ; actuel : `MonoGame.Framework.DesktopGL 3.8.5.1` (`CasaEngine/Directory.Packages.props`, commit `260c23f69`).
- Dernier build réussi de `RacingGameCasaEngine` : `bin/Debug/net9.0-windows/MonoGame.Framework.dll` version 3.8.2.1105 (WindowsDX). Aucun code WinForms dans RacingGame (`rg System.Windows.Forms` : 0).
- Jeu legacy : `RacingGame.csproj` et `RacingGame.Shared.csproj` référencent `MonoGame.Framework.WindowsDX 3.8.*`, `RacingGame.csproj` `MonoGame.Content.Builder.Task 3.8.*` ; `RacingGame/.config/dotnet-tools.json` mgcb 3.8.4 ; `RacingGame/Content/Content.mgcb`, `RacingGame/Content/Shaders/Shaders.mgcb` et `RacingGame.Shared/Content/Content.mgcb` (vide) en `/platform:Windows` ; la tâche MGCB 3.8.5.1 passe `/platform:$(MonoGamePlatform)` et utilise le manifeste d'outils du projet (`MonoGame.Content.Builder.Task.targets:174`) ; sortie dans `bin/$(MonoGamePlatform)` (`:79`). Les 10 shaders `.fx` incluent `Macros.fxh`, qui a une branche `#elif OPENGL` (vs_3_0/ps_3_0).
- Audio du jeu legacy : XACT (`RacingGame.Shared/Sounds/Sound.cs:52-54,75-82`), banques précompilées copiées (`RacingGame/Content/Content.mgcb:17-27`). Comportement sous DesktopGL non vérifié.
- `scripts/TrackPlacementExporter` : WindowsDX 3.8.2.1105 + AssimpNet 4.1.0, hors `RacingGame.slnx`, ne référence ni CasaEngine ni MGUI.

Modèles `.X` (sondes du 2026-09-26/27, dans le scratchpad) :

- 57 `.X` dans `RacingGame/Content/Models`. Leurs textures ne sont déclarées que dans l'`EffectInstance` (`Building.X:82-100`) : `diffuseTexture`, `normalTexture` (100 `.tga` distinctes), `reflectionCubeTexture`, `NormalizeCubeTexture` (`.dds`) ; effets `..\\shaders\\NormalMapping.fx` (78 matériaux) et `..\\shaders\\ReflectionSimpleGlass.fx` (4).
- Import éditeur (`AssimpToGltfConverter.Convert` + `GltfStaticModelReader.ReadWithMetadata`, `EditorAssetImportService.cs:366-370`) : 57/57 convertis, 0 image dans chaque `.glb` ; 51/57 font échouer le lecteur (`GltfStaticModelReader.cs:359`, transform en matrice : `Needs to be in SRT representation`).
- Le lecteur ne renvoie un chemin de texture que pour une image **externe** (`GltfStaticModelReader.cs:226-231`).
- Sonde de conversion sans code moteur : `.glb` de l'éditeur → transforms de nœuds décomposés (`LocalTransform.GetDecomposed()`) → texture PNG externe → `SaveGLTF` : **57/57 lus** par `GltfStaticModelReader`, chemin de texture renvoyé. Une texture `.tga` est refusée par SharpGLTF (`Must be a valid image: Png, Jpg`).
- L'ancien importeur (`git show 295db0c6:CasaEngine/Framework/Assets/Loaders/StaticModelImporter.cs`) : post-traitements Assimp `Triangulate | FlipUVs | JoinIdenticalVertices | GenerateSmoothNormals | FlipWindingOrder | GlobalScale` (`:53-58`) ; métadonnées d'effet lues par matériau, appariées **par nom de matériau** (`:193`, parseur `:206-345`) et appliquées en `:345-392` (`EffectFilePath`, `LegacyTechniqueIndex` ← dword `technique`, `AmbientColor`, `DiffuseColor`, `SpecularColor`, `SpecularPower` ← `shininess`, textures diffuse/normal/réflexion résolues relativement au fichier modèle), **avant** le profil legacy. Le convertisseur de l'éditeur applique `Triangulate | LimitBoneWeights | JoinIdenticalVertices | GenerateSmoothNormals` (`AssimpToGltfConverter.cs`, `ConversionPostProcess`).
- Le profil `RacingGameLegacyMaterialImportProfile` utilise le nom du modèle, le nom de la texture diffuse (`Palm`, `Leave`, `Ast`, `plants`) et `RacingGameLegacyMaterialTuning.ShouldEnableReflection` (fichier d'effet `ReflectionSimpleGlass.fx`, `RacingGameLegacyMaterialTuning.cs:85-106`) ; le lecteur applique le profil en `GltfStaticModelReader.cs:505-520` (`Neutral` si `null`, `:60`).
- StbImageSharp 2.30.16 est déjà une dépendance transitive de `CasaEngine` et `CasaEngine.EditorServices` (`obj/project.assets.json`). Aucune collision de nom de base entre les `.tga` et les `.png` de `RacingGame/Content/Textures`.

MGUI (vérifié) : aucune écriture de brush dans RacingGame ; risque non vérifié à l'exécution : brushes statiques partagés non gelés (`RacingGame.Shared/UI/MGUI/MguiUiTheme.cs:30-32`) auxquels chaque `MGBorder`/`MGElement` s'abonne désormais (`MGBorder.cs:60-70`, `MGElement.cs:1724-1730`).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Les 57 `.X` sont convertis en `.gltf` avec leurs textures par un **outil versionné dans `scripts/`** ; aucun code ajouté à CasaEngine (2026-09-27, remplace la voie « import par l'éditeur »). |
| D2 | Tout passe en **DesktopGL 3.8.5.1** : `RacingGame`, `RacingGame.Shared`, `RacingGameCasaEngine` (plus de WindowsDX). |
| D3 | `RaceGameMode` migre vers **`GameplayMode`**, enregistré par `World.SetGameplayMode`. |
| D4 | Pas de branche dédiée : commits sur `remaster`. |
| D5 | Le bump CasaEngine `295db0c6` → `f8629e05` est commité — fait par l'auteur (`3ed325f`). |
| D6 | Le gitlink racine `MGUI` est retiré de l'index (`git rm --cached MGUI`) ; le dossier reste sur disque. |
| D7 | Plan dans `docs/racinggame-casaengine-update-tasks.md`. |
| D8 | `RacingGameCasaEngine` lit les `.gltf` **directement** avec `GltfStaticModelReader` (même forme d'API que l'ancien importeur). |
| D9 | Les métadonnées d'effet des `.X` sont portées dans les **`extras` glTF** des matériaux et relues par `RacingGameCasaEngine` via SharpGLTF. |
| D10 | Les modifications non commitées de l'auteur dans `RacingGame.csproj`, `RacingGame.Shared.csproj` et `RacingGame.PipelineExtension.csproj` sont intégrées au commit de T1.1 (accord explicite de l'auteur, 2026-09-27). |
| D11 | Toutes les versions MonoGame sont **épinglées en 3.8.5.1** (DesktopGL, Content.Builder.Task, Content.Pipeline, outils mgcb), alignées sur CasaEngine. |
| D12 | Exécution en mode **AUTO** après approbation ; l'auteur ne modifie pas le dépôt pendant l'exécution. |
| D13 | `RacingGame.PipelineExtension` revient en **`net8.0`** (TFM de l'outil mgcb 3.8.5.1), avec `MonoGame.Framework.Content.Pipeline 3.8.5.1` ; `Content.mgcb:13` garde `bin\Debug\net8.0`. Remplace, pour ce seul point, la modification TFM de l'auteur intégrée par D10 (décision de l'auteur, 2026-09-27). |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | Exception à « commit compilable » pour `RacingGameCasaEngine` seul : non compilable depuis `3ed325f` (commit de l'auteur), **jusqu'à T4.2** ; pendant cette fenêtre, la validation par commit porte sur `dotnet build RacingGame/RacingGame.csproj` (et sur l'outil en phase 2), jamais sur `RacingGame.slnx` ; en phase 3, validation intermédiaire de `RacingGameCasaEngine` = liste d'erreurs strictement réduite et restreinte aux sites encore à migrer. `dotnet build RacingGame.slnx` redevient l'exigence à partir de T4.2. |
| P2 | Emplacement des fichiers produits : `RacingGameCasaEngine/Content/Models/<Nom>.gltf` + `<Nom>.bin` (nom de base du `.X` conservé, dont `Car`) et `RacingGameCasaEngine/Content/Textures/<Nom>.png` (nom de base de la `.tga` conservé) ; URI d'image `../Textures/<Nom>.png`, miroir de l'ancien `..\\textures\\<Nom>.tga`. Les `.X` et `.tga` restent en place pour le jeu legacy. |
| P3 | `RaceGameMode : GameplayMode` en préservant le comportement : `RaceFlowCoordinatorComponent` continue de piloter compte à rebours et chrono ; `Start()` porte l'ancien `StartMatch` (`StartedAtUtc`) ; `EvaluateResult()` renvoie `Success` quand `IsRaceFinished`, sinon `Running` ; la pause reste celle de `RaceGameMode` (`TogglePause`) ; les appels `EndMatch()` disparaissent (aucun effet observable, cf. état vérifié). |
| P4 | Lectures de `pawn.Controller` → `World.GetPlayerController(entity)` ; lectures de `pawn.InputEnabled` → `PlayerController.IsInputEnable` seul, après vérification de l'écriture contrôleur seule `RaceFlowCoordinatorComponent.cs:107`. |
| P5 | `AssetContentManager.Load` (`RacingGameCasaEngineGame.cs:172,178`) → `Acquire` ou `LoadCopy` selon la sémantique de l'ancien `Load` lue dans `git show 295db0c6:CasaEngine/…/AssetContentManager.cs` (cache partagé → `Acquire`, copie → `LoadCopy`). |
| P6 | Outil `scripts/LegacyModelGltfConverter` : console `net9.0-windows`, `ProjectReference` vers `CasaEngine/CasaEngine.EditorServices` (réutilise `AssimpToGltfConverter`, AssimpNetter et SharpGLTF) ; décodage TGA par StbImageSharp (déjà transitif) ; encodage PNG par `System.Drawing` (framework Windows Desktop, `UseWindowsForms`) ; **aucun nouveau paquet NuGet**. Parseur d'`EffectInstance` porté de l'ancien importeur (`295db0c6`, `StaticModelImporter.cs:206-345`), appariement par nom de matériau comme en `:193`. |
| P7 | Schéma des `extras` : copie brute et complète de l'`EffectInstance` par matériau — `{"legacyEffect": {"file": "<chaîne .X>", "dwords": {…}, "floats": {…}, "strings": {…}}}` — sans interprétation ; `RacingGameCasaEngine` en tire exactement les champs que remplissait l'ancien importeur (`:345-392`). |
| P8 | Pas de copie du modèle de plan dans `ai-agent/` ni d'index `docs/README.md` (D7). |
| P9 | `scripts/TrackPlacementExporter` n'est pas touché : la mise à jour ne le casse pas, et D2 est lu comme portant sur les trois projets du jeu. |

## Règles d'exécution pour l'agent

- Commits sur `remaster` (D4). Jamais de commit sur `main`/`master`. **Aucune modification** dans `CasaEngine/` ni `CasaEngine/MGUI/`.
- **Une seule tâche à la fois.** Avant de commencer : `⏳` → `🚧`. À la fin : validation, puis `✅`, `🧪` ou `⚠️`, courte note sous la tâche, **commit dédié** incluant la mise à jour de ce fichier.
- **Un commit par tâche**, atomique ; compilable sauf exception P1 ; message en anglais `type(area): summary`.
- **Ne jamais pousser**, ni merger.
- **Ne rien inventer** : toute API ou règle vient du dépôt, d'une réponse de l'auteur ou d'une doc officielle citée. Sinon ⚠️ Blocked + question dans « Points ouverts » + **arrêt**.
- **Build obligatoire** avant ✅ dès que du code est touché.
- **Ne jamais indexer** `log.txt`, `.serena/`, ni aucune modification préexistante de l'auteur hors D10 : `git add <chemin>` fichier par fichier. Avant chaque tâche, `git status` : tout changement inattendu → ⚠️ + arrêt.
- **Langue** : plan en français ; code, commits, `docs/` (hors plans) en anglais.
- **Rollback** : jamais de réécriture d'historique (ni `reset` de commits, ni `rebase`, ni `commit --amend`) ; une tâche commitée s'annule par `git revert <sha>`. Jamais de `git checkout`/`git restore` sur un fichier portant des modifications non commitées de l'auteur.
- **Budget** : au plus 3 cycles « build/validation → correction » par tâche, corrections limitées aux fichiers listés par la tâche ; au-delà, ou s'il faut toucher un autre fichier → ⚠️ + arrêt, dans l'état de rollback de la tâche.

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur (exigé à partir de T4.2, cf. P1).
- `RacingGame/obj/project.assets.json`, `RacingGame.Shared/obj/project.assets.json` et `RacingGameCasaEngine/obj/project.assets.json` ne contiennent qu'un seul paquet `MonoGame.Framework.*` : `MonoGame.Framework.DesktopGL/3.8.5.1`.
- `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --smoke-frontend`, puis `-- --verify-legacy-import-profile` et `-- --capture-track-audit` : sans échec.
- Rendu de `RacingGameCasaEngine` : voiture et décor texturés, orientés et à l'échelle comme dans le jeu legacy (comparaison visuelle par l'auteur, 🧪 sinon).
- Smoke manuel du jeu legacy (`dotnet run --project RacingGame/RacingGame.csproj`) : menus MGUI, sélection, course, HUD, effets post-écran, audio XACT (initialisation sans exception ; musique, moteur et effets sonores audibles).
- Passe `verifier` indépendante sur le résultat final.

---

## Phase 0 — Mise en place

### ✅ T0.1 — Plan et nettoyage du gitlink MGUI

> Validation (2026-09-27) : `git diff --cached --stat` → `MGUI` (gitlink supprimé) + ce plan ; `.gitmodules` inchangé ; les trois csproj de l'auteur restent ` M` non indexés ; `MGUI/` reste sur disque (non suivi).

- Objectif : versionner ce plan et retirer le gitlink orphelin (D6, D7).
- Fichiers : `docs/racinggame-casaengine-update-tasks.md` (nouveau), index Git `MGUI`.
- Étapes :
  1. Écrire ce plan dans `docs/`.
  2. `git rm --cached MGUI` (le dossier `MGUI/` reste sur disque et devient non suivi).
- Validation : `git diff --cached --stat` ne montre que le plan et la suppression du gitlink ; `.gitmodules` inchangé ; les trois csproj de l'auteur restent ` M` non indexés.
- Rollback : avant commit, `git restore --staged MGUI docs/racinggame-casaengine-update-tasks.md` puis suppression du fichier de plan créé par la tâche ; après commit, `git revert <sha>`.
- Budget : une tentative (aucun build).
- Commit : `chore(repo): add CasaEngine update plan and drop stale MGUI gitlink`

## Phase 1 — Jeu legacy (`RacingGame`, `RacingGame.Shared`)

### ⏳ T1.1 — Jeu legacy : net9, namespaces MGUI et DesktopGL 3.8.5.1

- Objectif : le jeu legacy compile et tourne contre CasaEngine `f8629e05` / MGUI `d3e0cd12`, en DesktopGL 3.8.5.1 seul (D2, D11), en partant du contenu actuel des fichiers, modifications de l'auteur comprises (D10) sauf le TFM de l'extension (D13). Une seule étape : un état intermédiaire « WindowsDX seul » n'existe pas avec ce MGUI.
- Fichiers (valeurs « avant » = contenu de l'arbre de travail au 2026-09-27) :
  - `RacingGame/RacingGame.csproj` : TFM `net9.0-windows` (auteur, conservé) ; `MonoGame.Framework.WindowsDX 3.8.*` → `MonoGame.Framework.DesktopGL 3.8.5.1` ; `MonoGame.Content.Builder.Task 3.8.*` → `3.8.5.1` ;
  - `RacingGame.Shared/RacingGame.Shared.csproj` : TFM `net9.0-windows` (auteur, conservé) ; `MonoGame.Framework.WindowsDX 3.8.*` → `MonoGame.Framework.DesktopGL 3.8.5.1` ;
  - `RacingGame.PipelineExtension/RacingGame.PipelineExtension.csproj` : TFM `net9.0` (auteur) → `net8.0` (D13) ; `MonoGame.Framework.Content.Pipeline 3.8.*` → `3.8.5.1` ;
  - `RacingGame/Content/Content.mgcb:13` : inchangé (`/reference:..\..\RacingGame.PipelineExtension\bin\Debug\net8.0\RacingGame.PipelineExtension.dll`, cohérent avec D13) ;
  - `RacingGame.Shared/UI/MGUI/MguiUiTheme.cs`, `UI/MGUI/Views/CarSelectionView.cs`, `MainMenuView.cs`, `SplashScreenView.cs`, `TrackSelectionView.cs` : `using MGUI.Core.UI.Brushes.Border_Brushes/Fill_Brushes` → `BorderBrushes/FillBrushes` ;
  - `RacingGame/.config/dotnet-tools.json` (5 outils mgcb) et `RacingGame.Shared/.config/dotnet-tools.json` (4 outils `dotnet-mgcb-editor*`, suivi par Git) : versions 3.8.4 → 3.8.5.1 (mêmes versions que `CasaEngine/.config/dotnet-tools.json`), liste d'outils inchangée ;
  - `RacingGame/Content/Content.mgcb`, `RacingGame/Content/Shaders/Shaders.mgcb` et `RacingGame.Shared/Content/Content.mgcb` (vide, non construit) : `/platform:Windows` → `/platform:DesktopGL`.
- Étapes :
  0. Sauvegarde des modifications de l'auteur avant toute édition : copie des trois csproj (`RacingGame/RacingGame.csproj`, `RacingGame.Shared/RacingGame.Shared.csproj`, `RacingGame.PipelineExtension/RacingGame.PipelineExtension.csproj`) et `git diff` de ces trois fichiers dans le scratchpad de la session (`t1.1-author-backup/`).
  1. Appliquer les changements ci-dessus.
  2. Garde avant validation : `rg -n --hidden "net8\.0-windows|WindowsDX|/platform:Windows|3\.8\.\*|3\.8\.4" RacingGame RacingGame.Shared RacingGame.PipelineExtension --glob "!**/bin/**" --glob "!**/obj/**"` ne renvoie plus rien ; `<TargetFramework>` de `RacingGame.PipelineExtension.csproj` = `net8.0` = dossier TFM de `Content.mgcb:13`. Tout autre résultat → ⚠️ + question.
  3. `git add` fichier par fichier des fichiers listés (dont les trois csproj de l'auteur, D10) et du plan.
- Validation :
  - `dotnet build RacingGame.PipelineExtension/RacingGame.PipelineExtension.csproj` puis `dotnet build RacingGame/RacingGame.csproj` : 0 erreur ; le build de contenu charge l'extension `net8.0` reconstruite (3.8.5.1) et traite les entrées `RacingGameModelProcessor` ; contenu produit sous `RacingGame/Content/bin/DesktopGL` ; tout échec de chargement de l'extension par mgcb → ⚠️ + question, sans autre changement de TFM ;
  - `RacingGame/obj/project.assets.json` et `RacingGame.Shared/obj/project.assets.json` ne contiennent que `MonoGame.Framework.DesktopGL/3.8.5.1` comme paquet `MonoGame.Framework.*` ;
  - lancement (`dotnet run --project RacingGame/RacingGame.csproj`) : menus MGUI, sélection, course, HUD, shaders et effets post-écran ; audio XACT initialisé sans exception, musique, moteur et effets sonores audibles ;
  - 🧪 si une vérification visuelle ou sonore reste à faire par l'auteur ; toute différence visuelle, exception ou absence de son → ⚠️ + question.
- Rollback : avant commit, `git restore --staged` des fichiers indexés, `git restore` des seuls fichiers **sans** modification préalable de l'auteur (`.mgcb`, `dotnet-tools.json`, les 5 fichiers `.cs`), puis recopie des trois csproj depuis `t1.1-author-backup/` : état d'avant la tâche, modifications de l'auteur intactes et non indexées. Après commit, `git revert <sha>`, puis réapplication du `git diff` sauvegardé pour remettre les modifications de l'auteur, non commitées, dans l'arbre de travail.
- Budget : 3 cycles build → correction (règle d'exécution) ; toute correction hors des fichiers listés → ⚠️.
- Commit : `build(racing-legacy): move legacy game to net9 and MonoGame DesktopGL 3.8.5.1`

## Phase 2 — Conversion des modèles `.X` en `.gltf` (outil `scripts/`)

### ⏳ T2.1 — Outil `scripts/LegacyModelGltfConverter`

- Objectif : outil versionné (D1, P6) qui convertit un dossier de `.X` en `.gltf` + `.bin` + PNG, avec textures associées et `extras` (P7).
- Fichiers : `scripts/LegacyModelGltfConverter/LegacyModelGltfConverter.csproj`, `Program.cs` et fichiers de l'outil (nouveaux).
- Étapes, par modèle :
  1. Parser les matériaux et l'`EffectInstance` du `.X` (port du parseur `295db0c6` `StaticModelImporter.cs:206-345`).
  2. Convertir la géométrie avec `AssimpToGltfConverter.Convert` vers un `.glb` temporaire, le charger avec SharpGLTF.
  3. Décomposer le `LocalTransform` de chaque nœud.
  4. Apparier chaque matériau glTF à son `EffectInstance` par nom (comme `:193`) ; attacher en PNG externe la texture `diffuseTexture` (canal `BaseColor`) et `normalTexture` (canal `Normal`) ; écrire l'`EffectInstance` brute dans les `extras` (P7).
  5. Convertir chaque `.tga` utilisée en PNG une seule fois (nom de base conservé) ; écrire `.gltf`/`.bin` et PNG aux emplacements P2.
  6. Écrire un rapport par modèle et échouer (code de sortie ≠ 0) sur tout matériau non apparié, texture introuvable ou transform non décomposable.
- Validation :
  - `dotnet build scripts/LegacyModelGltfConverter` : 0 erreur, sans nouveau paquet NuGet (`obj/project.assets.json` : seules des dépendances déjà présentes via `CasaEngine.EditorServices`) ;
  - exécution sur `RacingGame/Content/Models` vers un dossier temporaire : code 0 ; 57/57 lus par `GltfStaticModelReader.ReadWithMetadata` ; chaque matériau dont l'`EffectInstance` a `diffuseTexture`/`normalTexture` a un `DiffuseTextureFilePath`/`NormalTextureFilePath` non nul ; `extras` présents sur chaque matériau glTF apparié à un matériau `.X` porteur d'une `EffectInstance` (82 occurrences dans les 57 `.X`) ; matrice recomposée de chaque nœud = matrice d'origine (écart ≤ 1e-4) ;
  - parité géométrique avec l'ancien importeur (O1) sur `Car.x`, `Building.X`, `AlphaPalm.X` : nombre de sommets, boîte englobante, plage des UV, ordre des sommets et transforms de nœuds comparés à la sortie de l'ancien `StaticModelImporter.ImportWithMetadata`, exécuté depuis une copie `git archive` de CasaEngine `295db0c6` (et de MGUI `6be7a719`) dans le scratchpad, sans toucher au dépôt ; si cette référence ne peut pas être construite, ou en cas d'écart → ⚠️ + question.
- Commit : `feat(tools): add legacy X to glTF model converter`

### ⏳ T2.2 — Génération des modèles `.gltf`

- Objectif : produire et versionner les 57 modèles convertis.
- Fichiers : `RacingGameCasaEngine/Content/Models/*.gltf`, `*.bin`, `RacingGameCasaEngine/Content/Textures/*.png` (nouveaux, P2).
- Étapes : lancer l'outil de T2.1 vers les emplacements P2 ; `git add` des fichiers produits uniquement.
- Validation : mêmes contrôles que T2.1 sur les fichiers versionnés ; `git status` : aucun autre fichier modifié.
- Commit : `feat(racing-casa): add glTF conversions of legacy models`

## Phase 3 — `RacingGameCasaEngine` : API CasaEngine et backend

Exception de compilation P1 (ouverte par `3ed325f`) jusqu'à T4.2.

### ⏳ T3.1 — Backend DesktopGL et namespaces

- Fichiers : `RacingGameCasaEngine/RacingGameCasaEngine.csproj` (`MonoGame.Framework.WindowsDX 3.8.2.1105` → `MonoGame.Framework.DesktopGL 3.8.5.1`, `FontStashSharp.MonoGame` 1.5.4 → 1.5.7 comme `CasaEngine/Directory.Packages.props`, retrait du `NoWarn`/`WarningsNotAsErrors`/`RestoreNoWarn` NU1605) ; `GlobalUsings.cs` (retrait du `using …DebugTools`) ; `UI/RaceUiTheme.cs`, `UI/LegacyMenuUiTheme.cs`, `Screens/TrackSelectionScreen.cs`, `Screens/RaceHudScreen.cs`, `Screens/CarSelectionScreen.cs`, `Screens/PauseScreen.cs` (`using` des brushes).
- Validation : restore sans `NU1605` ; `obj/project.assets.json` : seul `MonoGame.Framework.DesktopGL/3.8.5.1` ; erreurs restantes ⊂ sites de T3.2-T3.4 et `StaticModelImporter`.
- Commit : `build(racing-casa): switch to DesktopGL and updated MGUI namespaces`

### ⏳ T3.2 — Entité, possession et gate d'input

- Fichiers : `Entities/RacingCarPawn.cs:9` (`: Entity`) ; `Bootstrap/RuntimeRaceWorldBinder.cs:73-75` (`Possess`) ; lectures `Controller`/`InputEnabled` : `Components/ArcadeCarMovementComponent.cs:78-79,574`, `Components/ArcadeVehicleDynamicsSolver.cs:106,202,406,409`, `Components/VehicleDynamicsComponent.cs:97-98`, `Components/RaceFlowCoordinatorComponent.cs:51`, `Bootstrap/RaceRuntimeUiCoordinator.cs:133`.
- Étapes : appliquer P4.
- Validation : ces erreurs disparaissent ; aucune nouvelle erreur.
- Commit : `fix(racing-casa): use entity possession and controller input gate`

### ⏳ T3.3 — `RaceGameMode` sur `GameplayMode`

- Fichiers : `GameFramework/RaceGameMode.cs` ; `Bootstrap/RuntimeRaceWorldBinder.cs:16-18,49-50,80`.
- Étapes : appliquer D3 et P3 ; remplacer la réflexion sur `World.GameMode` par `World.SetGameplayMode`.
- Validation : ces erreurs disparaissent ; aucune nouvelle erreur.
- Commit : `refactor(racing-casa): host race rules in a GameplayMode`

### ⏳ T3.4 — Transforms locaux et chargement d'assets

- Fichiers : `Bootstrap/RuntimeRaceWorldBinder.cs:55` (`CopyLocalTransformFrom`), `Worlds/LegacyTrackSceneFactory.cs:429-431` (`LocalTransform`), `Bootstrap/RacingGameCasaEngineGame.cs:172,178` (P5).
- Validation : erreurs restantes = `StaticModelImporter` seulement.
- Commit : `fix(racing-casa): use LocalTransform and asset handles`

## Phase 4 — `RacingGameCasaEngine` : chargement des `.gltf`

### ⏳ T4.1 — Métadonnées legacy depuis les `extras`

- Objectif : reconstruire à partir des `extras` (P7) les champs que remplissait l'ancien importeur, puis appliquer le profil RacingGame (D9).
- Fichiers : nouveau fichier dans `RacingGameCasaEngine/Bootstrap/` (lecteur d'`extras` + application du profil).
- Étapes :
  1. Lire les `extras` des matériaux du `.gltf` avec SharpGLTF (`SharpGLTF.Core`, transitif via CasaEngine), par index de matériau (`StaticModelImportedMaterial.MaterialIndex`).
  2. Remplir `EffectFilePath`, `LegacyTechniqueIndex`, `AmbientColor`, `DiffuseColor`, `SpecularColor`, `SpecularPower`, `ReflectionTextureFilePath` comme l'ancien importeur (`295db0c6` `StaticModelImporter.cs:345-392`, chemins résolus relativement au fichier modèle).
  3. Appliquer ensuite `RacingGameImportProfiles.LegacyMaterialProfile` avec les mêmes affectations que `GltfStaticModelReader.cs:510-519` (le lecteur est appelé avec le profil `null` → `Neutral`, pour que le profil RacingGame voie les métadonnées d'effet, comme avant).
- Validation : `RacingGameCasaEngine` : aucune nouvelle erreur de compilation.
- Commit : `feat(racing-casa): restore legacy material metadata from glTF extras`

### ⏳ T4.2 — Remplacement de `StaticModelImporter`

- Fichiers : `Components/LegacyCarVisualFactory.cs:30,59,72,84` (`Car.gltf`), `Worlds/LegacyTrackSceneFactory.cs:56,211,219` (`{modelName}.gltf`), `Bootstrap/LegacyImportProfileVerifier.cs` ; `RacingGameCasaEngine.csproj` : retrait du lien `..\RacingGame\Content\Models\*.X`, ajout de `Content\Models\*.gltf`, `Content\Models\*.bin`, `Content\Textures\*.png` en `CopyToOutputDirectory` (liens `Textures\*.*`, `*.CombiModel`, `*.Track` conservés).
- Étapes : `GltfStaticModelReader` (`IsFileSupported`, `ReadWithMetadata`) + T4.1 à la place de `StaticModelImporter`.
- Validation : `dotnet build RacingGame.slnx` : **0 erreur** (fin de l'exception P1) ; `dotnet run … -- --verify-legacy-import-profile` sans échec ; `-- --smoke-frontend` sans échec.
- Commit : `fix(racing-casa): load converted glTF models`

### ⏳ T4.3 — Documentation

- Fichiers : `docs/legacy-import-profile.md`, `docs/racinggame-casaengine-material-gap-analysis.md` (références à `StaticModelImporter`), `README.md` (section « Legacy Import Profile »), `scripts/LegacyModelGltfConverter` (usage dans le README de l'outil ou `docs/`).
- Validation : `rg -n "StaticModelImporter" docs README.md` : plus de référence au code supprimé (hors historique explicite).
- Commit : `docs(racing-casa): document glTF model conversion`

## Phase 5 — Validation finale

### ⏳ T5.1 — Validation globale et rapport

- Étapes : « Validation globale » complète ; vérification à l'exécution du risque MGUI des brushes partagés (R4) ; passe `verifier` indépendante ; rapport de fin.
- Commit : `docs(racing): close CasaEngine update plan`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Parité géométrique : l'ancien importeur appliquait `FlipUVs`, `FlipWindingOrder`, `GlobalScale` ; le convertisseur de l'éditeur non. UV, ordre des sommets et échelle des `.gltf` peuvent différer de l'ancien import sur lequel `RacingGameCasaEngine` a été réglé. Mesuré en T2.1 ; tout écart → question à l'auteur. | T2.1 |
| O2 | Rendu correct des 10 shaders legacy sous OpenGL (branche `OPENGL` présente, non vérifié à l'exécution). | T1.1 |
| O3 | Lecture des banques XACT précompilées (`.xgs`/`.xwb`/`.xsb`) sous DesktopGL, non vérifiée. | T1.1 |
| O4 | Encodage PNG par `System.Drawing` dans l'outil : à confirmer au build de T2.1 ; sinon ⚠️ + question (pas de nouveau paquet sans accord). | T2.1 |

## Risques

- R1 : `RacingGameCasaEngine` (et donc `RacingGame.slnx`) reste non compilable de `3ed325f` à T4.2 (P1).
- R2 : parité visuelle des modèles convertis (O1).
- R3 : différences visuelles ou audio du jeu legacy en DesktopGL (O2, O3).
- R4 : brushes MGUI statiques partagés et abonnements (`MguiUiTheme.cs:30-32`) : rétention possible, non vérifiée ; option documentée : `Freeze()` (`UIFreezableBrush.cs:25`), à décider avec l'auteur si le problème est constaté.

## Hors périmètre

- Toute modification de CasaEngine ou de MGUI.
- Push, merge, PR.
- Refactor, retuning ou nouvelles features hors migration.
- `scripts/TrackPlacementExporter` (P9).
- Suppression du dossier `MGUI/` sur disque (laissée à l'auteur).
