# Plan agent IA — Fusion du travail PBR poussé et reprise de la note de passation

Plan d'exécution de la reprise décrite dans [HANDOVER.md](../HANDOVER.md) (présent après T1.2 ; avant, `git show origin/remaster:ai-agent/HANDOVER.md`).
Les décisions D1 → D4 ci-dessous ont été arbitrées avec l'auteur le 2026-11-16 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Réunir sur de nouvelles branches le travail local de ce PC et le travail poussé depuis l'autre PC (bascule PBR du monde de course, moteur `origin/main`), dans le jeu et dans le moteur, puis reprendre les suites de la note de passation, une tranche à la fois.

Tranches :

| Tranche | Contenu | Dépend de | État |
|---|---|---|---|
| S1 | Fusions du moteur et du jeu sur de nouvelles branches (Phases 0 et 1) | — | Approuvée le 2026-11-16 |
| S2 | HANDOVER §7 : vérification indépendante des fusions, banc de capture du jeu et captures UI sur le moteur fusionné, nouvelle référence sur ce PC | S1 | À détailler après S1 |
| S3 | Tunnel presque noir | S2 | À détailler ; choix de l'auteur attendu (O1) |
| S4 | Voiture centrale du carrousel trop blanche | S2 | À détailler ; choix de l'auteur attendu (O2) |
| S5 | Défauts connus du §4 de la note (moteur sur une branche moteur dédiée, jeu sur cette branche) | S2 | À détailler |

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

## Règles d'exécution pour l'agent

- **Branches dédiées** `chantier/rgce-pbr-handover` (jeu) et `chantier/merge-editor-project` (moteur). Ne jamais committer sur `main`, `master` ni `remaster`.
- **Une seule tâche à la fois**, icône de statut tenue à jour, **un commit par tâche** qui inclut la mise à jour de ce fichier, message en anglais `type(area): summary`.
- **Ne jamais pousser.**
- **Ne rien inventer** : sinon ⚠️ Blocked, question dans « Points ouverts », arrêt.
- **Build obligatoire** avant ✅ dès que du code est touché ; tests dès que du code testé est touché.
- **Ne jamais indexer** `.serena/`, `log.txt`, `.codegraph/` ; `git add` fichier par fichier.
- **Aucune commande git dans `MGUI` ni `NvgSharp`.**
- Un seul lancement du jeu ou de l'éditeur à la fois, jamais pendant un build ou des tests.
- **Règle de conflit du jeu** :
  - (a) matériaux, éclairage, PBR du monde de course, tables de réglage héritées : la version d'`origin/remaster` l'emporte ; chaque modification locale d'un fichier supprimé là-bas est d'abord relue, puis classée obsolète ou reportée là où la logique a été déplacée ; une modification qu'on ne sait pas placer bloque la tâche ;
  - (b) interface, automatisation, captures, projet éditeur : la version locale est gardée ;
  - (c) fichier touché pour les deux raisons : les deux intentions gardées, bloc par bloc, après lecture des commits des deux côtés ;
  - (d) fichiers binaires : si un seul côté diffère de la base, prendre ce côté ; si les deux diffèrent et que le commit local porte une correction indépendante, bloquer la tâche ;
  - (e) index des ADR et des plans : garder les lignes des deux côtés ; numéros d'ADR en double : renuméroter les ADR locaux et corriger leurs renvois ;
  - (f) API du moteur qui a bougé : adaptation minimale du point d'appel du jeu, listée dans le commit ; une correction qui demande de toucher au moteur bloque la tâche.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale (S1)

- Moteur : `dotnet build CasaEngine.MonoGame.sln` et `dotnet build CasaEngine.Editor.MonoGame.sln` à 0 erreur ; `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj --blame-hang-timeout 5m` vert, pas moins de 6119 tests.
- Jeu : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -c Debug` à 0 erreur ; projets de test du jeu verts ; `--smoke-frontend` et `--verify-legacy-import-profile` en succès.
- S1 n'affirme pas l'équivalence visuelle : c'est l'objet de S2.

Retour arrière de S1 : jeu, `git switch remaster` puis `git branch -D chantier/rgce-pbr-handover` ; moteur, `git switch chantier/rgce-editor-project`, `git branch -D chantier/merge-editor-project`, `git branch -f main 778f6aa8c`.

---

## Phase 0 — Préparation

### ✅ T0.1 — Branche du jeu et plan

- Objectif : créer la branche de travail du jeu et y enregistrer ce plan.
- Fichiers : `ai-agent/tasks/rgce-pbr-handover-tasks.md`, `ai-agent/README.md`.
- Validation : `git status` ne montre que ces deux fichiers indexés.
- Commit : `docs(ai-agent): plan the merge of the pushed PBR work and the handover follow-up`

---

## Phase 1 — Fusions (S1)

### ⏳ T1.1 — Fusion du moteur

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

### ⏳ T1.2 — Fusion du jeu

- Objectif : `chantier/rgce-pbr-handover` = `remaster` local + `origin/remaster`, sous-module pointé sur le commit de fusion de T1.1.
- Fichiers : les chemins en conflit listés plus haut, le pointeur `CasaEngine`, ce plan.
- Étapes :
  1. `git merge --no-ff --no-commit origin/remaster`.
  2. Résoudre selon la règle de conflit du jeu.
  3. `git add CasaEngine` une fois le moteur sur le commit de T1.1 ; vérifier avec `git diff --cached --submodule`.
  4. Recopier `ai-agent/handover/pbr-baseline/*.sh` vers `CasaEngine/CasaEngine/artifacts/pbr-baseline/` (dossier ignoré par git).
- Validation : aucun marqueur de conflit ; build du jeu à 0 erreur ; projets de test du jeu verts ; `--smoke-frontend` puis `--verify-legacy-import-profile` en succès.
- Commit : `merge: integrate origin/remaster (PBR race world) into the local remaster work`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

- O1 (S3) : tunnel presque noir, relever sa couleur de base ou lui donner une lumière ? Choix de l'auteur.
- O2 (S4) : voiture centrale du carrousel, quelle intensité viser ? Choix de l'auteur.
