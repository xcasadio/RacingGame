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
| S1m | Textures du monde de course avec mipmaps (`58bb871`) : un réglage par asset pour une texture cuite, avec une évolution du moteur | S1e | Faite le 2026-11-17, verdict CONFIRMED |
| S1f | Matériaux PBR cuits (D6) : recettes par famille, cuiseur en `lit-pbr`, peinture de la voiture et plateau du carrousel, texture de détail du terrain, vérificateurs, suppression des tables de réglage et du relèvement d'ambiant du profil d'import ; ADR du jeu mis à jour (0018 de là-bas et 0026 d'ici réunis par un ADR qui les amende) | S1m | Faite le 2026-11-17, verdict CONFIRMED après la correction du moteur (GGX côté dos) |
| S1g | Monde de course : `LinearHdr`, lumière clé, ombres, ciel en phase de scène, mode `Legacy` pour le banc, tangentes de la route, des tunnels et des maillages générés | S1f | Faite le 2026-11-17, verdict CONFIRMED ; luminosité en question (O13) |
| S1h | Géométrie qui n'existe que sur l'autre PC, en générateurs : dessous de la route, sol de la ville (le décor de remplissage aléatoire est passé en S1a) ; ADR-0019 corrigé (il affirme qu'il ne manque rien face à l'original) | S1f | Faite le 2026-11-17, verdict CONFIRMED |
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

### ✅ T9.1 — Moteur : réglage de mipmaps par asset `.texture`

- Plan propre dans le dépôt du moteur : `CasaEngine/ai-agent/tasks/texture-asset-mipmaps-tasks.md`, branche moteur `chantier/texture-asset-mipmaps` (partie de `chantier/static-model-tangents`).
- Validation : celle du plan du moteur, puis un `verifier` frais sur ce plan avant T9.2.
- Commits : ceux du plan du moteur ; dans le jeu, pointeur du sous-module avancé dans le commit de T9.2.

> Validation (2026-11-17) : moteur, branche `chantier/texture-asset-mipmaps`, commits `b9cc2ef23` (code et tests) et `2b64b0591` (ADR-0124 et doc).
> - Tests du moteur : 6391 lancés, 6383 réussis ; les 8 échecs sont les 8 tests GPU connus (O3), aucun autre.
> - `verifier` frais : **CONFIRMED**.
> - Remarques notées sans suite (P3, P4) :
>   - une image qui n'est pas au format Color donne une erreur avalée par le `catch` général déjà présent dans `MaterialCompiler` ;
>   - un exemple JSON du doc a un `sampler_state` vide ;
>   - un réglage changé après `Load` laisserait fuir une prise ;
>   - les matériaux déjà compilés gardent l'ancienne texture après une perte de périphérique (comportement antérieur à ce plan) ;
>   - aucun test de la branche de remise à zéro quand le réglage est faux.

### ✅ T9.2 — Textures cuites et peinture de la voiture avec mipmaps

- Fichiers : `scripts/RgceAssetCooker/Cooker.cs` (`generate_mipmaps` vrai sur chaque `.texture` cuit), `Verifier.cs` (le compare), `README.md` ; les 107 `Content/Textures/*.texture` recuits ; `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs` (contrôle de chaîne ci-dessous) ; `RacingGame.Gameplay/Components/LegacyCarVisualFactory.cs` (les deux textures de peinture créées avec `mipMap: true`, chaîne écrite par `MipmapChain.UploadChain` à la création et après chaque peinture, comme `58bb871`) ; pointeur `CasaEngine` ; ce plan.
- Validation :
  - build du jeu et du cuiseur à 0 erreur ; cuiseur `--check` et `--verify` sans écart ; second passage sans écriture ;
  - les 107 `.texture` ont `generate_mipmaps` à vrai, aucun autre champ changé ;
  - `--smoke-frontend` et `--verify-legacy-import-profile` en succès ;
  - mesure de scène inchangée (1147, 1693, 3404 appariés, 0 manquant, 0 en trop, 0 écart d'ombre ; 15 distances inchangées) ;
  - banc relancé vers `artifacts/pbr-handover/s1m/` (deux rejeux, `nopost`, `full`, sondes, répétabilité, comparaison avec `s1e`), section du doc générée par script ; nouvelles références `--capture-track-audit` et `--capture-ui-screens` ;
  - contrôle de la chaîne : sous `--capture-reference-views`, une fois le premier monde de course prêt, le validateur journalise pour `Texture.Road` (`438bfe97-634a-50cc-a008-7661c239ab25`), `Texture.Landscape` (`ee748e62-6069-552c-a7cb-2656da413b49`) et `Texture.Building` (`5dd4c964-020b-59c5-9d59-b40fc856c0f9`) une ligne `[CaptureBench] mipmaps <nom> levels <n> expected <m>`, avec `n` le `LevelCount` de `Resource` du `Texture` acquis et chargé par identifiant, et `m` = `MipmapChain.GetLevelCount(largeur, hauteur)`. Réussite : trois lignes, `n` = `m` > 1 pour chacune. Le validateur fait partie des fichiers de cette tâche. Même contrôle pour la peinture de la voiture : le validateur journalise aussi `[CaptureBench] mipmaps CarPaint.Painted levels <n> expected <m> chain <ok|KO>` et la même ligne pour `CarPaint.Unpainted`, lues par un accès interne ajouté à `LegacyCarVisualFactory` ; `chain ok` veut dire que le niveau 1 lu sur la texture est égal à `MipmapChain.ComputeNextLevel` de son niveau 0, ce qui prouve que la chaîne suit la dernière peinture. Réussite : deux lignes de plus, `n` = `m` > 1 et `chain ok`.
- Arrêts : `--verify` en écart sans cause trouvée ; un écart de mesure de scène.
- Commit : `feat(racing-casa): draw the race world textures with their mipmaps`

> Validation (2026-11-17), jeu relevé à `e7a0200` avant la tâche, pointeur du moteur avancé de `61b4e6fae` à `2b64b0591` (cinq commits du moteur).
> - Builds du jeu (`RacingGameCasaEngine.csproj`, Debug) et du cuiseur à 0 erreur, 0 avertissement.
> - Cuiseur : avant recuisson `--verify` donnait 107 écarts (`generate_mipmaps` absent), preuve que le contrôle mord ; un tour de recuisson (sur deux) : 107 fichiers écrits, 154 inchangés, `--verify` 0 écart ; `--check` 0 manquant ; second passage : 0 écrit, 261 inchangés, 0 écart.
> - Les 107 `Content/Textures/*.texture` changent, chacun d'une seule ligne ajoutée, `"generate_mipmaps": true` ; contrôlé par script contre `HEAD` (champs JSON et lignes), 0 anomalie ; aucun autre fichier de contenu changé.
> - `--smoke-frontend` (« Smoke validation completed successfully » au journal) et `--verify-legacy-import-profile` (« Legacy import profile verification passed. ») : sortie 0, relancés après la reconstruction qui copie le contenu recuit dans `bin`.
> - Deux rejeux `--capture-reference-views` (`cand-a`, `cand-b`) : sortie 0 ; mesure de scène inchangée (1147/1147, 1693/1693, 3404/3404 appariés, 0 manquant, 0 en trop, 0 écart d'ombre d'émission et de réception) ; les 15 distances d'alignement inchangées.
> - Contrôle de chaîne, journal des deux rejeux : `Texture.Road` 11 niveaux pour 11 attendus, `Texture.Landscape` 12 pour 12, `Texture.Building` 10 pour 10, `CarPaint.Painted` 12 pour 12 `chain ok`, `CarPaint.Unpainted` 12 pour 12 `chain ok`.
> - Premier essai du rejeu (non gardé) : 1 niveau pour les trois textures de la piste, parce que le jeu avait été reconstruit avant la recuisson et que `bin` avait encore les anciens `.texture` ; reconstruit, puis rejeué.
> - Banc vers `artifacts/pbr-handover/s1m/` : `nopost`, `full`, sondes, répétabilité (12 fichiers identiques sur 15), comparaison avec `s1e`, nouvelles références `audit` (30 captures) et `ui` (10), comparaisons `UiCaptureCompare` et `compare_track_audit.py` ; section « After the mipmaps » du doc générée par script (`gen_section_s1m.py`, hors dépôt).
> - Écart des vues avec `s1e`, moyenne des trois canaux : de 0,41 à 1,44 ; arêtes appariées (`nopost`) plus basses sur les 15 vues, de 0,1 à 5,7 points ; totaux des sondes inchangés (11, 0, 21, 0) ; UI au plus 0,79 de différence, audit au plus 1,589.

### ✅ T9.3 — Vérification indépendante de S1m

- Objectif : un `verifier` frais confirme ou réfute : les textures cuites et la peinture de la voiture ont leur chaîne, en relançant le contrôle de chaîne de T9.2 (les cinq lignes `[CaptureBench] mipmaps`, dont les deux de la peinture) ; pas de régression ; les chiffres du doc sont ceux des sorties.
- Étapes et verdicts : comme T5.4.
- Commit : `docs(ai-agent): record the verification of the mipmap slice`

> Validation (2026-11-17) : `verifier` frais, **CONFIRMED** sur les quatre affirmations, par ses propres lancements (jeu reconstruit avant).
> - Contenu : les 107 `.texture` ne changent que par `"generate_mipmaps": true` ; le cuiseur, remis sur les anciennes textures dans une copie, signale bien 107 écarts.
> - Exécution : les cinq lignes `[CaptureBench] mipmaps` retrouvées à l'identique ; les textures mesurées sont celles que dessinent les matériaux ; la chaîne de la peinture est encore juste après une repeinte ; le changement `<` → `!=` du tampon de peinture ne réalloue pas à chaque peinture (textures de 2048 × 2048).
> - Pas de régression ; chiffres du doc contrôlés par script (131 contrôles).
> - Remarques notées sans suite (candidat confirmé) :
>   - P3 : une repeinte de la voiture coûte maintenant 62 à 67 ms au lieu de 10 à 21 ms, parce que la chaîne est recalculée sur le processeur à chaque couleur ; un à-coup ponctuel à la sélection de voiture, comme sur l'autre PC ;
>   - P4 : « 0 avertissement » dans la note de T9.2 vaut pour un build incrémental ; un build complet en compte dans `CasaEngine/`, aucun dans le code du jeu.

---

## Phase 10 — Matériaux PBR cuits (S1f)

Décision D6 : les assets restent cuits, et le cuiseur cuit des matériaux `lit-pbr`. Source à porter : `RacingGameCasaEngine/Bootstrap/RacingGamePbrMaterialMapping.cs` d'`origin/remaster` (familles, règle de correspondance) et les commits `d0db72f`, `09f1c54`, `d35a25a`, `f309d6b`, `fb7daa2`, `b5391fe`, `46f2013` (lisibles par `git show`). Le monde de course reste dans le pipeline couleur `Legacy` et sous sa lumière actuelle jusqu'à S1g : le rendu de cette tranche est un état intermédiaire, mesuré mais pas réglé.

État vérifié (2026-11-17) :

- Le cuiseur écrit 98 matériaux `lit-diffuse` (`scripts/RgceAssetCooker/Cooker.cs`, `CreateMaterialAsset`), depuis les recettes de `RacingGame.Gameplay/Import/RaceMaterialRecipes.cs` (`ForScenery`, `ForCar`, `ForPlate`, `ForRoad`, `ForGround`, `ForTerrain`, `ForGuardRail`, `ForRoadColumn`, `ForRoadTunnel`), qui passent par `RacingGameLegacyMaterialTuning`.
- Clés de `lit-pbr` dans le moteur : `base_color_texture`, `normal_texture`, `metallic_roughness_texture`, `occlusion_texture`, `emissive_texture`, `detail_texture`, `detail_tiling`, `base_color`, `metallic`, `roughness`, `emissive_color`, `occlusion_strength`, `normal_scale`, `clear_coat`, `clear_coat_roughness`, `alpha_cutoff` (`BuiltInMaterialDefinitions.cs`) ; pas de clé d'ambiant, de spéculaire ni de réflexion.
- Familles de l'autre PC (métal, rugosité, vernis, rugosité du vernis) : Terrain 0 / 1 ; Road 0 / 0,85 ; GuardRail 1 / 0,45 ; LampPost 1 / 0,5 ; Masonry 0 / 0,9 ; Signage 0 / 0,6 ; Vegetation 0 / 0,95 ; MatteIndustrial 0 / 0,7 ; BuildingGlass 1 / 0,05 ; CarPaint 0 / 0,5, vernis 1 / 0,05 ; CarChrome 1 / 0,1 ; CarGlass 1 / 0,05 ; CarTyre 0 / 0,9 ; CarouselPlate 1 / 0,3 ; Default 0 / 0,8. Règle : le verre des immeubles par son effet d'origine (`ReflectionSimpleGlass.fx`), les autres par le nom de la texture diffuse ; la voiture par `LegacyCarVisualFactory.ResolveFamily` d'`origin/remaster` : verre si le nom contient `glass` ou `fenster` OU si l'effet est `ReflectionSimpleGlass.fx` (le verre cuit de la voiture s'appelle `Standard 19` et n'est reconnaissable que par son effet), puis chrome (`chrome`), pneu (`gummi`), peinture, sinon `Default`.
- La peinture de la voiture copie en code le matériau cuit de chaque emplacement à texture de voiture (`LegacyCarVisualFactory.CreateCarTextureCopy`, `CopyWithBaseTexture`, sur `LitDiffuseMaterial`) et choisit peint ou non peint par le nom de l'emplacement et par `Reflects(source)`, qui lit des champs de réflexion que `lit-pbr` n'a pas.
- `LandscapeDetail.png` a une entrée `Image.LandscapeDetail` mais pas d'asset `.texture`.
- `RacingGameCasaEngine/Bootstrap/LegacyImportProfileVerifier.cs` vérifie le réglage spéculaire et l'ambiant relevé par `RacingGameLegacyMaterialTuning` ; celui de l'autre PC vérifie les familles.

Retour arrière de S1f : relever `git rev-parse HEAD` avant T10.1 et le noter sous T10.1 ; annuler les commits de S1f sur `chantier/rgce-pbr-handover` seulement ; `artifacts/pbr-handover/s1m/` reste la référence d'avant. Sur demande de l'auteur seulement.

Budget de S1f : T10.2, au plus trois tours de recuisson ; deux passes de correction après vérification.

### ✅ T10.1 — Recettes PBR

- Objectif : les recettes calculent aussi, pour chaque matériau cuit, les valeurs `lit-pbr` de l'autre PC. Les membres `lit-diffuse` des recettes et les tables de réglage restent jusqu'à T10.2, qui les retire avec le passage du cuiseur : le commit de T10.1 se construit et ne change aucun fichier cuit.
- Fichiers : `RacingGame.Gameplay/Import/RaceMaterialRecipe.cs` (membres ajoutés : famille, métal, rugosité, vernis, rugosité du vernis, couleur de base, texture et répétition de détail), `RaceMaterialRecipes.cs`, `RacingGamePbrFamily.cs` et `RacingGamePbrMaterialMapping.cs` (nouveaux, portés de l'autre PC dans `RacingGame.Gameplay/Import`, avec la règle de la voiture de `LegacyCarVisualFactory.ResolveFamily` d'`origin/remaster`). Aussi : `RacingGame.Gameplay/Components/LegacyCarVisualFactory.cs` (un accès interne qui compte, pour une voiture, les emplacements à texture de voiture peints et non peints, sans rien changer) et `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs` (ligne permanente `[CaptureBench] car paint slots painted <p> unpainted <u>` pour la voiture 0, écrite à côté des lignes `mipmaps`).
- Règles (celles de `RacingGamePbrMaterialMapping.Create` de l'autre PC) :
  1. la texture diffuse devient `base_color_texture`, la normal map reste ;
  2. la couleur diffuse devient `base_color`, alpha forcé à 255 ; pour la voiture, 210 sur tout sauf le verre (au sens de la règle 5) (`LegacyCarDefaultDiffuseColor`, `Model.RenderCar` de l'original) ;
  3. l'émissive est gardée ; l'ambiant, le spéculaire et la réflexion par matériau disparaissent ;
  4. file, seuil alpha, culling et échantillonneur gardés ;
  5. familles : décor par `ResolveImportedFamily` ; voiture par le nom du matériau ou, pour le verre, par l'effet `ReflectionSimpleGlass.fx` ; plateau du carrousel `CarouselPlate` ; route, dessous de route, tunnel, colonnes `Road` ; terrain et sol `Terrain` ; rails `GuardRail` ;
  6. terrain : `detail_texture` = `Texture.LandscapeDetail`, `detail_tiling` = 24 (`f309d6b`).
- Validation : build du jeu et du cuiseur à 0 erreur ; cuiseur `--verify` sur le contenu de S1m sans écart, avec les mêmes notes de rôle de peinture pour les 6 matériaux de la voiture qu'avant T10.1 ; un script liste la famille calculée de chacun des 98 matériaux et le note ; `Material.Car.Standard 19` y est `CarGlass` (métal 1, rugosité 0,05, couleur de base 13, 13, 13, 255). Un rejeu des vues de l'original sur le contenu de S1m donne la ligne `car paint slots` ; ses deux nombres sont la référence d'avant, notés sous cette tâche.
- Commit : `feat(racing-casa): compute PBR material recipes from the surface families`

> Validation (2026-11-17), jeu relevé à `a3a53dab610f17d483fe5a0e11f57a5a360cf811` avant la tâche (point de retour arrière de S1f).
> - Builds du jeu (`RacingGameCasaEngine.csproj`, Debug) et du cuiseur à 0 erreur, 0 avertissement. Aucun fichier cuit changé (`git status` : rien sous `RacingGameCasaEngine/Content`).
> - Cuiseur `--verify` sur le contenu de S1m : 98 matériaux (2940 valeurs), 0 écart ; les 6 notes de rôle de peinture (`Standard 19` File, `lack` CarPainted, `gummi`, `chrome`, `gummi 2`, `chrome 2` CarUnpainted) sont identiques, ligne à ligne, à celles du `--verify` de S1m.
> - Familles calculées des 98 matériaux (outil jetable hors dépôt, qui rejoue `CookPlanner.Build` et relit les glTF pour l'effet) : Default 29, Masonry 13, Signage 18, LampPost 11, MatteIndustrial 6, Vegetation 4, BuildingGlass 3, Road 3, GuardRail 2, Terrain 2, CarChrome 2, CarTyre 2, CarGlass 1, CarPaint 1, CarouselPlate 1 ; total 98. `Material.Car.Standard 19` : CarGlass, métal 1, rugosité 0,05, couleur de base 13, 13, 13, 255 (reconnu par l'effet `ReflectionSimpleGlass.fx`). Les 29 `Default` sont les matériaux des décors qu'aucune liste de textures de l'autre PC ne nomme : arbres et palmiers (7), train (2), barrages (2), cactus (4), rochers (2), château de sable (1), segment de colonne (11, dont 10 sans texture) ; l'autre PC en relevait aussi 29 (sa revue de T3.1).
> - Rejeu des vues de l'original (`--capture-reference-views` vers `artifacts/pbr-handover/s1f/before`), sortie 0 : `[CaptureBench] car paint slots painted 1 unpainted 10`, **les deux nombres de référence d'avant S1f**. Mesure de scène identique à celle de S1m (les trois `scene-measure-*.txt` égaux octet pour octet), les cinq lignes `mipmaps` inchangées.
> - Écart au plan : la famille porte un nom (`Name`, premier membre de `RacingGamePbrFamily`), absent de la version de l'autre PC ; sans lui le verre des immeubles et celui de la voiture, de mêmes valeurs, ne se distingueraient pas dans le décompte par famille.

### ✅ T10.2 — Cuisson en `lit-pbr`, peinture de la voiture

- Objectif : les 98 matériaux sont recuits en `lit-pbr`, la voiture peinte reste juste, rien d'autre ne bouge.
- Fichiers, en plus de ceux qui suivent : `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs` si un changement de `LegacyCarVisualFactory` l'impose, rien d'autre ; suppression de `RacingGame.Gameplay/Import/RacingGameLegacyMaterialTuning.cs`, des membres `lit-diffuse` des recettes (ambiant, spéculaire, réflexion) et du relèvement d'ambiant de `RacingGameLegacyMaterialImportProfile.cs` (`b5391fe`, `46f2013`) ; l'appel à `ShouldEnableReflection` de ce profil est remplacé sur place par le test de l'effet `ReflectionSimpleGlass.fx`, comme sur l'autre PC, pour que l'indice de réflexion lu par les recettes de la voiture et du plateau ne change pas ; `RacingGameCasaEngine/Bootstrap/LegacyImportProfileVerifier.cs` porté pour vérifier les familles (comme `b5391fe` là-bas) ; tout fichier que `rg "RacingGameLegacyMaterialTuning|SpecularColor|ReflectionCubeFilePath|UseSceneReflectionCube" RacingGame.Gameplay scripts/RgceAssetCooker RacingGameCasaEngine --glob "*.cs"` désigne.
- Fichiers : `scripts/RgceAssetCooker/Cooker.cs` (`CreateMaterialAsset` en `lit-pbr` ; cuisson de `Texture.LandscapeDetail`, avec `generate_mipmaps`, et son entrée de catalogue), `CookPlan.cs`, `Verifier.cs` (compare les valeurs `lit-pbr`), `README.md` ; les `.material` recuits, `Content/Textures/LandscapeDetail.texture`, `Content/AssetInfos.json` ; `RacingGame.Gameplay/Components/LegacyCarVisualFactory.cs` (copie d'un `LitPbrMaterial` cuit ; l'accès de comptage de T10.1 suit la nouvelle règle ; un emplacement à texture de voiture est peint quand son matériau cuit porte un vernis, `clear_coat` > 0, la famille `CarPaint` étant la seule à en avoir) ; `docs/decisions/` (ADR de la décision, skill `adr` : ADR-0018 de l'autre PC et ADR-0026 d'ici réunis, l'un et l'autre marqués amendés) ; ce plan.
- Validation :
  - la commande `rg` ci-dessus ne trouve plus rien ;
  - builds à 0 erreur ; cuiseur `--check` et `--verify` sans écart ; second passage sans écriture ;
  - les 98 `.material` ont `lit-pbr` pour définition ; un script compte les matériaux par famille et le note (le décompte doit couvrir les 98) ;
  - `--verify-legacy-import-profile` en succès, avec les contrôles de familles de l'autre PC ;
  - `--smoke-frontend` en succès ; mesure de scène inchangée (1147, 1693, 3404 appariés, 0 manquant, 0 en trop, 0 écart d'ombre ; 15 distances inchangées) ; les cinq lignes `[CaptureBench] mipmaps` inchangées ;
  - peinture : la ligne `[CaptureBench] car paint slots` du rejeu donne les mêmes deux nombres que ceux notés sous T10.1 ;
  - banc relancé vers `artifacts/pbr-handover/s1f/` (deux rejeux, `nopost`, `full`, sondes, répétabilité, comparaison avec `s1m`), section du doc générée par script ; nouvelles références d'audit et d'interface.
- Arrêts : `--verify` en écart sans cause trouvée ; un écart de mesure de scène ; un matériau sans famille qui ne soit pas un cas `Default` voulu ; besoin de modifier le moteur.
- Commit : `feat(racing-casa): cook the race world materials as PBR`

> Validation (2026-11-17), un tour de recuisson sur trois.
> - Builds du jeu (`RacingGameCasaEngine.csproj`, Debug) et du cuiseur à 0 erreur, 0 avertissement.
> - Cuiseur : avant recuisson `--verify` donnait 199 écarts sur les anciens fichiers (définition `lit-diffuse`, `detail_texture` absent, `lack` non peint), preuve que le contrôle mord ; recuisson : 99 fichiers écrits (les 98 `.material` et `Textures/LandscapeDetail.texture`), 163 inchangés, catalogue 607 -> 608 entrées, `--verify` 0 écart (98 matériaux, 2744 valeurs, 108 textures) ; `--check` 0 manquant ; second passage : 0 écrit, 262 inchangés, 0 écart. Les 6 notes de rôle de peinture sont celles d'avant, ligne à ligne. Seuls fichiers de contenu changés : les 98 `.material`, `LandscapeDetail.texture` (nouveau) et `AssetInfos.json` (une entrée ajoutée).
> - Les 98 `.material` ont `lit-pbr` pour définition ; décompte par famille, lu dans les fichiers cuits et croisé avec le plan de cuisson (script, section du doc de banc) : Default 29, Signage 18, Masonry 13, LampPost 11, MatteIndustrial 6, Vegetation 4, BuildingGlass 3, Road 3, CarChrome 2, CarTyre 2, GuardRail 2, Terrain 2, CarGlass 1, CarPaint 1, CarouselPlate 1 ; total 98, aucun fichier hors plan.
> - `rg "RacingGameLegacyMaterialTuning|SpecularColor|ReflectionCubeFilePath|UseSceneReflectionCube"` sur les trois dossiers : plus de table de réglage ni de membre de recette ; 4 résultats restent, qui ne sont pas des réglages de matériau : `SpecularColor` des deux lumières directionnelles (`RaceWorldFactory.cs:175`, `CarSelectionCarousel.cs:110`, propriété de `LightComponent`) et les deux lectures du spéculaire de la glTF d'origine par `LegacyGltfModelReader.cs:70` et `:106` (donnée importée du modèle, plus lue par personne). Laissés, hors tâche.
> - `--verify-legacy-import-profile` : « Legacy import profile verification passed. », sortie 0, avec les contrôles de familles (immeuble, panneau, feux, palmier avec file alpha-test, bannière, verre d'hôtel métal lisse opaque, voiture verre, peinture, chrome, pneu, moulin). `--smoke-frontend` : sortie 0, « Smoke validation completed successfully » au journal, aucun avertissement ni erreur.
> - Mesure de scène inchangée sur les deux rejeux (`cand-a`, `cand-b`) : 1147/1147, 1693/1693, 3404/3404 appariés, 0 manquant, 0 en trop, 0 écart d'ombre ; les trois `scene-measure-*.txt` égaux octet pour octet à ceux de S1m, les 15 lignes `alignment` identiques. Les cinq lignes `[CaptureBench] mipmaps` identiques à celles de S1m.
> - Peinture : `[CaptureBench] car paint slots painted 1 unpainted 10` sur `cand-a` et `cand-b`, **les nombres d'avant S1f** (T10.1).
> - Banc vers `artifacts/pbr-handover/s1f/` : deux rejeux, `nopost`, `full`, sondes, répétabilité (13 fichiers identiques sur 15, écarts moyens de 0,00), comparaison avec `s1m`, nouvelles références d'audit (30 captures) et d'interface (10), comparées à celles de `s1m` ; section « After the PBR materials » du doc générée par script (`gen_section_s1f.py`, hors dépôt).
> - Mesures, moyenne des trois canaux, de l'état intermédiaire (lumière, pipeline couleur et ombres inchangés) : écart des vues avec `s1m` de 22,37 à 50,91 ; luminance moyenne du port 56,0 pour 96,3 à l'original (93,6 avant) ; arêtes appariées (`nopost`) plus basses sur les 15 vues, de 10,4 à 68,3 points ; sondes 10 sur 34 au critère 1 (11 avant), 21 sur 33 au critère 3 (inchangé) ; UI : sélection de voiture 4,86 / 4,21 / 4,23, HUD de course 30,02 / 28,72 / 26,38, pause 8,96, fin de course 23,31, les six autres écrans à 0,00 ; audit au plus 56,691, au moins 14,910. Le rendu est nettement plus sombre qu'en S1m : réglage de la lumière en S1g.
> - Écarts au plan : le membre `DiffuseColor` des recettes est retiré avec les membres `lit-diffuse` (il double `BaseColor`) ; le vérificateur du cuiseur range en écart un fichier cuit auquel manque une valeur de sa définition, au lieu de s'arrêter sur une exception ; `RaceSkySystem.ShouldUseSceneReflectionCube` (propre aux recettes) est retiré. Plateau du carrousel : `CarSelectionCarousel.cs` n'a pas changé (il charge le modèle cuit) ; ses lumières et son éclairage sont de S1g.
> - ADR-0028 (`docs/decisions/`) enregistre la décision et marque amendés ADR-0018 et ADR-0026 (lignes d'état et index).

### ✅ T10.3 — Vérification indépendante de S1f

- Objectif : un `verifier` frais confirme ou réfute : chaque matériau cuit porte les valeurs de sa famille selon les règles de T10.1 ; la peinture de la voiture et le plateau du carrousel sont justes ; les tables de réglage ont disparu ; pas de régression ; les chiffres du doc sont ceux des sorties.
- Étapes et verdicts : comme T5.4.
- Commit : `docs(ai-agent): record the verification of the PBR materials`

> Bloquée le 2026-11-17.
> - `verifier` frais : **REFUTED**. Affirmations 1, 2, 3 et 5 confirmées par ses propres lancements : les 98 matériaux suivent les règles de l'autre PC (script indépendant du cuiseur, 0 erreur ; les 29 `Default` le sont aussi là-bas), cuiseur et contenu cohérents, placement, mipmaps et peinture inchangés, chiffres du doc justes.
> - F1 (P2, introduit par S1f) : traînées blanches saturées sur les crêtes raides du terrain (`Beginner-start` 3145 pixels blancs contre 50 avant, `Expert-start` 2080 contre 246, aussi dans `Beginner-sector-80`). Isolées par essais sur une copie du build : elles disparaissent sans la normal map du terrain, restent avec une couleur de base noire (c'est du spéculaire), ne dépendent pas de la texture de détail. Ce n'est pas l'effet de la lumière d'avant S1g.
> - Cause, lue dans le moteur : `PbrDistributionGgx` (`Pbr.fxh`) prend `1 - NoH^2` du produit vectoriel de N et H depuis `6ead47308` ; ce n'est juste que si N fait face à H. Côté dos, le dénominateur tend vers 0 et la distribution explose.
> - Correction tentée dans le moteur, plan `CasaEngine/ai-agent/tasks/pbr-ggx-back-facing-tasks.md`, branche `chantier/pbr-ggx-back-facing` : bloquée au budget, 6 tests GPU de `lit-pbr` passent au noir avec les deux formes essayées, alors que le calcul reste juste dans le cas mesuré ; hypothèse d'une limite d'instructions de la chaîne OpenGL, non vérifiée. Aucun code du moteur n'est committé ; le pointeur du jeu reste sur `2b64b0591`.
> - Remarques P4 du vérificateur, notées : `Material.Track.Ground` n'est utilisé par aucune scène ; l'ADR-0028 et un commentaire citent un dessous de route qui n'existe pas encore ici ; `Material.Track.Road` n'a pas de normal map, comme avant.
> - Effet sur le programme : S1f reste ouverte tant que F1 n'est pas corrigé ; S1g, S1h et les tranches suivantes en dépendent et attendent. Question à l'auteur, notée en O12.
> - Reprise demandée par l'auteur (réponse à O12, 2026-11-17). Correction, passe 1 sur 2 de cette tâche : moteur `8a3933b36` sur `chantier/pbr-ggx-back-facing` (plan moteur `pbr-ggx-back-facing-tasks.md`, T2) : dénominateur `(1 - NoH)(1 + NoH) + NoH^2 alpha^2`. Tests du moteur : seuls les 8 échecs connus. Démo PBR identique sur trois lancements dans chaque variante.
> - Jeu, sur ce commit du moteur : zone de la traînée de `Beginner-start` à 0 pixel blanc (2354 avant) ; image entière 791 et 760 pixels blancs pour `Beginner-start` et `Expert-start`, comme le cas sans normal map du vérificateur (792 et 760). Mesure de scène, mipmaps et peinture inchangées. Pointeur du sous-module avancé dans ce commit. Un `verifier` frais revoit F1.
> - `verifier` frais sur la correction : **CONFIRMED** sur les cinq affirmations. Zone de la traînée à 0 pixel blanc ; les pixels blancs restants de l'image entière sont, pixel pour pixel, ceux du cas sans normal map, tous sur les rails métalliques et une tête de lampadaire, aucun sur le terrain ; tests du moteur, démo PBR et mesures du jeu reproduits.
> - Disposition, remarque A1 (P3) du vérificateur : la validation écrite du plan moteur visait le nombre de pixels blancs de l'image entière au niveau d'avant le PBR (50 et 246) ; elle n'est pas tenue (791 et 760). Cette cible était mal choisie, puisque le PBR sous la lumière d'avant S1g ajoute des reflets sur les rails et les lampadaires sans rapport avec F1 ; la preuve retenue est la zone de la traînée et l'identité avec le cas sans normal map. Décision prise en mode AUTO, à confirmer par l'auteur.
> - A2 (P4) : ces reflets des rails et des lampadaires relèvent du réglage de la lumière de S1g, qui les mesurera.
> - S1f close.

---

## Phase 11 — Monde de course (S1g)

Décision déjà prise : ADR-0018 (le monde de course est rendu en lumière linéaire HDR avec des matériaux PBR, acceptée le 2026-11-16 sur l'autre PC, présente ici depuis S1b). Cette tranche l'applique au monde construit ici ; pas de nouvelle ADR. Sources à porter, lisibles par `git show` : `09e4a9f` (pipeline couleur, mode `legacy` du banc, ciel en phase de scène), `037f5a5` (lumière clé π, ambiant par le cube du ciel), `d66c210` (lumière unique de l'original ; sa partie « ambiant par matériau » est sans objet en `lit-pbr`), `d40b020` (ombres douces et teintées ; sa partie « qui projette, qui reçoit » est déjà faite ici en S1a), `a18b364` (tangentes et normal map de la route).

État vérifié (2026-11-17, jeu `4f72249`, moteur `8a3933b36`) :

- `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` : trois lumières directionnelles (`LegacyDirectionalLights` : clé 0,92 vers (-0,5265 ; -0,5736 ; -0,6275), remplissage 0,71, arrière 0,36), ombres `Resolution` 4096, `MaxDistance` 150, `DepthBias` 0,001, `NormalBias` 0,1, sans couleur d'ombre, sans filtre, sans fondu, sans `DepthRange` ; aucun réglage d'ambiant (le défaut du moteur est `AmbientSource.Auto`, `WorldEnvironmentSettings.cs:56`) ; aucun réglage de `ColorPipeline`.
- Autre PC (`origin/remaster`, même fichier) : une seule lumière blanche vers `normalize(-8500, -15000, -7250)`, intensité π, `SpecularColor` (235, 235, 235) ; `AmbientSource.Auto`, `AmbientColor` blanc, `AmbientIntensity` 1, `SpecularIntensity` 1 ; ombres 4096, `MaxDistance` 100, `FadeDistance` 15, `DepthRange` 150, `FilterRadius` 3, `Mode` `MultiplyLitColor`, `ShadowColor` (0,25 ; 0,26 ; 0,27), `DepthBias` 0,001, `NormalBias` 0,1 ; `LinearHdr`, `AcesApprox`, exposition 1, bloom et SSAO coupés, FXAA, sauf avec `--color-pipeline legacy`.
- Le moteur a déjà tout ce qu'il faut : `IScenePhaseViewPipeline`, `ShadowSettings.ShadowColor`, `FilterRadius`, `FadeDistance`, `DepthRange`, `ShadowMode.MultiplyLitColor`, `WorldEnvironmentSettings.SpecularIntensity`, `RenderStats.FxaaPasses`, `frame.Environment.LinearColorActive` ; `LegacySkyCube.fx` déclare `ColorPipelineIsLinear` et décode sa couleur (`DecodeColor4`). Aucun changement du moteur n'est prévu.
- `RacingGameCasaEngine/Bootstrap/LegacySkyCubeViewPipeline.cs` n'implémente que `IViewRenderPipeline` et ne lie pas `ColorPipelineIsLinear`. `RacingGameCasaEngineGame.cs:303-307` donne déjà le cube du ciel au monde de course comme `SpecularEnvironmentCubemap`.
- `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs` : la route (`CreateRoadModel`, `CreateRoadVertex`) et les tunnels (`CreateTunnelModel`, `CreateTunnelVertex`) sont en `VertexPositionNormalTexture`, sans tangente ; le moteur n'active la normal map que si le maillage a des tangentes (`RenderFeatureResolver.cs:177-183`), donc la normal map cuite de `Material.Track.RoadTunnel` est ignorée aujourd'hui. Rails, colonnes et terrain ont déjà leurs tangentes.
- Original (`RacingGameOriginal/Core/Tracks/TrackVertex.cs`, `Track.cs`) : la route est en `TangentVertex(position, uv, up, right)` avec le matériau `("Road", "RoadNormal")` ; le tunnel prend `LeftTangentVertex` et `RightTangentVertex` (normale et tangente retournées, `Track.cs:547-550`) et `TunnelTop…SideTangentVertex` (normale `-up`, tangente `-right`) : les quatre sommets d'un point de tunnel ont la normale `-up` et la tangente `-right`.
- `RaceMaterialRecipes.ForRoad` ne prend pas de normal map (`Material.Track.Road`, `normal_texture` nul) ; `RoadNormal.png` est dans `RacingGameCasaEngine/Content/Textures`.
- Cinq recettes des matériaux de piste générés (`ForRoad`, `ForGround`, `ForTerrain`, `ForGuardRail`, `ForRoadColumn`, `RaceMaterialRecipes.cs:119`, `134`, `154`, `170`, `186`) gardent une émissive de 0,008 à 0,03 ; `ForRoadTunnel` n'en a pas (`RoadTunnel.material`, émissive nulle) ajoutée par le portage ; l'original n'en a pas (`Track.cs`, `Material(...)` sans émissive), et l'autre PC l'a retirée (`d66c210`, puis ses matériaux PBR de piste sans émissive). Les matériaux importés gardent l'émissive de leur présentation, comme là-bas.
- Le carrousel de sélection a son propre monde et ses lumières (`UI/CarSelectionCarousel.cs`) ; l'autre PC l'a passé à l'ambiant par le cube du ciel et à une lumière multipliée par π avec la voiture (sa T3.1). Hors de cette tranche : S4 (voiture trop blanche du carrousel).
- Référence du banc avant S1g : aucune capture complète n'existe sur le moteur `8a3933b36` (`s1f/cand-a` et `cand-b` sont d'avant la correction de GGX) ; T11.1 en prend une.

Retour arrière de S1g : relever `git rev-parse HEAD` avant T11.1 et le noter sous T11.1 ; annuler les commits de S1g sur `chantier/rgce-pbr-handover` seulement. Sur demande de l'auteur seulement.

Budget de S1g : T11.3, au plus deux tours de recuisson ; deux passes de correction après vérification.

Règles communes : un seul lancement du jeu à la fois, jamais pendant un build ; rebuild du jeu avant tout lancement qui suit une recuisson ; sorties sous `artifacts/pbr-handover/s1g/` ; aucun contournement d'un défaut du moteur dans le jeu (arrêt et question).

### ✅ T11.1 — Pipeline couleur linéaire, mode `legacy` du banc, ciel en phase de scène

- Objectif : le monde de course est rendu en `LinearHdr` par défaut ; `--color-pipeline legacy` garde exactement le rendu d'aujourd'hui pour le banc. Lumière, ombres et matériaux ne changent pas dans cette tâche.
- Avant tout changement, sur `HEAD` (moteur `8a3933b36`), références d'avant S1g : rejeu des vues de l'original vers `artifacts/pbr-handover/s1g/base` ; captures d'interface vers `s1g/base-ui` et d'audit vers `s1g/base-audit`, par les mêmes commandes qu'en T10.2. Sondes contre l'original sur `s1g/base` (`--probes scripts/TrackCaptureCompare/probes.json --no-terrain-detail-correction`) : critères 1 à 4 notés sous T11.1 ; ce sont les valeurs de référence de T11.2 et T11.5 (les 21 sur 33 de S1f sont d'avant la correction de GGX).
- Fichiers : `RacingGameCasaEngine/Bootstrap/RaceLaunchOptions.cs` (`UseLinearColorPipeline`, vrai par défaut), `Program.cs` (`--color-pipeline legacy|linear`, valeur inconnue refusée avec un message), `RacingGameCasaEngineGame.cs` (propriété interne lue par la fabrique), `Worlds/RaceWorldFactory.cs` (`ConfigureLinearColorPipeline` : `LinearHdr`, `AcesApprox`, exposition 1, bloom et SSAO coupés, FXAA ; `MarkDirty`), `Bootstrap/LegacySkyCubeViewPipeline.cs` (implémente `IScenePhaseViewPipeline` : `SupportsScenePhase`, `RenderScene` qui dessine le ciel puis la scène du pipeline enveloppé ; lie `ColorPipelineIsLinear` depuis `frame.Environment.LinearColorActive`), `Bootstrap/ReferenceViewCaptureValidator.cs` seulement si sa ligne existante `[CaptureBench] view <piste>-<vue> pipeline <legacy|linear> fxaaPasses <n>` (`:313-325`, portée en S1c) a besoin de la propriété du jeu. `scripts/TrackCaptureCompare/README.md` documente déjà l'option (`:23-28`) : relu, corrigé seulement s'il diffère du code.
- Validation :
  - build du jeu à 0 erreur ; `--smoke-frontend` en succès ;
  - deux rejeux `legacy` (`s1g/t1-legacy-a`, `-b`) : 15 lignes `pipeline legacy fxaaPasses 0` chacun ; comparaison avec `s1g/base` à 0,00 de moyenne par canal sur les 15 vues ; mesure de scène égale octet pour octet à celle de `base` ;
  - deux rejeux `linear` (`s1g/t1-linear-a`, `-b`) : 15 lignes `pipeline linear fxaaPasses 1` chacun ; répétabilité `a` contre `b` à 0,00 de moyenne ; aucun `[Warning]`, `[Error]` ni exception au journal (en particulier aucun avertissement du moteur sur un pipeline de vue sans `IScenePhaseViewPipeline`) ;
  - ciel décodé : sur `Beginner-overview`, carré 9x9 centré en (640, 30), la valeur `linear` mesurée est à 4 niveaux au plus, par canal, de `encode(ACES(decode(L)))` où `L` est la valeur `legacy` au même endroit (calcul par script, formule ACES approchée et encodage sRGB du moteur lus dans ses shaders et cités) ;
  - luminance moyenne des 15 vues notée dans les deux modes.
- Arrêts : `legacy` différent de `base` ; un avertissement de phase de scène ; ciel hors tolérance.
- Commit : `feat(racing-casa): render the race world in linear HDR and keep a legacy mode for the bench`

> Validation (2026-11-17), jeu relevé à `53297f7e5ee35a6ea162a9773a2471db18a62067` avant la tâche (point de retour arrière de S1g), moteur `8a3933b36`. Sorties sous `artifacts/pbr-handover/s1g/`.
> - Références d'avant S1g, prises sur ce `HEAD` : `base` (rejeu des vues, sortie 0, 15 lignes `pipeline legacy fxaaPasses 0`, aucun `[Warning]` ni `[Error]`, trois `scene-measure-*.txt` égaux octet pour octet à ceux de `s1f/cand-a`), `base-ui` (10 captures), `base-audit` (30 captures).
> - Sondes contre l'original sur `base` (`compare-base-probes/probes.md`), valeurs de référence de T11.2 et T11.5 : critère 1 : 10 sur 34 ; critère 2 : 0 sur 4 ; **critère 3 : 21 sur 33** ; critère 4 : 0 sur 3. Rapports ombre sur lumière, original puis port : terrain 0,284 puis 1,038 ; ville 0,276 puis 1,037 ; route 0,228 puis 0,804 ; terrain 0,267 puis 1,015. Luminance moyenne des 15 vues : original 96,3, port 55,9. Rapport `road-lit-1` / `terrain-lit-1` de `Beginner-start` : original 0,348, port 0,815. Pixels blancs de `Beginner-start` et `Expert-start` : 728 et 691 avec les trois canaux à 255 ; 791 et 760 avec les trois canaux à 250 au moins, qui est le décompte du vérificateur de S1f (3145 et 2080 sur `s1f/cand-a` par ce décompte, relu ici : 3039 et 1992 à 255 exactement, 3145 et 2080 à 250 au moins).
> - Builds du jeu à 0 erreur, 0 avertissement ; `--smoke-frontend` : sortie 0, « Smoke validation completed successfully », aucun `[Warning]` ni `[Error]` au journal.
> - Quatre rejeux. `legacy` (`t1-legacy-a`, `-b`) : sortie 0, 15 lignes `pipeline legacy fxaaPasses 0` chacun ; comparaison avec `base` à 0,00 de moyenne par canal sur les 15 vues (13 fichiers sur 15 égaux par empreinte, les deux autres à 0,00 : le même léger non-déterminisme que les autres rejeux) ; les trois `scene-measure-*.txt` égaux octet pour octet à ceux de `base`. `linear` (`t1-linear-a`, `-b`) : sortie 0, 15 lignes `pipeline linear fxaaPasses 1` chacun ; répétabilité `a` contre `b` à 0,00 de moyenne (12 fichiers sur 15 égaux par empreinte) ; mesure de scène égale à celle de `base` ; aucun `[Warning]`, `[Error]` ni exception dans les quatre journaux, donc aucun avertissement de phase de scène.
> - Ciel décodé, `Beginner-overview`, carré 9x9 centré en (640, 30) (`t1-sky-check.txt`) : valeur `legacy` moyenne (138,05 ; 178,99 ; 197,22), valeur `linear` mesurée (165,54 ; 200,28 ; 210,63), valeur attendue `encode(ACES(decode(L)))` (165,58 ; 200,31 ; 210,65) : écart 0,04 / 0,03 / 0,02 niveau sur la moyenne, au plus 0,79 / 0,68 / 0,54 pixel par pixel sur les 81 pixels, sous la tolérance de 4 niveaux. Formules lues dans les shaders du moteur : décodage `pow(c, 2.2)` (`ColorPipeline.fxh`, `DecodeColor`), ACES approchée `c (2.51 c + 0.03) / (c (2.43 c + 0.59) + 0.14)` puis `pow(saturate(c), 1 / 2.2)` (`Tonemap.fx`, `PS_AcesApprox` et `EncodeForDisplay` ; l'encodage du moteur est la loi de puissance 2,2, pas la courbe sRGB exacte), exposition 1.
> - Luminance moyenne des 15 vues contre l'original (`compare-t1-*-nopost/report.md`) : `legacy` 55,9 ; `linear` 80,0 ; original 96,3. Sondes en `linear` : critère 1 : 5 sur 34 ; critère 2 : 0 sur 4 ; critère 3 : 21 sur 33 ; critère 4 : 0 sur 3 ; rapport `road-lit-1` / `terrain-lit-1` 0,733 ; pixels blancs de `Beginner-start` et `Expert-start` : 0 et 0 (255 comme 250 au moins). En `legacy`, les sondes et les mesures sont celles de `base`.
> - Aucun changement de `ReferenceViewCaptureValidator.cs` (sa ligne `[CaptureBench] view` lit déjà le mode dans le monde) ni de `scripts/TrackCaptureCompare/README.md` (relu : il décrit déjà l'option et les deux modes tels que le code les fait).

### ✅ T11.2 — Lumière de l'original, ambiant par le ciel, ombres douces et teintées

- Objectif : une seule lumière, celle de l'original, à l'échelle du PBR ; les réglages d'ambiant valent aujourd'hui leurs défauts du moteur ; ils sont écrits explicitement pour que le monde ne dépende pas de ces défauts ; l'ambiant diffus et les reflets viennent du cube du ciel ; les ombres ont la couleur, la douceur et la portée de l'autre PC. Dans les deux modes du pipeline couleur.
- Fichiers : `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` seulement (les trois lumières remplacées par la lumière clé `Light.Key` : direction `normalize(-8500, -15000, -7250)`, blanche, intensité π, `SpecularColor` (235, 235, 235), ombres si l'option `Shadows` est active ; `AmbientSource.Auto`, `AmbientColor` blanc, `AmbientIntensity` 1, `SpecularIntensity` 1 ; ombres : `MaxDistance` 100, `FadeDistance` 15, `DepthRange` 150, `FilterRadius` 3, `Mode` `MultiplyLitColor`, `ShadowColor` (0,25 ; 0,26 ; 0,27), `DepthBias` et `NormalBias` inchangés, `Resolution` 4096) ; les commentaires de constantes repris de l'autre PC, avec leurs sources dans l'original (`LensFlare.cs`, `ShadowMap.fx`, `ShadowMapShader.cs`) vérifiées par `rg` dans `RacingGameOriginal` avant d'être citées.
- Validation :
  - build ; `--smoke-frontend` ;
  - rejeux `legacy` et `linear` (`s1g/t2-legacy`, `s1g/t2-linear`, deux chacun pour la répétabilité) ; mesure de scène inchangée ;
  - sondes contre l'original (`--probes scripts/TrackCaptureCompare/probes.json --no-terrain-detail-correction`) dans les deux modes : critère 3 (classe ombre ou lumière) au moins égal à celui de `s1g/base` noté en T11.1 ; en `legacy`, rapport ombre sur lumière du terrain des deux vues du critère 2 à 0,03 au plus de l'original (0,284 et 0,267) ; tous les critères notés, sans autre cible (couleurs non comparables à l'original en dehors de ce rapport, comme sur l'autre PC) ;
  - luminance moyenne des 15 vues et rapport « route éclairée / terrain éclairé » (`Beginner-start`, `road-lit-1` / `terrain-lit-1`) notés dans les deux modes, avec ceux de `base` et de l'original ;
  - pixels blancs saturés (les trois canaux à 255) de `Beginner-start` et `Expert-start` comptés et notés (remarque A2 de S1f).
- Arrêts : critère 3 sous celui de `s1g/base` ; rapport du terrain hors tolérance sans cause trouvée ; changement de la mesure de scène.
- Commit : `feat(racing-casa): light the race world as the original with image-based ambient and soft tinted shadows`

> Validation (2026-11-17), jeu sur `93d55e8` (T11.1) plus les changements de cette tâche, moteur `8a3933b36`. Sorties sous `artifacts/pbr-handover/s1g/` (`t2-legacy-a`, `-b`, `t2-linear-a`, `-b`, `compare-t2-*`, `meas-t2-*.txt`).
> - Build à 0 erreur, 0 avertissement ; `--smoke-frontend` : sortie 0, « Smoke validation completed successfully », aucun `[Warning]` ni `[Error]`. Quatre rejeux, sortie 0 : 15 lignes `pipeline legacy fxaaPasses 0` (`legacy`) ou `pipeline linear fxaaPasses 1` (`linear`) chacun, aucun `[Warning]`, `[Error]` ni exception. Mesure de scène : les douze `scene-measure-*.txt` (trois pistes, quatre rejeux) sont égaux octet pour octet à ceux de `base`. Répétabilité `a` contre `b` : 0,00 de moyenne par canal sur les 15 vues dans les deux modes (12 fichiers égaux par empreinte en `legacy`, 13 en `linear`).
> - Sondes contre l'original (`--probes ... --no-terrain-detail-correction`), valeurs de `s1g/base` entre parenthèses (T11.1) :
>   - `legacy` : critère 1 : 0 sur 34 (10) ; critère 2 : 2 sur 4 (0) ; **critère 3 : 27 sur 33 (21), donc au moins égal à `base`** ; critère 4 : 0 sur 3 (0). Rapports ombre sur lumière, original puis port : terrain 0,284 puis 0,291 ; ville 0,276 puis 1,865 ; route 0,228 puis 0,835 ; terrain 0,267 puis 0,275. **Les deux rapports du terrain sont à 0,007 et 0,008 de l'original** (tolérance 0,03).
>   - `linear` : critère 1 : 1 sur 34 ; critère 2 : 0 sur 4 ; **critère 3 : 27 sur 33** ; critère 4 : 3 sur 3. Rapports : terrain 0,284 puis 0,183 ; ville 0,276 puis 3,127 ; route 0,228 puis 0,902 ; terrain 0,267 puis 0,141. Aucune cible en `linear` : la teinte d'ombre multiplie la couleur éclairée en lumière linéaire, avant la tonalité et l'encodage, ce qui ne donne pas les mêmes niveaux d'écran que l'original ; seul le rapport du terrain en `legacy` était cible.
> - Luminance moyenne des 15 vues (`compare-t2-*-nopost/report.md`) : original 96,3 ; `base` 55,9 ; `legacy` 142,6 ; `linear` 133,5. Rapport `road-lit-1` / `terrain-lit-1` de `Beginner-start` : original 0,348 ; `base` 0,815 ; `legacy` 0,766 ; `linear` 0,806. Les couleurs ne sont pas comparables à l'original en dehors du rapport du terrain, comme sur l'autre PC : l'état d'avant (`base`) était plus sombre, celui-ci est plus clair que l'original.
> - Pixels blancs saturés (trois canaux à 255, puis à 250 au moins comme le décompte du vérificateur de S1f) : `legacy` `Beginner-start` 6808 puis 7604, `Expert-start` 6814 puis 7151 (`base` : 728 puis 791 et 691 puis 760) ; `linear` 0 et 0 pour les deux vues (en 255 comme en 250 au moins). En `legacy`, la lumière de niveau PBR (intensité π) est calculée dans l'espace d'affichage, sans tonalité : elle sature ; c'est pourquoi la vérification des reflets des rails et des lampadaires (remarque A2 de S1f) se fait en `linear`, le mode du jeu.
> - Écart au plan : l'énoncé « les réglages d'ambiant valent aujourd'hui leurs défauts du moteur » est exact pour la source (`Auto`), l'intensité d'ambiant (1) et celle du spéculaire (1), mais pas pour la couleur d'ambiant : le défaut du moteur est 0,05 (`WorldEnvironmentSettings.ResetToDefaults`), et `LitPbr.fx` multiplie l'irradiance du ciel par `EnvironmentAmbientColor`. La couleur blanche écrite ici change donc l'ambiant du monde (il était à 5 % de l'irradiance) . Sa part dans le passage de 55,9 à 142,6 (`legacy`) et 133,5 (`linear`) de luminance n'est pas isolée par un rejeu à part. Constaté, non réglé ici.

### ✅ T11.3 — Tangentes de la route et des tunnels, normal map de la route, émissive retirée

- Objectif : la route et les tunnels ont des tangentes, comme dans l'original, ce qui active leurs normal maps ; la route reçoit `RoadNormal` ; les matériaux de piste générés perdent l'émissive que l'original n'a pas.
- Fichiers : `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs` (route : `VertexPositionNormalTextureTangent`, tangente = vecteur droite du point converti par la conversion de direction du port, normalisé comme la normale l'est aujourd'hui, `w` = +1 ; tunnel : même format, tangente = l'opposé de ce vecteur, `w` = +1, normale inchangée ; positions, UV, indices inchangés) ; `RacingGame.Gameplay/Import/RaceMaterialRecipes.cs` (`ForRoad` prend une normal map, nom en constante comme les autres ; émissive retirée de `ForRoad`, `ForGround`, `ForTerrain`, `ForGuardRail`, `ForRoadColumn`) ; `scripts/RgceAssetCooker/CookPlan.cs` (passe `RoadNormal.png` à `ForRoad`) ; les `.material` de piste recuits, la `.texture` de `RoadNormal` si le cuiseur la crée, `Content/AssetInfos.json` ; tout appel de ces fabriques que le build désigne.
- Validation :
  - builds du jeu et du cuiseur à 0 erreur ; cuiseur `--check` et `--verify` sans écart, second passage sans écriture ; `rg -n "EmissiveColor" RacingGame.Gameplay/Import/RaceMaterialRecipes.cs` ne désigne plus aucune recette de piste ; après recuisson, `git status` ne montre que `Materials/Track/Road.material` (émissive et normal map), `Ground.material`, `Terrain.material`, `GuardRail.material`, `RoadColumn.material`, la `.texture` de `RoadNormal` et `AssetInfos.json` ; `RoadTunnel.material` est inchangé octet pour octet ;
  - test ou contrôle par script, sur les trois pistes (Beginner, Advanced, Expert) : nombre de sommets de route et de tunnel noté par piste, au moins un sommet de tunnel au total (Expert en a : capture `audit-Expert-helper-tunnel`) ; pour chaque sommet de route et de tunnel, la tangente est unitaire, orthogonale à la normale à 1e-4 près, et égale au vecteur attendu ; positions, UV et indices égaux à ceux d'avant (même script avant et après) ;
  - rejeux `legacy` et `linear` : mesure de scène inchangée ; sondes, luminance, rapport route sur terrain et pixels blancs notés comme en T11.2 ; une vue de route (`Beginner-start`) et la capture d'audit `audit-Expert-helper-tunnel` regardées en recadrage, contre celles de `s1g/base` et `s1g/base-audit`, le relief de la normal map visible et sans traînée.
- Arrêts : un écart du cuiseur sans cause ; une traînée ou un aplat saturé nouveau sur la route ou le tunnel ; besoin de toucher le moteur.
- Commit : `feat(racing-casa): give the road and tunnels their tangents and the road its normal map`

> Validation (2026-11-17), un tour de recuisson sur deux, jeu sur `f0ea65f` (T11.2) plus les changements de cette tâche, moteur `8a3933b36`. Sorties sous `artifacts/pbr-handover/s1g/` (`t3-*`).
> - Builds du jeu (`RacingGameCasaEngine.csproj`, Debug) et du cuiseur à 0 erreur. `rg -n "EmissiveColor" RacingGame.Gameplay/Import/RaceMaterialRecipes.cs` ne désigne plus que les trois recettes de présentation importée (`ForScenery`, `ForCar`, `ForPlate`, lignes 55, 86, 105) : aucune recette de piste.
> - Cuiseur : avant recuisson `--verify` donnait 8 écarts (normal map de la route, émissive des cinq matériaux, `RoadNormal.texture` et son entrée de catalogue absentes), preuve que le contrôle mord ; recuisson : 6 fichiers écrits, 257 inchangés, catalogue 608 -> 609 entrées, `--verify` 0 écart (98 matériaux, 2744 valeurs, 109 textures) ; `--check` : 0 manquant ; second passage : 0 écrit, 263 inchangés, 0 écart. `git status` après recuisson : `Materials/Track/Road.material` (normal map et émissive), `Ground.material`, `Terrain.material`, `GuardRail.material`, `RoadColumn.material`, `Textures/RoadNormal.texture` (nouveau) et `AssetInfos.json` (une entrée) ; `RoadTunnel.material` inchangé octet pour octet.
> - Contrôle de géométrie (outil jetable hors dépôt, qui rejoue `ComputeRaceLayout`, `CreateRoadModel` et `CreateTunnelModel` sur les trois pistes ; `t3-geometry-before.txt` puis `t3-geometry-after.txt`) : sommets de route 6660 / 10860 / 22330 et de tunnel 404 / 860 / 896 (Beginner / Advanced / Expert), soit 2160 sommets de tunnel au total (les trois pistes ont un tunnel). Avant : aucune tangente ; empreinte (positions, normales, UV, indices) de chaque maillage identique avant et après sur les six maillages. Après : pour chaque sommet, tangente unitaire (écart de longueur à 1 au plus 1,8e-7), orthogonale à la normale (au plus 9,9e-8), égale au vecteur attendu (route : droite du point converti, `(x, z, -y)` normalisé ; tunnel : son opposé ; écart maximal 0), `w` = 1 ; 0 échec.
> - Quatre rejeux, sortie 0 (`t3-legacy-a`, `-b`, `t3-linear-a`, `-b`) : lignes `pipeline` attendues (15 `legacy fxaaPasses 0` ou 15 `linear fxaaPasses 1`), aucun `[Warning]`, `[Error]` ni exception ; `--smoke-frontend` sortie 0 sans avertissement ; les douze `scene-measure-*.txt` égaux octet pour octet à ceux de `base` ; répétabilité `a` contre `b` à 0,00 de moyenne par canal (14 fichiers égaux sur 15 en `legacy`, 13 en `linear`).
> - Sondes (valeurs de T11.2 entre parenthèses) : `legacy` critère 1 : 0 sur 34 (0) ; critère 2 : 2 sur 4 (2) ; critère 3 : 27 sur 33 (27) ; critère 4 : 0 sur 3 (0) ; rapports du terrain 0,282 et 0,263 (0,291 et 0,275), à 0,002 et 0,004 de l'original. `linear` critère 1 : 1 sur 34 (1) ; critère 2 : 0 sur 4 (0) ; critère 3 : 27 sur 33 (27) ; critère 4 : 3 sur 3 (3) ; rapports du terrain 0,183 et 0,141 (identiques).
> - Luminance moyenne des 15 vues : `legacy` 140,5 (142,6), `linear` 133,5 (133,5) ; original 96,3. Rapport `road-lit-1` / `terrain-lit-1` de `Beginner-start` : `legacy` 0,756 (0,766), `linear` 0,806 (0,806) ; original 0,348. Pixels blancs de `Beginner-start` et `Expert-start` (255 exactement, puis 250 au moins) : `legacy` 6062 et 6500, puis 6824 et 6800 (T11.2 : 6808 et 6814, puis 7604 et 7151) ; `linear` 0 et 0, 0 et 0.
> - Regard en recadrage : `Beginner-start` (route, 300-800 x 380-720) de `base`, `t3-legacy-a` et `t3-linear-a` côte à côte : le relief de la normal map (grain, fissures, joints de dalles) est visible sur la route dans les deux modes, plat sur `base` ; aucune traînée blanche ni aplat saturé nouveau. `audit-Expert-helper-tunnel` de `base-audit`, `t3-audit-legacy` et `t3-audit-linear` : le tunnel, noir uni sur `base-audit`, montre sa texture et le relief de sa normal map (appareil de briques en `legacy`, grain en `linear`), sans traînée ni aplat saturé. Le tunnel reste très sombre (point O1 de S3, hors de cette tâche).
> - Écart au plan : aucun écart de fichiers ; la validation de géométrie passe par un outil jetable hors dépôt (assembly nommé `RgceAssetCooker` pour lire les membres internes de `RacingGame.Gameplay`), non versionné.


### ✅ T11.4 — Banc complet et références

- Objectif : la référence du port après S1g, dans les deux modes.
- Étapes : banc complet vers `artifacts/pbr-handover/s1g/` comme en T10.2 (deux rejeux par mode, `nopost`, `full`, sondes, répétabilité, comparaison avec `base` et avec `s1f/cand-a`), nouvelles références d'audit et d'interface ; section « After the race world lighting » de `docs/racinggame-3d-capture-bench-local-baseline.md` générée par script, qui dit quel mode chaque chiffre mesure ; `scripts/TrackCaptureCompare/README.md` si le mode change une commande.
- Validation : les chiffres de la section sont ceux des sorties (relus par script) ; interface, contre `s1g/base-ui` : `splash`, `main-menu`, `highscores`, `options`, `help`, `car-selection` et `track-selection` à 0,00 (le carrousel n'est pas touché par S1g) ; `race-hud`, `pause` et `race-finished` notés (le monde de course change dessous) ; audit comparé à `s1g/base-audit` et noté.
- Commit : `docs(racing-casa): record the capture bench after the race world lighting`

> Validation (2026-11-17), jeu sur `8f1471b` (T11.3), contenu recuit, moteur `8a3933b36`, build refait avant les lancements. Sorties sous `artifacts/pbr-handover/s1g/`.
> - Banc complet : deux rejeux par mode (`legacy-a`, `-b`, `linear-a`, `-b`), tous de sortie 0, 15 lignes `pipeline` attendues chacun, aucun `[Warning]`, `[Error]` ni exception ; comparaisons `nopost`, `full` et sondes par mode (`compare-<mode>-nopost`, `-full`, `-probes`) ; répétabilité (`legacy` : 14 fichiers égaux sur 15, `linear` : 13 sur 15, écarts moyens de 0,00) ; comparaison avec `base` (`legacy` : de 48,54 à 111,27 ; `linear` : de 51,42 à 98,22, moyenne des trois canaux) et avec `s1f/cand-a` ; nouvelles références d'interface (`ref-ui`, 10 captures) et d'audit (`ref-audit`, 30 captures), prises dans le mode par défaut du jeu (`linear`).
> - Section « After the race world lighting » du doc générée par script (`gen_section_s1g.py`, hors dépôt) : les tableaux viennent des fichiers de sortie et des `.material` cuits ; chaque chiffre dit son mode (`legacy` ou `linear`). Relecture par un second script, qui relit en texte brut les `report.md`, `probes.md` et `report.txt` : 78 valeurs (images `nopost` et `full` des deux modes avec les arêtes, critères de sondes, interface) comparées au doc, 0 différence.
> - Mesure de scène : les douze `scene-measure-*.txt` (trois pistes, deux modes, deux rejeux) égaux octet pour octet à ceux de `base`. Mipmaps et lignes `car paint slots` (1 peint, 10 non peints) comme avant.
> - Interface contre `base-ui` : `splash`, `main-menu`, `highscores`, `options`, `help`, `car-selection` et `track-selection` à 0,00 (le carrousel n'est pas touché) ; `race-hud` 62,77 / 69,11 / 68,43, `pause` 18,36 / 19,97 / 19,54, `race-finished` 56,24 / 62,67 / 60,56 (le monde de course change dessous : lumière et pipeline couleur). Audit contre `base-audit` : au plus 110,816 (`audit-Advanced-sector-80`), au moins 24,087 (`audit-Beginner-helper-tunnel`).
> - `scripts/TrackCaptureCompare/README.md` : aucune commande ne change (l'option `--color-pipeline` y est déjà décrite telle que le code la fait) ; non modifié.
> - Chiffres de référence de S1g : critère 3 des sondes 27 sur 33 dans les deux modes (21 sur 33 pour `base`) ; luminance moyenne `legacy` 140,5, `linear` 133,5, original 96,3, `base` 55,9 ; rapports ombre sur lumière du terrain en `legacy` 0,282 et 0,263 (original 0,284 et 0,267) ; pixels blancs de `Beginner-start` et `Expert-start` en `linear` : 0 et 0 (`legacy` : 6062 et 6500 à 255 exactement).


### ✅ T11.5 — Vérification indépendante de S1g

- Objectif : un `verifier` frais confirme ou réfute : `legacy` reproduit `base` ; `linear` est actif, répétable, avec le ciel décodé ; la lumière, l'ambiant et les ombres sont ceux de l'autre PC ; les tangentes de la route et du tunnel sont justes et activent les normal maps ; l'émissive des matériaux de piste générés a disparu et rien d'autre n'a changé dans le contenu ; la mesure de scène et le critère 3 des sondes ne régressent pas par rapport à `s1g/base` ; les chiffres du doc sont ceux des sorties.
- Étapes et verdicts : comme T5.4.
- Commit : `docs(ai-agent): record the verification of the race world lighting`

> Validation (2026-11-17), sorties dans `artifacts/pbr-handover/s1g/verify/`.
> - `verifier` frais : **CONFIRMED** sur les sept affirmations, par ses propres lancements sur `ae457b8` (build refait, 0 erreur) : `legacy` à 0,00 de `base` et mesure de scène égale octet pour octet ; `--color-pipeline foo` refusé avec un message ; `linear` par défaut, 15 lignes `fxaaPasses 1`, répétable à 0,00, journal propre ; ciel à 0,79 niveau au plus de `encode(ACES(decode(L)))` ; valeurs de lumière, d'ambiant et d'ombres relues et égales à celles d'`origin/remaster` ; tangentes de la route et des tunnels conformes à l'original (tunnels texturés sur les trois pistes, relief de la route visible sans traînée) ; contenu changé limité aux cinq matériaux, `RoadNormal.texture` et `AssetInfos.json`, cuiseur `--verify` et `--check` propres ; critère 3 des sondes 27 sur 33 dans les deux modes (21 sur 33 sur `base`), rapports du terrain en `legacy` 0,282 et 0,263 ; 165 cellules du doc relues, 0 écart. Interface : sept écrans hors course à 0,00 contre `base-ui`, les dix à 0,00 contre `ref-ui` ; `--smoke-frontend` en succès.
> - Limite notée par le vérificateur : le commit `93d55e8` n'a pas été rejoué seul (worktree impossible sans git dans `MGUI` et `NvgSharp`) ; l'affirmation 1 repose sur les sorties de T11.1 et sur la lecture du code, confirmée par `legacy` sur `HEAD`.
> - A1 (P3) : la luminance moyenne monte à 140,5 (`legacy`) et 133,5 (`linear`) pour 96,3 dans l'original ; rapport route sur terrain 0,806 pour 0,348. Valeurs voulues par le plan (couleur d'ambiant blanche, comme l'autre PC), mais la phrase de T11.2 « les réglages d'ambiant valent aujourd'hui leurs défauts du moteur » était fausse pour la couleur : le défaut est 0,05 (`WorldEnvironmentSettings.cs:94`) et `LitPbr.fx` multiplie l'irradiance du ciel par elle ; l'ambiant passe de 5 % à 100 % de l'irradiance. Erreur de l'étude du plan, pas de l'exécution. Question à l'auteur : O13.
> - A2 (P4) : valeur attendue du ciel notée en T11.1 à 0,05 niveau près de celle du vérificateur. A3 (P4) : `legacy` sature (6062 et 6500 pixels à 255), attendu sans tonemapping. Sans suite.
> - S1g close.

---

## Phase 12 — Dessous de la route et sol de la ville (S1h)

Sujets faits sur l'autre PC seulement (`0ac894d`, puis `d0db72f` pour leurs matériaux PBR) : portés dans l'architecture d'ici, en générateurs et en matériaux cuits. Les tunnels de `0ac894d` sont faits ici depuis l'ADR-0019, alignés en S1a et munis de tangentes en S1g : hors de cette tranche.

État vérifié (2026-11-17, jeu `4cf6899`) :

- Original, dessous de la route (`RacingGameOriginal/Core/Tracks/Track.cs:408` et suivantes, `TrackVertex.cs`) : 4 sommets par point de route (`LeftTangentVertex`, `BottomLeftSideTangentVertex`, `BottomRightSideTangentVertex`, `RightTangentVertex`), le bas à `RoadThickness × roadWidth` sous le bord, V à 0, 0,135, 0,865 et 1, U multiplié par `RoadBackHullTextureWidthFactor` (1), 6 triangles par segment comme le tunnel ; matériau `roadBackMaterial` = `("RoadBack", "RoadBackNormal")` ; `Track.GenerateShadow` et `Track.UseShadow` l'ignorent (ni projeté ni reçu). Les constantes exactes sont relues dans l'original par l'exécutant avant usage.
- Original, sol de la ville (`Landscape.cs:900-933`, `PlaneRenderer.cs`) : un seul plan, créé dans le constructeur du `Landscape` (`new Landscape(Level.Beginner)`, `RacingGameManager.cs:402`) au premier objet de `landscapeObjects` dont le nom de modèle contient `hotel` ou `building` (`IsBigBuilding`, `Landscape.cs:71-77`) ; gardé pour les trois pistes livrées (le terrain livré ne change pas). Quatre sommets en bande de triangles, `(±right ±dir) × size + up × 0,1`, `right` = (1, 0, 0), `dir` = (0, 1, 0), `up` = (0, 0, 1), `size` = `min(x, y)` de la position de l'immeuble, UV `±size / 20`, normale `up`, tangente `right`, translation à la position de l'immeuble ; matériau `cityMat` = ambiant 32, diffus 200, spéculaire 128, `("CityGround", "CityGroundNormal")`, technique `DiffuseSpecular20`. Il n'est pas un receveur d'ombre dans l'original ; l'autre PC le fait recevoir parce que le masque d'ombre en espace écran de l'original assombrit ce qui est dessiné à cet endroit (son plan S2b C1).
- Autre PC (`origin/remaster`, `LegacyTrackSceneFactory.RoadStructures.cs`) : même géométrie ; dessous de route avec le vecteur droite brut, ni projeté ni reçu ; sol de ville projeté non, reçu oui ; ancre cherchée une fois dans les objets du fichier de Beginner.
- Ici : `RaceRoadGeneratorComponent` produit la route et les tunnels (matériau des tunnels en constante, non sérialisé : les mondes sauvegardés ne changent pas) ; `RaceTerrainGeneratorComponent` produit le terrain ; aucun dessous de route ni sol de ville. Textures présentes : `RoadBack.png`, `RoadBackNormal.png`, `CityGround.png`, `CityGroundNormal.png` (`RacingGameCasaEngine/Content/Textures`).
- `Material.Track.Ground` est cuit mais utilisé par aucune scène (remarque de S1f) ; sa recette `ForGround` prend la première de `GroundTextureCandidates` = `Landscape.png`, `CityGround.png`, donc la texture du terrain, et pas de normal map.
- Le maillage de la route d'ici prend le vecteur droite normalisé (O11, choix de l'auteur en attente) ; l'original et l'autre PC prennent le vecteur brut pour le dessous.
- La mesure de scène (`scene.json` de l'original) ne compte que les objets placés : ni le dessous de route ni le sol de ville n'y figurent.
- ADR-0019 (« Consequences ») affirme : « Against the original, nothing is missing ». Faux tant que ces deux pièces manquent ; une ADR ne se réécrit pas.

Décisions de la tranche (prises ici en mode AUTO, à confirmer par l'auteur avec l'ADR) :

- H1 : le dessous de la route suit le maillage de la route d'ici (même vecteur droite), pour que les deux maillages se touchent ; si l'auteur tranche O11 pour le vecteur brut, les deux changent ensemble.
- H2 : le dessous de la route est un troisième modèle de `RaceRoadGeneratorComponent`, matériau `Material.Track.RoadBack` en constante (comme les tunnels) ; le sol de la ville est un second modèle de `RaceTerrainGeneratorComponent`, matériau `Material.Track.Ground` en constante, sa recette corrigée pour `CityGround` (famille `Terrain` gardée, règle de T10.1).
- H3 : l'ancre du sol de la ville est une constante en coordonnées de l'original, écrite avec sa dérivation en commentaire, et contrôlée par l'exécutant de deux façons indépendantes (voir T12.2). Un générateur de piste ne lit pas le fichier d'une autre piste.
- H4 : le sol de la ville reçoit les ombres, comme sur l'autre PC.

Retour arrière de S1h : relever `git rev-parse HEAD` avant T12.1 et le noter sous T12.1 ; annuler les commits de S1h sur `chantier/rgce-pbr-handover` seulement. Sur demande de l'auteur seulement.

Budget : au plus deux tours de recuisson par tâche ; deux passes de correction après vérification.

Règles communes : celles de S1g. Sorties sous `artifacts/pbr-handover/s1h/`. Référence d'avant : `s1g/` (référence du port après S1g, mode par défaut `linear`), plus un rejeu `legacy` et `linear` sur `HEAD` vers `s1h/base` avant T12.1, avec sondes.

### ✅ T12.1 — Dessous de la route

- Objectif : chaque piste a le dessous de route de l'original.
- Fichiers : `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.cs` (`CreateRoadBackModel`, `VertexPositionNormalTextureTangent` : bords hauts normale `up`, tangente `right` ; bas normale `-up`, tangente `-right`, comme l'original ; positions des bords hauts égales à celles des bords du maillage de route ; `CastShadows` et `ReceiveShadows` faux), `LegacyTrackSceneFactory.Generators.cs`, `RacingGame.Gameplay/Components/RaceRoadGeneratorComponent.cs` (troisième modèle, matériau en constante), `LegacyTrackSceneFactory.Models.cs` (`RaceTrackMaterialAssets.RoadBack`), `RacingGame.Gameplay/Import/RaceMaterialRecipes.cs` (`ForRoadBack` : `RoadBack.png`, `RoadBackNormal.png`, famille `Road`, culling et échantillonneur de la route, pas d'émissive), `scripts/RgceAssetCooker/CookPlan.cs` (rôle `RoadBack`), contenu recuit (`Materials/Track/RoadBack.material`, `.texture` de `RoadBack` et `RoadBackNormal` si le cuiseur les crée, `AssetInfos.json`).
- Validation :
  - builds du jeu et du cuiseur à 0 erreur ; cuiseur `--verify` et `--check` sans écart, second passage sans écriture ; `git status` du contenu : seulement les fichiers listés ci-dessus ;
  - contrôle par script sur les trois pistes : 4 sommets par point de route, 18 indices par segment ; chaque sommet haut du dessous égal, à 1e-5 près, au sommet de bord correspondant du maillage de route ; chaque sommet bas égal, à 1e-5 près, à ce bord moins `up × RoadThickness (1,0) × roadWidth`, converti et moins l'origine (`TrackVertex.cs:49`, `:171-197`) ; UV de chaque sommet égaux à `(TextureU × 1, V)` avec V = 0, 0,135, 0,865 et 1 pour les sommets 0 à 3 (`Track.cs:33`, `:48`, `:412-437`) ; les 18 indices de chaque segment dans l'ordre du motif de `Track.cs:464-486` (pas de 4), pour garder l'enroulement sous `CullCounterClockwise` ; normales et tangentes unitaires, orthogonales, égales aux vecteurs attendus ; pour chaque contrôle et chaque piste (Beginner, Advanced, Expert), succès ou échec et écart maximal notés ;
  - mesure de scène inchangée (octet pour octet contre `s1h/base`) ; sondes : critère 3 au moins celui de `s1h/base` ;
  - une vue où le dessous est visible (choisie parmi les vues et l'audit, nommée dans la note) regardée en recadrage contre l'original et contre `s1h/base`.
- Arrêts : un écart du cuiseur sans cause ; un dessous qui ne touche pas la route ; changement de la mesure de scène.
- Commit : `feat(racing-casa): build the road underside of the original`

> Validation (2026-11-17), jeu relevé à `4b2d4f867f464513b9c2afc826be25dcc0c076fe` avant la tâche (point de retour arrière de S1h), moteur `8a3933b36`, un tour de recuisson sur deux. Sorties sous `artifacts/pbr-handover/s1h/`.
> - Références d'avant, prises sur ce `HEAD` : `base-legacy-a`, `-b`, `base-linear-a`, `-b` (rejeux des vues, sortie 0, 15 lignes `pipeline` attendues, aucun `[Warning]` ni `[Error]`), `base-ui` (10 captures), `base-audit` (30). Répétabilité `a` contre `b` : 0,00 de moyenne (14 fichiers égaux sur 15 en `legacy`, 12 sur 15 en `linear`). Contre `s1g/legacy-a` et `s1g/linear-a` : 0,00, et les six `scene-measure-*.txt` égaux octet pour octet ; interface de `base-ui` à 0,00 contre `s1g/ref-ui` (dix écrans). Sondes sur `base` (critères 1 à 4) : `legacy` 0 sur 34, 2 sur 4, **27 sur 33**, 0 sur 3 ; `linear` 1 sur 34, 0 sur 4, **27 sur 33**, 3 sur 3. Sondes `city-shadow-1` à `-3` de `Beginner-sector-20` (critère 3, toutes trois en échec : le port est éclairé) : `legacy` 124,139,132 / 39,45,44 / 40,46,46 contre 31,34,37 / 29,33,35 / 30,33,35 à l'original ; `linear` 120,131,123 / 26,30,28 / 20,24,24.
> - Builds du jeu et du cuiseur à 0 erreur, 0 avertissement. Cuiseur : avant recuisson `--verify` donnait 6 écarts (matériau, deux textures, trois entrées de catalogue de `RoadBack`), preuve que le contrôle mord, avec l'identifiant attendu `1ea60ef1-eda1-5b39-ad41-e9937e9b54ee` égal à la constante du jeu ; recuisson : 3 fichiers écrits, 263 inchangés, catalogue 609 -> 612 ; `--verify` 0 écart (99 matériaux, 2772 valeurs, 111 textures) ; `--check` 0 manquant ; second passage : 0 écrit, 266 inchangés. `git status` du contenu : `Materials/Track/RoadBack.material`, `Textures/RoadBack.texture`, `Textures/RoadBackNormal.texture` (nouveaux) et `AssetInfos.json` (trois entrées) ; rien d'autre.
> - Contrôle de géométrie (outil jetable hors dépôt qui rejoue `ComputeRaceLayout`, `CreateRoadModel` et `CreateRoadBackModel`, formules de `TrackVertex.cs` et `Track.cs` recalculées en double précision ; `t1-geometry.txt`). Beginner / Advanced / Expert : 1332 / 2172 / 4466 points de route, 5328 / 8688 / 17864 sommets (4 par point), 23958 / 39078 / 80370 indices (18 par segment) ; **succès sur les trois pistes pour tous les contrôles**. Sommets hauts égaux à ceux du maillage de route : écart maximal 0 (position, normale, tangente) ; sommets haut et bas égaux à l'expression flottante de l'original (`pos -/+ RoadWidthScale * roadWidth * right / 2 - up * RoadThickness * roadWidth`, converti, moins l'origine) : écart maximal 0 ; contre la double précision, 1,2e-4 pour les sommets hauts (ceux de la route elle-même) et 2,4e-4 pour les bas, soit l'arrondi flottant de coordonnées de l'original de l'ordre de 1e3 m, hors de portée de la tolérance de 1e-5 par un autre moyen que la même suite d'opérations ; UV égaux à `(TextureU x 1, V)`, V = 0, 0,135, 0,865, 1 (écart maximal 9,5e-9) ; indices : 0 écart avec le motif de `Track.cs:464-486` au pas de 4 ; normales et tangentes unitaires (écart de longueur à 1 au plus 1,8e-7), orthogonales (au plus 9,9e-8), égales aux vecteurs attendus (au plus 1,5e-7), `w` = 1 ; enroulement : pour chaque piste, les triangles du fond ont tous leur produit vectoriel opposé à la normale (face avant sous `CullCounterClockwise`, vue d'en dessous). Modèle : `CastShadows` et `ReceiveShadows` faux.
> - Quatre rejeux (`t1-legacy`, `t1-linear`) : sortie 0, 15 lignes `pipeline` attendues, aucun `[Warning]` ni `[Error]` ; les six `scene-measure-*.txt` égaux octet pour octet à ceux de `base`. Sondes : `legacy` 0 sur 34, 2 sur 4, **27 sur 33**, 0 sur 3 ; `linear` 1 sur 34, 0 sur 4, **27 sur 33**, 3 sur 3 : critère 3 égal à `base` ; `city-shadow-1` à `-3` identiques à `base` ; luminance moyenne 140,5 (`legacy`) et 133,5 (`linear`) ; pixels blancs de `Beginner-start` et `Expert-start` inchangés (6062 et 6500 en `legacy`, 0 et 0 en `linear`). Écart des vues avec `base` : de 0,00 à 1,76 de moyenne par canal (au plus `Expert-sector-50`, 1,33 / 1,62 / 1,76 en `legacy`, 1,53 / 1,70 / 1,79 en `linear`).
> - Regard en recadrage (`crops/t1-*.png` : original `nopost`, `base`, après) : `Expert-sector-50`, où la route passe au-dessus de l'horizon et se voit par en dessous, montre à l'original le dessous sombre avec son épaisseur ; `base` n'a aucun dessous, la route y est une feuille mince ; après, le dessous est là, avec l'épaisseur et la texture, et touche la route sans fente. `Beginner-sector-80` : le chant de la route (le mur d'épaisseur) apparaît sur le bord extérieur du virage, comme à l'original. Le dessous est plus clair que celui de l'original (même écart de lumière que pour le reste de la route, point O13).
> - Écart au plan : `scripts/RgceAssetCooker/README.md` (liste des matériaux de piste) mis à jour avec `RoadBack`, fichier non listé par la tâche.

### ✅ T12.2 — Sol de la ville

- Objectif : les trois pistes ont le plan de ville de l'original.
- Fichiers : `RacingGame.Gameplay/Track/LegacyTrackSceneFactory.Terrain.cs` ou un fichier voisin (`CreateCityGroundModel` : constante d'ancre en coordonnées de l'original et sa dérivation en commentaire, géométrie de `PlaneRenderer`, conversion par la conversion de points du port moins l'origine de la piste ; `CastShadows` faux, `ReceiveShadows` vrai), `RacingGame.Gameplay/Components/RaceTerrainGeneratorComponent.cs` (second modèle, matériau `Material.Track.Ground` en constante), `LegacyTrackSceneFactory.Models.cs` (`RaceTrackMaterialAssets.Ground` si absent), `RaceMaterialRecipes.cs` (`ForGround` : `CityGround.png` et `CityGroundNormal.png`, culling de la route ; `GroundTextureCandidates` ne sert plus au sol de ville ; le terrain garde ses textures), `scripts/RgceAssetCooker/CookPlan.cs`, contenu recuit (`Ground.material`, `.texture` de `CityGround` et `CityGroundNormal` si créées, `AssetInfos.json`).
- Validation :
  - builds, cuiseur comme en T12.1 ; `Terrain.material` inchangé octet pour octet ;
  - ancre : (a) calculée par un outil jetable à partir du placement d'ici pour Beginner (premier objet `hotel` ou `building` dans l'ordre d'ajout de l'original, que l'exécutant vérifie dans le code de l'original : ordre des objets du fichier de piste et des objets générés dans `landscapeObjects` au moment de `CreateCityPlane`) ; (b) recalculée dans l'original, par lecture de son code et du fichier de piste de Beginner, ou par une ligne de journal ajoutée temporairement à la copie de l'original et retirée ensuite (rien de committé dans `RacingGameOriginal`) ; les deux à 1e-3 près, égales à la constante ; valeur, taille et méthode notées ;
  - contrôle par script : 4 sommets, 6 indices, positions et UV égaux à la formule de `PlaneRenderer` pour chaque piste ;
  - mesure de scène inchangée ; sondes : critère 3 au moins celui de T12.1 ; les sondes `city-shadow-1` à `-3` de `Beginner-sector-20` notées avant et après ;
  - `Beginner-sector-20` et une vue d'Advanced ou d'Expert où la ville est visible, regardées contre l'original : le plan est à sa place et à sa taille (pas de plan flottant ni de scintillement avec le terrain).
- Arrêts : les deux calculs de l'ancre en désaccord ; scintillement entre le plan et le terrain que le plan de l'original n'a pas ; changement de la mesure de scène.
- Commit : `feat(racing-casa): build the city ground plane of the original`

> Validation (2026-11-17), jeu sur `d45015f` (T12.1) plus les changements de cette tâche, moteur `8a3933b36`, un tour de recuisson sur deux. Sorties sous `artifacts/pbr-handover/s1h/` (`t2-*`).
> - Ancre, valeur retenue **(994,9773 ; 426,16977 ; 28,235294)** en coordonnées de l'original, taille du plan `min(x, y)` = **426,16977** ; constante `LegacyCityGroundAnchor`, dérivation en commentaire. Deux calculs indépendants, d'accord au dernier chiffre d'un flottant (tolérance 1e-3) : (a) outil jetable hors dépôt sur le placement d'ici de Beginner (`RaceTrackLayout.Placements`, 1274 placements, premier dont le nom contient `hotel` ou `building` : le placement n° 0, un `Building`, premier modèle du combo `CombiBuildings` ; le premier objet du fichier, `TrackBeginner`, n'est pas un modèle du paysage) : (994,9773 ; 426,16977 ; 28,235294) ; (b) ligne de journal ajoutée à `CreateCityPlane` de la copie de l'original (écriture d'une ligne dans un fichier, `t2-anchor-original.txt`), retirée ensuite par `git checkout` du fichier, `git status RacingGameOriginal` vide, original recompilé après le retrait ; lancé par `--capture-track-audit --seed 1` sur les trois niveaux : une seule ligne, `level Beginner object count 1155 first big building index 0 position 994,9773 426,16977 28,235294`, ce qui confirme au passage que le plan n'est créé qu'une fois, pour Beginner, et gardé pour les trois pistes. Les premiers gros immeubles d'Advanced et d'Expert d'ici (1105,7615 ; 658,1227 ; 28,23529 et 2007,9181 ; 1129,4265 ; 44,84082) ne servent pas : le plan est celui de Beginner.
> - Builds du jeu et du cuiseur à 0 erreur, 0 avertissement. Cuiseur : avant recuisson `--verify` donnait 6 écarts (`Ground.material` : texture de base et normal map ; `CityGround.texture`, `CityGroundNormal.texture` et leurs entrées de catalogue absentes), preuve que le contrôle mord ; recuisson : 3 fichiers écrits, 265 inchangés, catalogue 612 -> 614 ; `--verify` 0 écart (99 matériaux, 2772 valeurs, 113 textures) ; `--check` 0 manquant ; second passage : 0 écrit, 268 inchangés. `git status` du contenu : `Materials/Track/Ground.material` (texture et normal map), `Textures/CityGround.texture` et `CityGroundNormal.texture` (nouveaux), `AssetInfos.json` (deux entrées) ; `Terrain.material` inchangé octet pour octet (`git diff` vide).
> - Contrôle de géométrie (outil jetable hors dépôt, `t2-geometry.txt`), Beginner / Advanced / Expert : **succès partout** ; 4 sommets et 6 indices `0, 1, 2, 2, 1, 3` ; positions égales à l'expression flottante de `PlaneRenderer` (`(±right ±dir) × size + up × 0,1`, translation de l'ancre, conversion du port moins l'origine de la piste) à un écart maximal 0, et à 3,05e-5 de la même formule en double précision ; UV égaux à `±size / 20` (= `±21,308488`, écart maximal 3,8e-7) ; normale `up` (0, 1, 0) et tangente `right` (1, 0, 0) du port, exactes, unitaires, orthogonales, `w` = 1 ; enroulement : les deux triangles ont leur produit vectoriel opposé à la normale, face avant vue d'en haut sous `CullCounterClockwise`, comme la route. Modèle : `CastShadows` faux, `ReceiveShadows` vrai.
> - Quatre rejeux (`t2-legacy`, `-b`, `t2-linear`, `-b`), sortie 0, 15 lignes `pipeline` attendues chacun, aucun `[Warning]` ni `[Error]` ; les douze `scene-measure-*.txt` égaux octet pour octet à ceux de `base`. Répétabilité `a` contre `b` : 0,00 de moyenne (12 fichiers égaux sur 15 en `legacy`, 13 sur 15 en `linear`), donc pas de scintillement d'une image à l'autre.
> - Sondes (valeurs de T12.1 entre parenthèses) : `legacy` critère 1 : 0 sur 34 (0) ; critère 2 : 2 sur 4 (2) ; critère 3 : **27 sur 33 (27)** ; critère 4 : 0 sur 3 (0). `linear` 1 sur 34 (1) ; 0 sur 4 (0) ; **27 sur 33 (27)** ; 3 sur 3 (3). Sondes `city-shadow-1`, `-2`, `-3` de `Beginner-sector-20` (critère 3, en échec avant comme après : le port est éclairé là où l'original est à l'ombre), avant puis après, original 31,34,37 / 29,33,35 / 30,33,35 : `legacy` 124,139,132 / 39,45,44 / 40,46,46 puis 195,225,234 / 69,87,99 / 56,71,79 ; `linear` 120,131,123 / 26,30,28 / 20,24,24 puis 184,204,213 / 56,72,84 / 32,43,51. Le plan de ville remplace la texture du terrain à cet endroit, plus claire et bleutée (même écart de lumière que la route, point O13) ; l'ombre du palmier d'ici ne tombe pas sur ces trois points. Rapport ombre sur lumière de la ville : `legacy` 1,921 puis 1,850, `linear` 3,133 puis 2,556 (original 0,276). Luminance moyenne des 15 vues : `legacy` 142,2 (140,5), `linear` 134,8 (133,5) ; pixels blancs de `Beginner-start` et `Expert-start` inchangés (6062 et 6500 en `legacy`, 0 et 0 en `linear`).
> - Vues qui changent contre T12.1 (`linear`, moyenne des canaux) : `Beginner-sector-20` 12,72 / 14,87 / 18,93, `Beginner-overview` 1,51 / 1,70 / 2,26, `Expert-overview` 3,05 / 3,44 / 4,27 ; les douze autres à 0,00 ou presque (`Beginner-start` 0,04, `Expert-start` 0,01) : le plan n'est visible que là. Aucune vue d'Advanced ne change.
> - Regard en recadrage (`crops/t2-*.png`, original `nopost`, `base`, après) : `Beginner-sector-20` : les pavés de la ville recouvrent le sol à gauche et à droite de la route, au même endroit qu'à l'original, l'ombre des immeubles et du palmier tombe sur le plan (il reçoit les ombres) ; `Expert-overview` : la silhouette du plan est celle de l'original, y compris les bords où le relief le traverse, à la même place et à la même taille, aucun plan flottant, aucun scintillement ; `Beginner-overview` de même. Le plan est nettement plus clair et plus bleuté que le gris de l'original (luminance du monde, point O13, non réglé ici).
> - Écart au plan : aucun écart de fichiers (le nouveau fichier est `LegacyTrackSceneFactory.CityGround.cs`, « fichier voisin » de la tâche) ; `GroundTextureCandidates` reste utilisé par le seul terrain.

### ✅ T12.3 — ADR, banc et références

- Objectif : la décision écrite et la référence du port après S1h.
- Étapes : ADR-0029 (skill `adr`, `Proposed`) : H1 à H4, qui amende l'ADR-0019 (sa conséquence « nothing is missing » était fausse) ; ligne d'état de l'ADR-0019 et index mis à jour. Banc complet vers `artifacts/pbr-handover/s1h/` comme en T11.4 (deux modes, comparaisons avec `s1h/base` et `s1g`), nouvelles références d'audit et d'interface ; section « After the road underside and the city ground » du doc de banc générée par script.
- Validation : chiffres du doc relus par script contre les sorties ; interface : écrans hors course à 0,00 contre `s1h/base` (captures d'interface prises avec `s1h/base`).
- Commit : `docs(racing-casa): record the road underside and the city ground`

> Validation (2026-11-17), jeu sur `1f9d5da` (T12.2), build refait avant les lancements, moteur `8a3933b36`. Sorties sous `artifacts/pbr-handover/s1h/`.
> - ADR-0029 (`docs/decisions/0029-rgce-builds-the-road-underside-and-the-city-ground.md`, `Proposed`, skill `adr`) : H1 à H4, source citée ; la ligne d'état de l'ADR-0019 devient « Accepted; amended by ADR-0027, ADR-0029 » (seule modification de ce fichier) et l'index est mis à jour (ADR-0019 et ADR-0029). Écart à la procédure du skill : l'ADR est dans le commit de la tâche, pas dans un commit `docs(adr)` à part (un commit par tâche, règle du plan).
> - Banc complet : deux rejeux par mode (`legacy-a`, `-b`, `linear-a`, `-b`), tous de sortie 0, 15 lignes `pipeline` attendues chacun, aucun `[Warning]`, `[Error]` ni exception ; comparaisons `nopost`, `full` et sondes par mode (`compare-<mode>-nopost`, `-full`, `-probes`), répétabilité (13 fichiers égaux sur 15 dans chaque mode, écarts moyens de 0,00), comparaison avec `base` (`compare-<mode>-vs-base`) et avec `s1g` (`compare-<mode>-vs-s1g`) ; nouvelles références d'interface (`ref-ui`, 10 captures) et d'audit (`ref-audit`, 30 captures), prises dans le mode par défaut du jeu (`linear`).
> - Section « After the road underside and the city ground » du doc générée par script (`gen_section_s1h.py`, hors dépôt) : les tableaux viennent des fichiers de sortie, des `.material` cuits et de la source. Relecture par un second script indépendant (`check_doc_s1h.py`, hors dépôt), qui relit en texte brut les `report.md`, `probes.md`, `run.log`, `t1-geometry.txt`, `t2-geometry.txt`, les sorties d'ancre, `ui-compare/report.txt` et l'audit : 290 valeurs comparées au doc, 0 différence.
> - Mesure de scène : les douze `scene-measure-*.txt` (trois pistes, deux modes, deux rejeux) égaux octet pour octet à ceux de `base`. Sondes : critère 3 de 27 sur 33 dans les deux modes (`base` : 27 sur 33), critères 1, 2 et 4 inchangés. Luminance moyenne : `legacy` 142,2 (140,5 avant), `linear` 134,8 (133,5).
> - Interface contre `s1h/base-ui` : `splash`, `main-menu`, `highscores`, `options`, `help`, `car-selection` et `track-selection` à **0,00** ; `race-hud` 0,04 / 0,05 / 0,06, `pause` 0,01 / 0,01 / 0,02, `race-finished` 0,04 / 0,05 / 0,06 (le plan de ville et le dessous de la route sont visibles dans l'arrière-plan de course). Audit contre `s1h/base-audit` : au plus 23,808 (`audit-Expert-helper-laterns`), au moins 0,000 (`audit-Advanced-helper-laterns`).
> - `scripts/TrackCaptureCompare/README.md` : aucune commande ne change ; non modifié.

### ✅ T12.4 — Vérification indépendante de S1h

- Objectif : un `verifier` frais confirme ou réfute : géométrie et matériaux des deux pièces conformes à l'original (H1 noté) ; ancre juste par ses propres calculs ; contenu changé limité à ce qui est listé ; mesure de scène et critère 3 sans régression ; ADR et doc justes.
- Étapes et verdicts : comme T5.4.
- Commit : `docs(ai-agent): record the verification of the road underside and the city ground`

> Validation (2026-11-17), sorties dans `artifacts/pbr-handover/s1h/verify/`.
> - Vérification indépendante en trois volets frais (un seul lance le jeu et les builds, deux en lecture seule) : **CONFIRMED** sur les trois, aucun constat P0 à P2.
> - Volet lancements, sur `e4b1a86` (build refait, 0 erreur) : quatre rejeux `legacy` et `linear` en sortie 0, 15 lignes de pipeline, journal propre ; mesure de scène égale octet pour octet à `s1h/base` ; critère 3 des sondes 27 sur 33 dans les deux modes ; rejeux à 0,00 de ceux de l'exécutant ; cuiseur `--verify` et `--check` propres, second passage sans écriture ; `--smoke-frontend` en succès ; sept écrans hors course à 0,00 contre `s1h/base-ui` ; dessous de route présent et joint à la route (`Expert-sector-50`, `Beginner-sector-80`), sol de ville à la place et à l'étendue de l'original (`Beginner-sector-20`, `Expert-overview`), sans plan flottant ; ancre retrouvée dans une troisième source, `scene.json` de l'original (premier objet de Beginner, `Building`).
> - Volet géométrie, par lecture et recalcul : dessous de route et plan de ville conformes à l'original (ordre des sommets, décalage du bas, UV, indices, enroulement après conversion d'axes, normales, tangentes, drapeaux d'ombre) ; ancre recalculée entièrement depuis les données et le code de l'original ; rien de nouveau n'est sérialisé ; les deux maillages ne touchent ni la physique ni l'export de scène.
> - Volet contenu et doc : contenu changé limité à la liste de la phase, `Terrain.material` intact ; ADR-0029 fidèle à H1 à H4, ADR-0019 changée sur sa seule ligne d'état ; 471 valeurs de la section du doc relues, 0 écart.
> - Remarque P3 (introduite) : le sol de la ville a une couleur de base blanche (255) alors que `cityMat` de l'original a un diffus de 200 ; écart non déclaré. Différé : c'est un réglage de couleur, joint à O13 (les autres matériaux de piste générés d'ici sont blancs aussi quand ils ont une texture).
> - Remarques P4, notées sans suite : commentaire faux dans `CreateRoadBackModel` (dit que le dernier point n'est pas dupliqué) ; commentaire périmé de `GroundTextureCandidates` ; la note de T12.1 dit « quatre rejeux » pour deux dossiers ; l'écart de tolérance de T12.1 n'est pas sur la ligne « Écart au plan » et ses chiffres en ulp surestiment l'erreur (au plus 2,4e-4 m) ; sol de ville en famille `Terrain` (rugosité 1) alors que l'original a un spéculaire de 128 (H2) ; quelques pixels diffèrent d'un rejeu à l'autre sur deux vues d'ensemble, hors des deux pièces, comme sur `base` ; ombre et lumière inversées sur les pavés de `Beginner-sector-20` face à l'original, déjà là avant S1h (sondes `city-shadow`) ; le monde `TrackBeginnerGenerators.world` est inchangé mais aucun mode ne le charge, donc pas de preuve de chargement.
> - S1h close.

---

## Phase 13 — Couleur d'ambiant du monde de course (O13)

Décision de l'auteur (2026-11-17, après la mesure notée en O13) : la couleur d'ambiant du monde de course passe de blanc à 0,05, le défaut du moteur. Le reste de O13 (16 points de luminance en trop, rapports d'ombre en `linear`, pavés de la ville, couleur de base du sol de ville) reste ouvert.

Retour arrière : annuler les commits de cette phase. Référence d'avant : `artifacts/pbr-handover/s1h/` ; mesure attendue : `artifacts/pbr-handover/o13/amb005-linear` et `amb005-legacy`.

### ✅ T13.1 — Ambiant à 0,05, banc et références

- Fichiers : `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` (`AmbientColor` = `new Vector3(0.05f)`, en constante nommée ; le commentaire dit que c'est le défaut du moteur, qu'il multiplie l'irradiance du ciel dans `lit-pbr`, et renvoie à cette phase ; le commentaire actuel, qui dit que ces valeurs sont celles que le monde doit avoir, est corrigé) ; `docs/racinggame-3d-capture-bench-local-baseline.md` (section « After the ambient colour change », générée par script, mode de chaque chiffre indiqué) ; ce plan.
- Étapes : changement ; build ; banc complet vers `artifacts/pbr-handover/s13/` comme en T12.3 (deux rejeux par mode, `nopost`, `full`, sondes, répétabilité, comparaison avec `s1h`), nouvelles références d'audit et d'interface.
- Validation :
  - build à 0 erreur ; `--smoke-frontend` en succès ; journaux sans avertissement ni erreur ;
  - les rejeux `linear` et `legacy` sont à 0,00 de moyenne par canal de `o13/amb005-linear` et `o13/amb005-legacy` sur les 15 vues ;
  - mesure de scène égale octet pour octet à celle de `s1h` ; critère 3 des sondes au moins 27 sur 33 dans les deux modes ;
  - interface : les sept écrans hors course à 0,00 contre `s1h/ref-ui` ; `race-hud`, `pause`, `race-finished` notés ;
  - aucun fichier de contenu changé ; chiffres du doc relus par script contre les sorties.
- Arrêts : un rejeu différent de la mesure `o13` ; critère 3 sous 27.
- Commit : `fix(racing-casa): use the engine's default ambient colour in the race world`

> Validation (2026-11-17), jeu sur `7c21223` plus le changement de cette tâche, moteur `8a3933b36`, aucune recuisson. Sorties sous `artifacts/pbr-handover/s13/`.
> - Changement : `RaceWorldFactory.cs` seul (constante nommée `AmbientColor` = `new(0.05f)` avec son commentaire, commentaire des réglages d'ambiant corrigé ; fins de ligne CRLF gardées, le diff ne montre que les lignes voulues). Build à 0 erreur, 0 avertissement ; `--smoke-frontend` : sortie 0, « Smoke validation completed successfully », aucun `[Warning]` ni `[Error]` (`smoke-run.log`). Aucun fichier de contenu, de `RacingGame.Gameplay` ni du cuiseur changé.
> - Banc complet : deux rejeux par mode (`legacy-a`, `-b`, `linear-a`, `-b`), tous de sortie 0, 15 lignes `pipeline` attendues chacun, aucun `[Warning]`, `[Error]` ni exception ; `nopost`, `full` et sondes par mode, répétabilité (13 fichiers égaux sur 15 dans chaque mode, écarts moyens de 0,00), comparaison avec `s1h` (`compare-<mode>-vs-s1h`) ; nouvelles références d'interface (`ref-ui`, 10 captures) et d'audit (`ref-audit`, 30), prises en `linear`.
> - **Mesure `o13` reproduite** : les quatre rejeux sont à 0,00 de moyenne par canal de `o13/amb005-legacy` et `amb005-linear` sur les 15 vues (14 et 12 fichiers égaux par empreinte sur 15 en `legacy`, 14 et 13 en `linear`).
> - Mesure de scène : les douze `scene-measure-*.txt` égaux octet pour octet à ceux de `s1h`. Sondes (valeurs de `s1h` entre parenthèses) : `legacy` critère 1 : 3 sur 34 (0) ; critère 2 : 2 sur 4 (2) ; **critère 3 : 27 sur 33 (27)** ; critère 4 : 3 sur 3 (0) ; `linear` 9 sur 34 (1) ; 0 sur 4 (0) ; **27 sur 33 (27)** ; 3 sur 3 (3). Rapports ombre sur lumière, terrain, ville, route, terrain (original 0,284, 0,276, 0,228, 0,267) : `legacy` 0,271, 1,894, 0,717, 0,267 (0,282, 1,850, 0,835, 0,263) ; `linear` 0,159, 2,903, 0,838, 0,138 (0,183, 2,556, 0,905, 0,141). Rapport `road-lit-1` / `terrain-lit-1` de `Beginner-start` : `legacy` 0,711 (0,756), `linear` 0,745 (0,806) ; original 0,348. Les sondes `city-shadow-1` à `-3` de `Beginner-sector-20` restent en échec (le port est éclairé là où l'original est à l'ombre) : `legacy` 119,129,135 / 42,47,51 / 34,39,41 (195,225,234 / 69,87,99 / 56,71,79), `linear` 166,175,180 / 46,53,57 / 27,31,33 (184,204,213 / 56,72,84 / 32,43,51), original 31,34,37 / 29,33,35 / 30,33,35.
> - Luminance moyenne des 15 vues (original 96,3) : `legacy` 88,4 (142,2), `linear` 112,3 (134,8). Écart moyen par canal avec l'original (`nopost`, moyenne des 15 vues) : `linear` 25,3 / 25,4 / 24,4 (38,1 / 46,4 / 46,1) ; `legacy` 22,1 / 19,8 / 16,6 (41,5 / 54,1 / 53,7). Pixels blancs de `Beginner-start` et `Expert-start` à 255, puis à 250 au moins : `linear` 0 et 0 (inchangé) ; `legacy` 12 puis 15 et 1547 puis 1579 (6062 puis 6824 et 6500 puis 6800).
> - Interface contre `s1h/ref-ui` : `splash`, `main-menu`, `highscores`, `options`, `help`, `car-selection` et `track-selection` à **0,00** ; `race-hud` 15,04 / 21,99 / 23,40, `pause` 4,37 / 6,33 / 6,63, `race-finished` 14,44 / 20,82 / 21,44 (le monde de course change dessous). Audit contre `s1h/ref-audit` : au plus 36,927 (`audit-Beginner-helper-laterns`), au moins 4,410 (`audit-Beginner-helper-tunnel`).
> - Section « After the ambient colour change » du doc générée par script (`gen_section_s13.py`, hors dépôt), chaque chiffre disant son mode ; relecture par un second script indépendant (`check_doc_s13.py`, hors dépôt) : 278 valeurs comparées au doc, 0 différence.
> - Écart au plan : aucun. `scripts/TrackCaptureCompare/README.md` non touché (aucune commande ne change).

### ⏳ T13.2 — Vérification indépendante

- Objectif : un `verifier` frais confirme ou réfute les validations de T13.1 par ses propres lancements, et que rien d'autre n'a changé.
- Commit : `docs(ai-agent): record the verification of the ambient colour change`

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
- O12 (S1f, bloquant) : traînées blanches du terrain en `lit-pbr`, causées par le dénominateur de GGX du moteur quand la normale tourne le dos au demi-vecteur (régression de `6ead47308`). La correction directe fait passer 6 tests GPU de `lit-pbr` au noir sur ce PC (détail dans le plan moteur `pbr-ggx-back-facing-tasks.md`). Question : reprendre la correction avec la piste notée dans ce plan (une passe de plus, d'abord vérifier l'hypothèse de la limite d'instructions), ou accepter le défaut pour l'instant et poursuivre S1g et la suite ? **Tranché par l'auteur le 2026-11-17 : reprendre la correction ; faite (moteur `8a3933b36`), vérifiée.**
- O13 (S1g, non bloquant) : le monde de course est plus clair que l'original (luminance moyenne 133,5 en `linear`, 96,3 dans l'original ; route blanc bleuté). La couleur d'ambiant blanche (comme l'autre PC) fait passer l'ambiant de 5 % à 100 % de l'irradiance du ciel ; sa part dans l'écart n'est pas mesurée. Question : garder le blanc et régler à l'œil (exposition, ambiant), ou mesurer d'abord un rejeu avec la couleur d'ambiant à 0,05 ? S1h y ajoute : le sol de la ville et le dessous de la route sont plus clairs et plus bleus que dans l'original ; couleur de base du sol de ville blanche au lieu du diffus 200 de l'original ; ombre et lumière inversées sur les pavés de `Beginner-sector-20` (sondes `city-shadow`), cause non cherchée.
  - Mesure demandée par l'auteur (2026-11-17, sorties dans `artifacts/pbr-handover/o13/`, code remis ensuite, rien de committé) : un rejeu avec la couleur d'ambiant à 0,05 au lieu de blanc. Luminance moyenne des 15 vues, original 96,3 : `linear` 112,3 (134,8 avec le blanc), `legacy` 88,4 (142,2). Écart moyen par canal avec l'original, `linear` : 25,3 / 25,4 / 24,4 (38,1 / 46,4 / 46,1 avec le blanc). Sondes `linear` : critère 1, 9 sur 34 (1 avec le blanc) ; critère 3 inchangé, 27 sur 33 ; rapports ombre sur lumière du terrain 0,159 et 0,138 (0,183 et 0,141 ; original 0,284 et 0,267), ville 2,903, route 0,838. En `legacy` : critère 1, 3 sur 34 ; critère 4, 3 sur 3 (0 avec le blanc) ; terrain 0,271 et 0,267. L'ambiant blanc porte donc 22,5 des 38,5 points de luminance en trop en `linear` ; il en reste 16 avec 0,05. Les rapports d'ombre en `linear` et l'inversion des pavés de la ville ne viennent pas de l'ambiant.
