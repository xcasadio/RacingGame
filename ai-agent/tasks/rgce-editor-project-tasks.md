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
| D9 | Ajoutée le 2026-10-08, réponse de l'auteur sur O7 : le défaut de lecture des éléments `WidthHelper`, `RoadHelper` et `CombiObject` est corrigé **avant S6**. Le décor complet (palmiers, lanternes, tunnels, aides de largeur, contenu des CombiModel) entre dans le jeu, avec de nouvelles références de rendu, puis S6 le fige dans les mondes. Le plan à part `ai-agent/tasks/rgce-track-helpers-tasks.md` s'exécute **après la clôture de S5 et avant S6**. Il porte trois décisions de l'auteur (2026-10-08) : la comparaison avec l'original par `scripts/TrackPlacementExporter`, plus un contrôle visuel de l'auteur ; une suite aléatoire propre aux palmiers ; et l'ordre d'exécution. S6 part de ses références « après ». |
| D14 | Ajoutée le 2026-11-21 (réponse de l'auteur, « Ouverture + aller-retour »), pour S7. Les décisions D10 à D13 sont celles du plan `rgce-worlds-as-assets-tasks.md` ; la numérotation continue ici. **Jusqu'où va le contrôle de l'éditeur** : chacun des assets du catalogue s'ouvre dans l'éditeur sans erreur ; l'enregistrement sans perte est prouvé sur tous les assets des types éditables par les écrivains du moteur, hors de l'éditeur, et dans l'éditeur piloté sur un échantillon des types qu'il sait piloter. Aucun changement du moteur dans cette tranche. |
| D15 | Ajoutée le 2026-11-21 (« Mesurer, puis décider »). **Échecs** : la tranche écrit les contrôles et les lance sans rien corriger. La liste des échecs, classés par cause, revient à l'auteur avec une proposition ; les corrections font l'objet d'un plan qu'il approuve. |
| D16 | Ajoutée le 2026-11-21. **Mode d'exécution de S7** : `AUTO` une fois le détail approuvé ; ni push, ni avance de `master` ou de `remaster` sans demande de l'auteur. |

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
| P22 | **S7, branche.** Le jeu travaille sur `chantier/rgce-editor-closure`, créée depuis `remaster` (`5e5d17a` = `origin/remaster`). Le moteur ne change pas : le pointeur reste `01cdf483f` (= `main` = `origin/main` du moteur), qui est donc « le commit final du chantier moteur » du résultat de S7. P22 à P29 suivent P11 à P21 du plan `rgce-worlds-as-assets-tasks.md`. |
| P23 | **S7, critère d'ouverture (A2).** Un lancement de l'éditeur par asset, sur une copie du contenu hors du dépôt : `CasaEngine.Editor.exe --project <copie>/RacingGameCasaEngine.json --open-asset <file_name de l'entrée> --diagnostics-out <fichier> --automation-timeout <secondes>`. Un asset **passe** quand le code de sortie est 0 (`EditorAutomationOutcome` : 0 réussi, 1 échec, 2 délai dépassé), que le fichier de diagnostic porte la ligne `[Automation] Opened asset='<file_name>'` et qu'il ne porte aucune ligne `[Error]`. Un `[Warning]` qui nomme le fichier de l'asset (son chemin ou son nom de fichier) fait **échouer** l'asset : c'est ainsi qu'un aperçu signale une image, un cube, une police ou un modèle qu'il n'arrive pas à charger (`CasaEngine/CasaEngine.Editor/Controls/AssetPreviewPanel.cs:297`, `:332`, `:372`, `:544`, `:589`). D'autres défauts ne laissent **aucune trace dans le diagnostic**, seulement dans le texte d'état du panneau : un `.wav` qui n'est pas un RIFF/WAVE ou n'a pas de bloc `fmt`, un `.fnt` qui n'est pas un descripteur BMFont en texte (un BMFont binaire compris), un `.dds` qui n'est pas un DDS (`CasaEngine/CasaEngine.EditorServices/AssetPreview/AssetPreviewReader.cs:341`, `:395`, `:434`, `:524` ; `AssetPreviewPanel.cs:244-247`), et un glTF sans maillage (`AssetPreviewPanel.cs:593-597`). P30 couvre les défauts d'en-tête ; le glTF sans maillage n'est vu par aucun contrôle de cette tranche, et T7.5 le dit à l'auteur. Les autres `[Warning]` sont relevés par texte distinct et classés un par un en T7.5, « admis » avec sa raison ou « échec » ; aucun ne reste sans classement. Un asset qui ne passe pas est un **échec mesuré**, noté avec sa cause, jamais corrigé ici (D15). |
| P24 | **S7, niveau d'ouverture par type, sur le catalogue d'aujourd'hui** (P1 mis à jour ; 622 entrées relevées le 2026-11-21). Document d'édition : `.world` (7), `.entity` (1), `.staticmodel` (56), `.material` (99), `.texture` (122), `.sound` (28), `.uiscreen` (9), `.sprite` (50), soit 372. Aperçu en lecture seule (`CasaEngine/docs/editor/asset-documents.md`) : images `.png` (149) et `.dds` (2), `.wav` (28), `.fnt` (1), modèles source `.gltf` (56), données `.track` (3), `.combimodel` (10), `.data` (1), soit 250. Le contrôle note pour chaque asset la sorte de panneau ouvert, quand le diagnostic la donne. |
| P25 | **S7, aller-retour hors de l'éditeur.** Un mode de l'outil, `scripts/RgceAssetCooker --roundtrip`, charge chacun des 372 assets éditables par le chargeur du moteur pour son type, sans périphérique graphique, l'écrit par l'écrivain public de l'éditeur, `EditorAssetWriterService.SaveAsset(fileName, asset)` (`CasaEngine/CasaEngine.EditorServices/EditorAssetWriterService.cs:48-71` ; il couvre ces huit types et l'outil l'emploie déjà, `scripts/RgceAssetCooker/Cooker.cs:101`), le recharge et le réécrit. L'outil passe à l'écrivain un chemin absolu sous `artifacts/editor-closure/roundtrip/` et pose `EngineEnvironment.ProjectPath` sur la copie du contenu ; il n'écrit jamais dans `Content` et n'appelle aucun type interne du moteur. **Pour un `.world`, la méthode est celle que l'outil emploie déjà** (`scripts/RgceAssetCooker/WorldVerifier.cs:112-113`, `:200-211` ; `WorldWriter.cs:269`), parce qu'un monde ne crée ses entités que dans un jeu lancé (`CasaEngine/CasaEngine/Framework/Scene/World/World.cs:1040-1051`, `EntityReference.cs:19-68`) et que `SaveAsset` sur un monde chargé hors du jeu lèverait une exception : `World.Load` sur une copie du JSON dont `entity_references` est vidé ; chaque nœud d'entité en ligne (`asset_id` vide : les 7 mondes n'en ont pas d'autre sorte) chargé par `Entity.Load` dans une `Entity` neuve ; écriture par `EditorWorldWriter.SaveWorld(World, IReadOnlyList<Entity>)` (`CasaEngine/CasaEngine.EditorServices/EditorWorldWriter.cs:24-32`), `world.FileName` posé sur le chemin absolu de sortie. Une référence dont l'`asset_id` n'est pas vide est un échec mesuré (« référence d'asset non prise en charge »). `SaveAsset` n'écrit jamais un `World` ; les sept autres types, `.entity` compris, gardent leur chargeur et `SaveAsset`. **Les composants du jeu doivent être chargés sous leur vrai type** : l'outil charge l'assemblage `RacingGame.Gameplay` avant la première lecture et enregistre un journal auprès de `Logs` (`Logs.AddLogger`), car un type de composant introuvable devient sans bruit un `UnresolvedComponent` qui réécrit son nœud JSON tel quel (`CasaEngine/CasaEngine/Framework/Assets/ElementFactory.cs:315-331`, `:376-398`), ce qui ferait passer l'aller-retour à vide ; pour chaque monde (sur les entités chargées une à une et leurs arbres de composants) et pour la voiture, tout `UnresolvedComponent`, et toute erreur reçue par ce journal, est un échec, et le rapport donne le nombre de composants chargés sous leur vrai type. Deux contrôles de plus par asset : la seconde écriture a les octets de la première ; aucune clé du fichier source ne manque dans le fichier écrit et aucune valeur ne diffère (nombres à 1e-6 près, tableaux dans le même ordre). Les clés que l'écrivain ajoute sont listées par type, elles ne sont pas des pertes. Un type que l'outil ne sait pas charger hors du jeu est un échec mesuré, pas un contournement. |
| P26 | **S7, aller-retour dans l'éditeur piloté, sur un échantillon.** Un asset par type que l'éditeur sait piloter. Par les commandes à distance (`casa-editor`, éditeur lancé avec `--remote`, script `artifacts/entity-asset-remote/scripts/editorctl.py`) : un matériau, un son, un écran, l'entité `PlayerCar.entity`, un monde ; modifier une valeur, enregistrer, relire le fichier (il ne diffère de la source que par cette valeur, écarts admis d'O2 et d'O4 du plan `rgce-worlds-as-assets-tasks.md` mis à part), remettre la valeur, enregistrer, comparer à la source. Par les options d'automatisation, qui restaurent d'elles-mêmes le fichier en fin de lancement : une texture (`--set-texture-sampler`) et un modèle (`--set-static-model-material`). **Pour ces deux types le contrôle est plus étroit** : il montre que la modification est appliquée, enregistrée et relue du fichier enregistré, puis que le fichier est restauré ; il ne compare pas le fichier enregistré à la source, que l'éditeur restaure avant qu'on puisse le lire. Leur enregistrement sans perte ne repose donc que sur P25, comme pour les sprites. Critère : le lancement sort en 0, le diagnostic porte la ligne `[Automation] Updated texture sampler '<clé>'='<valeur>' (saved file: …)` ou `[Automation] Updated static model material '<maillage>'='<id>' (saved file: …)` (`GameEditor.cs:9943-9944`, `:10024`), et les empreintes de la copie après le lancement sont celles d'avant. La restauration a lieu après l'écriture du diagnostic : elle ne s'y lit pas, seules les empreintes la montrent. Les sprites n'ont ni commande ni option : ils ne sont couverts que par P25, et la note le dit. |
| P27 | **S7, références.** A4 se juge contre `artifacts/worlds-as-assets/m1/ref/` (76 images), `m1/export-a` et les audits de voitures de M1, par la « Validation commune (parité) » du plan `rgce-worlds-as-assets-tasks.md` : les références de S0 ont été remplacées par des décisions prises depuis (ADR-0027 à 0041), et cette référence tient sur le moteur d'aujourd'hui (plan `origin-main-integration-2-tasks.md` du moteur, T3.1). A3 s'appuie sur M5 et M9 de ce même plan, avec leurs deux écarts admis par l'auteur (clés de caméra, O4 ; transform de `BodyVisual`, O2), et P26 en rejoue un monde et la voiture. |
| P28 | **S7, copie et verrou.** La copie du contenu (`RacingGameCasaEngine/Content`, 258 Mo, avec `RacingGame.Gameplay.dll` produite par le build) va dans `D:/development/rgce-s7/`, hors du dépôt ; elle est créée par T7.1 et **supprimée à la clôture** (l'approbation de ce détail le couvre). Une autre session travaille sur les écarts de rendu dans son propre worktree : avant chaque lancement de l'éditeur ou du jeu, la session crée `D:/development/rgce-run.lock` (son nom, l'heure), attend s'il existe déjà, et le supprime à la fin du lancement, erreur comprise. |
| P29 | **S7, documents.** Aucune nouvelle ADR n'est prévue : celles qui remplacent l'ADR-0002 existent (ADR-0023 et ADR-0024, sa ligne d'état les nomme) et D14 à D16 règlent un contrôle, pas l'architecture. Si le bilan de T7.5 amène une décision d'architecture, elle sera prise avec l'auteur et aura son ADR dans le plan de corrections. |
| P30 | **S7, en-têtes des aperçus.** Le mode `--roundtrip` de l'outil lit aussi l'en-tête de chacun des 250 assets d'aperçu par `AssetPreviewReader.Read(chemin)` (public, `CasaEngine/CasaEngine.EditorServices/AssetPreview/AssetPreviewReader.cs:29`), qui est ce que l'aperçu de l'éditeur lit : un `Problem` non nul est un échec mesuré, noté avec son texte. Ce contrôle comble ce que le diagnostic de l'éditeur ne dit pas (P23). Il ne voit pas un glTF dont l'en-tête est sain mais qui ne donne aucun maillage : limite notée en T7.5. |

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
| **S6** | Mondes en assets (`.world`, `PlayerCar.entity`) chargés par le jeu | S4, S5, plan O7 (`rgce-track-helpers-tasks.md`) | RacingGame |
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

### S5 — Modèles, matériaux et textures en assets (détaillée en Phase 5)

- **Résultat** : les 56 modèles sont des `.staticModel` avec leurs `.material` et `.texture` (P8), et le jeu les charge par le catalogue. Les règles de matériaux qui dépendaient des noms de fichiers ne s'appliquent plus à l'exécution : leurs valeurs sont dans les assets.
- **Acceptation** :
  - export par maillage identique (mêmes valeurs de matériaux) ;
  - A4 identique aux références ;
  - chaque `.staticModel`, `.material` et `.texture` s'ouvre dans l'éditeur.

### S6 — Mondes en assets (détaillée en Phase 6)

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
- **Références de parité** : elles partent de `artifacts/o7-track-helpers/after/`, prises à la fin du plan O7 (décor corrigé, tunnels compris).

### S7 — Contrôle de l'éditeur et clôture (détaillée en Phase 7 le 2026-11-21)

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
  - `docs/decisions/0023-racinggame-gameplay-is-the-editor-gameplay-assembly.md` (skill `adr`), qui remplace la partie « view models » de l'ADR-0002 ;
  - l'ADR-0002, dont le statut devient « Superseded in part by ADR-0023 » sans autre changement ;
  - `docs/decisions/README.md` ;
  - `RacingGameCasaEngine/Assets/README.md:43` et les autres docs citant l'ancien nom (`rg`).
- **Validation** :
  - `rg -l "RacingGameCasaEngine\.UI" docs RacingGameCasaEngine/Assets` ne trouve plus que l'ADR-0002 historique et la nouvelle ADR-0023, qui cite l'ancien nom. La portée exclut le code, vérifié en T1.1 ; les plans de `ai-agent/` sont historiques ;
  - vérificateur indépendant de S1 (renommage, plugin, parité des captures, écrans dans l'éditeur) : CONFIRMED.
- **Commit** : `docs(adr): record RacingGame.Gameplay as the editor's gameplay assembly`

> Validation (2026-10-08) :
> - **ADR-0023** (`Accepted`) consigne D1 et D7 :
>   - renommage, et espace de noms propre à l'exe inchangé ;
>   - `Plugin` et `GameplayCsprojName` ;
>   - chargement unique dans le jeu (P4) ;
>   - `.props` local à la machine ;
>   - conséquences : copie fantôme de la DLL seule, dossiers jamais nettoyés, `Initialize` rejouable, noms de types uniques.
> - **ADR-0002** : statut « Accepted; superseded in part by ADR-0023 (view-model library, `GameplayCsprojName`) », sans autre changement. L'index est à jour.
> - `RacingGameCasaEngine/Assets/README.md` : DLL de gameplay, recompilation avant Play, `.props`, dossiers `script-build`.
> - `rg -l "RacingGameCasaEngine\.UI" docs RacingGameCasaEngine/Assets` ne trouve que l'ADR-0002 et l'ADR-0023.
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
  - `docs/decisions/0024-rgce-content-holds-every-game-file.md`. Il remplace la règle « le contenu lié non catalogué reste lié » de l'ADR-0002, dont le statut devient « superseded in part by ADR-0023, ADR-0024 » ;
  - index des ADR ;
  - `RacingGameCasaEngine/Assets/README.md` (contenu, générateur, sons) ;
  - ce plan.
- **Validation** : vérificateur indépendant de S3, CONFIRMED.
- **Commit** : `docs(adr): record that the project content holds every game file`

> Validation (2026-10-08) :
> - **Documents** :
>   - ADR-0024, avec son entrée dans l'index ;
>   - le statut de l'ADR-0002, « superseded in part by ADR-0023 … and ADR-0024 (linked content) » ;
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
>   - P3, volumes des catégories XACT : `Music` -3 dB et `Gears` -4 dB ne sont pas repris. L'ADR-0024 et le README le disent maintenant, au lieu de « garde les réglages XACT » ; il faudra les reprendre quand le jeu jouera ces sons ;
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

### ✅ T4.5 — Composants générateurs (D5)

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

> Validation (2026-10-08) :
> - **Code**, dans `RacingGame.Gameplay/Components/` :
>   - une base abstraite publique, `RaceTrackGeneratorComponent`, qui porte :
>     - les deux ids ;
>     - une signature des réglages (`record struct`, sans allocation), comparée à chaque `Update` ;
>     - la reconstruction ;
>     - la libération des tampons GPU ;
>   - les 4 générateurs : `RaceTerrainGeneratorComponent`, `RaceRoadGeneratorComponent`, `RaceGuardRailGeneratorComponent` (`side`, `holder_gap`) et `RaceColumnGeneratorComponent` (`columns_distance`) ;
>   - `RaceGeneratedModelComponent`, en `[Browsable(false)]` ;
>   - `RaceGuardRailSide`.
> - **Ce que les générateurs possèdent** :
>   - ils libèrent les tampons des maillages procéduraux qu'ils créent (terrain, route, rails, colonnes), à la reconstruction et au détachement ;
>   - ils ne libèrent jamais les modèles des poteaux et des segments ni les textures, qui appartiennent aux caches. Ces enfants portent `OwnsBuffers=false` ;
>   - avant de générer, ils retirent les enfants générés qu'ils trouvent, et `Clone()` ne garde que les réglages ;
>   - ils implémentent `IEntityPolicyDefaultsProvider` pour être mis à jour dans l'éditeur. Dans le jeu, les entités restent `StaticDecoration` ;
>   - `GetBoundingBox` rend les bornes de ce qu'ils dessinent ;
>   - un id absent ou inconnu écrit un seul avertissement nommant le propriétaire, puis le générateur ne produit rien.
> - **Physique** : `RaceTrackPhysicsComponent` est public, avec un constructeur sans paramètre et les deux ids, et calcule son profil à `InitializeWithWorld`.
> - **Calcul des placements** :
>   - `RaceTrackLayout.Compute` prend des `RaceTrackLayoutSettings` (15 et 33 par défaut) et `includeScenery` ;
>   - `RaceBootstrapAssets.TryGetTrackName` retrouve le nom d'une piste à partir de son asset.
> - **Fabriques** : celles du jeu assemblent la course avec ces composants, sous les mêmes noms d'entités. Les entités `Track.GuardRailHolder.*` et `Track.ColumnSegment.*` deviennent les enfants générés de leurs rails et colonnes.
> - **Fichier ajouté hors plan, `LegacyTrackFileReader.cs`** :
>   - dans l'éditeur, `XmlSerializer` échoue sur un type de la DLL de gameplay, chargée dans un contexte collectible : « A non-collectible assembly may not reference a collectible assembly ». Un lecteur `XDocument` le remplace pour les `.Track` et les `.CombiModel` ;
>   - comparé à `XmlSerializer` sur les 3 `.Track` et les 10 `.CombiModel`, il donne des résultats identiques, matrices comprises.
> - **Défaut ancien relevé**, gardé tel quel pour la parité (voir « Points ouverts », O7) : les éléments des fichiers (`WidthHelper`, `RoadHelper`, `CombiObject`) ne correspondent pas aux noms que `XmlSerializer` attendait (`LegacyWidthHelper`, `LegacyRoadHelper`, `LegacyCombiObject`). Le jeu actuel ignore donc :
>   - les aides de largeur et de route, d'où l'absence des palmiers, des lanternes et des tunnels ;
>   - le contenu des CombiModel.
> - **Contrôles** :
>   - `dotnet build RacingGame.slnx` : 0 erreur, 0 avertissement ;
>   - `check_gameplay_type_names.py` : 115 types, 0 collision.
> - **A4 contre T4.1** (`artifacts/editor-project-baseline/s4/t4.5/`) :
>   - **export** : 0 maillage non apparié (1 074, 689, 2 156). Les renommages sont exactement les poteaux et les segments devenus enfants :
>     - Advanced : 902 (742 poteaux, 160 segments) ;
>     - Beginner : 556 (455 poteaux, 101 segments) ;
>     - Expert : 1 866 (1 529 poteaux, 337 segments) ;
>     - aucun autre renommage ;
>   - **audit** : 21 vues, au plus 0,001 ;
>   - **audits des voitures** identiques à S0, ce qui valide la physique de piste ;
>   - **captures d'interface** : 0,00 ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.
> - **Éditeur**, essai préparatoire de T4.6 sur une copie : un monde de 7 entités (les générateurs, une lumière, la physique) s'ouvre avec le code 0. La section `Logs:` ne contient que les deux avertissements connus.
>   - La capture lue montre le terrain, la route, les rails et leurs poteaux, et l'inspecteur montre le générateur et ses enfants générés.
>   - Avec `holder_gap` à 45, le rail gauche compte 76 poteaux au lieu de 455.
>   - Un `--save-project` garde les réglages et n'écrit aucun enfant généré.

### ✅ T4.6 — Monde de test des générateurs dans l'éditeur

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

> Validation (2026-10-08) :
> - **Monde** : `Worlds/Test/TrackBeginnerGenerators.world`, dans la forme qu'écrit l'éditeur, avec 7 entités à l'échelle 1 :
>   - `Track.Road.Beginner` ;
>   - `Track.GuardRail.Left.Beginner` et `Track.GuardRail.Right.Beginner` (`holder_gap` 15) ;
>   - `Track.Columns.Beginner` (`columns_distance` 33) ;
>   - `Track.Ground.Beginner` ;
>   - une lumière directionnelle ;
>   - `Track.Physics`, avec `RaceTrackPhysicsComponent` et ses deux ids.
>   Son id (`29b7f6c4-…`) est celui de son entrée de catalogue, `World.Test.TrackBeginnerGenerators`.
> - **Catalogue** : `generate_rgce_content_assets.py` catalogue aussi les `Worlds/**/*.world` qu'aucune entrée ne nomme, et s'arrête si l'id d'un monde diffère de celui de son entrée. `Editor.world` est laissé tel quel. Deux lancements de plus ne changent rien, et `--check` rapporte 0.
> - **Ouverture**, sur une copie : code 0. La section `Logs:` ne contient que les deux avertissements connus : aucun composant non résolu, aucune erreur de chargement ni de génération.
> - **Cadrage** : aucune option ni aucun fichier ne cadre la caméra 3D de l'éditeur. Elle part toujours à 10 unités de l'origine, et seul un double-clic sur une entité la recentre. À l'échelle 1, elle est dans le terrain : la capture ne montre que la grille.
>   - Les copies capturées réduisent donc les racines à 0,02 ; le monde committé reste à l'échelle 1.
>   - La capture lue montre le terrain, la route, les rails et leurs poteaux, et les colonnes sous la route surélevée, petites à cette échelle. L'inspecteur liste les enfants générés des colonnes (508 composants).
>   - Le README donne l'astuce.
> - **Aller-retour d'enregistrement**, par `--save-project` sur une copie :
>   - le premier et le second enregistrement donnent les mêmes octets que le fichier committé (`024c766d…`) ;
>   - aucune clé perdue, et `RaceTrackPhysicsComponent` garde ses deux ids ;
>   - aucun `RaceGeneratedModelComponent` n'est écrit.
> - **Réglage** : dans une copie, `holder_gap` 45 sur le rail gauche réduit visiblement ses poteaux (228 composants contre 678). Le fichier enregistré garde 45 à gauche et 15 à droite.
> - **Jeu inchangé** :
>   - l'export par maillage donne 0 non apparié et les mêmes 3 324 renommages qu'en T4.5 ;
>   - `--smoke-frontend` : code 0 ;
>   - `dotnet build RacingGame.slnx` : 0 erreur, aucun code touché.
> - **Limite relevée** : l'automatisation de l'éditeur ne sait pas cadrer la caméra 3D. C'est un manque de CasaEngine, hors de ce chantier.

### ✅ T4.7 — ADR, documentation et clôture de S4

- **Fichiers** :
  - `docs/decisions/0025-racinggame-gameplay-holds-the-game-code.md` : la DLL de jeu, `IRaceGameHost`, la voiture en entité, les générateurs et leurs enfants transitoires. Elle complète l'ADR-0023 sans la remplacer ;
  - l'index des ADR ;
  - `RacingGameCasaEngine/Assets/README.md` ;
  - ce plan.
- **Validation** : vérificateur indépendant de S4, CONFIRMED.
- **Commit** : `docs(adr): record the game code and track generators in RacingGame.Gameplay`

> Validation (2026-10-08) :
> - **Documents** :
>   - ADR-0025, avec son entrée dans l'index ;
>   - `RacingGameCasaEngine/Assets/README.md` : section des générateurs (T4.6), origine des ids de piste, contenu de l'assembly de gameplay.
> - **Vérificateur indépendant de S4 : CONFIRMED**, sans constat P0 à P2, à `792437b` avec ces documents :
>   - **code** :
>     - la DLL ne référence pas l'exe ;
>     - `IRaceGameHost` est le seul accès au jeu ;
>     - `RacingCarPawn` a disparu ;
>     - 115 types de jeu, 0 collision ;
>   - **A4 refait** : `RacingGame.slnx` recompilé. L'export apparie tout, et les 3 324 renommages sont tous des poteaux et des segments. L'audit reste à 0,001 au plus. Les captures d'interface sont à 0,00, les audits des voitures identiques, et le smoke et le profil d'import sortent avec le code 0 ;
>   - **audit reproductible** : deux audits neufs sont à 0,000 entre eux. Le contrôle négatif est signalé par l'audit (18 vues sur 21) et par l'export (les 6 maillages de route) ;
>   - **éditeur** : le monde de test s'ouvre avec le code 0. Deux enregistrements donnent les octets committés. Aucun enfant généré n'est écrit, et la physique garde ses ids. Un id inconnu donne un seul avertissement en 5 s d'images ;
>   - **revue** :
>     - aucune allocation par image ;
>     - seuls les tampons des maillages procéduraux sont libérés ;
>     - les enfants générés sont retirés à l'initialisation, à la reconstruction, au détachement et au clonage ;
>     - la lecture `XDocument` est identique au bit près à celle de `XmlSerializer` sur les 13 fichiers ;
>     - les déplacements de T4.2 à T4.5 ne changent rien hors de ce que disent leurs notes.
> - **Avis P4 reportés**, sans retouche d'un candidat confirmé (règles de vérification) :
>   - ADR-0025, Context, écrit « 5 of them reached the game through `World.Game as RacingGameCasaEngineGame` ». Il s'agit en fait de 4 composants, plus `RaceGamepadVibration`, qui recevait le jeu par son constructeur ;
>   - ADR-0025, Consequences, écrit « Culling and shadow casting are decided per generator entity ». Seules les bornes de culling sont par entité ; le drapeau d'ombre est porté par chaque enfant généré.
>   Une prochaine ADR sur ce sujet les corrigera.
> - **Non vérifié en conditions réelles** : la reconstruction en direct après un réglage dans l'inspecteur, et la duplication d'une entité génératrice, faute d'automatisation ; le code a été relu. Le temps de chargement d'une course, qui calcule maintenant le tracé une fois par générateur et pour la physique, n'a pas été mesuré.

---

## Phase 5 — Modèles, matériaux et textures en assets (S5)

> Relecture (2026-10-08) : quatre vérificateurs de plan. Les trois premiers ont rendu REVISE, avec 5, puis 1, puis 3 blocages : états de rendu nommés, transparence, conception de la voiture, surcharges absentes de l'export, ordre des contrôles, fonctionnalités de rendu des matériaux compilés, `RenderItem.Features`, matériau des sous-maillages. Après le troisième, la tranche a été mise en pause (O8). L'auteur a demandé une dernière relecture du brouillon corrigé, puis l'exécution. Le quatrième a rendu **READY**.

Faits relevés pour cette phase (2026-10-08). Les sources sont :
- deux découvertes en lecture seule (`s5-disc/` du scratchpad) ;
- les fichiers relus en session ;
- une relecture par un vérificateur de plan ;
- les résultats de S4.

- **Chargement actuel des modèles** : le jeu lit les glTF par chemin et construit les matériaux en code.
  - **Décor, poteaux, segments et feux de départ** : `LegacyTrackSceneFactory.LoadLegacyModel` (`RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs:135-167`) lit le glTF par `LegacyGltfModelReader`, avec le profil RacingGame. Ensuite :
    - `CreateImportedRuntimeMaterial` (`:449-484`) construit un `LitDiffuseMaterial` à partir de deux sources :
      - les réglages `RacingGameLegacyMaterialTuning.EvaluateRuntimeTuning`, choisis par nom de texture ;
      - la présentation `LegacyImportedMaterialPresentationResolver` : file, coupure alpha, culling, ambiante et émissive (`CasaEngine/CasaEngine/Framework/Assets/Loaders/LegacyImportedMaterialPresentation.cs:12-36`).
      Il y ajoute `SamplerState.AnisotropicWrap` et borne la puissance spéculaire entre 2 et 48 ;
    - `ApplyFallbackMaterials` (`:567-585`) donne une couleur par mot-clé du nom aux maillages sans matériau ;
    - `ApplyLegacyModelRootCorrection` (`:169-183`) ajoute une racine tournée de π/2 autour de X.
  - **Voiture** : `LegacyCarVisualFactory` (`RacingGame.Gameplay/Components/LegacyCarVisualFactory.cs`).
    - Un `StaticModel` est lu dans `Car.gltf` pour chaque indice de voiture, et mis en cache par indice (`:42,53-71`). Les trois voitures du carrousel sont à l'écran ensemble.
    - Chaque matériau dont la texture diffuse est une texture de voiture (`RacerCar`, `RacerCar2`, `RacerCar3`, `:37`) reçoit la texture `Painted` ou `Unpainted` du `CarPaint` de son indice, selon des règles sur le nom affiché (`IsPaintMaterial`, `:230-248`).
    - `CarPaint` décode le PNG par StbImageSharp et réécrit le magenta en couleur-clé (`:36,491-533`). Ses pixels ne sont donc pas ceux d'un `.texture` (`Texture2D.FromStream`).
    - Autres règles propres à la voiture : réflexion, facteurs 0,85 et 0,75 (`UsesLegacyCarReflectionBlend`), carte de normales `RacerCarNormal` imposée, matériau de repli (`:175-228,305-324`). Pas de rotation de π/2.
  - **Plateaux du carrousel** : `CarSelectionCarousel.LoadPlateModel` (`RacingGameCasaEngine/UI/CarSelectionCarousel.cs:345-388`), sans les réglages par nom, avec une réflexion de 0,35 et sans rotation.
  - **Route, sol, terrain, rails et colonnes** : des matériaux écrits en code (`LegacyTrackSceneFactory.cs:402-427`, `.Terrain.cs:143-147`, `.GuardRails.cs:292-314`).
  - **Caches** : un modèle de décor est chargé une fois par nom et partagé (`ModelCache`), une texture une fois par chemin (`TextureCache`).
- **Ce qui change à l'exécution** :
  - la peinture de la voiture, à chaque choix de couleur (`:62-63`) ;
  - le feu de départ : `RaceStartLight` remplace le `StaticModel` de son composant à chaque signal, rouge, jaune puis vert (`RacingGame.Gameplay/GameFramework/RaceStartLight.cs:61-93`) ;
  - le cube de réflexion de la scène, utilisé quand un modèle ne nomme aucun cube ou nomme `SkyCubeMap.dds` (`RacingGame.Gameplay/Race/RaceSkySystem.cs:23-28`).
- **Moteur** :
  - un `.material` de définition `lit-diffuse` porte :
    - la file, l'état de rastérisation, l'échantillonneur, les états de mélange et de profondeur par leur nom, et les ombres (`CasaEngine/Projects/SampleProject/NewLitDiffuseMaterial.material`) ;
    - `diffuse_color`, `ambient_color`, `emissive_color`, `specular_color`, `specular_power`, `alpha_cutoff` ;
    - `base_color_texture` et `normal_texture` (ids de `.texture`), et `reflection_texture` (id de `dds`) ;
    - `use_scene_reflection_cube` et les trois facteurs de réflexion ;
  - `MaterialCompiler` en fait un `LitDiffuseMaterial` (`CasaEngine/CasaEngine/Framework/Materials/Compilation/MaterialCompiler.cs:285-354`). `TintColor` n'est pas réglable, et le jeu ne s'en sert pas ;
  - **différence d'états** : le compilateur fixe toujours les états de mélange, de profondeur, de rastérisation et d'échantillonneur, par leur nom (`:350-353`), et un nom d'état ne peut pas être vide (`MaterialAsset.cs:19-22,357-371`). Les matériaux du jeu en laissent plusieurs à `null` :
    - le mélange et la profondeur partout ;
    - la rastérisation, sauf en CullNone ;
    - l'échantillonneur des matériaux de repli.
    - Le rendu traite `null` comme Opaque, Default, CullCounterClockwise et AnisotropicClamp (`CasaEngine/CasaEngine/Framework/Rendering/Draw/RenderStateCache.cs:51-54`) : l'image ne change pas. L'export, lui, écrit `null` (`RacingGameCasaEngine/Bootstrap/TrackRuntimeSceneExporter.cs:264-272`) ;
  - **transparence** : hors file AlphaTest, un alpha diffus inférieur à 255 rend le matériau compilé transparent (`MaterialRenderStateResolver.cs:42-75`). Les 92 matériaux des 56 glTF ont tous un `legacyMaterial.diffuse` à 4 composantes, d'alpha 1,0. Les couleurs écrites en code sont à vérifier par T5.3 ;
  - un `StaticModelComponent` qui porte `StaticModelAssetId` partage le modèle, compté, avec ses autres utilisateurs (`StaticModelComponent.cs:202-238`). `AssetContentManager.LoadCopy<T>(id)` donne une copie privée, hors cache (`AssetContentManager.cs:98-101`) ;
  - le matériau d'un maillage vient de son `material_asset_id` (`StaticModelMaterialResolver.cs:11-57`). Les nœuds du modèle et leurs transforms deviennent des sous-composants (`StaticModelComponent.cs:240-275`), et les sous-maillages lisent le matériau du modèle au dessin ;
  - un `.texture` lit son image par le même `Texture2D.FromStream` que le jeu (`Texture.cs:70-76`, `Texture2DLoader.cs:12`) ;
  - **fonctionnalités de rendu** : pour un matériau dont l'id est dans le cache des matériaux compilés (`MaterialCache`), ce qui est le cas de tout matériau venu d'un `.material`, le rendu prend `CompiledMaterial.Features`. Celles-ci sont calculées sur le matériau seul (`MaterialCompiler.cs:66`, `StaticMeshRendererComponent.cs:276-290,450-466`). Un matériau construit en code n'a pas d'entrée dans ce cache, et passe par `RenderFeatureResolver.Resolve(matériau, maillage)`. Ce résolveur retire `NormalMap` quand le maillage n'a pas de tangentes, et ajoute `VertexColor` quand il a des couleurs de sommet (`RenderFeatureResolver.cs:35-46,139-152`).
    - Aucun des 56 glTF n'a de tangentes, et environ 156 références `normalTexture` y figurent ; la voiture reçoit en plus `RacerCarNormal` imposé.
    - Le jeu dessine donc aujourd'hui sans carte de normales, alors que des matériaux cuits l'activeraient. L'export actuel n'enregistre ni les fonctionnalités ni la technique ;
    - la propriété `RenderItem.Features` rend toujours `CompiledMaterial.Features` quand un matériau compilé est attaché, quelle que soit la valeur fixée par le rendu (`Framework/Rendering/Draw/RenderItem.cs:75-79`). Les passes la lisent (`RenderPass.cs:77-79`, `InstanceBatcher.cs:113-115`, `RenderShaderSelector.cs:53`, `OpaqueAndTransparentPasses.cs:36,86`) ;
  - **matériau dessiné** : le rendu prend, pour chaque sous-maillage (`SubMesh`, une plage d'indices), la surcharge de l'instance, puis `SubMesh.Material`, puis le matériau du maillage (`StaticMeshRendererComponent.cs:262-266`). À l'initialisation, `StaticModel` donne à chaque `SubMesh` son matériau résolu (`StaticModel.cs:79-85`). L'export actuel ne lit que le matériau du maillage (`TrackRuntimeSceneExporter.cs:152-155`) ;
  - **surcharges par instance** : celles d'un `StaticModelComponent` ne prennent que des ids d'assets, et sont recalculées à chaque `InitializeWithWorld` (`StaticModelComponent.cs:154-185,263-265`). L'export lit `ModelMesh.Material` et ignore ces surcharges (`TrackRuntimeSceneExporter.cs:152-178`).
- **Catalogue** : les PNG des textures 3D y sont nommés `Texture.<nom>`, de type `png` (S3). Rien ne les référence par id : le jeu les charge par chemin.

### Prérequis, risques et arrêts de S5

- **Prérequis** : S4 clos (vérificateur CONFIRMED). T5.2, une correction de CasaEngine, et son pointeur précèdent T5.3.
- **Arrêts** :
  - un écart inexpliqué de `compare_race_scene_exports.py`, avec la règle d'états de T5.1, met la tâche en ⚠️ Blocked. Cela vaut pour les sommets, les matrices, les valeurs de matériau, les fonctionnalités de rendu et les pixels de texture ;
  - un audit des voitures qui diffère, ou une capture d'interface hors seuil, fait de même ;
  - si T5.3 trouve un matériau hors AlphaTest avec un alpha diffus inférieur à 255, la tâche passe en ⚠️ Blocked. On choisit alors avec l'auteur une correction de CasaEngine ou une autre forme ;
  - si `lit-diffuse` ne peut pas rendre une autre valeur que le jeu utilise, le manque se corrige dans CasaEngine, avec un pointeur ; s'il faut une décision, la tâche passe en ⚠️ Blocked.
- **Risques** :
  - **Arrondis** : l'enregistrement JSON des valeurs, que l'export compare au matériau d'exécution ;
  - **Partage** : la peinture et le feu ne doivent rien modifier de visible dans un objet partagé ;
  - **Noms courts** : le contrôle des noms repasse à chaque tâche qui ajoute des types.

### ✅ T5.1 — Comparaison des matériaux compilés

- **Objectif** : comparer un matériau compilé à un matériau construit en code sur ce qui se voit à l'écran.
- **Fichiers** :
  - `scripts/compare_race_scene_exports.py` : une option `--effective-states`. Elle remplace un état `null` par celui que le rendu applique (`RenderStateCache.cs:51-54`) : mélange Opaque, profondeur Default, rastérisation CullCounterClockwise, échantillonneur AnisotropicClamp. Elle s'applique aux deux exports avant la comparaison ;
  - `RacingGameCasaEngine/Bootstrap/TrackRuntimeSceneExporter.cs` :
    - un enregistrement par `SubMesh` dessiné, ou par maillage quand il n'a pas de `SubMesh`, avec le matériau que le rendu prend, dans son ordre : la surcharge de l'instance, puis `SubMesh.Material`, puis le matériau du maillage ;
    - il écrit aussi les fonctionnalités de rendu que le rendu applique : `CompiledMaterial.Features` du cache des matériaux compilés quand l'id du matériau y est, sinon `RenderFeatureResolver.Resolve(matériau, maillage)`. T5.2 remplace cette règle par l'API unique du moteur ;
  - `compare_race_scene_exports.py` compare ces fonctionnalités avec les valeurs du matériau ;
  - de nouvelles références, `artifacts/editor-project-baseline/s5/scene-a.json` et `-b.json`, sont prises avec ce code avant tout autre changement de S5. Les tâches suivantes se comparent à elles.
- **Validation** :
  - build à 0 erreur ;
  - un nouvel export contre `s4/scene-a.json` : identique, avec et sans `--effective-states`, pour les maillages sans `SubMesh`, hors le nouveau champ des fonctionnalités. Les maillages à plusieurs `SubMesh` ont désormais un enregistrement par `SubMesh` ; la note en donne le nombre et vérifie que leurs matériaux sont ceux du maillage. Aujourd'hui, aucune surcharge n'existe ;
  - `s5/scene-a.json` contre `s5/scene-b.json` : identiques, fonctionnalités comprises ;
  - **contrôles négatifs** sur une copie de l'export, modifiée à la main :
    - un `RasterizerState` passé de `null` à `CullNone` sur un matériau de décor est signalé, avec ou sans l'option ;
    - un passage de `null` à `CullCounterClockwise` n'est plus signalé avec l'option ;
    - un `NormalMap` ajouté aux fonctionnalités d'un maillage de décor est signalé ;
    - un `SubMesh` dont le matériau diffère de celui de son maillage est signalé.
- **Commit** : `test(racing-casa): compare cooked materials by the states they draw with`

> Validation (2026-10-08) :
> - **Export** :
>   - un enregistrement par `SubMesh` dessiné, ou par maillage sans `SubMesh`, avec le matériau dans l'ordre du rendu (surcharge, `SubMesh.Material`, matériau du maillage). Pour un maillage sans `SubMesh`, la surcharge est cherchée par `mesh.MaterialSlotIndex`, comme le fait le rendu (`StaticMeshRendererComponent.cs:303`) ;
>   - nouveaux champs : `RenderFeatures` (noms triés) et, pour les sous-maillages, `SubMeshIndex`, `SubMeshIndexStart` et `SubMeshPrimitiveCount` ;
>   - le cache des matériaux compilés est celui du jeu (`CasaEngineGame.MaterialCache`, le même que `RuntimeContext.MaterialCache` lu par le rendu), passé par `TrackRuntimeSceneExportValidator`.
> - **Comparaison** : la clé comprend les fonctionnalités et les champs de sous-maillage. `--effective-states` remplace `"null"` par `BlendState.Opaque`, `DepthStencilState.Default`, `RasterizerState.CullCounterClockwise` et `SamplerState.AnisotropicClamp`. `--ignore-features` sert à comparer aux références de S4.
> - **Références** : `artifacts/editor-project-baseline/s5/scene-a.json` et `-b.json`, identiques entre elles (3 919 enregistrements, fonctionnalités comprises), et `s5/track-audit-a/`.
> - **Contre S4** (`--ignore-features`, avec ou sans `--effective-states`) : les 3 919 enregistrements sont appariés.
>   - Aucun maillage du jeu n'a plusieurs `SubMesh`.
>   - Fonctionnalités relevées :
>     - `BasColorTexture` : 3 781 ;
>     - `AlphaTest, BasColorTexture` : 101 ;
>     - `Reflection` : 22 ;
>     - `BasColorTexture, Emissive` : 3 ;
>     - `BasColorTexture, Emissive, NormalMap` : 12. Ce sont le sol, les rails et les colonnes, générés avec tangentes.
>   - Aucun maillage de glTF n'a `NormalMap`.
> - **Contrôles négatifs**, sur des copies modifiées dans `s5/negative/` :
>   - rastérisation `null` passée à `CullNone` : signalée, avec ou sans l'option ;
>   - `null` passé à `CullCounterClockwise` : signalé sans l'option, plus avec ;
>   - `NormalMap` ajouté : signalé ;
>   - un `SubMesh` à puissance spéculaire différente : signalé. Ce contrôle est construit des deux côtés, faute de maillage réel à plusieurs `SubMesh`.
> - **Autres contrôles** : `dotnet build RacingGame.slnx` à 0 erreur ; `--smoke-frontend` à 0 ; audit dans le seuil de T4.1 (21 sur 21).
> - **Non éprouvé sur des données réelles** : les enregistrements par `SubMesh` et la lecture des surcharges, que le jeu n'a pas encore.

### ✅ T5.2 — Moteur : fonctionnalités d'un matériau compilé ajustées au maillage (D3)

- **Objectif** : un matériau compilé et un matériau construit en code de mêmes valeurs se dessinent avec les mêmes fonctionnalités sur le même maillage. C'est un défaut du moteur : un matériau catalogué avec carte de normales, posé sur un maillage sans tangentes, choisit une technique de carte de normales sans les données qu'elle demande.
- **Fichiers**, dans CasaEngine, sur `chantier/rgce-editor-project` :
  - `CasaEngine/Framework/Rendering/Shaders/RenderFeatureResolver.cs` : une API publique unique, qui ajuste des fonctionnalités à un maillage : `NormalMap` retiré sans tangentes, `VertexColor` ajouté avec des couleurs de sommet. `Resolve` l'utilise ;
  - `CasaEngine/Framework/Application/Components/StaticMeshRendererComponent.cs` : les fonctionnalités d'un matériau compilé passent par cette API, et le résultat est fixé sur l'élément de rendu ;
  - `CasaEngine/Framework/Rendering/Draw/RenderItem.cs` : `Features` rend la valeur fixée, et non plus `CompiledMaterial.Features`. Chaque endroit qui construit un `RenderItem` avec un matériau compilé fixe `Features` par la même règle ; la note les liste ;
  - un test dans `CasaEngine.Tests` qui construit un `RenderItem` comme le rendu, avec un `CompiledMaterial` dont les fonctionnalités comprennent `NormalMap` :
    - sur un maillage sans tangentes, `item.Features` n'a pas `NormalMap` ;
    - avec tangentes, il le garde ;
    - avec des couleurs de sommet, il a `VertexColor` ;
  - côté RacingGame :
    - le commit du pointeur `chore(casaengine): …` ;
    - puis `test(racing-casa): export the features the renderer draws with`, qui fait passer `TrackRuntimeSceneExporter` par l'API du moteur, avant T5.3.
- **Validation** :
  - les deux solutions de CasaEngine compilent avec 0 erreur, et `CasaEngine.Tests` passe en entier ;
  - un export de RacingGameCasaEngine contre `s5/scene-a.json` : identique, fonctionnalités comprises. Le jeu ne dessine encore aucun matériau catalogué ;
  - un contrôle ponctuel de la fonction de l'exporteur, sur un matériau compilé avec carte de normales et un maillage sans tangentes : pas de `NormalMap` ;
  - `RacingGame.slnx` compile avec 0 erreur.
- **Commit** : `fix(rendering): resolve compiled material features against the drawn mesh` dans CasaEngine, puis le pointeur dans RacingGame.

> Validation (2026-10-08) :
> - **CasaEngine `359a0a98b`** :
>   - `RenderFeatureResolver.AdjustToMesh` porte la règle unique : `NormalMap` retiré sans tangentes ou en skinning ; `Skinned`, `Instanced` et `VertexColor` ajoutés. `Resolve` s'en sert ;
>   - `ResolveForMesh(matériau, matériau compilé, maillage)` applique cette règle aux deux chemins ;
>   - `StaticMeshRendererComponent` fixe `Features` par `ResolveForMesh` dans ses deux constructions d'éléments, avec ou sans `SubMesh` ;
>   - `RenderItem.Features` rend la valeur fixée ;
>   - **producteurs de `RenderItem`** : seul `StaticMeshRendererComponent` en construit. Le skinning passe par `ResolveSkinned` sans `RenderItem`, et le batch d'instances et les passes ne font que lire ;
>   - `EffectiveShaderId` garde le même motif de propriété ; il ne dépend que du matériau, et ne touche pas ce défaut ;
>   - tests : `RenderItemFeaturesTests` (6), et `RenderItemTests` mis à jour (son premier test affirmait l'ancien repli) ;
>   - docs : `materials-sources-of-truth.md` et `materials-workflow.md` ;
>   - les deux solutions compilent avec 0 erreur, et `CasaEngine.Tests` passe à 4 323 sur 4 323.
> - **Pointeur** : `c2b68c4`.
> - **Exporteur** : `CaptureRenderFeatures` fait la même recherche dans le cache que le rendu, puis appelle `RenderFeatureResolver.ResolveForMesh`. Un contrôle ponctuel par réflexion, sur un matériau compilé (`BasColorTexture` + `NormalMap`) injecté dans un `MaterialCache`, donne :
>   - sans tangentes : `BasColorTexture` ;
>   - avec tangentes : `BasColorTexture,NormalMap`.
> - **Jeu** :
>   - `RacingGame.slnx` compile avec 0 erreur ;
>   - l'export contre `s5/scene-a.json` est identique, fonctionnalités comprises (1 074, 689, 2 156) ;
>   - `--smoke-frontend` sort avec le code 0 ;
>   - l'audit reste à 0,001 au plus.
>   - Un premier lancement des captures d'interface a donné 0,27 (`track-selection`), 0,66 (`race-hud`) et 10,7 (`race-finished`), des états animés ou chronométrés. Le lancement suivant a donné 0,00 sur les 10 états.
> - **Projet d'exemple** : aucun matériau de `Projects/SampleProject` n'a de carte de normales sur un maillage sans tangentes, donc rien n'y change. La capture de `Box.staticModel` est normale.

### ✅ T5.3 — Recettes de matériaux séparées de leur construction

- **Objectif** : les valeurs de chaque matériau sont calculées par une fonction pure, sans GPU, réutilisée par l'outil de cuisson (T5.4) et par le jeu tant qu'il charge encore les glTF. Aucun changement de rendu.
- **Fichiers** :
  - `RacingGame.Gameplay/Import/RaceMaterialRecipe.cs` : un enregistrement de toutes les valeurs d'un `LitDiffuseMaterial` du jeu. Il porte :
    - les couleurs et la puissance ;
    - les chemins des textures, et pour la voiture le rôle de la texture : peinte, non peinte, ou une autre texture ;
    - la réflexion, la file, la coupure, la rastérisation, l'échantillonneur et les ombres ;
  - les fonctions qui calculent ces valeurs à partir du matériau importé et du nom du modèle, pour chaque chemin : décor, voiture, plateau, repli, et matériaux de la piste écrits en code ;
  - `LegacyTrackSceneFactory`, `LegacyCarVisualFactory` et `CarSelectionCarousel` construisent leurs matériaux à partir de ces recettes.
- **Validation** :
  - build à 0 erreur ;
  - A4 : export contre `s5/scene-a.json` identique jusqu'aux noms, fonctionnalités comprises. Captures d'interface à 0,00, ce qui couvre le carrousel, et audits des voitures identiques ;
  - **transparence** : une passe des recettes sur les 56 glTF et sur les matériaux écrits en code compte les recettes hors AlphaTest dont l'alpha diffus est inférieur à 255. La note donne ce nombre. S'il n'est pas nul, arrêt.
- **Commit** : `refactor(racing-casa): compute material values apart from building materials`

> Validation (2026-10-08) :
> - **Code**, dans `RacingGame.Gameplay/Import/` :
>   - `RaceMaterialRecipe` : enregistrement immuable, avec les noms d'états (`RaceMaterialStates`) et le rôle de la texture de base (`RaceBaseTextureRole` : `File`, `CarPainted`, `CarUnpainted`) ;
>   - `RaceMaterialRecipes` : fonctions pures, déplacées telles quelles :
>     - `ForScenery` et `ForSceneryFallback` ;
>     - `ForCar` et `ForCarFallback`, avec les règles de peinture, de mélange de réflexion et de carte de normales ;
>     - `ForPlate` ;
>     - `ForRoad`, `ForGround`, `ForTerrain`, `ForGuardRail` et `ForRoadColumn` ;
>   - `RaceMaterialBuilder` (interne) : seul endroit qui traduit une recette et ses textures chargées en `LitDiffuseMaterial` ;
>   - les fabriques de la piste, de la voiture et du carrousel construisent leurs matériaux par ces recettes.
>   - Pour les matériaux de piste écrits en code, la couleur diffuse dépend de la texture qui s'est chargée : `LoadProjectTexture` rend le chemin chargé, que la recette prend.
>   - `rg` ne trouve ni GPU ni asset dans les deux fichiers de recettes.
> - **Contrôles** :
>   - `dotnet build RacingGame.slnx` : 0 erreur ;
>   - `check_gameplay_type_names.py` : 0 collision ;
>   - export contre `s5/scene-a.json` : identique jusqu'aux noms, fonctionnalités comprises (1 074, 689, 2 156) ;
>   - captures d'interface à 0,00, `car-selection` compris. Le premier lancement avait perdu `race-finished`, une capture manquante ; le second était propre ;
>   - audits des voitures identiques à S0 ;
>   - `--smoke-frontend` : code 0 ;
>   - `--verify-legacy-import-profile` : code 0 ;
>   - audit dans le seuil de T4.1 (21 sur 21).
> - **Transparence (règle d'arrêt)** : une sonde a calculé 116 recettes :
>   - 85 pour le décor (54 modèles) ;
>   - 18 pour la voiture (6 matériaux, 3 indices) ;
>   - 1 pour le plateau ;
>   - 12 pour les 6 matériaux de piste, avec et sans texture.
>   Aucune n'a un alpha diffus inférieur à 255 ; les 12 en `AlphaTest` non plus. Aucun glTF n'a de maillage sans matériau.
> - **Couverture** :
>   - recettes distinctes, sans compter le nom : 66 pour le décor, 4 pour la voiture (seuls le nom et le rôle de la texture changent d'un indice à l'autre), 1 pour le plateau ;
>   - aucun glTF ne nomme de cube de réflexion ;
>   - `Blockade`, `Blockade2` et `Laterne2Sides` ne sont chargés par aucun chemin ;
>   - 8 autres ne sont nommés que par les `.Track`.

### ✅ T5.4 — Outil de cuisson et assets cuits

- **Objectif** : P8. Les 56 modèles, leurs matériaux, ceux de la piste et leurs textures deviennent des assets catalogués, aux ids déterministes.
- **Fichiers** :
  - **`scripts/RgceAssetCooker/`**, outil console (`RgceAssetCooker.csproj`, README) qui référence `RacingGame.Gameplay` et `CasaEngine.EditorServices`. Il écrit avec les sérialiseurs de l'éditeur (`EditorAssetJsonSerializer`) :
    - **un `.staticModel` par glTF** : `Content/Models/<Nom>.staticModel`, nommé `StaticModel.<Nom>`. La racine tournée de π/2 est cuite dans les modèles que charge `LoadLegacyModel`, pas dans la voiture ni le plateau ;
    - **un `.material` par matériau de modèle** : `Content/Materials/Models/<Nom>/<Emplacement>.material`, nommé `Material.<Nom>.<Emplacement>`, avec les valeurs des recettes de T5.3 :
      - les états `null` du jeu deviennent les états que le rendu applique ;
      - les emplacements de la voiture à texture de voiture prennent `Texture.RacerCar`, que la peinture remplace à l'exécution (T5.6) ;
    - **un `.material` par matériau de piste** : `Content/Materials/Track/<Rôle>.material`, nommé `Material.Track.<Rôle>` ;
    - **un `.texture` par image utilisée** : `Content/Textures/<Nom>.texture`, nommé `Texture.<Nom>`, avec l'échantillonneur des recettes. Les entrées `png` de S3 sont renommées `Image.<Nom>` dans `generate_rgce_content_assets.py`, puisque rien ne les référence par id ;
    - les entrées du catalogue, avec des ids uuid5 du nom, comme les autres générateurs.
  - **Comportement de l'outil** : un second lancement ne change aucun octet. `--check` liste les glTF ou les images sans asset cuit.
- **Validation** :
  - build de l'outil et de `RacingGame.slnx` à 0 erreur ;
  - second lancement identique ;
  - `generate_rgce_content_assets.py --check` à 0 ;
  - **fidélité** : l'outil relit chaque `.staticModel` cuit. Sommets, indices et nœuds sont ceux de la lecture du glTF, plus la racine tournée là où elle s'applique. Chaque `.material` compilé par `MaterialCompiler` donne, avec la règle d'états de T5.1, les valeurs de sa recette ;
  - les nouveaux assets s'ouvrent dans l'éditeur par `catalogue_open.ps1` : code 0 pour chacun.
- **Commit** : `feat(racing-casa): cook the models, materials and textures into assets`

> Validation (2026-10-08), en workflow : une implémentation, puis deux vérificateurs indépendants, l'un sur la fidélité, l'autre sur le catalogue et l'outillage. Tous deux rendent **CONFIRMED**, sans constat bloquant.
> - **Outil** : `scripts/RgceAssetCooker/` (`Program`, `Cooker`, `CookPlan`, `Verifier`, `CatalogFile`, `Uuid5`, README), hors de `RacingGame.slnx` comme les autres outils de `scripts/`.
>   - `RacingGame.Gameplay` accorde `InternalsVisibleTo("RgceAssetCooker")`, pour le lecteur glTF et le profil d'import.
>   - L'outil écrit par `EditorAssetWriterService.SaveAsset` dans un dossier temporaire et ne copie que les fichiers changés.
>   - Il fixe les ids par réflexion, sans toucher CasaEngine.
> - **Assets** :
>   - 56 `.staticModel` ;
>   - 97 `.material` : 92 de modèles, 5 de piste (`Road`, `Ground`, `Terrain`, `GuardRail`, `RoadColumn`), dont 85 Opaque, 12 AlphaTest, 12 CullNone et 4 au cube de la scène ;
>   - 105 `.texture`.
>   Le catalogue passe de 346 à 604 entrées. Les entrées `png` de S3 deviennent `Image.<Nom>`, ce que confirme `rg` : aucun fichier ne les référençait par id. Les `dds` gardent `Texture.<Nom>`. Le générateur de contenu laisse aux soins de l'outil les types `staticmodel`, `material` et `texture`.
> - **Fidélité** (`--verify`) : 0 écart.
>   - **Modèles** : 56 modèles, 105 maillages, 55 976 sommets, 141 591 indices et 175 nœuds identiques à la lecture du glTF, avec la racine tournée de π/2 exactement là où `LoadLegacyModel` l'ajoute.
>   - **Matériaux** : 97 matériaux et 2 910 valeurs identiques à leurs recettes, avec la règle d'états.
>   - **Textures** : 105 identiques.
>   - **Seconde sonde** : celle du vérificateur, indépendante de l'outil, couvre les 56 modèles et donne 0 écart.
> - **Déterminisme** :
>   - un second lancement n'écrit rien ;
>   - régénérés depuis le catalogue de `HEAD` dans une copie, générateur puis outil, les assets sont identiques octet pour octet à ceux de l'arbre ;
>   - `--check` et `--verify` signalent bien un asset retiré et un matériau modifié.
> - **Catalogue** : aucun id ni nom en double, chaque fichier existe, et `generate_rgce_content_assets.py --check` rapporte 0.
> - **Jeu** : `RacingGame.slnx` compile avec 0 erreur, et `--smoke-frontend` sort avec le code 0. Le jeu ne charge pas encore les assets cuits.
> - **Éditeur** : les 266 entrées `staticmodel`, `material` et `texture` (les 258 nouvelles et les 8 textures d'interface) ont été ouvertes une par une, avec une copie de l'éditeur et une copie du contenu (`artifacts/editor-project-baseline/s5/catalogue-t5.4/`). Toutes s'ouvrent avec le code 0, ce qui exerce `MaterialCompiler`. La capture lue de `StaticModel.Hotel01` montre ses maillages et leurs matériaux cuits, par leurs ids.
> - **Avis P4** : la vérification de l'outil ne compare des sous-maillages que leur nombre, et aucun modèle n'en a ; lancer l'outil avant le générateur donne les mêmes entrées dans un autre ordre.

### ✅ T5.5 — Le jeu charge la piste et le décor par le catalogue

- **Objectif** :
  - le décor, les poteaux, les segments et les feux de départ sont des `StaticModelComponent` qui portent `StaticModelAssetId` ;
  - les générateurs prennent leurs matériaux par id : un `material_asset_id` en `[SerializedProperty]`, qui vaut par défaut l'asset cuit ;
  - le chemin de la piste n'appelle plus `LoadLegacyModel` ni les réglages par nom.
- **Fichiers** :
  - `LegacyTrackSceneFactory*` et les générateurs ;
  - `RaceStartLight` : il change l'asset de son composant au lieu d'un `StaticModel` construit à la main ;
  - le monde de test de T4.6, réenregistré par l'éditeur s'il gagne des propriétés.
- **Validation** :
  - build à 0 erreur ; contrôle des noms à 0 ;
  - **A4** :
    - export contre `s5/scene-a.json`, avec `--effective-states` : mêmes maillages, matrices, valeurs de matériau, fonctionnalités de rendu et pixels de texture ;
    - pour le reste, contre les références de T4.1 :
    - audit dans le seuil, dont `start-full` et ses feux ;
    - audits des voitures identiques ;
    - captures d'interface à 0,00 ;
    - smoke à 0 ;
  - `rg "LoadLegacyModel|EvaluateRuntimeTuning|ApplyFallbackMaterials" RacingGame.Gameplay/Track RacingGame.Gameplay/GameFramework/RaceStartLight.cs` ne trouve plus d'appel. La voiture garde les siens jusqu'à T5.6.
- **Commit** : `feat(racing-casa): load the track and scenery models from the catalogue`

> Validation (2026-10-09), en workflow : une implémentation, puis deux vérificateurs indépendants, l'un sur la parité, l'autre sur le code et l'éditeur. Tous deux rendent **CONFIRMED**.
> - **Code** :
>   - **modèles** : décor, poteaux, segments et feux de départ sont des `StaticModelComponent` (ou `RaceGeneratedModelComponent`) portant `StaticModelAssetId`, partagés par les handles du moteur. `LegacyTrackSceneFactory.TryGetStaticModelAssetId` passe du nom du modèle hérité (alias compris) à son asset ;
>   - **taille des modèles** pour les placements : elle vient du modèle cuit (`Acquire`, puis `ComputeLegacyModelSize`) ;
>   - **matériaux des générateurs** : chacun a un `material_asset_id` en `[SerializedProperty]`, cuit par défaut (`RaceTrackMaterialAssets`), qui entre dans sa signature. Les maillages procéduraux ne portent plus que `MaterialAssetId`, et le moteur résout le matériau (`StaticModelMaterialResolver`, `MaterialRuntimeResolver`, `MaterialCache`). `Material.Track.Ground` ne sert pas : le sol qui s'en servait était déjà du code mort ;
>   - **`RaceStartLight`** prend les trois modèles d'avance et change l'asset de son composant à chaque signal, au même rythme ;
>   - **retirés du chemin de la piste**, devenus morts : `LoadLegacyModel`, la racine tournée, les matériaux importés et de repli, les chargeurs et caches de textures, les fabriques de matériaux par rôle et `LoadStartLightModels`. La voiture, les plateaux, l'outil de cuisson et les validateurs gardent ce qu'il leur faut ;
>   - **monde de test** : réenregistré par l'éditeur, avec `material_asset_id` sur les cinq générateurs.
> - **Parité** :
>   - **export** contre `s5/scene-a.json` avec `--effective-states` : les 1 074, 689 et 2 156 enregistrements sont appariés, sans renommage. Sans l'option, les états `null` et nommés diffèrent comme prévu. Les enregistrements portent maintenant des états nommés, preuve que le chemin compilé sert ;
>   - **contrôles négatifs**, dans le dossier de sortie, puis remis à l'identique : une puissance spéculaire changée dans un matériau de décor signale les 7 maillages `Glas [39]` de `Hotel01` ; dans `Road.material`, elle signale les 3 routes ;
>   - **audit** : 21 vues dans le seuil, au plus 0,013, et les `start-full` à 0,000 ;
>   - **audits des voitures** identiques à S0 ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0 ;
>   - le `rg` de la tâche ne trouve rien.
> - **Captures d'interface** : `UiCaptureCompare` sort avec le code 0, mais pas à 0,00. `splash` est à 0,66, `race-hud` entre 0,66 et 1,69, et `race-finished` entre 10,7 et 16,3, au-dessus du seuil.
>   - Le même jeu construit à `HEAD`, sans T5.5 (`git archive`), donne les mêmes chiffres : c'est l'environnement de la machine, devenu plus irrégulier, pas la tâche.
>   - Comparés l'un à l'autre, le build de T5.5 et ce témoin donnent 0,00 sur `race-hud` et `race-finished`, les deux écrans où la piste apparaît.
>   - La parité de l'interface est donc établie contre ce témoin. Les tâches suivantes procèdent de même tant que le bruit dure.
> - **Éditeur** : le monde de test s'ouvre avec le code 0. Deux `--save-project` donnent les octets committés.
> - **Avis P4 reportés** : deux commentaires décrivent encore l'ancien code (`RaceGeneratedModelComponent`, l'outil de cuisson). Si `Acquire` échoue pour le 2ᵉ ou le 3ᵉ modèle du feu, les handles déjà pris ne sont pas rendus.

### ✅ T5.6 — La voiture et le carrousel par le catalogue

- **Objectif** : la voiture et les plateaux chargent `StaticModel.Car` et `StaticModel.CarSelectionPlate`. La peinture reste un état d'exécution, propre à chaque indice de voiture, qui ne modifie aucun asset partagé.
- **Conception de la voiture** :
  - **modèle** : pour chaque indice de voiture, `LegacyCarVisualFactory` prend une copie privée du modèle cuit par `AssetContentManager.LoadCopy<StaticModel>(StaticModel.Car)`, l'initialise et la garde dans son cache par indice, comme aujourd'hui (`:42`) ;
  - **emplacements à texture de voiture** : ce sont ceux dont le matériau cuit a `Texture.RacerCar` pour texture de base. Pour chacun, la fabrique remplace, dans la copie privée seulement, le matériau du maillage **et** celui de chacun de ses `SubMesh` par une copie du matériau compilé, puisque le rendu prend d'abord `SubMesh.Material`. Cette copie prend pour texture de base `CarPaint.Painted` ou `CarPaint.Unpainted` de l'indice ;
  - **règle peinte ou non** : elle reste la règle documentée du chemin de peinture, appliquée au nom de l'emplacement (`slot_name`) et à la réflexion du matériau cuit, comme `IsPaintMaterial` aujourd'hui sur le matériau importé. Les autres règles par nom (réglages, facteurs de réflexion, carte de normales, repli) sont cuites dans les `.material` ;
  - **textures** : `CarPaint` garde son décodage StbImageSharp avec la couleur-clé ; le `.texture` cuit ne sert que de valeur par défaut, dans l'éditeur ;
  - **ids des copies** : chaque copie de matériau d'une voiture reçoit un nouvel id (`Guid.NewGuid()`), comme les matériaux construits en code aujourd'hui. Elle n'a donc pas d'entrée dans le cache des matériaux compilés, et le rendu prend sa propre texture peinte et les fonctionnalités résolues sur le maillage. Garder l'id de l'asset ferait dessiner le matériau compilé partagé, avec sa texture non peinte ;
  - **rattachement** : le `StaticModelComponent` `BodyVisual` reçoit la copie privée par sa propriété `StaticModel`, comme aujourd'hui, sans asset id. Aucune surcharge par instance n'est nécessaire, et l'export voit les matériaux peints par `ModelMesh.Material` ;
  - **plateaux** : le modèle cuit, partagé par asset id, sans peinture.
- **Fichiers** : `LegacyCarVisualFactory`, `LegacyCarVisualComponent`, `CarSelectionCarousel`, et si besoin `RaceCarEntityFactory`.
- **Validation** :
  - build à 0 erreur ;
  - export contre `s5/scene-a.json`, avec `--effective-states` : les maillages de la voiture ont les empreintes de texture peinte et les fonctionnalités de la référence ;
  - **contrôle négatif** : un export pris avec une autre couleur de voiture signale les emplacements peints ;
  - captures d'interface à 0,00, dont `car-selection` (les trois voitures peintes et les plateaux) et `race-hud` ;
  - audits des voitures identiques ;
  - `rg "EvaluateRuntimeTuning|UsesCarDiffuseTexture|UsesLegacyCarReflectionBlend|LoadLegacyModel|ApplyFallbackMaterials"` ne trouve plus rien hors de l'outil de cuisson et de `RaceMaterialRecipe`. `IsPaintMaterial`, la règle de peinture, ne reste que dans le chemin de peinture documenté.
- **Commit** : `feat(racing-casa): load the car and the selection plates from the catalogue`

> Validation (2026-10-09), en workflow : une implémentation, puis deux vérificateurs indépendants, l'un sur la parité, l'autre sur le code. Tous deux rendent **CONFIRMED**.
> - **Voiture** :
>   - pour chaque indice, `LoadCopy<StaticModel>(StaticModel.Car)`, `Initialize`, puis le cache par indice ;
>   - les emplacements dont le matériau compilé a `Texture.RacerCar` pour texture de base reçoivent, sur le maillage et chacun de ses `SubMesh`, une copie champ par champ (`CopyWithBaseTexture`). Elle porte un nouvel id et `BasColor` = `CarPaint.Painted` ou `Unpainted`. `BasColorAssetId` reste vide, comme pour un matériau construit en code ;
>   - rien de partagé n'est modifié : ni le `StaticModel.Car` du cache, ni les matériaux compilés ;
>   - `BodyVisual` reçoit la copie par sa propriété `StaticModel`.
> - **Règle de peinture** : `IsPaintSlot(nom d'emplacement, réflexion)` applique la règle des noms, puis la réflexion cuite. `RgceAssetCooker --verify` la compare au rôle de `RaceMaterialRecipes.ForCar` pour les 6 matériaux de `Car.gltf`, sur 12 maillages : 0 écart.
>   - Peint : `lack`.
>   - Non peints : `gummi`, `gummi 2`, `chrome`, `chrome 2`.
>   - Autre texture : `Standard 19`.
> - **Plateaux** : `StaticModelComponent` partagés sur `StaticModel.CarSelectionPlate`. `LoadPlateModel` et son chargeur de textures sont retirés.
> - **Code mort retiré** : `RaceMaterialBuilder`, `ForCarFallback`, `ForSceneryFallback` et `InferSceneryColor`.
> - **Parité** :
>   - **export** contre `s5/scene-a.json` avec `--effective-states` : tout est apparié. Les 12 maillages de la voiture par piste ont les empreintes de texture peinte (`556ac047…` pour `lack`) et les fonctionnalités de la référence ;
>   - **contrôle négatif** : un export avec la couleur 1 signale les 3 `Car [lack]` ;
>   - **audit** dans le seuil (au plus 0,028) ; **audits des voitures** identiques à S0 ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.
> - **Captures d'interface**, contre un témoin construit à `HEAD` : `car-selection` et `race-hud` à 0,00. `splash` et `race-finished` restent bruités d'un lancement à l'autre, même entre deux lancements de la même version.
>   - Un vérificateur a trouvé une cause probable : les deux vérificateurs lançaient le jeu en même temps.
>   - Les prochains workflows n'auront plus qu'un seul angle qui lance le jeu.
> - **Écart accepté sur le `rg` (P3)** : `EvaluateRuntimeTuning` reste appelé par `LegacyImportProfileVerifier`. C'est un outil de contrôle, pas le chemin du jeu, et `--verify-legacy-import-profile` doit continuer de passer. Aucun chemin d'exécution du jeu ne correspond.
> - **Avis P4 reportés** :
>   - le README de l'outil de cuisson cite encore les fonctions de repli retirées (à corriger en T5.7) ;
>   - les copies privées gardent le `MaterialAssetId` cuit, si bien qu'un rechargement à chaud d'un matériau de voiture rendrait le matériau non peint ;
>   - les copies et les textures de peinture vivent jusqu'à la fin du processus, comme avant ;
>   - `Initialize` compile les matériaux cuits de la voiture, inutilisés une fois peints.

### ✅ T5.7 — ADR, documentation et clôture de S5

- **Fichiers** :
  - `docs/decisions/0026-rgce-models-and-materials-are-cooked-assets.md`, avec l'index des ADR ;
  - `RacingGameCasaEngine/Assets/README.md` : la cuisson, les noms, le second lancement, la peinture ;
  - ce plan.
- **Validation** :
  - le catalogue entier ouvert dans l'éditeur : chaque entrée avec le code 0 ;
  - vérificateur indépendant de S5, CONFIRMED.
- **Commit** : `docs(adr): record the cooked models, materials and textures`

> Validation (2026-10-09) :
> - **ADR-0026** (`docs/decisions/0026-rgce-models-and-materials-are-cooked-assets.md`) et son entrée d'index.
> - **`RacingGameCasaEngine/Assets/README.md`** :
>   - nouvelle section « Cooked models, materials and textures » : cuisson, noms, second lancement, peinture, feu, matériaux des générateurs ;
>   - la liste des outils du catalogue passe à trois : `Image.<nom>` pour les PNG, `World.<chemin>`, puis l'outil de cuisson.
> - **README de l'outil de cuisson**, corrigé :
>   - les fonctions de repli retirées en T5.6 ;
>   - la racine tournée, que le jeu n'ajoute plus depuis T5.5 ;
>   - le contrôle de la règle de peinture par `--verify`.
> - **Catalogue entier ouvert dans l'éditeur** (`artifacts/editor-project-baseline/s5/catalogue-t5.7/`), avec des copies de l'éditeur et du contenu faites après `8e45f6f` :
>   - 604 entrées sur 604 : `OPENED exit=0`, avec une capture chacune ;
>   - par type : 148 `png`, 113 `texture`, 97 `material`, 56 `staticmodel`, 56 `gltf`, 50 `sprite`, 28 `sound`, 28 `wav`, 10 `combimodel`, 9 `uiscreen`, 3 `track`, 2 `dds`, 2 `world`, 1 `data`, 1 `fnt` ;
>   - les captures lues de `StaticModel.Car` et de `Material.Car.lack` montrent le modèle cuit, ses matériaux par id, et les valeurs du matériau dans l'inspecteur.
> - **Index des plans** (`ai-agent/README.md`) : les lignes de ce plan et du plan O7 sont mises à jour.
> - **Vérificateur indépendant de S5 : CONFIRMED**, sans constat P0, P1 ni P2 (sorties dans `verify-s5/` du scratchpad). Un seul processus de jeu à la fois, et les réglages de l'auteur restaurés à l'identique.
>   - **Assets cuits** :
>     - `--check` et `--verify` sortent avec le code 0 (0 écart, règle de peinture sur 6 matériaux et 12 maillages) ;
>     - une cuisson complète sur une copie de `Content` n'en change aucun octet ;
>     - le catalogue a 604 entrées, soit 346 avant S5, plus 56, 97 et 105. Ids uuid5 vérifiés, sans doublon, et chaque fichier existe.
>   - **Chargement par id**, lu dans le diff : décor, poteaux, segments, feu, voiture (`LoadCopy` et copies de matériaux peints), plateaux, et `material_asset_id` des quatre générateurs. `rg` ne trouve les règles par nom que dans `RaceMaterialRecipes` (appelé par l'outil de cuisson ; le jeu n'en lit que `CarDiffuseTextureNames`) et dans `LegacyImportProfileVerifier` (P3 connu).
>   - **Rendu inchangé** :
>     - export contre `s5/scene-a.json` avec `--effective-states` : 1 074, 689 et 2 156 enregistrements appariés ;
>     - **témoin** construit depuis `2b622d6` : il a les outils de T5.1 et T5.2 et le correctif du moteur, mais encore les matériaux construits en code. Son export est apparié au même fichier et à celui de S5 ;
>     - **contrôle négatif** : une puissance spéculaire changée est signalée ;
>     - **audit** : 21 vues sur 21 dans le seuil, au plus 0,013 ;
>     - **audits des voitures** identiques à S0, `details/` compris ;
>     - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0 ;
>     - **captures d'interface**, lancées dans l'ordre témoin, S5, témoin, S5 :
>       - chaque écart entre S5 et le témoin est une valeur déjà vue entre deux lancements du témoin (au plus 1,28 sur `race-hud`) ;
>       - contre `references-acb6ad9`, `race-finished` est à environ 13,8. Cette valeur est la même pour le témoin et pour S5 : elle est antérieure à S5.
>   - **Éditeur** : le TSV du parcours suit le catalogue entrée par entrée. Les 604 captures existent, sans exception dans les diagnostics, et trois captures ont été lues.
>   - **ADR-0026 et README** : chaque fait est vérifié dans le code et les fichiers cuits :
>     - 81 matériaux sur 97 avec une carte de normales ;
>     - aucune tangente dans les glTF ;
>     - 54 modèles avec la racine tournée ;
>     - 138 `Image.*` ;
>     - les états par défaut du rendu.
>   - **Avis P4** :
>     - la plage de commits du brief était fausse. S5 va de `a553a75` à `8e45f6f`, et T5.1 à T5.2 précèdent `2b622d6`. Cela ne change rien au résultat ;
>     - `Material.Track.Ground` est cuit, mais le jeu ne s'en sert pas. Déjà noté en T5.5, et les docs ne disent pas le contraire.

## Phase 6 — Mondes en assets (S6)

> **Remplacée (2026-11-19).** Le détail ci-dessous, écrit le 2026-10-09 pour le jeu d'avant le PBR, est remplacé par le plan [rgce-worlds-as-assets-tasks.md](rgce-worlds-as-assets-tasks.md), approuvé par l'auteur le 2026-11-18 et exécuté par ses tranches M0 à M8 (l'état de chacune est dans ce plan). Les tâches T6.1 à T6.6 ci-dessous ne sont pas à faire : leurs résultats sont livrés par M3 (champs enregistrés), M4 (constructeurs dans la DLL), M5 (mondes et voiture écrits en assets), M6 (le jeu les charge), M7 (carrousel) et M8 (ADR-0042). La question O9 sur le ciel est levée par la décision D10 de ce plan. Suite : S7.

> Brouillon (2026-10-09), écrit après la clôture de S5 et l'exécution du plan O7. À relire par un vérificateur de plan avant toute exécution.
>
> Première relecture : **REVISE**, avec un blocage. Le plan ne disait pas comment l'outil remplit les références d'entités d'un monde sans initialiser les entités. `SaveWorld` n'écrit que `World._entityReferences`, et les voies publiques initialisent les entités. Correction : P12, T6.2 et T6.3 décrivent le mécanisme (réflexion dans l'outil, sans changer le moteur), la mise en place sans jeu et la taille des modèles.
>
> Seconde relecture : **REVISE**, avec deux blocages ; le premier est levé. Dispositions :
> - **B1, aller-retour de `PlayerCar.entity` : corrigé.** `--save-project` ne réécrit pas un `.entity` ouvert sans modification (`GameEditor.cs:5942-5950`), donc le contrôle passait à vide. T6.3 garde `--save-project` pour les mondes, qui deviennent le monde courant à l'ouverture. Pour la voiture, une sonde réécrit vraiment le fichier, et l'enregistrement par l'éditeur devient une étape manuelle écrite.
> - **B2, ciel : corrigé.** P14 laissait le ciel hors des mondes, contre D2 et le résultat de S6. Désormais, les mondes de course portent la cubemap du ciel dans leur `environment` (P14), et le jeu la lit.
> Une dernière relecture, de clôture, suit. Un nouveau REVISE met S6 en pause, avec une question à l'auteur.
>
> Relecture de clôture : **REVISE**. B1 est levé, mais le ciel (B2) ne l'est qu'à moitié : les mondes écrits portent l'id, sans rien qui prouve que le jeu s'en sert.
> - `CarSelectionCarousel.cs:98` demande le cube de réflexion de la course au front-end, avant tout monde de course.
> - Le cube partagé est mis en cache au premier essai, par un seul drapeau et un chemin fixe (`RacingGameCasaEngineGame.cs:327-336`). Le premier chemin qui le charge fixe donc le ciel pour toute la session.
> - La parité A4 de T6.4 passe aussi si le jeu ignore l'id.
> **S6 est en pause (2026-10-09)** : la règle de relecture interdit une nouvelle boucle sans l'auteur. Question O9 dans « Points ouverts ». Aucune tâche de S6 n'est commencée.

Faits relevés pour cette phase (2026-10-09), à `2fa7ac7`. Les sources :
- deux découvertes en lecture seule, rangées dans `s6-disc2/` du scratchpad, sur une première découverte (`s6-disc/`) faite avant S5 et O7 ;
- les fichiers relus en session.

- **Assemblage actuel des mondes, en code** :
  - **front-end** : `RaceWorldFactory.CreateFrontEndWorld` (`RacingGameCasaEngine/Worlds/RaceWorldFactory.cs:82-93`), monde `RacingGameCasaEngine.FrontEndWorld` avec deux entités :
    - `BootstrapCamera` : `CameraLookAtComponent` en (0, 6, −18), cible (0, 0, 0), `DynamicDefault` ;
    - `PlayerStart` : `PlayerStartComponent` en (0, 0, −4), `StaticDecoration`.
  - **course** : `CreateRaceWorld` (`:95-139`), monde `RacingGameCasaEngine.RaceWorld.<piste>`. Entités, dans l'ordre :
    1. `BootstrapCamera`, avec `ChaseCameraRigComponent` et `DebugFreeCameraComponent` ;
    2. `RaceRoot` (Static, EveryFrame, StaticIndex, Static), sans racine, avec `RaceTrackPhysicsComponent` (ids de la piste), `RaceFlowCoordinatorComponent` et `RaceCourseDebugComponent` ;
    3. les ombres de l'environnement : activées selon l'option, 4 096, 150, biais 0,001 et 0,1 ;
    4. trois lumières directionnelles blanches, `Light.Key` (0,92, spéculaire, seule à projeter des ombres quand l'option est active), `Light.Fill` (0,71) et `Light.Back` (0,36, spéculaire) ;
    5. les générateurs de `LegacyTrackSceneFactory.Create` (`RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs:22-65`) ;
    6. le décor `Track.Scenery.<Modèle>.<index:000>`, `StaticModelComponent` par `StaticModelAssetId`, ombres projetées ;
    7. `Checkpoint.<n:00>` (`RaceCheckpointTriggerComponent`, demi-dimensions calculées) ;
    8. `PlayerStart` à la pose de départ ;
    9. `PlayerCar` : `RaceCarEntityFactory.Create()`, puis l'état choisi (`:277-293`).
  - **carrousel** : `CarSelectionCarousel` (`RacingGameCasaEngine/UI/CarSelectionCarousel.cs`) construit son propre monde `FrontEnd.CarSelectionCarousel` :
    - `Carousel.Light` ;
    - `Carousel.Plate.<i>` (`StaticModel.CarSelectionPlate`, sans ombre) ;
    - `Carousel.Car.<i>` (modèles peints, à l'exécution) ;
    - une caméra hors du monde, en (0, 2,75, −10,45) vers (0, −1, 0) ;
    - les positions et rotations sont recalculées à chaque image.
- **Chargement par le moteur** :
  - `GameManager.SetWorldToLoad(string)` → `UpdateWorld` :
    - résout le chemin par `AssetCatalog.GetByFileName` (clé exacte), et lève une exception s'il est inconnu ;
    - vide le monde sortant (`World.Clear`), ce que `SetWorldToLoad(World)` ne fait pas ;
    - appelle `LoadCopy<World>`, `LoadContent`, `BeginPlay`, puis `WorldLoaded` (`GameManager.cs:84-152`).
  - Le premier monde exige `FirstWorldLoaded` quand aucun monde n'est posé (`GameManager.cs:73-81`). `Program.cs:49` ne fixe que `ProjectName`, et le fichier projet nomme `Worlds/Editor.world`, réservé à l'éditeur.
  - Un monde ou une entité chargés prennent **le nom de leur entrée de catalogue** (`AssetContentManager.cs:235-240`). `IsRaceWorld` (préfixe) et l'égalité avec `FrontEndWorldName` servent dans 7 validateurs, le jeu et le lieur.
  - `World.SpawnEntity<T>(string assetName)` cherche l'entrée par son **nom** de catalogue, fait `LoadCopy` puis `Clone`, et met l'entité en file : elle n'entre dans le monde qu'à la prochaine mise à jour (`World.cs:153-173,774-802`).
  - `runtimeContext.ProjectSettings`, que `Program.cs` règle, est l'objet que lit `GameManager` (`GameSettings.ProjectSettings`, `EngineRuntimeContext.cs:55`).
- **Ce qu'un enregistrement garde** (`CasaEngine.EditorServices/EditorEntityJsonSerializer.cs`) :
  - le transform et les enfants de tout `SceneComponent` ;
  - les cas propres aux lumières (complets), aux caméras 3D (sans la cible), aux `StaticModelComponent` (id du modèle, ombres) et aux `PlayerStartComponent` ;
  - pour les autres composants, l'id, le nom et le type, puis les `[SerializedProperty]` ;
  - une racine absente est écrite `"null"`, comme `Track.Physics` du monde de test.
- **Pertes** pour les mondes du jeu, toutes deux sans attribut :
  - les demi-dimensions des checkpoints (défauts 6 / 4 / 2,5 ; le jeu les calcule) ;
  - `RacingCarAnchorComponent.AnchorId`, par lequel la voiture retrouve ses ancres.
  `[SerializedProperty]` accepte `string`, `float` et `Quaternion` (`SerializedProperties.cs:43-47`). Les autres composants sont créés avec leurs valeurs par défaut, et la cible de `BootstrapCamera` est l'origine, la valeur par défaut.
- **Ce que le jeu règle à l'exécution, après le chargement** (`RacingGameCasaEngineGame.cs:255-263`) :
  - le lieur de course : le contrôleur, `Possess`, la session, et le profil et le mode de la voiture ;
  - le ciel, avec son cube de réflexion à l'exécution ;
  - les options d'affichage, le curseur et la visibilité.
  L'option des ombres et la lumière clé ne sont réglées qu'à la création du monde (`RaceWorldFactory.cs:110-118,176`).
  - **Ciel** : `ConfigureCurrentWorldSky` (`RacingGameCasaEngineGame.cs:273-303,323-346`) :
    - remet `SpecularEnvironmentCubemapAssetId` à vide ;
    - charge `Textures/SkyCubeMap.dds` par son chemin (`RaceSkySystem.ResolveLegacySharedSkyCubePath`), avec `TextureCubeLoader` ;
    - en fait le cube de réflexion et le pipeline de ciel des mondes de course (`LegacySkyCubeViewPipeline`, teinte 232). Un ciel procédural prend le relais si le fichier manque.
    Le catalogue a l'entrée `Texture.SkyCubeMap` (`Textures/SkyCubeMap.dds`). `WorldEnvironmentSettings` enregistre `specular_cubemap_asset_id`, que le résolveur d'environnement du moteur utilise quand il n'est pas vide (`EnvironmentResolver.cs:54-55`). `LegacyCarVisualComponent` lit l'indice et la couleur de la voiture à son `InitializeWithWorld` (`:18-22,52`).
- **Ids** : `ObjectBase.Id` vaut `Guid.NewGuid()` par défaut, avec un setter privé. L'outil de cuisson fixe déjà les ids d'assets par réflexion (uuid5, `CookPlan.cs:233`) et écrit par `EditorAssetWriterService.SaveAsset`.
- **Écriture d'un monde** :
  - `EditorAssetWriterService.SaveAsset` passe par `EditorAssetJsonSerializer` (cas `World` et `Entity`). `EditorEntityJsonSerializer.SaveWorld` n'écrit que la liste privée `World._entityReferences`, qu'il lit par réflexion (`EditorEntityJsonSerializer.cs:18,21-38,162-165`). Il ignore donc les entités ajoutées par `World.AddEntity`, qui ne sont qu'en file (`World.cs:168-173`) ;
  - une référence en ligne (`asset_id` vide) écrit son entité par `SaveEntity` (`:167-185`). `EntityReference.Entity` a un setter interne (`EntityReference.cs:17`) ;
  - la seule voie publique qui remplit cette liste, `EditorWorldEditingService.AddEntity` → `World.AddEntityReferenceImmediate` (`World.cs:1015-1025`), appelle `Initialize` et `InitializeWithWorld`. Sans jeu, `StaticModelComponent.InitializeWithWorld` échouerait sur `world.Game.AssetContentManager` (`StaticModelComponent.cs:202-220`), et les générateurs construiraient leurs maillages ;
  - `LegacyTrackSceneFactory` lit le catalogue global (`AssetCatalog`, `LegacyTrackSceneFactory.cs:24-25,83`, `.Models.cs:41`) et les fichiers sous `EngineEnvironment.ProjectPath` (`:398-403`). Pendant l'écriture, l'outil de cuisson fait pointer ce chemin vers un dossier temporaire (`Cooker.cs:54-77`) ;
  - la taille d'un modèle vient de `ComputeLegacyModelSize`, privée (`LegacyTrackSceneFactory.cs:326`), sur le `StaticModel` qu'`Acquire` charge (`.Models.cs:61-78`). L'outil sait déjà charger un `.staticModel` sans jeu, par `StaticModel.Load` du JSON (`Verifier.cs:97-98`).
- **Catalogue** : `generate_rgce_content_assets.py` catalogue les `Worlds/**/*.world` en `World.<chemin>` et vérifie que l'id du fichier est celui de l'entrée. Il n'a aucune règle pour `.entity`.
- **Éditeur** : `--save-project` enregistre les documents modifiés (entités comprises) et le monde courant. `--open-asset` passe par `TryOpenEditorAsset`. L'ouverture d'un `.entity` par l'automatisation n'a pas encore été éprouvée.

### Propositions de S6

| Réf | Proposition |
|---|---|
| P11 | **Constructeurs dans la DLL.** La construction des entités des mondes passe de `RaceWorldFactory` (exe) à `RacingGame.Gameplay` (`Race/RaceWorldTemplates.cs`), sans changer une valeur. Le jeu s'en sert jusqu'à T6.4, puis seul l'outil d'écriture s'en sert. `LegacyTrackSceneFactory.Create` reçoit la fonction de taille des modèles en paramètre. |
| P12 | **Outil d'écriture** : `RgceAssetCooker --worlds`, qui référence déjà `RacingGame.Gameplay` et les sérialiseurs de l'éditeur. Il écrit :<br>• `Worlds/FrontEnd.world` ;<br>• `Worlds/Race/TrackBeginner.world`, `TrackAdvanced.world` et `TrackExpert.world` ;<br>• `Worlds/CarSelection.world` ;<br>• `Entities/PlayerCar.entity`.<br>La taille des modèles vient des `.staticModel` cuits, par le calcul du jeu (`ComputeLegacyModelSize`). Les ids sont des uuid5 du nom de l'asset, de l'entité et du chemin du composant : un second lancement n'écrit rien. Un nouveau lancement écrase les modifications faites dans l'éditeur, comme pour les assets cuits (ADR-0026).<br>**Mécanisme**, sans changer le moteur :<br>• pour chaque entité des constructeurs, dans leur ordre, l'outil crée une `EntityReference` en ligne (`AssetId` vide) ;<br>• il lui donne son entité par réflexion (setter interne de `Entity`), puis l'ajoute par réflexion à `World._entityReferences`, le champ que lit le sérialiseur de l'éditeur ;<br>• aucune entité n'est initialisée : ni `World.AddEntity`, ni `EditorWorldEditingService`.<br>**Sans jeu** : l'outil charge le catalogue global (`AssetCatalog.Load`) et fait pointer `EngineEnvironment.ProjectPath` vers `Content` pendant le calcul. Il calcule tous les mondes d'abord, puis les écrit par le dossier temporaire de l'outil.<br>**Taille des modèles** : `StaticModel.Load` du `.staticModel` cuit, puis `ComputeLegacyModelSize`. Ce sont le même fichier et le même calcul que le jeu, qui passe par `Acquire`. |
| P13 | **Nom des mondes** : un monde chargé porte le nom de son entrée. Les constantes deviennent ces noms : `FrontEndWorldName` = `World.FrontEnd`, et le préfixe des courses `World.Race.`, d'où `World.Race.TrackBeginner`. Les noms d'entités ne changent pas. |
| P14 | **Ombres et ciel** :<br>• **ombres** : un monde enregistré porte les ombres activées, et sa lumière clé en projette. `OnWorldLoaded` applique ensuite l'option des ombres aux deux, comme le faisait la création ;<br>• **ciel** (D2) : chaque monde de course porte dans son `environment` `specular_cubemap_asset_id` = l'id de `Texture.SkyCubeMap`. Le front-end et le carrousel n'en portent pas ;<br>• à `OnWorldLoaded`, `ConfigureCurrentWorldSky` lit cet id et résout son fichier par le catalogue. Il charge ce cube comme aujourd'hui, en fait le cube de réflexion et le pipeline de ciel, puis remet l'id à vide comme aujourd'hui. Le moteur ne charge donc pas le cube lui-même, et l'image ne change pas ;<br>• un monde de course sans id prend le ciel procédural, comme aujourd'hui quand le fichier manque. Changer l'id dans l'éditeur change donc le ciel de la course. |
| P15 | **Voiture** (P6) : les mondes de course n'ont pas de `PlayerCar`. Le lieur appelle `SpawnEntity<Entity>("Entity.PlayerCar")`, renomme l'entité `PlayerCar` et règle son état (indice, couleur, piste, profil, mode, vitesse) avant qu'elle n'entre dans le monde, puis la possède. |
| P16 | **Carrousel** : `CarSelection.world` contient `Carousel.Light` et les trois `Carousel.Plate.<i>`. Le carrousel le charge par `LoadCopy<World>`. Restent en code :<br>• les voitures, peintes à l'exécution ;<br>• la caméra, dont la cible n'est pas enregistrée ;<br>• le placement à chaque image ;<br>• le cube de réflexion, celui de la course, que le carrousel reçoit du jeu. |
| P17 | **Catalogue** : `generate_rgce_content_assets.py` catalogue aussi `Entities/**/*.entity` en `Entity.<chemin>`, avec la même vérification d'id que pour les mondes. |

### Prérequis, risques et arrêts de S6

- **Prérequis** : S5 close (✅) ; plan O7 exécuté, références `artifacts/o7-track-helpers/after/` (T3.1 en 🧪 pour le seul contrôle visuel de l'auteur).
- **Références de parité** : `after/` (export, audit, audits des voitures, captures d'interface), avec le témoin construit à `HEAD` quand les captures d'interface sont bruitées.
- **Arrêts** (en plus de ceux de l'enveloppe) :
  - un écart inexpliqué de l'export par maillage contre `after/` ;
  - un audit des voitures qui change ;
  - un champ perdu à l'aller-retour d'un monde ;
  - un `.entity` que l'automatisation de l'éditeur n'ouvre pas : la tâche reste en 🧪, avec l'étape manuelle écrite.
- **Risques** :
  - **`World.Clear`** au changement de monde : le jeu ne vidait jamais le monde de course. Il faut vérifier les tampons des générateurs, les sons et la physique au retour au front-end, par le smoke et deux courses enchaînées ;
  - **coût de chargement** : Expert compte 729 entités de décor en JSON ;
  - **clone** : `SpawnEntity` clone la voiture, et chaque composant doit copier son état (`Clone` des composants de la voiture) ;
  - **noms courts** : les nouveaux types repassent `check_gameplay_type_names.py`.

### ⏳ T6.1 — Champs perdus à l'enregistrement

- **Objectif** : un checkpoint et une ancre de voiture gardent leurs valeurs après un aller-retour d'enregistrement.
- **Fichiers** :
  - `RacingGame.Gameplay/Components/RaceCheckpointTriggerComponent.cs` : `[SerializedProperty("half_width")]`, `half_height` et `half_depth` ;
  - `RacingGame.Gameplay/Components/RacingCarAnchorComponent.cs` : `[SerializedProperty("anchor_id")]`.
- **Validation** :
  - build à 0 erreur ;
  - une sonde du scratchpad enregistre puis recharge, par les sérialiseurs de l'éditeur et `Entity.Load`, un checkpoint aux demi-dimensions non par défaut et la voiture de `RaceCarEntityFactory.Create()` : mêmes valeurs, mêmes ancres ;
  - le jeu ne change pas : export identique à `after/scene-a.json`, et smoke à 0.
- **Commit** : `feat(racing-casa): save the checkpoint extents and the car anchor ids`

### ⏳ T6.2 — Construction des mondes dans la DLL (P11)

- **Objectif** : le jeu construit ses mondes avec du code de `RacingGame.Gameplay`, que l'outil d'écriture pourra appeler. Le rendu et le jeu ne changent pas.
- **Fichiers** :
  - `RacingGame.Gameplay/Race/RaceWorldTemplates.cs` (nouveau) : les entités du front-end, et celles d'une course sans la voiture, avec les valeurs exactes de `RaceWorldFactory` (caméra, `RaceRoot`, ombres, lumières, générateurs, décor, checkpoints, départ). Les lumières sont paramétrées par l'option des ombres ;
  - `LegacyTrackSceneFactory.Create(trackName, getModelSize)` : la taille des modèles devient un paramètre, et le jeu passe celle d'aujourd'hui. `ComputeLegacyModelSize` passe de `private` à `internal`, pour l'outil (`InternalsVisibleTo("RgceAssetCooker")`) ;
  - `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` : il appelle ces constructeurs, puis ajoute la voiture comme aujourd'hui.
- **Validation** :
  - build à 0 erreur ; `check_gameplay_type_names.py` à 0 ;
  - A4 contre `after/` : export identique, audit dans le seuil de 0,5, audits des voitures identiques, captures d'interface (témoin à `HEAD` si bruit), smoke et `--verify-legacy-import-profile` à 0.
- **Commit** : `refactor(racing-casa): build the world entities in RacingGame.Gameplay`

### ⏳ T6.3 — Les mondes et la voiture écrits en assets (P12, P16, P17)

- **Objectif** : les cinq mondes et `PlayerCar.entity` existent, catalogués, et s'ouvrent dans l'éditeur sans perte (A3).
- **Fichiers** :
  - `scripts/RgceAssetCooker/` :
    - `Program.cs` : l'option `--worlds` ;
    - `WorldWriter.cs` (nouveau) : le mécanisme de P12 (références par réflexion, ids uuid5, catalogue global, chemin du projet, taille des modèles) ;
    - le README ;
  - `scripts/generate_rgce_content_assets.py` : `Entities/**/*.entity` (P17) ;
  - `RacingGameCasaEngine/Content/Worlds/FrontEnd.world`, `Worlds/Race/Track*.world`, `Worlds/CarSelection.world`, `Entities/PlayerCar.entity`, et le catalogue.
- **Validation** :
  - build de l'outil à 0 erreur ;
  - un second lancement n'écrit rien, et `generate_rgce_content_assets.py --check` est à 0 ;
  - **outil** : son journal ne contient aucune exception ni erreur d'initialisation ;
  - **nombre d'entités** : chaque `.world` écrit a autant d'entrées `entity_references` que ses constructeurs font d'entités. Le front-end en a 2. Une course compte la caméra, `RaceRoot`, 3 lumières, ses générateurs, son décor (323 / 265 / 729, Advanced / Beginner / Expert), ses 3 checkpoints et `PlayerStart`. `CarSelection.world` en a 4 ;
  - **contenu** : une sonde compare chaque monde écrit, rechargé par `World.Load`, aux entités que construisent les constructeurs de T6.2. Mêmes noms dans le même ordre, mêmes composants, transforms à 1e-6 près, mêmes ids de modèles, mêmes valeurs de lumières, d'ombres et de checkpoints. Chaque monde de course porte `specular_cubemap_asset_id` = `Texture.SkyCubeMap`, le front-end et le carrousel un id vide ;
  - **décor contre `after/`** : pour chaque piste, les entités `Track.Scenery.*` du monde écrit ont les modèles et les transforms de l'export `after/scene-a.json` ;
  - **éditeur (A3)**, sur des copies de l'éditeur et du contenu :
    - chaque monde et la voiture s'ouvrent avec le code 0 ;
    - les captures des copies réduites (0,02) sont lues ;
    - **mondes** : chaque `.world` ouvert devient le monde courant, et deux `--save-project` le réécrivent avec les octets de l'outil, sans champ perdu (comparaison JSON) ;
    - **voiture** : `--save-project` ne réécrit pas un `.entity` ouvert sans modification. Une sonde du scratchpad charge donc `PlayerCar.entity` par `Entity.Load` (le chargeur du moteur), puis le réenregistre deux fois par `EditorAssetWriterService.SaveAsset` dans une copie. Elle compare les octets à ceux de l'outil et les champs en JSON, ancres comprises ;
    - **enregistrement de la voiture par l'éditeur lui-même** : étape manuelle écrite dans la note (ouvrir `PlayerCar.entity`, modifier un champ puis le rétablir, enregistrer, comparer les octets). La tâche reste en 🧪 jusqu'à ce que l'auteur l'ait faite, ce qui ne bloque pas T6.4.
    Si l'automatisation n'ouvre pas le `.entity`, ce contrôle rejoint l'étape manuelle.
  - Le jeu ne charge pas encore ces fichiers : rien ne change à l'exécution.
- **Commit** : `feat(racing-casa): write the worlds and the player car as assets`

### ⏳ T6.4 — Le jeu charge le front-end et les courses (P13, P14, P15)

- **Objectif** : le jeu charge `FrontEnd.world` et les trois courses par le catalogue. La voiture vient de `PlayerCar.entity`. Rendu et jeu inchangés.
- **Fichiers** :
  - `RacingGameCasaEngine/Program.cs` : `runtimeContext.ProjectSettings.FirstWorldLoaded = "Worlds/FrontEnd.world"`, sans toucher au fichier projet, qui garde `Worlds/Editor.world` pour l'éditeur ;
  - `RacingGameCasaEngineGame.cs` et `RaceFrontEndFlow` : `SetWorldToLoad(chemin)`, avec la course choisie par le nom de la piste ;
  - `RaceWorldFactory` : les noms de P13. La construction des mondes en code disparaît ; les aides de nom et de visibilité restent ;
  - `RuntimeRaceWorldBinder` : la voiture par `SpawnEntity`, puis réglée avant d'entrer dans le monde (P15) ;
  - `OnWorldLoaded` : l'option des ombres et la lumière clé, et `ConfigureCurrentWorldSky`, qui prend son cube de l'id du monde (P14) ;
  - les validateurs, s'ils testent un nom de monde autrement que par ces constantes.
- **Validation** :
  - build à 0 erreur ;
  - A4 contre `after/` :
    - export identique (noms, matrices, matériaux), voiture comprise ;
    - audit dans le seuil de 0,5 ;
    - audits des voitures identiques ;
    - captures d'interface (témoin à `HEAD` si bruit) ;
  - placement des objets : la mesure de scène du banc de captures (`scripts/TrackCaptureCompare` contre le `scene.json` de l'original) rend tous les objets appariés, 0 manquant, 0 en trop (`scripts/TrackPlacementExporter` a été supprimé le 2026-11-17, `rgce-render-gaps-tasks.md` R1) ;
  - `--smoke-frontend`, qui enchaîne front-end et course, et `--verify-legacy-import-profile` : code 0 ;
  - une sonde enchaîne deux courses puis revient au front-end : aucune erreur dans le journal, aucun son de course après le retour ;
  - l'option des ombres désactivée : la course n'a ni carte d'ombres ni lumière qui en projette.
- **Commit** : `feat(racing-casa): load the front end and race worlds from the catalogue`

### ⏳ T6.5 — Le carrousel charge `CarSelection.world` (P16)

- **Fichiers** : `RacingGameCasaEngine/UI/CarSelectionCarousel.cs`.
- **Validation** :
  - build à 0 erreur ;
  - capture `car-selection` égale à `after/` (ou au témoin à `HEAD`) ;
  - smoke à 0.
- **Commit** : `feat(racing-casa): load the car selection lights and plates from a world`

### ⏳ T6.6 — ADR, documentation et clôture de S6

- **Fichiers** :
  - une ADR des mondes en assets (P11 à P17), et son index ;
  - `RacingGameCasaEngine/Assets/README.md` : les mondes, la voiture, l'outil, l'aller-retour ;
  - ce plan.
- **Validation** : vérificateur indépendant de S6, CONFIRMED.
- **Commit** : `docs(adr): record the worlds and the player car as assets`

---

## Phase 7 — Contrôle de l'éditeur et clôture (S7)

> Détail écrit le 2026-11-21, sur les décisions D14 à D16 et les points P22 à P29. À relire par un vérificateur de plan, puis à approuver par l'auteur avant exécution.
>
> Première relecture (2026-11-21) : **REVISE**, cinq constats, tous corrigés : P26 exigeait dans le diagnostic une ligne `Restored edited` qui n'y est jamais écrite (remplacée par la ligne `Updated …` et les empreintes) ; P23 laissait passer un aperçu au contenu illisible, signalé par un seul `[Warning]` (un `[Warning]` qui nomme le fichier fait échouer, les autres sont classés en T7.5) ; les témoins n'exerçaient qu'une condition sur plusieurs (un témoin par condition en T7.1 et en T7.3) ; A1 cherchait 18 classes dont deux ne peuvent pas être proposées (ensemble attendu : 16) ; P25 nommait un écrivain interne au moteur (remplacé par `EditorAssetWriterService.SaveAsset`, public).
>
> Seconde relecture (2026-11-21, relecteur frais) : **REVISE**, trois constats. Traitement par la session principale, puis une seule relecture de clôture :
> - `FIX` : l'aller-retour des mondes et de la voiture pouvait passer à vide, les composants du jeu chargés comme `UnresolvedComponent` se réécrivant tels quels. P25 exige le chargement sous le vrai type, avec un témoin « type non résolu ».
> - `FIX` : P23 disait à tort qu'un `[Warning]` est la seule trace d'un aperçu illisible ; un `.wav`, un `.fnt` ou un `.dds` à l'en-tête faux ne laisse rien dans le diagnostic. P30 ajoute la lecture des en-têtes des 250 assets d'aperçu hors de l'éditeur, avec son témoin ; le glTF sans maillage reste hors de vue, et T7.5 le dit.
> - `FIX`, par réduction : T7.4 demandait les octets enregistrés d'une texture et d'un modèle, que l'éditeur restaure avant qu'on puisse les lire. Le contrôle piloté de ces deux types est borné à « appliqué, relu du fichier enregistré, restauré » ; leur enregistrement sans perte repose sur P25, comme pour les sprites.
>
> Relecture de clôture (2026-11-21, relecteur frais) : **REVISE**, un constat bloquant et quatre remarques. La tranche **n'a donc pas été déclarée prête** par cette relecture. Traitement, sans nouvelle relecture automatique (la règle n'en permet qu'une de clôture) :
> - `FIX` : P25 ne disait pas comment écrire un monde hors du jeu ; tel qu'écrit, les 7 mondes auraient échoué sur un projet correct (un monde chargé hors du jeu n'a pas d'entités, `SaveAsset` aurait levé une exception). P25 prescrit maintenant la méthode que l'outil emploie déjà (monde et entités chargés séparément, `EditorWorldWriter.SaveWorld`), et T7.3 exige pour chaque monde un nombre de composants supérieur à zéro.
> - `FIX`, remarques : les empreintes de la copie excluent partout `.casaeditor/` ; les témoins à type de composant altéré prennent un nom qu'aucun assemblage ne déclare ; l'outil sans option, qui écrit dans `Content`, n'est plus lancé en T7.3 ; le BMFont binaire est nommé en P23.
> - Confirmé par ce relecteur : le chargement de l'assemblage du jeu, le journal et la détection des composants non résolus sont faisables depuis l'outil ; rien d'autre ne se dégrade en silence au chargement d'un monde ; `AssetPreviewReader.Read` est public et sans périphérique graphique ; les comptes concordent (372 + 250 = 622) ; rien ne dépasse D14 à D16.
> - Ces corrections n'ont pas été relues. La tranche est présentée à l'auteur avec ce fait : il l'approuve telle quelle ou demande une relecture de plus.

Faits relevés pour cette phase (2026-11-21, jeu `5e5d17a` = `remaster` = `origin/remaster`, moteur `01cdf483f` = `main` = `origin/main`, arbres propres) :

- **Catalogue** : `RacingGameCasaEngine/Content/AssetInfos.json` a 622 entrées (clés `id`, `name`, `file_name`, `asset_type`), réparties comme en P24.
- **Ouverture d'un asset** : les routes de l'éditeur (`CasaEngine/CasaEngine.Editor/GameEditor.cs:4976-4992`) donnent un document aux `.uiscreen`, `.entity`, `.sprite`, `.sound`, `.material`, `.texture`, `.staticmodel`, `.world` ; tout autre fichier qui n'est pas un format du moteur s'ouvre en aperçu en lecture seule (`CasaEngine/docs/editor/asset-documents.md:9-21` : image, cube, audio, police, modèle, texte, binaire). **Un aperçu s'ouvre et écrit sa ligne même si le contenu est illisible** (`GameEditor.cs:5586-5638`) : selon le cas, le défaut apparaît par un `[Warning]` qui nomme le fichier (`AssetPreviewPanel.cs:297`), ou par le seul texte d'état du panneau, qui n'atteint pas le diagnostic (`AssetPreviewPanel.cs:244-247` ; détail en P23). **Un monde dont un type de composant est inconnu s'ouvre avec le code 0** et un `[Error]` « Unknown component type » (`CasaEngine/CasaEngine/Framework/Assets/ElementFactory.cs:324-325`).
- **Automatisation** : options `--project`, `--open-asset`, `--diagnostics-out`, `--automation-timeout`, `--set-material-property`, `--set-texture-sampler`, `--set-static-model-material`, `--set-screen-property`, `--save-project`, `--keep-open`, `--remote` (`CasaEngine/CasaEngine.Editor/EditorAutomationOptions.cs:150-395`). Le code de sortie d'un lancement vaut 0, 1 ou 2 (`EditorAutomationOutcome.cs`) : 1 dès qu'une ligne de diagnostic commence par `[Automation] Unable to open asset`, `[Automation] Failed to `, `[Automation] Refused to ` ou `[Automation] Timed out`. L'automatisation restaure en fin de lancement les fichiers qu'elle a modifiés (`GameEditor.cs:10540-10627`) ; elle le fait après avoir écrit le fichier de diagnostic (`GameEditor.cs:8388-8389`), qui ne garde de toute façon du journal que les `[Warning]` et les `[Error]` : les lignes `Restored edited …` n'y figurent donc pas.
- **Durée** : un lancement de l'éditeur sur un monde prend 6 à 8 secondes (`artifacts/worlds-as-assets/m5/roundtrip-worlds.log`) ; 622 lancements font de l'ordre d'une heure et demie.
- **Écrivain** : `EditorAssetJsonSerializer.TrySerialize` (`CasaEngine/CasaEngine.EditorServices/EditorAssetJsonSerializer.cs:31-117`) écrit `Entity`, `World`, `MaterialAsset`, `SpriteData`, `UIScreenAsset`, `StaticModel`, `Texture`, `SoundAsset`. Cette classe est interne ; la voie publique est `EditorAssetWriterService.SaveAsset` (`EditorAssetWriterService.cs:48-71`), qui l'appelle et écrit sous `EngineEnvironment.ProjectPath`, pour sept des huit types ; un monde chargé hors du jeu n'a pas d'entités, et s'écrit par `EditorWorldWriter.SaveWorld(World, IReadOnlyList<Entity>)` à partir d'entités chargées une à une, comme le fait déjà l'outil (P25).
- **Composants** : l'éditeur propose à l'ajout toute sous-classe concrète d'`EntityComponent` à constructeur sans paramètre, non marquée `[Browsable(false)]` (`CasaEngine/CasaEngine.Editor/Runtime/EditorWorldOperations.cs:37-44`) ; la commande `component.types` rend cette liste par nom d'affichage. `RacingGame.Gameplay` déclare 18 classes de composants, dont les quatre générateurs de D5 (`RaceRoadGeneratorComponent`, `RaceGuardRailGeneratorComponent`, `RaceColumnGeneratorComponent`, `RaceTerrainGeneratorComponent`). Deux ne peuvent pas être proposées : `RaceTrackGeneratorComponent` est abstraite (`RaceTrackGeneratorComponent.cs:30`) et `RaceGeneratedModelComponent` est `[Browsable(false)]` (`RaceGeneratedModelComponent.cs:10-11`) ; les 16 autres sont concrètes, sans constructeur à paramètres.
- **Pilotage** : scripts de la session du 2026-11-19, ignorés par git, `artifacts/entity-asset-remote/scripts/editorctl.py` (lancer l'éditeur avec `--remote`, appeler `casa-editor`, l'arrêter) et `m9_run.py`.
- **Parité** : 76 images identiques à `artifacts/worlds-as-assets/m1/ref/` sur le moteur d'aujourd'hui (plan du moteur `origin-main-integration-2-tasks.md`, T3.1).

Règles propres à cette phase, en plus des règles du plan : aucun fichier de `Content`, du moteur ni du jeu (hors `scripts/` et documents) n'est modifié ; un contrôle qui échoue est noté, pas corrigé (D15) ; un seul lancement de l'éditeur ou du jeu à la fois sur la machine, sous le verrou de P28, jamais pendant un build ; **toute empreinte de la copie prise dans cette phase exclut `.casaeditor/`**, que l'éditeur réécrit à chaque fermeture (`GameEditor.cs:535-540`) ; les réglages de l'utilisateur sous `%LOCALAPPDATA%/CasaEngine/RacingGameCasaEngine/` ne sont jamais modifiés, et leurs empreintes sont celles de M1 avant tout rejeu du jeu.

### ⏳ T7.1 — Script de contrôle d'ouverture (A2)

- **Objectif** : un script versionné qui ouvre chaque asset du catalogue dans l'éditeur et rend un verdict par asset.
- **Fichiers** : `scripts/check_editor_opens_assets.py` (nouveau) ; ce plan.
- **Étapes** :
  - le script lit `AssetInfos.json` de la copie, lance l'éditeur une fois par entrée selon P23, sous le verrou de P28, avec un délai borné, et écrit dans un dossier de sortie un fichier `results.json` (par asset : nom, fichier, type, code de sortie, ligne d'ouverture trouvée ou non, sorte de panneau, nombre d'`[Error]` et de `[Warning]`, durée, verdict) et un résumé par type ; options `--only <fichier>`, `--type <extension>`, `--resume` (ne relance pas un asset déjà jugé) ; il sort en 1 dès qu'un asset échoue ;
  - `dotnet build RacingGame.slnx` et `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln`, puis copie de `RacingGameCasaEngine/Content` vers `D:/development/rgce-s7/content` ;
  - essai sur quatre assets (un monde, une texture, un `.png`, un `.gltf`) ;
  - **un témoin négatif par condition de P23**, chaque fichier touché étant remis ensuite : (a) code de sortie : une entrée dont le fichier est retiré de la copie ; (b) `[Error]` avec le code 0 : un `.world` de la copie dont un nom de type de composant est remplacé par un nom qu'aucun assemblage ne déclare (pas un simple changement de casse : la recherche ignore la casse, `ElementFactory.cs:394-398`) ; (c) `[Warning]` qui nomme le fichier : un `.png` de la copie remplacé par des octets qui ne sont pas une image ; (d) ligne d'ouverture absente : la fonction de verdict du script appelée sur le diagnostic enregistré d'un asset passé, dont la ligne `[Automation] Opened asset=` est retirée.
- **Validation** : les quatre essais passent ; les quatre témoins échouent, chacun par la condition qu'il vise (la condition est écrite dans `results.json`), et le script sort en 1 ; `git status --short` ne nomme que le script et ce plan ; une fois les fichiers des témoins remis, l'empreinte de chaque fichier de la copie (hors `.casaeditor/`) est celle d'avant les essais.
- **Arrêts** : l'éditeur ne s'ouvre pas sur le projet ; un témoin passe, ou échoue par une autre condition que la sienne (le critère ne distingue rien).
- **Commit** : `feat(scripts): check that the editor opens every catalogued asset`

### ⏳ T7.2 — Mesure : le projet et les 622 assets dans l'éditeur (A1, A2)

- **Objectif** : savoir ce qui s'ouvre et ce qui ne s'ouvre pas.
- **Fichiers** : ce plan (sorties sous `artifacts/editor-closure/a2/`, ignorées par git).
- **Étapes** :
  - A1 : éditeur lancé avec `--remote` sur la copie ; `editor.status` nomme le projet ; `component.types` est relevé ; l'ensemble attendu est celui de la règle de l'éditeur appliquée aux sources du jeu, soit les 16 classes proposables, cherchées par le nom d'affichage que leur donne `CreateComponentTypeLookup` (`EditorWorldOperations.cs:509`) ; l'absence de `RaceTrackGeneratorComponent` et de `RaceGeneratedModelComponent` est attendue et constatée ; **échec A1** : une des 16 classes absente de la liste, ou une ligne `[Error]` ; `world.open` d'un monde de course, puis `logs.read` : aucune ligne `[Error]` (une ligne « Unknown component type » montrerait que la DLL du jeu n'est pas chargée) ;
  - A2 : le script de T7.1 sur les 622 entrées ; les échecs sont classés par cause, à partir de leurs lignes de diagnostic.
- **Validation** (c'est une mesure : la tâche est faite quand la mesure est complète et notée, quel que soit le nombre d'échecs) : 622 verdicts dans `results.json`, aucun asset sans verdict ; pour chaque type, le nombre d'assets passés et échoués ; pour chaque cause d'échec, la ligne de diagnostic et la liste des assets ; les 16 classes attendues présentes ou non dans `component.types`, et les deux absences attendues ; chaque texte distinct de `[Warning]` relevé, avec son nombre d'occurrences ; la copie intacte après la mesure (empreintes).
- **Arrêts** : une empreinte de la copie qui change (l'éditeur écrit en ouvrant) ; plus de 5 % de délais dépassés (la mesure ne vaut rien, le délai est à revoir avec l'auteur).
- **Commit** : `docs(ai-agent): record the editor opening check of every asset`

### ⏳ T7.3 — Mesure : aller-retour d'enregistrement et en-têtes d'aperçu, hors de l'éditeur (P25, P30)

- **Objectif** : savoir si les écrivains du moteur réécrivent les 372 assets éditables sans perte, et si les 250 assets d'aperçu ont un en-tête lisible.
- **Fichiers** : `scripts/RgceAssetCooker/RoundTripChecker.cs` (nouveau), `scripts/RgceAssetCooker/Program.cs` (option `--roundtrip`), `scripts/RgceAssetCooker/README.md` ; ce plan.
- **Étapes** : le mode de P25 et de P30 ; un rapport par asset éditable (stable ou non au second enregistrement ; clés perdues ; valeurs changées ; clés ajoutées ; pour les mondes et la voiture, nombre de composants chargés sous leur vrai type et nombre de non résolus) ; un rapport par asset d'aperçu (sorte, `Problem`) ; un résumé par type, sous `artifacts/editor-closure/roundtrip/`.
- **Validation** : `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj` à 0 erreur ; `--verify`, `--check` et `--worlds --verify` rendent ce qu'ils rendaient (0 écart) ; l'outil sans option, qui cuit et écrit dans `Content`, n'est pas lancé ; `--roundtrip` rend un verdict pour chacun des 372 assets éditables et des 250 assets d'aperçu ; pour chacun des 7 mondes et pour la voiture, un nombre de composants chargés sous leur vrai type supérieur à zéro, zéro composant non résolu et aucune erreur au journal de l'outil, sinon c'est un échec mesuré ; **un témoin par contrôle**, sur des copies d'un matériau dans le dossier de sortie : (a) une clé retirée du fichier écrit avant la comparaison est signalée « clé perdue » ; (b) une valeur changée dans le fichier écrit est signalée « valeur changée » ; (c) la seconde écriture altérée d'un octet est signalée « instable » ; (d) une copie d'un `.world` dont un nom de type de composant est remplacé par un nom qu'aucun assemblage ne déclare est signalée « type non résolu » ; (e) une copie d'un `.wav` remplacée par des octets qui ne sont pas un RIFF est signalée « aperçu illisible » ; `git status --short` ne nomme que les trois fichiers de l'outil et ce plan, rien sous `Content`.
- **Arrêts** : l'outil devrait modifier le moteur ou initialiser un périphérique graphique pour charger un type (le type est noté non chargeable, D15) ; un témoin n'est pas signalé, ou l'est sous un autre nom que le sien.
- **Commit** : `feat(scripts): check the save round trip of the editable assets`

### ⏳ T7.4 — Mesure : aller-retour dans l'éditeur piloté, échantillon (P26)

- **Objectif** : voir l'éditeur lui-même modifier, enregistrer et rétablir un asset de chaque type qu'il sait piloter.
- **Fichiers** : ce plan (script et sorties sous `artifacts/editor-closure/driven/`).
- **Étapes** : sur la copie, selon P26 : un matériau, un son, un écran, `Entities/PlayerCar.entity`, un monde par les commandes à distance ; une texture et un modèle par les options d'automatisation.
- **Validation** (mesure) : pour chaque asset piloté par les commandes à distance, ce qui a été modifié, les octets changés à l'enregistrement, et l'état après rétablissement (identique à la source, ou différences listées) ; pour la texture et le modèle, le code de sortie 0, la ligne `[Automation] Updated …` avec la valeur relue du fichier enregistré, et les empreintes de la copie égales avant et après (P26) ; la voiture retrouve l'écart de huit valeurs admis en O2, le monde celui des deux clés de caméra d'O4, et rien d'autre ; à la fin la copie est remise à l'état de la source (empreintes), par restauration des fichiers touchés.
- **Arrêts** : une commande refusée par l'éditeur pour une raison que la documentation ne donne pas ; une écriture hors de la copie.
- **Commit** : `docs(ai-agent): record the driven save round trip of the editor`

### ⏳ T7.5 — Bilan et décision de l'auteur (D15)

- **Objectif** : rendre la liste des échecs et des pertes, classés par cause, avec une proposition.
- **Fichiers** : ce plan.
- **Étapes** : tableau par cause (ouverture, enregistrement hors éditeur, enregistrement piloté) : assets touchés, ligne de diagnostic ou différence, lieu probable de la cause (jeu, contenu, moteur), correction proposée et son coût.
- **Validation** : chaque échec de T7.2 à T7.4 est dans le tableau ; chaque texte distinct de `[Warning]` relevé en T7.2 et en T7.4 est classé « admis », avec sa raison, ou « échec ». Le bilan écrit aussi **ce que les contrôles ne voient pas** : un glTF sans maillage (P30) ; l'enregistrement d'une texture, d'un modèle ou d'un sprite depuis l'éditeur lui-même, prouvé seulement hors de l'éditeur (P25, P26).
- **Suite** : **s'il y a au moins un échec, une perte ou un `[Warning]` classé « échec », la tâche passe en ⚠️, la question est écrite dans « Points ouverts » et la session s'arrête** : les corrections sont un plan que l'auteur approuve (D15). S'il n'y en a aucun, la session continue avec T7.6.
- **Commit** : `docs(ai-agent): record the outcome of the editor checks`

### ⏳ T7.6 — Jeu et dépôt (A4, A5)

- **Objectif** : le jeu et le dépôt sont dans l'état que le plan exige à la clôture.
- **Fichiers** : ce plan (sorties sous `artifacts/worlds-as-assets/s7/` et `artifacts/editor-closure/a5/`).
- **Étapes** : A4 par la « Validation commune (parité) » de `rgce-worlds-as-assets-tasks.md`, points 1 à 6, contre la référence de M1 (P27), sous le verrou ; A5 : `RacingGameCasaEngine.csproj` ne lie plus rien depuis `RacingGame/Content` (lecture du fichier) ; `python scripts/generate_rgce_content_assets.py --check` à 0.
- **Validation** : `slice_check.py s7` rend `RESULT: PASS` ; A5 tenu.
- **Arrêts** : une image, un export ou un rapport qui diffère de la référence.
- **Commit** : `docs(ai-agent): record the game and repository checks of the editor project`

### ⏳ T7.7 — Documentation

- **Objectif** : la documentation dit comment contrôler le projet dans l'éditeur.
- **Fichiers** : `RacingGameCasaEngine/Assets/README.md` (section sur les deux contrôles et leurs commandes, niveaux d'ouverture par type) ; `scripts/RgceAssetCooker/README.md` si T7.3 ne l'a pas déjà complété ; `ai-agent/README.md` ; `ai-agent/HANDOVER.md` (section 15) ; ce plan.
- **Validation** : chaque commande citée a été lancée telle quelle dans cette tranche ; les liens ajoutés mènent à un fichier.
- **Commit** : `docs(racing-casa): document the editor checks of the project`

### ⏳ T7.8 — Vérification de clôture (A6)

- **Objectif** : un `verifier` frais confirme l'ensemble.
- **Fichiers** : ce plan, `ai-agent/README.md`, `ai-agent/HANDOVER.md`.
- **Étapes** : le vérificateur rejoue par lui-même : le script d'ouverture sur un échantillon de son choix d'au moins 40 assets couvrant tous les types, avec un témoin négatif de son invention ; `--roundtrip` en entier, avec une perte de son invention ; un aller-retour piloté de son choix ; la relecture des sorties de T7.2 à T7.6 contre les notes ; l'état des dépôts. Traitement des constats par la session principale ; rapport de fin ; suppression de `D:/development/rgce-s7/` (P28), chemin regardé avant.
- **Validation** : verdict CONFIRMED sur A1 à A5 tels que ce détail les définit ; aucun constat P0 à P2 ouvert.
- **Arrêts** : REFUTED ; un INCONCLUSIVE qui subsiste après une reprise.
- **Commit** : `docs(ai-agent): record the closing verification of the editor project`

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | **Levé (T1.2).** `--play-smoke` recompile le csproj de gameplay quand ses sources sont plus récentes que la DLL chargée, puis rapporte la DLL de jeu chargée. | T1.2 |
| O2 | **Levé (T1.2).** `RacingGame.Gameplay/CasaEngine.EnginePath.props`, propre à la machine, est ignoré par git. | T1.2 |
| O3 | Le sous-module contient un plan RGCE égaré, `CasaEngine/ai-agent/tasks/racinggame-car-profile-parity-plan.md`. On le laisse tel quel, la branche moteur le portera. | S2 |
| O4 | PNG d'interface hérités non portés : `HelpScreenWindows`, `HelpScreenXbox360`, `OptionsScreenXbox360`, `MouseCursor`. Faut-il compter aussi la copie brute de `GameFont.png` ? Elle est proposée, D4 voulant « tout porter ». | S3 |
| O5 | Les cartes de normales héritées gardent la disposition `agb`, et le jeu actuel les lit telles quelles. Le chantier conserve exactement ces pixels, sans réencodage. | S3, S5 |
| O6 | **Levé (T0.2).** Le panneau Logs de l'éditeur restait vide sur les captures d'automatisation. Sur décision de l'auteur, il a été corrigé (MGUI `4e94d7e8`, CasaEngine `c0f3343`), et l'option `--logs-filter` (`4ce5ea0`) met l'avertissement à l'écran. La sonde `pluginprobe`, d'abord proposée, n'a pas été retenue. | T0.1, T1.2 |
| O7 | **Levé (2026-10-09), par le plan [rgce-track-helpers-tasks.md](rgce-track-helpers-tasks.md) et l'ADR-0019.** Les aides et les CombiModel sont lus comme dans l'original. Il reste le contrôle visuel de l'auteur (T3.1 🧪) et la luminosité des tunnels (O3 de ce plan). **Décidé (D9) : corrigé avant S6.** Défaut ancien relevé en T4.5 : les éléments `WidthHelper`, `RoadHelper` et `CombiObject` des `.Track` et des `.CombiModel` ne correspondent pas aux types que lisait `XmlSerializer` (`LegacyWidthHelper`, `LegacyRoadHelper`, `LegacyCombiObject`). Le jeu ignore donc les aides de largeur et de route (palmiers, lanternes, tunnels) et le contenu des CombiModel. Le chantier garde ce comportement pour la parité ; le corriger change le rendu, c'est un chantier à part. | Après le chantier |
| O9 | **Levé (2026-11-18), par la décision D10 du plan [rgce-worlds-as-assets-tasks.md](rgce-worlds-as-assets-tasks.md) et l'ADR-0042 : les mondes de course et celui du carrousel portent l'id de la cubemap du ciel, le jeu le lit, et une sonde montre que l'id sert. Texte d'origine :** S6 en pause (2026-10-09). Le détail de S6 a eu trois relectures, toutes REVISE ; les deux premières ont été corrigées (Phase 6). Reste le ciel (P14) : les mondes de course portent `specular_cubemap_asset_id` = `Texture.SkyCubeMap`, mais rien ne prouve que le jeu s'en sert. Le carrousel charge le cube de la course au front-end, et ce cube est mis en cache par chemin, au premier essai.<br>**Correction proposée**, non relue :<br>(a) le cube du carrousel vient de l'entrée `Texture.SkyCubeMap`, résolue par le catalogue ;<br>(b) le cache des cubes est indexé par id d'asset ;<br>(c) T6.4 ajoute une sonde qui charge une course avec l'id vidé (ciel procédural), puis avec l'id réel (le cube), et montre que les images diffèrent ; la parité A4 se fait avec l'id réel.<br>**Autre voie** : restreindre D2 pour le ciel. Il resterait réglé en code, les mondes ne porteraient pas de cubemap, et le résultat de S6 serait corrigé.<br>**Question à l'auteur** : appliquer (a) à (c), avec une relecture puis l'exécution de S6 ; restreindre D2 pour le ciel ; ou relire le plan d'abord ? | S6 |
| O8 | **Levé (2026-10-08) : l'auteur a demandé une dernière relecture du brouillon corrigé, rendue READY, puis l'exécution.** S5 en pause (2026-10-08). Le détail de S5 a eu trois relectures, toutes REVISE ; les deux dernières ont relevé des P1, et la règle d'arrêt interdit de les lever par une nouvelle boucle sans l'auteur. Les blocages sont tous corrigés dans le brouillon (`s5_plan.md` du scratchpad). Ils portent sur trois points : (1) le compilateur nomme les états de rendu que le jeu laisse vides ; (2) la conception de la voiture et de sa peinture ; (3) les fonctionnalités de rendu d'un matériau compilé ignorent le maillage, d'où une carte de normales sans tangentes. Le point (3) demande une correction de CasaEngine (`RenderFeatureResolver`, `RenderItem.Features`) et un export qui enregistre les fonctionnalités et le matériau de chaque sous-maillage. S6 et S7 dépendent de S5. Question à l'auteur : relancer une revue sur le brouillon corrigé puis exécuter S5, ou relire le plan d'abord. | S5 |

## Hors périmètre

- **Play du parcours complet du jeu dans l'éditeur** : enchaînement des mondes, écrans XAML dans la vue, hub `RacingGameCasaEngineGame`. Le jeu se teste avec l'exe (D1).
- **Éditeur de tracé** : édition des points de contrôle d'une piste dans l'éditeur.
- **Le reste de l'ancien jeu** : shaders `.fx`, banques XACT et `Cube.X` ; la cubemap `SkyCubeMap.dds` et le pipeline de ciel du jeu restent.
- **Données de jeu tenues en code** : profils et couleurs des voitures, liste des pistes, règles de course, entrées, scores. Leur passage en assets de données serait un chantier suivant, à confirmer par l'auteur.
- **Lecture des nouveaux sons** par le jeu (moteur, freins, chocs, checkpoints, musiques).
- **Rendu** : brouillard, post-effets, glow, PBR ; tout ce qui changerait le rendu d'aujourd'hui.
- **Dépôt** : push, fusion, pull request.
