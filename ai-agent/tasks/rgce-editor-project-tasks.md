# Plan agent IA — Projet éditeur complet de RacingGameCasaEngine : tous les assets et les mondes chargés et édités dans l'éditeur CasaEngine

Plan d'exécution de la demande de l'auteur du 2026-10-08 : « il faut créer complètement le projet pour que l'éditeur puisse l'ouvrir et le configurer. On garde RacingGameCasaEngine pour pouvoir tester le jeu. Le but principal est de vérifier que tout est prêt côté éditeur pour pouvoir charger et éditer tous les assets de ce jeu. »

Les décisions D1 → D8 ont été arbitrées avec l'auteur le 2026-10-08, en deux séries de questions : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

À la fin du chantier :
- **Projet unique.** `RacingGameCasaEngine/Content` est un projet CasaEngine complet, que l'éditeur ouvre et configure. Le jeu et l'éditeur lisent les mêmes assets ; l'exe RacingGameCasaEngine reste le moyen de tester le jeu.
- **Code du jeu dans une DLL.** Il vit dans `RacingGame.Gameplay`, la DLL de gameplay chargée par l'éditeur, avec les view models des écrans.
- **Mondes en assets.** Le menu, le carrousel de sélection et les trois courses sont des `.world`, et la voiture un `.entity`. Le jeu les charge et l'éditeur les ouvre, les affiche, les modifie et les enregistre sans perte.
- **Route, rails, colonnes et terrain** sont reconstruits par des composants générateurs à partir de données cataloguées. Le décor est figé en entités éditables.
- **Contenu hérité porté.** Tout ce qui reste de `RacingGame/Content` est porté dans `Content` et catalogué : textures, cubemaps, données, CombiModel, sons et musiques. Le csproj ne lie plus rien depuis l'ancien jeu.
- **Assets ouverts dans l'éditeur.** Chaque asset catalogué s'ouvre au niveau défini en P1, ce qu'une vérification automatique prouve.
- **Rendu et jeu inchangés.** Ils restent ceux d'aujourd'hui, mesurés contre des références prises avant le chantier.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-08)

Découverte en lecture seule, en deux workflows. Le premier, de 4 agents, portait sur le projet, le runtime, l'éditeur et la documentation ; le second, de 4 agents, sur le moteur, l'éditeur, le code RGCE et le contenu hérité. Chaque rapport a été contre-vérifié par un agent adverse, et les faits porteurs ont été relus directement. Les rapports sont dans le scratchpad de la session (`editor-disc/`, `editor-disc2/`).

- **Dépôts** :
  - RacingGame : branche `remaster`, HEAD `d8ac303` (« Update CasaEngine », pointeur `2bcb185`). `.serena/` et `log.txt`, non suivis, sont à l'auteur.
  - **Le sous-module est passé de `2bcb185` à `2435f8ab9`** (`main` de CasaEngine, fusion du chantier `chantier/fss-line-height`) pendant la découverte, par l'auteur. `git status` montre ` M CasaEngine`, non indexé : voir P3.
- **Projet éditeur actuel** (ADR-0001, ADR-0002) :
  - périmètre : les 9 écrans XAML seulement. La racine est `RacingGameCasaEngine/Content`.
  - `RacingGameCasaEngine.json` déclare `GameplayDllName = "RacingGameCasaEngine.UI.dll"` et `FirstWorldLoaded = "Worlds/Editor.world"`, sans `GameplayCsprojName`.
  - `AssetInfos.json` compte 94 entrées : 50 sprite, 10 png, 9 uiscreen, 8 texture, 6 wav, 6 sound, 3 track, 1 fnt, 1 world. 92 chemins utilisent `/`, les 2 de la police utilisent `\` (forme imposée par `BitmapFontDescriptor.CatalogFileNameNextTo`).
  - `Worlds/Editor.world` ne contient qu'une entité vide `EditorAutomationAnchor`.
- **Contenu de `Content` non catalogué** : 56 glTF et 56 `.bin` (`Models/`), 106 PNG (`Textures/`), dont 100 convertis à l'identique depuis les TGA nommés par les `.X` et 6 sources d'images d'interface.
- **Contenu lié depuis l'ancien jeu** par le csproj (`RacingGameCasaEngine.csproj:29-68`) :
  - `..\RacingGame\Content\Textures\*.*` (129 TGA, 2 DDS, 5 PNG) ;
  - `LandscapeHeights.data` ;
  - les 10 `*.CombiModel`.
  Ces fichiers n'existent que dans le dossier de sortie.
- **Contenu non porté** : 22 WAV (19 effets et bruits de moteur, 3 musiques en 22 050 Hz stéréo, 62 Mio), toutes en PCM 16 bits. Le tableau de correspondance XACT est dans `editor-disc2/cue_table.md` ; volume `.sound` = `10^(dB/2000)`, comme pour les 6 sons déjà portés.
- **Le jeu** :
  - Il construit ses trois mondes en code :
    - le front-end : caméra et `PlayerStart` ;
    - le carrousel, monde à part rendu dans une cible (`UI/CarSelectionCarousel.cs`) ;
    - la course, construite par `RaceWorldFactory` et `LegacyTrackSceneFactory*` : caméra, 3 lumières, ombres, terrain, route, rails, poteaux, colonnes, décor, 3 checkpoints, `PlayerStart`, `PlayerCar`.
  - Il charge modèles, textures, `.Track`, CombiModel et carte de hauteurs par chemin littéral. Il ne lit pas `RacingGameCasaEngine.json` (`Program.cs:37-45` fixe `ProjectPath` et charge le catalogue lui-même).
- **Code de jeu invisible pour l'éditeur** :
  - 12 classes vivent dans l'exe (9 `EntityComponent`, 2 `SceneComponent`, `RacingCarPawn : Entity`), avec `RaceGameMode` et `RacingPlayerController`.
  - `RacingGameCasaEngine.UI` ne contient que des view models et `LegacyScreenLayout`, sans `IPlugin`.
  - Quatre composants lisent l'instance du jeu par `World.Game as RacingGameCasaEngineGame`.
  - `RacingCarPawn` construit ses enfants dans son constructeur ; `RaceTrackPhysicsComponent` n'a pas de constructeur sans paramètre.
- **Moteur et éditeur, faits qui contraignent le plan** :
  - pas de sauvegarde des champs d'un composant de jeu : `EditorEntityJsonSerializer.SaveComponent` est un `switch` fermé et le défaut n'écrit que `id`, `name`, `type` ;
  - un type inconnu dans un `.world` fait planter l'ouverture dans l'éditeur ;
  - `ElementFactory` résout les types par nom court, premier trouvé ;
  - `IPlugin.Initialize()` ne reçoit rien et peut s'exécuter plusieurs fois ;
  - le format `.staticModel` porte ses sommets en ligne, sans tangentes. Les modèles actuels du jeu n'en ont pas non plus, car `GltfStaticModelReader` n'en construit pas ; seuls le terrain et les rails, générés, en ont ;
  - `lit-diffuse` n'expose pas les facteurs de réflexion utilisés par la voiture et le verre ;
  - les `.dds` ne sont acceptés que comme cubemaps carrées sans mips, ce que sont les deux DDS hérités ;
  - en mode édition, les composants passent par `InitializeWithWorld` et `Update` ; `BeginPlay`, les scripts et la physique sont coupés ;
  - l'éditeur n'a pas de document pour `.staticModel`, `.texture`, images, `.fnt`, `.Track`, `.gltf` ;
  - `--diagnostics-out` ne contient pas les `Logs` du moteur et le code de sortie vaut 0 même en échec.
- **Éditeur compilé** : `CasaEngine.Editor.exe` date du 2026-10-06 20:39, avant `2bcb185` et `2435f8ab9`. Il faut le recompiler (`dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln`).
- **Moyens de validation existants** :
  - `--smoke-frontend` ;
  - `--capture-ui-screens` et `scripts/UiCaptureCompare` ;
  - `--capture-track-audit` (21 captures) ;
  - `--export-track-runtime-scene` (vidage JSON des entités de course) ;
  - `--capture-car-profile-audit`, `--capture-car-top-speed-audit` ;
  - `--verify-legacy-import-profile` ;
  - `scripts/capture_editor_screen.ps1` côté éditeur.
  Il n'existe pas de projet de tests RGCE.
- **Git** : pas de `.gitattributes`, `core.autocrlf=true`, pas de LFS. Le dépôt contient déjà `Wave Bank.xwb` (70 Mo).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | `RacingGameCasaEngine/Content` reste le projet unique : éditeur et jeu partagent les mêmes assets. Le code du jeu passe dans une DLL de gameplay (`IPlugin`, `GameplayCsprojName`, comme `Projects/RPGDemo` et `Projects/CasaEngine.RPGDemo`). L'exe RacingGameCasaEngine s'en sert pour lancer le jeu et reste le moyen de le tester. |
| D2 | Les mondes deviennent éditables : le menu et les courses (piste, décor, lumières, voiture, caméra, ciel) deviennent des `.world` et `.entity` chargés par le jeu. La route, le terrain et les rails reçoivent une forme d'asset. |
| D3 | Les manques du moteur et de l'éditeur se corrigent dans CasaEngine, sur une branche dédiée. |
| D4 | Tout le contenu hérité est porté dans `Content` et catalogué : les 29 TGA en PNG, les 2 DDS, `LandscapeHeights.data`, les 10 CombiModel, et aussi les 22 WAV et musiques non portés, catalogués en `.sound` même s'ils ne sont pas encore joués. |
| D5 | Route, rails, colonnes et terrain sont des **composants générateurs** : ils reconstruisent la géométrie à partir des assets de données (`.Track`, carte de hauteurs, matériaux), y compris dans l'éditeur ; on les règle dans l'inspecteur. |
| D6 | Le décor aujourd'hui placé par tirage ou lu dans les `.Track` (palmiers, panneaux, bannières, lampadaires, objets et CombiModel) est **figé en entités** du monde par un export unique ; les poteaux de rail et les segments de colonnes restent générés par la route. |
| D7 | `RacingGameCasaEngine.UI` est **renommé `RacingGame.Gameplay`** et devient la DLL de gameplay. |
| D8 | Les nouveaux binaires vont dans git normalement, avec un `.gitattributes` qui marque png, wav, dds, data et tga comme binaires. |

## Points à valider avec ce plan

Arbitrages proposés par l'agent : l'approbation du plan les valide.

| Réf | Proposition |
|---|---|
| P1 | **Niveau d'ouverture exigé par type d'asset.** C'est la définition de « charger/éditer tous les assets » :<br>• **Ouvrir, éditer, enregistrer sans perte** : `.world`, `.entity`, `.staticModel`, `.material`, `.texture`, `.sound`, `.uiscreen`, `.sprite`.<br>• **Ouvrir en aperçu, sans édition du contenu** : images (`.png`, `.dds`), `.wav`, `.fnt`.<br>• **Ouvrir en aperçu en lecture seule** : données de jeu (`.Track`, carte de hauteurs, `.CombiModel`), avec type, taille et assets qui y renvoient. On les modifie par les réglages des composants générateurs, sans éditeur de tracé dans ce chantier. |
| P2 | **Branches.** RacingGame reste sur `remaster`, comme les chantiers précédents. CasaEngine prend `chantier/rgce-editor-project`, créée depuis `main` (`2435f8ab9`). |
| P3 | **Pointeur du sous-module.** Le passage de `2bcb185` à `2435f8ab9`, fait par l'auteur et non indexé, est enregistré par T0.1 dans un commit à part, `chore(casaengine): …`, avec l'accord donné par l'approbation de ce plan. Sinon l'auteur le committe lui-même avant T0.1. |
| P4 | **L'exe garde son amorçage.** Il fixe `ProjectPath`, charge le catalogue et ne passe pas le fichier projet au moteur. Il référence `RacingGame.Gameplay` comme projet : la DLL n'est donc chargée qu'une fois, sans le double chargement possible de `Assembly.LoadFile`. `GameplayDllName` et `GameplayCsprojName` ne servent qu'à l'éditeur. |
| P5 | **Accès au jeu depuis les composants.** Ils passent par une interface `IRaceGameHost`, définie dans `RacingGame.Gameplay` et implémentée par `RacingGameCasaEngineGame` : `World.Game as IRaceGameHost`. Dans l'éditeur, elle vaut `null`, et les composants ne font rien, comme aujourd'hui avec le transtypage. Pas d'état global ajouté. |
| P6 | **Voiture.** `RacingCarPawn` (sous-classe d'`Entity`, non chargeable depuis un fichier) devient une entité ordinaire avec composants, asset `Entities/PlayerCar.entity`. Son état de course passe dans un composant. Le lieur de course la crée par `SpawnEntity` puis la règle. |
| P7 | **Sauvegarde des composants de jeu (moteur).** Elle passe par un attribut sur les propriétés sérialisées : le runtime les lit avec une aide générique, et l'éditeur les écrit avec un écrivain générique. La sauvegarde reste du côté de l'outillage, comme le demande l'AGENTS.md de CasaEngine (§9.9). La conception exacte, les tests et l'ADR sont dans le plan moteur de S2. |
| P8 | **Modèles.** Un outil du dépôt les cuit en `.staticModel`, `.material` et `.texture` standard, aux identifiants déterministes (uuid5, comme les générateurs existants) :<br>• le profil RacingGame et les réglages de matériaux sont appliqués dans les valeurs des `.material` ;<br>• la rotation de π/2 autour de X appliquée au décor est cuite dans les modèles de décor seulement ;<br>• les tangentes ne sont pas perdues, puisque les modèles actuels n'en ont pas. |
| P9 | **Références de parité.** Elles sont prises en S0, avant toute modification. S4 étend l'export de scène à un enregistrement par maillage dessiné : nom, matrice, matériau, nombre et empreinte des sommets. |
| P10 | **Musiques et sons non joués.** Les musiques sont en `is_streaming: true` sur le bus `Music`, que le moteur sait lire en WAV ; les effets sont sur `Sfx`. Les boucles des vitesses (`Gear1`…`Gear5`) sont `is_looped: true`. Rien n'est joué par le jeu dans ce chantier. |

## Règles d'exécution pour l'agent

- **Branches** : RacingGame sur `remaster` ; CasaEngine sur `chantier/rgce-editor-project` (P2). Jamais de commit sur `master` ni sur `main`.
- **Une tranche à la fois, une tâche à la fois.** Chaque tranche (S0 → S7) est présentée détaillée et approuvée avant son exécution. Ce plan ne détaille aujourd'hui que S0 et S1 ; chaque tranche suivante est détaillée ici, relue par un vérificateur de plan, puis soumise à l'auteur. Le plan moteur (S2) vit dans `CasaEngine/ai-agent/tasks/` selon les règles de ce dépôt.
- **Statuts.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par la ligne `Co-Authored-By` de la session. Les commandes de commit commencent par `cd D:/development/repo/RacingGame &&` (ou `…/CasaEngine &&`).
- **Pointeur du moteur.** Chaque commit RacingGame se compile depuis son propre arbre après `git submodule update`.
  - Une tâche RacingGame qui a besoin de nouveaux commits de `chantier/rgce-editor-project` enregistre le pointeur `CasaEngine` sur ce commit dans un commit `chore(casaengine): …` qui la précède immédiatement.
  - La dernière tâche de S2 enregistre le pointeur sur la fin de S2.
  - Les commits de la branche moteur ne sont jamais réécrits (ni rebase ni amend après enregistrement d'un pointeur), pour que chaque pointeur enregistré reste résoluble.
- **Ne jamais pousser ni fusionner.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée. Sinon : ⚠️ Blocked, question dans « Points ouverts », **arrêt**.
- **Build obligatoire** avant ✅ dès que du code est touché :
  - `dotnet build RacingGame.slnx` ;
  - `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln` dès que l'éditeur sert à la validation ou que le moteur change ;
  - `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj` pour toute tâche moteur.
- **Lancer le jeu** uniquement par le script de la session qui sauvegarde et restaure `display-settings.json` et `front-end-options.json` (empreinte SHA-1 vérifiée). Sondes dans le scratchpad seulement ; pas de clavier ni de souris simulés.
- **Au plus 3 cycles de correction par tâche** ; au-delà, ⚠️ Blocked.
- **Ne jamais indexer** les fichiers de l'auteur : `git add` fichier par fichier ; jamais `.serena/` ni `log.txt`.
- **ADR** : aucune ADR n'est réécrite ; une décision qui en remplace une autre est une nouvelle ADR, et l'ancienne change de statut.
- **Langue** : ce plan en français ; code, commits, `docs/` et ADR en anglais.
- **Vérification** : un vérificateur de plan relit chaque tranche avant approbation ; un `verifier` indépendant contrôle chaque tranche terminée avant qu'elle soit déclarée faite.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale (fin de chantier)

- **A1, éditeur.** L'éditeur, recompilé au commit CasaEngine du chantier, ouvre `RacingGameCasaEngine/Content/RacingGameCasaEngine.json` et charge `RacingGame.Gameplay.dll` et son `Plugin`. Les composants de course sont proposés dans « Add Component ».
- **A2, assets.** Un script parcourt `AssetInfos.json` et ouvre chaque asset dans l'éditeur par l'automatisation (un processus par asset, délai borné, diagnostics). Chacun s'ouvre au niveau P1, sans erreur dans le journal : 100 %.
- **A3, mondes.** Les 5 mondes et `PlayerCar.entity` s'ouvrent, s'affichent (capture) et passent l'aller-retour d'enregistrement :
  - un deuxième enregistrement donne exactement les mêmes octets que le premier ;
  - aucun champ de composant n'est perdu par rapport à l'asset source (comparaison JSON).
- **A4, jeu.** Contre les références de S0 :
  - `--smoke-frontend` sort avec le code 0 ;
  - captures d'interface dans le seuil d'`UiCaptureCompare` ;
  - `--capture-track-audit` dans le seuil, ou écart expliqué ;
  - export de scène par maillage identique : mêmes noms, matrices à 1e-4 près, mêmes matériaux ;
  - audits des voitures identiques.
- **A5, dépôt.**
  - Le csproj ne lie plus rien depuis `RacingGame/Content`.
  - Tout fichier de `Content` est catalogué, sauf ceux du projet : le fichier projet, le catalogue, la DLL et le PDB du gameplay, `.casaeditor/`, les `.design.json` et les `.xaml` référencés par les `.uiscreen`.
- **A6, vérification.** Le vérificateur indépendant de clôture rend CONFIRMED.

---

## Enveloppe du programme : tranches

| Tranche | Résultat | Prérequis | Dépôt |
|---|---|---|---|
| **S0** | Références de parité et état de l'éditeur avant chantier, plan committé | — | RacingGame |
| **S1** | `RacingGame.Gameplay` : renommage, `Plugin`, recompilation depuis l'éditeur | S0 | RacingGame |
| **S2** | Fondations moteur et éditeur (plan moteur à part) | S0 | CasaEngine |
| **S3** | Contenu hérité porté et catalogué, plus aucun lien vers l'ancien jeu | S0 | RacingGame |
| **S4** | Composants de jeu dans la DLL, voiture en entité, composants générateurs | S1, S2 | RacingGame |
| **S5** | Modèles, matériaux et textures en assets de l'éditeur, chargés par le catalogue | S2, S3, S4 | RacingGame |
| **S6** | Mondes en assets (`.world`, `PlayerCar.entity`) chargés par le jeu | S4, S5 | RacingGame |
| **S7** | Contrôle de l'éditeur sur tous les assets, documentation, clôture | toutes | les deux |

**Retour arrière** :
- Chaque tranche est une suite de commits sur sa branche, qu'on peut annuler par `git revert`, tranche par tranche.
- Annuler une tranche RacingGame annule aussi ses commits `chore(casaengine)`. Le pointeur revient alors au commit moteur de la tranche précédente, toujours présent sur `chantier/rgce-editor-project`, et `git submodule update` redonne une paire moteur et jeu cohérente.
- Annuler S2 demande d'abord d'annuler S4 à S6, qui en dépendent.
- La branche moteur n'est pas fusionnée sans l'auteur.

**Arrêts** :
- une tâche ⚠️ Blocked ;
- 3 cycles de correction ;
- un constat P0 ou P1 d'un vérificateur ;
- un écart de parité inexpliqué ;
- une décision de l'auteur qui manque.
Un arrêt fige la tranche et celles qui en dépendent.

### S2 — Fondations moteur et éditeur (CasaEngine, détaillée dans le plan moteur)

- **Résultat** : les manques qui bloquent D2, D4 et P1 sont corrigés, testés et documentés.
- **Périmètre** :
  - **E1, sauvegarde des composants de jeu** : un attribut sur les propriétés sérialisées, lues par le runtime et écrites par l'éditeur (P7).
  - **E2, type inconnu** : à l'ouverture d'un monde, une erreur avec contexte remplace le plantage.
  - **E3, documents d'aperçu** :
    - `.staticModel` : aperçu du modèle et liste des emplacements de matériaux ;
    - `.texture` : aperçu de l'image et échantillonneur modifiable ;
    - images `.png` et `.dds` ;
    - `.fnt` ;
    - données brutes, en lecture seule (P1).
  - **E4, `lit-diffuse`** : les facteurs de réflexion (`ReflectionAddAmount`, `ReflectionMultiplyBase`, `ReflectionMultiplyFactor`, `UseSceneReflectionCube`) deviennent des propriétés du `.material`.
  - **E5, automatisation** :
    - les erreurs et avertissements des `Logs` vont dans `--diagnostics-out` ;
    - le code de sortie n'est plus 0 quand un asset ne s'ouvre pas ;
    - un délai borne l'attente.
- **Hors S2** :
  - éditeur de tracé ;
  - sélecteur d'asset typé ;
  - commande « cataloguer un fichier existant » ;
  - Play du jeu complet dans l'éditeur.
- **Acceptation** :
  - `dotnet build` des deux solutions et `dotnet test` verts ;
  - tests d'aller-retour pour E1 et E4 ;
  - tests d'ouverture pour E2 ;
  - un sample pour E1 et E3 (règle 6 de l'AGENTS.md de CasaEngine) ;
  - ADR et docs du moteur ;
  - vérificateur indépendant CONFIRMED.

### S3 — Contenu hérité porté (détaillée en Phase 3)

- **Résultat** : D4 et D8 sont faits.
- **Périmètre** :
  - `.gitattributes` ;
  - un mode « textures seules » de `scripts/LegacyModelGltfConverter`, pour convertir les 29 TGA en PNG sans perte ;
  - la copie des 2 DDS, de `LandscapeHeights.data`, des 10 CombiModel et des 5 PNG d'interface hérités (O4) ;
  - les 22 WAV renommés d'après les noms XACT (sans espaces), avec leurs `.sound` (P10) ;
  - le catalogue généré par un script aux identifiants déterministes ;
  - une règle de copie `Content\**` à la place des règles par dossier et des liens.
- **Point dur** : le jeu compare des noms de fichiers `.tga`, extension comprise :
  - les réglages de matériaux (`RacingGameLegacyMaterialTuning.cs:29-83`) ;
  - la peinture de la voiture (`LegacyCarVisualFactory.cs:34-36,279-296`) ;
  - `--verify-legacy-import-profile`.
  Il retombe aussi en silence sur une couleur unie quand un `.tga` manque. La tranche fait donc lire les PNG et comparer les noms sans extension, après avoir prouvé que chaque PNG a exactement les pixels de son TGA.
- **Acceptation** :
  - empreintes de pixels TGA et PNG identiques pour les 129 ;
  - A4 identique aux références ;
  - aucun lien vers `RacingGame/Content` dans le csproj.

### S4 — Code de jeu dans la DLL et composants générateurs (détaillée en Phase 4)

- **Résultat** : le code de jeu chargé par l'éditeur vit dans `RacingGame.Gameplay` :
  - composants, mode de jeu, contrôleur ;
  - lecture des pistes et des modèles hérités ;
  - profil et réglages de matériaux ;
  - composants générateurs de route, rails et poteaux, colonnes, terrain, physique de piste.
  Les mondes sont encore assemblés en code, mais à partir de ces composants.
- **Périmètre** :
  - `IRaceGameHost` (P5) ;
  - la voiture en entité et composants (P6) ;
  - le découpage de `LegacyTrackSceneFactory` entre le calcul des placements (pur, réutilisé par l'export de S6) et la génération de maillages (dans les composants) ;
  - des propriétés sérialisées par E1 ;
  - des noms de types préfixés, pour éviter les collisions de nom court ;
  - l'extension de l'export de scène (P9).
- **Acceptation** :
  - A4 identique aux références ;
  - les composants s'ouvrent dans l'éditeur sur un monde de test ;
  - un aller-retour d'enregistrement ne perd aucun champ.

### S5 — Modèles, matériaux et textures en assets (à détailler)

- **Résultat** : les 56 modèles sont des `.staticModel` avec leurs `.material` et `.texture` (P8), et le jeu les charge par le catalogue. Les règles de matériaux qui dépendaient des noms de fichiers ne s'appliquent plus à l'exécution : leurs valeurs sont dans les assets.
- **Acceptation** :
  - export par maillage identique (mêmes valeurs de matériaux) ;
  - A4 identique aux références ;
  - chaque `.staticModel`, `.material` et `.texture` s'ouvre dans l'éditeur.

### S6 — Mondes en assets (à détailler)

- **Résultat** :
  - Un export unique écrit les mondes :
    - `Worlds/FrontEnd.world` ;
    - `Worlds/CarSelection.world` ;
    - `Worlds/Race/TrackBeginner.world`, `TrackAdvanced.world`, `TrackExpert.world`.
  - Ils contiennent :
    - les entités des générateurs ;
    - le décor figé (D6) ;
    - les checkpoints et le départ, en entités ;
    - les lumières et l'environnement (ombres, cubemap du ciel).
  - `Entities/PlayerCar.entity` complète les mondes.
  - Le jeu charge ces assets. Les fabriques de mondes en code disparaissent ; le calcul des placements reste dans l'outil d'export.
- **Point dur** : le code retrouve des entités par leur nom (`PlayerStart`, `Checkpoint.*`, `Track.*`, `BootstrapCamera`, préfixe du nom du monde). Ces noms sont conservés.
- **Acceptation** : A3 et A4 identiques aux références.

### S7 — Contrôle de l'éditeur et clôture (à détailler)

- **Résultat** :
  - A1 à A6 ;
  - le script de contrôle A2 versionné dans `scripts/` ;
  - `RacingGameCasaEngine/Assets/README.md` et les docs à jour ;
  - les ADR qui remplacent l'ADR-0002 (périmètre du projet, contenu lié) ;
  - le pointeur CasaEngine sur le commit final du chantier moteur ;
  - le rapport de fin et le vérificateur de clôture.

---

## Phase 0 — Références avant chantier (S0)

### ✅ T0.1 — Plan, pointeur du moteur et références de parité

- **Objectif** : committer ce plan et le pointeur du sous-module (P3), puis prendre, avant toute modification, les références qui serviront à prouver la parité (P9) et l'état de l'éditeur.
- **Fichiers** :
  - `ai-agent/tasks/rgce-editor-project-tasks.md` ;
  - `ai-agent/README.md` (index) ;
  - le pointeur `CasaEngine`, dans un commit à part.
  Les références vont hors du dépôt : `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots\references-<sha>` et `artifacts/editor-project-baseline/`, que git ignore.
- **Étapes** :
  1. Committer le pointeur du sous-module à `2435f8ab9` (`chore(casaengine): …`), seul dans son commit (P3).
  2. `dotnet build RacingGame.slnx` et `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln`, sans erreur.
  3. Lancer avec le script de la session, chaque mode à part :
     - `--smoke-frontend` ;
     - `--capture-ui-screens`, dont la sortie est copiée en `references-<sha>` ;
     - `--capture-track-audit` ;
     - `--export-track-runtime-scene --track-runtime-export-file artifacts/editor-project-baseline/scene.json` ;
     - `--capture-car-profile-audit` et `--capture-car-top-speed-audit`, avec leurs fichiers dans `artifacts/editor-project-baseline/` ;
     - `--verify-legacy-import-profile`.
  4. Éditeur recompilé :
     - `scripts/capture_editor_screen.ps1` sur les 9 écrans, deux fois, avec `-OutputDirectory artifacts/editor-project-baseline/editor-screens/` puis `…/editor-screens-run2/`. L'écart entre les deux séries, mesuré par `UiCaptureCompare`, fixe le bruit d'un lancement à l'autre ;
     - **contrôle négatif du plugin** : l'éditeur ouvert sur le projet avec les arguments du script plus `--activate-panel output --logs-filter Warning`, qui affiche le panneau « Logs » réduit à ses avertissements (option ajoutée par T0.2). La capture va dans `artifacts/editor-project-baseline/editor-logs.png`. Elle doit montrer l'avertissement `Gameplay assembly 'RacingGameCasaEngine.UI.dll' contains no IPlugin implementation.` (`AssemblyManager.cs:29`), puisque la DLL actuelle n'a pas de plugin ; on le lit sur l'image. Si le message n'est pas lisible dans le panneau, T0.1 passe en ⚠️ : T1.2 n'aurait pas de contrôle réfutable ;
     - ouverture par l'automatisation de chaque entrée du catalogue, en notant celles qui ne s'ouvrent pas. Le rapport en attend 28 sans document.
  5. Écrire les chiffres dans la note de validation : codes de sortie, nombre de fichiers, entités par piste, assets ouverts et non ouverts, bruit des captures de l'éditeur.
- **Validation** :
  - tous les modes sortent avec le code 0 ;
  - les fichiers de référence existent ;
  - l'avertissement « contains no IPlugin implementation » est visible sur `editor-logs.png` ;
  - les réglages de l'auteur sont restaurés à l'identique (SHA-1).
- **Commits** :
  - `chore(casaengine): move the engine to 2435f8a` ;
  - `docs(racing-casa): plan the editor project chantier and record its baselines`.

> Validation (2026-10-08) :
> - **Pointeur** : `acb6ad9` enregistre `2435f8a`. Ce commit retire aussi `UseExactScale` de l'ancien jeu (`RacingGame.Shared/UI/MGUI/MguiUiHost.cs` et les 6 thèmes de `RacingGameButtonThemes.xaml`). En effet, MGUI `ecd09ba4`, inclus dans ce pointeur, supprime ce réglage : il dessine désormais tout texte à l'échelle mesurée, ce que le réglage obtenait (D24 du plan de mise à jour). Sans cela, `RacingGame.slnx` ne compilait plus (CS1061). L'ancien jeu n'a pas été lancé après ce retrait : seul le build le couvre.
> - **Builds** : `dotnet build RacingGame.slnx` : 0 erreur, 3 avertissements. `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln` : 0 erreur ; éditeur recompilé le 2026-10-08 à 14:46.
> - **Jeu** : les 7 modes sortent avec le code 0, et les réglages de l'auteur sont restaurés à l'identique après chaque lancement.
>   - `--smoke-frontend` ;
>   - `--capture-ui-screens`, copié en `Screenshots/references-acb6ad9` (10 PNG) ;
>   - `--capture-track-audit`, dont les 21 captures sont copiées dans `artifacts/editor-project-baseline/track-audit/` ;
>   - `--export-track-runtime-scene` : `scene.json`, 8,9 Mo. Mondes stabilisés à 639, 1025 et 2083 entités (Beginner, Advanced, Expert) ;
>   - `--capture-car-profile-audit` et `--capture-car-top-speed-audit` : les deux `.md` et `details/` ;
>   - `--verify-legacy-import-profile`.
> - **Éditeur** :
>   - 9 écrans capturés deux fois (`editor-screens/`, `editor-screens-run2/`) : identiques au pixel près, bruit nul ;
>   - catalogue ouvert entrée par entrée (`catalogue/catalogue-open.tsv`) : 66 entrées sur 94 s'ouvrent (6 sound, 50 sprite, 9 uiscreen, 1 world). Les 28 autres renvoient « Unable to open asset » : 10 png, 8 texture, 6 wav, 3 track, 1 fnt, comme prévu.
> - **Contrôle négatif du plugin, d'abord bloqué puis fait.** Au premier essai : Avec `--activate-panel output`, le panneau « Logs » est bien actif (« Activate panel: output » dans les diagnostics), mais sa liste reste vide sur la capture, à 3 s comme à 8 s. Pourtant `LogsPanel.RefreshList` y recopie tout `LoggerEditor.Entries`, ce qui ressemble à un défaut d'affichage de l'éditeur, à noter pour S2. L'avertissement n'était donc pas lisible (O6). L'auteur a fait corriger le panneau et ajouter `--logs-filter` (T0.2). Avec l'éditeur de `4ce5ea0`, `editor-logs-final.png` montre deux fois « Gameplay assembly 'RacingGameCasaEngine.UI.dll' contains no IPlugin implementation. » : le contrôle sait échouer.
> - Les captures `editor-screens/`, prises avec l'éditeur d'avant T0.2, restent la référence : la série `editor-screens-fixed-run2`, prise avec l'éditeur corrigé, leur est identique au pixel près.

### ✅ T0.2 — Panneau Logs de l'éditeur lisible

- Objectif : décision de l'auteur sur O6 (2026-10-08) : corriger d'abord le panneau Logs de l'éditeur, vide sur les captures, pour garder un contrôle du plugin par capture.
- Fichiers :
  - MGUI `MGUI.Core/UI/Containers/VirtualizingStackPanel.cs` et le test `MGUI.Tests/Architecture/VirtualizingStackPanelMeasureTests.cs` : commit `4e94d7e8` sur la branche MGUI `chantier/rgce-editor-project`, créée depuis `develop` ;
  - CasaEngine `CasaEngine.Editor/Controls/LogsPanel.cs`, le test `CasaEngine.Tests/Log/LogsPanelFormatTests.cs` et le pointeur MGUI : commit `c0f3343` sur `chantier/rgce-editor-project`, créée depuis `main` ;
  - RacingGame : pointeur `CasaEngine` sur `c0f3343`.
  - Seconde décision de l'auteur (2026-10-08) : ajouter une option d'automatisation `--logs-filter`. Elle touche CasaEngine `EditorAutomationOptions.cs`, `GameEditor.cs`, `LogsPanel.TrySetFilter` et les tests `EditorAutomationOptionsTests` et `LogsPanelFormatTests` : commit `4ce5ea0` sur `chantier/rgce-editor-project`. Le pointeur RacingGame passe ensuite sur `4ce5ea0`.
- Commits :
  - MGUI `fix(listbox): keep a virtualizing panel's height when its width is unknown` ;
  - CasaEngine `fix(editor): show the Logs panel entries`, puis `feat(editor): add a --logs-filter automation option` ;
  - RacingGame `chore(casaengine): move the engine to c0f3343`, puis `chore(casaengine): move the engine to 4ce5ea0`.

> Validation (2026-10-08) :
> - **Cause 1, la liste vide** (MGUI) :
>   - Une sonde lancée dans l'éditeur (scratchpad `editorlogsprobe`) lit le panneau par réflexion à chaque image. Il contient 31 entrées, mais la liste virtualisée n'est jamais mise en page : boîte de 0×0, aucune ligne réalisée.
>   - Sa première mesure se fait avant tout arrangement, avec une largeur infinie, puisque la barre horizontale est en `Auto` et que la liste n'a pas de bordure. Elle ne trouve alors aucune largeur et rend 0. `MGElement.UpdateMeasurement` (`MGElement.cs:6057`) met alors toute la taille à 0. La zone de défilement garde ce 0 en cache, et rien ne l'invalide ensuite.
>   - Forcer une nouvelle mise en page affichait les lignes. Correction : `CanConsumeSpaceInSingleDimension`, comme `MGBorder` et `MGSpacer`.
>   - Le nouveau test échouait avant (hauteur 0 au lieu de 930) et passe après.
> - **Cause 2, la balise `[/c]` affichée** (CasaEngine) : le message était échappé en doublant `[`, ce que MGUI ne reconnaît pas, et les crochets `[Trace  ]` n'étaient pas échappés. Toute la partie visible passe maintenant par `FTTokenizer.EscapeMarkdown`. Un nouveau test vérifie que la ligne affiche exactement le texte copié, dans la couleur de son niveau.
> - **Tests et builds** :
>   - `MGUI.Tests` : 3 198 sur 3 198 ;
>   - `CasaEngine.Tests` : 4 154 sur 4 154 ;
>   - `CasaEngine.Editor.MonoGame.sln` et `CasaEngine.MonoGame.sln` : 0 erreur ;
>   - `RacingGame.slnx` : 0 erreur.
> - **Jeu inchangé** : `--smoke-frontend` sort avec le code 0. `--capture-ui-screens` reste dans le seuil d'`UiCaptureCompare` face à `references-acb6ad9`, au plus 0,66 sur `race-hud`, écart de rythme connu.
> - **Éditeur** :
>   - le panneau Logs affiche ses lignes, sans `[/c]` ;
>   - les 9 écrans restent identiques au pixel près à `editor-screens/` (série `editor-screens-fixed-run2`). Dans la première série, la vraie souris est passée sur la fenêtre : coordonnées affichées sur l'aperçu de MainMenu et de Splash.
> - **Limite** : le panneau défile jusqu'à la fin et n'affiche que les 6 dernières lignes. L'avertissement du plugin (entrées 1 et 12 sur 31, le projet étant chargé deux fois par l'automatisation) n'est donc pas sur la capture : voir O6.
> - **Option `--logs-filter`** (`4ce5ea0`) :
>   - `dotnet build CasaEngine.Editor.MonoGame.sln` : 0 erreur ; `CasaEngine.Tests` : 4 159 sur 4 159 ;
>   - avec `--activate-panel output --logs-filter Warning`, les diagnostics indiquent « Logs filter: Warning » puis « [Automation] Logs filter 'Warning' applied. ». La capture `editor-logs-final.png` montre les avertissements, dont deux fois « Gameplay assembly 'RacingGameCasaEngine.UI.dll' contains no IPlugin implementation. ».
> - **Faits relevés dans le journal de l'éditeur**, pour S2 :
>   - `DllNotFoundException` sur `openal`, suivie d'un repli sur le moteur audio MonoGame ;
>   - deux avertissements de contexte de script non déchargé.

---

## Phase 1 — `RacingGame.Gameplay`, la DLL de gameplay de l'éditeur (S1)

### ✅ T1.1 — Renommer `RacingGameCasaEngine.UI` en `RacingGame.Gameplay`

- **Objectif** : renommer le projet, sans aucun changement de comportement (D7).
- **Fichiers** : les 37 fichiers de code, de projet et de réglages que liste `rg -l "RacingGameCasaEngine\.UI" --hidden --glob '*.cs' --glob '*.csproj' --glob '*.slnx' --glob '*.json' --glob '.gitignore' --glob '!.claude/**' --glob '!CasaEngine/**' --glob '!**/bin/**' --glob '!**/obj/**' .` (relevé du 2026-10-08) :
  - `git mv RacingGameCasaEngine.UI RacingGame.Gameplay` (18 fichiers suivis), puis `RacingGame.Gameplay/RacingGame.Gameplay.csproj` : `RootNamespace` et `AssemblyName` = `RacingGame.Gameplay` ; la cible de copie vers `Content` reste ;
  - les 17 fichiers `.cs` de `ViewModels/`, dont l'espace de noms passe de `RacingGameCasaEngine.UI.ViewModels` à `RacingGame.Gameplay.ViewModels` ;
  - les `using` des 15 fichiers `.cs` de l'exe qui citent l'ancien nom (`Bootstrap/RacingGameCasaEngineGame.cs`, 10 fichiers de `Screens/`, 4 de `UI/`) ;
  - `RacingGameCasaEngine/RacingGameCasaEngine.csproj` (référence de projet) et `RacingGame.slnx` ;
  - `RacingGameCasaEngine/Content/RacingGameCasaEngine.json` : `GameplayDllName = "RacingGame.Gameplay.dll"` ;
  - `.gitignore` (lignes 260-261), avec les nouveaux noms de la DLL et du PDB copiés.
  La documentation (`docs/`, `RacingGameCasaEngine/Assets/README.md`) relève de T1.3.
- **Étapes** :
  1. Vérifier que rien d'autre ne dépend de l'ancien nom :
     - les XAML ne citent que les espaces de noms de MGUI ;
     - les `.design.json` désignent les view models par leur nom court (`view_model_type`) ;
     - `scripts/` : `rg` avant et après.
  2. Renommer, mettre à jour les références, compiler.
  3. Supprimer de `Content/` l'ancienne DLL et l'ancien PDB, fichiers ignorés et locaux. Sans eux, un `GameplayDllName` faux ne peut plus trouver l'ancienne DLL.
- **Validation** :
  - `dotnet build RacingGame.slnx` : 0 erreur et aucun nouvel avertissement ;
  - `--smoke-frontend` sort avec le code 0 ;
  - `--capture-ui-screens` contre `references-<sha>` de T0.1 : dans le seuil d'`UiCaptureCompare`, hors rebond du logo ;
  - éditeur : `capture_editor_screen.ps1` sur les 9 écrans, dans `artifacts/editor-project-baseline/editor-screens-t1.1/`. `UiCaptureCompare` contre `editor-screens/` de T0.1 doit rester dans le bruit mesuré en T0.1. Un view model non résolu enlève les données de conception de l'aperçu, ce que la comparaison voit ;
  - la même commande `rg` que dans « Fichiers » ne trouve plus aucun fichier.
- **Commit** : `refactor(racing-casa): rename the view-model project to RacingGame.Gameplay`

> Validation (2026-10-08) :
> - **Correction du plan** : l'exe a son propre espace de noms `RacingGameCasaEngine.UI`, celui de son dossier `UI/` (`CarSelectionCarousel`, `MenuSounds`, `UiImageResources`, `OpaqueGeometryAlphaViewPipeline`). Il n'a aucun lien avec le projet renommé et reste tel quel : le renommer serait un refactor hors tâche.
>   - Seuls changent l'espace de noms des view models `RacingGameCasaEngine.UI.ViewModels` (17 fichiers, plus les `using` de 10 écrans), le projet, la DLL et ses références : 32 fichiers.
>   - La vérification `rg` de « Fichiers » ne pouvait donc pas tomber à zéro. Elle porte sur `RacingGameCasaEngine\.UI\.ViewModels|RacingGameCasaEngine\.UI\.(dll|pdb|csproj)|RacingGameCasaEngine\.UI[\/]` avec les mêmes globs, et ne trouve plus rien.
> - `dotnet build RacingGame.slnx` : 0 erreur. La cible de copie dépose `RacingGame.Gameplay.dll` et `.pdb` dans `Content/` ; l'ancienne DLL et son PDB sont supprimés.
> - `--smoke-frontend` sort avec le code 0, et les réglages sont restaurés à l'identique.
> - `--capture-ui-screens` contre `references-acb6ad9` : 0,00 sur les 10 états.
> - **Éditeur** :
>   - les 9 écrans (`editor-screens-t1.1/`) sont identiques au pixel près à `editor-screens/` ;
>   - **contrôle négatif** : sur une copie du projet dans le scratchpad, `GameplayDllName` pointant vers une DLL absente, MainMenu diffère d'une moyenne de 19,9, alors que le script de capture répond `OK`. La comparaison voit donc bien un view model non résolu.
> - Les fins de ligne CRLF des fichiers modifiés sont conservées.

### ✅ T1.2 — `Plugin` et recompilation depuis l'éditeur

- **Objectif** : `RacingGame.Gameplay` devient la DLL de gameplay au sens de CasaEngine (D1). L'éditeur trouve son `IPlugin` et sait la recompiler, comme RPGDemo.
- **Fichiers** :
  - `RacingGame.Gameplay/RacingGame.Gameplay.csproj`, qui reçoit une référence de projet à `CasaEngine/CasaEngine/CasaEngine.csproj` ;
  - `RacingGame.Gameplay/Plugin.cs` : `public sealed class Plugin : IPlugin`, `Initialize()` vide et rejouable, comme `Projects/CasaEngine.RPGDemo/Plugin.cs` ;
  - `RacingGameCasaEngine/Content/RacingGameCasaEngine.json` : `GameplayCsprojName = "../../RacingGame.Gameplay/RacingGame.Gameplay.csproj"` (chemin relatif au dossier du projet, comme RPGDemo) ;
  - `.gitignore`, si l'éditeur écrit `CasaEngine.EnginePath.props` à côté du csproj (O2).
- **Étapes** :
  1. Ajouter la référence et le `Plugin`, compiler.
  2. Déclarer `GameplayCsprojName`.
  3. Ouvrir le projet dans l'éditeur recompilé avec la commande du contrôle négatif de T0.1 (`--activate-panel output --logs-filter Warning`), capture dans `artifacts/editor-project-baseline/editor-logs-t1.2.png`. « Gameplay assembly not found » est journalisé au niveau Error (`AssemblyManager.cs:20`) : une seconde capture avec `--logs-filter Error` le couvre. Contrôler aussi le fichier écrit à côté du csproj (O2).
  4. Faire une recompilation depuis l'éditeur (`File > Build Scripts` ou Play). Si l'automatisation ne peut pas la déclencher, la vérification devient manuelle (O1).
- **Validation** :
  - `dotnet build RacingGame.slnx` sans erreur ;
  - `--smoke-frontend` sort avec le code 0 ;
  - **contrôle du plugin**, réfutable : sur `editor-logs-t1.2.png`, ni « Gameplay assembly not found » ni « contains no IPlugin implementation » (`AssemblyManager.cs:20,29`), alors que T0.1 montrait le second. Sans `Plugin.cs`, l'avertissement reviendrait ;
  - les 9 écrans de l'éditeur, capturés dans `editor-screens-t1.2/`, restent dans le bruit de T0.1 face à `editor-screens/` ;
  - la recompilation depuis l'éditeur réussit, ou 🧪 avec la manipulation notée pour l'auteur.
- **Commit** : `feat(racing-casa): make RacingGame.Gameplay the editor's gameplay plugin`

> Validation (2026-10-08) :
> - **Changements** :
>   - `RacingGame.Gameplay.csproj` référence `CasaEngine/CasaEngine/CasaEngine.csproj` ;
>   - `Plugin.cs` : `Plugin : IPlugin`, `Initialize()` vide et rejouable ;
>   - `GameplayCsprojName` = `../../RacingGame.Gameplay/RacingGame.Gameplay.csproj`.
> - **Build et jeu** : `dotnet build RacingGame.slnx` : 0 erreur. `--smoke-frontend` sort avec le code 0, et les réglages sont restaurés à l'identique.
> - **Contrôle du plugin**, avec `--activate-panel output --logs-filter Warning` : `editor-logs-t1.2-final.png` ne montre pas « contains no IPlugin implementation », que le contrôle négatif de T0.1 montrait deux fois. « Gameplay assembly not found » est journalisé au niveau Error, que ce filtre ne montre pas. Le vérificateur de S1 l'a couvert : la capture filtrée sur Error est vide, et sur une copie du projet pointant vers une DLL absente, elle affiche ce message deux fois. Restent deux avertissements de déchargement du contexte de script, déjà présents en T0.1 et sans rapport avec ce chantier.
> - **Écrans de l'éditeur** : les 9 de `editor-screens-t1.2/` sont identiques au pixel près à `editor-screens/`.
> - **O2** : à l'ouverture du projet, l'éditeur écrit `RacingGame.Gameplay/CasaEngine.EnginePath.props`, dont l'en-tête dit « Machine-local : ne pas committer » et qui pointe vers l'éditeur compilé de cette machine. Il est ajouté au `.gitignore`.
> - **O1, recompilation depuis l'éditeur**, automatisable sans manipulation : `Plugin.cs` touché, puis l'éditeur lancé avec `--project … --play-smoke`.
>   - Le rapport donne les scripts de départ `Content\RacingGame.Gameplay.dll`, puis les scripts de jeu `Content\.casaeditor\script-build\20261008-135457-714\RacingGame.Gameplay.dll` : l'éditeur a recompilé le csproj et chargé la nouvelle DLL.
>   - Les 15 contrôles du rapport sont PASS, et `Content\RacingGame.Gameplay.dll` est remplacée.
>   - `.casaeditor/` est déjà ignoré par git.

### ✅ T1.3 — ADR et documentation de la DLL de gameplay

- **Objectif** : consigner D1 et D7 et mettre la documentation à jour.
- **Fichiers** :
  - `docs/decisions/0015-racinggame-gameplay-is-the-editor-gameplay-assembly.md` (skill `adr`), qui remplace la partie « view models » de l'ADR-0002 ;
  - l'ADR-0002, dont le statut devient « Superseded in part by ADR-0015 » sans autre changement ;
  - `docs/decisions/README.md` ;
  - `RacingGameCasaEngine/Assets/README.md:43` et les autres docs citant l'ancien nom (`rg`).
- **Validation** :
  - `rg -l "RacingGameCasaEngine\.UI" docs RacingGameCasaEngine/Assets` ne trouve plus que l'ADR-0002 historique et la nouvelle ADR-0015, qui cite l'ancien nom. La portée exclut le code, vérifié en T1.1 ; les plans de `ai-agent/` sont historiques ;
  - vérificateur indépendant de S1 (renommage, plugin, parité des captures, écrans dans l'éditeur) : CONFIRMED.
- **Commit** : `docs(adr): record RacingGame.Gameplay as the editor's gameplay assembly`

> Validation (2026-10-08) :
> - **ADR-0015** (`Accepted`) consigne D1 et D7 :
>   - renommage, et espace de noms propre à l'exe inchangé ;
>   - `Plugin` et `GameplayCsprojName` ;
>   - chargement unique dans le jeu (P4) ;
>   - `.props` local à la machine ;
>   - conséquences : copie fantôme de la DLL seule, dossiers jamais nettoyés, `Initialize` rejouable, noms de types uniques.
> - **ADR-0002** : statut « Accepted; superseded in part by ADR-0015 (view-model library, `GameplayCsprojName`) », sans autre changement. L'index est à jour.
> - `RacingGameCasaEngine/Assets/README.md` : DLL de gameplay, recompilation avant Play, `.props`, dossiers `script-build`.
> - `rg -l "RacingGameCasaEngine\.UI" docs RacingGameCasaEngine/Assets` ne trouve que l'ADR-0002 et l'ADR-0015.
> - **Vérificateur indépendant de S1 et de T0.2** (2026-10-08) : CONFIRMED sur les 6 points.
>   - Test MGUI : rouge sans le correctif (930 attendu, 0 obtenu), 3 198 tests verts avec.
>   - CasaEngine : build de l'éditeur à 0 erreur, 4 159 tests verts ; panneau Logs lisible ; `--logs-filter`.
>   - Renommage sans reste. `RacingGame.slnx` compile ; smoke ; captures d'interface à 0,00 ; 9 écrans de l'éditeur identiques.
>   - Plugin trouvé, avec deux contrôles négatifs : DLL absente, et DLL sans `IPlugin`.
>   - Recompilation depuis l'éditeur, 15 contrôles PASS. ADR et pointeurs exacts.
>   - Ses remarques, toutes traitées dans ce commit :
>     - P3 : un chemin de la note de T1.2 corrompu par un échappement ;
>     - P3 : O6 périmé ;
>     - P3 : le contrôle « Gameplay assembly not found » sur la capture Warning ne pouvait pas échouer. Il est couvert par une capture Error, et l'étape 3 de T1.2 le dit désormais ;
>     - P4 : dossier `RacingGameCasaEngine.UI/obj/` résiduel, ignoré par git et supprimé.

---

## Phase 2 — Fondations moteur et éditeur (S2)

Le détail est dans le plan moteur `CasaEngine/ai-agent/tasks/rgce-editor-project-engine-tasks.md` (T1 → T9), exécuté sur la branche CasaEngine `chantier/rgce-editor-project`.

### ✅ T2.1 — Pointeur moteur de fin de S2 et catalogue ouvert dans l'éditeur

- **Objectif** : enregistrer dans RacingGame le pointeur `CasaEngine` sur la fin de S2 (règle du pointeur), puis mesurer ce que S2 change pour ce projet.
- **Fichiers** : le pointeur `CasaEngine` ; ce plan.
- **Étapes** :
  1. Committer le pointeur `chore(casaengine): …`.
  2. Lancer `dotnet build RacingGame.slnx` et `--smoke-frontend`.
  3. Ouvrir le catalogue entrée par entrée dans l'éditeur recompilé, avec la sonde `catalogue_open.ps1` du scratchpad et le code de sortie de l'éditeur.
- **Validation** :
  - build à 0 erreur et smoke à 0 ;
  - les 94 entrées du catalogue s'ouvrent, contre 66 en T0.1.
- **Commit** : `chore(casaengine): move the engine to the end of the editor foundations`, avec la note de validation.

> Validation (2026-10-08) :
> - **Pointeur** : `CasaEngine` passe sur `f5dee0d6c`, clôture du plan moteur, archivé dans `CasaEngine/ai-agent/tasks/archive/rgce-editor-project-engine-tasks.md`. Le vérificateur de S2 avait d'abord rendu REFUTED (restauration d'un `.texture` qui perdait un BOM) ; après la correction `2b9d5ae35`, le contre-vérificateur a rendu CONFIRMED.
> - **Build et smoke** : `dotnet build RacingGame.slnx` donne 0 erreur. Les nouveaux avertissements viennent du sous-module ; aucun ne touche le jeu. `--smoke-frontend` sort avec le code 0.
> - **Catalogue** : depuis S3, il compte 345 entrées et non plus 94. Elles ont toutes été ouvertes une par une, avec une copie de l'éditeur de fin de S2 et une copie du contenu. Résultat dans `artifacts/editor-project-baseline/catalogue-t2.1/` :
>   - **345 sur 345** s'ouvrent avec le code 0, contre 66 sur 94 en T0.1 ;
>   - par type : 148 png, 56 gltf, 50 sprite, 28 wav, 28 sound, 10 combimodel, 9 uiscreen, 8 texture, 3 track, 2 dds, 1 data, 1 fnt, 1 world ;
>   - la copie du contenu est identique avant et après (SHA-1 de chaque fichier) ;
>   - deux captures relues : `CombiBuildings.CombiModel` en aperçu texte, et `AlphaPalm2.gltf` en 3D, en gris neutre.

---

## Phase 3 — Contenu hérité porté et catalogué (S3)

Faits relevés pour cette phase (2026-10-08) :
- **Noms `.tga` dans le code du jeu** :
  - `RacingGameLegacyMaterialTuning` compare `Path.GetFileName` de la texture diffuse avec des listes de noms `.tga` (`Bootstrap/RacingGameLegacyMaterialTuning.cs:29-83,108,149,169`) ;
  - `LegacyCarVisualFactory` charge `Textures/RacerCar*.tga` et compare ces noms (`Components/LegacyCarVisualFactory.cs:34-36,273-301,366`) ;
  - la route, le terrain, les rails et les colonnes chargent leurs textures `.tga` par nom littéral (`Worlds/LegacyTrackSceneFactory.cs:80-81`, `.Terrain.cs:139-140`, `.GuardRails.cs:412-431`) ;
  - `LegacyImportProfileVerifier` cherche des matériaux par nom `.tga` (`Bootstrap/LegacyImportProfileVerifier.cs:53-141`).
- **Résolution des textures des modèles** : `LegacyGltfModelReader.ResolveTexturePath` cherche le `..\textures\X.tga` des extras du glTF, puis son nom de fichier dans le dossier du modèle, sinon rien (`Bootstrap/LegacyGltfModelReader.cs:180-198`). Sans le TGA, c'est l'image PNG du glTF qui sert.
- **Convertisseur** : `scripts/LegacyModelGltfConverter/LegacyTextureConverter.ConvertToPng` décode le TGA par StbImageSharp, le décodeur du jeu, et l'écrit en PNG RGBA sans perte. Il n'est appelé que par la conversion des modèles (`Program.cs`).
- **Fichiers à porter** :
  - les 22 WAV et la table des repères XACT, dans `editor-disc2/wav_table.md` et `cue_table.md` du scratchpad ;
  - les volumes `.sound` existants, qui suivent `10^(dB/2000)`.

### ✅ T3.1 — `.gitattributes` et 29 TGA convertis en PNG

- **Objectif** : D8, et les 29 textures qui n'existent qu'en TGA rejoignent `Content/Textures` en PNG aux pixels identiques.
- **Fichiers** :
  - `.gitattributes` (`*.png`, `*.wav`, `*.dds`, `*.data`, `*.tga` et `*.bin` binaires ; `*.bin` est le tampon des glTF de `Content/Models`) ;
  - `scripts/LegacyModelGltfConverter/Program.cs` (modes `--textures <dossier tga> <dossier png> <nom>...` et `--verify-textures <dossier tga> <dossier png>`) et son README ;
  - les 29 PNG de `RacingGameCasaEngine/Content/Textures`.
- **Étapes** :
  1. Écrire le `.gitattributes`, puis `git add --renormalize --dry-run .` : rien ne doit changer.
  2. Ajouter les deux modes. `--verify-textures` décode chaque TGA et son PNG par StbImageSharp en RGBA, compare les octets et liste les différences.
  3. Convertir les 29 noms (`tga29.md` du scratchpad).
- **Validation** :
  - `--verify-textures` sur les 129 paires TGA et PNG : 0 différence ;
  - contrôle négatif : un PNG modifié d'un pixel, copié dans le scratchpad, est signalé ;
  - `dotnet build` du convertisseur et de `RacingGame.slnx` : 0 erreur.
- **Commit** : `feat(racing-casa): convert the remaining legacy TGA textures to PNG`

> Validation (2026-10-08) :
> - **`.gitattributes`** : `*.png`, `*.wav`, `*.dds`, `*.data`, `*.tga` et `*.bin` en `binary`.
>   - `git add --renormalize .` a été lancé dans un index temporaire (`GIT_INDEX_FILE`), pour ne pas toucher l'index réel.
>   - La même liste de 59 fichiers change avec ou sans le `.gitattributes`. Ce sont des sources `.cs` et `.csproj` déjà stockées en CRLF, sans rapport avec cette tâche, et aucune n'a une extension binaire : le fichier ne change la normalisation d'aucun fichier.
> - **Convertisseur** :
>   - `--textures` convertit les noms donnés par `LegacyTextureConverter.ConvertToPng`, la conversion des textures des modèles ;
>   - `--verify-textures` décode chaque `.tga` et le `.png` de même nom par StbImageSharp en RGBA, compare les octets, puis liste les paires différentes (nombre de pixels, premier pixel) et les `.tga` sans `.png`. Code de sortie 1 s'il y en a.
> - **Avant la conversion** : 129 `.tga`, 100 paires, 0 différence, 29 sans `.png`. Ce sont exactement les 29 de `tga29.md`.
> - **Conversion** des 29, puis `--verify-textures RacingGame/Content/Textures RacingGameCasaEngine/Content/Textures` : 129 paires, 0 différence, 0 sans `.png`.
> - **Contrôle négatif** : copie de `Road.png` dans le scratchpad, avec le pixel (10, 20) modifié. Le contrôle répond « Road: DIFFERENT 1 pixels differ, first at (10, 20) » et sort avec le code 1.
> - **Build** : convertisseur et `RacingGame.slnx` à 0 erreur ; `--smoke-frontend` sort avec le code 0. Les 29 PNG sont copiés dans la sortie par la règle `Content\Textures\*.png`. Le jeu lit encore les `.tga` liés jusqu'à T3.5.

### ✅ T3.2 — Cubemaps, carte de hauteurs, CombiModel et images d'interface héritées

- **Objectif** : copier dans `Content` ce qui n'y est encore que par lien.
- **Fichiers** :
  - `Content/Textures/SkyCubeMap.dds` et `NormalizeCubeMap.dds` ;
  - `Content/LandscapeHeights.data`, chemin que le jeu lit déjà ;
  - `Content/Models/*.CombiModel` (10) ;
  - `Content/Textures/{GameFont,HelpScreenWindows,HelpScreenXbox360,MouseCursor,OptionsScreenXbox360}.png` (O4).
- **Validation** : chaque copie est identique octet pour octet à sa source dans `RacingGame/Content` (SHA-1).
- **Commit** : `feat(racing-casa): copy the legacy cube maps, height map, combi models and UI images into the project`

> Validation (2026-10-08) :
> - **Copies** : 18 fichiers, chacun identique octet pour octet à sa source de `RacingGame/Content` (SHA-1) :
>   - 2 DDS ;
>   - `LandscapeHeights.data` ;
>   - 10 CombiModel ;
>   - 5 PNG d'interface (`GameFont`, `HelpScreenWindows`, `HelpScreenXbox360`, `MouseCursor`, `OptionsScreenXbox360`).
>   Aucun ne remplace un fichier existant.
> - **Ordre** : cette tâche passe avant T3.1, faute de pouvoir compiler le convertisseur pendant les travaux moteur de S2. Sans `.gitattributes`, `git ls-files --eol` classe déjà les DDS et `LandscapeHeights.data` en binaire (`-text`), et ce dernier ne contient aucun octet 0x0A ni 0x0D : aucune conversion de fin de ligne n'est possible.
> - **Sortie du jeu inchangée** : les liens du csproj copient encore les mêmes fichiers aux mêmes chemins de sortie, et T3.5 les retire.

### ✅ T3.3 — Les 22 sons et musiques non portés

- **Objectif** : P10. Pour chaque son : la WAV d'origine sous le nom de sa vague XACT, sans espace (`Content/Audio/<Nom>.wav`), et un `.sound` :
  - `volume` = `10^(dB/2000)` ;
  - bus `Sfx` pour les effets, `Music` pour les 3 musiques, avec `is_streaming: true` ;
  - `is_looped: true` pour les vagues que XACT joue en boucle : `Gear1` → `Gear5` et les musiques ;
  - `pitch` 0.
- **Fichiers** : 22 WAV, 22 `.sound` ; le générateur de T3.4 écrit les `.sound`.
- **Validation** :
  - chaque WAV est identique octet pour octet à l'original (SHA-1) ;
  - chaque `.sound` donne le volume de la table XACT à 0,001 près ;
  - les 6 sons déjà portés suivent la même formule et restent inchangés.
- **Commit** : avec T3.4.

> Validation (2026-10-08), par le générateur de T3.4 :
> - les 22 WAV sont identiques octet pour octet à leur original de `RacingGame/Content/Audio/Waves` (SHA-1), copiés sous le nom de leur vague XACT sans espace ;
> - chaque `.sound` concorde avec la table XACT (`cue_table.md`) :
>   - volume à 0,001 près : 1,000 à 0 dB, 0,891 à -1 dB, 0,398 à -8 dB ;
>   - bus `Sfx`, sauf les 3 musiques en `Music` et `is_streaming: true` ;
>   - `is_looped: true` pour `Gear1` → `Gear5` et les musiques, dont le « Play Wave Loop Count » XACT vaut 255 ;
> - les `.sound` gardent la forme des 6 sons déjà portés : indentation de 2 espaces, LF, sans saut de ligne final. Ces 6 sons restent inchangés, et `git status` ne les signale pas.

### ✅ T3.4 — Catalogue de tout le contenu

- **Objectif** : chaque fichier de `Content` est catalogué, sauf les fichiers du projet et les fichiers annexes. Ces exceptions sont :
  - le fichier projet, le catalogue, la DLL et le PDB du gameplay, `.casaeditor/` ;
  - les `.design.json` et `.xaml` des écrans ;
  - les `.bin` des glTF, référencés par leur `.gltf` ;
  - les `.fnt` et PNG de police déjà catalogués.
- **Fichiers** :
  - `scripts/generate_rgce_content_assets.py`, sur le patron de `generate_rgce_ui_assets.py` :
    - identifiants uuid5 du nom, avec le même espace de noms ;
    - entrées possédées par une liste explicite ;
    - format du fichier conservé.
  - Les entrées qu'il possède :
    - `Texture.<nom>` : PNG des textures 3D et d'interface hérités ;
    - `Texture.<nom>` : les 2 DDS (type `dds`) ;
    - `Data.LandscapeHeights` (type `data`) ;
    - `Data.<nom>` (type `combimodel`), par exemple `Data.CombiBuildings` ;
    - `ModelSource.<nom>` : les 56 glTF (type `gltf`) ;
    - `Sound.<nom>` et `Sound.<nom>Wave` : les 22 sons de T3.3.
  - Les entrées existantes sont gardées telles quelles. C'est le cas de `MenuBackground` et `MenuButtons`, déjà catalogués sur `Textures/background.png` et `buttons.png`.
  - `RacingGameCasaEngine/Content/AssetInfos.json`.
- **Validation** :
  - un deuxième lancement du script ne change aucun octet ;
  - un script de contrôle liste les fichiers de `Content` non catalogués : 0 hors exceptions ;
  - aucun id ni nom en double ;
  - `--smoke-frontend` sort avec le code 0.
- **Commit** : `feat(racing-casa): catalogue every content file and port the remaining sounds`

> Validation (2026-10-08) :
> - **Catalogue** : il passe de 94 à 345 entrées. Les 94 entrées existantes sont gardées à l'identique et à leur place. Les 251 ajoutées :
>   - 138 `png` (`Texture.<nom>`) : les 140 PNG de `Content/Textures`, moins `background.png` et `buttons.png`, déjà catalogués ;
>   - 2 `dds` ;
>   - 1 `data` ;
>   - 10 `combimodel` ;
>   - 56 `gltf` ;
>   - 22 `wav` et 22 `sound`.
>   Les types sont ceux qu'`AssetInfo.InferAssetType` déduit de l'extension.
> - **Un second lancement** du générateur ne change aucun octet (SHA-1 du catalogue et de `Content/Audio`).
> - **`--check`** : 0 fichier de `Content` hors catalogue, en dehors des exceptions ci-dessus.
> - **Doublons** : aucun id ni nom en double ; le générateur s'arrête s'il en trouve.
> - **Format** : le catalogue garde son indentation de 2 espaces, le CRLF et l'absence de saut de ligne final.
> - **Jeu** : `RacingGame.slnx` compile avec 0 erreur, et `--smoke-frontend` sort avec le code 0, sans avertissement dans le journal. La sortie contient les 56 fichiers de `Content/Audio`, que la règle `Content\Audio\*.*` copie.

### ✅ T3.5 — Le jeu ne lit plus que `Content`

- **Objectif** : retirer les liens vers `RacingGame/Content` sans changer le rendu.
- **Fichiers** :
  - `LegacyGltfModelReader.ResolveTexturePath` : un `.tga` absent se résout en `.png` de même nom ;
  - `RacingGameLegacyMaterialTuning` et `LegacyImportProfileVerifier` comparent les noms sans extension ;
  - `LegacyCarVisualFactory` et `LegacyTrackSceneFactory*` chargent les `.png` ;
  - `RacingGameCasaEngine.csproj` :
    - une règle `Content\**` remplace les règles par dossier. Elle exclut exactement `RacingGameCasaEngine.json`, `RacingGame.Gameplay.dll`, `RacingGame.Gameplay.pdb`, `.casaeditor\**` et `**\*.design.json`. `AssetInfos.json` et les `.xaml` restent copiés ;
    - les **3 liens** vers `..\RacingGame\Content` sont retirés : `Textures\*.*`, `LandscapeHeights.data` et `*.CombiModel` ;
    - le lien vers `..\CasaEngine\CasaEngine\Content\Fonts\tahoma.ttf`, police que le moteur lit au démarrage (`CasaEngineGame.cs:494`), **reste**.
- **Avant la modification du code**, sur le commit de T3.4 :
  - liste de sortie : chemin relatif et SHA-1 de chaque fichier du dossier de sortie `Content`, dans `artifacts/editor-project-baseline/output-content-t3.4.txt` ;
  - deux lancements de `--capture-track-audit` (`track-audit-t3.4-a/`, `track-audit-t3.4-b/`) ;
  - un `--export-track-runtime-scene` (`scene-t3.4.json`).
- **Outil de comparaison des 21 captures** : la sonde `compare_audit.py` du scratchpad. Elle apparie les captures par nom sans horodatage, puis calcule la différence absolue moyenne par canal R, G et B, la même mesure qu'`UiCaptureCompare`. Le seuil de chaque capture est son bruit entre les lancements `a` et `b`, plus 0,5.
- **Validation** :
  - `rg "RacingGame\\\\Content" RacingGameCasaEngine/RacingGameCasaEngine.csproj` ne trouve rien, et le lien de `tahoma.ttf` est toujours là ;
  - `dotnet build RacingGame.slnx` : 0 erreur ;
  - liste de sortie après T3.5 contre celle de T3.4 : la seule différence permise est le retrait des 129 `.tga`. `Content\Fonts\tahoma.ttf` et `Content\AssetInfos.json` sont présents ;
  - `--capture-track-audit` après T3.5 contre `track-audit-t3.4-a/` : chacune des 21 captures sous son seuil. La note donne le bruit `a`/`b` et l'écart de chaque capture ;
  - **contrôle négatif** : `Road.png` renommé dans le dossier de sortie (puis remis), un lancement de `--capture-track-audit` dépasse le seuil sur au moins une capture ;
  - `--export-track-runtime-scene` contre `scene-t3.4.json` : mêmes entités, modèles, noms et types de matériaux, transforms ;
  - `--smoke-frontend` sort avec le code 0 ;
  - `--capture-ui-screens` reste dans le seuil d'`UiCaptureCompare` face aux références ;
  - `--verify-legacy-import-profile` sort avec le code 0 ;
  - audits des voitures identiques à ceux de S0.
- **Commit** : `refactor(racing-casa): read every game file from the project content`

> Validation (2026-10-08) :
> - **Code** :
>   - `LegacyGltfModelReader.ResolveTexturePath` : pour chacun des deux chemins candidats, un `.tga` absent se résout en `.png` de même nom ;
>   - `RacingGameLegacyMaterialTuning`, `LegacyImportProfileVerifier` et `LegacyCarVisualFactory` comparent les noms sans extension. Leurs listes de noms n'ont plus de `.tga` ;
>   - `LegacyTrackSceneFactory*` et `LegacyCarVisualFactory` chargent les `.png` ;
>   - `RacingGameLegacyMaterialImportProfile.UsesLegacyAlphaCutout` comparait déjà les noms sans extension : inchangé.
> - **csproj** :
>   - une règle `Content\**` ; `Never` pour `**\*.design.json`, `RacingGameCasaEngine.json`, `RacingGame.Gameplay.dll` et `.pdb`, et `.casaeditor\**` ;
>   - les 3 liens vers `..\RacingGame\Content` sont retirés ;
>   - `rg -F "RacingGame\Content"` ne trouve plus rien, et le lien de `tahoma.ttf` est toujours là.
> - **Références** prises au commit de T3.4 (`0b6913e`), dans `artifacts/editor-project-baseline/` : `output-content-t3.4.txt` (634 fichiers, dont 129 `.tga`), `track-audit-t3.4-a/` et `-b/`, `scene-t3.4.json`.
> - **Build** : le dossier de sortie `Content` a été vidé, puis `RacingGame.slnx` compilé : 0 erreur.
> - **Liste de sortie** : 505 fichiers. Seuls les 129 `.tga` sont retirés ; aucun fichier n'est ajouté ni modifié (SHA-1). `Fonts/tahoma.ttf` et `AssetInfos.json` sont présents.
> - **Export de scène** : identique à `scene-t3.4.json`, hors horodatage : entités, modèles, noms et types de matériaux, transforms.
> - **Autres contrôles** :
>   - `--smoke-frontend` : code 0 ;
>   - `--capture-ui-screens` contre `references-acb6ad9` : 0,00 sur les 10 états, dont le choix de voiture et le HUD de course ;
>   - `--verify-legacy-import-profile` : code 0 ;
>   - audits des voitures identiques à ceux de S0, hors ligne `Generated:` et heure murale en tête des lignes de `details/`.
> - **Audit de piste** (`compare_audit.py`, seuil égal au bruit `a`/`b` plus 0,5) :
>   - les 3 vues `chase-road-only` sont identiques aux références (0,000), comme les deux références entre elles ;
>   - **écart expliqué** : 7 des 18 vues posées dépassent leur seuil. Ces vues ne sont pas reproductibles d'un lancement à l'autre :
>     - le bruit entre les deux références va jusqu'à 75 ;
>     - après la vue `chase-road-only`, le validateur active la caméra libre de débogage (`TrackMigrationCaptureValidator.cs:119`). Celle-ci lit la souris et le clavier (`DebugFreeCameraComponent.cs:61,84`) et remplace la pose à chaque image ;
>     - les images de la référence `a`, de la référence `b` et de T3.5 montrent trois poses différentes d'une même vue (terrain, sol, ciel), avec des textures correctes.
>     Le critère « chacune des 21 captures sous son seuil » n'est pas mesurable avec cet outil. La parité repose donc sur les 3 vues déterministes, l'export de scène, les captures d'interface, la liste de sortie et les pixels identiques de T3.1. T4.1 rend l'audit déterministe.
>   - **contrôle négatif** : `Road.png` renommé dans le dossier de sortie, les 3 vues `chase-road-only` passent à 31, 38 et 48 sur leur canal le plus écarté, pour un seuil de 0,5, avec le code 1. Le fichier a été remis, avec un SHA-1 identique à sa source.

### ✅ T3.6 — ADR, documentation et clôture de S3

- **Fichiers** :
  - `docs/decisions/0016-rgce-content-holds-every-game-file.md`. Il remplace la règle « le contenu lié non catalogué reste lié » de l'ADR-0002, dont le statut devient « superseded in part by ADR-0015, ADR-0016 » ;
  - index des ADR ;
  - `RacingGameCasaEngine/Assets/README.md` (contenu, générateur, sons) ;
  - ce plan.
- **Validation** : vérificateur indépendant de S3, CONFIRMED.
- **Commit** : `docs(adr): record that the project content holds every game file`

> Validation (2026-10-08) :
> - **Documents** :
>   - ADR-0016, avec son entrée dans l'index ;
>   - le statut de l'ADR-0002, « superseded in part by ADR-0015 … and ADR-0016 (linked content) » ;
>   - la section « Content and catalogue » et les sons de `RacingGameCasaEngine/Assets/README.md`.
> - **Fichier projet** : un File > Save de l'éditeur, fait sur une copie du projet avec `scripts/capture_editor_screen.ps1 -SaveProject`, laisse le catalogue de 345 entrées identique octet pour octet. Il déplaçait la clé `GameplayCsprojName`, ajoutée en T1.2 hors de l'ordre où l'éditeur l'écrit. Le fichier validé est remis dans cet ordre (`ProjectSettingsHelper.cs:126-132`), ce qui rend juste la phrase du README : « a Save leaves them unchanged ».
> - **Vérificateur indépendant de S3 : CONFIRMED**, sans constat P0 à P2. Il a refait :
>   - `--check` ;
>   - deux lancements du générateur sur une copie : 662 fichiers identiques ;
>   - `--verify-textures`, et une seconde comparaison des 129 paires avec un autre décodeur (Pillow) : 0 différence ;
>   - les 22 `.sound` contre `RacingGame.xap`, qu'il a lu lui-même ;
>   - la résolution des 102 noms de textures hérités des glTF, avant et après : mêmes fichiers, `.tga` devenu `.png` ;
>   - la liste de sortie, les exports de scène, un `--smoke-frontend`, et la relecture des captures de l'audit.
>   Il juge l'explication des 7 vues posées étayée par le code et par les images.
> - **Ses 4 avis, traités dans ce commit** :
>   - P3, volumes des catégories XACT : `Music` -3 dB et `Gears` -4 dB ne sont pas repris. L'ADR-0016 et le README le disent maintenant, au lieu de « garde les réglages XACT » ; il faudra les reprendre quand le jeu jouera ces sons ;
>   - P4 : le contenu propre au moteur (shaders, polices, icônes) arrive dans la sortie par la référence au projet CasaEngine. L'ADR et le README le précisent ;
>   - P4 : « Textures are PNG » devient « PNG, sauf les deux DDS ; 100 TGA convertis par la conversion des modèles, 29 par `--textures` » ;
>   - P4 : les chiffres du contrôle négatif de T3.5 sont corrigés (31, 38 et 48 sur le canal le plus écarté).

---

## Phase 4 — Code de jeu dans la DLL et composants générateurs (S4)

> Relecture (2026-10-08) : trois vérificateurs de plan successifs. Les deux premiers ont rendu REVISE : le premier avec 4 blocages (fermeture des dépendances de T4.2, effet du drapeau de T4.1 sur la conduite, physique absente du monde de test, prérequis et arrêts), le second avec 1 (implémentation d'`IRaceGameHost`). Tous ont été corrigés. Le troisième a rendu **READY**. L'exécution suit l'accord de l'auteur, « fait toutes les taches ».

Faits relevés pour cette phase (2026-10-08). Les sources sont 4 découvertes en lecture seule, rangées dans `s4-disc/` du scratchpad, les fichiers relus en session, et une relecture par un vérificateur de plan.

- **Audit de piste non reproductible** :
  - après la vue `chase-road-only`, le validateur active la caméra de débogage (`Bootstrap/TrackMigrationCaptureValidator.cs:117-119`) et la coupe au retour au front-end (`:207-209`) ;
  - fenêtre active, la caméra libre recalcule à chaque image la direction à partir de son propre lacet et de son tangage, plus le décalage de la souris (`Components/DebugFreeCameraComponent.cs:40-57,59-125`). Elle remplace la pose du validateur. Fenêtre inactive, elle ne fait rien (`:40`). Le résultat dépend donc du focus et de la souris ;
  - la caméra de débogage active arrête aussi :
    - le rig de poursuite (`Components/ChaseCameraRigComponent.cs:86`) ;
    - la conduite : entrées du joueur, chrono et checkpoints (`Components/RaceFlowCoordinatorComponent.cs:57-61`, `Bootstrap/RaceRuntimeUiCoordinator.cs:124`) ;
    - le curseur (`Bootstrap/RacingGameCasaEngineGame.cs:390-393`) ;
  - le compte à rebours et le feu de départ avancent pendant les vues posées (`RaceFlowCoordinatorComponent.cs:55-56`) ;
  - une capture lit la dernière image présentée (`RacingGameCasaEngineGame.cs:470`), qui peut avoir une mise à jour de retard (mémoire de session sur le décalage des captures).
- **Export de scène actuel** :
  - un enregistrement par entité : nom, modèle, transform, et sous-maillages avec le nom du maillage, le nom et le type du matériau. Pas de données de sommets (`Bootstrap/TrackRuntimeSceneExporter.cs:61-116`) ;
  - les sommets et les indices d'un maillage sont lisibles par `StaticModelMesh.GetVertices()` et `GetIndices()` (`CasaEngine/CasaEngine/Framework/Rendering/Models/StaticModelMesh.cs:69,71`).
- **Code de jeu dans l'exe** :
  - 12 types dérivés d'`EntityComponent`, `SceneComponent` ou `Entity`, plus `RaceGameMode` et `RacingPlayerController`, dans `Components/`, `Entities/` et `GameFramework/`. Ils sont publics, sauf `RaceTrackPhysicsComponent`, qui est interne ;
  - leur fermeture de dépendances tient dans `Components/`, `Entities/`, `GameFramework/`, `Gameplay/`, `Worlds/LegacyTrackSceneFactory*.cs`, `Worlds/RaceTrackPhysicsProfile.cs` et ces fichiers de `Bootstrap/` :
    - `RuntimeRaceSession`, `RaceFrontEndCatalog`, `RaceFrontEndState` et `RaceSkySystem` ;
    - `RaceBootstrapAssets`, lu par `LegacyTrackSceneFactory.cs:85` ;
    - `LegacyGltfModelReader`, `RacingGameImportProfiles` et `RacingGameLegacyMaterial*`.
  - Elle s'y ajoute deux liens vers l'exe :
    - `RaceCourseDebugComponent.cs:30-31` lit deux constantes de `RaceWorldFactory` ;
    - `RacingGameCasaEngineGame` est utilisé directement (voir l'accès au jeu).
  - Aucun écran, aucune interface ni persistance n'y figure. `RuntimeRaceSession`, `RaceFrontEndCatalog` et `RaceFrontEndState` n'ont pour `using` que ces dossiers ;
  - `RaceWorldFactory.CreateRaceWorld` prend `RacingGameCasaEngineGame` (`Worlds/RaceWorldFactory.cs:93`) ;
  - le chemin du contenu vient d'`EngineEnvironment.ProjectPath` (`Worlds/LegacyTrackSceneFactory.cs:1583-1588`), que l'éditeur fixe aussi.
- **Accès au jeu**, toutes les utilisations de `RacingGameCasaEngineGame` dans ces fichiers :
  - `VehicleDynamicsComponent.cs:73,220` : transtypage, puis `RaceSession` ;
  - `DebugFreeCameraComponent.cs:40,142,148` : transtypage, puis `IsActive`, `RaceSession`, `InputComponent` et `Window` ;
  - `ChaseCameraRigComponent.cs:81,154,245` : transtypage et paramètres, puis `RaceSession` et `InputComponent` ;
  - `RaceFlowCoordinatorComponent.cs:35,102` : transtypage et paramètre, puis `RaceSession` et `InputComponent` ;
  - `GameFramework/RaceGamepadVibration.cs` :
    - un champ `RacingGameCasaEngineGame _game` (`:28`) et un constructeur qui en prend un (`:35`) ;
    - un transtypage, puis `RaceVibration` (`:46`) ;
    - `_game.RaceSession` et `_game.InputComponent` (`:108-110`).
  - Il n'est appelé que depuis `RacingGameCasaEngineGame.cs:86`.
  - `RuntimeRaceSession` (`Bootstrap/RuntimeRaceSession.cs:10`) et `RaceGamepadVibration` (`:20`) sont internes, comme les propriétés `RaceSession` et `RaceVibration` de `RacingGameCasaEngineGame` (`Bootstrap/RacingGameCasaEngineGame.cs:121,124`), classe publique (`:25`) ;
  - la session baisse `IsDebugCameraEnabled` en deux endroits : à la liaison d'une course et à sa remise à zéro (`RuntimeRaceSession.cs:58,113`).
- **Noms de types** : aucun des 69 types de ces fichiers n'a le nom court d'un type de CasaEngine, de MGUI, de l'éditeur ou de `RacingGame.Gameplay` (recherche `rg` des déclarations). Le contrôle retrouve bien `Entity` et `World`.
- **Voiture** :
  - `RacingCarPawn : Entity` construit dans son constructeur (`Entities/RacingCarPawn.cs:73-161`) :
    - une hiérarchie d'ancres `RacingCarAnchorComponent` : `PhysicsRoot` > `VisualPivot` > `Body`, `ChaseCamera`, `CockpitCamera`, `AudioEmitter` ;
    - un `StaticModelComponent` `BodyVisual` ;
    - 4 composants ;
  - son état de course a 12 propriétés (`:31-53`). Elles sont lues ou écrites par la session, la caméra de poursuite, la dynamique, le rendu, le lieur, la fabrique, les deux validateurs des voitures et le HUD (`Screens/RaceHudScreen.cs:197-218`).
- **Géométrie générée, toute déterministe** :
  - route : `CreateRoadEntity` (`LegacyTrackSceneFactory.cs:256-303`), sans tangentes ;
  - rails et poteaux : `LegacyTrackSceneFactory.GuardRails.cs:84-227` ;
  - colonnes et segments : `:237-404` ;
  - terrain : `LegacyTrackSceneFactory.Terrain.cs:52-137`.
  Le seul hasard est le décor, tiré par une graine FNV-1a du nom de la piste (`LegacyTrackSceneFactory.cs:782-796`). Les caches statiques de modèles et de textures (`:59-68`) ne sont jamais vidés et sont partagés entre les mondes.
- **Moteur** :
  - un composant peut fabriquer un `StaticModel` en mémoire (`StaticModelMesh.SetData`, avec ou sans tangentes) et le poser sur un `StaticModelComponent` sans asset ; c'est le patron d'`ArrowComponent`. Le moteur ne libère jamais les tampons GPU d'un tel modèle (`StaticModel.cs:96-102`) ;
  - l'éditeur n'enregistre aucun composant dont le type porte `[Browsable(false)]` : racine, composants, enfants (`CasaEngine.EditorServices/EditorEntityJsonSerializer.cs:59,73,316,482-485,596`) ;
  - en mode édition, `InitializeWithWorld` et `Update` des composants s'exécutent, mais pas `BeginPlay` ni la physique (`GameplayExecutionPolicy.cs:31-42`, `Entity.cs:478-509`). `Game.ExecutionPolicy.IsEditorPreview` le signale ;
  - `[SerializedProperty]` accepte `Guid`, les nombres, les booléens, les énumérations et les vecteurs, pas les listes (`SerializedProperties.cs:43-47`) ;
  - l'automatisation de l'éditeur a `--save-project`, mais aucune option pour régler une propriété de composant (`EditorAutomationOptions.cs`).
- **« Audit dans le seuil de T4.1 »**, de T4.2 à T4.6 : `compare_track_audit.py artifacts/editor-project-baseline/s4/track-audit-a <nouvelle série> --threshold 0.5`. Chacune des 21 captures est au plus à 0,5 par canal de la référence `a` de T4.1.
- **Références des audits des voitures** : celles de S0, `artifacts/editor-project-baseline/car-profile-audit.md`, `car-top-speed-audit.md` et `details/`. On les compare sans la ligne `Generated:` ni l'heure murale en tête des lignes de `details/`, comme en T3.5.

### Prérequis, risques et arrêts de S4

- **Prérequis** : T2.1 ✅. Le pointeur `CasaEngine` doit porter la fin de S2 avant T4.1. S4 a besoin de :
  - E1 : `[SerializedProperty]` et son écrivain ;
  - E2 : un type inconnu signalé au lieu d'un plantage ;
  - E5 : la section `Logs:` et le code de sortie.
- **Arrêts** (en plus de ceux de l'enveloppe) :
  - si T4.1 n'obtient pas deux lancements de l'audit dans le seuil, T4.1 passe en ⚠️ Blocked et T4.2 à T4.6 ne commencent pas ;
  - un écart inexpliqué d'empreinte de sommets, de matrice ou de matériau dans `compare_race_scene_exports.py`, de T4.2 à T4.6, met la tâche en ⚠️ Blocked ;
  - si un audit des voitures diffère, la tâche passe en ⚠️ Blocked ;
  - en T4.6, si aucune voie automatique n'enregistre le monde ou ne règle un générateur, la tâche reste en 🧪 avec l'étape manuelle écrite, pas en ✅.
- **Risques** :
  - **Tampons GPU** des maillages générés : un générateur ne libère que les tampons qu'il a créés, jamais les modèles et textures partagés par les caches statiques ;
  - **Générateurs dans l'éditeur** : ils tournent en aperçu, où `Update` s'exécute. Ils ne dépendent pas d'`IRaceGameHost`, et ne font rien tant que leurs réglages ne changent pas ;
  - **Duplication** : une entité génératrice dupliquée dans l'éditeur pourrait copier ses enfants générés. Avant de générer, un générateur retire les enfants générés qu'il trouve ;
  - **Noms courts** : les types ajoutés en T4.3 et T4.5 repassent le contrôle des noms.

### ✅ T4.1 — Audit de piste reproductible, export par maillage dessiné et références de S4

- **Objectif** : rendre A4 mesurable pour S4 à S6 (P9).
- **Fichiers** :
  - `Bootstrap/RuntimeRaceSession.cs` : un drapeau `IsCameraPosedByAutomation`, à côté d'`IsDebugCameraEnabled`, baissé aux mêmes endroits que lui (`:58,113`), avec son accesseur dans `RacingGameCasaEngineGame` ;
  - `Components/DebugFreeCameraComponent.cs` : quand ce drapeau est levé, la caméra libre ne touche ni la caméra ni la souris ;
  - `Bootstrap/TrackMigrationCaptureValidator.cs` : il lève le drapeau en même temps qu'il active la caméra de débogage (`:119`), et le baisse quand il la coupe (`:209`). La caméra de débogage reste donc activée comme aujourd'hui : rig de poursuite arrêté, conduite, chrono et checkpoints bloqués, curseur inchangé. Seule disparaît la réécriture de la pose par la souris ;
  - `Bootstrap/TrackRuntimeSceneExporter.cs` : une section `DrawnMeshes` par piste, qui laisse intacts les champs actuels. Un enregistrement par `StaticModelSubMeshComponent` dessiné, avec :
    - le nom de l'entité et le chemin des composants ;
    - le nom du maillage ;
    - la matrice monde à 6 décimales ;
    - le type du matériau et ses paramètres (couleurs, puissance spéculaire, réflexion, coupure alpha, file, état de rastérisation, ombres) ;
    - l'empreinte SHA-1 des pixels de chaque texture liée ;
    - le nombre de sommets, d'indices et de primitives, et les empreintes SHA-1 des sommets et des indices ;
  - `scripts/compare_race_scene_exports.py` : compare deux exports en ensembles de maillages dessinés, indépendamment des noms d'entités, avec les matrices à 1e-4 près. Il liste les maillages en plus et en moins, et sort avec le code 1 s'il y en a ;
  - `scripts/compare_track_audit.py` : la sonde `compare_audit.py` du scratchpad, versionnée. Elle apparie les captures par nom sans horodatage et mesure l'écart absolu moyen par canal, comme `UiCaptureCompare`. Avec `--threshold`, elle sort avec le code 1 si une capture le dépasse ;
  - références dans `artifacts/editor-project-baseline/s4/` : deux lancements de l'audit (`track-audit-a/`, `-b/`) et deux exports (`scene-a.json`, `-b.json`). Ils sont pris avec ce code et avant toute autre modification de S4.
- **Validation** :
  - deux lancements de l'audit : chacune des 21 captures diffère d'au plus 0,5 par canal (`compare_track_audit.py` entre `a` et `b`). La note donne l'écart de chaque vue. Les 3 vues `chase-road-only` restent à 0,000 ;
  - deux exports : `compare_race_scene_exports.py` ne trouve aucune différence ;
  - les champs d'entité de l'export restent identiques à `scene-t3.5.json`, hors horodatage ;
  - **contrôle négatif** : `Road.png` renommé dans le dossier de sortie, puis remis. L'audit dépasse 0,5 sur les vues de la route, et la comparaison des exports signale les maillages de la route ;
  - `--smoke-frontend` sort avec le code 0, les captures d'interface restent à 0,00 contre `references-acb6ad9`, et les audits des voitures sont identiques à ceux de S0 ;
  - build à 0 erreur.
- **Commit** : `test(racing-casa): make the track audit repeatable and export every drawn mesh`

> Validation (2026-10-08) :
> - **Drapeau** : `IsCameraPosedByAutomation` est levé et baissé avec la caméra de débogage par le validateur, et remis à zéro aux deux endroits de la session. La caméra libre ne touche plus ni la caméra ni la souris quand il est levé.
> - **Le drapeau seul ne suffisait pas** : avec lui seul, deux lancements différaient encore sur 2 vues (4,08 et 39,5). Trois autres causes, toutes dans le validateur, ont été corrigées :
>   1. **image périmée** : la capture encode un PNG pendant une mise à jour, puis la boucle à pas fixe rattrape sans dessiner. 250 ms de temps de jeu peuvent donc passer sans image, et la capture suivante relit la vue précédente (mémoire de session). Le validateur attend en plus 3 dessins entre deux étapes, comptés par `RacingGameCasaEngineGame.DrawCount`, une surcharge de `Draw` qui appelle la base puis compte ;
>   2. **roulis du rig de poursuite** : la vue « look-at » prend l'axe haut de l'orientation du composant, que le rig laisse figée à une valeur qui dépend du nombre de mises à jour (`ChaseCameraRigComponent.cs:234`). Une pose remet l'orientation à l'identité (`PoseCamera`) ;
>   3. **vue de poursuite non stabilisée** : le rig lisse sa pose (6 et 7,5 par seconde). La capture `chase-road-only` attend 2 000 ms, moins que le compte à rebours de 3 s. Le feu est masqué dans cette vue, et les chronos du HUD lisent encore 0:00.00.
>   L'audit prend environ 0,5 s de plus par piste.
> - **Export** :
>   - section `DrawnMeshes`, un enregistrement par sous-maillage dessiné : entité, chemin des composants, maillage, matrice, type et paramètres du matériau, SHA-1 des pixels de chaque texture (mip 0, avec taille et format), drapeaux de dessin et d'ombres, nombres de sommets, d'indices et de primitives, SHA-1 des sommets et des indices ;
>   - `StaticModelMesh` n'expose pas ses tangentes : l'empreinte couvre positions, normales et coordonnées de texture, et `HasTangents` est noté ;
>   - les chemins de composants contiennent des GUID tirés à chaque lancement (`Object <guid>/…`) ; la comparaison les ignore.
> - **Scripts** :
>   - `scripts/compare_race_scene_exports.py`, avec `--renames`, `--tolerance` et `--max-list` ; sa clé comprend aussi les drapeaux et les nombres ;
>   - `scripts/compare_track_audit.py`, avec `--threshold`.
> - **Références** dans `artifacts/editor-project-baseline/s4/` :
>   - `track-audit-a/` et `-b/` : les 21 vues à 0,000 sur chaque canal. 4 lancements de plus, soit 5 paires, sont aussi à 0,000 partout ;
>   - `scene-a.json` et `-b.json` : 1 074, 689 et 2 156 maillages (Advanced, Beginner, Expert), tous appariés, 0 renommé ;
>   - la partie « entités » de `scene-a.json` est identique à `scene-t3.5.json`.
> - **Contrôle négatif** : `Road.png` renommé dans la sortie, puis remis (SHA-1 `e2b98833…`, identique à la source). L'audit dépasse 0,5 sur 18 vues sur 21, de 3 à 55 ; les vues d'ensemble du terrain restent dessous. La comparaison des exports signale les 6 maillages `<piste>.Road`.
> - **Autres contrôles** :
>   - `dotnet build RacingGame.slnx` : 0 erreur ;
>   - `--smoke-frontend` : code 0 ;
>   - `--capture-ui-screens` : 0,00 sur les 10 états ;
>   - audits des voitures identiques à S0, dans `s4/car-audits/`.

### ✅ T4.2 — Code de jeu dans `RacingGame.Gameplay` (P5)

- **Objectif** : le code de jeu que l'éditeur doit voir passe dans la DLL. L'exe garde l'amorçage, les écrans, l'interface, la persistance, les validateurs et l'assemblage des mondes.
- **Fichiers** :
  - **vers `RacingGame.Gameplay`, par `git mv`** :
    - `Components/`, `Entities/`, `GameFramework/`, `Gameplay/` ;
    - `Worlds/LegacyTrackSceneFactory*.cs` et `RaceTrackPhysicsProfile.cs` ;
    - `Bootstrap/RuntimeRaceSession.cs`, `RaceFrontEndCatalog.cs`, `RaceFrontEndState.cs`, `RaceSkySystem.cs`, `RaceBootstrapAssets.cs`, `LegacyGltfModelReader.cs`, `RacingGameImportProfiles.cs`, `RacingGameLegacyMaterialImportProfile.cs` et `RacingGameLegacyMaterialTuning.cs` ;
    - les espaces de noms deviennent `RacingGame.Gameplay.<Dossier>` ;
  - **`RacingGame.Gameplay/IRaceGameHost.cs`**, interface **interne** avec `RaceSession`, `RaceVibration`, `IsActive`, `Window` et `InputComponent` :
    - `RacingGameCasaEngineGame` l'implémente, grâce à `InternalsVisibleTo("RacingGameCasaEngine")` dans la DLL :
      - `RaceSession` et `RaceVibration` par implémentation explicite (`RuntimeRaceSession IRaceGameHost.RaceSession => RaceSession;`, de même pour `RaceVibration`). Les propriétés internes existantes restent inchangées : une propriété interne ne peut pas implémenter implicitement un membre d'interface (CS0737), et la rendre publique sur une classe publique exposerait un type interne (CS0053) ;
      - `IsActive`, `Window` et `InputComponent` implicitement, par les membres publics hérités de `Game` et de `CasaEngineGame` ;
    - chaque utilisation de `RacingGameCasaEngineGame` relevée plus haut passe par `IRaceGameHost` : les transtypages deviennent `World.Game as IRaceGameHost` (ou `is IRaceGameHost host`), et les paramètres des méthodes privées prennent `IRaceGameHost` ;
    - `RaceGamepadVibration` garde un champ `IRaceGameHost _host`, reçu par son constructeur. `RacingGameCasaEngineGame.cs:86` lui passe `this`. Il lit `_host.RaceSession` et `_host.InputComponent` ;
  - **noms d'entités** : `RaceWorldFactory` reste dans l'exe. Les noms et préfixes d'entités que les composants lisent, dont `PlayerStartEntityName` et `CheckpointEntityNamePrefix`, passent dans une classe de la DLL, `RaceWorldNames`. `RaceWorldFactory` les reprend sans changer leurs valeurs ;
  - **visibilité** : les composants, le mode de jeu et le contrôleur restent publics. Le reste, dont `IRaceGameHost`, `RuntimeRaceSession` et `RaceGamepadVibration`, garde sa visibilité, que l'exe voit par `InternalsVisibleTo` ;
  - `RacingGame.Gameplay.csproj` : les références que le code déplacé demande, en particulier `FontStashSharp.MonoGame` si une classe déplacée l'utilise ;
  - **`scripts/check_gameplay_type_names.py`** : la recherche des noms courts de la découverte, versionnée, puisqu'il n'y a pas de projet de tests RGCE. Elle compare les types de `RacingGame.Gameplay` à ceux de CasaEngine, de MGUI et de l'éditeur.
- **Garde-fous** :
  - aucun changement de comportement ;
  - `RaceTrackPhysicsComponent` garde son constructeur jusqu'à T4.5 ;
  - la DLL ne référence pas l'exe.
- **Validation** :
  - `dotnet build RacingGame.slnx` : 0 erreur, sans CS0737 ni CS0053 ;
  - `rg "RacingGameCasaEngineGame|RaceWorldFactory|RacingGameCasaEngine\." RacingGame.Gameplay` ne trouve rien, hors commentaires ;
  - `check_gameplay_type_names.py` : 0 collision ;
  - **A4 contre les références de T4.1** :
    - `compare_race_scene_exports.py` sans différence ;
    - audit dans le seuil de T4.1 ;
    - captures d'interface à 0,00 ;
    - audits des voitures identiques à S0 ;
    - `--smoke-frontend` et `--verify-legacy-import-profile` à 0 ;
  - **éditeur** : le projet s'ouvre (`--diagnostics-out`) avec le code 0, et la section `Logs:` ne contient ni « no IPlugin » ni erreur de chargement de l'assembly de gameplay. Qu'il résout les types des composants est vérifié en T4.6, par un monde qui les contient.
- **Commit** : `refactor(racing-casa): move the game code into RacingGame.Gameplay`

> Validation (2026-10-08) :
> - **Déplacements** : 37 fichiers, par `git mv`. Les espaces de noms deviennent `RacingGame.Gameplay.<Dossier>` :
>   - `Components/` (18), `Entities/` (1) et `GameFramework/` (4) gardent leur nom ;
>   - `Gameplay/` devient `Cars/` (1) ;
>   - les fichiers de `Worlds/` vont dans `Track/` (4) ;
>   - ceux de `Bootstrap/` vont dans `Race/` (5) ou `Import/` (4).
>   Aucun autre fichier de l'exe n'était nécessaire.
> - **Nouveaux fichiers** :
>   - `IRaceGameHost.cs`, interne. `RacingGameCasaEngineGame` implémente `RaceSession` et `RaceVibration` explicitement, et les trois autres membres par héritage ;
>   - `Race/RaceWorldNames.cs`, avec les 11 noms et préfixes d'entités. `RaceWorldFactory` les reprend, aux mêmes valeurs ;
>   - `GlobalUsings.cs`, copie de celui de l'exe ;
>   - `scripts/check_gameplay_type_names.py`.
> - **Projet** : `InternalsVisibleTo("RacingGameCasaEngine")` dans le csproj, sans nouvelle référence. `SharpGLTF` et `StbImageSharp` arrivent par CasaEngine, et l'éditeur les charge déjà. Aucun type n'est devenu public.
> - **Contrôles** :
>   - `dotnet build RacingGame.slnx` : 0 erreur, 0 avertissement ;
>   - `rg` des types de l'exe dans la DLL : seulement 3 commentaires de documentation ;
>   - `check_gameplay_type_names.py` : 94 types de jeu contre 2 794 du moteur et de l'éditeur, 0 collision.
> - **A4 contre T4.1** (`artifacts/editor-project-baseline/s4/t4.2/`) :
>   - export : 1 074, 689 et 2 156 maillages appariés, 0 renommé ;
>   - audit : 21 vues à 0,000 ;
>   - captures d'interface : 0,00 ;
>   - audits des voitures identiques à S0 ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.
> - **Éditeur** : une copie du projet, avec la nouvelle DLL, s'ouvre avec le code 0. La section `Logs:` n'a ni « no IPlugin » ni erreur de chargement. Elle garde seulement les deux avertissements connus du déchargement du contexte de scripts, déjà présents en T2.1.

### ✅ T4.3 — La voiture en entité ordinaire (P6)

- **Objectif** : `RacingCarPawn` disparaît. La voiture est une `Entity` avec composants, construite par une fabrique de la DLL, et son état de course est porté par un composant.
- **Fichiers** :
  - `RacingGame.Gameplay/Components/RaceCarStateComponent.cs` : les 12 propriétés d'état de `RacingCarPawn`, avec la même visibilité ;
  - `RacingGame.Gameplay/Entities/RaceCarEntityFactory.cs` : il construit la même hiérarchie d'ancres, le même `BodyVisual` et les mêmes composants, dans le même ordre ;
  - tous les lecteurs et écrivains relevés passent par `entity.GetComponent<RaceCarStateComponent>()` : session, caméra de poursuite, dynamique, rendu, lieur, fabrique de monde, validateurs des voitures, HUD.
- **Validation** :
  - build à 0 erreur ;
  - `rg "RacingCarPawn"` ne trouve plus rien dans le code ;
  - `check_gameplay_type_names.py` : 0 collision ;
  - A4 contre les références de T4.1, dont les audits des voitures identiques à S0. L'export peut renommer les composants de la voiture, mais ses maillages, leurs matrices et leurs matériaux restent identiques.
- **Commit** : `refactor(racing-casa): build the player car as an entity with components`

> Validation (2026-10-08) :
> - **Code** :
>   - `RacingCarPawn` est supprimé ;
>   - `RaceCarStateComponent` porte les 12 propriétés d'état, avec les mêmes types, valeurs par défaut et visibilités, et les copie dans `Clone()` ;
>   - `RaceCarEntityFactory.Create()` construit la même hiérarchie d'ancres, le même `BodyVisual` et les mêmes composants, dans le même ordre, plus `RaceCarStateComponent` ;
>   - les autres membres du pawn deviennent des méthodes d'extension sur `Entity`, dans `RaceCarEntityExtensions` : recherches d'ancres, matrices et positions du corps, avant visuel et de mouvement. Ce fichier n'était pas prévu par le plan ; il garde les appels lisibles ;
>   - le pawn ne surchargeait rien d'`Entity` ;
>   - la session expose `PlayerCar` (`Entity`) et `PlayerCarState`, au lieu de `PlayerPawn`. La dynamique met en cache le composant d'état et le recherche à nouveau si son propriétaire change ;
>   - le nom par défaut passe de « RacingCarPawn » à « RaceCar ». La fabrique du monde le remplaçait déjà par `PlayerCar` : l'export ne change pas.
> - **Contrôles** :
>   - `dotnet build RacingGame.slnx` : 0 erreur, 0 avertissement ;
>   - `rg RacingCarPawn` ne trouve plus rien ;
>   - `check_gameplay_type_names.py` : 96 types, 0 collision.
> - **A4 contre T4.1** (`artifacts/editor-project-baseline/s4/t4.3/`) :
>   - export : 1 074, 689 et 2 156 maillages appariés, 0 renommé, voiture comprise ;
>   - audit : 21 vues dans le seuil ;
>   - captures d'interface : 0,00, `race-hud` compris ;
>   - audits des voitures identiques à S0 ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.
> - **Éditeur** : le projet s'ouvre avec le code 0. La section `Logs:` ne contient que les deux avertissements connus.

### ✅ T4.4 — Placements de piste séparés de la génération des maillages

- **Objectif** : un calcul pur des placements, réutilisé par les générateurs (T4.5) et par l'export des mondes (S6).
- **Fichiers** :
  - `RacingGame.Gameplay/Track/RaceTrackLayout.cs`, avec les types qu'il retourne. Il calcule, à partir du `.Track` et de la carte de hauteurs :
    - la spline de la route, ses orientations, ses largeurs et ses coordonnées de texture ;
    - les points des rails et les placements des poteaux ;
    - les placements des colonnes et des segments ;
    - les placements du décor, avec la même graine et l'expansion des CombiModel ;
    - les checkpoints, le départ et le profil physique ;
  - `LegacyTrackSceneFactory*` ne garde que la construction des maillages, des matériaux et des entités à partir de ces placements.
- **Garde-fous** : même ordre de calcul et mêmes constantes ; les caches gardent leur portée.
- **Validation** : build à 0 erreur ; A4 contre les références de T4.1, avec un export par maillage identique jusqu'aux noms d'entités.
- **Commit** : `refactor(racing-casa): compute track placements apart from mesh building`

> Validation (2026-10-08) :
> - **Code**, dans `RacingGame.Gameplay/Track/`, tout en interne :
>   - `RaceTrackLayout.Compute(trackName, track, heights, loadCombiModel, getModelSize)` reprend tel quel le code du calcul de la fabrique, dans le même ordre et avec les mêmes constantes. Il rend :
>     - la spline de la route ;
>     - les points des rails, les poteaux, les colonnes et leurs segments ;
>     - le décor ;
>     - les checkpoints, le départ et le profil physique ;
>   - `RaceTrackLayoutTypes.cs` : les points et les placements ;
>   - `RaceTrackSpace.cs` : les conversions de l'espace hérité ;
>   - `LegacyTerrainHeightSampler.cs` : sorti de la fabrique ;
>   - `LegacyTrackSceneFactory*` ne garde que le chargement, les caches, les matériaux, la construction des maillages et des entités. La fabrique perd 1 333 lignes.
> - **Résultats identiques au bit près** :
>   - la classe mutable du point de spline est gardée, car un `Right.Normalize()` sur une copie temporaire ne normalise rien aujourd'hui, et des structures immuables auraient changé les flottants ;
>   - la dépendance à la taille des modèles passe par un délégué (`getModelSize`, nul quand le modèle manque), ce qui garde le calcul sans GPU pour S6.
>   Seul l'ordre de remplissage des caches change : les modèles du décor se chargent avant les textures de la piste.
> - **Pureté** : `rg "GraphicsDevice|Texture2D|StaticModel|\bEntity\b|\bWorld\b|AssetContentManager"` ne trouve rien dans les 4 nouveaux fichiers.
> - **Contrôles** :
>   - `dotnet build RacingGame.slnx` : 0 erreur, 0 avertissement ;
>   - `check_gameplay_type_names.py` : 103 types, 0 collision.
> - **A4 contre T4.1** (`artifacts/editor-project-baseline/s4/t4.4/`) :
>   - export : 0 renommé, 0 non apparié (1 074, 689, 2 156) ;
>   - la partie « entités » est identique à `scene-a.json`, aux GUID près ;
>   - audit : 21 vues à 0,000 ;
>   - audits des voitures identiques à S0 ;
>   - captures d'interface : 0,00 ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.

### ⏳ T4.5 — Composants générateurs (D5)

- **Objectif** : la route, les rails et leurs poteaux, les colonnes et leurs segments, le terrain et la physique de piste se reconstruisent à partir des assets de données, dans le jeu comme dans l'éditeur.
- **Fichiers**, dans `RacingGame.Gameplay/Components/` :
  - **générateurs**, des `SceneComponent` posés en racine de leur entité :
    - `RaceTerrainGeneratorComponent` ;
    - `RaceRoadGeneratorComponent` ;
    - `RaceGuardRailGeneratorComponent`, pour le côté gauche ou droit, poteaux compris ;
    - `RaceColumnGeneratorComponent`, pour les colonnes et leurs segments ;
  - chacun a des `[SerializedProperty]` :
    - `track_asset_id` et `heightmap_asset_id`, ids du catalogue ;
    - les réglages exposés, dont les constantes d'espacement et d'échelle de la tranche (écart des poteaux 15, distance des colonnes 33…), avec ces valeurs par défaut ;
  - **fonctionnement** :
    - à `InitializeWithWorld`, chacun calcule ses placements par `RaceTrackLayout` et construit ses maillages et ses matériaux comme aujourd'hui ;
    - il les ajoute en enfants d'un type `RaceGeneratedModelComponent : StaticModelComponent` marqué `[Browsable(false)]`, que l'éditeur n'enregistre pas. Il retire d'abord les enfants générés qu'il trouve ;
    - à chaque `Update`, une signature des réglages relance la génération si elle a changé ;
    - à la régénération et au détachement, il libère les tampons GPU qu'il a créés, jamais les modèles des caches ;
  - **`RaceTrackPhysicsComponent`** devient public, avec un constructeur public sans paramètre et `track_asset_id`, `heightmap_asset_id`. Le profil est calculé à `InitializeWithWorld` ;
  - **assemblage** : `RaceWorldFactory` et `LegacyTrackSceneFactory` assemblent la course avec ces composants. Les noms d'entités des générateurs sont gardés : `Track.Ground.*`, `Track.Road.*`, `Track.GuardRail.Left|Right.*`, `Track.Columns.*`. Les poteaux et les segments deviennent les enfants générés de leurs rails et colonnes.
- **Validation** :
  - build à 0 erreur ;
  - `check_gameplay_type_names.py` : 0 collision ;
  - **A4 contre les références de T4.1** :
    - `compare_race_scene_exports.py` : mêmes maillages dessinés, mêmes matrices à 1e-4 près, mêmes matériaux et mêmes empreintes. Seuls les noms d'entités des poteaux et des segments changent, et la note en donne le décompte ;
    - audit dans le seuil de T4.1 ;
    - audits des voitures identiques à S0, ce qui valide la physique de piste ;
    - captures d'interface à 0,00 ;
    - `--smoke-frontend` à 0.
- **Commit** : `feat(racing-casa): rebuild the track geometry with generator components`

### ⏳ T4.6 — Monde de test des générateurs dans l'éditeur

- **Objectif** : les composants s'ouvrent, s'affichent, se règlent et s'enregistrent dans l'éditeur.
- **Fichiers** :
  - `Content/Worlds/Test/TrackBeginnerGenerators.world`, écrit puis enregistré par l'éditeur. Il contient, sur la piste Beginner :
    - une lumière ;
    - une entité par générateur ;
    - une entité portant `RaceTrackPhysicsComponent`, avec ses deux ids ;
  - le catalogue : une entrée `World.Test.TrackBeginnerGenerators`. `generate_rgce_content_assets.py` catalogue les `.world` de `Worlds/` qu'aucune entrée ne nomme ;
  - `RacingGameCasaEngine/Assets/README.md`.
- **Validation** :
  - **ouverture** : `--open-asset Worlds/Test/TrackBeginnerGenerators.world` sort avec le code 0, et sa section `Logs:` n'a ni composant non résolu (E2) ni erreur de chargement. La capture, lue, montre le terrain, la route, les rails et les colonnes ;
  - **aller-retour d'enregistrement**, sur une copie, par `--save-project` :
    - un premier enregistrement, puis un second qui donne exactement les mêmes octets ;
    - aucune clé de composant perdue par rapport au fichier d'origine (comparaison JSON). Le nœud de `RaceTrackPhysicsComponent` garde `track_asset_id` et `heightmap_asset_id` ;
    - aucun composant généré enregistré ;
  - **réglage** : une copie du `.world` reçoit un autre écart des poteaux. Elle est ouverte, capturée, puis enregistrée ; la capture montre l'effet, et le fichier enregistré garde la valeur ;
  - le monde de course du jeu reste identique : A4 contre T4.1.
- **Commit** : `feat(racing-casa): open the track generators in the editor`

### ⏳ T4.7 — ADR, documentation et clôture de S4

- **Fichiers** :
  - `docs/decisions/0017-racinggame-gameplay-holds-the-game-code.md` : la DLL de jeu, `IRaceGameHost`, la voiture en entité, les générateurs et leurs enfants transitoires. Elle complète l'ADR-0015 sans la remplacer ;
  - l'index des ADR ;
  - `RacingGameCasaEngine/Assets/README.md` ;
  - ce plan.
- **Validation** : vérificateur indépendant de S4, CONFIRMED.
- **Commit** : `docs(adr): record the game code and track generators in RacingGame.Gameplay`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | **Levé (T1.2).** `--play-smoke` recompile le csproj de gameplay quand ses sources sont plus récentes que la DLL chargée, puis rapporte la DLL de jeu chargée. | T1.2 |
| O2 | **Levé (T1.2).** `RacingGame.Gameplay/CasaEngine.EnginePath.props`, propre à la machine, est ignoré par git. | T1.2 |
| O3 | Le sous-module contient un plan RGCE égaré, `CasaEngine/ai-agent/tasks/racinggame-car-profile-parity-plan.md`. On le laisse tel quel, la branche moteur le portera. | S2 |
| O4 | PNG d'interface hérités non portés : `HelpScreenWindows`, `HelpScreenXbox360`, `OptionsScreenXbox360`, `MouseCursor`. Faut-il compter aussi la copie brute de `GameFont.png` ? Elle est proposée, D4 voulant « tout porter ». | S3 |
| O5 | Les cartes de normales héritées gardent la disposition `agb`, et le jeu actuel les lit telles quelles. Le chantier conserve exactement ces pixels, sans réencodage. | S3, S5 |
| O6 | **Levé (T0.2).** Le panneau Logs de l'éditeur restait vide sur les captures d'automatisation. Sur décision de l'auteur, il a été corrigé (MGUI `4e94d7e8`, CasaEngine `c0f3343`), et l'option `--logs-filter` (`4ce5ea0`) met l'avertissement à l'écran. La sonde `pluginprobe`, d'abord proposée, n'a pas été retenue. | T0.1, T1.2 |

## Hors périmètre

- **Play du parcours complet du jeu dans l'éditeur** : enchaînement des mondes, écrans XAML dans la vue, hub `RacingGameCasaEngineGame`. Le jeu se teste avec l'exe (D1).
- **Éditeur de tracé** : édition des points de contrôle d'une piste dans l'éditeur.
- **Le reste de l'ancien jeu** : shaders `.fx`, banques XACT et `Cube.X` ; la cubemap `SkyCubeMap.dds` et le pipeline de ciel du jeu restent.
- **Données de jeu tenues en code** : profils et couleurs des voitures, liste des pistes, règles de course, entrées, scores. Leur passage en assets de données serait un chantier suivant, à confirmer par l'auteur.
- **Lecture des nouveaux sons** par le jeu (moteur, freins, chocs, checkpoints, musiques).
- **Rendu** : brouillard, post-effets, glow, PBR ; tout ce qui changerait le rendu d'aujourd'hui.
- **Dépôt** : push, fusion, pull request.
