# Plan agent IA — Fusion du travail PBR poussé et reprise de la note de passation

Plan d'exécution de la reprise décrite dans [HANDOVER.md](../HANDOVER.md) (présent après T1.2 ; avant, `git show origin/remaster:ai-agent/HANDOVER.md`).
Les décisions D1 → D4 ci-dessous ont été arbitrées avec l'auteur le 2026-11-16 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Réunir sur de nouvelles branches le travail local de ce PC et le travail poussé depuis l'autre PC (bascule PBR du monde de course, moteur `origin/main`), dans le jeu et dans le moteur, puis reprendre les suites de la note de passation, une tranche à la fois.

Tranches :

| Tranche | Contenu | Dépend de | État |
|---|---|---|---|
| S1 | Fusion du moteur (T1.1) | — | Faite le 2026-11-16, avec la réserve D5 |
| S1b | Fusion de `origin/remaster` en gardant le code et le contenu du jeu d'ici (Phase 3) : histoires jointes, docs, outils et original repris, ADR renumérotés. Le travail PBR de l'autre PC n'est pas actif après cette tranche | S1 | Faite le 2026-11-16 |
| S1v | HANDOVER §7 : vérification indépendante des fusions du moteur et du jeu (`verifier` frais), captures UI et audit de piste du jeu sur le moteur fusionné. Avant toute tranche qui change le moteur, le contenu ou les matériaux | S1b | Détaillée (Phase 4) |
| S1c | Banc de mesure porté ici : `--capture-reference-views`, points d'export de la scène, `ReferenceSceneMeasure`, `TrackCaptureCompare`. Première mesure de la version d'ici contre l'original sur ce PC, avant PBR ; écarts des sujets faits en double remis à l'auteur | S1v | À détailler |
| S1d | Moteur : le format `.staticModel` stocke les tangentes (branche moteur, ADR, tests, plan moteur) | S1c | À détailler |
| S1e | Assets : convertisseur porté (tangentes dans les glTF, normal maps au format standard, cube du ciel réorienté), contenu régénéré, cuiseur avec tangentes, recuisson | S1d | À détailler |
| S1f | Matériaux PBR cuits (D6) : recettes par famille, cuiseur en `lit-pbr`, peinture de la voiture et plateau du carrousel, texture de détail du terrain, vérificateurs, suppression des tables de réglage et du relèvement d'ambiant du profil d'import ; ADR du jeu mis à jour (0018 de là-bas et 0026 d'ici réunis par un ADR qui les amende) | S1e | À détailler |
| S1g | Monde de course : `LinearHdr`, lumière clé, ombres, ciel en phase de scène, mode `Legacy` pour le banc, tangentes de la route, des tunnels et des maillages générés | S1f | À détailler |
| S1h | Géométrie qui n'existe que sur l'autre PC, en générateurs : dessous de la route, sol de la ville, décor de remplissage aléatoire ; ADR-0019 corrigé (il affirme qu'il ne manque rien face à l'original) | S1f | À détailler |
| S2 | Vérification indépendante du portage PBR (S1c à S1h), banc de capture contre l'original, nouvelle référence sur ce PC | S1c, S1g, S1h | À détailler après S1 |
| S3 | Tunnel presque noir | S2 | À détailler ; choix de l'auteur attendu (O1) |
| S4 | Voiture centrale du carrousel trop blanche | S2 | À détailler ; choix de l'auteur attendu (O2) |
| S5 | Défauts connus du §4 de la note (moteur sur une branche moteur dédiée, jeu sur cette branche) | S2 | À détailler |

Les tranches S1b, S1v, S1c, S1d, S1e, S1f s'exécutent dans cet ordre, une à la fois ; S1g et S1h suivent S1f ; S2 vient après S1g et S1h. La mesure « avant PBR » de S1c est donc toujours prise avant tout changement du moteur, du contenu ou des matériaux.

Hors périmètre : tout push ; tout commit sur `main` (moteur), `master` ou `remaster` (jeu) ; les branches `claude/*` et les autres branches `chantier/*` ; toute commande git dans `MGUI` ou `NvgSharp` ; tout réglage visuel autre que ceux nommés ; tout refactor.

## État vérifié du dépôt (2026-11-16)

- Jeu : `remaster` local (`ad9f892`) a 71 commits d'avance et 46 de retard sur `origin/remaster` (`90e3d99`), ancêtre commun `d8ac303` (`git status -sb`, `git merge-base`).
- Jeu : `git merge-tree --write-tree HEAD origin/remaster` annonce des conflits sur le pointeur `CasaEngine`, `LegacyCarVisualFactory.cs`, `RacingGameLegacyMaterialTuning.cs` (renommé et modifié ici, supprimé là-bas), `LegacyTrackSceneFactory.Terrain.cs`, `RaceTrackLayout.cs`, `LegacyImportProfileVerifier.cs`, `RaceLaunchOptions.cs`, `RacingGameCasaEngineGame.cs`, six normal maps PNG et `SkyCubeMap.dds`, `RacingGameCasaEngine.csproj`, `CarSelectionCarousel.cs`, `LegacyTrackSceneFactory.GuardRails.cs`, `RaceWorldFactory.cs`, `ai-agent/README.md`, `docs/decisions/README.md`, `scripts/LegacyModelGltfConverter/Program.cs`.
- Jeu : pointeur du sous-module à `87fc5f01` ici, à `863627b4` sur `origin/remaster`.
- Moteur : branche `chantier/rgce-editor-project` (`87fc5f01b`), 24 commits absents d'`origin/main` (`187a420d7`), qui en a 248 de plus. `main` local (`778f6aa8c`) : 0 d'avance, 248 de retard.
- Moteur : conflits annoncés sur `EntityDetailsPanel.cs`, `EditorAssetWriterService.cs`, `MaterialDefinitionEditorRegistryTests.cs`, `ElementFactory.cs`, `BuiltInMaterialDefinitions.cs`, `ai-agent/README.md`, `docs/decisions/README.md`.
- `MGUI` : arbre à `d0efd9b4`, le pointeur d'`origin/main` (la branche de chantier enregistre `4e94d7e8`). `NvgSharp` : pointeur identique des deux côtés.
- Changements préexistants de l'auteur, jamais indexés : `.serena/`, `log.txt` (jeu) ; ` M MGUI` (moteur, résolu par la fusion sans commande dans `MGUI`).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Moteur : `main` local avancé en fast-forward sur `origin/main` (aucun commit sur `main`), nouvelle branche `chantier/merge-editor-project` créée depuis `main`, `chantier/rgce-editor-project` fusionné dedans. |
| D2 | Jeu : nouvelle branche `chantier/rgce-pbr-handover` créée depuis `remaster` local, `origin/remaster` fusionné dedans ; `remaster` n'est pas touché. |
| D3 | Après les fusions, reprendre de la note : §7 (vérification de la fusion), tunnel presque noir, carrousel trop blanc, défauts connus. |
| D4 | Mode ASK : arrêt et question à l'auteur à chaque blocage. |
| D5 | Fusion du moteur committée avec réserve : 8 tests GPU échouent déjà sur `origin/main` seul sur ce PC. |
| D6 | Matériaux du jeu : garder les assets cuits **et** le PBR. Le cuiseur et ses recettes produisent des matériaux `lit-pbr` (et les tangentes), puis tout est recuit. Tranche à part, plan détaillé soumis avant exécution. |
| D7 | Sujets faits en double sur les deux PC (O4) : la version de ce PC sert de référence. Ce qui n'existe que sur l'autre PC est porté dans l'architecture d'ici. |
| D8 | L'original de référence peut être modifié pour compiler et servir ici (copie `RacingGameOriginal/` d'`origin/remaster`, ou `D:/development/repo/MonoGame-Samples/RacingGame`). |
| D9 | L'original compile ici en retirant son multijoueur LAN, plutôt qu'en recopiant le package depuis l'autre PC. |

## Règles d'exécution pour l'agent

- **Branches dédiées** `chantier/rgce-pbr-handover` (jeu) et `chantier/merge-editor-project` (moteur). Ne jamais committer sur `main`, `master` ni `remaster`.
- **Une seule tâche à la fois**, icône de statut tenue à jour, **un commit par tâche** qui inclut la mise à jour de ce fichier, message en anglais `type(area): summary`.
- **Ne jamais pousser.**
- **Ne rien inventer** : sinon ⚠️ Blocked, question dans « Points ouverts », arrêt.
- **Build obligatoire** avant ✅ dès que du code est touché ; tests dès que du code testé est touché.
- **Ne jamais indexer** `.serena/`, `log.txt`, `.codegraph/` ; `git add` fichier par fichier.
- **Aucune commande git dans `MGUI` ni `NvgSharp`.**
- Un seul lancement du jeu ou de l'éditeur à la fois, jamais pendant un build ou des tests.
- **Règle de fusion du jeu** (remplace la règle de conflit écrite pour T1.2, abandonnée avec D7) :
  - (a) code et contenu du jeu : la version d'ici est la référence (D7). S1b garde l'état d'ici tel quel ; ce qui n'existe que sur l'autre PC est porté dans l'architecture d'ici par la tranche nommée dans la « Carte des commits de l'autre PC », jamais repris tel quel ;
  - (b) docs, plans, ADR, outils autonomes et original de référence ajoutés là-bas : repris par S1b ;
  - (c) index des ADR et des plans : lignes des deux côtés gardées ; numéros d'ADR en double : les ADR d'ici sont renumérotés, avec leurs renvois ;
  - (d) API du moteur qui a bougé : adaptation minimale du point d'appel du jeu, listée dans le commit ; une correction qui demande de toucher au moteur bloque la tâche ;
  - (e) sujet fait en double dont les deux versions divergent : la version d'ici reste, l'écart est mesuré par le banc (S1c) et remis à l'auteur. Aucune correction d'office ;
  - ces règles valent pour S1b et pour toutes les tranches de portage S1c à S1h.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale (S1)

- Moteur : `dotnet build CasaEngine.MonoGame.sln` et `dotnet build CasaEngine.Editor.MonoGame.sln` à 0 erreur ; `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj --blame-hang-timeout 5m` vert, pas moins de 6119 tests.
- Jeu : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` à 0 erreur ; le jeu n'a aucun projet de test (constat de la Phase 3) ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès.
- S1 n'affirme pas l'équivalence visuelle : c'est l'objet de S2.

Retour arrière de S1 : jeu, `git switch remaster` puis `git branch -D chantier/rgce-pbr-handover` ; moteur, `git switch chantier/rgce-editor-project`, `git branch -D chantier/merge-editor-project`, `git branch -f main 778f6aa8c`.

---

## Carte des commits de l'autre PC

Les 46 commits d'`origin/remaster` depuis `d8ac303`, chacun avec la tranche qui le reprend ou ce qu'il devient. « Porté » veut dire réécrit dans l'architecture d'ici, pas repris tel quel.

| Commit | Sujet | Devenir |
|---|---|---|
| `7686c0a`, `de010a2`, `b4374f4` | Copie de l'original, mode de capture, voiture sur la grille | S1b (déjà dans la branche par T2.1, multijoueur retiré par T2.2) |
| `337a43c` | `ThrowOnScriptTypeResolution` dans la politique d'exécution | S1b : propriété `required` du moteur fusionné, absente ici ; adaptation du point d'appel (règle d) |
| `c6a0372`, `868c8a7` | `TrackCaptureCompare` et ses sondes | S1b (outil repris), utilisé à partir de S1c |
| `b4642e8` | Rejeu des vues de référence de l'original | S1c, porté |
| `c3338a3`, `62f7956` | Export des points, objets et ombres ; mesure du port contre l'original | Côté original : S1b. Côté port : S1c, porté |
| `c24e677` | Lecture des aides de largeur et de route | Fait en double : version d'ici (D7). Écarts mesurés en S1c (règle e) |
| `80c667c` | Placement exact des objets | Fait en double : version d'ici (D7). Écarts mesurés en S1c (règle e) |
| `0ac894d` | Dessous de la route, tunnels, sol de la ville | Tunnels : faits en double, version d'ici (D7). Dessous de la route et sol de la ville : S1h, portés |
| `b406f4f` | Normal maps au format standard | S1e, porté |
| `8b30f98` | Tangentes des modèles importés | S1e, porté (dépend de S1d) |
| `074f242` | Cube du ciel aux axes du port | S1e, porté |
| `4c058ad` | Couleurs importées arrondies, matériaux sans effet par défaut | S1e, porté (lecteur glTF utilisé par le cuiseur, puis recuisson) |
| `58bb871` | Textures du monde de course avec mipmaps | S1e, porté (textures cuites) |
| `a18b364` | Tangentes et normal map de la route | Maillage : S1g. Matériau : S1f |
| `f309d6b` | Texture de détail du terrain | S1f, porté (matériau `lit-pbr`, pas de changement de maillage) |
| `fb7daa2` | Terme spéculaire retiré du terrain | S1f : sans objet en `lit-pbr` (pas de propriété spéculaire), à constater sur le matériau cuit |
| `46f2013` | Ambiant des panneaux, banderoles et éoliennes plus relevé | S1f : le relèvement existe encore ici dans le profil d'import ; retiré avec les tables de réglage |
| `d0db72f` | Terrain, route, rails et structures en PBR | Matériaux : S1f. Tangentes des maillages générés : S1g |
| `09f1c54` | Décor importé en PBR, immeubles de verre en miroir | S1f, porté |
| `d35a25a` | Peinture PBR vernie de la voiture, course et carrousel | S1f, porté |
| `b5391fe` | Suppression des tables de réglage spéculaire | S1f |
| `d66c210` | Lumière unique de l'original et ambiant par matériau | Lumière : S1g. Ambiant par matériau : sans objet en `lit-pbr`, à constater en S1f |
| `d40b020` | Ombres douces et teintées | S1g, porté |
| `09e4a9f` | Rendu `LinearHdr`, mode `Legacy` pour le banc | S1g, porté |
| `037f5a5` | Éclairage par le cube du ciel, lumière clé remise à l'échelle | S1g, porté |
| `08a4a32`, `c4b400c`, `9bd1693`, `32a7eb7`, `484c498`, `b2e54bb`, `3e4256e`, `7331eef`, `738902d`, `93cea46`, `392e4aa`, `2991d70`, `f8b73b0`, `4f3311b` | Docs, plans, constats du banc, note de passation | S1b (repris tels quels) |
| `734541a`, `7c46e5c`, `90e3d99` | Pointeur du sous-module | Remplacés par le pointeur `e942caa54` en S1b ; les renvois de docs de `7c46e5c` sont repris par S1b |

Ce qui reste de la note de passation : §7 → S1v ; §4 tunnel → S3, carrousel → S4, défauts connus → S5 ; réglages à l'œil, contrôles visuels et arbitrages à valider par l'auteur (P1 à P7 du plan clear coat, ADR du moteur et ADR-0018 de là-bas encore en `Proposed`) → hors de ce plan, rappelés au rapport final.

---

## Phase 0 — Préparation

### ✅ T0.1 — Branche du jeu et plan

- Objectif : créer la branche de travail du jeu et y enregistrer ce plan.
- Fichiers : `ai-agent/tasks/rgce-pbr-handover-tasks.md`, `ai-agent/README.md`.
- Validation : `git status` ne montre que ces deux fichiers indexés.
- Commit : `docs(ai-agent): plan the merge of the pushed PBR work and the handover follow-up`

---

## Phase 1 — Fusions (S1)

### ✅ T1.1 — Fusion du moteur

- Objectif : `chantier/merge-editor-project` = `origin/main` + les 24 commits locaux.
- Fichiers (moteur) : les 7 fichiers en conflit, les ADR locaux à renuméroter s'il y a lieu, le pointeur `MGUI`.
- Étapes :
  1. Vérifier `git rev-list --count origin/main..main` = 0, puis `git branch -f main origin/main`.
  2. `git switch -c chantier/merge-editor-project main`, puis `git merge --no-ff --no-commit chantier/rgce-editor-project`.
  3. Résoudre chaque conflit après lecture des commits des deux côtés pour le fichier ; garder les deux intentions.
  4. ADR : lister ceux ajoutés par chaque côté depuis l'ancêtre commun ; renuméroter les locaux en cas de doublon et corriger leurs renvois.
  5. Vérifier que l'index enregistre `MGUI` à `d0efd9b4` (`git ls-files -s MGUI`).
- Validation : aucun marqueur de conflit ; les deux builds à 0 erreur ; tests verts, total noté.
- Commit : `merge: integrate chantier/rgce-editor-project into main's history (origin/main 187a420d)`

> Validation (2026-11-16), commit moteur `e942caa54` sur `chantier/merge-editor-project` :
> - Les 7 conflits résolus en gardant les deux côtés. `EditorAssetSaveSource` : `WorldInspector` garde 8, `TextureEditorPanel` et `StaticModelEditorPanel` passent à 9 et 10.
> - Deux adaptations dues à la fusion : `EditorWorldOperations.IsAddableComponentType` cache aussi `UnresolvedComponent` ; `LitPbrTransparentGpuTests` renseigne lui-même `RenderItem.Features`.
> - ADR du chantier renumérotés avec leurs renvois : 0071 → 0118, 0072 → 0119, 0073 → 0120, 0074 → 0121, 0075 → 0122. Les renvois du jeu vers ces ADR (`docs/decisions/0017`, `0020`, `ai-agent/tasks/rgce-element-naming-tasks.md`) restent à décaler avec la fusion du jeu.
> - `MGUI` enregistré à `d0efd9b4`. Builds des deux solutions : 0 erreur.
> - Tests : 6378 lancés, 6370 verts, 8 en échec (7 empreintes de pixels de `DesktopFrameGpuAllocationTests` et `DrawTransactionRearmGpuTests`, et `IblCubeGeneratorGpuTests.OpenGlChain_IgnoresTheRequestedCubeLevel_SoTheAtlasIsUsed`). Ces 8 tests échouent à l'identique sur `origin/main` seul sur ce PC (worktree temporaire). Réserve acceptée par l'auteur (D5) ; suite en O3.
> - Écart connu, non traité : la liste des composants des commandes distantes (`EditorWorldOperations.ComponentTypes`) reste calculée une seule fois, alors que celle du dialogue est reconstruite à chaque ouverture.

### ⚠️ T1.2 — Fusion du jeu

- Objectif : `chantier/rgce-pbr-handover` = `remaster` local + `origin/remaster`, sous-module pointé sur le commit de fusion de T1.1.
- Fichiers : les chemins en conflit listés plus haut, le pointeur `CasaEngine`, ce plan.
- Étapes :
  1. Relever `git rev-parse HEAD` et le noter sous cette tâche, puis `git merge --no-ff --no-commit origin/remaster`.
  2. Résoudre selon la règle de conflit du jeu.
  3. `git add CasaEngine` une fois le moteur sur le commit de T1.1 ; vérifier avec `git diff --cached --submodule`.
  4. Recopier `ai-agent/handover/pbr-baseline/*.sh` vers `CasaEngine/CasaEngine/artifacts/pbr-baseline/` (dossier ignoré par git).
- Validation : aucun marqueur de conflit ; build du jeu à 0 erreur ; projets de test du jeu verts ; `--smoke-frontend` puis `--verify-legacy-import-profile` en succès.
- Commit : `merge: integrate origin/remaster (PBR race world) into the local remaster work`

> Bloquée le 2026-11-16, avant le début de la fusion : collision de conception que la règle de conflit ne tranche pas. Ici, les modèles et matériaux sont des assets cuits `lit-diffuse` chargés par identifiant (ADR-0026 d'ici, numéroté 0018 jusqu'à la fusion, `scripts/RgceAssetCooker`). Sur `origin/remaster`, les matériaux sont créés en code en `lit-pbr` (`RacingGamePbrMaterialMapping`), avec tangentes, normal maps converties et monde en `LinearHdr`. Décision de l'auteur : D6. La fusion sera reprise par une tranche détaillée à part (S1b), soumise à approbation.
> Autre collision relevée : les ADR du jeu 0015 à 0018 existent des deux côtés avec des sujets différents.

---

## Phase 2 — Original de référence sur ce PC

### ✅ T2.1 — Copie de référence dans la branche

- Objectif : avoir `RacingGameOriginal/` dans la branche avant la fusion du jeu.
- Fichiers : `RacingGameOriginal/` (371 fichiers), repris tels quels d'`origin/remaster` (`90e3d99`).
- Validation : `git diff --quiet <commit> origin/remaster -- RacingGameOriginal` sans différence.
- Commit : `chore(original): bring the reference copy from origin/remaster` (`a85694e`)

### ✅ T2.2 — Retrait du multijoueur LAN de l'original

- Objectif : l'original compile et capture sur ce PC sans le package `MonoGame.Xna.Framework.Net` (D9).
- Fichiers : supprimés, `Core/Multiplayer/*` (6 fichiers), `Core/GameScreens/LanBrowserScreen.cs`, `LanLobbyScreen.cs`, `MultiplayerGameScreen.cs` ; modifiés, `Core/Core.csproj`, `Core/RacingGameManager.cs`, `Core/Graphics/BaseGame.cs`, `Core/GameScreens/CarSelection.cs`, `PlayModeScreen.cs`, `LoadingScreen.cs`.
- Validation : voir la note.
- Commit : `chore(original): remove the LAN multiplayer so the reference builds without its network package`

> Validation (2026-11-16) :
> - `dotnet build Desktop/RacingGame.csproj` et `dotnet build TrackTool/TrackTool.csproj` : 0 erreur.
> - `RacingGame.exe --capture-track-audit --out <dossier> --size 1280x720` : sortie 0, 30 fichiers (3 pistes, 5 vues, `full` et `nopost`). `Beginner-start-full.png` regardée : piste, voiture, banderole, palmiers et ombres présents.
> - Non vérifié : une course jouée à la main ; l'égalité de ces captures avec celles de l'autre PC.
> - `Documentation/LAN-Multiplayer.md` et le README décrivent encore le multijoueur : laissés tels quels.
> - À la fusion du jeu : `origin/remaster` ramènera les 9 fichiers supprimés et l'ancienne version des 6 fichiers modifiés (ajouts des deux côtés) ; garder la version d'ici.

---

## Phase 3 — Fusion du jeu en gardant l'architecture d'ici (S1b)

Constats de l'étude du 2026-11-16 (trois lectures en parallèle, faits décisifs recoupés par l'agent principal) :

- Le code du jeu des deux côtés n'est pas fusionnable texte à texte : ici il vit dans `RacingGame.Gameplay` avec des générateurs et des assets cuits chargés par identifiant ; là-bas il est resté dans `RacingGameCasaEngine` et crée matériaux et entités en code. Six fichiers ajoutés là-bas (`RacingGamePbrMaterialMapping.cs`, `ReferenceSceneMeasure.cs`, `ReferenceViewCaptureValidator.cs`, `LegacyTrackSceneFactory.Scenery.cs`, `.RoadStructures.cs`, `.Diagnostics.cs`) complètent des classes qui ont changé de projet ici.
- Le contenu diffère : là-bas, 161 fichiers de `RacingGameCasaEngine/Content` modifiés et 7 ajoutés (56 glTF régénérés avec tangentes, normal maps converties, cube du ciel réorienté). Ici, 0 glTF avec `TANGENT`, et `--verify` du cuiseur compare les assets cuits aux glTF.
- Le format `.staticModel` du moteur ne stocke pas les tangentes (`StaticModelMesh.cs:26-27` lit des `VertexPositionNormalTexture` et vide les tangentes ; aucun champ de tangente dans `EditorAssetJsonSerializer.cs`). Sans tangentes, le moteur retire la normal map (`RenderFeatureResolver`).
- Le moteur compile un `.material` `lit-pbr` (`MaterialCompiler.cs`), avec clear coat et texture de détail.
- N'existe que là-bas : dessous de la route, sol de la ville, décor de remplissage aléatoire, tangentes de la route et des tunnels, banc de mesure contre l'original. L'ADR-0019 d'ici affirme qu'il ne manque rien face à l'original : ces trois éléments de géométrie le contredisent.
- Écarts sur les sujets faits en double, la version d'ici restant la référence (D7) : règle de début de plage des aides (`Reset`), échelle des poteaux de rail (1,125 ici, 1,35 là-bas), tailles des modèles (glTF ici, table de l'original là-bas), séquence aléatoire des palmiers (fixe par piste ici), filtre des CombiModel, ombres des colonnes. Ils seront mesurés par le banc en S1c et remis à l'auteur, pas corrigés d'office.
- Aucun projet de test du jeu, d'aucun côté.

### ✅ T3.1 — Fusion d'`origin/remaster`, code et contenu du jeu d'ici

- Objectif : joindre les deux histoires dans `chantier/rgce-pbr-handover` avec un jeu qui se construit et tourne comme avant la fusion, au moteur près. Le travail PBR de l'autre PC reste dans l'histoire et sert de source aux tranches S1c à S1h ; il n'est pas actif dans l'arbre après cette tâche.
- Fichiers :
  - **gardés dans l'état d'ici** (état de `HEAD` avant fusion, fichiers ajoutés là-bas retirés) : `RacingGame.Gameplay/`, `RacingGameCasaEngine/` (code, contenu, `.csproj`), `RacingGame.slnx`, `scripts/LegacyModelGltfConverter/`, `RacingGameOriginal/` (version de T2.2) ;
  - **repris de là-bas** : `scripts/TrackCaptureCompare/`, `docs/racinggame-pbr-preparation.md`, `docs/racinggame-3d-capture-bench-findings.md`, `docs/decisions/0015` à `0018` de là-bas, `ai-agent/HANDOVER.md`, `ai-agent/handover/`, les plans ajoutés dans `ai-agent/tasks/` ;
  - **fusionnés** : `ai-agent/README.md`, `docs/decisions/README.md` (lignes des deux côtés) ;
  - **renumérotés** : ADR locaux du jeu 0015 → 0023, 0016 → 0024, 0017 → 0025, 0018 → 0026, fichiers et renvois (index, ADR 0002, 0019 et 0023 à 0026, `ai-agent/README.md`, plans `rgce-editor-project`, `rgce-track-helpers` et celui-ci, `RacingGameCasaEngine/Assets/README.md`, `RacingGameCasaEngine.csproj`) ; les renvois vers les ADR de MGUI de même numéro ne bougent pas ;
  - **renvois vers les ADR du moteur** renumérotés par T1.1 : 0071 → 0118 et 0075 → 0122 dans les docs du jeu ;
  - `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` : `ThrowOnScriptTypeResolution` renseigné dans la politique d'exécution, propriété `required` du moteur fusionné (seule modification de code du jeu attendue, valeur de `337a43c`) ;
  - pointeur `CasaEngine` sur `e942caa54` ; ce plan.
- Étapes :
  1. `git merge --no-ff --no-commit origin/remaster`.
  2. Remettre les dossiers « gardés » dans l'état de `HEAD` (`git restore --source=HEAD --staged --worktree -- <dossier>`), puis vérifier `git diff --cached --quiet HEAD -- <dossier>` pour chacun. Exception : dans `RacingGameCasaEngine/`, les deux fichiers touchés par la renumérotation des ADR (`Assets/README.md`, `RacingGameCasaEngine.csproj`) ne diffèrent de `HEAD` que par ces numéros, et `Bootstrap/RacingGameCasaEngineGame.cs` que par la ligne `ThrowOnScriptTypeResolution`.
  3. Résoudre les deux index en gardant les lignes des deux côtés.
  4. Renuméroter les ADR locaux avec `git mv` ; corriger les renvois ligne par ligne, uniquement sur les lignes ajoutées ici depuis `d8ac303` (même méthode que T1.1) ; contrôler par `rg` qu'il ne reste aucun renvoi local vers 0015 à 0018 et que les docs de là-bas pointent toujours vers leurs propres ADR.
  5. Dans `ai-agent/README.md`, écrire sur la ligne du plan `rgce-pbr-race-world-tasks.md` (là-bas) que son code n'est pas dans l'arbre et sera porté par S1c à S1h. Ne pas modifier les docs de là-bas eux-mêmes.
  6. `git add CasaEngine` ; vérifier `git diff --cached --submodule`.
  7. Recopier `ai-agent/handover/pbr-baseline/*.sh` vers `CasaEngine/CasaEngine/artifacts/pbr-baseline/` (ignoré par git).
- Validation :
  - aucun marqueur de conflit ; l'étape 2 vérifiée pour chaque dossier « gardé » ;
  - `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` : 0 erreur ;
  - `dotnet build` de `scripts/TrackCaptureCompare`, de `scripts/RgceAssetCooker` et de `RacingGameOriginal/Desktop/RacingGame.csproj` : 0 erreur ;
  - cuiseur `--check` puis `--verify` : succès (contenu inchangé) ;
  - `--smoke-frontend` puis `--verify-legacy-import-profile` : succès, un lancement à la fois ;
  - `--capture-ui-screens` comparé à `artifacts/ui-capture-determinism/ref-a/ui` par `scripts/UiCaptureCompare`, et `--capture-track-audit` comparé à `artifacts/o7-track-helpers/after/track-audit-a` par `scripts/compare_track_audit.py` (ou, sans Python sur ce PC, par `scripts/TrackCaptureCompare` s.il accepte ces fichiers, sinon comparaison notée comme non faite) : résultats notés. Le seul changement attendu vient du moteur (`87fc5f01` → `e942caa54`). Un écart n'empêche pas le commit ; il est noté ici et remis à l'auteur. Un jeu qui ne démarre pas ou une capture qui échoue bloque la tâche.
- Arrêts : build en échec à cause d'une API du moteur qui a bougé et dont la correction ne tient pas en une adaptation minimale du point d'appel (les adaptations faites sont listées dans le commit) ; correction qui demanderait de toucher au moteur ; référence de capture introuvable.
- Retour arrière : `git merge --abort` avant le commit ; après, `git reset --hard <commit de fusion>^1` sur cette branche, c'est-à-dire le `HEAD` relevé juste avant l'étape 1 et noté sous cette tâche (aucune autre branche touchée, ce plan conservé), sur demande de l.auteur seulement.
- Commit : `merge: join origin/remaster, keeping the local game code and content`

> Validation (2026-11-16, mode AUTO choisi par l'auteur) :
> - `HEAD` relevé avant la fusion : `a53133b`.
> - Dossiers gardés : `git diff --cached --quiet HEAD` vrai pour `RacingGame.Gameplay/`, `RacingGame.slnx`, `scripts/LegacyModelGltfConverter/` et `RacingGameOriginal/` ; `RacingGameCasaEngine/` ne diffère que par `Assets/README.md` et `RacingGameCasaEngine.csproj` (numéros d'ADR) et par la ligne `ThrowOnScriptTypeResolution` de `Bootstrap/RacingGameCasaEngineGame.cs`.
> - ADR d'ici 0015 à 0018 renumérotés 0023 à 0026 avec leurs renvois ; renvois vers les ADR du moteur 0071 et 0075 passés à 0118 et 0122 ; les ADR 0015 à 0018 sont maintenant ceux de l'autre PC. Les cinq plans de l'autre PC portent dans l'index la mention que leur code n'est pas dans l'arbre.
> - Builds à 0 erreur : jeu, `scripts/TrackCaptureCompare`, `scripts/RgceAssetCooker`, `RacingGameOriginal/Desktop`.
> - Cuiseur : `--check` 0 manque, `--verify` 0 écart (56 modèles, 98 matériaux, 107 textures).
> - `--smoke-frontend` et `--verify-legacy-import-profile` : succès.
> - `--capture-ui-screens` contre `artifacts/ui-capture-determinism/ref-a/ui` : 6 états à 0,00 ; `car-selection`, `race-hud`, `pause`, `race-finished` entre 0,01 et 0,09, sous le seuil de 2,0. Captures dans `artifacts/pbr-handover/s1b/ui`.
> - `--capture-track-audit` contre `artifacts/o7-track-helpers/after/track-audit-a` : 30 vues, pire canal 0,352 (`Beginner-helper-tunnel`), la plupart sous 0,03 ; les vues sans décor (`road-only-start`, `terrain-overview`) à 0,000. La référence était identique au pixel d'un lancement à l'autre (a contre b : 0,000). Captures dans `artifacts/pbr-handover/s1b/track-audit`.
> - Constat remis à l'auteur et à S1v : depuis le moteur fusionné, les vues 3D ne sont plus identiques au pixel à la référence d'ici (écarts faibles, cause non cherchée dans cette tâche).
> - Scripts `pbr-baseline` recopiés dans `CasaEngine/CasaEngine/artifacts/pbr-baseline/`.

---

## Phase 4 — Vérification indépendante des fusions (S1v)

### ✅ T4.1 — Empreintes de référence du moteur sur ce PC

- Objectif : relever une fois, sur ce PC et sur le moteur fusionné (`e942caa54`), les douze empreintes du §6 de la note de passation. Elles dépendent du GPU : elles servent de référence d'ici, pas de comparaison avec l'autre PC.
- Sources : `CasaEngine/CasaEngine/artifacts/pbr-baseline/` (copie de `ai-agent/handover/pbr-baseline/`). `BIN` = `/d/development/repo/RacingGame/CasaEngine/CasaEngine.Demos/bin/Debug/net9.0-windows` (valeur par défaut des trois scripts, build Debug de T1.1). Sortie : `artifacts/pbr-handover/s1v/engine-baseline/` du jeu (ignoré par git).
- Lancements, un à la fois, jamais pendant un build :
  1. `capture-v2.sh <sortie>/legacy` : onze démos, deux captures chacune ;
  2. `lin-v2.sh <sortie> lin` ;
  3. `pbrdemo.sh <sortie> pbr` ;
  4. `pbrdemo.sh <sortie> pbr-legacy CASAENGINE_DEMO_COLOR_PIPELINE=Legacy` ;
  5. `pbrdemo.sh <sortie> pbr-clearcoat CASAENGINE_PBR_DEMO_CLEARCOAT=1`.
- Hors périmètre : `framestats.sh` et `framestats-t41.sh` (mesures de temps sur un build Release, pas des empreintes).
- Validation :
  - `capture-v2.sh` : pour les huit démos déterministes (`material`, `environment`, `static-model`, `static-shadow`, `render-to-texture`, `ui-overlay`, `frozen-frame`, `tile-map`), `files=yes`, `exceptions=0` et les deux empreintes égales ; pour `skinned`, `particles` et `split-screen` (non déterministes d'après la note), seulement `files=yes` et `exceptions=0` ;
  - lancements 2 à 5 : une empreinte de 16 caractères hexadécimaux et `exceptions=0` ;
  - les douze empreintes (huit, plus quatre) sont notées sous cette tâche dans un tableau de la forme du §6 de la note ;
  - une ligne qui ne remplit pas sa condition est notée telle quelle avec son journal, sans correction du moteur, et remise à T4.2 comme constat.
- Commit : `docs(ai-agent): record the engine baseline fingerprints of this machine`

> Validation (2026-11-16), moteur `e942caa54`, build Debug, sorties dans `artifacts/pbr-handover/s1v/engine-baseline/` :
> - les cinq lancements se terminent ; partout `exceptions=0` et `files=yes` ; les huit démos déterministes donnent deux empreintes égales ; `skinned`, `particles` et `split-screen` diffèrent d'une capture à l'autre, comme annoncé par la note.
>
> | Script | Empreinte sur ce PC |
> |---|---|
> | `capture-v2.sh` (`Legacy`) | material `40e2df3c20090e1f`, environment `4423dd1487a939b6`, static-model `efae412c45225453`, static-shadow `536a8183f8100d2e`, render-to-texture `e62b6b73a06f0939`, ui-overlay `08df0ac0282ae9c5`, frozen-frame `eb25f3917cfa6a0c`, tile-map `625f42300bcbd4ff` |
> | `lin-v2.sh` (`LinearHdr`) | `39f8355d905a14a8` |
> | `pbrdemo.sh` | `2d029da5659f2a7e` |
> | `pbrdemo.sh` avec `CASAENGINE_DEMO_COLOR_PIPELINE=Legacy` | `580dca11e3f9c87e` |
> | `pbrdemo.sh` avec `CASAENGINE_PBR_DEMO_CLEARCOAT=1` | `5d5850be6d2181e4` |
>
> - Seule `frozen-frame` a la même empreinte que sur l'autre PC (§6 de la note) ; les onze autres diffèrent, ce que la note annonce pour un autre GPU.

### ⏳ T4.2 — Vérification indépendante des deux fusions

- Objectif : faire réfuter ou confirmer par un `verifier` frais ce que T1.1 et T3.1 affirment. Le vérificateur ne modifie rien.
- Sources : moteur `chantier/merge-editor-project` (`e942caa54`, parents `187a420d7` et `87fc5f01b`) ; jeu `chantier/rgce-pbr-handover` (`61c465e`, parents `a53133b` et `90e3d99`) ; ce plan.
- Affirmations à vérifier :
  1. Moteur : la fusion contient `origin/main` et les 24 commits du chantier ; dans les 7 fichiers en conflit, aucun comportement d'un des deux côtés n'est perdu ; les deux adaptations (`EditorWorldOperations.IsAddableComponentType`, `LitPbrTransparentGpuTests`) sont justifiées ; les ADR 0118 à 0122 et leurs renvois sont cohérents, sans renvoi vers un mauvais ADR 0071 à 0075.
  2. Moteur : les deux solutions se construisent à 0 erreur ; la suite de tests donne 6370 succès sur 6378, et les 8 échecs sont ceux nommés en T1.1.
  3. Jeu : dans `61c465e`, les dossiers gardés sont identiques à `a53133b`, aux trois fichiers nommés près ; rien de ce que `origin/remaster` a ajouté hors de ces dossiers n'est perdu ; les ADR 0023 à 0026 et leurs renvois sont cohérents, et les docs de l'autre PC pointent toujours vers leurs ADR 0015 à 0018.
  4. Jeu : build à 0 erreur ; cuiseur `--check` et `--verify` sans écart ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès.
- Étapes :
  1. Commandes longues lancées par la session principale, en arrière-plan, sorties complètes gardées dans `artifacts/pbr-handover/s1v/` : les deux builds du moteur et `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj --blame-hang-timeout 5m`. Le vérificateur reçoit ces fichiers avec le commit sur lequel ils ont été produits.
  2. Un appel au `verifier` avec les quatre affirmations, les commits, les sorties de l'étape 1 et les commandes courtes qu'il lance lui-même (git, build du jeu, cuiseur, deux lancements du jeu, un à la fois).
  3. Verdict et constats notés sous cette tâche.
- Validation, selon le verdict :
  - `CONFIRMED` : tâche faite ;
  - `INCONCLUSIVE` : un seul nouvel appel, et seulement après avoir fourni la preuve qui manquait ; sinon S1v est mise en pause avec sa raison, ce qui bloque S1c ;
  - `REFUTED` ou constats : P0 ou P1, la suite est bloquée et l'auteur est interrogé ; P2 introduit par les fusions, correction dans les limites ci-dessous ; tout autre constat est noté et reporté avec sa raison.
- Limites des corrections : uniquement un défaut introduit par les fusions, uniquement dans les fichiers de résolution des deux fusions (ceux listés par T1.1 et T3.1), sur `chantier/merge-editor-project` ou `chantier/rgce-pbr-handover`. Une correction = un commit, suivi de la reproduction d'origine et d'un `verifier` frais sur l'affirmation touchée. Deux passes de correction au plus pour toute la tâche. Tout le reste (nouvelle fonction ou changement de conception du moteur, fichier hors de la résolution, plafond atteint) est reporté avec sa raison ou passe la tâche en ⚠️ Blocked ; il n'est pas corrigé ici.
- Hors périmètre : le banc de capture contre l'original (S1c) ; la cause des écarts faibles des vues 3D relevés en T3.1, notée en O7.
- Commit : `docs(ai-agent): record the independent verification of the merges`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

- O1 (S3) : tunnel presque noir, relever sa couleur de base ou lui donner une lumière ? Choix de l'auteur.
- O2 (S4) : voiture centrale du carrousel, quelle intensité viser ? Choix de l'auteur.
- O3 : 8 tests GPU du moteur échouent sur ce PC, sur `origin/main` comme sur la branche fusionnée (références prises sur l'autre PC). À traiter dans le moteur, hors de ce plan sauf demande.
- O4 (S1b) : au-delà des matériaux, les deux PC ont fait en double plusieurs sujets du monde de course, relevés le 2026-11-16 par les commits des deux côtés depuis `d8ac303` :

  | Sujet | Ici (`remaster` local) | Autre PC (`origin/remaster`) |
  |---|---|---|
  | Aides de piste (largeurs, route, palmiers) | `c0c3159`, `064138c`, `894ea77` (ADR-0019 local) | `c24e677` |
  | Tunnels | `12d4f5b` | `0ac894d` (dessous de route, tunnels, sol de la ville) |
  | Placement des objets | `609d4aa` (placements calculés à part) | `80c667c` (placement exact de l'original) |
  | Géométrie de la piste | composants générateurs (`8c9a1b2`), code dans `RacingGame.Gameplay` (`bdc9100`) | `LegacyTrackSceneFactory` réécrit, fichiers `Scenery` et `RoadStructures` ajoutés dans `RacingGameCasaEngine/Worlds` |
  | Matériaux | assets cuits `lit-diffuse` (`7d02ebb`, `5c30b9a`, `d8f8164`, `8e45f6f`) | `lit-pbr` en code (`d0db72f`, `09f1c54`, `d35a25a`, `b5391fe`) |
  | Textures | TGA convertis en PNG (`a0a0c97`), cubes copiés (`8a67738`) | normal maps au format standard (`b406f4f`), cube du ciel aux axes du port (`074f242`), mipmaps (`58bb871`) |
  | Tangentes | aucune (81 matériaux cuits nomment une normal map sans l'utiliser) | `8b30f98`, `a18b364` |
  | Banc de mesure | audit de piste (`d5f89df`, `3e92714`) | vues de référence et `TrackCaptureCompare` (`de010a2`, `b4642e8`, `c6a0372`) |

  Tranché par D7 : la version de ce PC sert de référence.
- O5 (S1b) : les ADR du jeu 0015 à 0018 existent des deux côtés avec des sujets différents ; les ADR locaux 0015 à 0018 sont à renuméroter (0023 à 0026), avec leurs renvois, y compris ceux du moteur s'il y en a.
- O6 (clos par T2.2, D9) : l'original de référence ne compilait pas sur ce PC. Essai du 2026-11-16 sur la copie `RacingGameOriginal/` d'`origin/remaster`, extraite hors du dépôt : `dotnet build Desktop/RacingGame.csproj` échoue avec `NU1101`, package `MonoGame.Xna.Framework.Net` introuvable sur nuget.org (`Core/Core.csproj` et `Desktop/RacingGame.csproj` ; version `1.0.0-preview.1` d'après `Documentation/LAN-Multiplayer.md`). Cinq fichiers l'utilisent, tous du multijoueur LAN (`Core/Multiplayer/*`, `LanBrowserScreen.cs`, `MultiplayerGameScreen.cs`). Deux voies : recopier le package depuis le cache NuGet de l'autre PC, ou retirer le multijoueur LAN de la copie (D8 l'autorise). Choix de l'auteur attendu.
- O7 : depuis le moteur fusionné, les vues 3D du jeu ne sont plus identiques au pixel à la référence d'ici (T3.1 : pire canal 0,352 sur 255 pour l'audit de piste, 0,09 pour les captures UI avec 3D). Cause non cherchée. Décision en mode AUTO : écart faible, hors du code du jeu (identique), non bloquant ; une nouvelle référence est prise en S1c. À confirmer par l'auteur.
