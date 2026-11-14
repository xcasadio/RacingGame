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

### 🧪 T3 — Les ombres de l'original dans le monde de course

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

## S2b — révision avant exécution (2026-11-14)

L'auteur a demandé le 2026-11-14, avant de s'absenter, d'exécuter les tâches qui suivent le plan des matériaux (« fait toutes les autres tâches qui suivent ce plan »). S2b est donc exécutée après la tranche S3 (`ai-agent/tasks/rgce-3d-materials-base-tasks.md`), avec les décisions D1, D2 et D3 telles que proposées, après une revue indépendante de la présente section. Les amendements B ci-dessous prévalent sur le texte de E3 → E6, T3 et T4 plus haut.

État depuis l'écriture de S2b : l'ambiant est corrigé (E1, E2), la lumière est celle de l'original (T2), la démo d'ombres a sa configuration de mesure (`CASAENGINE_SHADOW_DEMO_VALIDATION=1`, pixels `ombre` (310,420), `eclaire` (240,435), `reflet` (520,490) notés dans le plan du moteur), et la tranche S3 ajoute au moteur la texture de détail et les mipmaps. Branche du moteur : `chantier/racinggame-lighting`.

Faits vérifiés pour T3, dans l'original :

- Seuls les objets de la liste `nearTrackObjects` projettent et reçoivent des ombres (`RacingGameOriginal/Core/Landscapes/Landscape.cs:1263-1304`). Un objet y entre si l'appel `AddObjectToRender(nom, matrice, isNearTrackForShadowGeneration)` le demande, ou si son nom est `windmill` ou contient `hotel`, `building` ou `casino01` (`Landscape.cs:388-393`). Les objets neutres du fichier de circuit passent `false` (`Core/Tracks/TrackLine.cs:675`) ; les autres appels sont dans `Core/Tracks/Track.cs` (`:691, 784, 810, 852, 866, 936, 986, 1005, 1024, 1056, 1155`), `TrackColumns.cs:240`, `GuardRail.cs:289` (supports de rail : `false`) et `TrackCombiModels.cs:143`.
- Parmi eux, ceux dont le nom contient `banner` ou `sign` projettent sans recevoir (`Landscape.cs:60-63` de la classe `LandscapeObject`, `:1295-1299`).
- La piste projette (`Track.GenerateShadow`, `Core/Tracks/Track.cs:1294-1320`, sans le dessous de route) et reçoit (`Track.UseShadow`, `:1321-1340`) ; le terrain reçoit sans projeter ; le plan de ville ne reçoit pas ; la voiture projette et reçoit quand elle est dessinée.

Amendements :

- **B1 — Ordre et référence.** S2b commence après G6 de S3. Sa référence de départ est la dernière capture de S3 (nommée dans la note de G6). Ordre : E3 → E4 → E5 → E6 → T3a → T3 → T4.
- **B2 — E3, spéculaire et réflexion.** En `MultiplyLitColor`, la teinte multiplie la couleur finale du pixel hors émissif : couleur éclairée, spéculaire et contribution de réflexion (l'original multiplie l'image entière, `Core/Shaders/ShadowMapBlur.cs:22-28, 225`). La lumière directe et son spéculaire ne sont alors pas occultés avant ce produit. Assertions de source sur `LitForward.fx` et `skinEffect.fx`.
- **B3 — E3, mesure sur la démo.** Variables `CASAENGINE_SHADOW_DEMO_MODE=MultiplyLitColor` et `CASAENGINE_SHADOW_DEMO_COLOR=0.25,0.26,0.27`, lues en configuration de mesure. Attendu : le pixel `ombre` vaut la couleur que ce point aurait en pleine lumière (celle du pixel `eclaire` du même receveur uni, relevée dans la même capture) multipliée par (0,25 ; 0,26 ; 0,27), à ±3 ; le pixel `eclaire` est inchangé ; sans variable de mode, captures identiques au bit près à celles prises avant la tâche dans la même session.
- **B4 — E4 et E5, mesure sur la démo.** `CASAENGINE_SHADOW_DEMO_FILTER_RADIUS` : avec un rayon de 3, la transition entre `ombre` et `eclaire` le long d'une ligne de pixels traversant le bord d'ombre s'étale sur au moins 3 fois plus de pixels qu'avec le rayon 0 (largeurs notées). `CASAENGINE_SHADOW_DEMO_FADE_DISTANCE` (avec une distance maximale réduite par `CASAENGINE_SHADOW_DEMO_MAX_DISTANCE` pour que la bande de fondu soit dans l'image) : deux pixels à l'ombre, l'un près de la caméra, l'autre dans la bande de fondu : le second est plus clair, entre la valeur d'ombre et la valeur éclairée (valeurs notées), et identique au premier quand le fondu vaut 0.
- **B5 — T3a, sondes et mesure des projeteurs (nouvelle tâche, RacingGame).** `scene.json` de l'original gagne, par objet, `castsShadow` (appartenance à `nearTrackObjects`) et `receivesShadow` (appartenance, et nom sans `banner` ni `sign`) ; la mesure de scène de la conversion compare ces deux drapeaux à ceux de ses entités, objet par objet (les objets sont déjà appariés un à un). `probes.json` gagne la classe `away-shadowed` jugée par un quatrième critère (candidat à ±12 de l'original), avec au moins 3 sondes sur des faces de rail ou d'immeuble proche opposées au soleil. Validation : les images de l'original restent identiques au bit près à la référence ; la mesure affiche l'écart de drapeaux avant T3. Commit : `feat(capture): export shadow casters and receivers and probe shadowed faces`.
- **B6 — T3, périmètre.** Réglages du monde de course : `MultiplyLitColor`, `ShadowColor = (0.25, 0.26, 0.27)`, portée 129, rayon de filtre et fondu réglés sur les sondes (au plus trois cycles mesure → correction). Projeteurs et receveurs alignés sur l'original, objet par objet, par les drapeaux de composant existants (`CastShadows`, `ReceiveShadows`) : mesure de T3a à zéro écart. Route : projette et reçoit ; dessous de route : ne projette ni ne reçoit ; tunnels : projettent et reçoivent ; terrain : reçoit seulement ; plan de ville : ne reçoit pas. Validation : mesure de scène inchangée pour les positions ; critères de sonde 2 (rapport ombre / lumière à ±0,05), 3 (accord des classes) et 4 (faces opposées et ombrées à ±12) ; les 3 sondes `city-lit` passent le critère 1.
- **B7 — Limites connues, notées en T4 sans bloquer.** L'original centre sa carte d'ombres sur la voiture et la projette en perspective depuis une lumière virtuelle ; le moteur la centre sur la caméra, en projection orthographique. L'original floute l'ombre en espace écran ; le moteur filtre dans le shader. Si une sonde échoue pour l'une de ces causes après trois cycles, elle est consignée avec sa mesure et la tâche passe en 🧪, sans modifier le moteur au-delà de E3 → E5.
- **B8 — T4.** Vérification indépendante sur l'affirmation exacte : réglages et drapeaux de T3, comportement du moteur par défaut inchangé (captures de démo identiques), critères de sonde atteints ou consignés selon B7.

## S2b — révision 2 après revue indépendante (2026-11-14)

Première revue de S2b : REVISE (un point de priorité 1, deux de priorité 2, dix notes), tous retenus. Les amendements C ci-dessous prévalent sur les amendements B et sur le texte antérieur.

Faits corrigés ou ajoutés, dans l'original :

- **La réception se décide à l'écran, pas par objet.** Les receveurs sont dessinés seuls dans un masque avant la scène (`Core/GameScreens/TrackAuditCaptureScreen.cs:154-172`, `Core/Landscapes/Landscape.cs:1280-1305`), puis l'image entière est multipliée par ce masque (`Core/Shaders/ShadowMapBlur.cs:119-137, 225`). Un pixel visible prend donc l'ombre du receveur le plus proche sur son rayon, que l'objet affiché soit receveur ou non. Conséquence : le plan de ville, posé à z = 0,1 au-dessus du terrain (`Landscape.cs:902-911`), affiche l'ombre que reçoit le terrain ; les sondes `city-shadow-1..3` de l'original valent environ 30 contre 111 pour `city-lit` (`artifacts/capture-bench/report-g2/probes.md:53-60`).
- **Receveurs** : le terrain ; la piste : route, rails de sécurité (`Core/Tracks/Track.cs:1321-1340`, rails `:1333-1334`) et tunnels ; les objets de `nearTrackObjects` dont le nom ne contient ni `banner` ni `sign` et dont le modèle n'est pas « alpha » (`Core/Graphics/Model.cs:201, 862-867` : les modèles dont le nom commence par `alpha`, palmiers compris, ne reçoivent jamais) ; la voiture quand elle est dessinée. Ne reçoivent pas : dessous de route, colonnes générées (appels commentés, `Track.cs:1336-1338`), plan de ville (mais il affiche l'ombre du terrain, ci-dessus).
- **Projeteurs** : les objets de `nearTrackObjects` ; la piste : route, rails (`Track.cs:1310-1311`) et tunnels, sans le dessous de route ni les colonnes générées (`:1313-1315`, commentés) ; la voiture. Le terrain et le plan de ville ne projettent pas. Appartenance à `nearTrackObjects` : drapeau de l'appel, forcé à vrai pour `windmill` et les noms contenant `hotel`, `building` ou `casino01` (`Landscape.cs:388-393`), forcé à faux pour les enfants de modèles combinés (`Core/Tracks/TrackCombiModels.cs:143-144`) et pour les deux autres surcharges (`Landscape.cs:525-528, 536-539`).
- **Étendue de la carte d'ombres.** `Matrix.CreatePerspective(0,645 ; 1,0 ; 1 ; 218,75)` (`Core/Shaders/ShadowMapShader.cs:212-215, 238-261`) : la lumière virtuelle est à 129,25 m d'un point situé devant la voiture, et la carte couvre là environ 83 × 129 m (±42 × ±65 m), pas 129 m autour de la voiture comme écrit plus haut. Le moteur couvre un carré de demi-côté `MaxDistance` centré sur la caméra, et la même valeur fixe la profondeur (lumière à `MaxDistance` en amont, plan lointain à 2 × `MaxDistance`, `CasaEngine/CasaEngine/Framework/Rendering/Draw/ShadowPass.cs:239-252`) : réduire l'étendue latérale rogne aussi la profondeur et coupe les grands projeteurs.
- **Autres différences de l'original, à consigner en T4 sans les reproduire** : tous les projeteurs sont dessinés double face et sans test alpha (`Core/Content/Shaders/ShadowMap.fx:122-130`), donc les palmiers projettent des ombres pleines en forme de carte ; projeteurs et receveurs sont éliminés par distance à chaque image (`Model.cs:807-814, 874-881`) ; les receveurs dépendent du haut détail (`Landscape.cs:1293`, actif en capture).

Amendements :

- **C1 — Receveurs dans la conversion (remplace la liste de B6).** Approximation par objet du masque d'écran, choisie pour que chaque pixel affiche ce que l'original affiche là où les deux règles coïncident : le plan de ville **reçoit** (il affiche l'ombre du terrain qu'il couvre) ; terrain, route, rails, tunnels : reçoivent ; objets de `nearTrackObjects` hors bannières, panneaux et modèles alpha : reçoivent ; dessous de route et colonnes générées : ne reçoivent pas ; voiture : reçoit. Écart résiduel assumé, listé en T4 : dans l'original, un objet non receveur devant un receveur ombré est assombri par le masque (bannières, panneaux, palmiers, colonnes devant le terrain) ; dans la conversion il ne l'est pas.
- **C2 — Projeteurs dans la conversion.** Objets : selon le drapeau exporté (C3) ; route, rails, tunnels : projettent ; dessous de route, colonnes générées (`Track.Columns.*`), terrain, plan de ville : ne projettent pas ; voiture : projette. Après T3, `Track.Columns.*` a `CastShadows = false` et `ReceiveShadows = false`.
- **C3 — T3a, export et mesure (remplace B5 pour ces points).** `castsShadow` = appartenance à `nearTrackObjects` ; `receivesShadow` = appartenance, nom sans `banner` ni `sign`, et modèle non alpha, **lus dans l'état de l'original** (liste et `Model` chargé), pas redéduits des noms. Fichiers : `RacingGameOriginal/Core/Landscapes/Landscape.cs` (accès en lecture à `nearTrackObjects`), `Core/Graphics/Model.cs` (accès en lecture au drapeau alpha si nécessaire), `Core/GameScreens/TrackAuditCaptureScreen.cs` ; conversion : `Worlds/LegacyTrackSceneFactory.Diagnostics.cs` (drapeaux dans `LegacyPlacedObject`), `Worlds/LegacyTrackSceneFactory.Scenery.cs` (le portage d'`AddObjectToRender` reçoit le paramètre `isNearTrack` à chaque site d'appel, avec les trois règles de forçage), `Bootstrap/ReferenceSceneMeasure.cs` (compare les drapeaux ; accepte un `scene.json` sans eux) ; `scripts/TrackCaptureCompare/Program.cs`, `Probes.cs`, `probes.json`, `README.md` (classe `away-shadowed`, critère 4). Les sondes `away-shadowed` sont posées sur des faces d'immeuble proche (une face de rail est plus étroite que le carré de 9 × 9).
- **C4 — Étendue et profondeur de la carte d'ombres (nouvelle tâche moteur E5b, après E5).** `ShadowSettings.DepthRange` (flottant, 0 par défaut = comportement actuel, la profondeur suit `MaxDistance`) : quand il est positif, la lumière est placée à `DepthRange` en amont et le plan lointain à 2 × `DepthRange`, l'étendue latérale restant `MaxDistance`. Champ sérialisé optionnel (`depth_range`), copié, remis à zéro, lié comme les autres ; mêmes exigences que E3 → E5 (ADR commun aux réglages d'ombre, tests de sérialisation et de bornes, test de la matrice de `ShadowPass` si la fonction est isolable, démo). Validation : démo identique au bit près avec le défaut ; avec une étendue réduite et une profondeur conservée, l'ombre de la colonne de la démo reste entière là où elle est coupée sans `DepthRange` (deux captures, pixels notés). Commit : `feat(rendering): separate the shadow depth range from its lateral extent`.
- **C5 — T3, étendue.** `MaxDistance` et `FadeDistance` sont choisis dans les trois cycles de mesure pour approcher l'empreinte de l'original (±42 × ±65 m autour d'un point devant la voiture ; le moteur est carré et centré sur la caméra : valeur de départ 65, fondu de départ 15), avec `DepthRange` assez grand pour ne couper aucun projeteur (valeur de départ 150). La valeur « portée 129 » de B6 est retirée. Les sondes `city-lit-1..3` doivent passer le critère 1 par l'effet de l'étendue et des projeteurs, et `city-shadow-1..3` les critères 2 et 3, le plan de ville recevant.
- **C6 — E3, précisions d'implémentation.** Dans les trois pixel shaders à réflexion de `LitForward.fx`, la teinte s'applique après `ApplyReflectionContribution`, l'émissif étant porté à part et jamais teinté ; l'alpha n'est pas touché. Le facteur d'ombre est calculé une fois, hors de la boucle des lumières. Les techniques à éclairage par sommet n'ont d'ombre dans aucun mode (`Lighting.fxh:347-371`) : écrit dans l'ADR. Avec plusieurs lumières, `MultiplyLitColor` teinte les contributions de toutes par le facteur de la seule lumière ombrée : écrit dans l'ADR et la documentation. `ReceiveShadows = false` ou aucune lumière ombrée donnent une teinte de 1.
- **C7 — E4, limite de shader.** La validation de E4 exige que le contenu DesktopGL compile pour toutes les techniques de `LitForward.fx` (ps_3_0, `Macros.fxh:11`), avec la texture de détail de S3 ; si la limite d'instructions est atteinte, le nombre de points du disque est réduit (8, puis 5) et le choix est noté ; en dessous de 5, la tâche passe en ⚠️.
- **C8 — E5, précisions.** Distance mesurée dans le plan de la carte d'ombres, en carré (même forme que la carte) ; `FadeDistance` borné à `MaxDistance` ; champs sérialisés `filter_radius` (E4) et `fade_distance` (E5). Mesure de démo : les deux pixels sont sur le même sol, dans l'ombre de la colonne de gauche ; `CASAENGINE_SHADOW_DEMO_MAX_DISTANCE` et `..._FADE_DISTANCE` sont fixés pour que l'un soit hors de la bande et l'autre dedans, la fraction de fondu attendue étant calculée et notée avant la capture.
- **C9 — Éditeur.** Le panneau `CasaEngine.Editor/Controls/EntityDetailsPanel.cs:763-836` affiche chaque champ de `ShadowSettings` : E3, E4, E5 et E5b y ajoutent le leur, comme E2 l'a fait pour `AmbientSource`.
- **C10 — Plan du moteur.** Avant E3, `CasaEngine/ai-agent/tasks/lighting-ambient-shadow-tasks.md` est synchronisé (E3 → E6 avec B2 → B4 et C4, C6 → C9, tâche E5b) et sa mention « ne pas exécuter E3–E6 » est remplacée par la mention de la demande de l'auteur du 2026-11-14.
- **C11 — Ordre.** E3 → E4 → E5 → E5b → E6 → T3a → T3 → T4.

## S2b — révision 3 après deuxième revue indépendante (2026-11-14)

Deuxième revue de S2b : REVISE (deux points de priorité 2, dix notes), tous retenus. Les faits de la révision 2 sur les projeteurs, les receveurs et le plan de ville n'ont pas été remis en cause. Les amendements D ci-dessous prévalent sur C, B et le texte antérieur. Après cette révision, S2b passe une seule revue de clôture ; un nouveau refus met la tranche en pause jusqu'au retour de l'auteur.

Faits corrigés :

- **Centres des deux cartes d'ombres sur les vues du banc.** L'écran de capture dérive de `GameScreen` (`RacingGameOriginal/Core/GameScreens/TrackAuditCaptureScreen.cs:43`), donc l'original centre sa carte à 21,5 m devant le point de route (`Core/Shaders/ShadowMapShader.cs:250-259` : position de la voiture + direction × 129,25 / 6 ; `Core/RacingGameManager.cs:140-146`). La caméra d'une vue de secteur est à 18 m derrière, 10 m au-dessus et 5 m à droite de ce point (`TrackAuditCaptureScreen.cs:336-339`) : les deux centres sont à environ 40 m l'un de l'autre, et non 20 m comme l'écrit O1. Les deux empreintes sont alignées sur la lumière, pas sur la voiture.
- **Sondes et étendue.** Les sondes `city-lit-1..3` et `city-shadow-1..3` sont à 12 à 25 m de la caméra : dans les deux cartes pour toute étendue supérieure à 30 m. Leur verdict ne dépend que des projeteurs. Les seules sondes sensibles à l'étendue sont `road-shadow-1..3` de `Beginner-sector-50`, proches de l'horizon (50 à 100 m de la caméra).
- **Appartenance à `nearTrackObjects`** : le forçage à vrai par le nom (`Landscape.cs:388-393`) s'applique dans la méthode à trois arguments que tous les chemins appellent, donc aussi aux enfants de modèles combinés et aux autres surcharges, après leur drapeau faux. La règle de la conversion applique le forçage par le nom après le drapeau.

Amendements :

- **D1 — Objectif d'étendue, resserré.** S2b ne prétend plus que l'étendue des ombres de la conversion égale celle de l'original. Elle prétend que la carte de la conversion **couvre** l'empreinte de l'original sur les vues du banc. Écart résiduel assumé, noté en T4 : la conversion ombre plus loin sur les côtés et derrière la caméra, et son centre suit la caméra, pas la voiture.
- **D2 — T3, valeurs de départ (remplace celles de C5).** `MaxDistance` 100 (40 m d'écart entre les centres + 65 m de demi-longueur de l'empreinte de l'original, arrondi), `FadeDistance` 15, `DepthRange` 150 (le biais de profondeur en mètres vaut `DepthBias × (2 × DepthRange − 0,1)` : 150 conserve le biais actuel du monde de course, 0,3 m, `RaceWorldFactory.cs:55-56`). Contrôle de l'étendue : les sondes `road-shadow-1..3` de `Beginner-sector-50` passent les critères 2 et 3 ; leur distance à la caméra dans le plan de la lumière est calculée et notée, et doit être inférieure à `MaxDistance − FadeDistance`. Les sondes `city-lit-1..3` (critère 1) et `city-shadow-1..3` (critères 2 et 3) sont attendues par le seul effet des projeteurs (C1, C2).
- **D3 — E5b, preuve (remplace la validation de C4).** `DepthRange` ne nourrit que la matrice de `ShadowPass` : aucun uniforme de shader. Test unitaire obligatoire sur `ShadowPass.BuildDirectionalShadowViewProjection` (fonction `internal static`, déjà appelée par `CasaEngine.Tests/Rendering/ShadowPassTexelSnappingTests.cs:30-50`, qui doit continuer à compiler : surcharge, ou tests mis à jour) avec trois assertions : avec `DepthRange` 0, la matrice est identique au bit près à l'actuelle ; avec `DepthRange` > `MaxDistance`, un point situé en amont de la caméra d'une distance d, `MaxDistance` < d < `DepthRange`, a une profondeur dans ]0, 1[, alors qu'il était coupé ; les coordonnées latérales de tout point et l'alignement sur les texels sont inchangés. Démo : variable `CASAENGINE_SHADOW_DEMO_DEPTH_RANGE` ; affirmation réduite à « capture identique au bit près sans variable ; ombre de la colonne de gauche présente avec `DepthRange` réglé » (dans la scène de la démo, aucun réglage ne coupe l'ombre par la profondeur pendant qu'elle est dans la carte ; l'écart entre les deux captures ne peut venir que du biais). L'ADR et la documentation donnent la relation entre le biais et `DepthRange`.
- **D4 — E4, mesure.** La largeur de transition est mesurée entre 10 % et 90 % de l'écart ombre / lumière le long de la ligne de pixels, définie avant la capture ; critère : largeur avec rayon 3 au moins deux fois celle du rayon 0, valeurs notées.
- **D5 — E5, réglages de démo.** L'ombre de la colonne de gauche est à environ 26 m de l'axe dans le plan de la lumière : `CASAENGINE_SHADOW_DEMO_MAX_DISTANCE` autour de 28 et un fondu de quelques mètres placent la limite de bande dans cette ombre ; valeurs exactes et fraction attendue calculées et notées avant la capture.
- **D6 — T4, écarts à consigner.** Outre ceux de B7 et de la révision 2 : les projeteurs de file transparente sont ignorés par la passe d'ombres du moteur (`ShadowPass.cs:92`) ; sur les vues d'ensemble, la carte de la conversion est centrée sous une caméra à plus de 110 m de haut, donc ses ombres tombent autour du centre du circuit quand celles de l'original sont près du départ, et le sol y est proche du plan lointain : les sondes de `Beginner-overview` peuvent changer, leur variation est notée sans critère.
- **D7 — Retour arrière.** Annuler T3 avant E5b, E5, E4, E3 (ses réglages en dépendent). T3a est indépendant.
- **D8 — Autorité.** Les mentions antérieures selon lesquelles S2b repasse devant l'auteur avant exécution sont remplacées par sa demande du 2026-11-14 (exécuter les tâches qui suivent le plan des matériaux), sous réserve d'un verdict READY de la revue de clôture.

## S2b — révision 4 après la revue de clôture (2026-11-14)

Revue de clôture : REVISE, un seul point de priorité 2 (l'affirmation d'étendue de D1 et sa dérivation en D2), sept notes ; le reste du contrat a été trouvé cohérent (faits de la révision 3, test de matrice de D3, réglage de démo de D5, attente de la démo de E3, registres disponibles, emplacement du panneau d'éditeur). **Aucune revue n'a rendu READY.** Le point est corrigé ci-dessous en resserrant l'affirmation, comme la revue le propose. L'auteur étant absent et ayant demandé d'exécuter les tâches suivantes sans l'attendre, S2b est exécutée sur cette base, sans nouvelle revue de plan ; la vérification indépendante de fin de tranche (T4) reste obligatoire, et ce choix est signalé dans le rapport de fin. Les amendements F ci-dessous prévalent sur D, C, B et le texte antérieur.

- **F1 — Affirmation d'étendue (remplace D1 et le contrôle de D2).** S2b ne prétend ni égaler ni couvrir l'empreinte de la carte d'ombres de l'original. Elle prétend ceci, et rien de plus : sur les vues du banc, chaque sonde que l'original ombre (classe `shadow`, dont `road-shadow-1..3` de `Beginner-sector-50`, et classe `away-shadowed`) se trouve dans la zone de pleine force de la carte de la conversion, c'est-à-dire que sa distance au centre de la carte, mesurée en carré dans le plan de la lumière, est inférieure à `MaxDistance − FadeDistance`. T3 calcule cette distance pour chaque sonde et la note dans un tableau. L'empreinte réelle de l'original (pyramide en perspective, lumière à environ 53° d'élévation, texture de fondu `ShadowDistanceFadeoutMap`, `Core/Content/Shaders/ShadowMap.fx:224-232`) n'est pas mesurée : écart consigné en T4.
- **F2 — Valeurs de départ de T3.** `MaxDistance` 100, `FadeDistance` 15, `DepthRange` 150 sont des valeurs de départ, pas une preuve de couverture ; T3 les ajuste dans ses trois cycles pour satisfaire F1 et les critères de sonde 2, 3 et 4.
- **F3 — Rayon de filtre.** Les sondes sont placées hors des bords d'ombre et ne renseignent pas le rayon : il est fixé à 2 texels, valeur réglée sur la mesure de transition de la démo (D4), puis jugé sur les planches (bord visiblement dégradé sans halo ni bande) ; une seule retouche est permise, notée.
- **F4 — Écart moyen.** La règle « l'écart moyen par canal ne monte pas » s'applique en fin de S2b contre la référence de fin de S3 (B1), sauf pour les trois vues d'ensemble, dont la variation est notée sans critère (D6).
- **F5 — T3a, drapeaux lus sur les entités.** Les drapeaux enregistrés pour la mesure sont lus sur le composant créé (`CastShadows`, `ReceiveShadows`), pas recalculés depuis la règle de portage : un écart nul prouve l'état des entités.
- **F6 — Biais de profondeur.** Sous DesktopGL la profondeur est ramenée de [−1, 1] à [0, 1] (`ShadowDepth.fx:33`, `Lighting.fxh:165-167`) : le biais en mètres vaut `2 × DepthBias × (2 × DepthRange − 0,1)`, soit environ 0,6 m aujourd'hui. La relation, avec ce facteur de plateforme, est écrite dans l'ADR et la documentation de E5b. `DepthRange` 150 conserve le biais actuel.
- **F7 — E3, précision.** Les pixel shaders à réflexion réutilisent les pixel shaders de base, et `ApplyReflectionContribution` multiplie aujourd'hui l'émissif inclus dans la couleur de surface (`LitForward.fx:214-218, 634-655`). Pour garder la sortie par défaut identique au bit près et ne pas teinter deux fois, seule la part non émissive de cette forme linéaire est teintée.
- **F8 — E5b, test.** Les coordonnées latérales et l'alignement sur les texels sont vérifiés avec une tolérance, pas au bit près (l'arrondi flottant de la position de l'œil peut changer leurs derniers bits).
- **F9 — Plan du moteur.** La synchronisation de C10 porte aussi D3, D4, D5, D7 et F3, F6, F7, F8. La limite de B7 se lit « sans modifier le moteur au-delà de E3 → E5b ».

### T3a — résultat

> Validation (2026-11-14). Original : `scene.json` exporte par objet `castsShadow` (appartenance à `Landscape.nearTrackObjects`, lue par `Landscape.IsNearTrackObject`) et `receivesShadow` (appartenance, `LandscapeObject.isBanner` faux et `Model.hasAlpha` faux, lus sur l'état de l'objet). Nouveau run `artifacts/capture-bench/original-s2b` (`--seed 1`) : les 30 images sont identiques octet pour octet à celles de `original-s1a`, `views.json` identique, `scene.json` identique au précédent hors les deux champs. Projeteurs / receveurs de l'original : Beginner 132 / 88 sur 1147 objets, Advanced 223 / 102 sur 1693, Expert 515 / 278 sur 3404. `original-s2b` remplace `original-s1a` comme référence pour la suite (mêmes images).
>
> Conversion : `LegacyPlacedObject` garde le composant créé et lit `CastShadows` / `ReceiveShadows` à la fin de la construction de la piste ; `ReferenceSceneMeasure` compare les drapeaux objet par objet (0 objet sans correspondance sur les trois pistes, positions des points de route inchangées : 0,000) et accepte un `scene.json` sans drapeaux. **Écart de départ avant T3** (run `rgce-t3a-before`) : tous les objets de la conversion projettent et reçoivent. Beginner : 1015 écarts de projection et 1059 de réception sur 1147 ; Advanced : 1470 et 1591 sur 1693 ; Expert : 2889 et 3126 sur 3404.
>
> Sondes : classe `away-shadowed` jugée par un quatrième critère (candidat à ±12 de l'original par canal), `Probes.cs` et `README.md` de `TrackCaptureCompare`. `probes.json` gagne 6 sondes sur `Beginner-sector-20`, sur les faces sombres du `Building` proche (3 : zone d'ombres de l'original, à environ 28 m de côté et 25 m le long de la lumière du centre de sa carte) et de l'`Hotel02` (3 : 20 m de côté, 57 m le long de la lumière), carrés uniformes (écart type de 0,0 à 1,0 dans l'original, au plus 3,9 dans la conversion, au moins 8 px des arêtes). **L'original contre lui-même passe les quatre critères** : 34 / 34, 4 / 4, 33 / 33, 6 / 6. Conversion avant T3 (`report-t3a-before`) : 24 / 34, 0 / 4, 27 / 33 (identique à `report-rgce-vs3`) et **6 / 6 pour le critère 4 : il ne discrimine pas encore**, car ces faces sont presque noires dans l'original (2 à 17 sur 255) et dans la conversion (8 à 19). Le critère 4 ne dit donc quelque chose qu'après T3. Seul `Beginner-sector-20` montre un bâtiment proche dans la carte de l'original (les autres vues : aucun `Building`, `Hotel` ou `Casino01` projeteur dans ±42 m en travers et ±80 m le long de la lumière ; le moulin de `Expert-sector-80` est à 50 m de côté).

### T3 — résultat

> Validation (2026-11-14), run `rgce-t3-final` contre `original-s2b` (images identiques à `original-s1a`), rapport `artifacts/capture-bench/report-t3-final`. **T3 passe en 🧪 : les critères de sonde ne sont pas tous atteints ; les échecs restants sont classés ci-dessous.**
>
> **Réglages du monde de course** : `Mode = MultiplyLitColor`, `ShadowColor = (0,25 ; 0,26 ; 0,27)`, `MaxDistance` 100, `FadeDistance` 15, `DepthRange` 150, `FilterRadius` 3, résolution 4096, biais de profondeur 0,001 et de normale 0,1 inchangés. Biais en mètres `2 × 0,001 × (2 × 150 − 0,1)` = 0,6 m, comme avant (150 pour l'étendue et la profondeur). Rayon du filtre (F3, une retouche) : 2 au départ (bord plus net que l'original sur les planches), 5 essayé (le disque de 12 points se voit en grain sur le bord de l'ombre de `Expert-sector-50`), 3 retenu (bord dégradé, ni halo ni bande). Étendue : 90, 100 et 125 mesurés avec le rayon 3, somme des écarts moyens par vue (moyenne des trois canaux) 227,9 / 228,1 / 228,6, donc indifférent ; 100 gardé (marge F1 de 11 m, valeur de départ du plan). Trois cycles de mesure consommés (rayon 5, rayon 3, étendues 90 et 125). Le coût de rendu du filtre à 12 points et du mode multiplicatif n'est pas mesuré (le commentaire de `RaceWorldFactory.cs` le dit).
>
> **Drapeaux** : le portage d'`AddObjectToRender` reçoit `isNearTrackForShadowGeneration` à chaque site (palmes, lanternes, bannières, feu de départ, panneaux : vrai, comme `Track.cs:691-1074` ; objets du fichier de circuit, supports de rail, segments de colonne, enfants de modèles combinés, surcharge à distance : faux), avec le forçage par le nom (`windmill`, `hotel`, `building`, `casino01`) appliqué après le drapeau et avant les combinés (`Landscape.cs:388-393`) ; projette = membre ; reçoit = membre, nom sans `banner` ni `sign`, modèle ne commençant pas par `alpha`. Structures : route et rails projettent et reçoivent ; tunnels de même ; dessous de route ni l'un ni l'autre ; colonnes générées ni l'un ni l'autre ; terrain reçoit seulement ; plan de ville reçoit seulement (C1) ; voiture inchangée (projette et reçoit). **Écart de drapeaux mesuré par T3a : 0 écart de projection et 0 de réception sur 1147 / 1693 / 3404 objets** (avant : 1015 et 1059 / 1470 et 1591 / 2889 et 3126). Mesure de scène : points de route 0,000 (1332 / 2172 / 4466), 0 objet sans correspondance, 0 en trop ; `--verify-legacy-import-profile` code 0 ; build 0 erreur.
>
> **Critères de sonde** (original contre lui-même : 34 / 34, 4 / 4, 33 / 33, 3 / 3). Conversion avant T3 (`report-t3a-before`) puis après (`report-t3-final`) : critère 1 : 24 / 34 puis 24 / 34 ; critère 2 : 0 / 4 puis 2 / 4 (les deux surfaces de terrain : rapports 0,281 contre 0,284 et 0,264 contre 0,267) ; critère 3 : 27 / 33 puis 27 / 33 ; critère 4 : 3 / 3 puis 3 / 3 (valeurs 2, 2, 2 contre 2, 2, 3 dans l'original). Le critère 4 ne discriminait pas avant T3 (faces presque noires : 8 contre 2, écart de 6 sous la tolérance) ; il confirme le produit par la teinte après T3. **Correction de sondes** : les trois sondes `hotel-away-shadowed-*` de T3a sont retirées. Un run de l'original avec la passe d'ombres coupée (patch temporaire non commité de `TrackAuditCaptureScreen.cs`, `artifacts/capture-bench/original-noshadow`) donne pour l'Hôtel 15,17,18 / 24,24,24 / 18,20,22 contre 13,15,15 / 16,16,15 / 17,19,20 avec ombres : rapport 0,67 à 0,92, donc l'original ne les ombre pas (le haut de la façade sort de sa carte en perspective) ; les sondes `building-away-shadowed-*` donnent 8,9,9 contre 2,2,3, rapport 0,25. Il reste 3 sondes `away-shadowed`, sur le `Building` de `Beginner-sector-20`.
>
> **Échecs restants et classement** (critère 1 : `road-lit-1..3` de `Beginner-start`, `terrain-lit-1`, `city-lit-1..3`, `road-lit-1..2` de `Beginner-sector-50`, `building-away-1` ; critère 2 : surfaces `city` et `road` ; critère 3 : `city-shadow-1..3`, `road-shadow-1..3`).
>
> 1. **Assombrissement général de l'original, hors périmètre de T3 (mesuré).** Avec la passe d'ombres coupée, les sondes `lit` de l'original valent en moyenne 0,888 de leur valeur avec ombres (0,92 à 0,935 sauf la route de `Beginner-start`), et les sondes `away` (dont celles hors de toute carte) 0,936 : l'original assombrit de 6 à 8 % tout pixel, même hors ombre et hors carte. La conversion, éclairée, égale la valeur de l'original sans ombres. Cet écart explique le `terrain-lit-1` (+12, +11, +9) et une part de ceux de `city-lit` (+9 sur +22 une fois l'Hôtel retiré). Il ne se corrige pas par `ShadowColor`, qui reste la valeur de l'original. Les trois sondes `road-lit` de `Beginner-start` valent 0,60 de la valeur sans ombres dans l'original : elles sont à moitié dans une ombre floue, doute de placement de ces sondes, noté.
> 2. **`city-lit-1..3` (et l'écart de `Beginner-sector-20`) : projeteur, carte en perspective contre carte orthographique.** Le sol de ville à gauche est ombré dans la conversion (33,36,39 contre 111,117,119) ; run de diagnostic sans projeteur `Hotel*` (variable temporaire non commitée) : 133,139,143, l'ombre de l'`Hotel02` est donc la seule cause. Le drapeau est le même dans l'original (écart de drapeaux nul). C'est un bâtiment de 75 m de haut et d'environ 200 m de large (`Hotel02.gltf` × 1,2), à 20 m de côté et 57 m le long de la lumière du centre de la carte de l'original ; le calcul donne pour son pied (0,65 ; −0,96) en coordonnées normalisées de la carte (dedans) et, pour son sommet, 1,77 en latéral (dehors) : la carte en perspective de l'original (lumière virtuelle à 129 m) coupe le haut du bâtiment, la carte orthographique du moteur ne le coupe pas. Calcul, non mesuré.
> 3. **`road-shadow-1..3` (critères 2 et 3), `city-shadow-1` (bord) : cause non isolée.** Run de l'original où seuls la route, les rails et les tunnels projettent : 15,17,19 / 22,25,26 / 18,21,23 (la tache sombre reste) ; seuls les objets projettent : 55,61,63 / 47,52,53 / 59,66,69 (pas de tache). La tache vient donc de la piste, dont la conversion a les mêmes projeteurs ; elle est large (environ 6 m) et molle, la conversion n'y a que des bandes fines (61,68,69 contre 15,17,19). Écartés par mesure : le rejet des faces arrière (route et rails en `CullNone` dans la conversion : mêmes valeurs 61,68,69) et l'étendue (les sondes sont à 74 m dans le plan de la lumière, sous 85). Pistes non vérifiées : flou en espace écran de l'original contre filtre du shader, centre et perspective de sa carte (B7).
> 4. **`building-away-1` (`Beginner-overview`)** : mur de casino à plus de 300 m, noir dans l'original, éclairé dans la conversion : point ouvert O8, inchangé (hors de toute carte d'ombres).
>
> **Tableau F1** (distance au centre de la carte de la conversion, carré dans le plan de la lumière, centre = position de la caméra, `MaxDistance − FadeDistance` = 85). Méthode : le rayon du pixel de la sonde (caméra de `views.json`, 90° de champ vertical) est coupé avec le terrain (même fonction de hauteur que le portage, pas de 0,25 m, sans occultation par les objets), avec le plan du point de route le plus proche du rayon (route), avec le plan du sol de ville (terrain + 0,1) ou avec la boîte englobante du glTF du modèle (`Building`) ; distance = max(|latéral|, |haut du plan de la lumière|). Incertitude : routes ± 6 m le long du rayon (écartement des points de route, vue rasante : 1 m d'erreur de hauteur donne environ 9 m d'erreur horizontale à 90 m) ; terrain ± 2 m ; boîte du bâtiment ± 2 m ; centre de la carte arrondi au texel (0,05 m). Toutes les valeurs sont sous 85, la plus haute (74,3) laisse 11 m de marge.
>
> | Vue | Sondes | Distance à la caméra | Distance dans le plan de la lumière | Dans la carte de l'original |
> |---|---|---|---|---|
> | Beginner-start | `terrain-shadow-1..3` | 74,3 / 71,5 / 72,5 m | 21,8 / 20,4 / 17,3 | oui |
> | Beginner-sector-20 | `city-shadow-1..3` | 21,2 / 20,0 / 18,0 m | 14,7 / 13,0 / 9,6 | oui |
> | Beginner-sector-20 | `building-away-shadowed-1..3` | 51,2 / 59,9 / 52,6 m | 50,6 / 57,8 / 51,8 | oui |
> | Beginner-sector-50 | `road-shadow-1..3` | 90,9 / 84,2 / 91,0 m | 74,3 / 69,9 / 74,0 | oui |
> | Expert-sector-80 | `terrain-shadow-1..3` | 72,5 / 66,8 / 62,7 m | 63,3 / 59,0 / 54,6 | oui |
>
> **Écart moyen par vue** (moyenne des trois canaux, `report-rgce-vs3` puis `report-t3-final`) : Beginner-start 15,35 → 14,49 ; Beginner-sector-20 33,67 → 33,62 ; Beginner-sector-50 15,41 → 14,87 ; Beginner-sector-80 23,73 → 22,20 ; Advanced-start 15,71 → 15,24 ; Advanced-sector-20 20,44 → 20,03 ; Advanced-sector-50 14,15 → 13,73 ; Advanced-sector-80 12,30 → 12,23 ; Expert-start 12,60 → 11,88 ; Expert-sector-20 17,13 → 16,92 ; Expert-sector-50 14,15 → 13,94 ; Expert-sector-80 10,84 → 9,87 ; vues d'ensemble (sans critère) : Beginner 10,40 → 10,30, Advanced 8,70 → 8,70, Expert 10,05 → 10,05. La règle F4 est tenue sur la moyenne des canaux ; **par canal, `Beginner-sector-20` monte de 0,29 (rouge) et 0,18 (vert)** (l'ombre de l'Hôtel, point 2).
>
> **Planches** (`compare-Beginner-sector-20.png`, `compare-Beginner-start.png`, `compare-Expert-sector-50.png`). Teinte : l'ombre de la conversion est gris-bleu sombre, comme celle de l'original (rapport ombre / lumière 0,264 à 0,281 contre 0,267 à 0,284 sur le terrain). Placement : les ombres du terrain, des rails, de la bannière et de la voiture de `Beginner-start` et celles des palmes de `Expert-sector-50` sont aux bons endroits, avec un décalage de forme (les palmes de l'original projettent des cartes pleines, double face, sans test alpha) ; sur `Beginner-sector-20` le sol à gauche est ombré en trop (Hôtel) et la bande d'ombre de la route à droite est plus étroite que celle de l'original. Douceur : bord dégradé sur trois à quatre pixels, ni halo ni bande avec le rayon 3 (du grain à 5), mais encore plus net que le flou en espace écran de l'original ; une ombre fine (poteaux de panneaux, bords de rail) reste fine dans la conversion là où l'original l'étale. Aucun bord de carte visible sur ces vues (fondu de 15 m).
>
> Non vérifié : le coût de rendu du filtre ; les vues d'ensemble (leur variation est notée sans critère, D6) ; les positions des sondes sont reconstruites par calcul, non lues dans le moteur ; la hauteur d'ombre de l'Hôtel est une estimation à partir de la boîte du modèle.
