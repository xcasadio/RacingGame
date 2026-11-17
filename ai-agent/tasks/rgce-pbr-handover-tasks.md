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
| S1v | HANDOVER §7 : vérification indépendante des fusions du moteur et du jeu (`verifier` frais), captures UI et audit de piste du jeu sur le moteur fusionné. Avant toute tranche qui change le moteur, le contenu ou les matériaux | S1b | Faite le 2026-11-16, verdict CONFIRMED après une correction |
| S1c | Banc de mesure porté ici : `--capture-reference-views`, points d'export de la scène, `ReferenceSceneMeasure`, `TrackCaptureCompare`. Première mesure de la version d'ici contre l'original sur ce PC, avant PBR ; écarts des sujets faits en double remis à l'auteur | S1v | Faite le 2026-11-16, verdict CONFIRMED après une correction du doc |
| S1a | Alignement sur l'original des sujets faits en double (D11) : poteaux de rail, segments de colonne, lanternes, palmiers, panneaux, ombres portées, avec le décor de remplissage aléatoire, qui partage les mêmes tirages. Prouvé par la mesure de scène de S1c ; nouvelle référence « avant PBR » en fin de tranche | S1c | Faite le 2026-11-16, verdict CONFIRMED après une correction (rail et supports) ; un critère réduit (O9) |
| S1d | Moteur : le format `.staticModel` stocke les tangentes (branche moteur, ADR, tests, plan moteur) | S1a | Faite le 2026-11-16 (Phase 7), verdict CONFIRMED |
| S1e | Assets : convertisseur porté (tangentes dans les glTF, normal maps au format standard, cube du ciel réorienté), contenu régénéré, cuiseur avec tangentes, recuisson | S1d | Faite le 2026-11-17, verdict CONFIRMED ; les mipmaps passent en S1m |
| S1m | Textures du monde de course avec mipmaps (`58bb871`) : un réglage par asset pour une texture cuite, avec une évolution du moteur | S1e | Détaillée (Phase 9) |
| S1f | Matériaux PBR cuits (D6) : recettes par famille, cuiseur en `lit-pbr`, peinture de la voiture et plateau du carrousel, texture de détail du terrain, vérificateurs, suppression des tables de réglage et du relèvement d'ambiant du profil d'import ; ADR du jeu mis à jour (0018 de là-bas et 0026 d'ici réunis par un ADR qui les amende) | S1m | À détailler |
| S1g | Monde de course : `LinearHdr`, lumière clé, ombres, ciel en phase de scène, mode `Legacy` pour le banc, tangentes de la route, des tunnels et des maillages générés | S1f | À détailler |
| S1h | Géométrie qui n'existe que sur l'autre PC, en générateurs : dessous de la route, sol de la ville (le décor de remplissage aléatoire est passé en S1a) ; ADR-0019 corrigé (il affirme qu'il ne manque rien face à l'original) | S1f | À détailler |
| S2 | Vérification indépendante du portage PBR (S1c à S1h), banc de capture contre l'original, nouvelle référence sur ce PC | S1c, S1g, S1h | À détailler après S1 |
| S3 | Tunnel presque noir | S2 | À détailler ; choix de l'auteur attendu (O1) |
| S4 | Voiture centrale du carrousel trop blanche | S2 | À détailler ; choix de l'auteur attendu (O2) |
| S5 | Défauts connus du §4 de la note (moteur sur une branche moteur dédiée, jeu sur cette branche) | S2 | À détailler |

Les tranches S1b, S1v, S1c, S1a, S1d, S1e, S1m, S1f s'exécutent dans cet ordre, une à la fois ; S1g et S1h suivent S1f ; S2 vient après S1g et S1h. La mesure « avant PBR » de S1c est donc toujours prise avant tout changement du moteur, du contenu ou des matériaux.

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
| D10 | Moteur : `main` local avancé en fast-forward sur `chantier/merge-editor-project` (`b429b79ec`), sur autorisation de l'auteur du 2026-11-16, sans push. Les chantiers suivants du moteur partent de `main`. |
| D11 | Les sujets faits en double sont alignés sur l'original **maintenant**, avant le PBR (réponse à O8). La version d'ici reste la base (D7) : on la corrige, on ne la remplace pas. La référence « avant PBR » est reprise après l'alignement. |

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
  - (e) sujet fait en double dont les deux versions divergent : la version d'ici reste, l'écart est mesuré par le banc (S1c) et remis à l'auteur. Aucune correction d'office. Depuis D11, ces écarts sont corrigés vers l'original par la tranche S1a, et par elle seule : le code d'ici reste la base, on le corrige, on ne le remplace pas ;
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
| `c24e677` | Lecture des aides de largeur et de route | Fait en double : version d'ici (D7), mesurée en S1c, alignée sur l'original en S1a (D11) |
| `80c667c` | Placement exact des objets | Fait en double : version d'ici (D7), mesurée en S1c, alignée sur l'original en S1a (D11) |
| `0ac894d` | Dessous de la route, tunnels, sol de la ville | Tunnels : faits en double, version d'ici (D7), plages alignées sur l'original en S1a (D11). Dessous de la route et sol de la ville : S1h, portés |
| `b406f4f` | Normal maps au format standard | S1e, porté |
| `8b30f98` | Tangentes des modèles importés | S1e, porté (dépend de S1d) |
| `074f242` | Cube du ciel aux axes du port | S1e, porté |
| `4c058ad` | Couleurs importées arrondies, matériaux sans effet par défaut | S1e, porté (lecteur glTF utilisé par le cuiseur, puis recuisson) |
| `58bb871` | Textures du monde de course avec mipmaps | S1m, porté (réglage par asset, évolution du moteur) |
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

### ✅ T4.2 — Vérification indépendante des deux fusions

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

> Validation (2026-11-16) :
> - Commandes longues lancées par la session principale sur le moteur `e942caa54`, arbre propre, sorties dans `artifacts/pbr-handover/s1v/` : deux builds à 0 erreur ; tests 6378 lancés, 6370 verts, 8 en échec.
> - `verifier` frais, premier passage : **REFUTED**, neuf affirmations sur dix confirmées (1a, 1b, 1c, 2, 3a, 3b, 3c, 3d, 4), 1d réfutée.
>   - Constat F1 (P2, introduit par la fusion du moteur, documentation seule) : `ai-agent/README.md` du moteur, ligne du plan `rgce-editor-project-engine-tasks.md`, disait « ADR-0118, 0072 et 0073 » ; seul le premier numéro avait été renuméroté.
>   - Correction, passe 1 sur 2 : moteur `b429b79ec` sur `chantier/merge-editor-project`, une ligne (« ADR-0118, 0119 et 0120 »). Fichier de la résolution de T1.1.
>   - `verifier` frais sur l'affirmation 1d : **CONFIRMED**. Les 102 mentions restantes de 0071 à 0075 sont celles d'`origin/main` (97) et les cinq notes « numbered ADR-007x until » de l'index.
> - Réserves du vérificateur, notées sans suite ici :
>   - il n'a pas reproduit lui-même que les 8 tests échouent aussi sur `origin/main` seul (essai de la session principale en T1.1) ; les builds contrôlés étaient incrémentaux ;
>   - A1 (P4, code d'`origin/main`) : `EditorWorldOperations.ComponentTypes` reste calculé une seule fois, déjà noté en T1.1 ;
>   - A2 (P4, effet combiné de la fusion) : les matériaux `lit-pbr` compilés passent maintenant par `RenderFeatureResolver.AdjustToMesh`, qui retire la normal map sans tangentes. Aucun test n'échoue ; rien ne l'a exercé à l'écran. À surveiller en S1e et S1f.
> - Pointeur `CasaEngine` du jeu avancé sur `b429b79ec` (documentation seule, pas de nouveau build).

---

## Phase 5 — Banc de mesure porté ici (S1c)

Source à porter : le code de l'autre PC, lisible par `git show origin/remaster:<chemin>` (commits `b4642e8`, `c3338a3`, `62f7956`, et la partie banc de `09e4a9f`). Rien n'est repris tel quel : ce code vise des classes qui ont changé de projet et de forme ici (règle de fusion a).

État vérifié (2026-11-16) :

- L'original écrit `views.json`, `scene.json` et deux images par vue (`RacingGame.exe --capture-track-audit`, T2.2 : 30 fichiers pour 3 pistes et 5 vues).
- Ici, `RacingGameCasaEngine/Bootstrap/TrackMigrationCaptureValidator.cs` est le modèle d'un mode de capture : même cycle (`PreviewUpdate`, `StartRaceForAutomation`, `ReturnToFrontEndForAutomation`, `SetDebugCameraEnabled`, `SetCircuitOnlyViewEnabled`), et il recalcule déjà le tracé par `LegacyTrackSceneFactory.ComputeGeneratorLayout` (ligne 407).
- `RaceTrackLayout` expose `Origin`, `RoadPoints` (espace de l'original), et les placements `Scenery`, `LeftPosts`, `RightPosts`, `ColumnSegments` (espace du jeu). La conversion `RaceTrackSpace.ConvertLegacyPoint` est `(x, z, -y)`, puis on retranche `Origin` : elle s'inverse exactement.
- Les entités du décor s'appellent `Track.Scenery.<modèle>.<index>` (`LegacyTrackSceneFactory.cs`), les autres suivent les préfixes de `RaceWorldNames`.
- `scripts/TrackCaptureCompare` est dans l'arbre depuis S1b et se construit.
- Ici il n'y a pas de décor de remplissage aléatoire (S1h) : la graine de `views.json` n'a rien à piloter. Les objets de remplissage de l'original apparaîtront comme absents du port dans la mesure ; c'est attendu.

### ✅ T5.1 — Mode `--capture-reference-views`

- Objectif : le jeu rejoue les vues de `views.json` et écrit une image `<Piste>-<vue>.png` par vue.
- Fichiers : `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs` (nouveau), `RaceLaunchOptions.cs`, `RacingGameCasaEngineGame.cs`, `RacingGameCasaEngine/Program.cs` ; côté `RacingGame.Gameplay`, au plus une méthode qui convertit un point de l'original vers le monde de course d'une piste (même repère et même origine que la scène).
- Étapes :
  1. Options `--capture-reference-views <views.json>` et `--reference-capture-out <dossier>` ; le mode entre dans `IsAutomation` et dans le contrôle « un seul mode d'automation ».
  2. Validateur porté sur le modèle de `TrackMigrationCaptureValidator` : taille et projection lues dans `views.json` et appliquées à la capture seulement, rien d'enregistré dans les réglages ; première voiture, première couleur ; HUD retiré ; caméras de poursuite et de debug arrêtées ; lignes de debug du parcours retirées ; voiture cachée dans les vues où l'original ne la dessine pas ; capture par le même mécanisme de fin de `Draw` que les autres modes de capture d'ici.
  3. Journal : pour chaque vue, la distance entre la voiture de l'original convertie et l'ancre la plus proche (départ ou point de contrôle) ; pour chaque image, la ligne `[CaptureBench] view <Piste>-<vue> pipeline <legacy|linear> fxaaPasses <n>`.
- Validation :
  - build du jeu à 0 erreur ;
  - avec la référence de T5.3 étape 1 : sortie 0, 15 images (3 pistes, 5 vues), 15 lignes d'alignement et 15 lignes `[CaptureBench]` ;
  - alignement : pour chacune des 15 vues, la distance entre la voiture de l'original convertie et l'ancre la plus proche reste sous 5 m (l'autre PC relevait 0,5 à 3,7 m, `docs/racinggame-3d-capture-bench-findings.md`) ; les 15 distances sont citées dans la note de validation ; la conversion tient compte de l'origine du tracé et de la matrice de monde du générateur de route, comme `TrackMigrationCaptureValidator.cs:414-415` ;
  - les autres modes ne bougent pas : `--smoke-frontend` en succès ; `--capture-ui-screens` contre `artifacts/pbr-handover/s1b/ui`, les 10 états sous le seuil de `UiCaptureCompare`.
- Arrêts : besoin de modifier le moteur ; besoin de changer le tracé, les placements ou les matériaux du jeu ; une distance d'alignement au-dessus de 5 m (la tâche passe en ⚠️ Blocked avec la question, l'écart n'est pas enregistré comme résultat).
- Commit : `feat(racing-casa): replay the original's reference views`

> Validation (2026-11-16), commit `0e3485b`, exécutant `executor`, relu par la session principale :
> - Build à 0 erreur. Rejeu sur `artifacts/pbr-handover/s1c/ref/views.json` : sortie 0, 15 images 1280×720, 15 lignes d'alignement, 15 lignes `[CaptureBench]` (`pipeline legacy fxaaPasses 0`).
> - Alignement, en mètres, toutes sous 5 m : Beginner 0,00 / 1,07 / 0,00 / 1,57 / 0,00 ; Advanced 0,00 / 1,14 / 0,00 / 1,72 / 0,00 ; Expert 0,00 / 0,53 / 3,68 / 1,83 / 0,00 (ordre : start, sector-20, sector-50, sector-80, overview).
> - `--smoke-frontend` en succès ; `--capture-ui-screens` contre `artifacts/pbr-handover/s1b/ui` : les 10 états à 0,00.
> - Aucun changement dans `RacingGame.Gameplay`. La conversion est dans `Bootstrap/ReferenceTrackSpace.cs` : `ConvertLegacyPoint`, origine du tracé, puis matrice de monde du générateur de route (identité en pratique).
> - Écarts au code de l'autre PC : capture par `RequestScreenshotAtEndOfDraw`, une update par draw ; délai maximal 240 s ; pas de graine de décor, puisqu'il n'y a pas de remplissage aléatoire ici.

### ✅ T5.2 — Mesure de la scène contre `scene.json`

- Objectif : pour chaque piste, comparer les points de route et les modèles placés du jeu à ceux de l'original, dans le repère de l'original, et écrire `scene-measure-<Piste>.txt`.
- Fichiers : `RacingGameCasaEngine/Bootstrap/ReferenceSceneMeasure.cs` (nouveau, porté), `ReferenceViewCaptureValidator.cs` ; côté `RacingGame.Gameplay`, au plus un type de relevé (points de route, objets placés avec leur transformation dans le repère de l'original et leurs indicateurs d'ombre).
- Étapes :
  1. Relevé du jeu construit depuis le tracé de la course (points de route, `Scenery`, `LeftPosts`, `RightPosts`, `ColumnSegments`), transformations ramenées dans le repère de l'original par l'inverse exact de la conversion ; indicateurs d'ombre lus sur les composants des entités du monde, pas sur la règle qui les pose.
  2. Mesure portée sans changer ses tolérances ni son format de rapport ; résumé d'une ligne dans le journal.
- Validation :
  - build à 0 erreur ;
  - trois fichiers `scene-measure-<Piste>.txt`, chacun avec le nombre de points de route de l'original et du jeu et le décompte des objets appariés, manquants et en trop ;
  - contrôle de l'inverse : un point de l'original converti puis ramené redonne le point de départ (à l'arrondi flottant près), écrit une fois dans le journal ou vérifié par un calcul noté sous la tâche.
- Arrêts : comme T5.1.
- Commit : `feat(racing-casa): measure the scene against the original's points and objects`

> Validation (2026-11-16), commit `40fb7c1`, exécutant `executor`, relu par la session principale :
> - Build à 0 erreur. Rejeu : sortie 0, 15 images, trois `scene-measure-<Piste>.txt`.
> - Résumés : Beginner, points 1332/1332 à 0,000 m, objets 1147/821, 712 appariés, 435 manquants, 109 en trop ; Advanced, points 2172/2172, objets 1693/1226, 1007 appariés, 686 manquants, 219 en trop ; Expert, points 4466/4466, objets 3404/2595, 2140 appariés, 1264 manquants, 455 en trop.
> - Inverse : aller-retour d'un point, erreur maximale 0,000061 m ; transformations placées inchangées (lignes `round trip` du journal).
> - `ReferenceSceneMeasure.cs` porté sans changer tolérances ni format. Relevé dans `Bootstrap/ReferenceSceneSnapshot.cs`. Seul ajout dans `RacingGame.Gameplay` : `LegacyTrackSceneFactory.ComputeRaceLayout`, extrait de `Create` sans changement de comportement.
> - Limite : la mesure part des transformations du tracé, pas de la matrice de monde de chaque composant ; un générateur non placé à l'identité ne s'y verrait pas (le journal note que les matrices sont à l'identité). Le cas « pas de `scene.json` » n'a pas été lancé.

### ✅ T5.3 — Première mesure de la version d'ici, avant PBR

- Objectif : la référence de ce PC, prise avant tout changement de moteur, de contenu ou de matériaux, et les écarts des sujets faits en double remis à l'auteur.
- Fichiers : `docs/racinggame-3d-capture-bench-local-baseline.md` (nouveau, en anglais), `docs/README.md` s'il indexe les docs voisins, ce plan. Sorties dans `artifacts/pbr-handover/s1c/` (ignoré par git).
- Étapes, un lancement à la fois, jamais pendant un build :
  1. Original : `RacingGame.exe --capture-track-audit --seed 1 --size 1280x720 --out <s1c>/ref`.
  2. Jeu : `RacingGameCasaEngine.exe --capture-reference-views <s1c>/ref/views.json --reference-capture-out <s1c>/cand-a`, puis une seconde fois vers `cand-b`.
  3. `scripts/TrackCaptureCompare` selon son README : variante `full`, variante `nopost`, puis avec les sondes (`--probes scripts/TrackCaptureCompare/probes.json --no-terrain-detail-correction`).
  4. Répétabilité : `cand-a` contre `cand-b`, image par image (empreintes, ou différence moyenne si elles diffèrent).
  5. Doc : par piste et par vue, les chiffres du rapport ; le résumé des trois `scene-measure` ; un tableau des écarts par sujet fait en double (aides de piste, tunnels, placement, poteaux, tailles des modèles, palmiers) avec ce que la mesure montre pour chacun, et ce qui manque ici parce que cela n'existe que là-bas (dessous de la route, sol de la ville, remplissage).
- Validation : les quatre lancements se terminent en succès ; les rapports existent ; le doc cite pour chaque chiffre le fichier de sortie d'où il vient. Les écarts à l'original ne sont pas des échecs de cette tâche : ce sont ses résultats.
- Commit : `docs(racing-casa): record the capture bench baseline of the local version`

> Validation (2026-11-16) : les quatre lancements en succès (original, deux rejeux, comparaisons `nopost`, `full`, sondes, répétabilité). Résultats dans `docs/racinggame-3d-capture-bench-local-baseline.md`, sorties dans `artifacts/pbr-handover/s1c/`.
> - Répétabilité : 14 fichiers identiques sur 15 ; `Expert-start.png` diffère par son empreinte avec une différence moyenne de 0,00.
> - Images contre l'original (`nopost`) : différence moyenne par canal de 15 à 56 sur 255 ; pire vue `Beginner-sector-20` (sol de la ville noir).
> - Sondes : rapport ombre sur lumière de 0,23 à 0,28 dans l'original, de 0,67 à 1,13 ici ; aucun des quatre rapports dans la tolérance.
> - Points de route identiques à l'original sur les trois pistes.
> - Écarts des sujets faits en double, remis à l'auteur (O8) : poteaux de rail à l'échelle 1,125 au lieu de 1,35 ; environ deux fois trop de segments de colonne ; un tiers de lanternes en trop ; palmiers bien moins nombreux et peu appariés ; panneaux peu appariés ; presque tous les objets projettent une ombre alors que ceux de l'original ne le font pas.

### ✅ T5.4 — Vérification indépendante de S1c

- Objectif : un `verifier` frais confirme ou réfute : le mode rejoue bien les vues de l'original (taille, projection, positions : les 15 distances d'alignement sous 5 m, critère de T5.1) ; la mesure compare dans le bon repère ; les autres modes d'automation ne régressent pas ; les chiffres du doc sont ceux des sorties.
- Étapes : commandes longues éventuelles lancées par la session principale ; un appel au `verifier` ; verdict noté ici. `INCONCLUSIVE` : un seul nouvel appel après avoir fourni la preuve manquante, sinon S1c en pause. Constats : P0 ou P1, arrêt et question à l'auteur ; P2 introduit par S1c, corrigé dans les fichiers de T5.1 à T5.3 (un commit par correction, deux passes au plus, `verifier` frais sur l'affirmation touchée) ; le reste noté et reporté.
- Validation : `CONFIRMED`, ou constats traités comme ci-dessus.
- Commit : `docs(ai-agent): record the verification of the capture bench port`

> Validation (2026-11-16) :
> - `verifier` frais, premier passage : **REFUTED**. Affirmations 1 (rejeu fidèle), 2 (mesure dans le bon repère) et 3 (pas de régression) confirmées par ses propres lancements : 15 images 1280×720, 15 distances sous 5 m, réglages utilisateur inchangés (empreintes avant et après), `--smoke-frontend` en succès, cuiseur à 0 écart. Affirmation 4 (chiffres du doc) réfutée.
>   - F1 (P2, introduit par `389a7e5`) : le tableau des sondes donnait les totaux jugés comme échecs (34 et 33 au lieu de 24 et 12).
>   - F2 (P2, introduit par `389a7e5`) : une cellule fausse, `Expert-sector-80` `nopost`, 44,8 % au lieu de 40,8 %.
>   - F3 (P3) et F4 (P4) : formulations plus larges que la mesure (lanternes doubles toutes manquantes, un poteau en trop sur Advanced, segments de colonne 2,2 à 3,2 fois trop nombreux, `Casino01` absent sur Beginner).
>   - Correction, passe 1 sur 2 : `75651e5`, document seul ; F3 et F4 corrigés au passage ; ajout de la phrase sur le troisième lancement, qui diffère du premier par l'empreinte de deux fichiers.
> - `verifier` frais sur l'affirmation 4 : **CONFIRMED**, les 60 cellules du tableau des images et tous les triplets par modèle contrôlés.
> - Remarques P3 et P4 du second passage, notées sans suite (candidat confirmé) : « two to three times » alors qu'Advanced est à 3,2 ; le sens des écarts d'ombre n'est prouvé que sur les 12 exemples listés par piste ; l'échelle 1,125 des poteaux d'ici est déduite de l'écart 0,167, elle n'est pas écrite dans une sortie.
> - Répétabilité : pas totale au pixel (un ou deux fichiers d'empreinte différente selon le lancement, différence moyenne 0,00 quand elle a été mesurée).

---

## Phase 6 — Alignement sur l'original des sujets faits en double (S1a)

Décision D11. La base reste le code d'ici (`RaceTrackLayout`, générateurs, assets cuits) : on le corrige jusqu'à ce que la mesure de scène de S1c ne laisse aucun objet sans correspondance. Rien n'est repris tel quel de l'autre PC ; son code et son plan servent d'explication des causes.

État vérifié (2026-11-16) :

- Mesure de S1c, graine 1 (`artifacts/pbr-handover/s1c/cand-a/scene-measure-*.txt`) : points de route identiques ; objets 1147/821, 1693/1226, 3404/2595 (original/jeu) ; 712, 1007 et 2140 appariés.
- L'autre PC est parti d'un état comparable et a atteint 1147/1147, 1693/1693 et 3404/3404, 0 objet sans correspondance, distance et échelle à 0,000, rotation sous 0,04° (`ai-agent/tasks/rgce-3d-geometry-fidelity-tasks.md`, T4). Causes qu'il a relevées dans le code de l'original :
  - supports de rail et segments de colonne ne passent pas par le chemin d'ajout de l'original (échelle 1,2, plaquage au terrain, règle de doublon, ordre) ;
  - les décalages utilisent le vecteur droite normalisé, l'original le vecteur brut ;
  - l'angle de route est borné ici, pas dans l'original, ce qui change un tirage ;
  - le décor de remplissage aléatoire de l'original n'existe pas ici, et il partage le générateur aléatoire des palmiers, banderoles et panneaux (`RacingGameOriginal/Core/Tracks/Track.cs`, lignes 780 à 1147 d'après ce plan) ;
  - la règle de doublon de l'original utilise les tailles de modèle de l'original ; les tailles déduites des glTF diffèrent (table `LegacyModelSizes` de l'autre PC, relevée sur les `.xnb`) ;
  - les plages d'aides (tunnels, palmiers, lanternes) commencent sur tout repère dans l'original, `Reset` compris ; ici, pas sur `Reset`.
- Ici, les tirages viennent de deux suites fixées par piste (`RaceTrackLayout.cs`, graines `<piste>|…Palms` et `…Helpers`), décision de l'ADR-0019 pour des audits répétables.
- Ici, presque tous les objets placés projettent et reçoivent une ombre ; dans l'original, la plupart ne le font pas (S1c : 620 sur 712, 864 sur 1007, 1771 sur 2140 écarts).
- Conséquence sur l'enveloppe : le décor de remplissage aléatoire, prévu en S1h, entre dans cette tranche, parce qu'il consomme les mêmes tirages. S1h garde le dessous de la route et le sol de la ville.

Retour arrière de S1a : relever `git rev-parse HEAD` avant T6.1 et le noter sous T6.1 ; annuler les commits de S1a par `git revert` ou `git reset --hard <ce commit>` sur `chantier/rgce-pbr-handover` seulement ; `artifacts/pbr-handover/s1c/` reste la référence d'avant. Sur demande de l'auteur seulement.

Budget de S1a : pour T6.1, au plus six tours « correction puis mesure » ; pour T6.2, au plus trois. Chaque tour doit faire baisser le nombre d'objets sans correspondance ou d'écarts d'ombre. Au-delà, ou après un tour sans progrès, la tâche passe en ⚠️ Blocked avec le reste chiffré par modèle, et l'auteur est interrogé.

### ✅ T6.1 — Placement des objets comme l'original

- Objectif : avec la graine du banc, chaque objet de l'original a son objet dans le jeu, à la même place, orientation et échelle, et le jeu n'en a aucun en trop.
- Fichiers : `RacingGame.Gameplay/Track/RaceTrackLayout.cs`, `RaceTrackLayoutTypes.cs`, `LegacyTrackSceneFactory*.cs`, `LegacyTrackFileReader.cs` si la lecture est en cause ; `RacingGame.Gameplay/Components/Race*Generator*.cs` seulement pour suivre le tracé ; `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs` et `ReferenceSceneSnapshot.cs` pour la graine et le relevé.
- Règles :
  1. La source est le code de l'original (`RacingGameOriginal/Core/Tracks/Track.cs`, `Core/Landscapes/Landscape.cs`). Chaque règle portée cite son fichier et ses lignes en commentaire. Les effets de bord sont gardés (vecteur droite brut, angle non borné, ordre des tirages).
  2. Graine : le tracé reçoit une graine entière. Le mode `--capture-reference-views` passe celle de `views.json`. Partout ailleurs, la graine reste fixe par piste, dérivée du nom de la piste comme aujourd'hui : les autres modes d'automation et le jeu gardent des lancements répétables (ADR-0019). Pas de graine horaire.
  3. Un seul générateur aléatoire, tiré dans l'ordre de l'original.
  4. Tailles de la règle de doublon : celles de l'original, dans une table du jeu avec sa source. Les tailles des modèles cuits ne changent pas.
  5. Les supports de rail et les segments de colonne restent des enfants des générateurs. Les générateurs calculent leur liste par le même chemin d'ajout que la course, avec les objets neutres et combinés du fichier de piste et la table des tailles de la règle 4 (aujourd'hui `ComputeGeneratorLayout` les ignore : `includeScenery: false`, tailles nulles). Ils n'ont pas besoin de la graine : dans l'original, ces objets sont ajoutés avant tout tirage.
  5 bis. La mesure observe ce qui est généré : `ReferenceSceneSnapshot` lit la transformation de chaque enfant `Post.<n>` et `Segment.<n>` sur l'entité elle-même, et compte comme objet en trop tout enfant de générateur sans placement dans le tracé.
  6. Plages d'aides comme l'original, `Reset` compris. Le nombre de tunnels par piste est comparé à ce que donne l'algorithme de l'original (l'autre PC a relevé 1, 2 et 10) et noté.
  7. Matériaux, lumière, ciel, dessous de la route, sol de la ville : non touchés.
- Validation, sur `--capture-reference-views artifacts/pbr-handover/s1c/ref/views.json` :
  - points de route inchangés (1332, 2172, 4466, écart 0,000 m) ;
  - objets : 1147/1147, 1693/1693 et 3404/3404, autant d'appariés, 0 manquant, 0 en trop ; pour chaque modèle, distance maximale sous 0,01 m, écart de rotation sous 0,1°, écart d'échelle à 0,000 ;
  - les 15 distances d'alignement inchangées ;
  - build à 0 erreur ; cuiseur `--check` et `--verify` sans écart ; `--smoke-frontend` en succès ;
  - supports et segments : pour les trois pistes, le nombre d'enfants de chaque générateur égale celui du tracé, et leurs transformations lues sur les entités sont celles que la mesure compare (une ligne de journal par piste le dit) ;
  - graine fixe par piste : hors banc, le jeu journalise par piste une empreinte de la liste des placements (modèle et transformation) ; deux lancements de `--capture-track-audit` donnent les mêmes empreintes. Les images de ces deux lancements sont comparées par `scripts/compare_track_audit.py` avec un seuil de 0,01 par canal, à titre de contrôle secondaire.
- Arrêts : besoin de modifier le moteur ; un reste d'objets non appariés dont la cause n'est pas trouvée dans le code de l'original (la tâche passe en ⚠️ Blocked avec le reste chiffré par modèle ; on ne touche ni aux tolérances ni à la mesure pour le faire disparaître) ; une règle de l'original qui demanderait de changer un asset cuit.
- Commits : un par cause distincte, chacun compilable, par exemple `fix(racing-casa): place the rail posts and column segments as the original does`, `feat(racing-casa): place the random scenery in the original's draw order`. La validation complète porte sur le dernier.

> Validation (2026-11-16), `HEAD` avant la tranche `684c93c`, exécutant `executor`, mesure relancée par la session principale (`artifacts/pbr-handover/s1a/cand-a`) :
> - Commits : `60c06f8` (chemin d'ajout de l'original, vecteur droite brut, tailles de l'original, un seul générateur, remplissage aléatoire, plages d'aides, graine, empreinte des placements), `cd60b7d` (échelle gardée pour les placements cisaillés), `b1e0dec` (règle 5 bis : la mesure lit les enfants générés).
> - Objets : 1147/1147, 1693/1693, 3404/3404, tous appariés, 0 manquant, 0 en trop ; distance et écart d'échelle à 0,000 ; points de route inchangés ; les 15 distances d'alignement inchangées.
> - Générateurs : nombre d'enfants égal à celui du tracé sur les trois pistes (Beginner 224, 231 et 43 ; Advanced 368, 374 et 47 ; Expert 765, 764 et 130), transformations lues sur les entités.
> - Graine fixe par piste : deux lancements de `--capture-track-audit` journalisent les mêmes empreintes (`6EBF84F81D01AC88`, `FD0564E1183D98FE`, `29A2403A3F0CDCFA`) ; leurs 30 images sous le seuil de 0,01.
> - Build à 0 erreur ; cuiseur `--check` et `--verify` sans écart ; `--smoke-frontend` en succès. Tunnels : 1, 2 et 10 plages ; l'export de l'original ne contient pas les tunnels, le compte vient de l'algorithme porté.
> - Trois tours sur six.
> - **Critère non tenu, regradé par la session principale (mode AUTO) :** l'écart de rotation des supports de rail, lu sur les entités, est de 1,08°, 6,81° et 11,91° (critère : sous 0,1°). Cause : la matrice de l'original est cisaillée en pente, et une entité ne porte qu'une échelle, une rotation et une translation. Tous les autres modèles sont sous 0,04° dans la mesure, qui lit le décor dans le tracé ; lanternes, banderoles, panneaux et feu de départ portent le même cisaillement dans leurs entités. Tenir le critère demande une hiérarchie de pivots par objet ou une transformation matricielle dans le moteur, ce qui est un arrêt de cette tâche. Décision : approximation acceptée, position et échelle exactes, limite écrite dans l'ADR-0027 et remise à l'auteur (O9). L'affirmation de la tranche est réduite d'autant.
> - Écarts au plan : découpage en commits différent des exemples ; `TryDecomposePlacement` non prévu, il change aussi la décomposition des entités du décor ; table des tailles transcrite de celle de l'autre PC, non relue sur les `.xnb`.
> - Non traité, noté : `scripts/TrackPlacementExporter` réimplémente l'ancien placement ; le maillage des rails gardait le vecteur droite normalisé et les supports le vecteur brut ; corrigé en T6.4 (écart réel jusqu'à 0,6 m).

### ✅ T6.2 — Ombres portées et reçues comme l'original

- Objectif : chaque objet placé projette et reçoit une ombre quand, et seulement quand, celui de l'original le fait.
- Fichiers : `RacingGame.Gameplay/Track/LegacyTrackSceneFactory*.cs`, `RaceTrackLayout*.cs`, `Components/RaceGeneratedModelComponent.cs` ou les générateurs, selon l'endroit où les indicateurs sont posés.
- Règles : la règle vient du code de l'original, citée en commentaire ; elle est appliquée aux composants des entités ; les maillages générés (route, terrain, rails, colonnes) ne sont pas touchés.
- Validation : mesure relancée, 0 écart d'ombre projetée et 0 écart d'ombre reçue sur les objets appariés des trois pistes ; le reste de la validation de T6.1 inchangé.
- Arrêts : la règle de l'original dépend d'un état que le moteur ne sait pas exprimer (tâche ⚠️ Blocked, sans contournement).
- Commit : `fix(racing-casa): cast and receive object shadows as the original does`

> Validation (2026-11-16), commit `3035053`, mesure relancée par la session principale : 0 écart d'ombre projetée et 0 écart d'ombre reçue sur 1147, 1693 et 3404 objets appariés. Règle de l'original citée dans le code (`Landscape.cs:56`, `159-162`, `409-414`, `1314-1321` ; `Model.cs:212`, `877`). Un tour sur trois.

### ✅ T6.3 — Références reprises et décisions écrites

- Objectif : la référence « avant PBR » est celle de la version alignée, et les décisions sont écrites.
- Fichiers : `docs/racinggame-3d-capture-bench-local-baseline.md` (section ajoutée « After the alignment », les chiffres d'avant gardés), un ADR du jeu (skill `adr`, numéro libre suivant) qui amende l'ADR-0019 : ordre de tirage et chemin d'ajout de l'original, graine, table des tailles, ombres ; `docs/decisions/README.md` ; l'ADR-0019 marqué amendé ; ce plan.
- Étapes : banc complet relancé (deux rejeux, comparaisons `nopost`, `full`, sondes, répétabilité) vers `artifacts/pbr-handover/s1a/` ; nouvelles références d'ici notées : `--capture-track-audit` et `--capture-ui-screens` dans `artifacts/pbr-handover/s1a/` ; chaque chiffre du doc cité avec son fichier de sortie, tableaux recopiés par script, pas à la main.
- Validation : les lancements en succès ; le doc et l'ADR relus contre les sorties.
- Commit : `docs(racing-casa): record the alignment on the original's object placement`

> Validation (2026-11-16) : banc complet relancé vers `artifacts/pbr-handover/s1a/` (deux rejeux, `nopost`, `full`, sondes, répétabilité), tous en succès. Section « After the alignment » du doc générée par script depuis les sorties. ADR-0027 écrit en `Proposed` ; ADR-0019 marqué amendé.
> - Contours retrouvés (`nopost`) en hausse sur les 15 vues, par exemple `Expert-start` de 39,8 % à 56,4 %.
> - Sondes de lumière inchangées (le rapport ombre sur lumière relève de S1g).
> - Répétabilité : 12 fichiers identiques sur 15, les trois autres à une différence moyenne de 0,00.
> - Nouvelles références d'ici : audit de piste `artifacts/pbr-handover/s1a/audit-1`, captures UI `artifacts/pbr-handover/s1a/ui`. Contre les captures UI d'avant : 7 états à 0,00 ; `race-hud` 1,89, `pause` 0,52 et `race-finished` 1,20 au pire canal, parce que le décor a changé.

### ✅ T6.4 — Vérification indépendante de S1a

- Objectif : un `verifier` frais confirme ou réfute, par ses propres lancements : les décomptes d'objets et d'ombres de T6.1 et T6.2 ; chaque règle portée correspond au code de l'original cité ; la graine est celle de `views.json` sous le banc et fixe par piste ailleurs ; pas de régression des autres modes ; les chiffres du doc sont ceux des sorties.
- Étapes et verdicts : comme T5.4 (un appel ; `INCONCLUSIVE`, un seul nouvel appel après preuve fournie ; P0 ou P1, arrêt et question ; P2 introduit par S1a, corrigé dans les fichiers de T6.1 à T6.3, deux passes au plus, `verifier` frais sur l'affirmation touchée ; le reste noté).
- Commit : `docs(ai-agent): record the verification of the alignment on the original`

> Validation (2026-11-16) :
> - `verifier` frais, premier passage : **REFUTED**. Affirmations 1 à 5 confirmées par ses propres lancements (décomptes, mesure honnête, neuf règles relues contre l'original, graine, pas de régression). Il a recalculé l'inclinaison des supports depuis les matrices de l'original et retrouve 1,08°, 6,81° et 11,91°. Affirmation 6 réfutée sur une phrase de l'ADR-0027.
>   - F1 (P2, introduit par S1a) : l'ADR disait que supports et rail pouvaient s'écarter de « quelques centimètres » ; l'écart atteignait environ 0,3 m et 0,6 m sur les sections raides, parce que les supports étaient passés au vecteur droite brut et que le maillage du rail était resté au vecteur normalisé.
>   - Correction, passe 1 sur 2 : `4d34441`, le maillage du rail part des points de rail de ses supports, comme l'original (`GuardRail.cs:169-199`) ; `b57eb62`, ADR, doc et plan. Mesure, test du front-end et empreintes de placement inchangés.
>   - Remarques P3 et P4 du premier passage, corrigées au passage dans les textes : le cisaillement touche aussi lanternes, banderoles, panneaux et feu de départ dans leurs entités ; citation `TrackLine.cs:490` ; l'original ne réensemence par piste que dans sa capture de référence.
> - `verifier` frais sur la correction : **CONFIRMED** (rail et supports sur les mêmes points ; mesure inchangée, fichiers de mesure identiques à l'octet ; chiffres de l'ADR recalculés).
> - Remarques P3 et P4 du second passage, notées sans suite (candidat confirmé) :
>   - P3 : dans le contexte de l'ADR-0027, « The game seeds it once, from the clock » désigne le jeu d'origine, alors que « the game » désigne le port partout ailleurs dans la liste ; la section Decision est correcte ;
>   - P4 : les écarts rail contre bord de route (0,008, 0,298 et 0,627 m) négligent le retrait de 0,5 unité, lui aussi mis à l'échelle ; avec ce terme : 0,008, 0,284 et 0,584 m, mêmes décomptes ;
>   - P4 : les images de la section du doc datent d'avant la correction du rail ; un nouveau lancement ne diffère que par les trois fichiers déjà connus pour varier.

---

## Phase 7 — Tangentes dans le format `.staticModel` du moteur (S1d)

Tranche menée dans le dépôt du moteur, avec son propre plan : `CasaEngine/ai-agent/tasks/static-model-tangents-tasks.md` (relu READY au second passage). Branche moteur `chantier/static-model-tangents`, créée depuis `main` (D10).

### ✅ T7.1 — Format, tests, décision

> Validation (2026-11-16) :
> - Moteur `76d87d455` : un maillage de `.staticModel` peut porter un tableau `tangents` optionnel, une entrée `{x, y, z, w}` par sommet. Le chargeur le lit, le sérialiseur de l'éditeur l'écrit pour un maillage qui en a ; un fichier sans ce tableau se charge comme avant ; un nombre d'entrées incohérent est signalé avec le nom du maillage et le maillage se charge sans tangentes. Moteur `61b4e6fae` : ADR-0123 du moteur en `Proposed`, doc du format.
> - Les deux solutions à 0 erreur. Tests du moteur : 6382 lancés, 6374 verts, les 8 échecs GPU connus et eux seuls (4 nouveaux tests).
> - `verifier` frais : **CONFIRMED** sur les six affirmations. Son essai sur 20 000 sommets aux valeurs aléatoires donne 0 écart au bit près après écriture, texte et relecture ; un glTF avec `TANGENT` garde ses tangentes jusqu'à l'asset rechargé ; aucun autre chemin du moteur ne perd les tangentes.
> - Remarques P4, notées sans suite : une entrée de `tangents` à laquelle il manque une composante lève une exception sans contexte, comme le font déjà les sommets ; une clé `tangents` qui n'est pas un tableau est traitée comme absente, sans message.
> - Jeu : pointeur `CasaEngine` avancé sur `61b4e6fae` ; build à 0 erreur, cuiseur `--verify` à 0 écart, `--smoke-frontend` en succès. Les modèles du jeu n'ont pas encore de tangentes : c'est S1e.
> - Reste à l'auteur : valider l'ADR-0123, décider de la remontée de `chantier/static-model-tangents` dans `main` et du push.

---

## Phase 8 — Assets : tangentes, normal maps, cube du ciel (S1e)

Source à porter : le convertisseur de l'autre PC (`git show origin/remaster:scripts/LegacyModelGltfConverter/…`, commits `8b30f98`, `b406f4f`, `074f242`) et sa lecture des couleurs (`4c058ad`). Le contenu converti de l'autre PC sert d'oracle : le même convertisseur, sur les mêmes sources, doit redonner les mêmes fichiers.

État vérifié (2026-11-16) :

- Convertisseur d'ici (`scripts/LegacyModelGltfConverter/`) : modes `<x> <gltf> <png> [--skip]`, `--textures`, `--verify-textures`. Celui de l'autre PC a en plus `--normal-map <tga>` (canaux réécrits : R = V d'origine, V = alpha d'origine, B = B, A = 255), `--sky-cube <dds>` (`SkyCubeConverter.cs`, miroir en Z par permutation de blocs DXT1) et les tangentes (`AddTangents`, `ComputeTangents`, pour les primitives dont le matériau a une normal map) ; il n'a pas `--textures` ni `--verify-textures`. Les deux `.csproj` sont identiques.
- Sources d'origine présentes sur ce PC : `RacingGame/Content/Models` (56 `.X` et `Car.x`), `RacingGame/Content/Textures` (129 `.tga`, `SkyCubeMap.dds`).
- Contenu d'ici : 56 glTF sans `TANGENT` ; 55 normal maps PNG, toutes différentes de celles d'`origin/remaster` ; `SkyCubeMap.dds` identique à la source d'origine, donc non converti.
- Le cuiseur lit les glTF par le lecteur du moteur, qui lit `TANGENT`, et écrit par le sérialiseur du moteur, qui écrit `tangents` depuis S1d. `Verifier.cs:167` compare `HasTangents`, pas les valeurs des tangentes.
- `LegacyGltfModelReader.cs:176` tronque les couleurs ; l'autre PC les arrondit et donne aux matériaux sans effet d'origine un ambiant de 40 et un diffus de 210.
- 81 matériaux cuits nomment une normal map. Dès que leurs modèles auront des tangentes, les matériaux `lit-diffuse` dessineront avec : le rendu change dans cette tranche, avant le PBR.
- Le cube du ciel est lu par chemin au lancement (`RacingGameCasaEngineGame.cs`), aucun contrôle d'empreinte ne porte sur lui.
- Textures avec mipmaps (`58bb871`) : le moteur n'a qu'un réglage en code (`Texture2DLoader.GenerateMipmaps`), aucun champ par asset pour une texture cuite. Ce sujet sort de cette tranche : tranche S1m, à détailler, avec une évolution du moteur.

Retour arrière de S1e : relever `git rev-parse HEAD` avant T8.1 et le noter sous T8.1 ; annuler les commits de S1e sur `chantier/rgce-pbr-handover` seulement ; `artifacts/pbr-handover/s1a/` reste la référence d'avant. Sur demande de l'auteur seulement.

Budget de S1e : T8.1, au plus quatre tours « correction puis comparaison à l'oracle » ; T8.2, au plus trois tours de recuisson. Un tour sans progrès passe la tâche en ⚠️ Blocked avec l'écart chiffré.

### ✅ T8.1 — Convertisseur porté

- Objectif : le convertisseur d'ici sait écrire les tangentes, convertir les normal maps et le cube du ciel, en gardant ses modes `--textures` et `--verify-textures`.
- Fichiers : `scripts/LegacyModelGltfConverter/Program.cs`, `LegacyTextureConverter.cs`, `SkyCubeConverter.cs` (nouveau), `README.md`.
- Règles :
  1. Mêmes algorithmes que l'autre PC : tangentes par triangle, orthogonalisées contre la normale, `w = +1`, seulement pour les primitives à normal map ; canaux des normal maps ; permutation des blocs du cube. Une texture utilisée à la fois comme couleur et comme normal map est refusée. Aucune dépendance ajoutée.
  2. Le mode modèles reçoit `--normal-map <tga>` (répétable) et `--sky-cube <dds>`, comme celui de l'autre PC : `--normal-map` convertit les normal maps qu'aucun modèle ne référence, celles des maillages que le jeu génère.
  3. `--textures` reste une copie de couleur. Un nom suivi de `:normal` est converti comme normal map. Le README n'y cite plus `RoadNormal` sans ce suffixe.
  4. `--verify-textures` compare chaque paire TGA et PNG d'abord comme copie de couleur, puis, si elle diffère, comme normal map convertie ; il affiche les deux listes et leur nombre, et ne sort en erreur que pour une paire qui ne correspond à aucune des deux.
- Commande de l'oracle (celle du README de l'autre PC, `scripts/LegacyModelGltfConverter/README.md:18` dans `origin/remaster`), avec deux dossiers de sortie frères, parce que les glTF nomment leurs images par chemin relatif (`../Textures/<nom>.png`) :
  `dotnet run --project scripts/LegacyModelGltfConverter -- RacingGame/Content/Models <sortie>/Models <sortie>/Textures --skip Cube --normal-map RacingGame/Content/Textures/<nom>.tga` pour chacune des six normal maps des maillages générés (`LandscapeNormal`, `CityGroundNormal`, `RoadCementNormal`, `RoadBackNormal`, `RoadTunnelNormal`, `RoadNormal`), puis `--sky-cube RacingGame/Content/Textures/SkyCubeMap.dds`. `<sortie>` vaut `artifacts/pbr-handover/s1e/converted`.
- Validation :
  - build du convertisseur à 0 erreur ;
  - les 56 `.gltf` et leurs `.bin` produits sont identiques à l'octet à ceux d'`origin/remaster` ;
  - `SkyCubeMap.dds` produit est identique à l'octet à celui d'`origin/remaster` ;
  - les 55 normal maps produites ont les mêmes pixels que celles d'`origin/remaster` (comparaison des pixels décodés ; l'identité à l'octet est notée si elle tient) ;
  - les PNG de couleur produits ont les mêmes pixels que ceux d'ici (aucune régression du mode existant).
  - `--verify-textures`, sur un dossier qui contient tous les PNG : `artifacts/pbr-handover/s1e/overlay/Textures`, copie de `RacingGameCasaEngine/Content/Textures` par-dessus laquelle sont recopiés les fichiers de `<sortie>/Textures` (le mode modèles ne produit pas les PNG de couleur des maillages générés, que seul `--textures` écrit). Attendu : 129 `.tga`, 129 paires, 74 copies de couleur, 55 normal maps converties (les 55 fichiers `*Normal*.png` que l'autre PC a changés), 0 `.tga` sans `.png`, code de sortie 0. Sur le contenu d'ici, avant T8.2 : 129 paires, 129 copies de couleur, 0 normal map convertie, code de sortie 0.
- Arrêts : un fichier qui diffère de l'oracle sans cause trouvée dans le code du convertisseur ; besoin de modifier le moteur.
- Commit : `feat(scripts): write tangents and convert the normal maps and the sky cube`

> Validation (2026-11-16), `HEAD` avant la tranche `770dd6d`, exécutant `executor`, commit `2be57f2` :
> - Convertisseur construit à 0 erreur. Commande de l'oracle : 57 modèles, 1 ignoré (`Cube`), 0 échec, 100 textures converties, 6 normal maps en plus.
> - Les 56 `.gltf` et 56 `.bin`, `SkyCubeMap.dds` et les 106 PNG produits sont identiques à l'octet à ceux d'`origin/remaster`, dont les 55 normal maps ; les 51 PNG de couleur sont identiques à l'octet à ceux d'ici.
> - `--verify-textures` sur le dossier recouvert : 129 `.tga`, 129 paires, 74 copies de couleur, 55 normal maps converties, 0 différente, 0 sans `.png`, sortie 0 ; sur le contenu d'avant : 129 copies de couleur, 0 convertie, sortie 0. `--textures … Road RoadNormal:normal` redonne `Road.png` d'ici et `RoadNormal.png` de l'oracle.
> - Un tour sur quatre.

### ✅ T8.2 — Contenu régénéré et recuit

- Objectif : le contenu du jeu porte les tangentes, les normal maps converties et le cube du ciel réorienté, et les assets cuits en découlent.
- Fichiers : `RacingGameCasaEngine/Content/Models/*.gltf` et `*.bin`, `Content/Textures/*Normal*.png`, `Content/Textures/SkyCubeMap.dds`, les assets cuits (`Content/Models/*.staticModel`, `Content/Materials/**`, `Content/Textures/*.texture`, `Content/AssetInfos.json` si le cuiseur le touche) ; `RacingGame.Gameplay/Import/LegacyGltfModelReader.cs` (arrondi des couleurs, valeurs par défaut des matériaux sans effet d'origine) ; `scripts/RgceAssetCooker/Verifier.cs` (comparer aussi les valeurs des tangentes) et son `README.md` ; `RacingGameCasaEngine/Assets/README.md` (ce que vérifie `--verify-textures`) ; `docs/decisions/0024-rgce-content-holds-every-game-file.md` et l'index, statut seulement : amendé par les ADR-0016 et 0017, puisque les normal maps et le cube du ciel ne sont plus des copies de leur source.
- Étapes : recopier dans le contenu les fichiers produits par T8.1 ; porter l'arrondi et les valeurs par défaut de `4c058ad` ; faire comparer les tangentes par `--verify` ; recuire ; relancer `--check` et `--verify`.
- Validation :
  - build du jeu et du cuiseur à 0 erreur ; cuiseur `--check` et `--verify` sans écart ;
  - `--verify-textures RacingGame/Content/Textures RacingGameCasaEngine/Content/Textures` sort en succès sur le contenu régénéré, avec 55 normal maps converties ;
  - chaque maillage cuit dont le glTF a des tangentes a `tangents`, avec autant d'entrées que de sommets ; le nombre de maillages avec tangentes est noté et égal à celui des primitives à tangentes des glTF ;
  - un second passage du cuiseur ne réécrit aucun fichier ;
  - `--smoke-frontend` et `--verify-legacy-import-profile` en succès (ce dernier adapté seulement si l'arrondi ou les valeurs par défaut changent ce qu'il vérifie, chaque adaptation notée) ;
  - mesure de scène inchangée : 1147, 1693 et 3404 objets appariés, 0 manquant, 0 en trop, 0 écart d'ombre ; les 15 distances d'alignement inchangées.
- Arrêts : `--verify` en écart sans cause trouvée ; besoin de modifier le moteur ; un changement de taille de modèle qui déplacerait des objets.
- Commit : `feat(racing-casa): regenerate the models with tangents, the normal maps and the sky cube`

> Validation (2026-11-16), commit `2e9f263`, 270 fichiers : 112 glTF et bin, 56 `.staticModel`, 39 `.material`, 55 normal maps, `SkyCubeMap.dds`, 7 fichiers de code, README et ADR ; aucun `.texture`, catalogue inchangé.
> - Builds du jeu et du cuiseur à 0 erreur ; cuiseur : 95 fichiers écrits, `--check` et `--verify` sans écart ; un second passage n'écrit rien.
> - Tangentes : 101 primitives glTF sur 105 en ont ; 101 maillages cuits sur 105 ont un tableau `tangents` de la taille de leurs sommets ; `--verify` compare désormais leurs valeurs.
> - `--verify-textures` sur le contenu régénéré : 74 copies de couleur, 55 normal maps converties, sortie 0.
> - `--smoke-frontend` et `--verify-legacy-import-profile` en succès, sans adaptation du vérificateur.
> - Mesure de scène inchangée : 1147, 1693 et 3404 objets appariés, 0 manquant, 0 en trop, 0 écart d'ombre ; les 15 distances d'alignement inchangées.
> - Couleurs : 39 matériaux changés, 10 par les valeurs par défaut des matériaux sans effet d'origine (ambiant 0 → 40/255, diffus 149 → 210), 29 par l'arrondi (un niveau sur 255).
> - Écarts au plan : `scripts/RgceAssetCooker/Program.cs` touché pour afficher les comptes de tangentes ; les glTF copiés sont en fins de ligne LF dans l'arbre de travail, identiques à la sortie du convertisseur.
> - Noté sans suite : un message de `LegacyImportProfileVerifier.cs` parle encore de l'exception d'ambiant relevé des éoliennes, que l'autre PC a reformulé.

### ✅ T8.3 — Banc relancé et références reprises

- Objectif : mesurer ce que la tranche change à l'écran et reprendre les références d'ici.
- Fichiers : `docs/racinggame-3d-capture-bench-local-baseline.md` (section ajoutée, générée par script depuis les sorties), ce plan. Sorties dans `artifacts/pbr-handover/s1e/`.
- Étapes : deux rejeux des vues de l'original, comparaisons `nopost`, `full`, sondes, répétabilité ; `--capture-track-audit` et `--capture-ui-screens` comme nouvelles références d'ici.
- Validation : lancements en succès ; la section cite ses fichiers de sortie ; le ciel n'est plus le miroir de celui de l'original, constaté sur une planche de comparaison et noté.
- Commit : `docs(racing-casa): record the bench after the tangents, normal maps and sky cube`

> Validation (2026-11-17), sorties dans `artifacts/pbr-handover/s1e/`, section « After the tangents, the normal maps and the sky cube » du doc générée par script :
> - deux rejeux en succès ; mesure de scène inchangée ; 12 fichiers identiques sur 15 entre les deux, les trois autres à une différence moyenne de 0,00 ;
> - le ciel suit maintenant celui de l'original : sur la planche `compare-nopost/compare-Beginner-start.png`, la ligne des montagnes de l'original et celle du port coïncident ; avant, l'un était le miroir de l'autre ;
> - différence moyenne `nopost` à l'original en baisse sur 11 vues sur 15 (par exemple `Beginner-overview` de 22,03 à 16,06 sur le rouge), en hausse sur `Beginner-sector-50`, `Advanced-sector-80`, `Expert-sector-20` et `Expert-sector-80` ; contours retrouvés en hausse sur 11 vues, en baisse sur 4 (`Expert-sector-80` de 46,5 % à 42,4 %) ;
> - sondes inchangées dans leurs verdicts ; rapport ombre sur lumière de la ville passé de 0,682 à 0,965, celui de l'original étant 0,276 : les ombres restent l'affaire de S1g ;
> - nouvelles références d'ici : `audit/` (30 captures, de 1,671 à 28,136 d'écart au pire canal avec les précédentes) et `ui/` (états de menu à 0,00 ; `race-hud` 15,02, `pause` 4,89, `race-finished` 5,24 au pire canal, `car-selection` 1,56 : le ciel et les normal maps).

### ✅ T8.4 — Vérification indépendante de S1e

- Objectif : un `verifier` frais confirme ou réfute : les sorties du convertisseur sont celles de l'oracle ; le contenu du dépôt est celui que produit le convertisseur ; les assets cuits sont ceux que produit le cuiseur et portent les tangentes ; pas de régression ; les chiffres du doc sont ceux des sorties.
- Étapes et verdicts : comme T5.4 (un appel ; `INCONCLUSIVE`, un seul nouvel appel après preuve fournie ; P0 ou P1, arrêt et question ; P2 introduit par S1e, corrigé dans les fichiers de T8.1 à T8.3, deux passes au plus, `verifier` frais sur l'affirmation touchée ; le reste noté).
- Commit : `docs(ai-agent): record the verification of the asset slice`

> Validation (2026-11-17) : `verifier` frais, **CONFIRMED** sur les six affirmations, par ses propres lancements.
> - Sortie du convertisseur identique à `origin/remaster` et au contenu de `HEAD` (les `.gltf` ne diffèrent que par les fins de ligne CRLF écrites sous Windows, identiques une fois normalisées).
> - Code du convertisseur identique à `origin/remaster` hors des deux modes gardés ici ; une paire volontairement fausse fait bien échouer `--verify-textures`.
> - Le cuiseur détecte une tangente modifiée dans une copie du contenu ; les 39 matériaux changés s'expliquent tous par le port de `4c058ad`.
> - Placement et alignement inchangés ; ciel constaté à l'œil sur deux vues.
> - Chiffres du doc contrôlés cellule par cellule.
> - Remarques notées sans suite (candidat confirmé) :
>   - P3, note de T8.3 : « sondes inchangées dans leurs verdicts » ne vaut que pour les totaux par critère ; 14 verdicts du critère 1 ont basculé, 7 dans chaque sens, sur des sondes de terrain ;
>   - P4, note de T8.2 : un des 10 matériaux par défaut passait de 107 et non de 149 à 210 (`RoadColumnSegment/PDX04 - Default 04 - Default`) ;
>   - P4 : `--verify-textures` classe une paire comme normal map convertie d'après ses pixels, pas d'après son nom ; une texture de couleur convertie par erreur passerait, seule la liste affichée la montre ;
>   - P4 : le convertisseur écrit les `.gltf` en CRLF sous Windows, le dépôt les garde en LF ; une comparaison d'octets brute avec `git show` montre de fausses différences.
> - Fichiers laissés par la vérification, ignorés par git, à supprimer à votre convenance : `artifacts/pbr-handover/s1e/verify/mut-content` (253 Mo) et ses voisins `remaster`, `head`, `mut-tex`.

---

## Phase 9 — Mipmaps des textures du monde de course (S1m)

Source à porter : `58bb871` de l'autre PC (textures du monde de course et de la voiture chargées avec mipmaps, `git show 58bb871`). Ici les textures 3D sont des assets `.texture` cuits, chargés par identifiant : le réglage passe par l'asset.

État vérifié (2026-11-17) :

- Le moteur charge une image une fois, sans mipmaps, pour tous ses utilisateurs ; `Texture2DLoader.GenerateMipmaps` n'est réglable qu'en code, pour tout le chargeur (ADR-0101 du moteur).
- Le cuiseur écrit 107 `.texture`, avec `texture_asset_id` et `sampler_state` (`scripts/RgceAssetCooker/Cooker.cs`) ; ce sont les textures des modèles et de la piste, toutes en 3D. Les images de l'interface ne passent pas par ces assets.
- La peinture de la voiture crée deux textures en code, sans mipmaps (`RacingGame.Gameplay/Components/LegacyCarVisualFactory.cs`, `CarPaint.Load` et `Paint`).

Retour arrière de S1m : jeu, relever `git rev-parse HEAD` avant T9.2 et annuler les commits de S1m sur `chantier/rgce-pbr-handover` ; moteur, selon son plan. Sur demande de l'auteur seulement.

Budget de S1m : deux tours de recuisson ; deux passes de correction après vérification.

### ⏳ T9.1 — Moteur : réglage de mipmaps par asset `.texture`

- Plan propre dans le dépôt du moteur : `CasaEngine/ai-agent/tasks/texture-asset-mipmaps-tasks.md`, branche moteur `chantier/texture-asset-mipmaps` (partie de `chantier/static-model-tangents`).
- Validation : celle du plan du moteur, puis un `verifier` frais sur ce plan avant T9.2.
- Commits : ceux du plan du moteur ; dans le jeu, pointeur du sous-module avancé dans le commit de T9.2.

### ⏳ T9.2 — Textures cuites et peinture de la voiture avec mipmaps

- Fichiers : `scripts/RgceAssetCooker/Cooker.cs` (`generate_mipmaps` vrai sur chaque `.texture` cuit), `Verifier.cs` (le compare), `README.md` ; les 107 `Content/Textures/*.texture` recuits ; `RacingGame.Gameplay/Components/LegacyCarVisualFactory.cs` (les deux textures de peinture créées avec `mipMap: true`, chaîne écrite par `MipmapChain.UploadChain` à la création et après chaque peinture, comme `58bb871`) ; pointeur `CasaEngine` ; ce plan.
- Validation :
  - build du jeu et du cuiseur à 0 erreur ; cuiseur `--check` et `--verify` sans écart ; second passage sans écriture ;
  - les 107 `.texture` ont `generate_mipmaps` à vrai, aucun autre champ changé ;
  - `--smoke-frontend` et `--verify-legacy-import-profile` en succès ;
  - mesure de scène inchangée (1147, 1693, 3404 appariés, 0 manquant, 0 en trop, 0 écart d'ombre ; 15 distances inchangées) ;
  - banc relancé vers `artifacts/pbr-handover/s1m/` (deux rejeux, `nopost`, `full`, sondes, répétabilité, comparaison avec `s1e`), section du doc générée par script ; nouvelles références `--capture-track-audit` et `--capture-ui-screens` ;
  - une ligne de journal de chargement, ou un contrôle équivalent, montre qu'une texture du décor a plus d'un niveau.
- Arrêts : `--verify` en écart sans cause trouvée ; un écart de mesure de scène.
- Commit : `feat(racing-casa): draw the race world textures with their mipmaps`

### ⏳ T9.3 — Vérification indépendante de S1m

- Objectif : un `verifier` frais confirme ou réfute : les textures cuites et la peinture de la voiture ont leur chaîne ; pas de régression ; les chiffres du doc sont ceux des sorties.
- Étapes et verdicts : comme T5.4.
- Commit : `docs(ai-agent): record the verification of the mipmap slice`

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
- O8 (remis à l'auteur par S1c) : la mesure contre l'original montre que la version d'ici, gardée comme référence (D7), s'écarte de l'original sur plusieurs sujets faits en double : échelle des poteaux de rail, nombre de segments de colonne, lanternes en trop, palmiers, panneaux, ombres portées. Détail dans `docs/racinggame-3d-capture-bench-local-baseline.md`. Rien n'est corrigé d'office. Question : faut-il aligner ces sujets sur l'original (une tranche de plus, avant ou après le PBR), ou les laisser tels quels ? **Tranché le 2026-11-16 : D11, aligner maintenant (tranche S1a).**
- O9 (S1a) : un support de rail en pente penche par rapport à celui de l'original, jusqu'à 1,08°, 6,81° et 11,91° sur les trois pistes, parce qu'une entité ne peut pas porter le cisaillement de la matrice de l'original. Lanternes, banderoles, panneaux et objets de départ portent le même cisaillement. Approximation acceptée en mode AUTO (position et échelle exactes). Question : faut-il la version exacte, par une hiérarchie de pivots par objet côté jeu, ou par une transformation matricielle côté moteur ?
- O10 (S1a) : `scripts/TrackPlacementExporter` réimplémente l'ancien placement et ses tailles de modèle ; il ne décrit plus ce que le jeu place. À supprimer ou à réaligner : choix de l'auteur.
- O11 (S1a) : le maillage de la route garde le vecteur droite normalisé, alors que l'original, et maintenant le rail et ses supports, utilisent le vecteur brut. Là où ce vecteur est plus court que 1, le rail se tient à l'intérieur du bord du maillage de route : 0,008 m au plus sur Beginner, 0,298 m sur Advanced (6 points de rail sur 1087 au-dessus de 5 cm), 0,627 m sur Expert (12 sur 2234). Aligner aussi le maillage de la route toucherait la géométrie roulable : choix de l'auteur.
