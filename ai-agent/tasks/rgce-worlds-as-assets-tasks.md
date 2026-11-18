# Plan agent IA — Mondes en assets (S6 du projet éditeur, remis à jour)

Ce plan reprend la tranche **S6** de `rgce-editor-project-tasks.md` (Phase 6, en pause depuis le 2026-10-09 sur la question O9). Son détail d'octobre décrivait le jeu d'avant le PBR ; il est refait ici sur le code d'aujourd'hui et **le remplace**. Les décisions D1 à D9 et les propositions P1 à P10 de `rgce-editor-project-tasks.md` restent en vigueur. Demande de l'auteur du 2026-11-18 : « Ensuite avec la suite », puis choix « Projet éditeur, S6 ».

Les décisions D10 à D13 ci-dessous ont été arbitrées avec l'auteur le 2026-11-18 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> Relecture de l'enveloppe (2026-11-18) : **REVISE**, quatre blocages, tous traités dans cette version. (1) Le banc dessine le décor de la graine 1 et le jeu celui d'une graine par piste : tranché par l'auteur (D13), tranche M0. (2) Le contrôle de parité s'appuyait sur un script qui ne compare pas tout octet pour octet : la parité est redéfinie avec son propre script (M1). (3) Le ciel du front-end : P18 précisé. (4) Résultat, prérequis, arrêts et retour arrière ajoutés à chaque tranche. Seconde relecture de l'enveloppe : **READY**.
>
> Relecture de la tranche M0 (2026-11-18) : **REVISE**, deux blocages, traités : la validation ne prouvait pas que la graine atteint l'audit et l'interface (contrôle des empreintes de placements ajouté, `SceneryRandomSeed` supprimée) ; l'ensemble « ce qui change » devient « ce qui peut changer », avec sa règle.

## Objectif

Le jeu ne construit plus ses mondes en code : le front-end, les trois courses et le monde du carrousel sont des `.world`, la voiture est un `.entity`, tous écrits par l'outil du dépôt, catalogués, ouverts et réenregistrés sans perte par l'éditeur, et chargés par le jeu.

Une seule chose change à l'écran, une fois, en M0 : le décor aléatoire du jeu prend la graine du banc (D13). Ensuite **le rendu et le jeu ne changent plus** : chaque tranche rejoue le banc à l'identique, octet pour octet.

## État vérifié du dépôt (2026-11-18, jeu `170f41c` = `remaster` = `origin/remaster`, moteur `da32906e2` = `main` = `origin/main`)

Faits relevés par une découverte en lecture seule sur cinq surfaces (rapports `s6-*.md` du scratchpad de la session), recoupés par relecture des points qui portent une décision, puis par la relecture de l'enveloppe.

- **Les mondes sont construits en code** (`RacingGameCasaEngine/Worlds/RaceWorldFactory.cs`) :
  - **front-end** (`CreateFrontEndWorld`, `:124-135`) : la caméra (`CameraLookAtComponent` en (0, 6, −18)) et `PlayerStart` en (0, 0, −4) ; aucun réglage d'environnement ;
  - **course** (`CreateRaceWorld`, `:137-200`), dans l'ordre : la caméra (avec `ChaseCameraRigComponent` et `DebugFreeCameraComponent`) ; `RaceRoot` (`RaceTrackPhysicsComponent`, `RaceFlowCoordinatorComponent`, `RaceCourseDebugComponent`) ; les ombres (`:151-162` : 4096, distance 100, fondu 15, profondeur 150, rayon 3, 64 lectures, `MultiplyLitColor`, couleur (0,38 ; 0,3952 ; 0,4104), biais 0,001 et 0,1) ; l'ambiant (`:168-171` : `Auto`, 0,05, intensités 1) ; le pipeline de couleur **seulement en mode `linear`** (`:173-177`, `:204-212` : `LinearHdr`, `AcesApprox`, exposition 0,9, FXAA, ni bloom ni SSAO) ; **une seule lumière** `Light.Key` (`:235-253`, intensité π, spéculaire 235) ; les entités de `LegacyTrackSceneFactory.Create` (générateurs puis décor) ; les `Checkpoint.NN` (`:330-348`) ; `PlayerStart` ; `PlayerCar` (`:350-366`) ;
  - dépendent d'une **option du joueur** : `Shadows.Enabled` et `CastShadows` de la lumière (`state.EnableShadows`, `:152`, `:179`), lus à la création seulement ; dépend de la **ligne de commande** : le pipeline de couleur (`--color-pipeline legacy|linear`, `Program.cs:83`).
- **Graine du décor** : `LegacyTrackSceneFactory` tire le décor aléatoire (palmiers, lanternes, bannières, panneaux, objets) avec `SceneryRandomSeed ?? RaceTrackLayout.CreateTrackSeed(trackName)` (`RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs:17-23`, `:81` ; graine FNV-1a du nom de la piste, `RaceTrackLayout.cs:270-282`). **Le mode `--capture-reference-views` pose `SceneryRandomSeed` = la graine de son `views.json`** (`RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs:108-112`), qui vaut 1 dans `artifacts/pbr-handover/s1c/ref/views.json` et `s3/ref-tunnel/views.json`. Les 15 vues et les vues de tunnel du banc montrent donc le décor de la graine 1 ; le jeu, l'audit, l'interface et l'export de scène celui de la graine par piste (ADR-0027). L'option `--seed 1` que `runref.sh` passe à l'audit n'est lue par rien.
- **Carrousel** (`RacingGameCasaEngine/UI/CarSelectionCarousel.cs:103-141`) : un monde à part, `FrontEnd.CarSelectionCarousel`, hors `GameManager` ; ambiant 0,05, ombres (2048, distance 16, biais 0,001 et 0,05), `Carousel.Light` (intensité π), trois `Carousel.Plate.<i>`, trois `Carousel.Car.<i>` peintes à l'exécution, une caméra ; placement recalculé à chaque image ; `Shadows.Enabled` et `CastShadows` réglés à chaque image (`:191-192`). Son cube de réflexion est celui de la course (`:108`).
- **Ciel** (`RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs:300-381`), réglé à chaque monde chargé par `ConfigureCurrentWorldSky`, selon `IsRaceWorld` : un monde de course reçoit le cube de réflexion à l'exécution (`SpecularEnvironmentCubemapAssetId` remis à vide), le pipeline `LegacySkyCubeViewPipeline` (shader hérité, teinte 232) et la couleur d'horizon ; **le front-end reçoit un cube nul, aucun pipeline et un fond noir** (`:303-317`). Le cube vient du fichier `Textures/SkyCubeMap.dds`, par son chemin (`RaceSkySystem.ResolveLegacySharedSkyCubePath`), chargé une fois par un drapeau unique ; un ciel procédural prend le relais si le fichier manque.
- **Changement de monde** : `SetWorldToLoad(World)` partout (`RaceFrontEndFlow.cs:98-108`, `RacingGameCasaEngineGame.cs:227-233`), qui ne vide pas le monde sortant. `SetWorldToLoad(string)` résout le fichier par `AssetCatalog.GetByFileName`, **vide le monde sortant** (`World.Clear`) et charge par `LoadCopy<World>` (`CasaEngine/CasaEngine/Framework/Application/GameManager.cs:95-168`, `:384-393`). Sans monde posé, `EndLoadContent` exige `ProjectSettings.FirstWorldLoaded` (`:77-92`).
- **Noms** : un monde chargé par `LoadCopy` prend le nom de son entrée de catalogue (`AssetContentManager.cs:235-240`). Les constantes `FrontEndWorldName` et `RaceWorldNamePrefix` (`RaceWorldFactory.cs:18-19`) servent dans le jeu, le lieur et huit validateurs, toujours par ces constantes ou par `IsRaceWorld`.
- **Voiture** : `RaceCarEntityFactory.Create()` (`RacingGame.Gameplay/Entities/RaceCarEntityFactory.cs:29-44`), puis l'état posé avant l'entrée dans le monde (`RaceWorldFactory.cs:356-363`) ; `LegacyCarVisualComponent` lit l'indice et la couleur à son initialisation. Le lieur (`RuntimeRaceWorldBinder.cs:29-76`) cherche la voiture dans le monde chargé, la place sur `PlayerStart`, crée le contrôleur et la possède. `World.SpawnEntity<T>(nom)` charge, clone et met l'entité en file : elle n'entre dans le monde qu'à la mise à jour suivante (`World.cs:167-189`, `:874-897`).
- **Ce qu'un `.world` enregistre** : au niveau du monde, tout l'environnement dont le jeu se sert est une clé enregistrée : `shadows` (toutes les valeurs, dont `mode`, `shadow_color`, `filter_radius`, `filter_taps`, `fade_distance`, `depth_range`), `ambient_*`, `specular_intensity`, `specular_cubemap_asset_id`, `color_pipeline` (12 clés, écrit quand il n'est pas par défaut) (`WorldEnvironmentSettings.cs:103-231`, `ShadowSettings.cs:124-259`, `ColorPipelineSettings.cs:145-231`). **Ne s'enregistrent pas** : les cubes posés à l'exécution et le pipeline d'une vue.
- **Ce qu'une entité enregistre** : transform et enfants, lumières, caméras 3D, `StaticModelComponent` (id du modèle, drapeaux d'ombre), `PlayerStartComponent`, puis les `[SerializedProperty]` de tout composant (12 types, dont `float` et `string`, `SerializedProperties.cs:43-47`). **Deux pertes pour le jeu** : `RaceCheckpointTriggerComponent.HalfWidth/HalfHeight/HalfDepth` (le jeu les calcule) et `RacingCarAnchorComponent.AnchorId`, sans attribut. Les autres composants du jeu posés dans un monde gardent leurs valeurs par défaut ou sont réglés à l'exécution.
- **`LegacyTrackSceneFactory.Create(trackName, AssetContentManager)`** (`LegacyTrackSceneFactory.cs:29-68`) rend les entités des générateurs et du décor, plus la pose de départ et la définition des trois checkpoints ; son second paramètre ne sert pas. Elle lit le catalogue global, `EngineEnvironment.ProjectPath` et les fichiers de piste. La taille des modèles vient d'une table en code (`LegacyLandscapePlacer.cs:23-93`, ADR-0027) : le `ComputeLegacyModelSize` du détail d'octobre n'existe plus.
- **Outil** `scripts/RgceAssetCooker` : options `--content`, `--check`, `--verify` ; il référence `RacingGame.Gameplay` et `CasaEngine.EditorServices`, écrit dans un dossier temporaire puis ne copie que les fichiers dont les octets changent (`Cooker.cs:50-94`), fixe les ids par `CookPlanner.SetIdentity` (uuid5, `CookPlan.cs:247-249`). `RacingGame.Gameplay` déclare `InternalsVisibleTo("RgceAssetCooker")`.
- **Catalogue** : `generate_rgce_content_assets.py` catalogue `Worlds/**/*.world` en `World.<chemin>` et contrôle que l'id du fichier est celui de l'entrée (`:135-137`, `:196-200`) ; aucune règle pour `.entity` ; son `--check` signale tout fichier non catalogué. `Content/Worlds/` contient `Editor.world` et `Test/TrackBeginnerGenerators.world` ; `Content/Entities/` n'existe pas.
- **Écrire un monde sans jeu** : aucune voie publique du moteur ne remplit les références d'un monde sans initialiser les entités (détail dans le plan du moteur).
- **Banc** : `artifacts/pbr-handover/s22/scripts/runref.sh <racine> <genre> <nom>` (genres `views-linear`, `views-legacy`, `tunnel-linear`, `tunnel-legacy`, `ui`, `audit`, `smoke`, `profile`). Référence du port : `artifacts/pbr-handover/s25/g7/ref/<genre>-a` et `-b`, rejouée identique octet pour octet à `dfba582` ; les commits suivants ne touchent que des documents. `s24/scripts/slice_check.py` ne compare pas tout octet pour octet à l'ancienne racine (les vues `legacy` par leur seule moyenne, les vues 3D seulement au-delà d'une borne) : **il ne sert pas de verdict ici**. Les références `after/` du détail d'octobre sont périmées.
- Arbres de travail propres (`.serena/` et `log.txt` non suivis, jamais indexés).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D10 | **Ciel** (réponse à O9, 2026-11-18) : les mondes de course et celui du carrousel portent l'id de la cubemap du ciel (`specular_cubemap_asset_id` = `Texture.SkyCubeMap`) ; le jeu lit cet id et charge le cube par le catalogue, et une sonde prouve que l'id sert. Le dessin du fond de ciel (`LegacySkyCubeViewPipeline`) reste en code : le moteur n'enregistre pas le pipeline d'une vue. |
| D11 | **Outil** (2026-11-18) : l'écriture d'un monde depuis des entités non initialisées passe par une petite API publique du moteur (plan `CasaEngine/ai-agent/tasks/offline-world-writer-tasks.md`), pas par une réflexion sur un champ privé. |
| D12 | **Mode d'exécution** (2026-11-18) : `AUTO` une fois le plan approuvé. Ni push, ni avance de `main` ou de `remaster` sans demande de l'auteur. |
| D13 | **Décor figé** (2026-11-18, « Graine 1 partout ») : le jeu prend la graine du banc. Le décor figé dans les mondes est celui de la graine 1, comparé objet par objet à l'original (1147, 1693 et 3404 objets appariés, ADR-0027). Le décor change de place une fois dans le jeu ; les références d'audit et d'interface sont refaites ; l'ADR-0027 est amendée par une nouvelle ADR. |

## Points à valider avec ce plan

Arbitrages proposés par l'agent : l'approbation du plan les valide. Ils remplacent P11 à P17 du détail d'octobre.

| Réf | Proposition |
|---|---|
| P11 | **Constructeurs dans la DLL.** La construction des entités et des réglages d'environnement passe de l'exe à `RacingGame.Gameplay` (`Race/RaceWorldTemplates.cs`), sans changer une valeur : front-end ; course sans la voiture (caméra, `RaceRoot`, lumière, générateurs, décor, checkpoints, départ) et son environnement (ombres, ambiant, pipeline de couleur `linear`) ; partie fixe du carrousel (lumière, trois plateaux, environnement). Le jeu s'en sert jusqu'à M6 et M7, ensuite seul l'outil s'en sert. |
| P12 | **Outil d'écriture** : `RgceAssetCooker --worlds` écrit `Worlds/FrontEnd.world`, `Worlds/Race/TrackBeginner.world`, `TrackAdvanced.world`, `TrackExpert.world`, `Worlds/CarSelection.world` et `Entities/PlayerCar.entity`, par l'API du moteur (D11) pour les mondes et `EditorEntityWriter.SaveEntity` pour la voiture. Les ids (monde, entités, composants) sont des uuid5 du nom de l'asset, de l'entité et du chemin du composant, posés par le mécanisme que l'outil emploie déjà pour ses assets (`CookPlanner.SetIdentity`) : un second lancement n'écrit rien. `--worlds --verify` relit les fichiers et les compare aux constructeurs. |
| P13 | **Nom des mondes** : un monde chargé porte le nom de son entrée. `FrontEndWorldName` devient `World.FrontEnd`, le préfixe des courses `World.Race.` (d'où `World.Race.TrackBeginner`) ; le carrousel charge `World.CarSelection`. Les noms d'entités ne changent pas. |
| P14 | **Ce que porte un monde enregistré, et ce que le jeu règle au chargement** : un monde de course porte les ombres activées, une lumière qui en projette, l'ambiant et le pipeline de couleur `linear`. À `OnWorldLoaded`, le jeu applique l'option des ombres (au monde et à la lumière) et, en `--color-pipeline legacy`, remet le pipeline de couleur par défaut (`ColorPipelineSettings.ResetToDefaults`), ce qui redonne l'état qu'a aujourd'hui un monde créé en `legacy`. |
| P15 | **Voiture** : les mondes de course n'ont pas de `PlayerCar`. Le lieur appelle `SpawnEntity<Entity>("Entity.PlayerCar")`, nomme l'entité `PlayerCar` et pose son état (indice, couleur, piste, profil, mode, vitesse) avant qu'elle n'entre dans le monde, puis la place et la possède comme aujourd'hui. |
| P16 | **Carrousel** : `CarSelection.world` contient `Carousel.Light`, les trois `Carousel.Plate.<i>` et l'environnement (ambiant, ombres, id du cube). Le carrousel le charge par `LoadCopy<World>`. Restent en code : les voitures peintes, la caméra, le placement à chaque image, l'option des ombres. |
| P17 | **Catalogue** : `generate_rgce_content_assets.py` catalogue aussi `Entities/**/*.entity` en `Entity.<chemin>`, avec le même contrôle d'id que pour les mondes. |
| P18 | **Ciel, mécanisme de D10.** `IsRaceWorld` décide toujours du pipeline de vue, du cube et de la couleur de fond : **le front-end garde un cube nul, aucun pipeline et un fond noir**, sans regarder d'id. Pour un monde de course, le jeu lit à `OnWorldLoaded` son `SpecularEnvironmentCubemapAssetId`, trouve le fichier par le catalogue, charge le cube par le chargeur d'aujourd'hui (`TextureCubeLoader`) et le garde en cache **par id d'asset**, puis fait comme aujourd'hui (cube posé à l'exécution, id remis à vide sur la copie chargée, pipeline de vue). Un **monde de course** dont l'id est vide reçoit le ciel procédural, comme aujourd'hui quand le fichier manque : c'est ce que la sonde de D10 observe. Le carrousel lit l'id de son propre monde. |
| P19 | **Parité.** La preuve que rien ne change est le rejeu du banc, identique octet pour octet à la référence prise en M1, contrôlé par un script propre à ce plan (`artifacts/worlds-as-assets/scripts/parity.py`), plus l'export de scène et les audits de voitures de M1. |
| P20 | **Branches** : jeu `chantier/rgce-worlds-as-assets`, créée depuis `remaster` (`170f41c`) ; moteur `chantier/offline-world-writer`, créée depuis `main` (`da32906e2`). |
| P21 | **Graine, mécanisme de D13.** La graine par défaut du décor devient une constante, 1, à la place de la graine par piste. Le mode `--capture-reference-views` ne règle plus la graine : il contrôle que celle de son `views.json` est celle du jeu, et échoue avec un message clair sinon. Les mondes écrits par l'outil portent donc le décor que le banc compare à l'original. |

## Règles d'exécution pour l'agent

- **Branches** de P20. Jamais de commit sur `master`, `remaster` ni `main`. Jamais de push ni d'avance de branche sans demande de l'auteur (D12).
- **Une tranche à la fois, une tâche à la fois.** Chaque tranche M est relue par un `plan-verifier` frais juste avant son exécution, sur le code du moment ; deux `REVISE` puis une relecture de clôture au plus, sinon ⚠️ et question à l'auteur. Une tranche terminée est contrôlée par un `verifier` frais aux frontières indiquées.
- **Statuts** : `⏳` → `🚧` avant de commencer ; à la fin, validation, puis `✅`, `🧪` ou `⚠️`, note de validation, **un commit dédié** qui inclut ce fichier. Jamais de tâche laissée en `🚧`.
- **Commits** : message en anglais `type(area): summary`, par fichier (`git commit -F`), terminé par la ligne `Co-Authored-By` de la session ; commandes commençant par `cd D:/development/repo/RacingGame &&`. `git add` fichier par fichier ; jamais `.serena/`, `.codegraph/`, `log.txt`.
- **Moteur** : aucune commande git dans `CasaEngine/MGUI` ni `CasaEngine/NvgSharp` ; jamais `git submodule update`, `checkout`, `switch`, `reset`. Un défaut du moteur se corrige dans le moteur, pas par un contournement dans le jeu.
- **Build** avant ✅ : `dotnet build RacingGame.slnx`, 0 erreur ; `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln` dès que l'éditeur sert à la validation.
- **Lancements** : un seul lancement du jeu, de l'éditeur ou d'une démo à la fois, jamais pendant un build ; rebuild avant de lancer.
- **Sondes et sorties** sous `artifacts/worlds-as-assets/` (ignoré par git) ou dans le scratchpad ; aucun build temporaire committé (`git status` contrôlé).
- **Ne rien inventer** : sinon ⚠️ Blocked, question dans « Points ouverts », arrêt. Au plus 3 cycles de correction par tâche.
- **Langue** : ce plan en français ; code, commits, `docs/` et ADR en anglais. Aucune ADR réécrite.

### Validation commune aux tranches M2 à M7 (« parité »)

`<t>` est la tranche ; la référence est `artifacts/worlds-as-assets/m1/ref/`.

1. `dotnet build RacingGame.slnx` à 0 erreur ; `python scripts/check_gameplay_type_names.py` à 0.
2. Rejeux par `runref.sh worlds-as-assets/<t>/ref <genre> <genre>-a`, puis `<genre>-b`, pour `views-linear`, `views-legacy`, `tunnel-linear`, `tunnel-legacy`, `ui`, `audit` ; une fois pour `smoke` et `profile`.
3. `python artifacts/worlds-as-assets/scripts/parity.py worlds-as-assets/<t>/ref` : pour chacun des six genres, chaque PNG de `<genre>-a` a l'empreinte SHA-256 du PNG de même nom de `<genre>-b` et de celui de la référence, et les deux dossiers ont les mêmes fichiers ; `smoke` et `profile` sortent en succès ; aucun `[Warning]` ni `[Error]` qui ne soit déjà dans les journaux de la référence. Le script sort en échec **à la première différence d'un octet**.
4. `--export-track-runtime-scene` comparé à l'export de M1 par `scripts/compare_race_scene_exports.py` : 0 enregistrement sans pair.
5. `--capture-car-profile-audit` et `--capture-car-top-speed-audit` : rapports identiques à ceux de M1.
6. Cuiseur `--verify` et `--check` à 0 ; `python scripts/generate_rgce_content_assets.py --check` à 0.

Un écart à l'un de ces points est un **arrêt** : la tranche passe en ⚠️, sans seuil de tolérance décidé par l'agent.

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Tâches

### 🧪 M0 — Le décor du jeu prend la graine du banc (D13, P21)

- **Prérequis** : aucun.
- **Résultat** : le jeu, l'audit, l'interface et l'export de scène montrent le décor de la graine 1, celui des vues du banc. Seule tranche qui change une image.
- **Fichiers** : `RacingGame.Gameplay/Track/RaceTrackLayout.cs` (une constante, la graine du décor, de valeur 1, à la place de `CreateTrackSeed`, retiré) ; `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs` (`ComputeRaceLayout` passe cette constante ; **la propriété `SceneryRandomSeed` est supprimée**, plus rien ne règle la graine à l'exécution) ; `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs` (il n'écrit plus la graine : il compare celle de son `views.json` à la constante et, si elle diffère, échoue par sa voie d'échec existante avec un message qui nomme les deux valeurs) ; les commentaires de ces fichiers qui décrivent la graine par piste ; ADR-0041 (`Proposed`, amende l'ADR-0027 : ligne d'état) et son index ; `docs/racinggame-3d-capture-bench-findings.md` ; ce plan.
- **Validation**, sorties sous `artifacts/worlds-as-assets/m0/` :
  - build à 0 erreur ; `check_gameplay_type_names.py` à 0 ;
  - **ce qui ne doit pas changer** : `views-linear`, `views-legacy`, `tunnel-linear`, `tunnel-legacy`, rejoués deux fois, chaque PNG d'empreinte égale à celui de `artifacts/pbr-handover/s25/g7/ref/<genre>-a` (le banc dessinait déjà la graine 1) ; les fichiers `scene-measure-<Track>.txt` identiques à ceux de cette référence (tous les objets de l'original appariés) ; les écrans d'interface sans monde 3D et `ui-car-selection` identiques ;
  - **preuve que la graine atteint tous les modes** : chaque monde de course construit écrit au journal une ligne `Track placements <piste>: … digest <empreinte>` (`LegacyTrackSceneFactory.cs:35`), que `runref.sh` copie dans `run.log`. Dans les rejeux de M0, l'empreinte de chaque piste lue dans `audit-a/run.log` (trois pistes) et dans `ui-a/run.log` (la piste montrée) est **égale** à celle de `views-linear-a/run.log`, soit `197D7EF3A381BD78` (Beginner), `C6D4C53FF70AEEB1` (Advanced) et `EF69E08B051B8297` (Expert), valeurs de la référence `s25/g7/ref/views-linear-a/run.log` ; et elle **diffère** de celle de `s25/g7/ref/audit-a/run.log` (`6EBF84F81D01AC88`, `FD0564E1183D98FE`, `29A2403A3F0CDCFA`, la graine par piste). Même contrôle sur le journal d'un `--export-track-runtime-scene` ;
  - **ce qui peut changer** : les 30 captures d'audit et les écrans `ui-race-hud`, `ui-pause`, `ui-race-finished`. Exigences : les deux rejeux identiques entre eux, et le contrôle d'empreintes ci-dessus. Une capture de cet ensemble qui reste identique à `s25/g7/ref` est **acceptée** et notée (six captures d'audit sont prises route seule, `TrackMigrationCaptureValidator.cs:146-166`, et peuvent ne pas montrer le décor) ; le nombre de captures changées et inchangées est écrit dans la note. Toute capture **hors** de cet ensemble qui change est un arrêt ;
  - une planche avant / après de deux vues d'audit par piste, regardée à pleine résolution et laissée à l'auteur (`artifacts/worlds-as-assets/m0/judge-decor.png`) : elle informe, elle ne décide pas ;
  - `rg SceneryRandomSeed` et `rg CreateTrackSeed` sur le dépôt (hors `bin`, `obj`, `artifacts`, `docs/decisions` et `ai-agent`, qui gardent l'historique) ne trouvent plus rien ;
  - un `views.json` copié dont la graine est mise à 2 fait échouer `--capture-reference-views` avec le message prévu ;
  - `--smoke-frontend` et `--verify-legacy-import-profile` en succès.
- **Vérification** : un `verifier` frais (seule tranche qui change le jeu à l'écran).
- **Arrêts** : une vue du banc ou une mesure de scène qui change ; un écran sans monde 3D qui change ; une empreinte de placements d'un mode qui n'est pas celle du banc.
- **Retour arrière** : `git revert` du commit.
- **Commit** : `feat(racing-casa): draw the scenery of the game with the seed of the reference capture`

> Validation (2026-11-18), sorties dans `artifacts/worlds-as-assets/m0/` (`ref/`, `m0_check.txt`, `seed2/`, `judge-decor.png`) ; contrôle par `artifacts/worlds-as-assets/scripts/m0_check.py` : `RESULT: PASS`. Statut 🧪 : appliqué et contrôlé ; la vérification indépendante suit.
> - Code : `RaceTrackLayout.ScenerySeed` = 1 ; `CreateTrackSeed` et `SceneryRandomSeed` supprimés (`rg` hors `bin`, `obj`, `artifacts`, `docs/decisions`, `ai-agent` : plus rien) ; le validateur compare la graine du `views.json` à la constante. Build à 0 erreur ; `check_gameplay_type_names.py` à 0.
> - Rejeux, chacun deux fois, identiques entre eux octet pour octet ; aucun `[Warning]` ni `[Error]` ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès.
> - Inchangé contre `s25/g7/ref` : les 15 vues et les 3 vues de tunnel en `linear` et en `legacy` (36 images) ; les mesures de scène ; les 7 écrans d'interface hors course.
> - Empreintes de placements : dans `views-linear-a`, `audit-a`, `ui-a` (Beginner) et l'export de scène, `197D7EF3A381BD78`, `C6D4C53FF70AEEB1`, `EF69E08B051B8297` ; celles de l'audit diffèrent de l'ancienne référence d'audit. Placements : 1147, 1693 et 3404 au lieu de 1274, 1757 et 3432.
> - Peut changer : 23 des 30 captures d'audit changent, 7 restent identiques (les six prises route seule et `Beginner-terrain-overview`) ; `ui-race-hud`, `ui-pause` et `ui-race-finished` changent.
> - Graine 2 : `--capture-reference-views` sort avec le code 1, sans image, message « views.json was exported with the scenery seed 2, the game draws its scenery with the seed 1. ».
> - Planche `judge-decor.png` (avant / après, `start-full` et `sector-50` des trois pistes), regardée : des palmiers et des objets à d'autres places, rien d'anormal ; laissée à l'auteur (O3).
> - Documents : ADR-0041 `Proposed`, ligne d'état sur l'ADR-0027, section du doc de banc.

### ⏳ M1 — Références de départ et script de parité

- **Prérequis** : M0 confirmé.
- **Résultat** : la référence de toutes les tranches suivantes, et le script qui rend le verdict de parité.
- **Fichiers** : ce plan (aucun code du dépôt). Sous `artifacts/worlds-as-assets/` :
  - `scripts/parity.py`, écrit et figé avant usage (empreinte notée ici), avec son contrôle : sur une copie de la référence où un octet d'un PNG de `views-legacy-a` est changé, il sort en échec ; sur la référence comparée à elle-même, en succès ;
  - `m1/ref/` : les rejeux du point 2 de la parité ; les quatre genres de vues identiques à `s25/g7/ref` (par empreintes) ; `ui` et `audit` identiques aux rejeux de M0 ;
  - deux exports `--export-track-runtime-scene` comparés entre eux (0 sans pair ; sinon la comparaison ne peut pas servir et la tranche s'arrête) ; les deux audits de voitures lancés deux fois, identiques entre eux ;
  - par piste, le nombre d'entités de générateurs et de décor que rend `LegacyTrackSceneFactory.Create`, lu dans l'export ;
  - les 15 vues en `linear` **avec l'option des ombres désactivée**, deux fois identiques entre elles, par le moyen que la relecture de la tranche établit (le fichier `front-end-options.json` sauvegardé puis restauré, empreinte contrôlée, si les modes d'automatisation le lisent) ; sinon la note le dit et M6 remplace ce contrôle par une lecture des valeurs du monde chargé.
- **Arrêts** : deux rejeux qui diffèrent entre eux ; un export non répétable.
- **Retour arrière** : sans objet (aucun code).
- **Commit** : `docs(ai-agent): record the starting references of the worlds as assets`

### ⏳ M2 — Le moteur sait écrire un monde depuis du code (D11)

- **Prérequis** : M1 ; plan du moteur `offline-world-writer-tasks.md` exécuté (T0 à T2) et confirmé par sa vérification.
- **Résultat** : le jeu pointe sur ce moteur ; rien ne change.
- **Fichiers** : le pointeur `CasaEngine` ; ce plan.
- **Validation** : parité.
- **Arrêts** : communs.
- **Retour arrière** : `git revert`.
- **Commit** : `chore(casaengine): take the offline world writer`

### ⏳ M3 — Champs perdus à l'enregistrement

- **Prérequis** : M1.
- **Résultat** : un checkpoint et une ancre de voiture gardent leurs valeurs après un aller-retour d'enregistrement ; rien ne change à l'exécution.
- **Fichiers** : `RacingGame.Gameplay/Components/RaceCheckpointTriggerComponent.cs` (`[SerializedProperty]` `half_width`, `half_height`, `half_depth`) ; `RacingCarAnchorComponent.cs` (`anchor_id`) ; leurs `Clone` s'ils ne passent pas déjà par `SerializedProperties.Copy`.
- **Validation** : parité ; une sonde enregistre (`EditorEntityWriter.SaveEntity`) puis recharge (`Entity.Load`) un checkpoint aux demi-dimensions non par défaut et la voiture de `RaceCarEntityFactory.Create()` : mêmes valeurs, mêmes six ancres.
- **Arrêts** : un type de propriété que `[SerializedProperty]` n'accepte pas.
- **Retour arrière** : `git revert`.
- **Commit** : `feat(racing-casa): save the checkpoint extents and the car anchor ids`

### ⏳ M4 — Construction des mondes dans la DLL (P11)

- **Prérequis** : M1.
- **Résultat** : le jeu construit ses mondes avec du code de `RacingGame.Gameplay` que l'outil pourra appeler ; rien ne change à l'exécution.
- **Fichiers** : `RacingGame.Gameplay/Race/RaceWorldTemplates.cs` (nouveau) ; `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` et `UI/CarSelectionCarousel.cs`, qui appellent ces constructeurs avec les valeurs d'aujourd'hui ; `LegacyTrackSceneFactory.Create` perd son paramètre inutilisé si rien d'autre ne l'emploie.
- **Hors tranche** : le chargement par fichier, les noms de mondes, la voiture.
- **Validation** : parité.
- **Arrêts** : un type qu'il faudrait déplacer de l'exe vers la DLL et qui tient à l'exe (accès au jeu autrement que par `IRaceGameHost`).
- **Retour arrière** : `git revert`.
- **Commit** : `refactor(racing-casa): build the world entities in RacingGame.Gameplay`

### ⏳ M5 — Les mondes et la voiture écrits en assets (P12, P16, P17)

- **Prérequis** : M2, M3 et M4.
- **Résultat** : les cinq mondes et `PlayerCar.entity` existent, catalogués, et s'ouvrent dans l'éditeur sans perte. Le jeu ne les charge pas encore.
- **Fichiers** : `scripts/RgceAssetCooker/` (`Program.cs` : `--worlds` ; `WorldWriter.cs` et `WorldVerifier.cs`, nouveaux ; README) ; `scripts/generate_rgce_content_assets.py` (P17) ; `RacingGameCasaEngine/Content/Worlds/FrontEnd.world`, `Worlds/Race/Track*.world`, `Worlds/CarSelection.world`, `Entities/PlayerCar.entity`, `AssetInfos.json`.
- **Validation** :
  - parité (le jeu ne lit pas ces fichiers) ;
  - **outil** : un second lancement n'écrit rien ; `--worlds --verify` à 0 écart : pour chaque monde relu par `World.Load`, mêmes entités que les constructeurs de M4 (noms et ordre, composants, transforms à 1e-6, ids de modèles, valeurs de lumière, demi-dimensions des checkpoints), même environnement clé par clé ; les mondes de course et le carrousel portent `specular_cubemap_asset_id` = l'id de `Texture.SkyCubeMap`, le front-end un id vide ; le nombre d'entités de chaque course est celui relevé en M1 plus la caméra, `RaceRoot`, la lumière, trois checkpoints et `PlayerStart` ; chaque id de fichier est celui de son entrée de catalogue ;
  - **éditeur**, sur une copie du contenu et du build de l'éditeur : chaque monde et la voiture s'ouvrent par `--open-asset` avec le code 0 et sans erreur au journal ; deux `--save-project` réécrivent chaque monde avec les octets de l'outil ; pour la voiture, une sonde la recharge par `Entity.Load` et la réenregistre deux fois par `EditorEntityWriter.SaveEntity` : mêmes octets que l'outil ;
  - **reste 🧪 pour l'auteur** : enregistrer la voiture depuis l'éditeur lui-même (ouvrir, modifier un champ, le rétablir, enregistrer, comparer les octets). Cette étape manuelle ne bloque pas M6.
- **Vérification** : un `verifier` frais sur les fichiers écrits (sérialisation : frontière à contrôler avant que le jeu en dépende).
- **Arrêts** : un champ perdu à l'aller-retour ; un asset que l'automatisation de l'éditeur n'ouvre pas ; un besoin du moteur au-delà du plan `offline-world-writer`.
- **Retour arrière** : `git revert` ; les fichiers écrits ne sont lus par rien.
- **Commit** : `feat(racing-casa): write the worlds and the player car as assets`

### ⏳ M6 — Le jeu charge le front-end et les courses (P13, P14, P15, P18)

- **Prérequis** : M5 confirmé.
- **Résultat** : le jeu charge `FrontEnd.world` et les trois courses par le catalogue ; la voiture vient de `PlayerCar.entity` ; le ciel d'une course vient de l'id de son monde. Rendu et jeu inchangés.
- **Fichiers** : `RacingGameCasaEngine/Program.cs` (`FirstWorldLoaded`) ; `Bootstrap/RacingGameCasaEngineGame.cs` (premier monde, option des ombres et pipeline `legacy` à `OnWorldLoaded`, ciel par id et cache par id) ; `Bootstrap/RaceFrontEndFlow.cs` (`SetWorldToLoad(chemin)`) ; `Bootstrap/RuntimeRaceWorldBinder.cs` (P15) ; `Worlds/RaceWorldFactory.cs` (noms de P13 ; la construction en code des mondes de course et du front-end disparaît) ; les validateurs seulement s'ils nomment un monde autrement que par les constantes.
- **Validation** :
  - parité : les vues et les tunnels sont dessinés depuis les mondes que le jeu charge, dans les deux modes de couleur, identiques octet pour octet à la référence, sans `[Warning]` nouveau de la mesure de scène ; tous les `ui-*` identiques (dont les écrans du front-end : fond et ciel inchangés) ;
  - **ciel (D10)** : sur une copie du dossier de build hors du dépôt, le `specular_cubemap_asset_id` d'un monde de course mis à vide donne des vues différentes de la référence (ciel procédural) ; remis, des vues identiques ;
  - **ombres désactivées** : les 15 vues identiques à celles de M1 prises sans ombres (ou le contrôle de remplacement établi en M1) ;
  - **enchaînement** : une sonde enchaîne deux courses puis revient au front-end (le monde sortant est désormais vidé) : aucune erreur au journal, aucun son de course après le retour, la seconde course identique à la première dans l'export de scène.
- **Vérification** : un `verifier` frais (frontière d'intégration : c'est ici que le jeu bascule).
- **Arrêts** : une capture qui change ; la voiture qui n'est pas prête quand le lieur la possède ; un écart d'export sur la voiture.
- **Retour arrière** : `git revert` du commit ; M5 reste valable seul.
- **Commit** : `feat(racing-casa): load the front end and race worlds from the catalogue`

### ⏳ M7 — Le carrousel charge `CarSelection.world` (P16, P18)

- **Prérequis** : M5 (le fichier) et M6 (ciel par id, cache par id) confirmés.
- **Résultat** : la lumière, les plateaux et l'environnement du carrousel viennent de `CarSelection.world` ; son cube vient de l'id de ce monde. L'exe n'appelle plus les constructeurs de `RaceWorldTemplates`.
- **Fichiers** : `RacingGameCasaEngine/UI/CarSelectionCarousel.cs`.
- **Validation** : parité (dont `ui-car-selection` identique).
- **Arrêts** : une capture qui change.
- **Retour arrière** : `git revert` du commit ; M6 reste valable seul.
- **Commit** : `feat(racing-casa): load the car selection lights and plates from a world`

### ⏳ M8 — ADR, documentation, vérification finale, clôture

- **Prérequis** : M7.
- **Résultat** : la décision est enregistrée, la documentation décrit les mondes en assets, et l'ensemble est confirmé par une vérification indépendante.
- **Fichiers** : ADR-0042 des mondes en assets (D10 à D12, P11 à P20, skill `adr`, `Proposed`) et son index ; `RacingGameCasaEngine/Assets/README.md` (mondes, voiture, outil, aller-retour) ; `rgce-editor-project-tasks.md` (Phase 6 : renvoi vers ce plan, O9 levé) ; `ai-agent/README.md` ; `ai-agent/HANDOVER.md` ; ce plan.
- **Vérification** : un `verifier` frais sur l'ensemble (lancements, fichiers, documents) : CONFIRMED.
- **Retour arrière** : `git revert`.
- **Commit** : `docs(adr): record the worlds and the player car as assets`

## Hors périmètre

- Tout ce qui changerait une image au-delà de M0 : écarts de rendu restants (O4 de `rgce-shadow-colour-decor-tasks.md`), réglages de matériaux, ciel dessiné par le moteur.
- Le Play du parcours complet dans l'éditeur, un éditeur de tracé, les données de jeu tenues en code (profils, couleurs, pistes, règles) : hors périmètre de `rgce-editor-project-tasks.md`, inchangé.
- S7 de `rgce-editor-project-tasks.md` (contrôle de l'éditeur sur tous les assets, clôture du chantier) : plan suivant.
- Les points ouverts du moteur (carte d'ombre lue en bilinéaire, DirectX, captures de démo).
- Push, avance de `main` et de `remaster`.

## Points ouverts

- O1 (auteur, après M8) : validation des ADR-0041 et 0042 du jeu et de l'ADR-0128 du moteur ; avance de `main` et de `remaster` ; push.
- O2 (auteur, M5) : l'enregistrement de `PlayerCar.entity` depuis l'éditeur lui-même, étape manuelle.
- O3 (auteur, M0) : regarder la planche du décor à la graine 1 (`artifacts/worlds-as-assets/m0/judge-decor.png`).
