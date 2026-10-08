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

### S2 — Fondations moteur et éditeur (CasaEngine, à détailler)

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

### S3 — Contenu hérité porté (à détailler)

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

### S4 — Code de jeu dans la DLL et composants générateurs (à détailler)

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

### ⚠️ T0.1 — Plan, pointeur du moteur et références de parité

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
     - **contrôle négatif du plugin** : l'éditeur ouvert sur le projet avec les arguments du script plus `--activate-panel output`, qui affiche le panneau « Logs ». La capture va dans `artifacts/editor-project-baseline/editor-logs.png`. Elle doit montrer l'avertissement `Gameplay assembly 'RacingGameCasaEngine.UI.dll' contains no IPlugin implementation.` (`AssemblyManager.cs:29`), puisque la DLL actuelle n'a pas de plugin ; on le lit sur l'image. Si le message n'est pas lisible dans le panneau, T0.1 passe en ⚠️ : T1.2 n'aurait pas de contrôle réfutable ;
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
> - ⚠️ **Contrôle négatif du plugin impossible par le panneau Logs.** Avec `--activate-panel output`, le panneau « Logs » est bien actif (« Activate panel: output » dans les diagnostics), mais sa liste reste vide sur la capture, à 3 s comme à 8 s. Pourtant `LogsPanel.RefreshList` y recopie tout `LoggerEditor.Entries`, ce qui ressemble à un défaut d'affichage de l'éditeur, à noter pour S2. L'avertissement n'est donc pas lisible, et T1.2 n'a pas de contrôle réfutable sous la forme prévue (O6).

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

### ⏳ T1.1 — Renommer `RacingGameCasaEngine.UI` en `RacingGame.Gameplay`

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

### ⏳ T1.2 — `Plugin` et recompilation depuis l'éditeur

- **Objectif** : `RacingGame.Gameplay` devient la DLL de gameplay au sens de CasaEngine (D1). L'éditeur trouve son `IPlugin` et sait la recompiler, comme RPGDemo.
- **Fichiers** :
  - `RacingGame.Gameplay/RacingGame.Gameplay.csproj`, qui reçoit une référence de projet à `CasaEngine/CasaEngine/CasaEngine.csproj` ;
  - `RacingGame.Gameplay/Plugin.cs` : `public sealed class Plugin : IPlugin`, `Initialize()` vide et rejouable, comme `Projects/CasaEngine.RPGDemo/Plugin.cs` ;
  - `RacingGameCasaEngine/Content/RacingGameCasaEngine.json` : `GameplayCsprojName = "../../RacingGame.Gameplay/RacingGame.Gameplay.csproj"` (chemin relatif au dossier du projet, comme RPGDemo) ;
  - `.gitignore`, si l'éditeur écrit `CasaEngine.EnginePath.props` à côté du csproj (O2).
- **Étapes** :
  1. Ajouter la référence et le `Plugin`, compiler.
  2. Déclarer `GameplayCsprojName`.
  3. Ouvrir le projet dans l'éditeur recompilé avec la commande du contrôle négatif de T0.1 (`--activate-panel output`), capture dans `artifacts/editor-project-baseline/editor-logs-t1.2.png`. Contrôler aussi le fichier écrit à côté du csproj (O2).
  4. Faire une recompilation depuis l'éditeur (`File > Build Scripts` ou Play). Si l'automatisation ne peut pas la déclencher, la vérification devient manuelle (O1).
- **Validation** :
  - `dotnet build RacingGame.slnx` sans erreur ;
  - `--smoke-frontend` sort avec le code 0 ;
  - **contrôle du plugin**, réfutable : sur `editor-logs-t1.2.png`, ni « Gameplay assembly not found » ni « contains no IPlugin implementation » (`AssemblyManager.cs:20,29`), alors que T0.1 montrait le second. Sans `Plugin.cs`, l'avertissement reviendrait ;
  - les 9 écrans de l'éditeur, capturés dans `editor-screens-t1.2/`, restent dans le bruit de T0.1 face à `editor-screens/` ;
  - la recompilation depuis l'éditeur réussit, ou 🧪 avec la manipulation notée pour l'auteur.
- **Commit** : `feat(racing-casa): make RacingGame.Gameplay the editor's gameplay plugin`

### ⏳ T1.3 — ADR et documentation de la DLL de gameplay

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

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | L'automatisation de l'éditeur sait-elle déclencher `Build Scripts` ? La liste de `EditorAutomationOptions.cs` n'a pas d'option de ce nom ; sinon, vérification manuelle par l'auteur. | T1.2 |
| O2 | L'éditeur écrit `CasaEngine.EnginePath.props` à côté du csproj de gameplay à l'ouverture du projet (`EditorProjectAuthoringService.cs:22-57`). Il faut confirmer le contenu et le chemin, puis l'ignorer dans git s'il est propre à la machine. | T1.2 |
| O3 | Le sous-module contient un plan RGCE égaré, `CasaEngine/ai-agent/tasks/racinggame-car-profile-parity-plan.md`. On le laisse tel quel, la branche moteur le portera. | S2 |
| O4 | PNG d'interface hérités non portés : `HelpScreenWindows`, `HelpScreenXbox360`, `OptionsScreenXbox360`, `MouseCursor`. Faut-il compter aussi la copie brute de `GameFont.png` ? Elle est proposée, D4 voulant « tout porter ». | S3 |
| O5 | Les cartes de normales héritées gardent la disposition `agb`, et le jeu actuel les lit telles quelles. Le chantier conserve exactement ces pixels, sans réencodage. | S3, S5 |
| O6 | **Bloquant T0.1 et T1.2.** Décision de l'auteur : corriger d'abord le panneau (T0.2, fait). Reste : l'avertissement n'est pas visible sur la capture, car le panneau n'affiche que ses dernières lignes. Le panneau Logs de l'éditeur restait vide sur les captures d'automatisation. Proposition : remplacer ce contrôle par la sonde `pluginprobe` du scratchpad. Elle rejoue le chargement de l'éditeur à l'ouverture du projet : `ProjectSettingsHelper.Load`, avec le chargeur à copie fantôme de l'éditeur `EditorScriptAssemblyService.LoadShadowCopy` (`GameEditor.cs:326`). Elle affiche ce que `AssemblyManager` journalise et sort avec le code 1 sans `IPlugin`. Sur le projet actuel, elle affiche « Gameplay assembly 'RacingGameCasaEngine.UI.dll' contains no IPlugin implementation. » et sort avec le code 1 : c'est le contrôle négatif. Le panneau vide serait ajouté aux manques de S2. | T0.1, T1.2, S2 |

## Hors périmètre

- **Play du parcours complet du jeu dans l'éditeur** : enchaînement des mondes, écrans XAML dans la vue, hub `RacingGameCasaEngineGame`. Le jeu se teste avec l'exe (D1).
- **Éditeur de tracé** : édition des points de contrôle d'une piste dans l'éditeur.
- **Le reste de l'ancien jeu** : shaders `.fx`, banques XACT et `Cube.X` ; la cubemap `SkyCubeMap.dds` et le pipeline de ciel du jeu restent.
- **Données de jeu tenues en code** : profils et couleurs des voitures, liste des pistes, règles de course, entrées, scores. Leur passage en assets de données serait un chantier suivant, à confirmer par l'auteur.
- **Lecture des nouveaux sons** par le jeu (moteur, freins, chocs, checkpoints, musiques).
- **Rendu** : brouillard, post-effets, glow, PBR ; tout ce qui changerait le rendu d'aujourd'hui.
- **Dépôt** : push, fusion, pull request.
