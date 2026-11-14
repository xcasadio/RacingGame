# Plan agent IA — Fidélité 3D, tranche S1 : géométrie

Plan de la première tranche de corrections issues du banc de capture 3D (`docs/racinggame-3d-capture-bench-findings.md`, écarts G1 à G5), approuvé par l'auteur le 2026-11-14.
Les décisions D1 → D6 ci-dessous ont été arbitrées avec l'auteur le 2026-11-14 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Sur les 15 vues du banc, la route, ses ouvrages et les objets de la conversion sont aux mêmes endroits, avec la même orientation et la même échelle que dans l'original. Les matériaux, l'éclairage et les effets sont hors de cette tranche (S2 à S4).

## État vérifié du dépôt (2026-11-14)

- RacingGame : branche `remaster`, HEAD `08a4a32`. `CasaEngine` (sous-module, arbre de travail à `778f6aa`, propre, sur `main`), `.codegraph/`, `.serena/`, `MGUI/` et `log.txt` sont à l'auteur : jamais indexés.
- Tracé : les formules de `LegacyTrackSceneFactory.BuildRoadSplinePoints` (`:1055-1296`) correspondent ligne à ligne à `TrackLine.Load` (`RacingGameOriginal/Core/Tracks/TrackLine.cs:176-488`) d'après la lecture ; pourtant les vues montrent la route décalée loin du départ. Cause non établie.
- Dessous de route (`Track.cs:408-438`) et tunnels (`Track.cs:503-635`) : absents de la conversion.
- Sol de ville (`Landscape.cs:864-875`, `CreateCityPlane`) : absent de la conversion.
- Décor aléatoire (`Track.cs:1089-1161`) : absent. Palmiers, banderoles et variantes de panneaux utilisent un générateur semé par le nom du circuit (`LegacyTrackSceneFactory.cs:793-807`), l'original un générateur global semé par `--seed` (`RandomHelper.cs`).
- Segments de colonne : l'original ne les pose qu'en `HighDetail`, sans échelle propre (`TrackColumns.cs:237-243`) ; la conversion les pose toujours, à l'échelle 1,2 (`LegacyTrackSceneFactory.GuardRails.cs:401`).
- Les fichiers `.CombiModel` sont bien copiés dans la sortie de build (10 fichiers).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | La référence est `RacingGameOriginal/` ; toute correction se prouve en relançant le banc (ADR-0015). |
| D2 | On mesure avant de corriger : l'original exporte ses points de tracé et ses objets placés, la conversion journalise l'écart. |
| D3 | Pas de contournement : si une cause est dans le moteur, la tâche passe en ⚠️ et une tranche moteur est planifiée dans le dépôt `CasaEngine` (branche dédiée, jamais `main`, sans push). |
| D4 | Décor aléatoire : même ordre de tirage que l'original ; graine fixe pour le banc (celle de `views.json`), graine horaire en jeu comme l'original. |
| D5 | Mode AUTO, un commit par tâche sur `remaster`, aucun push. |
| D6 | Matériaux, lumière, ombres, ciel et effets ne sont pas touchés ici. |

## Règles d'exécution pour l'agent

- **Branche `remaster`**, sans push. Ne jamais committer sur `master`.
- **Une seule tâche à la fois**, icône mise à jour, validation notée sous la tâche, **un commit par tâche** incluant ce fichier.
- Messages de commit en anglais, format `type(area): summary`.
- **Ne jamais indexer** les fichiers de l'auteur : `git add` fichier par fichier.
- Sans argument de capture, les deux jeux se comportent comme avant, sauf les corrections voulues de la conversion.
- Langue : ce plan en français ; code, commits, ADR et docs en anglais.

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Validation globale

- `dotnet build RacingGameOriginal/Desktop/RacingGame.csproj -c Debug` et `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` : 0 erreur.
- Banc relancé (`scripts/TrackCaptureCompare/README.md`) : rapport comparé à celui du 2026-11-14 (`docs/racinggame-3d-capture-bench-findings.md`), part de contours retrouvés par vue notée ici.
- Journal de mesure de T1 : écart maximal du tracé et liste des objets sans correspondance, avant et après.

---

## Phase 1 — Géométrie

### ✅ T1 — Mesurer : tracé et objets exportés par l'original

> Validation (2026-11-14) : builds 0 erreur. Original relancé avec `--seed 1` : 30 PNG et `views.json` identiques au bit près à la référence du banc ; `scene.json` écrit (6,6 Mo). Conversion : `scene-measure-<Track>.txt` écrit pour les trois circuits. Mesure : positions des points de tracé à 0,000 m (1332, 2172 et 4466 points), vecteurs droite et haut à moins de 0,04° ; **largeur de route** différente jusqu'à 0,8 (Beginner), 0,6 (Advanced), 0,53 (Expert) ; objets placés 169 / 1147 (Beginner), 268 / 1693 (Advanced), 539 / 3404 (Expert). Cause de la largeur et des zones vides, lue dans le code : `LegacyTrackLayout` désérialise `WidthHelpers` et `RoadHelpers` sans nommer leurs éléments (`WidthHelper`, `RoadHelper`), donc les deux listes sont vides. Les supports de rail (`GuardRailHolder`) de la conversion ne sont pas enregistrés par la mesure : ils apparaissent tous comme manquants, à traiter en T2.

- Objectif : `--capture-track-audit` écrit aussi `scene.json` (par circuit : points de tracé avec position, droite, haut, largeur ; objets placés avec nom de modèle et matrice). `--capture-reference-views` lit ce fichier s'il est à côté de `views.json` et journalise, par circuit : l'écart de chaque point de tracé (maximum, moyenne, premier indice au-delà de 5 cm), et pour chaque objet de l'original l'objet de même modèle le plus proche dans la conversion (distance, écart d'orientation, écart d'échelle), plus les objets sans correspondance de chaque côté.
- Fichiers : `RacingGameOriginal/Core/GameScreens/TrackAuditCaptureScreen.cs`, accès en lecture dans `Core/Tracks/TrackLine.cs` et `Core/Landscapes/Landscape.cs` ; `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs`, `Worlds/LegacyTrackSceneFactory*.cs` (accès en lecture aux points et aux objets), `scripts/TrackCaptureCompare/README.md`.
- Validation : builds 0 erreur ; les images de l'original restent identiques au bit près à la référence ; le journal de la conversion contient la mesure pour les trois circuits.
- Commit : `feat(capture): export the original's track points and objects and measure the port against them`

### ✅ T2 — Corriger ce que la mesure désigne (G1, G5)

> Validation (2026-11-14) : cause 1 corrigée (`c24e677`) : `WidthHelpers` et `RoadHelpers` désérialisés avec leurs noms d'éléments ; largeur de route à 0,000 de l'original sur les trois circuits. Restent les objets : la mesure montre que l'ordre d'ajout, la règle de doublon (`Model.Size`), l'échelle 1,2 et le passage des supports de rail et des segments de colonne par `AddObjectToRender` diffèrent ; ce travail est fusionné avec T4 (même code, même critère : 0 objet sans correspondance).

- Objectif : le tracé de la conversion suit celui de l'original (écart maximal sous 5 cm) ; les objets communs sont à la même place, orientation et échelle (panneaux, segments de colonne).
- Fichiers : fixés par T1 (attendus : `Worlds/LegacyTrackSceneFactory*.cs`, éventuellement des modèles glTF de `RacingGameCasaEngine/Content/Models`).
- Validation : mesure de T1 relancée ; vues `sector-*` du banc.
- Commit : `fix(racing-casa): …` (un commit par cause distincte)

### ✅ T3 — Ouvrages absents : dessous de route, tunnels, sol de ville (G4)

> Validation (2026-11-14) : build 0 erreur ; mesure inchangée (`artifacts/capture-bench/rgce-s1d`, relancée par la session principale). Dessous de route porté de `Track.cs:408-485` (5328, 8688 et 17864 sommets), tunnels de `Track.cs:503-635` (1, 2 et 10 zones ; les mêmes zones que l'algorithme de l'original rejoué sur `scene.json`), plan de ville de `Landscape.CreateCityPlane` (un seul plan, celui de Beginner, pour les trois circuits comme dans l'original). Vu sur les planches : sol pavé entre les immeubles (`Beginner-sector-20`), dessous de route sur les sections surélevées (`Expert-sector-50`, `Advanced-sector-80`). Aucune des 15 vues ne montre un tunnel : vérifiés par les comptes seulement. Le sol de ville apparaît presque noir : il est dans l'ombre des immeubles, ombres noires et lumière relèvent de S2. Les zones commencent désormais sur tout type de repère, Reset compris, comme l'original.

- Objectif : la conversion construit le dessous de route, les tunnels et les plans de ville comme l'original (mêmes sommets, mêmes coordonnées de texture, mêmes textures).
- Fichiers : `Worlds/LegacyTrackSceneFactory*.cs`.
- Validation : build ; vues `Beginner-sector-20`, `Beginner-overview`, `Expert-overview` et vues de tunnel du banc.
- Commit : `feat(racing-casa): build the road underside, the tunnels and the city ground`

### ✅ T4 — Décor aléatoire dans l'ordre de l'original (G3)

> Validation (2026-11-14), faite avec la fin de T2 dans un seul commit : mesure relancée par la session principale (`artifacts/capture-bench/rgce-s1c`) : Beginner 1147 / 1147, Advanced 1693 / 1693, Expert 3404 / 3404 objets, 0 sans correspondance de chaque côté, distance et échelle à 0,000, rotation sous 0,04° ; tracé inchangé. Causes : les fichiers `.CombiModel` étaient désérialisés en listes vides (nom d'élément `CombiObject` non déclaré) ; supports de rail et segments de colonne ne passaient pas par le chemin d'ajout de l'original (échelle 1,2, plaquage au terrain, règle de doublon, ordre) ; le vecteur droite non normalisé de l'original n'était pas reproduit pour les décalages ; l'angle de route non borné (NaN) change un tirage ; décor aléatoire et ordre de tirage portés depuis `Track.cs:780-1147`. Graine : `LegacyTrackSceneFactory.SceneryRandomSeed`, celle de `views.json` pour le banc, horaire sinon. `--capture-track-audit` de la conversion (sans graine) : code de sortie 0, aucune erreur au journal.

- Objectif : palmiers, banderoles, panneaux et décor hors route sont tirés dans le même ordre et avec le même générateur que l'original ; avec la graine du banc, la mesure de T1 ne laisse aucun objet sans correspondance.
- Fichiers : `Worlds/LegacyTrackSceneFactory*.cs`, `Bootstrap/RaceLaunchOptions.cs` (graine), `Bootstrap/ReferenceViewCaptureValidator.cs`.
- Validation : mesure de T1 ; vues du banc.
- Commit : `feat(racing-casa): place the random scenery in the original's draw order`

### 🧪 T5 — Banc relancé et constats mis à jour

> Validation (2026-11-14) : banc relancé (`artifacts/capture-bench/rgce-s1d`, rapport `report-s1d`) ; constats mis à jour dans `docs/racinggame-3d-capture-bench-findings.md` (G1, G3, G5, G6 fermés ; G4 fermé pour la géométrie ; G2 reclassé en ombre, L1 ; G7 ouvert). Part de contours retrouvés en hausse sur les 15 vues. Reste : relecture par l'auteur.

- Objectif : relancer le banc, noter les mesures, mettre à jour `docs/racinggame-3d-capture-bench-findings.md` (écarts fermés, écarts restants).
- Validation : relecture par l'auteur.
- Commit : `docs(racing-casa): update the 3D findings after the geometry fixes`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | La règle de doublon utilise une table des tailles de modèle de l'original (`LegacyModelSizes`, relevée sur les `.xnb` de l'original) : les tailles déduites des glTF diffèrent (ordre des maillages pour les palmiers, Windmill, Casino01, Hotel01 ; RoadColumnSegment à 2,05 au lieu de 49,05). À instruire avec les matériaux : est-ce une erreur d'export des modèles ? | T2, S3 |
| O2 | `scripts/TrackPlacementExporter/RuntimeSceneComparison.cs` réimplémente l'ancien placement et ne correspond plus à la conversion. À supprimer ou à réaligner : décision de l'auteur. | T2 |
| O3 | Le maillage de route de la conversion utilise le vecteur droite normalisé ; l'original, le dessous de route et les tunnels utilisent le vecteur brut (non normalisé). Écart d'une fraction de pour cent sur la largeur là où sa norme est sous 1 : non mesuré par le banc, à aligner ou à accepter. | T3 |
| O4 | La conversion n'a pas de réglage « ne reçoit pas d'ombre » par maillage : dans l'original, le dessous de route et le plan de ville n'en reçoivent pas. À traiter dans le moteur avec S2. | T3, S2 |

## Hors périmètre

- Matériaux, lumière, ombres, ciel, effets (tranches S2 à S4).
- Le « ruban noir » (G2) et la banderole de départ d'Advanced (G6) tant que la mesure ne les rattache pas à la géométrie.
- Toute modification du moteur sans plan propre dans son dépôt.
