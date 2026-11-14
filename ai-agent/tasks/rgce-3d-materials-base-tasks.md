# Plan agent IA — Fidélité 3D, tranche S3 : socle commun des matériaux

Troisième tranche issue du banc de capture 3D. **Brouillon soumis à l'auteur le 2026-11-14 : aucune modification de code avant son approbation.**

Direction arbitrée par l'auteur le 2026-11-14 : « Socle commun puis PBR ». On ne poursuit plus la reproduction au pixel des formules d'éclairage de l'original (Phong par technique, réflexion multiplicative, verre) : le moteur recevra plus tard un matériau PBR (ADR-0013 du moteur, chantier séparé). Cette tranche ne fait que ce qui sert aux deux mondes : géométrie d'éclairage complète (tangentes, normal maps), textures manquantes, orientation du ciel, et retrait de ce que la conversion a ajouté sans équivalent dans l'original.

Ce plan touche RacingGame (branche `remaster`) et le moteur CasaEngine (sous-module, règles `CasaEngine/AGENTS.md`). Règle de l'auteur : pas de contournement, ce qui manque au moteur se corrige dans le moteur.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

1. La route est éclairée avec sa normal map, comme tous les autres maillages générés.
2. Le terrain porte sa texture de détail (écart M1) et n'a plus le spéculaire ajouté par la conversion.
2b. Les textures du monde de course ont des mipmaps, comme dans l'original.
3. Le ciel affiché a l'orientation de l'original (écart G7), et le fichier de ciel suit la convention du moteur (direction monde), celle qu'utilisera le PBR.
4. Les petites dérives de valeurs relevées par les vérifications sont corrigées.

Hors objectif, reporté au chantier PBR : modèle spéculaire, réflexions des panneaux et de la voiture, verre, tables de réglage spéculaire de la conversion (`RacingGameLegacyMaterialTuning`), peinture de la voiture. Reporté à S2b : ombres.

## État vérifié (2026-11-14)

Référence de départ : capture `artifacts/capture-bench/rgce-l4`, rapports `report-l4` (jeu à `9bd1693`, moteur à `b56ed923` sur `chantier/racinggame-lighting`). Sondes : critère 1 : 19 / 34.

Route :

- Original : technique `SpecularRoad20` en haut détail (`RacingGameOriginal/Core/Tracks/Track.cs:1218-1229` ; le mode capture force le haut détail, `Core/GameSettings.cs:97-99`), textures `Road` et `RoadNormal` (`Track.cs:73-74`), sommets `TangentVertex(position, uv, normale = up, tangente = right)` (`Core/Tracks/TrackVertex.cs:96-120`), normal map et texture sur les mêmes coordonnées.
- Conversion : maillage en `VertexPositionNormalTexture`, sans tangente (`RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs:268-281`), matériau sans normal map (`:981-996`). `RoadNormal.png` existe déjà en disposition standard (ADR-0016). Le moteur n'active le normal mapping que si le maillage a des tangentes (`CasaEngine/CasaEngine/Framework/Rendering/Shaders/RenderFeatureResolver.cs:139-152`).

Terrain :

- Original : `DiffuseWithDetail20` (`Core/Landscapes/Landscape.cs:1231-1233`) : `texture × (A + D × relief) × (2 × détail)`, détail échantillonné à `coordonnées × 24` (`Core/Content/Shaders/LandscapeNormalMapping.fx:498-520`, facteur fixé par `Core/Shaders/ShaderEffect.cs:673`), texture `LandscapeDetail` (`Landscape.cs:576-586`). Aucun spéculaire.
- Conversion : terrain avec `BasColor` et `NormalMap` seulement, spéculaire 0,05 puissance 6 (`Worlds/LegacyTrackSceneFactory.Terrain.cs:137-155`). `LandscapeDetail.tga` est déjà copiée en sortie (lien `RacingGame/Content/Textures`).
- Moteur : `LitDiffuseMaterial` n'a aucune seconde texture ni pavage (`Framework/Materials/Runtime/LitDiffuseMaterial.cs:17-34` ; `Content/Shaders/LitForward.fx:11-17` : emplacements de texture 0 à 6).
- Mipmaps : l'original construit ses 199 textures avec mipmaps (`RacingGameOriginal/Core/Content/RacingGameContent.mgcb`, `GenerateMipmaps=True` partout) et les filtre entre niveaux (`LandscapeNormalMapping.fx:85-91`). Le chargeur du moteur fait `Texture2D.FromStream` (`CasaEngine/CasaEngine/Framework/Assets/Loaders/Texture2DLoader.cs:12`) : un seul niveau. Toutes les textures 2D de la conversion sont donc sans mipmaps (`Worlds/LegacyTrackSceneFactory.cs:1055-1093`, `Components/LegacyCarVisualFactory.cs:354`) : scintillement à distance, et un détail pavé 24 fois donnerait du moiré.
- Mesure, dans l'état antérieur `rgce-l1` : les 12 sondes de terrain éclairé passent quand le spéculaire du terrain est à zéro (run `rgce-l1-diag`). Dans l'état actuel (`report-l4`), 6 échouent.

Ciel :

- Original : le cube est échantillonné avec `In.pos.xzy` dans son repère Z en haut (`Core/Content/Shaders/PreScreenSkyCubeMapping.fx:53`) : direction de cube = (x, z, y) de la direction monde.
- Conversion : repère converti par (x, y, z) → (x, z, −y) ; le shader du ciel échantillonne la direction monde telle quelle (`CasaEngine/CasaEngine/Content/Shaders/LegacySkyCube.fx:25-27`). Une direction monde de la conversion P correspond à la direction d'origine (P.x, −P.z, P.y), donc à la direction de cube (P.x, P.y, −P.z) : **le ciel de la conversion est le miroir en Z de celui de l'original** (déduit des deux shaders ; l'écart G7 est observé sur toutes les vues, la forme exacte du miroir n'est pas encore mesurée).
- Le même objet cube sert de ciel et de cube de réflexion du monde de course et du carrousel (`Bootstrap/RacingGameCasaEngineGame.cs:281-285, 313-347`, `UI/CarSelectionCarousel.cs:103`). `SkyCubeMap.dds` : cube DXT1 de 512, sans mipmaps (en-tête lu) ; le chargeur du moteur lit le DXT1 (`Framework/Assets/Loaders/TextureCubeLoader.cs:202-204`).
- Réflexions de l'original : toutes lisent le même cube (`Core/Shaders/ShaderEffect.cs:763, 809`), mais avec deux conventions. Avec l'échange (x, z, y), comme le ciel : `ReflectionSpecular` et `ReflectionSpecular20` (verre, `Core/Content/Shaders/NormalMapping.fx:452, 555, 634`), `ReflectionSimpleGlass.fx:130` (jamais chargé). Sans échange, donc incohérentes avec le ciel affiché : `SpecularWithReflection20` (panneaux, lampadaires, `NormalMapping.fx:495-496, 533-534`) et `SpecularWithReflectionForCar20` (peinture de la voiture, `:695-696`). La conversion lit le cube avec le vecteur de réflexion monde, sans échange (`CasaEngine/CasaEngine/Content/Shaders/LitForward.fx:193-198`), et n'active aujourd'hui la réflexion que sur le vitrage.

Dérives de valeurs (vérifications indépendantes de S2a) :

- Le diffus des modèles importés est tronqué au lieu d'être arrondi (`Bootstrap/LegacyGltfModelReader.cs:170-177`) : 212 au lieu de 213 pour les panneaux.
- `RoadColumnSegment.gltf` a 10 matériaux sans `legacyEffect` (ambiant 0 au lieu du défaut 40 de l'original) ; son seul maillage utilise le matériau 0, qui en a un : sans effet visible aujourd'hui.
- Un message périmé dans `Bootstrap/LegacyImportProfileVerifier.cs:210`.

## Décisions à arbitrer avant exécution

| Réf | Décision proposée | Alternative |
|---|---|---|
| D1 | Moteur : `LitDiffuseMaterial` gagne une texture de détail générique : `DetailTexture`, `DetailTiling` (défaut 1). La couleur de base est multipliée par `2 × détail(coordonnées × pavage)` (gris moyen = neutre), avant l'éclairage. Champs sérialisés optionnels ; sans texture, rendu inchangé. Reste valable pour un matériau PBR (détail d'albédo). | Paver et fusionner le détail dans la texture du terrain à la conversion : aucune modification du moteur, mais une texture de terrain de plusieurs dizaines de milliers de pixels de côté, ou une perte de finesse. |
| D2 | Ciel : l'asset est converti, comme les normal maps (ADR-0016). `SkyCubeMap.dds` est réécrit dans le repère de la conversion (miroir en Z) : le fichier suit alors la convention du moteur (une direction monde lit le texel que l'on voit dans cette direction), que le ciel affiché, les réflexions du moteur et le futur PBR utilisent tous. Le shader du moteur n'est pas modifié. Effet sur les réflexions : celles du vitrage (voiture, immeubles), que l'original lit avec l'échange, deviennent conformes à l'original ; aucune autre réflexion n'est active dans la conversion. Les réflexions sans échange de l'original (panneaux, peinture) ne sont pas reproduites : elles relèvent du chantier PBR et de la liste des comportements accidentels. | Inverser Z dans `LegacySkyCube.fx` : corrige le ciel affiché, mais le fichier reste contraire à la convention du moteur, et les réflexions du vitrage restent en miroir. |
| D3 | Moteur : la tâche E7 s'ajoute au plan du moteur existant (`ai-agent/tasks/lighting-ambient-shadow-tasks.md`) et à sa branche `chantier/racinggame-lighting`, sans nouvelle branche. | Nouvelle branche par chantier, chaînée sur la précédente. |
| D4 | Le spéculaire de la route, des rails et des modèles importés n'est pas retouché ici (il sera remplacé par le PBR). Seul le spéculaire du terrain est retiré, parce que l'original n'en a pas. | Aligner aussi les autres spéculaires sur l'original : travail jeté au passage au PBR. |
| D5 | Moteur : `Texture2DLoader.GenerateMipmaps`, option publique, fausse par défaut. Le jeu l'active pour les textures du monde de course. | Mipmaps activés par défaut dans le moteur : toutes les textures de tous les projets changent (interface comprise, mémoire +33 %). Ou mipmaps écrits par le convertisseur dans un format que le moteur lit déjà : aucun chargeur 2D du moteur ne lit de niveaux aujourd'hui. |

## Règles d'exécution pour l'agent

- RacingGame : branche `remaster`, un commit par tâche, `git add` fichier par fichier, jamais les fichiers de l'auteur (`CasaEngine` pointeur, `.codegraph/`, `.serena/`, `MGUI/`, `log.txt`), aucun push. Aucun commit de RacingGame ne contient de chemin sous `CasaEngine/`.
- CasaEngine : `AGENTS.md` s'applique (jamais `main`, build des deux solutions, tests, démo pour toute fonction visible, ADR pour toute API publique ou champ sérialisé, pas d'allocation ni de journal dans les chemins chauds, formats sérialisés stables). Exception déjà arbitrée : la branche `chantier/racinggame-lighting` part de `chantier/gltf-static-tangents`. Le sous-module imbriqué `MGUI` n'est jamais indexé.
- Une seule tâche à la fois ; une tâche bloquée passe en ⚠️ avec sa question dans « Points ouverts », et on s'arrête.
- Budget : au plus deux cycles « mesure → correction » par tâche après la première implémentation ; au-delà, ⚠️ avec les mesures.
- Retour arrière : chaque tâche est un commit, annulable par `git revert` dans son dépôt ; annuler G3 avant E7.
- Toute affirmation se prouve en relançant le banc (`scripts/TrackCaptureCompare/README.md`) contre la référence `--seed 1`.
- Langue : ce plan en français ; code, commits, ADR et docs en anglais.

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Validation globale

- `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` : 0 erreur ; moteur : les deux solutions à 0 erreur, `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj` sans échec (5097 tests au départ).
- `RacingGameCasaEngine.exe --verify-legacy-import-profile` : code 0.
- Mesure de scène inchangée (points de tracé 0,000 ; 0 objet sans correspondance sur les trois circuits).
- Sondes (`probes.json`), critère 1 : les 12 sondes `terrain-lit-*` (Beginner-start 1 à 3, Beginner-sector-50 1 à 3, Beginner-overview 1 à 3, Expert-sector-80 1 à 3) et les 12 sondes `terrain-away-*` des mêmes vues passent ; dans `report-l4`, 6 des 12 `terrain-lit` échouent (Beginner-start 2, Beginner-sector-50 1 à 3, Beginner-overview 2 et 3). Aucune sonde hors route qui passe dans `report-l4` n'échoue. Les sondes de route (`road-*`) sont notées avant et après, sans seuil et hors de la règle de non-régression : leur écart relève du spéculaire, reporté au PBR. Les critères 2 et 3 restent hors périmètre (S2b).
- Banc : pour chacune des 15 vues, l'écart moyen par canal ne monte pas par rapport à `report-l4`.
- Vérification indépendante (`verifier`) à la fin, sur l'affirmation exacte de cette section plus les critères de chaque tâche.

Ordre d'exécution : G1 → G2 → E7 → E8 → G2b → G3 → G4 → G5 → G6.

---

## Phase 1 — Route et terrain (RacingGame)

### ✅ G1 — La route porte ses tangentes et sa normal map

> Validation (2026-11-14), run `rgce-g1`, rapport `artifacts/capture-bench/report-g1` (contre l'original) et `report-l4-vs-g1` (`rgce-l4` contre `rgce-g1`, sans correction de détail) : build 0 erreur ; mesure de scène inchangée (points de tracé 0,000 ; 0 objet sans correspondance sur les trois circuits ; exit=0). Maillage de route en `VertexPositionNormalTextureTangent` : tangente = `ConvertLegacyDirection(point.Right)` (déjà normalisée), `w = +1` ; normale, positions, coordonnées de texture, indices et réglages du matériau inchangés ; `RoadNormal.png` liée par `LoadProjectTexture`. **Aucune sonde hors route ne change** : les lignes de `probes.md` de `rgce-l4` et `rgce-g1` sont identiques au caractère près (couleur et écart-type) hors route ; critère 1 : 19 / 34 (inchangé), critère 2 : 0 / 4, critère 3 : 27 / 33 (inchangés). Sondes de route, avant (`rgce-l4`) → après (`rgce-g1`), couleur candidate (écart-type), original entre parenthèses : Beginner-start `road-lit-1` 79,87,88 (4,4) → 78,86,87 (7,6) [45,51,51] ; `road-lit-2` 71,77,81 (6,9) → 72,78,81 (13,9) [48,53,55] ; `road-lit-3` 81,88,90 (4,2) → 81,89,91 (8,8) [47,52,53] ; Beginner-sector-50 `road-lit-1` 94,105,109 → 92,102,106 [81,90,94] ; `road-lit-2` 98,110,115 → 97,110,114 [85,96,100] ; `road-lit-3` 70,78,79 → 69,77,78 [63,70,72] ; `road-shadow-1` 60,67,69 → 61,68,70 [15,17,19] ; `road-shadow-2` 63,70,71 → 63,70,71 [18,20,21] ; `road-shadow-3` 53,61,64 → 53,60,63 [18,21,23]. Les moyennes de route bougent de 0 à 3 niveaux (l'écart-type, lui, double presque : le relief apparaît), l'écart de moyenne reste celui du spéculaire (O2). **Image** (`Beginner-sector-50.png`, comparée à `original-s1a/Beginner-sector-50-nopost.png`) : le relief de l'asphalte (grain, fissures, joints de dalles) est visible au premier plan, sans couture, sans zone noire, sans relief inversé apparent ; la route de l'original reste plus contrastée (reflet spéculaire de l'original au centre, que le plan renvoie au PBR) ; la différence `rgce-l4` / `rgce-g1` (x4) ne touche que la route, en fin grain. Aucun cycle de correction consommé. **Nouvelle référence de l'écart moyen (A4), `report-g1`, rouge / vert / bleu :** Beginner-start 21,84 / 21,19 / 18,92 ; Beginner-sector-20 34,34 / 34,99 / 34,72 ; Beginner-sector-50 20,34 / 19,73 / 17,25 ; Beginner-sector-80 29,28 / 29,70 / 25,98 ; Beginner-overview 19,12 / 17,76 / 16,04 ; Advanced-start 22,43 / 21,47 / 18,67 ; Advanced-sector-20 28,34 / 24,53 / 20,45 ; Advanced-sector-50 18,89 / 18,55 / 16,38 ; Advanced-sector-80 17,45 / 17,02 / 14,66 ; Advanced-overview 18,41 / 17,15 / 15,06 ; Expert-start 17,53 / 17,02 / 14,78 ; Expert-sector-20 24,32 / 23,36 / 19,05 ; Expert-sector-50 19,90 / 18,93 / 16,59 ; Expert-sector-80 16,36 / 15,37 / 13,14 ; Expert-overview 21,16 / 20,32 / 18,87. Par rapport à `report-l4`, l'écart baisse sur 11 vues (jusqu'à -0,41 sur Beginner-sector-50) et monte sur quatre vues où la route est petite : Beginner-overview +0,03 à +0,04, Expert-sector-80 +0,02, Advanced-overview et Expert-overview +0,01 (G1 n'est pas soumis à la règle, A4).

- Objectif : le maillage de route est construit en `VertexPositionNormalTextureTangent`, tangente = vecteur droite du point de tracé converti et normalisé, `w = +1`, normale = vecteur haut, comme `TrackVertex` de l'original ; le matériau de route lie `RoadNormal.png`. Positions, coordonnées de texture, indices, physique : inchangés.
- Fichiers : `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs` (`CreateRoadEntity`, `CreateRoadVertex`, `CreateRoadMaterial`).
- Validation : build ; mesure de scène inchangée ; les sondes hors route ne changent pas de plus de ±1 par canal entre la capture d'avant et celle d'après (la route seule change) ; sondes de route notées avant / après, sans seuil ; le relief de l'asphalte est visible sur `Beginner-sector-50`. Ombres et physique : inchangées (le maillage garde ses positions, ses indices et ses réglages de projection et de réception).
- Commit : `feat(racing-casa): give the road its tangents and its normal map`

### ⏳ G2 — Le terrain n'a plus de spéculaire

- Objectif : `SpecularColor` du matériau de terrain à zéro (l'original n'en a pas). Le sol de ville garde le sien (l'original le dessine en `DiffuseSpecular20`, `Core/Graphics/PlaneRenderer.cs:86-89`).
- Fichiers : `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.Terrain.cs`.
- Validation : build ; les 12 sondes `terrain-lit-*` passent le critère 1 (avec la correction de détail de l'outil, encore active à ce stade) ; aucune sonde hors route ne régresse. Indice antérieur, pris dans l'état `rgce-l1` et non dans l'état actuel : le run `rgce-l1-diag`, spéculaire de terrain à zéro, où les 12 passaient.
- Commit : `fix(racing-casa): remove the specular term the port added to the terrain`

## Phase 2 — Texture de détail (CasaEngine, branche `chantier/racinggame-lighting`)

### ⏳ E7 — Texture de détail sur `LitDiffuseMaterial` (D1)

- Objectif : `LitDiffuseMaterial.DetailTexture` (`Texture2D`), `DetailTextureAssetId` (`Guid`), `DetailTiling` (`float`, défaut 1, valeurs non finies ou ≤ 0 ramenées à 1). Dans `LitForward.fx`, la fonction `SampleBaseTexture` (`:170`), par laquelle passent tous les pixel shaders à texture (`:446, 468, 505, 553, 596, 649`, donc aussi `LitForward_PixelLighting_OneLight_Texture`), rend `texture × 2 × détail(coordonnées × DetailTiling)` sur les canaux de couleur quand une texture de détail est liée (alpha inchangé) ; uniforme drapeau `HasDetailTexture`, uniforme `DetailTiling`, et un échantillonneur `DetailTexture` sur l'emplacement de texture 7 (0 à 6 sont pris, `LitForward.fx:11-17`), en répétition. Aucune nouvelle technique. Sans texture de détail : sortie identique à aujourd'hui. `skinEffect.fx` n'est pas modifié (les maillages animés n'ont pas de détail ; écrit dans l'ADR). La passe d'ombres n'est pas touchée (le détail ne change pas l'alpha).
- Équivalence avec l'original : son terrain rend `D × (A + relief × Dc) × (2 × détail)` ; multiplier la couleur de base par `2 × détail` avant l'éclairage donne le même produit, le terrain n'ayant ni spéculaire (G2) ni réflexion.
- Sérialisation : deux propriétés nouvelles dans la définition `lit-diffuse` (`Framework/Materials/Definitions/BuiltInMaterialDefinitions.cs`) : `detail_texture` (texture, référence d'asset, défaut vide, alias `texture_detail_asset_id`) et `detail_tiling` (flottant, défaut 1), lues par `MaterialCompiler` comme `normal_texture` l'est (`Framework/Materials/Compilation/MaterialCompiler.cs:296-301`), par `LitDiffuseMaterial.Load`, et écrites par l'écrivain d'asset de matériau (`CasaEngine.EditorServices/EditorAssetJsonSerializer.cs`, vers `:1088`). Un asset sans ces clés se charge comme aujourd'hui (format stable). Conséquence côté éditeur, écrite dans l'ADR : l'inspecteur de matériau, piloté par la définition, affiche les deux nouvelles propriétés ; aucun autre code d'interface n'est modifié.
- Fichiers, côté runtime (`CasaEngine/CasaEngine/`) : `Content/Shaders/LitForward.fx`, `Framework/Materials/Runtime/LitDiffuseMaterial.cs`, `Framework/Rendering/Shaders/ShaderParameterNames.cs`, `Framework/Materials/Definitions/BuiltInMaterialDefinitions.cs`, `Framework/Materials/Compilation/MaterialCompiler.cs`. Côté éditeur et outillage : `CasaEngine.EditorServices/EditorAssetJsonSerializer.cs`. Tests : `CasaEngine.Tests/` (tests du matériau, `Rendering/LightingShaderCoverageTests.cs`). Démo : `CasaEngine.Demos/Demos/StaticShadowValidationDemo.cs`. Documents : nouvel ADR et index dans `docs/decisions/`, page et index dans `docs/engine/`, plan `ai-agent/tasks/lighting-ambient-shadow-tasks.md` (tâche E7 ajoutée).
- Tests : défauts ; normalisation du pavage ; aller-retour d'un asset de matériau portant une texture de détail et un pavage à travers la définition et `MaterialCompiler` ; chargement d'un asset sans les nouvelles clés ; assertions de source dans `LightingShaderCoverageTests` (uniformes et échantillonneur déclarés, multiplication gardée par le drapeau, placée dans `SampleBaseTexture`).
- Démo : en configuration de mesure (`CASAENGINE_SHADOW_DEMO_VALIDATION=1`), `CASAENGINE_SHADOW_DEMO_DETAIL=1` lie au receveur une texture de détail unie de valeur 64 / 255 avec un pavage de 4. Le receveur de la démo est texturé, sans normal map, sous une seule lumière directionnelle : il passe par `LitForward_PixelLighting_OneLight_Texture`.
- Validation : builds, tests. Captures de démo prises dans la même session, avant la modification (au `HEAD` actuel du moteur) puis après : sans variable, et en configuration de mesure sans détail, identiques au bit près avant et après ; avec le détail : le pixel `ombre` du receveur (ambiant seul, 64 sans détail) vaut `64 × 2 × 64 / 255 ≈ 32` à ±3, le pixel `eclaire` est multiplié par le même facteur 0,502 à ±3, le pixel `reflet` est inchangé (emplacements des pixels : plan du moteur, E0).
- Commit (moteur) : `feat(materials): add a detail texture to the lit diffuse material`

## Phase 3 — Terrain, ciel, nettoyage (RacingGame)

### ⏳ E8 — Le chargeur de textures peut construire une chaîne de mipmaps (D5)

- Objectif : `Texture2DLoader` gagne une option publique `GenerateMipmaps` (propriété d'instance, défaut `false` = comportement actuel). Quand elle est vraie, `LoadAsset` rend une texture dotée de la chaîne complète de niveaux, chaque niveau calculé par moyenne 2 × 2 du précédent (arrondi au plus proche, dimensions impaires gérées, jusqu'à 1 × 1), dans le même format de surface que le niveau 0. Le niveau 0 est identique au bit près à celui d'aujourd'hui. Le calcul se fait au chargement, hors des chemins chauds.
- ADR (API publique) ; documentation dans `docs/engine/` ; tâche E8 ajoutée au plan du moteur.
- Fichiers (`CasaEngine/`) : `CasaEngine/Framework/Assets/Loaders/Texture2DLoader.cs`, un test dans `CasaEngine.Tests/` (le calcul des niveaux est isolé dans une fonction pure testable sans carte graphique : tailles des niveaux pour des dimensions paires, impaires et non carrées, valeurs moyennées, niveau 0 inchangé), `docs/decisions/`, `docs/engine/`, `ai-agent/tasks/lighting-ambient-shadow-tasks.md`.
- Validation : builds des deux solutions, tests ; démo d'ombres sans variable : capture identique au bit près avant et après (aucun appelant du moteur n'active l'option).
- Commit (moteur) : `feat(assets): let the texture loader build a mipmap chain`

### ⏳ G2b — Les textures du monde de course ont leurs mipmaps

- Préalable : E8.
- Objectif : les textures 2D chargées pour le monde de course (`LegacyTrackSceneFactory.LoadTexture` / `LoadProjectTexture`, `LegacyCarVisualFactory`) le sont avec `GenerateMipmaps = true`, comme l'original, dont les 199 textures sont construites avec mipmaps (`RacingGameOriginal/Core/Content/RacingGameContent.mgcb`). Les textures d'interface et le carrousel ne changent pas. Les échantillonneurs des matériaux (`AnisotropicWrap`) filtrent déjà entre niveaux.
- Fichiers : `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs`, `Components/LegacyCarVisualFactory.cs`.
- Validation : build ; mesure de scène inchangée ; une assertion au chargement (journal) confirme `LevelCount > 1` pour les textures de route et de terrain ; écart moyen des 15 vues non supérieur à celui de G2 ; aucune sonde hors route ne change de verdict ; le scintillement des textures lointaines disparaît sur `Beginner-overview` (planche).
- Commit : `fix(racing-casa): load the race world textures with mipmaps`

### ⏳ G3 — Le terrain porte sa texture de détail

- Préalable : E7, G2b.
- Objectif : le matériau de terrain lie `LandscapeDetail.tga` en texture de détail, pavage 24, comme l'original. Les commandes du banc passent à `--no-terrain-detail-correction` (les deux jeux ont maintenant le détail) : `scripts/TrackCaptureCompare/README.md` mis à jour.
- Fichiers : `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.Terrain.cs`, `scripts/TrackCaptureCompare/README.md`.
- Validation : build ; sans correction de l'outil, les 12 sondes `terrain-lit-*` et les 12 `terrain-away-*` passent le critère 1 ; le grain fin est visible au premier plan de `Beginner-start` ; la texture de détail liée a `LevelCount > 1` (assertion au chargement) et le terrain lointain ne montre pas de moiré sur `compare-Beginner-overview.png` ni `compare-Expert-sector-80.png` ; écart moyen des 15 vues non supérieur à celui de G2b.
- Commit : `feat(racing-casa): give the terrain its detail texture`

### ⏳ G4 — Le ciel et le cube de réflexion dans le repère de la conversion (D2)

- Objectif : d'abord mesurer : une capture de diagnostic non committée, où le cube est échantillonné en (x, y, −z), est comparée à l'original ; si le ciel s'y superpose, la forme du miroir est confirmée ; sinon la tâche passe en ⚠️ avec les images. Ensuite : `scripts/LegacyModelGltfConverter` gagne la conversion du cube (`--sky-cube <dds>`), qui écrit `RacingGameCasaEngine/Content/Textures/SkyCubeMap.dds` tel que `nouveau(P) = ancien(P.x, P.y, −P.z)`, sans perte (les blocs DXT1 sont permutés, pas recompressés) ou, à défaut, en format non compressé que `TextureCubeLoader` lit ; la conversion charge ce fichier (le lien vers le fichier d'origine de même nom est retiré de la copie en sortie). Le shader du moteur n'est pas modifié. La tâche ne prétend rien sur les réflexions sans échange de l'original (panneaux, peinture). ADR-0016 complété (ou nouvel ADR) : les assets sont convertis vers les conventions du moteur.
- Fichiers : `scripts/LegacyModelGltfConverter/` (code, README), `RacingGameCasaEngine/Content/Textures/SkyCubeMap.dds` (nouveau), `RacingGameCasaEngine/RacingGameCasaEngine.csproj`, `docs/decisions/`.
- Validation : build ; sur les 15 vues, les contours du ciel (montagnes lointaines, nuages) se superposent à ceux de l'original (planches) ; l'écart moyen par canal baisse sur les vues où le ciel occupe plus d'un quart de l'image (`Beginner-sector-20`, `Advanced-start`, `Beginner-sector-50`) et ne monte sur aucune ; le fichier converti relu par l'outil redonne l'ancien cube après une seconde conversion (aller-retour identique au bit près si la conversion est sans perte).
- Commit : `fix(racing-casa): convert the sky cube to the port's axes`

### ⏳ G5 — Dérives de valeurs

- Objectif : le diffus des modèles importés est arrondi (212 → 213 pour les panneaux) ; un matériau importé sans `legacyEffect` prend l'ambiant 40 et le diffus 210 par défaut de l'original (`Core/Graphics/Material.cs:29-32`) ; le message périmé du vérificateur est corrigé.
- Fichiers : `RacingGameCasaEngine/Bootstrap/LegacyGltfModelReader.cs`, `Bootstrap/LegacyImportProfileVerifier.cs`.
- Validation : build ; `--verify-legacy-import-profile` code 0 ; aucune sonde ne change de verdict ; écart moyen des 15 vues non supérieur à celui de G4.
- Commit : `fix(racing-casa): round imported diffuse colours and default missing legacy materials`

### ⏳ G6 — Banc, constats, vérification indépendante

- Objectif : relancer le banc ; mettre à jour `docs/racinggame-3d-capture-bench-findings.md` (M1, M4 pour sa part de relief, G7) ; passe `verifier` sur la validation globale ; lister ce qui reste pour S2b et pour le chantier PBR.
- Validation : relecture par l'auteur.
- Commit : `docs(racing-casa): update the 3D findings after the material base slice`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Le maillage de route de la conversion utilise le vecteur droite normalisé, l'original le vecteur brut (point ouvert O3 de S1). G1 ne change pas les positions ; à traiter à part si l'auteur le souhaite. | G1 |
| O2 | Le spéculaire de la route reste celui de la conversion (0,14, puissance 18) : les sondes de route éclairée ne sont pas attendues dans la tolérance avant le chantier PBR. | G1 |
| O3 | Le cube converti change les réflexions de tous les matériaux de la conversion qui en ont une : aujourd'hui les seuls matériaux de vitrage (`Car.gltf` Standard_19, `Casino01.gltf` et `Hotel01.gltf` Material__39, `Hotel02.gltf` fenster) et le socle du carrousel (réflexion 0,35). Vues concernées : les trois vues de départ et les vues d'ensemble (voiture), `Beginner-sector-20` et les vues où figurent hôtels et casino, et l'écran de sélection. Capture avant / après jointe en G6 ; critère : les sondes hors ciel ne régressent pas. | G4 |
| O4 | Chantier PBR : plan séparé dans le dépôt du moteur (ADR-0013), après S2b. Les tables de réglage spéculaire de la conversion et les réflexions par technique de l'original y seront traitées, avec la liste des comportements accidentels de l'original à trancher un par un. | — |

## Hors périmètre

- Modèle spéculaire, réflexions, verre, peinture de la voiture, tables de réglage de la conversion (chantier PBR).
- Ombres (S2b) ; glow et lens flare (S4).
- Push, merge, pointeur du sous-module.

## Revues indépendantes

Trois revues, toutes REVISE. La troisième a confirmé le cœur du plan (miroir du ciel et conventions de réflexion, portée de `SampleBaseTexture`, coordonnées du terrain, tangente de route cohérente avec ADR-0016, arithmétique de la démo, comptes de sondes, fichiers et sérialisation de E7) et n'a bloqué que sur les deux tâches de mipmaps, ajoutées après la deuxième revue. Ses points sont intégrés dans les amendements ci-dessous, **qui n'ont pas été relus**.

## Amendements (ils prévalent sur le texte au-dessus)

- **A1 — G2b, chemins de texture.** Trois chemins créent une texture 2D du monde de course : `LegacyTrackSceneFactory.LoadTexture` (inclus), `LegacyCarVisualFactory.LoadTexture` (inclus) et `CarPaint.Load` (`Components/LegacyCarVisualFactory.cs:518-539`, textures de carrosserie créées par `new Texture2D(..., false, ...)` puis repeintes par `SetData` sur le niveau 0, `:583`) : inclus aussi. Les textures de `CarPaint` sont créées avec leur chaîne de niveaux, recalculée après chaque `Paint`, par la fonction de calcul des niveaux du moteur, qui est donc publique (écrit dans l'ADR de E8). Le carrousel partage le chargeur et le cache de `LegacyCarVisualFactory` (`UI/CarSelectionCarousel.cs:125, 192`) : ses voitures changent aussi ; seul son socle, qui a son propre chargeur (`:355`), ne change pas. L'assertion `LevelCount > 1` au chargement couvre une texture de chacun des trois chemins. Validation supplémentaire : sur la planche, le décor découpé lointain (palmiers, arbres) est comparé à l'original, sans seuil (l'original construit aussi ses niveaux en alpha non prémultiplié, `RacingGameContent.mgcb:230-243`).
- **A2 — E8, preuve sur une vraie carte graphique.** Les tailles de niveau suivent la convention de MonoGame : `max(1, dimension >> i)`, `floor(log2(max(largeur, hauteur))) + 1` niveaux. La validation ajoute une exécution réelle avec l'option active : sous une variable d'environnement, `StaticShadowValidationDemo` charge une image PNG et une image TGA, dont une de dimensions impaires et non carrées, par `Texture2DLoader { GenerateMipmaps = true }`, journalise `LevelCount` et la taille de chaque niveau, et compare le niveau 0 lu par `GetData` à celui de `Texture2D.FromStream`. Le journal est noté dans le plan du moteur. Motif : une taille de niveau fausse lève une exception dans `SetData`, que `LoadTexture` de la conversion avale en rendant une texture nulle (`Worlds/LegacyTrackSceneFactory.cs:1089-1100`).
- **A3 — Échantillonneurs.** Le moteur ne règle que l'échantillonneur 0 (`Framework/Rendering/Draw/RenderStateCache.cs:54, 79`) ; sous DesktopGL, `DECLARE_TEXTURE` ignore l'indice d'emplacement et ne déclare aucun état (`Content/Shaders/Macros.fxh:28-35`). La normal map et la nouvelle texture de détail sont donc filtrées par le défaut du périphérique (linéaire entre niveaux, répétition). Le résultat visé tient ; l'ADR de E7 note que la répétition dépend de ce défaut.
- **A4 — Écart moyen.** La règle « l'écart moyen ne monte pas » se mesure, à partir de G2, contre le rapport pris après G1 (la route change en G1 et son spéculaire est hors périmètre), noté sous G1. G1 lui-même n'est pas soumis à cette règle. « Scintillement » se lit « bruit d'aliasing visible sur la planche ».
- **A5 — Retour arrière.** Annuler G2b avant E8, et G3 avant E7.
- **A6 — G5.** L'arrondi s'applique à toutes les couleurs que `ClampByte` convertit (`Bootstrap/LegacyGltfModelReader.cs:170-177`), pas seulement au diffus.
- **A7 — G4.** La capture de diagnostic demande une modification temporaire, non committée, de `CasaEngine/CasaEngine/Content/Shaders/LegacySkyCube.fx` dans le sous-module ; elle est annulée avant de continuer, et `git status` du moteur le montre. Le miroir en Z inverse aussi les texels à l'intérieur de chaque face : les blocs DXT1 voient leurs indices retournés, pas seulement leur ordre ; cela reste sans perte.
- **A8 — E7.** La clé JSON lue par `LitDiffuseMaterial.Load` et écrite par `SaveLitDiffuseMaterial` est `texture_detail_asset_id`, déclarée comme alias de `detail_texture` dans la définition, comme `normal_map_asset_id` l'est pour `normal_texture`.
