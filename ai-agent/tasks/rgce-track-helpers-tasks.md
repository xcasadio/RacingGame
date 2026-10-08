# Plan agent IA — Aides de piste et CombiModel lus par RacingGameCasaEngine (point ouvert O7)

Plan d'exécution du point ouvert O7 du [plan du projet éditeur](rgce-editor-project-tasks.md). Le constat date de T4.5 : RacingGameCasaEngine ignore une partie de chaque piste héritée. Ses éléments `WidthHelper`, `RoadHelper` et `CombiObject` ne sont jamais lus, et le jeu dessine donc la piste sans les largeurs de route, les palmiers, les lanternes, les tunnels ni le contenu des CombiModel.

Les décisions D1 → D3 ont été arbitrées avec l'auteur le 2026-10-08 : **ce plan les applique, il ne les rediscute pas**.

> Relecture (2026-10-08) : deux vérificateurs de plan. Le premier a rendu REVISE : la comparaison avec l'original ne contrôlait qu'un sens, et un objet de décor en trop serait passé. Corrections :
> - la liste complète des objets en trop (T0.2) ;
> - la lecture coupée en deux, T1.2 puis T1.3 ;
> - le contrôle inverse dans T1.2, T1.3, T3.1 et la validation globale.
> Le second, sur le plan corrigé, a rendu **READY**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

À la fin du chantier :
- **Lecture complète** : le jeu lit les aides de largeur, les aides de route et les objets des CombiModel, comme le jeu d'origine.
- **Ce qu'elles produisent** est dessiné :
  - les largeurs de route, avec les rails, les poteaux, les checkpoints et la physique de piste qui en dépendent ;
  - les palmiers et les lanternes le long de la route ;
  - les tunnels ;
  - les objets des CombiModel.
- **Banc de comparaison** : de nouvelles références de parité, prises avant et après le correctif, avec une comparaison automatique au jeu d'origine et un contrôle visuel de l'auteur. S6 du plan éditeur figera ce décor corrigé en `.world`, et partira de ces références.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-08)

Découverte en lecture seule. La sonde et ses sorties sont dans le dossier de notes des sessions, `o7-track-helpers/` :
- `probe-o7/`, sonde .NET qui appelle par réflexion le code du jeu compilé, `RacingGameCasaEngine/bin/Debug/net9.0-windows/RacingGame.Gameplay.dll` du 2026-10-08 à 22:31, postérieur au dernier commit de `RacingGame.Gameplay/Track` ;
- `exporter-compare-s4-scene-a.txt`, sortie de `TrackPlacementExporter`.

Les faits porteurs ont été relus dans le code.

- **Dépôt** : branche `remaster`. La découverte s'est faite à HEAD `a553a75`. `.serena/` et `log.txt`, non suivis, sont à l'auteur. Le plan éditeur a S0 à S4 faits, et S5 est détaillée et approuvée (`rgce-editor-project-tasks.md`, Phase 5). S5 a commencé pendant l'écriture de ce plan : T5.1 et T5.2 sont committées (HEAD `b0960a3`), et T5.3 est en cours dans l'arbre de travail. Ce plan ne cite que du code de `a553a75` ; T0.1 le revérifie après S5.
- **Fichiers de données** :
  - les 3 `.Track` (`RacingGameCasaEngine/Content/Tracks/`) et les 10 `.CombiModel` (`Content/Models/`) sont identiques, octet pour octet, à ceux du jeu d'origine (`RacingGame/Content/`), vérifié par `cmp` ;
  - éléments comptés par `grep` :

    | Piste | `<WidthHelper>` | `<RoadHelper>` (Palms, Laterns, Tunnel, Reset) | `<NeutralObject>` |
    |---|---|---|---|
    | Beginner | 4 | 8 (1, 4, 1, 2) | 58 |
    | Advanced | 5 | 12 (2, 4, 2, 4) | 44 |
    | Expert | 6 | 35 (4, 8, 10, 13) | 72 |

  - **CombiModel** : 79 `<CombiObject>` en tout. Buildings 8, Hotels 6, OilTanks 5, Palms 3, Palms2 5, Ruins 11, Ruins2 8, SandCastle 10, Stones 7, Stones2 16. La racine est `ArrayOfCombiObject`.
  - **Aucun fichier** ne contient `LegacyWidthHelper`, `LegacyRoadHelper` ni `LegacyCombiObject`.
- **Le lecteur actuel** (`RacingGame.Gameplay/Track/LegacyTrackFileReader.cs`) cherche les éléments sous le nom des classes C# (`:41`, `:48`, `:76`, `nameof(Legacy…)`). Son commentaire le dit (`:12-15`). Il reproduit le défaut de l'ancien `XmlSerializer`, qui lisait `LegacyTrackLayout` sans `XmlArrayItem` pour ces deux listes (`LegacyTrackSceneFactory.cs:645-653`).
- **Ce qu'il rend aujourd'hui** (sonde) : 0 aide de largeur et 0 aide de route sur les 3 pistes, 0 objet dans chacun des 10 CombiModel. Les points de tracé (18, 41, 89) et les objets neutres (58, 44, 72) sont lus.
- **Le jeu d'origine lit tout** : ses types portent les noms des fichiers.
  - `TrackData.WidthHelper` et `TrackData.RoadHelper` (`RacingGame.Shared/Tracks/TrackData.cs:32,73`) sont lus par `XmlSerializer(typeof(TrackData))` (`:258`).
  - `TrackCombiModels.CombiObject` (`TrackCombiModels.cs:28`) est lu par `XmlSerializer(typeof(List<CombiObject>))` (`:111-114`).
- **Ce que fait l'original avec ces données** :
  - **largeurs** : `TrackLine.AdjustRoadWidths` (`TrackLine.cs:565-618`). Le portage RGCE est `RaceTrackLayout.AdjustRoadWidths` (`RacingGame.Gameplay/Track/RaceTrackLayout.cs:625-655`), même calcul ;
  - **plages des aides de route** : `TrackLine.GenerateTunnelsAndLandscapeObjects` (`TrackLine.cs:655-711`). Le portage est `RaceTrackLayout.BuildRoadHelperPositions` (`:871-930`). Une seule différence : RGCE n'ouvre pas de plage `Reset`, que l'original ouvre puis ignore. Les plages Tunnel, Palms et Laterns sont donc les mêmes, d'après la lecture du code ;
  - **palmiers et lanternes** : `Track.GenerateObjectsForTrack` (`Track.cs:621-772`). Le portage est `RaceTrackLayout.AddPalmAndLaternPlacements` (`:939-1029`), même calcul. Les tirages de l'original passent par un générateur global initialisé sur l'horloge (`RandomHelper.cs:12-13,23-26`) : ses palmiers changent à chaque lancement ;
  - **tunnels** : une géométrie à part (`Track.cs:464-606`) :
    - 4 sommets par point de route : bord gauche, haut gauche, haut droit, bord droit (`TrackVertex.cs:84-104,193-222`), avec une hauteur de 7,125 (`TrackVertex.cs:44`) ;
    - coordonnées U × 0,25 et V 0 / 0,235 / 0,765 / 1 ;
    - normales −up sur les 4 sommets, les deux du bas étant retournées ;
    - 6 triangles par segment ;
    - dessinée sans culling (`Track.cs:1227-1243`), dans la carte d'ombres aussi (`:1284-1285`) ;
    - matériau `roadTunnelMaterial` : ambiante (182,182,182), diffuse (80,80,80), spéculaire (64,64,64), textures `RoadTunnel` et `RoadTunnelNormal` (`Track.cs:75-80`).
    **RGCE n'a aucun code de tunnel** : `rg Tunnel RacingGame.Gameplay` ne trouve que l'énumération `LegacyRoadHelperType` (`LegacyTrackSceneFactory.cs:667-673`). `Textures/RoadTunnel.png` et `RoadTunnelNormal.png` sont dans `Content` et catalogués (`AssetInfos.json`, `Texture.RoadTunnel`, `Texture.RoadTunnelNormal`) ;
  - **CombiModel** : un objet neutre dont le nom est celui d'un CombiModel est remplacé par ses objets (`TrackObjectManager.cs:234-241`). Le portage est `RaceTrackLayout.AddSceneryPlacement` (`:809-851`), avec les mêmes alias, la même mise au sol, le même filtre de doublons et la même échelle de 1,2. Les 21 modèles cités par les CombiModel existent en glTF dans `Content/Models/`.
- **Hasard des palmiers dans RGCE** : la suite pseudo-aléatoire de la piste (`CreateDeterministicTrackRandom`, `RaceTrackLayout.cs:1178-1192`, graine FNV-1a de `"<NOM>|LegacyTrackSceneFactory.Helpers"`) sert d'abord aux palmiers (`:993-1002`), puis aux bannières de checkpoint et aux panneaux (`:1070,1118-1125`). Lire les palmiers changerait donc aussi les modèles des bannières et des panneaux : la sonde le confirme sur les 3 pistes. Les lanternes ne tirent rien.
- **Effet attendu de la lecture**, calculé par la sonde avec le code de calcul du jeu, en lisant les éléments sous les noms des fichiers. Les comptes d'objets sont faits sans filtre de doublons, donc ce sont des bornes hautes :

  | | Beginner | Advanced | Expert |
  |---|---|---|---|
  | Points de route | 1 332, inchangés | 2 172, inchangés | 4 466, inchangés |
  | Largeur de route (1 partout aujourd'hui) | 0,9 à 1,8, moyenne 1,324 ; 921 points ≠ 1 | 0,75 à 1,6, moyenne 1,08 ; 1 301 points | 1 à 1,53, moyenne 1,127 ; 2 698 points |
  | Largeur au point de départ | 1 | 1 | 1 |
  | Pose de départ | identique | identique | identique |
  | Demi-largeur des 3 checkpoints (8,13 aujourd'hui) | 8,13 / 13,42 / 7,46 | 8,13 / 8,12 / 12,1 | 8,13 / 10,77 / 8,85 |
  | Poteaux gauche/droite | 225/230 → 224/231 | 368/374 → 369/374 | 765/764, inchangés en nombre |
  | Colonnes | 101, inchangées | 160, inchangées | 337, inchangées |
  | Lanternes (borne haute) | ≤ 53 | ≤ 89 | ≤ 330 |

  - **Largeurs** : celles de la sonde sont exactement celles que `TrackPlacementExporter` calcule pour l'original (mêmes minimum, maximum et moyenne).
  - **Route la plus large** : 1,8 vers 31 % du tour (Beginner), 1,6 vers 68 % (Advanced), 1,53 vers 55 % (Expert). La plus étroite : 0,75 vers 43 % (Advanced).
  - **Plages des aides** (indices de points de route, et part du tour) :
    - **Beginner** :
      - palmiers 136–294 (10–22 %) ;
      - lanternes 294–562 (22–42 %) et 867–998 (65–75 %) ;
      - tunnel 767–867 (58–65 %, 244 unités héritées).
    - **Advanced** :
      - palmiers 12–255 (1–12 %) et 825–1005 (38–46 %) ;
      - lanternes 255–562 (12–26 %), 1137–1329 (52–61 %) et 1972–2169 (91–100 %) ;
      - tunnels 562–757 (26–35 %, 494 u) et 1329–1347 (61–62 %, 42 u).
    - **Expert** :
      - lanternes 78–277, 1015–1871, 2494–3690 et 4065–4463 (2–6 %, 23–42 %, 56–83 %, 91–100 %) ;
      - palmiers 277–519, 737–928, 1871–2494 et 3874–4065 (6–12 %, 17–21 %, 42–56 %, 87–91 %) ;
      - 9 tunnels courts entre 519 et 647 (12–14 %, de 7 à 27 u chacun) ;
      - un tunnel 3690–3839 (83–86 %, 381 u).
  - **Taille des maillages de tunnel**, d'après l'algorithme d'origine (4 sommets par point et 18 indices par segment de chaque plage) :
    - Beginner : 404 sommets et 1 800 indices ;
    - Advanced : 860 et 3 834 ;
    - Expert : 896 et 3 852.
- **Comparaison automatique avec l'original : `scripts/TrackPlacementExporter`**, outil de la migration d'avril 2026 :
  - ses types portent les noms des fichiers (`Program.cs:1177-1215`), et il reconstruit selon l'algorithme d'origine :
    - les échantillons de route avec leur largeur ;
    - les objets neutres et l'expansion des CombiModel, avec le filtre de doublons sur les tailles des `.X` d'origine ;
    - en sortie : `racinggame-placements.json` ;
  - avec `--compare-live-runtime-scene <export>`, il compare ce décor attendu à un export de RGCE (`RuntimeSceneComparison.cs:680-765`) :
    - les entités nommées (checkpoints, sol) se comparent par nom ;
    - le décor (`Kind` `track-scenery`) s'apparie par modèle et par transform, au plus proche, dans un seuil de 0,25 en position, 5° en rotation et 0,05 en échelle (`:772-851`) ;
    - un objet attendu sans partenaire compte dans `missingInLive`. Une entité de RGCE sans partenaire va dans `additionalLiveEntities`, dont la sortie ne montre que les 4 plus gros groupes (`:751-762`) ;
  - il compile (0 erreur). Lancé le 2026-10-08 contre `artifacts/editor-project-baseline/s4/scene-a.json` :

    | Piste | Attendu | `missingInLive` | `modelMismatch` | `transformMismatch` |
    |---|---|---|---|---|
    | Advanced | 112 | 77 | 0 | 4 |
    | Beginner | 174 | 134 | 0 | 4 |
    | Expert | 155 | 101 | 0 | 4 |

    - Les manquants sont exactement les objets des CombiModel : 77, 134 et 101 objets placés par l'original.
    - Les 4 écarts de transform par piste sont anciens et viennent des conventions de l'outil : `Checkpoint.01` à `.03` en rotation (149,4° / 90,5° / 53,7° sur Advanced, 155,2° / 122,1° / 72,4° sur Beginner, 43,0° / 84,7° / 94,7° sur Expert), et `Track.Ground.<piste>` en position (65,9 / 39,1 / 95,6). Ils ne concernent pas le décor.
  - **Objets que RGCE place et que l'original retire** : le filtre de doublons de l'original écarte ces objets neutres, que les objets des CombiModel placés avant eux recouvrent. RGCE les place aujourd'hui, et doit les retirer une fois les CombiModel lus :
    - Advanced : 1 `Windmill`, `AlphaTrain` ;
    - Beginner : 2 `Windmill`, `AlphaTrain`, `Building5` ;
    - Expert : 1 `Windmill`, `AlphaTrain`.
    Calcul : comptes par modèle de `scene-a.json` comparés aux placements de premier niveau de `racinggame-placements.json`. Les autres objets neutres ont le même compte des deux côtés.
  - **Décor de RGCE aujourd'hui**, entités `Track.Scenery.*` de `scene-a.json` :
    - Advanced : 108 = 29 objets neutres attendus + les 2 en trop + 77 panneaux, bannières et feu ;
    - Beginner : 68 = 34 + 4 + 30 ;
    - Expert : 202 = 48 + 2 + 152.
    Après lecture des CombiModel, en suivant l'original, il en faut 183, 198 et 301, avant les palmiers et les lanternes. Le calcul : on retire les objets en trop et on ajoute les 77, 134 et 101 objets des CombiModel.
- **Ce qui ne dépend pas des aides** : le terrain, les colonnes et leurs segments (aucune lecture de `RoadWidth` dans `BuildColumnPlacements`), le feu et la bannière de départ (largeur 1 au départ), le ciel et la voiture.
- **Le jeu d'origine** (`RacingGame/`, dans `RacingGame.slnx`) n'a aucun mode d'automatisation (`RacingGame/Program.cs` lance le jeu, sans arguments).
- **Audit de piste actuel** : 7 vues par piste (`TrackMigrationCaptureValidator.cs:128-221`), dont 3 sur les checkpoints. La pose d'une vue de checkpoint est dans `PrepareCheckpointView` (`:281-320`). Aucune vue ne vise une plage d'aide. `RaceTrackLayout`, `LegacyTrackSceneFactory` et `RaceTrackSpace` sont internes à `RacingGame.Gameplay`, visibles de l'exe par `InternalsVisibleTo` (`RacingGame.Gameplay.csproj:16`).
- **Audits des voitures** : `--capture-car-profile-audit` roule sur la première piste du catalogue, Beginner (`CarProfileAuditValidator.cs:112`, `RaceFrontEndCatalog.cs:38`) ; `--capture-car-top-speed-audit` parcourt les pistes (`CarTopSpeedAuditValidator.cs:109`). La demi-largeur de la physique de piste vaut largeur × 13,25 / 2 (`RaceTrackLayout.cs:370`) : elle suit la nouvelle largeur.
- **Générateurs** : la route est construite par `RaceRoadGeneratorComponent.Generate` → `LegacyTrackSceneFactory.CreateRoadGeneratorModel` → `CreateRoadModel` (`LegacyTrackSceneFactory.cs:185-232`, matériau `:399-413`). Le calcul du générateur passe `includeScenery: false` (`LegacyTrackSceneFactory.Generators.cs`, `ComputeGeneratorLayout`). Les plages d'aides n'y sont donc pas calculées aujourd'hui (`RaceTrackLayout.cs:209-217`).
- **Code mort relevé** : `LegacyTrackSceneFactory.GetNearestWidthScale` (`:296`) et `LegacyTrackSceneFactory.SampleLoopPoint` (`:318`) n'ont aucun appelant (`rg`). Ils sont laissés tels quels (« Hors périmètre »).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | **Calendrier** : le chantier s'exécute après S5 du plan éditeur, close par son vérificateur, et avant S6. S6 fige donc le décor corrigé, et part des références de ce chantier. |
| D2 | **Comparaison avec l'original** : automatique par `TrackPlacementExporter` pour la partie déterministe (largeurs de route, objets neutres, expansion des CombiModel). L'auteur fait aussi un contrôle visuel dans le jeu d'origine aux endroits que le plan nomme : palmiers, lanternes, tunnels, largeurs et décor des CombiModel. |
| D3 | **Les palmiers ont leur propre suite pseudo-aléatoire**, issue du nom de la piste. Les bannières de checkpoint et les panneaux gardent leurs modèles d'aujourd'hui ; seuls leur décalage latéral et l'échelle des bannières suivent la nouvelle largeur. |

## Points à valider avec ce plan

Arbitrages proposés par l'agent : l'approbation du plan les valide.

| Réf | Proposition |
|---|---|
| P1 | **Branche `remaster`**, à la suite de S5, comme le chantier éditeur (son P2). Le code touché n'existe que sur `remaster`, et le correctif doit précéder S6. |
| P2 | **Lecture** : le lecteur cherche les éléments sous les noms des fichiers (`WidthHelper`, `RoadHelper`, `CombiObject`), et seulement eux : les noms `Legacy…` n'apparaissent dans aucun fichier. Les noms des classes C# ne changent pas. |
| P3 | **Graine des palmiers** : la même fonction FNV-1a, avec le suffixe `"|LegacyTrackSceneFactory.Palms"`. La suite existante (`"|LegacyTrackSceneFactory.Helpers"`) ne sert plus qu'aux bannières et aux panneaux. |
| P4 | **Tunnels dans le générateur de route** : `RaceRoadGeneratorComponent` produit un second modèle généré, `<piste>.Tunnel`, à côté de `<piste>.Road`.<br>• Pas de nouveau composant, ni de nouvelle entité, ni de nouvelle propriété sérialisée : les `.world`, dont le monde de test de T4.6, ne changent pas.<br>• Le calcul des plages d'aides passe hors de `includeScenery`, puisque le générateur l'appelle sans décor.<br>• Géométrie, coordonnées, normales et triangles de l'original (« État vérifié »), sans culling. Le modèle projette des ombres et libère ses tampons comme la route (`OwnsBuffers: true`). |
| P5 | **Matériau du tunnel** : les valeurs de `roadTunnelMaterial`, en `lit-diffuse` :<br>• `ambient_color` (182,182,182), `diffuse_color` (80,80,80), `specular_color` (64,64,64) ;<br>• textures `RoadTunnel` et `RoadTunnelNormal` ;<br>• rastérisation CullNone.<br>Il suit le chemin des matériaux de piste que S5 aura mis en place : une recette, un asset cuit `Material.Track.<Rôle>`, et l'id porté par le générateur (T5.3 à T5.5 du plan éditeur). T0.1 relève les noms réels de ce chemin après S5. Sans tangentes, la carte de normales ne s'applique pas, comme ailleurs depuis T5.2. |
| P6 | **Vues d'audit aux aides** : 3 vues de plus par piste, ajoutées après `terrain-overview`. Les 21 vues actuelles gardent ainsi leur ordre et leur temps. Les noms et les indices de points de route :<br>• `helper-palms` : 215 (Beginner), 133 (Advanced), 398 (Expert) ;<br>• `helper-laterns` : 428, 408, 177 ;<br>• `helper-tunnel` : 817, 659, 3764.<br>Ce sont les milieux de la première plage de palmiers, de la première plage de lanternes et du plus long tunnel ; les points de route ne bougent pas avec le correctif. La pose est celle de `PrepareCheckpointView` (18 en arrière, 12 en haut, 4 à droite, cible 6 en avant), à partir du point de route converti en espace d'exécution. |
| P7 | **Références**, hors du dépôt dans `artifacts/o7-track-helpers/`, que git ignore :<br>• `before/` (T0.2), puis `after/` (T3.1) ;<br>• dans chacun : deux audits de piste (`track-audit-a/`, `-b/`, 30 vues), deux exports (`scene-a.json`, `-b.json`), les audits des voitures, les captures d'interface et la sortie de `TrackPlacementExporter`.<br>`after/` devient la référence de S6. |
| P8 | **ADR** : une nouvelle ADR, au numéro libre suivant celle de S5, consigne :<br>• que RGCE lit les aides et les CombiModel comme l'original ;<br>• que les références de parité du plan éditeur repartent de `after/` ;<br>• la décision D3.<br>Aucune ADR existante n'est réécrite. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (P1). Jamais de commit sur `master` ni sur `main`.
- **Prérequis** : S5 du plan éditeur est close (T5.7 ✅, vérificateur CONFIRMED). Avant cela, aucune tâche ne commence.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par la ligne `Co-Authored-By` de la session. Les commandes de commit commencent par `cd D:/development/repo/RacingGame &&`.
- **Ne jamais pousser ni fusionner.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée. Sinon : ⚠️ Blocked, question dans « Points ouverts », **arrêt**.
- **Build obligatoire** avant ✅ dès que du code est touché :
  - `dotnet build RacingGame.slnx` ;
  - `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln` dès que l'éditeur sert à la validation.
- **Lancer le jeu** uniquement par un script qui sauvegarde et restaure `display-settings.json` et `front-end-options.json` (empreinte SHA-1 vérifiée), comme dans le chantier éditeur. Sondes dans le scratchpad seulement ; pas de clavier ni de souris simulés.
- **Au plus 3 cycles de correction par tâche** ; au-delà, ⚠️ Blocked.
- **Ne jamais indexer** les fichiers de l'auteur : `git add` fichier par fichier ; jamais `.serena/` ni `log.txt`.
- **Langue** : ce plan en français ; code, commits, `docs/` et ADR en anglais.
- **Vérification** : un vérificateur de plan relit ce plan avant approbation ; un `verifier` indépendant contrôle le chantier terminé (T3.2) avant qu'il soit déclaré fait.
- Rappel moteur : pas d'allocation, de LINQ ni de closure dans les chemins chauds ; restaurer tout état GPU modifié ; le runtime ne dépend pas de l'éditeur.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur ; `scripts/check_gameplay_type_names.py` : 0 collision.
- **Lecture** : 15 aides de largeur, 55 aides de route et 79 objets de CombiModel lus. Leurs valeurs sont celles qu'un `XmlSerializer` à types nommés comme les fichiers lit (T1.2, T1.3).
- **Original** (D2), par `TrackPlacementExporter --compare-live-runtime-scene after/scene-a.json` :
  - dans les deux sens :
    - `missingInLive` = 0 et `modelMismatch` = 0 sur les 3 pistes. Les seuls écarts de transform sont les 4 anciens par piste, aux mêmes valeurs ;
    - **sens inverse** : aucune entité de décor de RGCE sans partenaire dans l'original, hors panneaux, bannières, feu de départ, palmiers et lanternes (liste complète de T0.2). Le nombre de palmiers et de lanternes sans partenaire est celui qu'ajoute T1.3 ;
  - les objets en trop de `before/` (`Windmill`, `AlphaTrain`, `Building5`) ont disparu ;
  - largeurs de route égales à celles de l'original, à 1e-4 près, point par point.
- **Écarts attendus et seulement eux** : `compare_race_scene_exports.py` entre `before/` et `after/` ne trouve que les écarts comptés de T1.2, T1.3 et T2.1.
- **Références `after/`** : deux audits de piste à au plus 0,5 l'un de l'autre, et deux exports identiques.
- **Jeu** : `--smoke-frontend` à 0, et `--verify-legacy-import-profile` à 0.
- **Éditeur** : le monde de test de T4.6 s'ouvre avec le code 0, et sa capture montre la route élargie et le tunnel de Beginner. Deux enregistrements donnent ses octets committés.
- **Contrôle visuel de l'auteur** dans le jeu d'origine, à l'aide de la liste de T3.1.
- Vérificateur indépendant : CONFIRMED.

---

## Phase 0 — Préalables, après S5

### ✅ T0.1 — Plan revérifié contre le code de S5

- **Objectif** : ce plan cite du code que S5 va modifier (`LegacyTrackSceneFactory`, générateurs, export, matériaux). Avant d'écrire du code, il est confronté au code après S5.
- **Sources** : ce plan ; le code à la fin de S5 ; le plan éditeur (notes de T5.1 à T5.7).
- **Étapes** :
  1. Vérifier que S5 est close (T5.7 ✅, vérificateur CONFIRMED) et que l'arbre de travail ne contient que les fichiers de l'auteur.
  2. Relire chaque fait cité dans « État vérifié » et dans P4 à P6. La note donne les numéros de ligne qui ont bougé.
  3. Relever le chemin réel des matériaux de piste après S5 (P5) : nom de la recette, de l'outil de cuisson, de l'asset `Material.Track.<Rôle>` de la route, et de la propriété d'id du générateur. Puis compléter T2.1 avec ces noms.
  4. Recompiler la sonde du dossier de notes (`o7-track-helpers/probe-o7/`) contre le code de S5 et la relancer : mêmes chiffres qu'ici.
  5. Si un fait porteur a changé, ou si le chemin des matériaux demande une autre forme que P5 : ⚠️ Blocked. Le plan est alors corrigé, relu par un vérificateur de plan et soumis à l'auteur.
- **Validation** : la note liste les faits revérifiés et les noms relevés, et dit si la sonde redonne les mêmes chiffres.
- **Commit** : `docs(racing-casa): recheck the track helper plan after slice S5`

> Validation (2026-10-09), à `708e141` :
> - **S5 close** : T5.7 ✅, vérificateur CONFIRMED. L'arbre de travail ne contient que `.serena/` et `log.txt`, à l'auteur.
> - **Fichiers non touchés par S5** (`git diff --stat a553a75..HEAD`). Leurs citations restent exactes, numéros de ligne compris :
>   - `LegacyTrackFileReader.cs`, `RaceTrackLayout.cs` et `RaceTrackLayoutTypes.cs` ;
>   - `TrackMigrationCaptureValidator.cs`, `CarProfileAuditValidator.cs:112`, `CarTopSpeedAuditValidator.cs:109` et `RaceFrontEndCatalog.cs:38` ;
>   - `scripts/TrackPlacementExporter`.
>   `InternalsVisibleTo("RacingGameCasaEngine")` reste à `RacingGame.Gameplay.csproj:16`, et S5 a ajouté `RgceAssetCooker` à `:18`.
> - **`LegacyTrackSceneFactory.cs`, réduit par S5** : les faits sont les mêmes, mais les lignes ont bougé :
>   - `CreateRoadModel` passe de `:185-232` à `:120-167`. Le maillage ne porte plus de matériau construit, mais `MaterialAssetId` ;
>   - `LegacyTrackLayout`, sans `XmlArrayItem` pour les deux listes d'aides, passe de `:645-653` à `:324-333` ;
>   - `LegacyRoadHelperType` passe de `:667-673` à `:347-353` ;
>   - `GetNearestWidthScale` passe de `:296` à `:207`, et `SampleLoopPoint` de `:318` à `:229`, toujours sans appelant ;
>   - `ComputeGeneratorLayout` passe toujours `includeScenery: false` (`LegacyTrackSceneFactory.Generators.cs:86-96`) ;
>   - `RaceGeneratedModel` garde `CastShadows` et `OwnsBuffers` (`:14`).
> - **Catalogue** : les PNG des tunnels s'appellent maintenant `Image.RoadTunnel` et `Image.RoadTunnelNormal` (T5.4). Aucun `.texture` ne les cite encore, puisque aucun matériau cuit ne s'en sert.
> - **Chemin des matériaux de piste** (P5, O1), relevé dans le code :
>   - recette : `RaceMaterialRecipes.ForRoad` et ses voisines (`RacingGame.Gameplay/Import/RaceMaterialRecipes.cs:128-209`), avec les noms de textures en constantes (`RoadColumnTextureName`…) ;
>   - **outil de cuisson** : `TrackRoles` (`scripts/RgceAssetCooker/CookPlan.cs:70`) et le dictionnaire de `BuildTrackMaterials` (`:156-180`). Il écrit `Materials/Track/<Rôle>.material` (`Material.Track.<Rôle>`), puis un `.texture` pour chaque image utilisée ;
>   - **id** : `RaceTrackMaterialAssets` (`LegacyTrackSceneFactory.Models.cs:10-23`), uuid5 du nom ;
>   - **générateur** : `RaceRoadGeneratorComponent.MaterialAssetId`, `[SerializedProperty("material_asset_id")]`, valant par défaut `RaceTrackMaterialAssets.Road`. Il entre dans la signature.
>   T2.1 reçoit ces noms. P4 interdit toute nouvelle propriété sérialisée, donc l'id du matériau du tunnel est une constante du générateur et non une propriété (lecture de P4 avec P5, voir T2.1).
> - **Valeur manquante dans P5** : la puissance spéculaire. `roadTunnelMaterial` ne la fixe pas, et garde le défaut de `Material`, soit 24 (`RacingGame.Shared/Graphics/Material.cs`, `DefaultSpecularPower`). T2.1 la reprend.
> - **Sonde** : copiée dans `o7/probe-o7/` du scratchpad, recompilée contre la DLL de `708e141` et relancée. Sa sortie est identique, octet pour octet, à celle de la découverte (`diff` vide).
> - **`TrackPlacementExporter`** contre `s5/scene-a.json` :
>   - mêmes `missingInLive` (77 / 134 / 101), `modelMismatch` = 0, et les 4 écarts anciens par piste, aux mêmes valeurs ;
>   - seul `additionalLiveEntities` change : de 984 / 593 / 2 023 à 82 / 37 / 157. L'écart est exactement `GuardRailHolder` + `RoadColumnSegment` (742 + 160, 455 + 101, 1 529 + 337). La référence de S4 datait d'avant T4.5, qui a fait des poteaux et des segments des enfants générés, et non plus des entités.
>   - Ce n'est donc pas un fait porteur. Mais la validation de T1.2 prévoyait de voir les poteaux dans `additional-other:` : elle les contrôle maintenant dans l'export par maillage (corrigé ci-dessous).

### ✅ T0.2 — Vues d'audit aux aides et références « avant »

- **Objectif** :
  - des images des endroits où agissent les aides, avant et après le correctif. Ce sont aussi les images RGCE du contrôle visuel de l'auteur ;
  - une comparaison avec l'original dans les deux sens ;
  - puis les références `before/` (P7).
- **Fichiers** :
  - `RacingGameCasaEngine/Bootstrap/TrackMigrationCaptureValidator.cs` : il ajoute les 3 vues de P6 par piste, après `terrain-overview`. Les points de route viennent de `LegacyTrackSceneFactory.TryResolveTrackSource` et `ComputeGeneratorLayout`, par les ids de la piste chargée. Les positions sont converties par `RaceTrackSpace` ;
  - `scripts/TrackPlacementExporter/RuntimeSceneComparison.cs` : après la ligne de résumé de chaque piste, deux lignes de plus.
    - `additional-scenery:` : **tous** les groupes, par modèle, des entités `track-scenery` sans partenaire ;
    - `additional-other:` : tous les groupes des autres entités sans partenaire.
    La ligne de résumé et les comptes actuels ne changent pas.
- **Étapes** :
  1. Ajouter les vues et les deux lignes ; build du jeu et de l'outil.
  2. Prendre `before/`, chaque mode lancé à part :
     - `--capture-track-audit` deux fois ;
     - `--export-track-runtime-scene` deux fois ;
     - `--capture-car-profile-audit` et `--capture-car-top-speed-audit` ;
     - `--capture-ui-screens` ;
     - `--smoke-frontend`.
  3. Lancer `TrackPlacementExporter --repo-root . --output-dir artifacts/o7-track-helpers/before/original --compare-live-runtime-scene artifacts/o7-track-helpers/before/scene-a.json`.
- **Validation** :
  - build à 0 erreur ;
  - les 21 vues actuelles restent dans le seuil de 0,5 contre la dernière référence d'audit de S5 (`compare_track_audit.py --threshold 0.5`) ;
  - entre `track-audit-a/` et `-b/`, les 30 vues sont dans le seuil ;
  - les 9 nouvelles vues sont lues : route nue, sans palmier, lanterne ni tunnel ;
  - `scene-a.json` et `-b.json` identiques (`compare_race_scene_exports.py`). L'export est identique à la dernière référence de S5 ;
  - les audits des voitures sont identiques à ceux de S5 ;
  - `TrackPlacementExporter` :
    - `missingInLive` = 77 / 134 / 101 (Advanced / Beginner / Expert), `modelMismatch` = 0, et les 4 écarts anciens par piste ;
    - **contrôle du sens inverse** : hors `Banner*`, `Sign*` et `StartLight*`, la ligne `additional-scenery:` liste exactement les objets en trop de « État vérifié » : `Windmill` 1 / 2 / 1, `AlphaTrain` 1 / 1 / 1, `Building5` 0 / 1 / 0. Le contrôle repère donc bien un objet que l'original ne place pas ;
    - la ligne `additional-other:` est notée telle quelle : c'est la référence des groupes hors décor ;
    - si ces nombres diffèrent après S5, la note les explique ; sinon ⚠️ Blocked ;
  - les réglages de l'auteur sont restaurés à l'identique.
- **Commit** : `test(racing-casa): capture the track audit where the road helpers act`

> Validation (2026-10-09) :
> - **Code** :
>   - `TrackMigrationCaptureValidator` ajoute `helper-palms`, `helper-laterns` et `helper-tunnel` après `terrain-overview`, aux points de route de P6 ;
>   - le point vient de la disposition du générateur de route de la piste chargée : `TryResolveTrackSource` par ses ids, puis `ComputeGeneratorLayout`. Il est converti par `RaceTrackSpace`, puis par la matrice monde du générateur ;
>   - la pose est celle de `PrepareCheckpointView`, dans la direction du point de route ;
>   - `RuntimeSceneComparison` écrit `additional-scenery:` et `additional-other:` après la ligne de résumé, avec tous les groupes. La ligne de résumé ne change pas.
> - **Écart dû à S5, corrigé dans l'outil** :
>   - depuis T5.5, le jeu charge les modèles cuits, et l'export écrit le nom de l'asset (`StaticModel.Windmill`) au lieu du nom hérité ;
>   - `TrackPlacementExporter` n'appariait donc plus le décor : `missingInLive` = 106 / 168 / 149 ;
>   - en T0.1, ce défaut ne se voyait pas, puisque l'export de T5.1 date d'avant T5.5 ;
>   - le chargement de l'export retire maintenant ce préfixe des noms de modèles, et les comptes attendus reviennent, ci-dessous. Sur l'export de T5.1, sans préfixe, les comptes ne changent pas.
> - **Références `artifacts/o7-track-helpers/before/`**, chaque mode lancé à part (`o7_refs.sh` du scratchpad), réglages de l'auteur restaurés à l'identique :
>   - deux audits (`track-audit-a/`, `-b/`, 30 vues chacun) ;
>   - deux exports ;
>   - les audits des voitures ;
>   - les captures d'interface (`ui/`, lancement `ui-run-20261009-012259`) ;
>   - la sortie de `TrackPlacementExporter` (`original/`, `original-compare.txt`) ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.
> - **Contrôles** :
>   - build à 0 erreur et 0 avertissement ; `check_gameplay_type_names.py` : 0 collision ;
>   - **audit** : les 21 vues de S5 sont dans le seuil de 0,5 contre `s5/track-audit-a`, au plus 0,013 (`chase-road-only` d'Expert), les autres à 0,000. Les 9 nouvelles vues sont signalées « EXTRA », comme prévu. `track-audit-a` contre `-b` : 30 vues sur 30 dans le seuil ;
>   - **les 9 nouvelles vues**, lues sur une planche : route nue, sans palmier, lanterne ni tunnel. On y voit des rails, du décor et des panneaux ;
>   - **export** : `scene-a.json` et `-b.json` sont identiques (1 074, 689, 2 156). Contre `s5/scene-a.json`, avec `--effective-states`, tout est apparié ;
>   - **audits des voitures** identiques à S0, `details/` compris ;
>   - **`TrackPlacementExporter`** :
>     - `missingInLive` = 77 / 134 / 101 (Advanced / Beginner / Expert), `modelMismatch` = 0, et les 4 écarts anciens par piste, aux mêmes valeurs ;
>     - **sens inverse** : hors `Banner*`, `Sign*` et `StartLight*`, `additional-scenery:` liste exactement `Windmill` 1 / 2 / 1, `AlphaTrain` 1 / 1 / 1 et `Building5` 0 / 1 / 0 ;
>     - **`additional-other:`** (référence) : `<piste>.Columns`, `<piste>.GuardRail.Left` et `<piste>.GuardRail.Right`, une fois chacun par piste. Ce sont les entités des générateurs de colonnes et de rails. Les poteaux et les segments n'y sont plus, depuis T4.5 (voir T0.1).

---

## Phase 1 — Lecture des aides et des CombiModel

### ✅ T1.1 — Les palmiers tirent dans leur propre suite (D3)

- **Objectif** : séparer la suite des palmiers de celle des bannières et des panneaux, avant que les palmiers existent. Rien ne change à l'écran : aucune aide n'est encore lue, donc aucun palmier n'est tiré.
- **Fichiers** : `RacingGame.Gameplay/Track/RaceTrackLayout.cs`.
  - `AddHelperDrivenSceneryPlacements` crée une seconde suite, avec la graine de P3, et la passe à `AddPalmAndLaternPlacements`.
  - `AddSignAndCheckpointPlacements` garde la suite existante.
- **Validation** :
  - build à 0 erreur ;
  - **A4 contre `before/`** :
    - export identique (`compare_race_scene_exports.py`, 0 différence) ;
    - audit dans le seuil de 0,5 ;
    - audits des voitures identiques ;
    - captures d'interface à 0,00 ;
    - smoke à 0 ;
  - **preuve de D3**, par la sonde du scratchpad sur la DLL compilée : avec les aides lues sous les noms des fichiers, la suite des modèles de bannières et de panneaux est identique à celle sans aides. Avant T1.1, la même sonde la trouve différente.
- **Commit** : `refactor(racing-casa): draw the palms from their own random sequence`

> Validation (2026-10-09) :
> - **Code** : `CreateDeterministicTrackRandom(trackName, sequenceName)` garde le même FNV-1a, avec le suffixe `|LegacyTrackSceneFactory.<sequenceName>`.
>   - `AddPalmAndLaternPlacements` reçoit la suite `Palms` (P3).
>   - `AddSignAndCheckpointPlacements` garde la suite `Helpers`, dont la graine est inchangée.
> - Build à 0 erreur.
> - **Preuve de D3**, par la sonde `o7/probe-o7` (aides et CombiModel lus sous les noms des fichiers) :
>   - la suite des modèles de bannières et de panneaux est identique à celle sans aides sur les 3 pistes (29, 76 et 151 modèles, `same model sequence: True`) ;
>   - avant T1.1, la même sonde donnait 29 → 32, 76 → 72 et 151 → 152, avec `False`.
> - **A4 contre `before/`** (`artifacts/o7-track-helpers/t1.1/`) :
>   - export identique (1 074, 689, 2 156 enregistrements appariés) ;
>   - audit : 30 vues sur 30 dans le seuil de 0,5 ;
>   - audits des voitures identiques ;
>   - `TrackPlacementExporter` : mêmes lignes que `before/` ;
>   - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.
> - **Captures d'interface** : pas toutes à 0,00. Deux lancements du même build T1.1 diffèrent entre eux : `main-menu` 0,70, `help` 0,44, `race-hud` 0,69, `race-finished` 10,76. C'est le bruit déjà relevé en S5.
>   - Contre `before/`, chaque écran est à 0,00 dans l'un des deux lancements, sauf `options`, à 0,39 dans les deux.
>   - L'écran Options est capturé avant tout chargement de piste, donc avant que le code modifié puisse s'exécuter. Les deux images sont identiques à l'œil : l'écart vient de la phase du fond animé du menu.

### ⏳ T1.2 — Largeurs de route et objets des CombiModel

- **Objectif** : P2, pour deux éléments. Le lecteur lit `WidthHelper` et `CombiObject` : la route prend ses largeurs, et les CombiModel leurs objets. `RoadHelper` reste non lu jusqu'à T1.3. Il n'y a donc encore ni palmier ni lanterne, et tout le décor se compare strictement à l'original.
- **Fichiers** : `RacingGame.Gameplay/Track/LegacyTrackFileReader.cs` : noms d'éléments `WidthHelper` et `CombiObject`. Le commentaire de classe dit ce qui est lu comme dans l'original, et ce qui reste à lire en T1.3.
- **Validation** :
  - build à 0 erreur ;
  - **lecture**, par la sonde du scratchpad :
    - comptes du lecteur : 4 / 5 / 6 aides de largeur (Beginner / Advanced / Expert), 79 objets de CombiModel, et toujours 0 aide de route ;
    - chaque valeur (position, échelle, nom de modèle, matrice) est égale à celle que lit un `XmlSerializer` aux types nommés comme les fichiers, ceux de `TrackPlacementExporter` ;
  - **largeurs** : pour chaque point de route de chaque piste, |largeur RGCE × 13,25 − `RoadWidth` de `racinggame-placements.json`| ≤ 1e-4, par la sonde et `before/original/` ;
  - **original, dans les deux sens**, par `TrackPlacementExporter --compare-live-runtime-scene` sur un export neuf :
    - `missingInLive` = 0 et `modelMismatch` = 0 sur les 3 pistes, avec les 4 écarts anciens par piste aux mêmes valeurs ;
    - `additional-scenery:` ne contient que `Banner*`, `Sign*` et `StartLight*`, aux comptes de `before/`. Aucun autre modèle, palmiers des CombiModel compris ;
    - les objets en trop de `before/` ont donc disparu : `Windmill` 1 / 2 / 1, `AlphaTrain` 1 / 1 / 1, `Building5` 0 / 1 / 0 (Advanced / Beginner / Expert). Ce retrait est exigé ;
    - `additional-other:` garde les groupes et les comptes de `before/`. Depuis T4.5, les poteaux ne sont plus des entités, et n'y figurent donc pas (T0.1). Leur changement se contrôle dans l'export : les maillages dont le chemin de composant contient `GuardRailHolder` (742 / 455 / 1 529 dans `s5/scene-a.json`) passent de 742 à 743 sur Advanced, et restent à 455 sur Beginner et à 1 529 sur Expert (« État vérifié ») ;
    - si le filtre de doublons de RGCE (tailles des glTF) écarte un autre objet que l'original (tailles des `.X`), chaque cas est décrit avec les deux tailles et soumis à l'auteur : la tâche passe en ⚠️ Blocked ;
  - **export** contre `before/`, par `compare_race_scene_exports.py` :
    - entités `Track.Scenery.*` : 183 / 198 / 301 (Advanced / Beginner / Expert), contre 108 / 68 / 202 ;
    - les maillages **en moins** sont tous dans ces catégories :
      - la route ;
      - les rails gauche et droit ;
      - des poteaux ;
      - des bannières de checkpoint et des panneaux, là où la largeur ≠ 1 ;
      - les 2 / 4 / 2 objets en trop ;
    - aucun maillage en moins pour le terrain, les colonnes et leurs segments, le feu, la bannière de départ, les autres objets neutres et la voiture ;
    - les modèles des bannières et des panneaux sont les mêmes qu'avant, dans le même ordre ;
    - les maillages **en plus** sont les remplaçants de ceux en moins et les objets des CombiModel (77 / 134 / 101) ;
    - la note donne le compte par catégorie et par piste ;
  - **entités** : les demi-largeurs des checkpoints sont celles de « État vérifié », et la pose de départ est inchangée ;
  - **audit** : chaque vue est lue. Les changements visibles sont décrits par vue : largeur, décor des CombiModel ;
  - **audits des voitures** : identiques, ou écart expliqué ligne à ligne par la largeur ou un checkpoint (fichiers `details/`) ; sinon ⚠️ Blocked ;
  - **captures d'interface** : 0,00, sauf `race-hud` si la scène derrière le HUD change. L'écart est alors lu et décrit ;
  - `--smoke-frontend` et `--verify-legacy-import-profile` : code 0.
- **Commit** : `fix(racing-casa): read the width helpers and CombiModel items of the legacy files`

### ⏳ T1.3 — Aides de route : palmiers et lanternes

- **Objectif** : P2, pour le dernier élément. Le lecteur lit `RoadHelper` : palmiers et lanternes sont placés, et les plages de tunnel sont lues pour T2.1.
- **Fichiers** : `RacingGame.Gameplay/Track/LegacyTrackFileReader.cs` : nom d'élément `RoadHelper`, et le commentaire de classe.
- **Validation** :
  - build à 0 erreur ;
  - **lecture**, par la sonde : 8 / 12 / 35 aides de route (Beginner / Advanced / Expert), aux types et positions lus par `XmlSerializer` aux types nommés comme les fichiers ;
  - **export** contre celui de T1.2, par `compare_race_scene_exports.py` :
    - **aucun maillage en moins**. Les bannières et les panneaux sont donc identiques (D3), comme tout le reste ;
    - les maillages en plus ne sont que des palmiers (`AlphaPalm`, `AlphaPalm2`, `AlphaPalm3`, `AlphaPalmSmall`) et des lanternes (`Laterne`), au plus 53 / 89 / 330 lanternes ;
    - la note donne leur nombre par piste et par modèle ;
  - **original** :
    - `missingInLive`, `modelMismatch`, les écarts anciens et `additional-other:` sont ceux de T1.2 ;
    - `additional-scenery:` est celle de T1.2, plus les seuls palmiers et lanternes ajoutés, aux nombres de l'export ;
  - **audit** : les vues `helper-palms` et `helper-laterns` montrent palmiers et lanternes, ce qu'on lit sur les captures. Les autres vues ne changent que là où un palmier ou une lanterne est dans le champ ;
  - **audits des voitures** : identiques à ceux de T1.2 ; sinon l'écart est expliqué, ou ⚠️ Blocked ;
  - captures d'interface : celles de T1.2, ou écart de `race-hud` lu et décrit ; smoke à 0.
- **Commit** : `fix(racing-casa): read the road helpers of the legacy tracks`

---

## Phase 2 — Tunnels

### ⏳ T2.1 — Le générateur de route construit les tunnels (P4, P5)

- **Objectif** : les plages `Tunnel` lues en T1.3 deviennent la géométrie de l'original.
- **Fichiers** :
  - `RacingGame.Gameplay/Track/RaceTrackLayout.cs` et `RaceTrackLayoutTypes.cs` : le calcul des plages d'aides se fait hors de `includeScenery`. La propriété `TunnelSpans` donne leurs indices de début et de fin, dans l'ordre de la route ;
  - `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs` (ou le fichier de la route après S5) : `CreateTunnelModel` construit le maillage de P4, avec ses normales et ses coordonnées converties comme celles de la route ;
  - `RacingGame.Gameplay/Components/RaceRoadGeneratorComponent.cs` : le générateur ajoute `<piste>.Tunnel` à sa sortie quand la piste a des tunnels ;
  - le matériau du tunnel, par le chemin des matériaux de piste relevé en T0.1 (P5) :
    - `RacingGame.Gameplay/Import/RaceMaterialRecipes.cs` :
      - une recette `ForRoadTunnel(trackName, diffuseTexturePath, normalTexturePath)`, avec les valeurs de P5 et une puissance spéculaire de 24, le défaut de `Material` d'origine ;
      - les constantes `RoadTunnelTextureName` (`RoadTunnel.png`) et `RoadTunnelNormalTextureName` (`RoadTunnelNormal.png`) ;
    - `scripts/RgceAssetCooker/CookPlan.cs` : le rôle `RoadTunnel` dans `TrackRoles` et dans `BuildTrackMaterials`. L'outil écrit :
      - `Content/Materials/Track/RoadTunnel.material` (`Material.Track.RoadTunnel`) ;
      - `Content/Textures/RoadTunnel.texture` et `RoadTunnelNormal.texture` (`Texture.RoadTunnel`, `Texture.RoadTunnelNormal`, qui citent `Image.RoadTunnel` et `Image.RoadTunnelNormal`) ;
      - leurs entrées du catalogue ;
      `--verify` les couvre comme les autres ;
    - `RaceTrackMaterialAssets.RoadTunnel` (`LegacyTrackSceneFactory.Models.cs`), uuid5 de `Material.Track.RoadTunnel` ;
    - `RaceRoadGeneratorComponent` passe `RaceTrackMaterialAssets.RoadTunnel` au tunnel. Il ne s'agit pas d'une propriété sérialisée, contrairement au `material_asset_id` de la route : P4 n'en permet aucune nouvelle, et les `.world` ne changent pas. Le matériau du tunnel se règle donc dans l'asset cuit, pas dans l'inspecteur du générateur ;
    - le README de l'outil (cinq matériaux de piste) et `RacingGameCasaEngine/Assets/README.md` sont mis à jour en T3.2.
- **Validation** :
  - build à 0 erreur ; `check_gameplay_type_names.py` : 0 collision ;
  - **export** contre celui de T1.3 : un seul maillage en plus par piste, `<piste>.Tunnel`, rien en moins.
    - Sommets et indices : 404 et 1 800 (Beginner), 860 et 3 834 (Advanced), 896 et 3 852 (Expert).
    - Matériau : `Material.Track.RoadTunnel`, aux valeurs de P5 avec CullNone et une puissance spéculaire de 24. Le maillage projette des ombres ;
  - **outil de cuisson** : un second lancement n'écrit rien, et `--check` et `--verify` sortent avec le code 0 ;
  - **audit** : `helper-tunnel` montre le tunnel sur les 3 pistes, ce qu'on lit sur la capture. Les autres vues ne changent que là où un tunnel est dans le champ ;
  - **éditeur**, sur une copie de `Worlds/Test/TrackBeginnerGenerators.world`, à l'échelle 0,02 comme en T4.6 :
    - l'ouverture sort avec le code 0, et la section `Logs:` n'a que les avertissements connus ;
    - la capture lue montre la route élargie et le tunnel ;
    - deux `--save-project` donnent les octets du fichier committé, sans enfant généré ;
  - le nouveau matériau s'ouvre dans l'éditeur (code 0), par le contrôle du catalogue de S5 ;
  - **original** : les lignes de `TrackPlacementExporter` sont celles de T1.3. Seule `additional-other:` peut gagner une entrée par piste, celle qui porte le tunnel ;
  - audits des voitures identiques à ceux de T1.3 : le tunnel n'a pas de physique ;
  - smoke à 0.
- **Commit** : `feat(racing-casa): build the road tunnels of the legacy tracks`

---

## Phase 3 — Comparaison avec l'original et clôture

### ⏳ T3.1 — Références « après », comparaison avec l'original et contrôle visuel

- **Objectif** : D2 et P7.
- **Étapes** :
  1. Prendre `after/` comme `before/` en T0.2, avec les mêmes modes et la sortie de `TrackPlacementExporter`.
  2. Comparer `after/` à `before/` : la note résume, par piste, les écarts de T1.2, de T1.3 et de T2.1.
  3. Écrire dans la note la liste du contrôle visuel de l'auteur, avec les captures `helper-*` de `after/` à regarder à côté. Les endroits sont donnés en part du tour depuis la ligne de départ :
     - **Beginner** :
       - route jusqu'à 1,8 fois plus large vers 31 % ;
       - palmiers 10–22 % ;
       - lanternes 22–42 % et 65–75 % ;
       - tunnel 58–65 % ;
       - groupes des CombiModel : immeubles, rochers, palmiers, réservoirs, château de sable, ruines.
     - **Advanced** :
       - palmiers dès le départ (1–12 %) et 38–46 % ;
       - lanternes 12–26 %, 52–61 % et 91–100 % ;
       - tunnels 26–35 % et 61–62 % ;
       - route à 1,6 vers 68 % et à 0,75 vers 43 %.
     - **Expert** :
       - lanternes 2–6 %, 23–42 %, 56–83 % et 91–100 % ;
       - palmiers 6–12 %, 17–21 %, 42–56 % et 87–91 % ;
       - 9 tunnels courts 12–14 % ;
       - tunnel 83–86 % ;
       - route à 1,53 vers 55 %.
     - **Ce qui diffère par nature** :
       - les palmiers de l'original changent de modèle et de rotation à chaque lancement : seuls leur présence et leur côté se comparent ;
       - les lanternes alternent de côté, tournées vers la route ;
       - différences connues, hors chantier (« Hors périmètre ») : objets de remplissage aléatoires, dessous de la route, lens flare.
  4. Soumettre la liste à l'auteur. Ses remarques deviennent des points ouverts ou des tâches.
- **Validation** :
  - deux audits `after/` à au plus 0,5 l'un de l'autre, et deux exports identiques ;
  - `TrackPlacementExporter` sur `after/scene-a.json`, dans les deux sens :
    - `missingInLive` = 0, `modelMismatch` = 0, et les 4 écarts anciens par piste aux mêmes valeurs ;
    - `additional-scenery:` ne contient que `Banner*`, `Sign*`, `StartLight*`, les palmiers et `Laterne`, aux comptes de T1.3. `Windmill`, `AlphaTrain` et `Building5` n'y sont plus ;
    - `additional-other:` est celle de T2.1 ;
  - largeurs de route égales à l'original à 1e-4 (sonde) ;
  - **contrôle visuel de l'auteur** : la tâche reste 🧪 jusqu'à son retour.
- **Commit** : `test(racing-casa): record the references after reading the track helpers`

### ⏳ T3.2 — ADR, documentation, plan éditeur et vérification

- **Fichiers** :
  - `docs/decisions/00NN-rgce-reads-the-legacy-track-helpers.md` (P8) et l'index des ADR ;
  - `RacingGameCasaEngine/Assets/README.md` : le générateur de route construit aussi les tunnels, et les aides sont lues ;
  - `ai-agent/tasks/rgce-editor-project-tasks.md` : O7 passe à « Levé », avec un renvoi à ce plan. Une ligne dans S6 dit que ses références de parité partent de `artifacts/o7-track-helpers/after/` ;
  - ce plan et `ai-agent/README.md`.
- **Validation** : un `verifier` indépendant rend CONFIRMED sur la revendication suivante :
  - les aides et les CombiModel sont lus ;
  - les écarts entre `before/` et `after/` sont ceux de T1.2, de T1.3 et de T2.1 ;
  - la comparaison avec l'original est à 0 manquant, et à 0 objet en trop hors panneaux, bannières, feu, palmiers et lanternes ;
  - le monde de test s'ouvre et s'enregistre à l'identique.
  Il prend la claim, le diff de `before/` et les commits du chantier.
- **Commit** : `docs(adr): record the legacy track helpers read by RacingGameCasaEngine`

---

## Enveloppe, retour arrière et arrêts

- **Retour arrière** : chaque tâche est un commit sur `remaster`, qu'on annule par `git revert`, dans l'ordre inverse. T1.1 et T0.2 ne changent rien à l'écran. Annuler T1.3 retire les palmiers, les lanternes et les plages de tunnel : le générateur ne construit alors plus de tunnel, même avec T2.1. Les références de `artifacts/` restent.
- **Arrêts** (en plus des règles d'exécution) :
  - S5 pas close ;
  - un fait porteur changé en T0.1 ;
  - un écart inexpliqué de l'export ou de `TrackPlacementExporter` ;
  - un audit des voitures qui change sans explication ;
  - un constat P0 ou P1 du vérificateur ;
  - une décision de l'auteur qui manque.
- **Risques** :
  - **Coût d'image et de chargement** : sur Beginner, 134 objets de CombiModel et jusqu'à 53 lanternes de plus ; sur Expert, 101 et jusqu'à 330 ; s'y ajoutent les palmiers. Aucune référence de performance n'existe (T4.7 du plan éditeur). Les notes de T1.2 et de T1.3 donnent les nombres d'entités et de maillages par piste ;
  - **Filtre de doublons** : les tailles de RGCE (glTF) et celles de l'original (`.X`) peuvent écarter des objets différents (T1.2) ;
  - **Physique** : la demi-largeur et les checkpoints changent ; le jeu se conduit autrement là où la route s'élargit.

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | **Levé (T0.1).** Nom et forme exacts du chemin des matériaux de piste après S5 (P5) : recette, rôle de l'outil de cuisson, `Material.Track.RoadTunnel`, id constant du générateur. Écrits dans T2.1. | T0.1, T2.1 |
| O2 | Si le chargement ou l'image d'Expert se dégrade visiblement avec le décor ajouté, le remède (regroupement, instanciation) est à décider avec l'auteur, dans un autre chantier. | T1.2, T1.3 |

## Hors périmètre

- **Objets de remplissage aléatoires de l'original** (`Track.cs:1025-1108`, `autoGenerationNames` de `TrackObjectManager.cs`, dont 6 CombiModel) : RGCE ne les a pas, et ce n'est pas le défaut de lecture. C'est un chantier à part, à proposer.
- **Dessous de la route** (`RoadBack`, `Track.cs:365-463`) : RGCE ne le construit pas. Ses textures sont cataloguées. Même remarque.
- **Lens flare coupé dans les tunnels** (`Track.cs:1477,1496-1517`) : RGCE n'a pas de lens flare.
- **Cartes de normales** du tunnel : sans tangentes, comme pour tous les maillages (S5, T5.2).
- **Ombres par objet de l'original** (`isNearTrackForShadowGeneration`, `TrackObjectManager.cs:198-238`) : RGCE fait projeter des ombres à tout le décor, comme aujourd'hui.
- **Code mort** `GetNearestWidthScale` et `SampleLoopPoint` de `LegacyTrackSceneFactory` : relevé, non retiré.
- **Écarts anciens de `TrackPlacementExporter`** sur les checkpoints et le sol : ils viennent de l'outil, et on les garde comme repères.
- **Dépôt** : push, fusion, pull request.
