# Plan agent IA — Monde de course en PBR et en lumière linéaire

Chantier PBR côté jeu, préparé par [racinggame-pbr-preparation.md](../../docs/racinggame-pbr-preparation.md). **Approuvé par l'auteur le 2026-11-16** (points P1 à P9 compris).

Les décisions D1 → D8 ci-dessous ont été arbitrées avec l'auteur les 2026-11-15 et 2026-11-16 : **ce plan les applique, il ne les rediscute pas**. Les points P1 → P9 sont des arbitrages proposés par l'agent : l'auteur les valide ou les corrige en approuvant le plan.

Ce plan touche RacingGame (branche `remaster`) et, pour une seule tâche (E1), le moteur CasaEngine (sous-module, règles `CasaEngine/AGENTS.md`). Règle de l'auteur : pas de contournement côté jeu, ce qui manque au moteur se corrige dans le moteur. Tout autre manque du moteur découvert en route passe la tâche en ⚠️ Blocked.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

1. Le monde de course est rendu en `LinearHdr` (lumière linéaire, tonemapping, FXAA), avec un mode `Legacy` conservé pour le banc de capture.
2. Les matériaux du monde de course et de la voiture passent de `LitDiffuseMaterial` (Blinn-Phong) à `LitPbrMaterial` (metallic-roughness), avec un vernis (clear coat) sur la peinture des voitures.
3. Les reflets viennent de l'éclairage par l'image du ciel (IBL du moteur), et non plus du cube de réflexion par matériau.
4. Les tables de réglage spéculaire de la conversion (`RacingGameLegacyMaterialTuning`) sont supprimées.
5. Les quatre comportements accidentels de l'original arbitrés (A2, A5, A10, A11) sont appliqués.

## État vérifié du dépôt (2026-11-16)

Jeu à `734541a` sur `remaster` ; sous-module `CasaEngine` pointé sur `a200f12b`. Moteur : `main` local à `674dd64c` (PBR, pipeline couleur, IBL ; non poussé), branche `chantier/pbr-clearcoat-ssao-fxaa` (clear coat, SSAO, FXAA ; non fusionnée).

Jeu :

- Tous les matériaux du monde de course sont des `LitDiffuseMaterial`. Aucun symbole `LitPbr`, `ClearCoat`, `LinearHdr` ni réglage de pipeline couleur dans le code du jeu (`rg` sur `RacingGameCasaEngine`, hors `bin`, `obj`).
- Le monde de course est construit en code : `Worlds/RaceWorldFactory.cs:112-163` (`CreateRaceWorld`) ; ombres `:126-136` ; ambiant forcé à `AmbientSource.Flat`, blanc, intensité 1 (`:137-141`) ; lumière directionnelle d'intensité 1 (`:186-203`).
- Ciel : `Bootstrap/RaceSkySystem.cs:7-28` ; `Bootstrap/RacingGameCasaEngineGame.cs:274-346` (le monde de course reçoit déjà le cube du ciel comme `SpecularEnvironmentCubemap`, `:281-285` ; pipeline de vue `LegacySkyCube`, `:313-316`).
- **Le pipeline de vue du ciel appartient au jeu** : `Bootstrap/LegacySkyCubeViewPipeline.cs:8` n'implémente que `IViewRenderPipeline` et ne lie pas `ColorPipelineIsLinear` (`:92-95`). Conséquences en `LinearHdr` tant qu'il n'est pas adapté : le ciel n'est pas décodé ; la vue entière, interface comprise, passe par le tonemapping ; FXAA est ignoré (`CasaEngine/docs/engine/color-pipeline.md:98, 121, 128, 135`). Modèle moteur à suivre : `CasaEngine/CasaEngine/Framework/Rendering/SkyBackgroundViewPipeline.cs:101` et `IScenePhaseViewPipeline.cs:5-11`.
- Carrousel de sélection : monde et vue en cible de rendu à fond transparent, pipeline du jeu `UI/OpaqueGeometryAlphaViewPipeline.cs:15-36` qui écrit l'alpha de découpe (`UI/CarSelectionCarousel.cs:76, 101-107, 259-267`). En `LinearHdr` le tonemapping écrit toujours un alpha de 1 (`CasaEngine/CasaEngine/Content/Shaders/Tonemap.fx:49, 56`) : la découpe de la voiture sur le menu serait perdue.
- Matériaux construits en code : route `Worlds/LegacyTrackSceneFactory.cs:993-1010` ; terrain `LegacyTrackSceneFactory.Terrain.cs:140-169` (texture de détail pavée 24 fois, spéculaire 0) ; rails `LegacyTrackSceneFactory.GuardRails.cs:405-423` ; colonnes `:425-443` ; dessous de route, tunnel, sol de ville `LegacyTrackSceneFactory.RoadStructures.cs:308-368`.
- Matériaux importés (décor, voiture) : `LegacyTrackSceneFactory.cs:1031-1066` (`CreateImportedRuntimeMaterial` : texture, normal map, cube de réflexion, `DiffuseColor`, `AmbientColor`, `EmissiveColor`, spéculaire, file, découpe, culling), repli sans texture `:1155-1173` ; lecture glTF `Bootstrap/LegacyGltfModelReader.cs:67-148` ; profil d'import `Bootstrap/RacingGameLegacyMaterialImportProfile.cs:12-47`.
- Tables de réglage : `Bootstrap/RacingGameLegacyMaterialTuning.cs`, clés = nom du fichier de texture diffuse ; utilisées par `LegacyTrackSceneFactory.cs`, `RacingGameLegacyMaterialImportProfile.cs`, `LegacyImportProfileVerifier.cs`, `Components/LegacyCarVisualFactory.cs`.
- Voiture : `Components/LegacyCarVisualFactory.cs:180-238` ; parties reconnues par nom de matériau (`lack` ou `paint` peints, `chrome`, `gummi`, `glass`, `fenster`, `:240-258`) ; diffus fixe (210, 210, 210) hors vitres (`:223-226`) ; normal map toujours `Textures/RacerCarNormal.png` (`:35`, `:279-288`) ; peinture par CPU dans la texture, dont l'alpha sert de masque de peinture (`:507-589`) ; plateau du carrousel `UI/CarSelectionCarousel.cs:343-387`.
- Immeuble de verre : `Models/Hotel02.gltf`, matériau `fenster`, effet `ReflectionSimpleGlass.fx` ; rendu aujourd'hui opaque avec cube de réflexion (`RacingGameLegacyMaterialTuning.cs:103-105`).
- Ombres par objet : `LegacyTrackSceneFactory.cs:375-400`, `LegacyTrackSceneFactory.Scenery.cs:154-169, 267-272`. Découpe alpha : indice posé par le profil d'import (`RacingGameLegacyMaterialImportProfile.cs:12-15`).
- Banc de capture : `--capture-reference-views <views.json> --reference-capture-out <dir>` (`Bootstrap/RaceLaunchOptions.cs:26-30`, `Program.cs:20-29`) ; outil `scripts/TrackCaptureCompare` (README : commandes, critères de sondes) ; vérificateur `--verify-legacy-import-profile`.
- Pas de projet de tests dans le dépôt du jeu. Les documents `docs/racinggame-3d-capture-bench-findings.md:134, 178, 179, 222` et `docs/racinggame-pbr-preparation.md:15-20` citent les ADR d'éclairage du moteur sous leurs anciens numéros (0089 à 0093, devenus 0097 à 0101).
- Changements préexistants de l'auteur, à ne jamais indexer : `.codegraph/`, `.serena/`, `MGUI/`, `log.txt`.

Moteur :

- `LitPbrMaterial` (`CasaEngine/CasaEngine/Framework/Materials/Runtime/LitPbrMaterial.cs:25-62`) : `BaseColor`, `Metallic`, `Roughness`, `EmissiveColor`, `ClearCoat`, `ClearCoatRoughness`, `NormalScale`, textures de couleur de base, de normales, metallic-roughness, occlusion, émissive. Il passe dans la file transparente dès que `BaseColor.A` < 255 (`:152`). Le masque d'ombre suit la texture de base en file `AlphaTest` (`:54`). **Pas de texture de détail**, alors que `LitDiffuseMaterial` en a une (`LitDiffuseMaterial.cs:24, 47`).
- Unités : à intensité de lumière égale, le diffus d'une surface `lit-pbr` est π fois plus sombre que celui d'une surface `lit-diffuse` (`CasaEngine/docs/engine/pbr-rendering.md:40`). L'exposition étant globale, un monde qui mélange les deux matériaux ne peut pas être équilibré par l'exposition seule.
- L'IBL n'est générée qu'une fois un shader `lit-pbr` lié avec un cube source (`pbr-rendering.md:73`). `AmbientSource.Auto` avec un cube de monde change aussi l'ambiant des matériaux `lit-diffuse` (`CasaEngine/docs/engine/lit-ambient-and-ambient-source.md:25`).
- Démo de référence d'un monde construit en code en PBR : `CasaEngine/CasaEngine.Demos/Demos/PbrMaterialDemo.cs` (`ColorPipeline` `:328-331`, lumière d'intensité 1,4 `:109, 334`, IBL `:454-459`).
- Prochain numéro d'ADR libre du moteur : 0116. Dernier ADR du jeu : 0017.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Le monde de course passe en `LinearHdr`. Le banc de capture garde un mode `Legacy` pour les contrôles de régression de géométrie et de placement des ombres. Pas d'écran HDR requis : l'image est ramenée à l'écran par le tonemapping. Réversible. |
| D2 | Correspondance des matériaux : peinture de voiture = diélectrique avec vernis (clear coat) ; chromes = métal lisse ; rails et lampadaires = métal ; le reste (terrain, route, maçonnerie, panneaux, bannières, végétation) = diélectrique rugueux. |
| D3 | `RacingGameLegacyMaterialTuning` est supprimé avec la bascule. |
| D4 | A2 : la carrosserie garde sa propre normal map (`RacerCarNormal.png`), pas celle héritée du ciment. |
| D5 | A5 : les immeubles de verre sont un miroir (métal lisse opaque, reflets du ciel), pas des panneaux sombres. |
| D6 | A10 : les palmiers projettent des ombres en forme de feuilles (découpe alpha du matériau), comportement actuel de la conversion. |
| D7 | A11 : pas d'assombrissement global de 7 % ; la luminosité se règle par l'exposition du tonemapping. |
| D8 | Anti-crénelage FXAA (choix de l'auteur du 2026-11-15). |

## Points à valider par l'auteur

| Réf | Arbitrage proposé | Alternative |
|---|---|---|
| P1 | Le terrain passe lui aussi en PBR, pour que tout le monde de course ait la même unité de lumière. Il a besoin de sa texture de détail : le moteur l'ajoute à `lit-pbr` (tâche E1, même comportement que celle de `lit-diffuse` : la couleur de base est multipliée par `2 × détail(coordonnées × pavage)`). | Terrain laissé en `LitDiffuseMaterial` : π fois plus clair que le reste à lumière égale, à compenser par une teinte, ce qui est un contournement. |
| P2 | Valeurs de départ par famille (tableau « Correspondance »), ajustées à l'œil par l'auteur ensuite. Aucune texture metallic-roughness n'est créée : scalaires par matériau. | Peindre des textures metallic-roughness par modèle : hors périmètre. |
| P3 | Réglages du monde en `linear` : tonemapper `AcesApprox`, exposition 1, FXAA activé, bloom désactivé, SSAO désactivé (5,5 ms par image mesurés sur la démo). | SSAO ou bloom activés : un réglage à changer, à décider sur l'image. |
| P4 | Ambiant en `linear` et en `legacy`, une fois les matériaux en PBR : `AmbientSource.Auto` à partir du cube du ciel (IBL diffuse et spéculaire), `AmbientIntensity` 1, `SpecularIntensity` 1. Le cube de réflexion par matériau (`ReflectionCube`, `UseSceneReflectionCube`) n'est plus utilisé. | Garder l'ambiant plat blanc : pas de reflets sur les chromes ni sur le verre. |
| P5 | Le carrousel de sélection reçoit les matériaux PBR de la voiture mais **reste en `Legacy`** : sa vue a besoin d'un alpha de découpe que le tonemapping n'écrit pas. `lit-pbr` se rend dans les deux pipelines ; la voiture y aura la même matière, pas exactement la même teinte qu'en course. | Carrousel en `LinearHdr` : demande au moteur un tonemapping qui conserve l'alpha et un pipeline de carrousel en deux phases. Chantier moteur séparé. |
| P6 | Prérequis hors de l'autorité de l'agent : fusionner `chantier/pbr-clearcoat-ssao-fxaa` dans `main` du moteur, puis, après E1, fusionner la branche de E1 et avancer le pointeur du sous-module. L'auteur fait ou autorise ces gestes. | Pointer le sous-module sur une branche de chantier sans fusion. |
| P7 | Le mode `Legacy` du banc s'obtient par `--color-pipeline legacy`. Les matériaux y sont les mêmes (PBR), seule la chaîne de couleur change. À partir de T2.1 les couleurs du mode `legacy` ne sont plus comparables à l'original ni à la référence T0.2 (annoncé dans la préparation, §3.2) ; restent contrôlés : géométrie, placement des ombres, répétabilité. | Garder aussi les anciens matériaux derrière l'option : deux jeux de matériaux à entretenir, contraire à D3. |
| P8 | Lumière : l'intensité de la lumière directionnelle passe de 1 à π (3,1416) quand les matériaux sont en PBR, dans les deux modes, pour qu'une surface diffuse éclairée de face garde le niveau qu'elle avait. L'exposition reste à 1 ; l'auteur règle ensuite à l'œil. | Garder 1 et monter l'exposition : assombrit aussi le ciel et l'ambiant. |
| P9 | Propriétés des matériaux actuels sans équivalent PBR : `AmbientColor` par matériau et réglages de mélange de reflet abandonnés (l'ambiant vient de l'environnement) ; `DiffuseColor` devient `BaseColor` avec alpha forcé à 255 ; `EmissiveColor` conservée ; spéculaire et puissance remplacés par la correspondance. | Reproduire l'ambiant par matériau : contraire à la direction « pas de fidélité au pixel pour les matériaux ». |

### Correspondance (P2, P9)

Règles communes à toute conversion d'un `LitDiffuseMaterial` en `LitPbrMaterial` :

- `BaseColorTexture` = texture diffuse actuelle ; `NormalTexture` = normal map actuelle.
- `BaseColor` = `DiffuseColor` actuelle, **alpha forcé à 255** (sinon le matériau bascule en file transparente). Un matériau sans `DiffuseColor` explicite reçoit blanc.
- `EmissiveColor` conservée. `AmbientColor`, `SpecularColor`, `SpecularPower`, `ReflectionCube`, `UseSceneReflectionCube`, montants de mélange de reflet : abandonnés.
- File (`Queue`), `AlphaCutoff`, état de rastérisation (culling) : conservés tels quels. État d'échantillonnage : conservé si `LitPbrMaterial` l'expose ; sinon ⚠️ Blocked.
- Matériau de repli sans texture (`LegacyTrackSceneFactory.cs:1155-1173`, `LegacyCarVisualFactory.cs:315-334`) : `BaseColor` = sa couleur actuelle, ligne « Défaut ».
- La texture de la voiture garde son alpha de masque de peinture ; la file reste opaque (elle ne dépend que de `BaseColor.A`).

| Famille | Reconnue par | Metallic | Roughness | Autre |
|---|---|---|---|---|
| Terrain | Construit en code | 0 | 1 | Texture de détail pavée 24 fois (E1) |
| Route, dessous de route, tunnel, colonnes, sol de ville | Construits en code | 0 | 0,85 | |
| Rails : ceux construits en code, et tout matériau importé de texture `Leitplanke` (modèle `GuardRailHolder`) | Construits en code ; nom de texture | 1 | 0,45 | |
| Lampadaires, signaux (textures `Light`, `TLight`, `streetlamp`, `streetlamp2`) | Nom de texture | 1 | 0,5 | |
| Maçonnerie (liste « Masonry » actuelle) | Nom de texture | 0 | 0,9 | |
| Panneaux et bannières (liste « Signage » actuelle) | Nom de texture | 0 | 0,6 | |
| Végétation (`plants`, `PalmLeave`) | Nom de texture | 0 | 0,95 | Découpe alpha conservée |
| Industriel mat (liste « Matte industrial » actuelle, sauf `Leitplanke`) | Nom de texture | 0 | 0,7 | |
| Verre des immeubles (effet `ReflectionSimpleGlass.fx`, hors voiture) | Effet | 1 | 0,05 | Opaque (D5) |
| Voiture, peinture (`lack`, `paint`) | Nom de matériau | 0 | 0,5 | `ClearCoat` 1, `ClearCoatRoughness` 0,05 |
| Voiture, chromes (`chrome`) | Nom de matériau | 1 | 0,1 | |
| Voiture, vitres (`glass`, `fenster`) | Nom de matériau | 1 | 0,05 | Opaque, comme aujourd'hui |
| Voiture, pneus (`gummi`) | Nom de matériau | 0 | 0,9 | |
| Plateau du carrousel | Modèle `CarSelectionPlate` | 1 | 0,3 | |
| Défaut (aucune correspondance) | — | 0 | 0,8 | |

## Règles d'exécution pour l'agent

- **Branches** : jeu sur `remaster` (branche de travail de l'auteur, comme les plans précédents), jamais `master`. Moteur (E1) : branche dédiée `chantier/pbr-detail-texture` créée depuis `main` une fois P6 fait, jamais `main`.
- **Dépendances** : E1 → T0.1 → T0.2 → T1.1 → T2.1 → T2.2 → T2.3 (T1.1, T2.1 et T2.2 exigent T0.2 ✅) ; T3.1 après T2.2 ; T4.1 après T2.3 et T3.1 ; T4.2 en dernier. Une tâche bloquée arrête ce qui en dépend, pas le reste.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`.
- **Ne jamais pousser.** Ne jamais fusionner. Ne jamais déplacer le pointeur du sous-module sans l'accord de l'auteur (P6).
- **Ne rien inventer** : toute API utilisée existe dans le dépôt ou le moteur pointé. Sinon : ⚠️ Blocked, question dans « Points ouverts », arrêt de ce qui en dépend.
- **Manque du moteur** autre que E1 : pas de contournement côté jeu, ⚠️ Blocked avec la description du manque.
- **Build obligatoire** avant ✅ : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug`, 0 erreur. Pour E1 : les deux solutions du moteur et `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj --blame-hang-timeout 5m`.
- **Ne jamais indexer** `.codegraph/`, `.serena/`, `MGUI/`, `log.txt` (jeu) ni ` M MGUI` (moteur) : `git add` fichier par fichier.
- **Captures** : jamais pendant un build ; fenêtre du jeu laissée ouverte et non réduite ; une capture douteuse est refaite jusqu'à trois fois.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- Vérification visuelle manquante : `🧪 Needs testing`, avec ce qui manque.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` : 0 erreur.
- `RacingGameCasaEngine.exe --verify-legacy-import-profile` : succès.
- `RacingGameCasaEngine.exe --smoke-frontend` : succès.
- Banc en `--color-pipeline legacy` : deux captures successives identiques (différence 0,00) ; rapport de géométrie et critères de placement des ombres (classe ombre ou lumière des sondes) au niveau de la référence T0.2.
- Banc en `linear` : deux captures successives identiques ; aucune exception ni avertissement de pipeline dans le journal ; ligne `[CaptureBench] … fxaaPasses 1` pour chaque vue (T1.1).
- `rg RacingGameLegacyMaterialTuning RacingGameCasaEngine` et `rg "new LitDiffuseMaterial" RacingGameCasaEngine` : aucun résultat.
- Contrôle de l'auteur, à l'œil : une course sur chaque piste, le carrousel de sélection.

---

## Phase 0 — Prérequis et référence

### ✅ E1 — Moteur : texture de détail dans `lit-pbr`

- Objectif : P1. `LitPbrMaterial` gagne `DetailTexture`, `DetailTextureAssetId` et `DetailTiling` (défaut 1), avec la formule et les noms de `LitDiffuseMaterial` ; sans texture, rendu inchangé au bit près.
- Prérequis : P6 (première fusion) ; branche `chantier/pbr-detail-texture`.
- Fichiers (moteur) : `CasaEngine/Content/Shaders/LitPbr.fx`, `Framework/Materials/Runtime/LitPbrMaterial.cs`, la définition `lit-pbr` et son mappeur, `CasaEngine.Tests` (tests du matériau et test GPU), `docs/engine/pbr-rendering.md`, `docs/decisions/0116-…md` et l'index.
- Étapes :
  1. Lire comment `LitForward.fx` et `LitDiffuseMaterial` portent la texture de détail (drapeau `HasDetailTexture`, échantillonneur, pavage) ; vérifier qu'un échantillonneur reste libre dans `LitPbr.fx` (14 utilisés sur 16) ; sinon ⚠️ Blocked.
  2. Ajouter la texture au shader (multiplication de la couleur de base avant le décodage, comme `lit-diffuse`), au matériau, à la définition (clés additives, omises par défaut) et au mappeur.
  3. Tests : sérialisation aller-retour ; test GPU en trois cas : (a) sans texture de détail, image identique au bit près à celle d'avant la tâche ; (b) texture de détail grise 128, chaque canal à 1 niveau au plus de (a) (`2 × 128 / 255` = 1,0039 : aucun gris 8 bits n'est exactement neutre) ; (c) texture de détail grise 64, un pixel nommé de couleur de base moyenne est plus sombre que (a) d'au moins 20 niveaux.
  4. ADR-0116 ; documentation. Pas de démo dédiée : le test GPU et le terrain du jeu servent de preuve (écart assumé à `AGENTS.md` §6, à valider par l'auteur).
- Validation : builds des deux solutions ; suite complète verte ; empreintes de référence du moteur inchangées (`capture-v2.sh`, `lin-v2.sh`, `pbrdemo.sh`).
- Commit (moteur) : `feat(materials): add a detail texture to lit-pbr (ADR-0116)`
- Note de validation (2026-11-16) : commit moteur `48d720a6` sur `chantier/pbr-detail-texture`. Échantillonneur `DetailTexture` (15 sur 16 utilisés), clés additives `detail_texture` et `detail_tiling`, ADR-0116. La texture de détail n'agit qu'avec une texture de base, comme `lit-diffuse`. Builds des deux solutions : 0 erreur ; 6076 tests verts ; les douze empreintes de référence du moteur inchangées. Vérification indépendante : **CONFIRMED**, aucun constat bloquant, trois remarques P4. Non vérifié : terrain réel pavé 24 fois (tâche T2.1), compilation DX11 du shader.

### ✅ T0.1 — Avancer le moteur et corriger les renvois

- Objectif : le jeu compile contre le moteur qui contient le PBR, le clear coat, FXAA et E1 ; les documents citent les bons numéros d'ADR.
- Prérequis : P6 fait ou autorisé par l'auteur, E1 ✅.
- Fichiers : pointeur `CasaEngine` ; `docs/racinggame-3d-capture-bench-findings.md` ; `docs/racinggame-pbr-preparation.md`.
- Étapes :
  1. Vérifier que le sous-module pointe sur le commit désigné par l'auteur ; build du jeu.
  2. Remplacer ADR-0089 → 0097, 0090 → 0098, 0091 → 0099, 0092 → 0100, 0093 → 0101 dans les deux documents (huit lignes), après avoir vérifié chaque titre dans `CasaEngine/docs/decisions/README.md`.
  3. Inscrire D1 à D8 dans `docs/racinggame-pbr-preparation.md` (§3 et §4 : décision prise, date).
- Validation : build 0 erreur ; `--smoke-frontend` ; `rg "ADR-00(89|9[0-3])" docs` : aucun résultat.
- Commit : `chore(engine): point the CasaEngine submodule at the PBR work and fix the decision references`
- Note de validation (2026-11-16) : pointeur du sous-module avancé de `a200f12` à `48d720a6` (E1 sur `chantier/pbr-detail-texture`, qui contient `main` du moteur avec PBR, clear coat, SSAO, FXAA). Les cinq titres d'ADR ont été vérifiés dans `CasaEngine/docs/decisions/README.md` (0097 ambiant appliqué une fois, 0098 ambiant plat avec cube de réflexion, 0099 texture de détail, 0100 mipmaps, 0101 réglages d'ombre) puis les huit renvois corrigés (quatre dans chaque document). D1 à D8 inscrits dans `docs/racinggame-pbr-preparation.md` (§3 « Decisions taken », rappel en fin de §4). Build du jeu : 0 erreur (28 avertissements, dans le moteur et `ReferenceViewCaptureValidator.cs`, aucun dans du code modifié par cette tâche). `--smoke-frontend` : code 0, journal sans `[Warning]`, `[Error]` ni exception. `rg "ADR-00(89|9[0-3])" docs` : aucun résultat.

### ⏳ T0.2 — Référence du banc avant bascule

- Objectif : une capture de référence de la conversion dans son état actuel, sur le moteur avancé et avant tout changement.
- Prérequis : T0.1 ✅.
- Sources : `scripts/TrackCaptureCompare/README.md`.
- Étapes :
  1. Capturer les vues de référence deux fois ; vérifier la répétabilité (0,00).
  2. Produire le rapport et les sondes contre l'original ; noter dans la note de la tâche : géométrie, classe ombre ou lumière de chaque sonde, et pour une vue nommée le rapport de luminance « route éclairée / terrain éclairé » sur deux sondes nommées.
- Validation : deux captures identiques ; valeurs notées.
- Commit : `docs(racing-casa): record the capture bench reference before the PBR switch`

## Phase 1 — Pipeline couleur

### ⏳ T1.1 — Monde de course en `LinearHdr`, option `Legacy` pour le banc

- Objectif : D1, D7, D8, P3, P7. Les matériaux ne changent pas encore.
- Fichiers : `Worlds/RaceWorldFactory.cs`, `Bootstrap/LegacySkyCubeViewPipeline.cs`, `Bootstrap/RaceLaunchOptions.cs`, `Program.cs`, `scripts/TrackCaptureCompare/README.md`.
- Étapes :
  1. Option `--color-pipeline legacy|linear` (défaut `linear`).
  2. En `linear`, régler `ColorPipeline` du monde de course comme `PbrMaterialDemo.cs:328-331` : `LinearHdr`, `AcesApprox`, exposition 1, bloom désactivé, `AntiAliasing = Fxaa`, SSAO désactivé. En `legacy`, rien n'est réglé.
  3. `LegacySkyCubeViewPipeline` : lier `ColorPipelineIsLinear` d'après `frame.Environment.LinearColorActive` (modèle : `SkyBackgroundViewPipeline.cs:101`) ; implémenter `IScenePhaseViewPipeline` (`RenderScene` dessine le ciel puis appelle `RenderScene` du pipeline interne ; `SupportsScenePhase` délègue au pipeline interne).
  4. Preuve d'exécution de FXAA : pendant `--capture-reference-views`, le code de capture du jeu écrit une ligne de journal par vue capturée, `[CaptureBench] view <nom> pipeline <legacy|linear> fxaaPasses <n>`, où `n` est `RenderStats.FxaaPasses` de la vue de course pour l'image capturée. Fichier : celui qui pilote la capture des vues de référence (à repérer depuis `Program.cs:81-82`, à ajouter à la liste des fichiers dans la note). Rien n'est écrit hors du mode capture.
  5. Documenter l'option dans le README du banc.
- Validation : build ; banc `legacy` identique à T0.2 (différence 0,00 vue par vue) et `fxaaPasses 0` ; banc `linear` répétable, 0 exception, aucun avertissement de pipeline sans `IScenePhaseViewPipeline`, `fxaaPasses 1` sur chaque vue ; sur une vue nommée, un pixel du HUD est identique entre `legacy` et `linear` (l'interface n'est pas tonemappée) ; ciel décodé : pour une sonde de ciel nommée de valeur `legacy` L par canal, chaque canal en `linear` vaut `encode(ACES(decode(L)))` à ±4 niveaux, avec `decode(x) = (x / 255)^2,2`, `ACES(c) = c (2,51 c + 0,03) / (c (2,43 c + 0,59) + 0,14)`, `encode(c) = 255 × c^(1 / 2,2)` (`Tonemap.fx:52-56`) ; la note donne la valeur prédite décodée et la valeur prédite non décodée `encode(ACES(L / 255))`, et la sonde est choisie pour que les deux diffèrent de plus de 8 niveaux sur au moins un canal ; capture jointe.
- Commit : `feat(racing-casa): render the race world in linear HDR and keep a legacy mode for the bench`

## Phase 2 — Matériaux du monde

### ⏳ T2.1 — Correspondance PBR et matériaux construits en code

- Objectif : D2, P1, P9 pour le terrain, la route, le dessous de route, le tunnel, les colonnes, le sol de ville et les rails.
- Fichiers : nouveau `Bootstrap/RacingGamePbrMaterialMapping.cs` (le tableau et les règles communes « Correspondance », en un seul endroit) ; `Worlds/LegacyTrackSceneFactory.cs`, `.Terrain.cs`, `.GuardRails.cs`, `.RoadStructures.cs`.
- Étapes :
  1. Écrire la correspondance : famille → `Metallic`, `Roughness`, `ClearCoat`, `ClearCoatRoughness`, et une fonction unique qui applique les règles communes.
  2. Remplacer les `LitDiffuseMaterial` de ces sept familles par des `LitPbrMaterial`. Terrain : texture de détail et pavage 24 conservés (E1).
- Validation : build ; banc `legacy` et `linear` répétables, 0 exception ; géométrie et classe ombre ou lumière des sondes au niveau de T0.2 ; capture jointe. L'image est attendue plus sombre à ce stade (π, corrigé en T2.2) : le noter, ne pas le compenser ici.
- Commit : `feat(racing-casa): build the terrain, road, rails and road structures with PBR materials`

### ⏳ T2.2 — Lumière et éclairage par l'image du ciel

- Objectif : P4, P8.
- Fichiers : `Worlds/RaceWorldFactory.cs`, `Bootstrap/RacingGameCasaEngineGame.cs`.
- Étapes :
  1. Intensité de la lumière directionnelle : π, dans les deux modes.
  2. Environnement du monde de course, dans les deux modes : `AmbientSource.Auto`, `AmbientIntensity` 1, `SpecularIntensity` 1, cube du ciel comme source (il est déjà fourni comme `SpecularEnvironmentCubemap`), sur le modèle de `PbrMaterialDemo.cs:454-459`.
- Validation : build ; en `linear`, la ligne `[IblCache]` de génération apparaît exactement une fois dans le journal ; banc répétable dans les deux modes ; sur la vue nommée en T0.2, le rapport « route éclairée / terrain éclairé » en `linear` est à ±20 % de celui de T0.2, sinon l'écart est noté pour l'auteur (réglage à l'œil, O4) sans bloquer ; capture jointe.
- Commit : `feat(racing-casa): light the PBR race world from the sky cube and rescale the key light`

### ⏳ T2.3 — Matériaux importés du décor, verre des immeubles

- Objectif : D2, D5, D6, P9 pour le décor importé.
- Fichiers : `Worlds/LegacyTrackSceneFactory.cs` (`CreateImportedRuntimeMaterial`, repli), `Bootstrap/RacingGamePbrMaterialMapping.cs`, `Bootstrap/RacingGameLegacyMaterialImportProfile.cs`.
- Étapes :
  1. `CreateImportedRuntimeMaterial` produit un `LitPbrMaterial` par la fonction commune : famille reconnue par le nom de la texture diffuse (listes reprises des tables actuelles dans la correspondance) ou par l'effet pour le verre.
  2. Verre des immeubles : métal lisse opaque (D5).
  3. Découpe alpha et drapeaux d'ombre inchangés (D6).
- Validation : build ; `--verify-legacy-import-profile` : ses contrôles de découpe passent, ses contrôles d'indice de reflet sont adaptés ici s'ils échouent (sinon en T4.1) ; banc répétable ; captures jointes d'une vue avec `Hotel02` (reflet du ciel visible sur le vitrage) et d'une vue avec palmiers (ombre en forme de feuilles : une sonde nommée dans la trouée d'une ombre de palmier est classée « lumière »).
- Commit : `feat(racing-casa): map the imported scenery materials to PBR and make the glass buildings mirrors`

## Phase 3 — Voiture

### ⏳ T3.1 — Voiture en PBR avec vernis

- Objectif : D2, D4, P5, P9 pour la voiture en course et dans le carrousel.
- Fichiers : `Components/LegacyCarVisualFactory.cs`, `UI/CarSelectionCarousel.cs`, `Bootstrap/RacingGamePbrMaterialMapping.cs`.
- Étapes :
  1. Parties de la voiture en `LitPbrMaterial` par la fonction commune : peinture avec `ClearCoat`, chromes, vitres, pneus, repli. La peinture par CPU de la texture reste telle quelle.
  2. Normal map : `RacerCarNormal.png` sur la carrosserie (D4).
  3. Plateau du carrousel en PBR. Le carrousel reste en `Legacy` (P5) ; son monde reçoit le cube du ciel en `AmbientSource.Auto` et une lumière d'intensité multipliée par π, comme le monde de course.
- Validation : build ; `--smoke-frontend` ; `--capture-ui-screens` : l'écran de sélection montre la voiture **et le fond du menu reste visible autour d'elle**, 0 exception ; banc répétable ; captures jointes (voiture en course, carrousel).
- Commit : `feat(racing-casa): give the car a PBR clear-coated paint, in the race and in the carousel`

## Phase 4 — Nettoyage et clôture

### ⏳ T4.1 — Supprimer les tables de réglage

- Objectif : D3.
- Fichiers : suppression de `Bootstrap/RacingGameLegacyMaterialTuning.cs` ; `Bootstrap/RacingGameLegacyMaterialImportProfile.cs`, `Bootstrap/LegacyImportProfileVerifier.cs` et tout appelant restant.
- Étapes :
  1. Retirer les derniers usages ; supprimer le fichier. `ShouldEnableReflection` (`RacingGameLegacyMaterialImportProfile.cs:17`) est remplacé par le même test écrit sur place (effet `ReflectionSimpleGlass.fx`), pour que l'indice `Reflection` lu par `LegacyCarVisualFactory.cs:257` et `CarSelectionCarousel.cs:377` ne change pas.
  2. Adapter le vérificateur : il contrôle la famille PBR attendue (verre miroir, peinture vernie, découpe des palmiers) au lieu des indices de reflet.
- Validation : build ; les deux `rg` de la validation globale sont vides ; `--verify-legacy-import-profile` succès ; banc identique à celui de la tâche précédente dans les deux modes (différence 0,00).
- Commit : `refactor(racing-casa): delete the legacy specular tuning tables`

### ⏳ T4.2 — Décision, documentation, rapport

- Objectif : trace durable.
- Fichiers : `docs/decisions/0018-race-world-renders-in-linear-hdr-with-pbr-materials.md` et l'index ; `docs/racinggame-3d-capture-bench-findings.md` ; `docs/racinggame-pbr-preparation.md` ; `ai-agent/README.md`.
- Étapes :
  1. ADR-0018 : pipeline, correspondance des matériaux, lumière à π, A2, A5, A10, A11, carrousel laissé en `Legacy`, ce que le banc mesure encore.
  2. Mettre à jour les deux documents et le tableau des plans ; rapport de fin.
- Validation : validation globale complète ; liens des documents résolus.
- Commit : `docs(racing-casa): record the PBR switch of the race world`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Qualité de l'IBL tirée du cube du ciel (512, sans mipmaps, plage basse) : à juger sur l'image. | T2.2 |
| O2 | `Casino01` : sa liste de matériaux n'a pas été inspectée ; le mur noir du banc (A5) peut venir d'un matériau sans texture. À constater dans la capture de T2.3. | T2.3 |
| O3 | Carrousel en `LinearHdr` : chantier moteur séparé si l'auteur le veut (P5). | — |
| O4 | Valeurs de la correspondance, exposition et intensités : points de départ, à régler par l'auteur sur l'image. | T2.1 à T3.1 |

## Hors périmètre

- Textures metallic-roughness, occlusion ou émissives par modèle.
- Glow et lens flare de l'original (préparation, §2).
- Verre transparent ; carrousel en `LinearHdr`.
- Monde de l'éditeur, écrans d'interface.
- Toute modification du moteur autre que E1.

## Journal des revues

| Époque | Objet | Verdict | Suite |
|---|---|---|---|
| 1 | Plan entier | REVISE (P1 × 1, P2 × 4) | Tous **FIX**. Le pipeline du ciel est au jeu : il lie le décodage et passe en deux phases (T1.1), avec preuve de FXAA, du HUD et du ciel. Carrousel resserré : PBR en `Legacy` (P5). IBL déplacée après les premiers matériaux PBR, réglages d'ambiant écrits par mode (T2.2). Unité de lumière : terrain en PBR par une texture de détail ajoutée au moteur (P1, E1) et lumière à π (P8). Règles de conversion par propriété, dont l'alpha de `BaseColor` (P9, « Correspondance »). |
| 2 | Plan révisé | REVISE (P2 × 4) | Tous **FIX**. Critère du ciel par valeur prédite (décodage puis ACES) au lieu d'une fenêtre autour de `legacy` ; ligne de journal `[CaptureBench]` qui expose `FxaaPasses` ; T0.2 ordonnée entre T0.1 et T1.1 ; test GPU de E1 en trois cas avec tolérance pour le gris 128. |
| 3 | Relecture de clôture | REVISE (P2 × 1) | **FIX** sans nouvelle relecture (la boucle est close) : la texture `Leitplanke` importée (`GuardRailHolder`) rejoint la ligne « Rails », conformément à D2. P3 intégrés : remplacement de `ShouldEnableReflection` précisé en T4.1 ; absence de démo pour E1 signalée à l'auteur. P4 noté : une sonde d'ombre sans sonde éclairée sur la même surface peut être mal classée en T2.1, à contrôler à la main si le cas se présente. Points confirmés par le relecteur : API du moteur citées, budget d'échantillonneurs, formule du ciel, alpha du carrousel, couverture de tous les `new LitDiffuseMaterial`. |

## Passation (2026-11-16)

État à l'arrêt de la session : le plan est approuvé. Le chantier `chantier/pbr-clearcoat-ssao-fxaa` est fusionné dans `main` du moteur (avance rapide jusqu'à `6ead4730`, non poussé). E1 est fait et confirmé sur `chantier/pbr-detail-texture` (`48d720a6`), **pas encore fusionné**. Le pointeur du sous-module n'a pas bougé (`a200f12b`). Aucun code du jeu n'est modifié.

Suite, dans l'ordre : fusionner `chantier/pbr-detail-texture` dans `main` du moteur (avance rapide) et pointer le sous-module dessus, avec l'accord de l'auteur ; puis T0.1, T0.2, T1.1 et la suite selon la règle de dépendances.

À savoir pour reprendre :

- Scripts et empreintes de référence du moteur : `CasaEngine/CasaEngine/artifacts/pbr-baseline/` (ignoré par git, propre à cette machine et à son GPU). Empreintes en vigueur : journal du plan moteur `CasaEngine/ai-agent/tasks/pbr-clearcoat-ssao-fxaa-tasks.md`, dernière ligne.
- Tests du moteur : `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj --blame-hang-timeout 5m`, jamais `--no-build` (le projet n'est dans aucune solution).
- Git sur cette machine : `git -c safe.directory=*` ; message de commit dans un fichier puis `git commit -F` ; ne jamais pousser ; ne jamais indexer `.codegraph/`, `.serena/`, `MGUI/`, `log.txt`.
- Captures : jamais pendant un build ou des tests ; fenêtres laissées ouvertes et non réduites.
- À faire par l'auteur : push de `main` du moteur et de `remaster` ; contrôles manuels listés dans les deux plans du moteur.
