# Plan agent IA — Fidélité 3D, tranche S2 : lumière et ombres

Deuxième tranche de corrections issues du banc de capture 3D (`docs/racinggame-3d-capture-bench-findings.md`, écarts L1, G2, M5 pour sa part d'éclairage, point ouvert O4 de S1). **Brouillon soumis à l'auteur le 2026-11-14 : aucune modification de code avant son approbation.** Révision 3 après deux revues indépendantes (REVISE à cinq puis trois points, tous retenus et corrigés).

**Découpage.** La tranche est scindée en deux unités approuvables séparément :

- **S2a — ambiant et lumière** : T1, E0, E1, E2, T2, puis T2b (banc et constats intermédiaires). C'est l'unité soumise à l'approbation maintenant.
- **S2b — ombres** : E3 → E6, T3, T4. Décrite ici pour le cadre ; elle repasse en revue indépendante puis devant l'auteur avant son exécution, avec les mesures de S2a.

Ce plan touche deux dépôts : RacingGame (branche `remaster`) et le moteur CasaEngine (sous-module `CasaEngine/`, règles `CasaEngine/AGENTS.md`). L'auteur a demandé le 2026-11-14 : « Pas de contournement : si le bug est dans le moteur on le corrige. »

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Sur les 15 vues du banc, une surface de la conversion reçoit la même lumière que dans l'original : même direction du soleil, même terme ambiant, ombres portées de même direction, de même teinte (jamais noires) et à bord doux, sur les mêmes receveurs.

Hors objectif : textures de détail, normal maps manquantes, spéculaire, réflexions, peinture de la voiture, ciel (S3) ; glow, lens flare (S4).

## État vérifié (2026-11-14)

Original (`RacingGameOriginal/`) :

- Une seule lumière directionnelle. Direction vers la lumière : `LensFlare.DefaultLightPos = (8500, -7250, 15000)` normalisée (`Core/Graphics/LensFlare.cs:45-47`, appliquée par `Core/GameScreens/GameScreen.cs:40-42`).
- Couleur d'un pixel éclairé : `texture × (ambiant + diffus × N·L)`, avec les constantes du matériau (`Core/Content/Shaders/NormalMapping.fx:216, 402`).
- Constantes ambiant / diffus des matériaux générés :

  | Matériau | Ambiant | Diffus | Source |
  |---|---|---|---|
  | Défaut (`Material(texture, normal)`) | 40 | 210 | `Core/Graphics/Material.cs:30-32` |
  | Route | défaut | défaut | `Core/Tracks/Track.cs:73-74` |
  | Dessous de route | défaut | défaut | `Track.cs:78-79` |
  | Tunnel | 182 | 80 | `Track.cs:84-89` |
  | Colonnes (RoadCement) | défaut | défaut | `Track.cs:94-95` |
  | Rail de sécurité | 72 | 182 | `Track.cs:101-105` |
  | Terrain | 88 | 234 | `Core/Landscapes/Landscape.cs:576-581` |
  | Plan de ville | 32 | 200 | `Landscape.cs:593-596` |
  | Modèles | valeurs du `.X` | valeurs du `.X` | portées par `legacyEffect.floats` des glTF |

- Ombres (`Core/Shaders/ShadowMapShader.cs`, `ShadowMap.fx`, `ShadowMapBlur.cs`) : carte 2048 en haut détail, 10 échantillons, flou gaussien en demi-résolution, puis **multiplication de l'image entière** par `lerp(1, ShadowColor, ombre)` avec `ShadowColor = (0.25, 0.26, 0.27)` (`ShadowMap.fx:36, 240`) ; fondu avec la distance (`ShadowDistanceFadeoutMap`) ; portée d'environ 129 m autour de la voiture. Le terrain reçoit sans projeter ; bannières et panneaux projettent sans recevoir ; le dessous de route et le plan de ville ne reçoivent pas.

Conversion (`RacingGameCasaEngine/`) :

- Trois lumières directionnelles codées en dur (`Worlds/RaceWorldFactory.cs:48-53`) : principale à environ 19° de celle de l'original, plus deux lumières d'appoint sans équivalent.
- Le monde de course lie un cube de réflexion (`Bootstrap/RacingGameCasaEngineGame.cs:281-285`, `SpecularEnvironmentCubemap`). Le moteur en déduit `HasEnvironmentCubeTexture = 1` (`EnvironmentShaderBinder.cs:11-13`), donc l'ambiant suit la voie « environnement » : `cube(normale) × EnvironmentAmbientColor × ambiant du matériau` (`LitForward.fx:175-181`, `skinEffect.fx:168-176`), et non `AmbientColor × ambiant du matériau`.
- Les glTF portent les constantes de l'original (`legacyEffect.floats`), lues par `Bootstrap/LegacyGltfModelReader.cs:86-89`. La conversion force un plancher d'ambiant à 128 / 255 sur les modèles dont le nom commence par Sign, Banner ou Windmill (`Bootstrap/RacingGameLegacyMaterialImportProfile.cs:17-19, 55-58`, indice `BrightAmbient` ; le plancher lui-même est dans le moteur, `CasaEngine/CasaEngine/Framework/Assets/Loaders/LegacyImportedMaterialPresentation.cs:15, 25-29`). `Bootstrap/LegacyImportProfileVerifier.cs:77-82, 143, 203` vérifie cet indice.
- Ombres : carte 4096, portée 150, seule la lumière principale projette (`RaceWorldFactory.cs:43-46, 107-116`).
- Normal maps : depuis ADR-0016 (`b406f4f`, `8b30f98`, moteur `9c57de77`), toutes les surfaces à normal map de la conversion, maillages générés et modèles importés, décodent la même normale perturbée que l'original (canaux convertis ; tangentes mesurées contre les modèles de l'original, bitangente `cross(N, T)`, `w = +1`). `N·L` est donc comparable entre les deux jeux une fois la direction de lumière alignée.

Moteur (`CasaEngine/`, arbre de travail sur `chantier/gltf-static-tangents` à `9c57de77`, soit `main` à `778f6aa` plus la lecture de `TANGENT`) :

- **Ambiant multiplié deux fois par la couleur de surface.** `ComputeBaseAmbientTerm` rend déjà `couleur × DiffuseColor × ambiant` (`LitForward.fx:175-181` ; même chose dans `skinEffect.fx:168-176`), puis `ComposeLitSurfaceColor` multiplie `(direct + ambiant)` par la couleur (`Lighting.fxh:185-188`, appelée en `LitForward.fx:485-489, 509-513, 533-537, 557-561, 608-612, 659-663` et `skinEffect.fx:379-380`). L'ambiant arrivant à l'écran vaut donc `texture² × DiffuseColor × ambiant`.
- Dans le moteur, `DiffuseColor` est la teinte de base du matériau : elle multiplie la lumière directe (`Lighting.fxh:248`) et l'ambiant. La couleur passée à `ComposeLitSurfaceColor` est la texture seule. Le modèle du moteur, une fois le doublon retiré, est donc `texture × DiffuseColor × (N·L × lumière + AmbientColor × MaterialAmbientColor)`.
- **Chaîne de correspondance avec l'original**, qui calcule `texture × (A + D × N·L)` avec A et D les constantes ambiant et diffus du matériau : avec une lumière blanche d'intensité 1, `AmbientColor = 1` en source plate, `DiffuseColor = D` et `MaterialAmbientColor = A / D`, le moteur rend `texture × D × (N·L + A / D) = texture × (D × N·L + A)`. C'est l'identité visée par T2. `A / D` dépasse 1 pour le tunnel (182 / 80 = 2,275) ; le plafond à 1 de `LegacyImportedMaterialPresentation.cs:35` ne concerne que les modèles importés passant par ce résolveur.
- `ShadowSettings` (`Framework/Rendering/Shadows/ShadowSettings.cs`) : `Enabled`, `Resolution`, `DepthBias`, `NormalBias`, `MaxDistance`. Pas de teinte, pas de douceur, pas de fondu.
- Receveur (`Content/Shaders/Lighting.fxh:127-176`) : 4 échantillons ponctuels à ±0,5 texel ; hors de la carte, la surface est éclairée sans transition. Le facteur d'ombre ne multiplie que la lumière directe.
- Deux shaders consomment les ombres et composent la couleur éclairée : `LitForward.fx` (constantes d'ombre déclarées en `:136-145`) et `skinEffect.fx` (`:140-149`, registres c100-c105, tampon c90-c92). Tout nouvel uniforme lu par `Lighting.fxh` doit être déclaré dans les deux.
- `CasaEngine.Tests/Rendering/LightingShaderCoverageTests.cs:109-127, 147-160` fige par assertions sur le source la composition actuelle de l'ombre et de l'ambiant.
- Démo d'ombres existante : `CasaEngine.Demos/Demos/StaticShadowValidationDemo.cs`, capturable par la variable `CASAENGINE_CAPTURE_SCREENSHOT_PATH`.
- Le réglage « reçoit les ombres » existe déjà par composant (`PrimitiveComponent.ReceiveShadows`).

## Décisions à arbitrer avant exécution

| Réf | Décision proposée | Alternative |
|---|---|---|
| D1 | Moteur, ombre : `ShadowSettings.Mode`, `OccludeDirectLight` (défaut, comportement actuel) ou `MultiplyLitColor` : la couleur éclairée entière est multipliée par `lerp(1, ShadowColor, ombre)`, comme l'original. Plus `ShadowColor` (défaut noir). | Garder le seul modèle actuel et ajouter une « force » d'ombre : plus simple, mais ne reproduit pas l'original (l'ambiant n'y est pas assombri). |
| D2 | Moteur, douceur : `ShadowSettings.FilterRadius` en texels (0 = les 4 échantillons actuels ; au-delà, disque de Poisson fixe). | Passe de flou en espace écran comme l'original : fidèle au pixel, mais demande une chaîne de post-traitement que le moteur n'a pas (prévue en S4). |
| D3 | Moteur, fondu : `ShadowSettings.FadeDistance` : l'ombre s'efface sur cette distance avant `MaxDistance` (0 = coupure nette actuelle). | Ne rien faire : bord d'ombre visible. |
| D4 | Jeu : une seule lumière, direction de l'original convertie, intensité 1 ; ambiant du monde à 1, en voie « plate » (D8) ; les deux lumières d'appoint sont retirées ; le cube de réflexion du monde de course est conservé. | Garder les lumières d'appoint et ajuster les intensités : c'est un contournement. |
| D5 | Moteur : branche dédiée `chantier/racinggame-lighting` créée depuis `chantier/gltf-static-tangents` (elle-même issue de `main`, un commit : lecture de `TANGENT`, dont la conversion dépend désormais) dans le sous-module, un commit par tâche, aucun push, aucun merge. Le pointeur du sous-module dans RacingGame reste à l'auteur, non indexé. | L'auteur fournit une autre branche. |
| D6 | Les tâches moteur sont recopiées dans `CasaEngine/ai-agent/tasks/lighting-ambient-shadow-tasks.md` sur cette branche (règle du moteur : plan dans son dépôt), sans changer leur contenu. | — |
| D7 | Moteur, ambiant : corriger pour tous les appelants. `ComputeBaseAmbientTerm` et `ComputeSkinnedAmbientContribution` ne multiplient plus par la texture (elles rendent `DiffuseColor × ambiant`), la texture n'étant appliquée qu'une fois par `ComposeLitSurfaceColor`. `DiffuseColor` reste dans l'ambiant : c'est la teinte de base du matériau, la retirer décolorerait l'ambiant des matériaux sans texture. **Impact : toute scène existante du moteur voit son ambiant s'éclaircir** (d'un facteur 1 / texture). Traité comme un bug, avec ADR et note de migration. | Option par monde, défaut = comportement actuel : aucune scène ne change, mais le défaut reste faux. |
| D8 | Moteur, ambiant : `WorldEnvironmentSettings.AmbientSource`, `Auto` (défaut, comportement actuel : cube s'il y en a un) ou `Flat` (toujours `AmbientColor × ambiant du matériau`, même avec un cube de réflexion lié). Champ sérialisé optionnel. | Retirer le cube du monde de course : supprime les réflexions dont S3 dépend. |

## Règles d'exécution pour l'agent

- RacingGame : branche `remaster`, un commit par tâche, `git add` fichier par fichier, jamais les fichiers de l'auteur (`CasaEngine` pointeur, `.codegraph/`, `.serena/`, `MGUI/`, `log.txt`), aucun push. Aucun commit de RacingGame ne contient de chemin sous `CasaEngine/`.
- CasaEngine : `AGENTS.md` du moteur s'applique en entier (branche dédiée, jamais `main`, build des deux solutions, tests, sample pour toute feature visible, ADR pour toute API publique ou champ sérialisé, restauration de l'état GPU, pas d'allocation dans les chemins chauds, formats sérialisés stables : tout nouveau champ est optionnel à la lecture, son défaut garde le comportement actuel, sauf D7 qui est une correction assumée).
- Une seule tâche à la fois ; une tâche bloquée passe en ⚠️ avec sa question dans « Points ouverts », et on s'arrête.
- Retour arrière : chaque tâche est un commit ; l'annuler se fait par `git revert` de ce commit dans son dépôt. La branche du moteur n'est ni poussée ni fusionnée.
- Toute affirmation de fidélité se prouve en relançant le banc (`scripts/TrackCaptureCompare/README.md`) contre la référence `--seed 1`.
- Langue : ce plan en français ; code, commits, ADR et docs en anglais.

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Validation globale

- `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` : 0 erreur ; dans le moteur, `dotnet build CasaEngine.MonoGame.sln` et `CasaEngine.Editor.MonoGame.sln` : 0 erreur ; `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj` : aucun test en échec qui ne l'était pas en E0.
- `RacingGameCasaEngine.exe --verify-legacy-import-profile` : code de sortie 0.
- Mesure de scène de S1 inchangée (points de tracé 0,000 ; 0 objet sans correspondance).
- Sondes (outil de T1), sur les points versionnés dans `probes.json` :
  - sondes « éclairé » et « face opposée au soleil » : couleur de la conversion à ±12 (sur 255) de l'original par canal ;
  - sondes « à l'ombre » : rapport ombre / lumière de la conversion à ±0,05 de celui de l'original ;
  - chaque sonde « à l'ombre » de l'original est à l'ombre dans la conversion, et chaque sonde « éclairé » y est éclairée (même direction et même étendue d'ombre aux points sondés).
- Banc : pour chacune des 15 vues, l'écart moyen par canal ne monte pas par rapport à `report-t1`.
- Vérification indépendante (`verifier`) à la frontière d'intégration, après T4.

Ordre d'exécution : S2a = T1 → E0 → E1 → E2 → T2 → T2b ; puis, après nouvelle revue et accord, S2b = E3 → E4 → E5 → E6 → T3 → T4.

Validation de S2a seule : builds, tests, vérificateur du profil d'import, mesure de scène inchangée, critères de sonde « éclairé » et « face opposée au soleil ». Les critères « à l'ombre » et la règle « l'écart moyen ne monte pas » ne s'appliquent qu'à la fin de S2b : entre les deux, les ombres de la conversion restent celles du moteur actuel (lumière directe occultée), sur un ambiant devenu juste.

**Démo du moteur pour les captures.** `StaticShadowValidationDemo` ne lit aujourd'hui aucun réglage (`ConfigureSceneLighting`, `:32-66`, cube nul en `:39`). Chaque tâche moteur qui ajoute un réglage lui ajoute une variable d'environnement lue dans `ConfigureSceneLighting`, documentée dans le plan du moteur, pour que la capture par `CASAENGINE_CAPTURE_SCREENSHOT_PATH` montre le comportement non par défaut : `CASAENGINE_SHADOW_DEMO_AMBIENT_SOURCE` et `CASAENGINE_SHADOW_DEMO_REFLECTION_CUBE` (E2), `CASAENGINE_SHADOW_DEMO_MODE` et `CASAENGINE_SHADOW_DEMO_COLOR` (E3), `CASAENGINE_SHADOW_DEMO_FILTER_RADIUS` (E4), `CASAENGINE_SHADOW_DEMO_FADE_DISTANCE` (E5). Sans variable, la démo est inchangée.

La démo n'a aujourd'hui que des matériaux sans texture ni réflexion (`:100-125`) et une lumière d'appoint non ombrée d'intensité 0,22 (`:59-65`). Une variable `CASAENGINE_SHADOW_DEMO_VALIDATION=1`, ajoutée en E0, met la scène en configuration de mesure : un receveur texturé (texture unie de valeur connue, générée en mémoire), un objet réfléchissant (matériau avec `UseSceneReflectionCube`), lumière d'appoint éteinte. Les valeurs attendues des pixels de E1 et E2 se calculent sur cette configuration ; l'emplacement des pixels mesurés est noté dans le plan du moteur à la première capture (E0).

---

## Phase 1 — Mesurer (RacingGame)

### ✅ T1 — Sondes de lumière et d'ombre dans l'outil de comparaison

> Validation (2026-11-14) : `--probes`, `--terrain-detail`, `--no-terrain-detail-correction` ajoutés ; sans `--probes`, `report.md` et les 15 planches sont identiques au bit près à `report-t1`. 46 sondes sur 5 vues (`probes.json`). Original contre lui-même : critère 1 : 34 / 34, critère 2 : 4 / 4, critère 3 : 33 / 33. Valeurs de départ (original contre `rgce-t1`, rapport `artifacts/capture-bench/report-t1-probes/probes.md`) : critère 1 : 10 / 34 ; critère 2 : 0 / 4 (rapport ombre / lumière de l'original 0,23 à 0,28, de la conversion 0,66 à 1,06) ; critère 3 : 21 / 33. Facteur de détail du terrain : 0,996 / 0,980 / 0,996. Limites : la classe « face opposée au soleil » est jugée sur l'image, pas par un calcul de N·L ; une seule sonde sur un mur lointain ; aucune sonde sur les vues Advanced. Aucune sonde « face opposée et ombrée » (S2b).

- Objectif : `scripts/TrackCaptureCompare` accepte un fichier de sondes (`probes.json` : par vue, des points image nommés, chacun classé « éclairé », « à l'ombre » ou « face opposée au soleil » d'après l'image de l'original) et écrit pour chaque sonde la couleur moyenne (carré de 9 × 9 pixels) dans les deux images, puis par vue le rapport ombre / lumière de chaque côté et le verdict de chaque critère de la validation globale.
- Les sondes sont posées sur des surfaces dont S1 a prouvé l'alignement, de texture uniforme à l'échelle du carré : route, sol de ville, terrain proche, et, pour la classe « face opposée au soleil » (surface dominée par l'ambiant), uniquement des points que l'original n'ombre pas : versants de terrain sans objet projetant, et murs d'immeuble à plus de 129 m de la position de voiture de la vue (hors portée de la carte d'ombres de l'original). `probes.json` note pour chaque sonde de cette classe pourquoi l'original ne l'ombre pas. Les faces opposées au soleil des rails et des immeubles proches, que l'original ombre en plus (aucun test N·L au receveur, `ShadowMap.fx:130, 212-240`), forment une quatrième classe, « face opposée et ombrée », évaluée seulement en S2b. Le terrain de l'original multiplie en plus sa couleur par `2 × détail` (`LandscapeNormalMapping.fx:509-519`, texture `LandscapeDetail`, pavage ×24) : l'outil mesure la moyenne de `2 × LandscapeDetail` et divise par elle la couleur de l'original pour les sondes de terrain ; la valeur est notée dans le rapport. Aucune des 15 vues ne montre l'intérieur d'un tunnel (A / D = 2,275) : ce matériau est vérifié par ses valeurs dans le code, pas par sonde. Au moins 4 vues, au moins 3 sondes de chaque classe. Versionnées dans `scripts/TrackCaptureCompare/probes.json`.
- Fichiers : `scripts/TrackCaptureCompare/Program.cs`, `README.md`, `probes.json` (nouveau).
- Validation : rapport de sondes produit sur `original-s1a` / `rgce-t1` ; valeurs de départ notées sous cette tâche ; comparaison de l'original avec lui-même : tous les critères passent.
- Commit : `feat(scripts): probe light and shadow levels in the capture comparison`

## Phase 2 — Ambiant (CasaEngine, branche `chantier/racinggame-lighting`)

### ⏳ E0 — Branche, plan du moteur, état initial

- Objectif : créer la branche depuis `chantier/gltf-static-tangents` (D5) ; recopier E0 → E6 dans `CasaEngine/ai-agent/tasks/lighting-ambient-shadow-tasks.md` et l'ajouter au tableau de `CasaEngine/ai-agent/README.md` ; ajouter à `StaticShadowValidationDemo` la configuration de mesure `CASAENGINE_SHADOW_DEMO_VALIDATION=1` (voir « Démo du moteur pour les captures ») ; relever l'état initial : build des deux solutions, résultat de `dotnet test`, deux captures de la démo (`CASAENGINE_CAPTURE_SCREENSHOT_PATH`), sans variable et en configuration de mesure, conservées hors dépôt comme images de référence, avec les pixels mesurés et leurs valeurs.
- Validation : état initial noté dans le plan du moteur.
- Commit (moteur) : `docs(ai-agent): plan the ambient and shadow lighting fixes`

### ⏳ E1 — L'ambiant n'est multiplié qu'une fois par la couleur de surface (D7)

- Objectif : dans `LitForward.fx` (`ComputeBaseAmbientTerm`) et `skinEffect.fx` (`ComputeSkinnedAmbientContribution`), le terme ambiant ne contient plus la couleur de surface ; `ComposeLitSurfaceColor` reste seule à l'appliquer. Résultat : `texture × DiffuseColor × (N·L × lumière + ambiant du monde × ambiant du matériau) + émissif` (voir la chaîne de correspondance dans « État vérifié »).
- ADR dans `CasaEngine/docs/decisions/` (numéro suivant, index mis à jour) : la correction, son impact visible sur les scènes existantes, la note de migration.
- Tests : `LightingShaderCoverageTests` mis à jour, avec une assertion sur le source des deux shaders qui échoue si le terme ambiant reprend la couleur de surface.
- Validation : builds des deux solutions, tests ; démo sans variable : image identique à celle de E0 (ses matériaux n'ont pas de texture, la couleur de surface y vaut 1) ; démo en configuration de mesure : sur le receveur texturé, un pixel à l'ombre de la lumière principale passe de `texture² × DiffuseColor × ambiant` à `texture × DiffuseColor × ambiant`, à ±3 sur 255 (valeurs attendues calculées et notées), et un pixel en pleine lumière ne change que de la part d'ambiant.
- Commit (moteur) : `fix(rendering): apply the surface colour to the ambient term only once`

### ⏳ E2 — Source d'ambiant plate même avec un cube de réflexion (D8)

- Objectif : `WorldEnvironmentSettings.AmbientSource` (`Auto` par défaut, `Flat`), sérialisé de façon optionnelle, résolu par `EnvironmentResolver`, lié aux shaders par un uniforme distinct de `HasEnvironmentCubeTexture` (déclaré dans `LitForward.fx` et `skinEffect.fx`, registres choisis sans collision dans les deux). En `Flat`, l'ambiant vaut `AmbientColor × ambiant du matériau` et les réflexions du cube sont inchangées.
- ADR ; documentation dans `docs/engine/`.
- Tests : sérialisation et défaut de `AmbientSource` ; résolution (`Flat` avec cube lié donne la voie plate, `Auto` garde le comportement actuel) ; assertions de source dans `LightingShaderCoverageTests`.
- Démo : en configuration de mesure, `CASAENGINE_SHADOW_DEMO_REFLECTION_CUBE=1` lie un cube (généré par le moteur) et `CASAENGINE_SHADOW_DEMO_AMBIENT_SOURCE=Flat` choisit la voie plate.
- Validation : builds, tests ; démo sans variable : image identique à celle de E1 ; en configuration de mesure avec le cube lié, deux captures, `Auto` puis `Flat` : en `Flat`, le pixel à l'ombre du receveur texturé (lumière d'appoint éteinte, donc éclairé par l'ambiant seul) vaut `texture × DiffuseColor × AmbientColor × ambiant du matériau` à ±3 (valeur attendue calculée et notée) et diffère de la capture `Auto` ; un pixel de l'objet réfléchissant est identique dans les deux captures.
- Commit (moteur) : `feat(rendering): let a world use a flat ambient term while keeping a reflection cube`

## Phase 3 — Lumière (RacingGame)

### 🧪 T2 — Une lumière, celle de l'original, et l'ambiant des matériaux

> Correction après vérification indépendante (2026-11-14) : l'indice `BrightAmbient` avait été conservé à tort en T2 ; dans l'original, l'ambiant à 128 des panneaux, bannières et éoliennes est du code mort (`Model.cs:261-264` ne s'exécute que si l'effet a un paramètre `ambientColor`, or les modèles se chargent en `BasicEffect`, et `MeshRenderManager.cs:415-420` reprend de toute façon l'ambiant du fichier). L'indice est retiré du profil, du vérificateur et de la fabrique, comme le plan le prévoyait ; run `rgce-l4`, rapport `report-l4` : sondes inchangées (19 / 34), écart moyen en baisse sur 13 vues sur 15 par rapport à `rgce-l3`, `--verify-legacy-import-profile` code 0.
>
> Validation (2026-11-14), run `rgce-l3`, rapport `artifacts/capture-bench/report-l3` : build 0 erreur ; mesure de scène inchangée ; `--verify-legacy-import-profile` code 0. Une seule lumière (direction `normalize(-8500, -15000, -7250)`, blanche, intensité 1), ambiant plat à 1, cube de réflexion conservé ; matériaux générés à `D` et `A / D` du tableau, émissif à zéro ; modèles importés à `A / D` depuis leur `legacyEffect`. Écart moyen par canal en baisse sur les 15 vues par rapport à `report-t1` (rouge : 16,5 à 34,7 contre 24,5 à 50,0). **Critère 1 des sondes : 19 / 34 (10 / 34 au départ) : l'objectif « toutes les sondes éclairées et opposées au soleil à ±12 » n'est pas atteint.** Les 15 échecs, avec leur cause (mesurée par un run de diagnostic pour le terrain et le sol de ville, déduite du code et non mesurée pour la route) : 7 sondes de terrain éclairé (+12 à +19) : spéculaire de terrain ajouté par la conversion (0,05, puissance 6), absent de l'original, toutes passent quand il est mis à zéro (S3, A7) ; 5 sondes de route éclairée (+13 à +37) : la route de la conversion n'a ni normal map ni tangentes, l'original calcule `texture × (A + D × relief)` (S3, A7) ; 3 sondes de sol de ville : la conversion y projette une ombre que l'original n'a pas, elles passent sans ombres (S2b) ; 1 sonde sur le mur du casino (+77) : cause non trouvée. Les sondes de terrain opposé au soleil passent toutes (ambiant du terrain vérifié). Un écart au plan, fondé sur le code de l'original : la voiture en course prend l'ambiant 40 et le diffus 210 par défaut sur toutes ses pièces sauf le vitrage, comme `Model.RenderCar` (`Model.cs:584-585, 666-668`). Aucun cycle de correction du budget A3 consommé. Temps d'image (A3) : non mesuré, le jeu n'a pas d'outil pour cela (point ouvert O6).

- Préalable : E1 et E2 ; l'arbre de travail du sous-module est sur la branche du moteur.
- Objectif : le monde de course a une seule lumière directionnelle, de direction `-(8500, -7250, 15000)` convertie par l'axe (x, y, z) → (x, z, −y), blanche, intensité 1 ; `AmbientSource = Flat`, ambiant du monde à 1, cube de réflexion conservé ; chaque matériau porte `DiffuseColor = D` et `MaterialAmbientColor = A / D` (chaîne de correspondance de « État vérifié »), avec A et D du tableau pour les matériaux générés et du `legacyEffect` du glTF pour les modèles importés, sans le plancher `BrightAmbient` de la conversion. Pour les modèles importés, la conversion calcule `A / D` elle-même à partir des valeurs brutes lues par `LegacyGltfModelReader` (là où elle crée ses matériaux, `LegacyTrackSceneFactory.cs`, `CreateImportedRuntimeMaterial`, et `Components/LegacyCarVisualFactory.cs` pour la voiture en course), sans passer par le plafond à 1 du résolveur du moteur ; si D vaut 0, l'ambiant du matériau vaut A et le diffus 0. Si `LitDiffuseMaterial` ou sa liaison plafonne l'ambiant du matériau à 1, la tâche passe en ⚠️ : la correction est alors dans le moteur. Le spéculaire, les réflexions et les textures ne sont pas touchés (S3).
- Le carrousel de sélection de voiture construit ses voitures par `LegacyCarVisualFactory` (`UI/CarSelectionCarousel.cs:125, 192`) et son socle est un `LitDiffuseMaterial` texturé (`:365-378`) : E1 l'éclaircit et T2 change le matériau de ses voitures. Ce changement est accepté ; une capture avant / après du carrousel (`--capture-ui-screens`) est jointe en T2b. Ses lumières propres ne sont pas modifiées. Le résolveur du moteur `LegacyImportedMaterialPresentation` n'est pas modifié.
- Fichiers : `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs`, `Worlds/LegacyTrackSceneFactory*.cs` (matériaux générés et importés), `Components/LegacyCarVisualFactory.cs` (voiture en course), `Bootstrap/RacingGameLegacyMaterialImportProfile.cs` (retrait de l'indice), `Bootstrap/LegacyImportProfileVerifier.cs` (assertions mises à jour).
- Si une sonde reste hors tolérance pour une cause située dans le moteur, la tâche passe en ⚠️ avec la cause mesurée.
- Validation : build ; `--verify-legacy-import-profile` code 0 ; mesure de scène inchangée ; critères de sonde « éclairé » et « face opposée au soleil » de la validation globale ; chaque `LitDiffuseMaterial` créé dans `LegacyTrackSceneFactory*.cs` porte `D` et `A / D` du tableau, tunnel compris (relecture du diff).
- Commit : `fix(racing-casa): light the race world with the original's single light and material ambient`

### 🧪 T2b — Banc intermédiaire et constats de S2a

> Validation (2026-11-14) : banc relancé (`rgce-l4`, `report-l4`) ; constats mis à jour dans `docs/racinggame-3d-capture-bench-findings.md`. Carrousel : captures avant (`artifacts/capture-bench/ui-before`) et après (`ui-after`) : l'image de la sélection de voiture diffère au niveau des octets, sans différence visible. Vérification indépendante (A2) : lumière, comportement du moteur et banc CONFIRMED (run refait par le vérificateur, mêmes chiffres) ; valeurs de matériau REFUTED sur un point, l'ambiant des panneaux, bannières et éoliennes, corrigé (`46f2013`) puis revérifié ; attribution des sondes en échec INCONCLUSIVE pour la route (cause déduite, non mesurée), d'où la reformulation ci-dessus. **S2a n'atteint pas son critère de sondes (19 / 34) : les causes restantes sont hors de son périmètre et réparties entre S2b et S3.** Reste : relecture par l'auteur.

- Objectif : relancer le banc ; capturer le carrousel de sélection avant (au commit précédant E1 côté moteur et T2 côté jeu, images de `--capture-ui-screens` conservées hors dépôt avant de commencer E1) et après ; noter sous cette tâche le rapport de sondes et l'écart moyen par vue ; mettre à jour `docs/racinggame-3d-capture-bench-findings.md` (M5, part d'éclairage de M2) ; lister ce que les mesures apprennent pour S2b.
- Validation : validation de S2a (ci-dessus) ; relecture par l'auteur.
- Commit : `docs(racing-casa): record the light and ambient measures before the shadow work`

## Phase 4 — Ombres (CasaEngine, même branche) — S2b, à revoir avant exécution

Exigences déjà retenues pour S2b, issues de la deuxième revue :

- En `MultiplyLitColor`, le spéculaire de la lumière qui projette l'ombre et la contribution de réflexion sont multipliés par la même teinte que la couleur éclairée (l'original multiplie l'image entière, `ShadowMapBlur.cs:22-28, 225`). Aujourd'hui le spéculaire est ajouté après la composition (`LitForward.fx:493, 517, 541, 565, 614, 666`, `skinEffect.fx:381`) et la réflexion ensuite (`LitForward.fx:626, 638, 667`) : E3 doit les couvrir, avec une assertion de source et une capture de démo sans reflet du soleil dans l'ombre.
- E3, E4 et E5 prouvent chacun leur comportement non par défaut par une capture de démo pilotée par variable d'environnement, valeurs de pixel notées ; E5 en particulier montre l'ombre qui s'efface sur la bande de fondu (deux distances).

### ⏳ E3 — Mode d'application et teinte de l'ombre (D1)

- Objectif : `ShadowSettings.Mode` (`OccludeDirectLight` par défaut, `MultiplyLitColor`) et `ShadowSettings.ShadowColor` ; lus et écrits de façon optionnelle (`mode`, `shadow_color`), copiés par `CopyFrom`, remis par `ResetToDefaults` ; liés par `ForwardLightBinder` ; déclarés dans `LitForward.fx` et `skinEffect.fx` (registres sans collision) ; appliqués dans `Lighting.fxh`. En `MultiplyLitColor`, la lumière directe n'est plus occultée et la couleur éclairée (direct + ambiant, hors émissif) est multipliée par `lerp(1, ShadowColor, ombre)`.
- `StaticShadowValidationDemo` gagne de quoi activer `MultiplyLitColor` avec une teinte grise (variable d'environnement ou touche, selon le mécanisme existant de la démo).
- Un seul ADR couvre D1, D2 et D3 (réglages d'ombre) ; index mis à jour.
- Tests : chargement, sauvegarde, copie et défauts de `ShadowSettings` ; `LightingShaderCoverageTests` : les nouveaux uniformes sont présents dans les deux shaders, et l'assertion existante « l'ambiant n'est pas ombré » est restreinte au mode par défaut.
- Validation : builds, tests ; démo : image identique à celle de E2 avec les défauts ; avec `MultiplyLitColor` et une teinte grise, l'ombre capturée n'est plus noire (valeur de pixel notée).
- Commit (moteur) : `feat(rendering): add a shadow mode that multiplies the lit colour by a shadow colour`

### ⏳ E4 — Bord doux (D2)

- Objectif : `ShadowSettings.FilterRadius` (texels, 0 par défaut = filtrage actuel) ; au-delà de 0, le receveur échantillonne un disque de Poisson fixe de 12 points mis à l'échelle par le rayon, dans `Lighting.fxh`, uniforme déclaré dans les deux shaders. Aucune allocation, aucun état GPU changé. La démo permet de régler le rayon.
- Tests : sérialisation ; valeur négative ou non finie ramenée à 0 ; assertion de source.
- Validation : builds, tests ; démo : image identique à E3 avec le défaut ; avec un rayon de 3, la transition d'ombre s'étale sur plusieurs pixels (largeur mesurée sur la capture, notée).
- Commit (moteur) : `feat(rendering): add a shadow filter radius for soft shadow edges`

### ⏳ E5 — Fondu à la distance maximale (D3)

- Objectif : `ShadowSettings.FadeDistance` (0 par défaut) ; l'ombre passe linéairement à « éclairé » entre `MaxDistance − FadeDistance` et `MaxDistance` du centre de la carte d'ombres ; uniforme déclaré dans les deux shaders.
- Tests : sérialisation, bornes, assertion de source.
- Validation : builds, tests ; démo : image identique à E4 avec le défaut.
- Commit (moteur) : `feat(rendering): fade shadows out before the shadow distance`

### ⏳ E6 — Documentation

- Objectif : `docs/engine/` décrit les réglages d'ombre et la source d'ambiant (résumé, extrait d'usage, limites) ; index `docs/README.md` à jour ; plan du moteur clos.
- Commit (moteur) : `docs(engine): document the shadow and ambient lighting settings`

## Phase 5 — Ombres (RacingGame)

### ⏳ T3 — Les ombres de l'original dans le monde de course

- Préalable : E3 → E5.
- Objectif : le monde de course utilise `MultiplyLitColor`, `ShadowColor = (0.25, 0.26, 0.27)`, un rayon de filtre et un fondu réglés sur les sondes ; portée 129 m ; receveurs et projeteurs comme l'original : le terrain reçoit sans projeter ; bannières et panneaux projettent sans recevoir ; dessous de route et plan de ville ne reçoivent pas (ferme O4 de S1).
- Fichiers : `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs`, `Worlds/LegacyTrackSceneFactory*.cs`.
- Validation : build ; mesure de scène inchangée ; tous les critères de sonde de la validation globale.
- Commit : `feat(racing-casa): cast the original's soft tinted shadows in the race world`

### ⏳ T4 — Banc relancé, constats, vérification indépendante

- Objectif : relancer le banc ; mettre à jour `docs/racinggame-3d-capture-bench-findings.md` (L1, G2, M5) ; passe `verifier` sur l'affirmation de la validation globale.
- Validation : relecture par l'auteur.
- Commit : `docs(racing-casa): update the 3D findings after the lighting and shadow fixes`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | L'original centre sa carte d'ombres sur la voiture, le moteur sur la caméra. En course la caméra suit la voiture ; sur les vues du banc l'écart est d'environ 20 m. Accepté tant que les sondes passent ; sinon à traiter dans le moteur (centre de carte réglable), après accord de l'auteur. | T3 |
| O2 | Le flou d'ombre en espace écran de l'original n'est pas reproduit au pixel par un filtre dans le shader (D2). Les sondes sont posées hors des bords d'ombre ; l'écart résiduel sur les bords est noté en T4. | E4, T4 |
| O3 | Tant que le pointeur du sous-module n'est pas avancé par l'auteur, les commits T2, T3 et T4 de RacingGame ne compilent qu'avec la branche du moteur dans l'arbre de travail. | T2, T3 |
| O4 | D7 éclaircit l'ambiant de toutes les scènes du moteur (démos, jeux d'exemple, éditeur). Leur réajustement éventuel n'est pas dans ce plan : il est listé dans l'ADR comme suite. | E1 |
| O5 | Les sondes sur des modèles importés (murs d'immeuble) dépendent aussi du diffus et de la texture de ces modèles. Si une sonde échoue pour une cause de matériau relevant de S3 (texture, spéculaire, réflexion), elle est notée et reportée à S3 avec sa mesure, sans élargir S2. | T2 |
| O6 | A3 demandait de borner le coût de rendu de E1 et E2 par une mesure du temps d'image : le jeu tourne à pas fixe et n'a aucun outil de mesure. Non mesuré. Le changement de shader est un test de plus sur un uniforme. À décider : construire une mesure, ou accepter. | T2 |
| O7 | Le spéculaire du terrain (0,05) est une valeur de la conversion sans équivalent dans l'original ; le mettre à zéro fait passer 7 sondes. Laissé en l'état parce que le spéculaire est hors périmètre de S2a ; premier point de S3. | T2, S3 |
| O8 | Mur du casino dans `Beginner-overview` : éclairé dans la conversion, noir uni dans l'original, cause non trouvée (ni les ombres, ni le spéculaire, ni les valeurs de matériau). | T2 |
| O9 | `--capture-ui-screens` expire à l'étape « fin de course » (code 1) ; constaté avant E1 et T2. Antériorité à S1 non vérifiée. | T2b |

## Hors périmètre

- S3 (matériaux : détail du terrain, normal maps et spéculaire de route, réflexions, peinture, ciel) et S4 (glow, lens flare).
- L'éclairage des menus et du carrousel de sélection.
- Le réajustement des scènes existantes du moteur après D7.
- Push, merge, pointeur du sous-module.


## Suites données aux revues

| Revue | Point | Suite |
|---|---|---|
| 3, R1 | Normal maps décodées avec les mauvais canaux | Corrigé hors de ce plan (ADR-0016) : textures converties, tangentes des modèles importés écrites et lues ; mesuré contre les modèles de l'original. |
| 3, R2 | Faces opposées au soleil ombrées dans l'original | T1 : classe « face opposée au soleil » restreinte aux points non ombrés par l'original ; quatrième classe pour S2b. |
| 3, R3 | Démo d'ombres sans texture ni réflexion, avec lumière d'appoint | E1 et E2 : voir « Démo du moteur pour les captures ». |
| 3, R4 | Carrousel de sélection modifié par E1 et T2 | T2 : écrit et accepté, capture avant / après en T2b. |
| 3, R5 | Détail du terrain dans la formule de l'original | T1 : sondes de terrain corrigées par la moyenne mesurée de `2 × détail`. |
| 4 | Configuration de mesure de la démo trop lâche | Amendement A1. |
| 4 | Référence de départ antérieure aux normal maps et aux tangentes | Référence remplacée par `rgce-t1` / `report-t1` dans tout le plan. |
| 4 | Pas de vérification indépendante de S2a | Amendement A2. |
| 4 | Pas de budget pour S2a | Amendement A3. |
| 4 | Notes non bloquantes | Amendements A4 à A8. |

## Amendements de la révision 5 (ils prévalent sur le texte au-dessus)

- **A1 — Configuration de mesure de la démo (`CASAENGINE_SHADOW_DEMO_VALIDATION=1`).** Ambiant du monde `(0.5, 0.5, 0.5)`, intensité 1 ; lumière d'appoint éteinte ; receveur texturé : texture unie de valeur 128 / 255, `DiffuseColor` blanc, ambiant du matériau 1, spéculaire 0 ; objet réfléchissant : ambiant du matériau 0 ; cube lié sous `CASAENGINE_SHADOW_DEMO_REFLECTION_CUBE=1` : cube uni de couleur `(0.2, 0.6, 1.0)` créé par la démo. Valeurs attendues du pixel du receveur à l'ombre de la lumière principale (ambiant seul), canal par canal sur 255 : E0 (avant correction) `128/255 × 128/255 × 0.5 × 255 ≈ 32` ; E1 `128/255 × 0.5 × 255 = 64` ; E2 `Flat` avec cube : 64 ; E2 `Auto` avec cube : `64 × (0.2, 0.6, 1.0) × EnvironmentAmbientColor / 0.5`, valeur calculée et notée à la capture, qui doit différer de 64 de plus de 12 sur au moins un canal. Le pixel de l'objet réfléchissant, dont l'ambiant est nul, est identique en `Auto` et en `Flat`. Tolérance ±3. Si la sortie du moteur applique une courbe (gamma) qui déplace ces valeurs, elle est établie en E0 sur la capture de référence et les valeurs attendues sont recalculées avec elle, notées dans le plan du moteur.
- **A2 — Vérification indépendante de S2a.** À la fin de T2b, une passe `verifier` neuve sur l'affirmation exacte : « à `HEAD` des deux dépôts, le monde de course a une seule lumière de la direction de l'original ; les critères de sonde "éclairé" et "face opposée au soleil" de `probes.json` passent ; E1 et E2 se comportent comme A1 le prédit ; les défauts du moteur hors D7 sont inchangés ; mesure de scène et vérificateur du profil d'import inchangés ». S2a n'est déclarée faite qu'après un verdict CONFIRMED.
- **A3 — Budget de S2a.** En T2, au plus trois cycles « mesure des sondes → correction » ; au-delà, ⚠️ avec les mesures, et arrêt. Coût de rendu : le temps d'image du monde de course, mesuré sans limite d'images sur la vue de départ de Beginner avant E1 et après E2 (trois mesures de chaque, même procédé que celui cité dans `RaceWorldFactory.cs:41-42` s'il est reproductible, sinon le compteur d'images du jeu), ne monte pas de plus de 0,1 ms ; sinon ⚠️.
- **A4 — Règle D = 0 (remplace celle de T2).** `MaterialAmbientColor = A / D` se calcule canal par canal ; un canal où D vaut 0 reçoit 0 (le moteur multiplie l'ambiant par `DiffuseColor`, un canal sans diffus y est noir quoi qu'on fasse). Si un matériau de l'original a D = 0 et A > 0 sur un canal (ambiant sans diffus), l'identité n'est pas atteignable par les valeurs de matériau : le cas est listé avec le modèle concerné et la tâche passe en ⚠️.
- **A5 — Émissif.** Les matériaux générés par la conversion portent un émissif que l'original n'a pas (`LegacyTrackSceneFactory.cs:965, 981, 1138`, `RoadStructures.cs:317, 336, 355`, `GuardRails.cs:412, 430`, `Terrain.cs:147`). T2 le met à zéro, pour la même raison que le retrait du plancher d'ambiant. `CreateGroundMaterial` (non appelé) est supprimé s'il est toujours sans appelant ; `ApplyFallbackMaterials` (modèle sans matériau) garde ses valeurs, notées comme hors de l'identité.
- **A6 — D5 est une exception à `CasaEngine/AGENTS.md` §4** (« branche créée depuis `main` ») : la branche part de `chantier/gltf-static-tangents`, dont la conversion dépend. À arbitrer par l'auteur avec D5. Le commit de E0 est typé `chore` (il contient du code de démo).
- **A7 — O5 est élargi à toutes les sondes.** Une sonde qui échoue pour une cause de matériau relevant de S3 (spéculaire de la route : `SpecularRoad20`, spéculaire blanc de puissance 24 dans l'original, 0,14 sans normal map dans la conversion ; réflexion ; détail local du terrain) est notée avec sa mesure et reportée à S3, sans élargir S2. Les sondes « éclairé » de route sont posées hors du reflet du soleil de l'original. La correction de détail du terrain s'applique à la seule couleur de l'original, y compris dans le contrôle « l'original contre lui-même », où elle est désactivée.
- **A8 — Divers.** Les tests de E2 couvrent `Clone`, `CopyFrom`, `ResetToDefaults`, `Load` et `Save` de `AmbientSource`. Retour arrière : annuler T2 avant E2 puis E1 (les valeurs de matériau de T2 supposent E1 et E2).
