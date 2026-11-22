# Plan agent IA — Le jeu sur le moteur d'`origin/main` (plans de profondeur des caméras)

Plan d'exécution de l'intégration, dans le jeu, des 8 commits que `origin/main` du moteur a reçus depuis un autre poste le 2026-11-22 (`164f8d680` → `aac8911cf`). Demande de l'auteur du 2026-11-22 : « Oui » à la question « Voulez-vous que j'intègre aussi ces 8 commits du moteur dans le jeu ? », posée avec la recommandation « avec la même mesure sur le banc ».
Les décisions D1 à D3 ci-dessous viennent de l'auteur : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Le jeu `RacingGameCasaEngine`, ses outils et son projet d'éditeur fonctionnent sur le moteur à `aac8911cf`, avec la preuve que rien ne change :

1. le `main` local du moteur et le pointeur du sous-module du jeu sont à `aac8911cf` ;
2. le jeu et l'outil compilent sans modification de leur code, et les mondes recuits sont les mêmes fichiers ;
3. le banc de capture rend les mêmes images, à l'octet, que la référence `artifacts/worlds-as-assets/e2/ref` (celle du jeu sur `164f8d680`) ;
4. les contrôles d'éditeur qui touchent une caméra tiennent sur l'éditeur du nouveau moteur ;
5. les deux phrases des documents du jeu que le nouveau moteur rend fausses sont corrigées.

Ce que le plan ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-11-22)

Relevé en lecture seule par la session principale. `NEW` = les 12 fichiers changés, exportés à `aac8911cf` dans `artifacts/engine-depth-range/engine-new/` (hors version), avec le diff complet `164f8d680..aac8911cf.diff`.

- **Dépôts.** Jeu : branche `chantier/rgce-engine-camera-depth-range` créée sur `6e8424d` = `remaster` = `origin/remaster` (poussé) ; arbre propre hors `.serena/` et `log.txt`, non suivis, jamais indexés. Moteur : `HEAD` sur `chantier/rgce-on-origin-main-4` = `main` local = `164f8d680`, arbre propre ; `origin/main` = `aac8911cf373e80f0f8b7c414919b2181b11f284` (après `git fetch origin main`) ; `git rev-list --count 164f8d680..origin/main` : 8.
- **Les 8 commits** (`git log 164f8d680..origin/main`) : `257f036e8` plan, `26ea3ebee` `fix(camera): keep the depth range of a camera when it joins a world`, `c9e156d9c` tests, `c23f8e969` ADR-0153 du moteur, `f9027f8cf` scène d'exemple, `f75f97439`, `fb28c5634` et `aac8911cf` notes de plan. `git diff --stat` : 12 fichiers, 806 insertions, 8 suppressions. Aucun ne touche `MGUI`, `NvgSharp`, un `.fx`, un `.props`, un `.targets`, un `.csproj` ni une solution (`git diff --name-only`, recherche vide).
- **Code changé hors tests : deux fichiers.** `CasaEngine/Framework/Scene/Entities/Components/CameraComponent.cs` : `InitializeWithWorld` ne remplace plus les plans par 1 et 1000 ; il garde ceux que la caméra porte quand `IsValidDepthRange` est vrai (plan proche > 0 et plan lointain > plan proche), sinon il met 1 et 1000 (`DefaultNearPlane`, `DefaultFarPlane`) et écrit un avertissement, sauf quand les deux valent 0 ; il marque aussi la projection à recalculer. `CasaEngine.Editor/Runtime/EditorCameraFactory.cs` : commentaires seulement. Aucune API n'est retirée ni renommée ; deux constantes et une méthode statique publiques sont ajoutées.
- **Le reste des 12 fichiers** : deux fichiers de tests (`CameraDepthRangeTests.cs`, `OrientedSpriteSampleDataTests.cs`), une scène d'exemple du moteur (`Projects/OrientedSpriteSample/`), l'ADR-0153 du moteur, son index, sa page `docs/engine/perspective-camera-component.md`, son plan et l'index de ses plans.
- **Ce que l'ADR-0153 du moteur annonce** : pas de changement de format ; la largeur et la hauteur du viewport viennent toujours de l'écran ; une caméra créée par code porte 0 et 0 jusqu'à ce qu'elle rejoigne un monde ; « a depth range set by code after a camera joined a world now survives the next time the camera joins one ».
- **Les mondes du jeu ne portent pas de plans.** Quatre mondes ont une caméra (`Content/Worlds/FrontEnd.world`, `Content/Worlds/Race/Track{Beginner,Advanced,Expert}.world`) ; dans chacun, lignes 38 à 45 : `"view_distance": 0.0`, `"min_depth": 0.0`, `"max_depth": 0.0`. Aucun autre fichier de `RacingGameCasaEngine/Content` ne contient `min_depth` (`rg -c`). Ces caméras reçoivent donc 1 et 1000 sur le nouveau moteur comme sur l'ancien, sans avertissement. Les quatre fichiers du dossier de build (`bin/Debug/net9.0-windows/Content/Worlds`) sont identiques à ceux de `Content` (`cmp`).
- **Le code du jeu ne règle des plans qu'à deux endroits, toujours après que la caméra a rejoint son monde** (`rg "NearPlane|FarPlane|MinDepth|MaxDepth" -g '*.cs'` hors `CasaEngine`) : `RacingGameCasaEngine/UI/CarSelectionCarousel.cs` (la caméra est créée ligne 111, rejoint le monde ligne 114, et `ConfigureCamera`, lignes 283 à 291, écrit 0,5 et 1750) ; `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs:546-547` (plans lus dans le fichier des vues de référence, écrits avant chaque vue). `RaceWorldTemplates.cs:273` crée la caméra des mondes sans plans. Aucune classe du jeu ne dérive d'une caméra du moteur.
- **Conséquence attendue, à prouver par le banc** : aucune image ne change. Le seul cas où le nouveau moteur se comporte autrement pour le jeu serait une caméra qui rejoint un monde une seconde fois après qu'un code lui a donné des plans ; le carrousel ne rejoint son monde qu'une fois, et le validateur réécrit ses plans avant chaque vue.
- **Outil** : `scripts/RgceAssetCooker/WorldVerifier.cs:363-366, 404-405` compare `ViewDistance` et le viewport (plans compris) de chaque caméra construite à ceux de la caméra relue. Le format n'ayant pas changé, la recuisson doit rendre les mêmes fichiers.
- **Deux phrases des documents du jeu deviennent fausses sur le nouveau moteur** : `RacingGameCasaEngine/Assets/README.md:111` (« the camera's initialization then overwrites the size, the depths and the distance at every load ») et `scripts/RgceAssetCooker/README.md:88-89` (« the game never reads those values from the file »). L'ADR-0042 du jeu, ligne 31, porte la première tournure.
- **Banc de parité** : `artifacts/engine-simple-cameras/parity_runs.py <tranche>` lance les douze rejeux, `smoke`, `profile`, l'export et les deux audits de voitures sous le verrou ; `artifacts/worlds-as-assets/scripts/slice_check.py <tranche> --ref worlds-as-assets/e2/ref` exige les mêmes PNG à l'octet dans `-a`, `-b` et la référence, aucun `[Warning]`/`[Error]` absent du journal de référence, l'export et les audits de voitures de M1, et les contrôles d'outil à 0. Dernier passage : `slice_check.py e2 --ref worlds-as-assets/e2/ref`, `RESULT: PASS`, jeu sur `164f8d680`.
- **Tests du moteur sur ce poste, aujourd'hui, sur `164f8d680`** : 8533 tests, 0 échec (`artifacts/engine-simple-cameras/engine-tests-new.txt`), rejoués par le vérificateur de clôture avec le même résultat.
- **Contrôle d'ouverture de l'éditeur** : `scripts/check_editor_opens_assets.py` (`--only <file_name>` et `--type <ext>` répétables) ; le catalogue a 622 entrées de 16 genres (`.world` 7, `.entity` 1, `.staticModel` 56, `.material` 99, `.texture` 122, `.sound` 28, `.uiscreen` 9, `.sprite` 50, `.png` 149, `.dds` 2, `.wav` 28, `.fnt` 1, `.gltf` 56, `.Track` 3, `.CombiModel` 10, `.data` 1). Le rejeu complet du plan précédent a pris 4583 s de temps d'éditeur (7,4 s par lancement) et a privé la session de rendu de créneau pendant une demi-heure.
- **Session de rendu** (`local_84bb0e29…`, autre arbre de travail) : elle lance aussi le jeu ; verrou `D:/development/rgce-run.lock`, libre au moment du relevé. Elle a reçu aujourd'hui la demande de l'auteur d'amener ses branches sur `164f8d680`.
- **ADR du jeu** : le dernier est 0045 ; 0043 et 0044 sont pris par la branche de rendu.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | **Intégrer les 8 commits, avec la même mesure sur le banc** (« Oui » de l'auteur, 2026-11-22). |
| D2 | **Aucun seuil d'image** : la règle « mesurer, puis décider » du plan précédent reste celle de l'auteur. Si une image diffère de la référence, l'agent mesure et s'arrête ; il n'admet aucun écart de lui-même. |
| D3 | **Mode d'exécution : AUTO** (réponse de l'auteur du 2026-11-22 : « Approuvé AUTO »). En AUTO : commits sur la branche de chantier seulement ; ni push, ni avance de `remaster` ou de `master` ; arrêt sur blocage, défaut du moteur, ou décision qui revient à l'auteur. |

## Points que l'approbation du plan valide

| Réf | Point |
|---|---|
| P1 | **Le `main` local du moteur est avancé** de `164f8d680` à `aac8911cf` : dans le sous-module, sur la branche `chantier/rgce-on-origin-main-4` déjà en place, `git merge --ff-only aac8911cf373e80f0f8b7c414919b2181b11f284`, puis `git update-ref refs/heads/main aac8911cf373e80f0f8b7c414919b2181b11f284 164f8d6809f6293405a81458016bbc5ae2212076`, après `merge-base --is-ancestor`. C'est une avance rapide vers ce qui est déjà sur `origin/main` : aucun commit du moteur n'est créé, rien n'est poussé ; aucun `checkout`, `switch`, `reset`, `submodule update` ; aucune commande git dans `MGUI` ni `NvgSharp`. La cible est ce commit précis : si `origin/main` a encore avancé au moment de l'exécution, les commits suivants ne sont pas pris. |
| P2 | **Contrôles d'éditeur réduits à ce que le changement peut toucher.** Le seul code changé est l'initialisation d'une caméra ; les 622 ouvertures du plan précédent sont remplacées par **22 ouvertures** : les 7 mondes, la voiture, et le premier asset du catalogue pour chacun des 14 autres genres (chaque genre passe par sa propre vue de l'éditeur, donc par ses propres caméras). L'aller-retour hors éditeur (622 sur 622, cinq témoins) et l'échantillon piloté sur `Worlds/FrontEnd.world` sont rejoués en entier. Le contrôle des composants proposables n'est pas rejoué : les 8 commits n'ajoutent ni ne retirent aucun composant. **Si l'auteur veut les 622 ouvertures (environ 77 minutes d'éditeur sous le verrou), il le dit à l'approbation.** |
| P3 | **Dossier hors dépôt** : une copie de `RacingGameCasaEngine/Content` dans `D:/development/rgce-e5/` pour les contrôles de l'éditeur, créée en T3.1 et **supprimée à la clôture** (T4.3), après l'avoir regardée. Rien d'autre n'est créé ni supprimé hors du dépôt. |
| P4 | **Pas d'ADR du jeu.** Le jeu ne prend aucune décision ici : ni son code, ni ses fichiers ne changent. Les deux README sont corrigés et citent l'ADR-0153 du moteur ; l'ADR-0042 n'est pas réécrite (règle des ADR), et `ai-agent/HANDOVER.md` dit que sa phrase décrit le moteur d'avant `aac8911cf`. Si l'auteur préfère un ADR-0046 d'amendement, il le dit à l'approbation. |
| P5 | **Tests du moteur** : lancés une fois, sur `aac8911cf`. Le témoin du poste est le passage d'aujourd'hui sur `164f8d680` (8533 tests, 0 échec) ; il n'est pas rejoué. Tout échec arrête le plan (R2). |
| P6 | **Sorties non versionnées** sous `artifacts/engine-depth-range/` et `artifacts/worlds-as-assets/e5/` (dossiers ignorés par git). Les scripts de lancement du plan précédent (`artifacts/engine-simple-cameras/parity_runs.py`, `driven/driven_run.py`) sont réutilisés ; `driven_run.py` est **copié** dans `artifacts/engine-depth-range/driven/` avec `COPY = 'D:/development/rgce-e5/content'` et `OUT` dans ce dossier, pour ne pas écraser les sorties du plan précédent. |
| P7 | **Retour en arrière** : le travail du jeu est sur la branche de chantier, que rien n'oblige à fusionner. Le `main` local du moteur ne revient à `164f8d680` (`git update-ref refs/heads/main 164f8d680… aac8911cf…`) que sur demande de l'auteur ; un arrêt du plan le laisse à `aac8911cf`, qui est sur `origin/main`. |
| P8 | **Rien n'est poussé et `remaster` n'est pas avancé par ce plan.** La question est posée à l'auteur à la clôture. |

## Règles d'exécution pour l'agent

- **Branche `chantier/rgce-engine-camera-depth-range`** (jeu). Jamais de commit sur `master` ni `remaster` ; aucun commit dans le moteur (R2).
- **Une seule tâche à la fois**, icône `🚧` avant, `✅`/`🧪`/`⚠️` après, note de validation sous la tâche, **un commit par tâche** qui inclut ce fichier. Message en anglais, `type(area): summary`, par fichier et `git commit -F`, commande commençant par `cd D:/development/repo/RacingGame`.
- **Ne jamais pousser.** `git add` fichier par fichier ; jamais `.serena/`, `.codegraph/`, `log.txt`.
- **R1 — Un lancement à la fois.** Tout lancement du jeu, de l'éditeur ou des tests du moteur se fait sous le verrou `D:/development/rgce-run.lock` (création exclusive, relecture une seconde après, suppression seulement s'il porte encore notre ligne), jamais pendant un build, et après un build à jour. Le jeu lit son contenu dans `RacingGameCasaEngine/bin/Debug/net9.0-windows/Content`, que seul `dotnet build RacingGame.slnx` met à jour. Le verrou d'une autre session n'est ni volé ni supprimé : on attend. Entre deux séries de lancements, le verrou reste libre au moins 60 secondes pour laisser un créneau à la session de rendu.
- **R2 — Défaut du moteur.** Si un comportement du moteur à `aac8911cf` est en cause (échec de ses tests, exception, avertissement de plans non valides, régression d'image que le jeu ne provoque pas), la tâche passe en ⚠️ Blocked avec le constat et la reproduction dans « Points ouverts », et le plan s'arrête. Aucun contournement dans le jeu, aucun commit dans le moteur.
- **R3 — Parité.** Critère : `slice_check.py e5 --ref worlds-as-assets/e2/ref` rend `RESULT: PASS`. Sinon : relever quelles images ou quels points diffèrent et si `-a` et `-b` sont identiques entre eux, mesurer l'écart image par image avec `python -B artifacts/engine-simple-cameras/measure_images.py <racine candidate> <racine de référence> <dossier de sortie>` (sans `-I` : il lui faut `numpy` et `Pillow`), puis ⚠️ Blocked pour la décision de l'auteur (D2). Une ligne `[Warning]`/`[Error]` nouvelle dans un journal est un échec de ce critère.
- **R4 — Code du jeu.** Ce plan ne prévoit aucune modification du code du jeu ni de l'outil. Si le compilateur en exige une, ou si la recuisson change un fichier de `Content`, la tâche passe en ⚠️ Blocked : le relevé serait faux.
- **R5 — Réglages de l'utilisateur** : `front-end-options.json` (SHA-1 `84abb0c0a6bf4d495295b61a6c7e1a43b5040301`) et `display-settings.json` (SHA-1 `18eafcb4c8df587195def3f11aead8a887090e1a`) sont vérifiés avant et après chaque série de lancements et ne sont jamais modifiés.
- **R6 — Mondes.** On ne retouche pas un `.world` à la main ; `RgceAssetCooker --worlds`, puis `python scripts/generate_rgce_content_assets.py`, puis `--worlds --verify`. L'outil n'est jamais lancé sans option.
- **R7 — Build de l'outil.** `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj` à 0 erreur, après `dotnet build RacingGame.slnx`, avant tout appel de `RgceAssetCooker.exe`.
- **Ne rien inventer.** Toute API vient du code lu à `aac8911cf`. Information manquante : ⚠️ Blocked, question dans « Points ouverts », arrêt.
- **Langue** : ce plan en français ; code, commentaires, commits, README, ADR en anglais. Fins de ligne préservées. Python lancé avec `-B` (et `-I` quand `numpy` n'est pas requis).
- Sous-agents : rôles nommés seulement ; relecture du plan par `plan-verifier` avant approbation, vérification de clôture par un `verifier` frais (T4.2).

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Critères d'acceptation

| Réf | Critère | Preuve |
|---|---|---|
| E1 | Le moteur local est à `aac8911cf` ; l'éditeur compile ; les tests du moteur passent sur ce poste, tests nouveaux compris. | T1.1 : `git rev-parse`, build, sortie de `dotnet test` (0 échec, au moins 8533 tests, `CameraDepthRangeTests` présents). |
| E2 | Le jeu et l'outil compilent sur `aac8911cf` sans changement de leur code ; la recuisson des mondes ne change aucun fichier ; les contrôles d'outil sont à 0. | T1.2 : builds, `git status`, sorties des contrôles. |
| E3 | Le banc rend les mêmes images qu'avant : 76 PNG identiques à l'octet entre `-a`, `-b` et `e2/ref` ; export et audits de voitures de M1 ; aucun avertissement nouveau. | T2.1 : `slice_check.py e5 --ref worlds-as-assets/e2/ref`, `RESULT: PASS`. |
| E4 | Les contrôles d'éditeur retenus (P2) tiennent sur l'éditeur de `aac8911cf` : 22 ouvertures sur 22 sans avertissement nouveau ; aller-retour 622 sur 622 et cinq témoins ; échantillon piloté aux mêmes écarts qu'avant. | T3.1 et T3.2. |
| E5 | Les documents du jeu disent ce que fait le moteur à `aac8911cf` pour les plans d'une caméra. | T4.1 : les deux README, `ai-agent/HANDOVER.md`. |
| E6 | Vérification indépendante de l'ensemble. | T4.2 : `verifier` frais, **CONFIRMED**. |

---

## Phase 1 — Le moteur avancé, le jeu dessus

### ✅ T1.1 — Avancer le moteur local et le contrôler (E1)

- Objectif : sous-module et `main` local du moteur à `aac8911cf` ; build de l'éditeur et tests du moteur relevés sur ce poste.
- Fichiers : aucun fichier versionné du jeu, hors ce plan (le pointeur est indexé en T1.2).
- Étapes :
  1. Dans `CasaEngine` : vérifier que `HEAD` est la branche `chantier/rgce-on-origin-main-4` à `164f8d680`, que l'arbre est propre, que `refs/heads/main` vaut `164f8d680`, et `merge-base --is-ancestor 164f8d680 aac8911cf` ; sinon ⚠️ Blocked. Puis `git merge --ff-only aac8911cf373e80f0f8b7c414919b2181b11f284` et `git update-ref refs/heads/main aac8911cf… 164f8d680…` (P1). Vérifier `git status` propre et que `MGUI` et `NvgSharp` ne sont pas signalés modifiés.
  2. `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln` : 0 erreur.
  3. Sous le verrou (R1) : `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj` (avec build, sortie `.trx`). Relever le nombre de tests, le nom de chaque échec et le nombre de tests `CameraDepthRangeTests` dans `artifacts/engine-depth-range/engine-tests.txt`.
- Validation : `git -C CasaEngine rev-parse HEAD refs/heads/main` rend deux fois `aac8911cf…`, et ce commit est un ancêtre de `refs/remotes/origin/main` ou lui est égal ; build de l'éditeur à 0 erreur ; 0 échec de test, au moins 8533 tests, les tests `CameraDepthRangeTests` présents dans le `.trx` (sinon R2).
- Commit : `docs(ai-agent): record the engine advance to the camera depth range`

> Approbation de l'auteur (2026-11-22) : « Approuvé AUTO ».
>
> Validation (2026-11-22), sorties dans `artifacts/engine-depth-range/` :
> - **Avance** (étape 1, P1) : avant, `HEAD` sur `chantier/rgce-on-origin-main-4` = `main` local = `164f8d680`, arbre propre, `origin/main` = `aac8911cf`, `merge-base --is-ancestor` vrai. `git merge --ff-only aac8911cf…` puis `git update-ref refs/heads/main aac8911cf… 164f8d680…`. Après : `git rev-parse HEAD refs/heads/main refs/remotes/origin/main` rend trois fois `aac8911cf373e80f0f8b7c414919b2181b11f284` ; `git status` propre ; `MGUI` (`2f9773d05`) et `NvgSharp` (`9c0da031d`) non signalés modifiés. Aucun commit du moteur, rien de poussé.
> - **Build de l'éditeur** (étape 2) : `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln`, 0 erreur (`build/editor.txt`). Le `CasaEngine.dll` du dossier de build de l'éditeur contient `IsValidDepthRange` et `DefaultNearPlane` : c'est bien le nouveau moteur.
> - **Tests sur `aac8911cf`** (étape 3), sous le verrou : **8562 tests, 8562 réussis, 0 échec** (`engine-tests.txt`, `engine-tests/new.trx`), soit 29 de plus que les 8533 du même jour sur `164f8d680` ; le fichier de résultats contient 27 résultats `CameraDepthRangeTests` et 15 `OrientedSpriteSampleDataTests`. **E1 est tenu.**

### ✅ T1.2 — Le jeu compile sur le nouveau moteur, sans changement (E2)

- Objectif : le pointeur du sous-module est à `aac8911cf` ; le jeu, l'outil et les mondes sont inchangés.
- Fichiers : `CasaEngine` (pointeur), ce plan. Aucun autre (R4).
- Étapes :
  1. `dotnet build RacingGame.slnx` : 0 erreur ; `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj` : 0 erreur (R7).
  2. `RgceAssetCooker --worlds`, `python scripts/generate_rgce_content_assets.py`, puis `--worlds --verify`, `--verify`, `--check` : 0 ; `generate_rgce_content_assets.py --check` : 0 ; `python scripts/check_gameplay_type_names.py` : 0 collision.
  3. `git status --short` : seul `CasaEngine` est modifié (plus ce plan, `.serena/` et `log.txt` non suivis) ; aucun fichier de `RacingGameCasaEngine/Content` ni de code (R4).
- Validation : les deux builds à 0 erreur ; les contrôles de l'étape 2 à 0 ; `--worlds --verify` rend les mêmes nombres qu'avant (6 fichiers, 3341 entités, 0 écart) ; l'étape 3 conforme ; `git ls-tree HEAD CasaEngine` après le commit rend `aac8911cf…`.
- Commit : `chore(casaengine): move the game to the engine's camera depth range`

> Validation (2026-11-22), sorties dans `artifacts/engine-depth-range/build/` et `artifacts/engine-depth-range/t12/` :
> - **Builds** : `dotnet build RacingGame.slnx`, 0 erreur ; `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj`, 0 erreur (R7). Aucune erreur de compilation : le code du jeu et de l'outil ne change pas (R4). Les `CasaEngine.dll` des dossiers de build du jeu et de l'outil contiennent `IsValidDepthRange` : c'est le nouveau moteur.
> - **Mondes** (R6) : `--worlds` écrit **0 fichier, 6 inchangés** ; catalogue inchangé (622 entrées) ; `--worlds --verify` : 6 fichiers, 3341 entités, 3364 composants, 0 écart ; `--verify` : 0 ; `--check` : 0 ; `generate_rgce_content_assets.py --check` : 0 ; `check_gameplay_type_names.py` : 0 collision (123 types du jeu, 3126 du moteur et de l'éditeur). Ces sorties sont celles de T1.2 du plan précédent, à deux lignes près, toutes deux attendues : « 0 files written » au lieu de 4, et 123 types du jeu au lieu de 124 (la classe de caméra de l'étape A n'existe plus).
> - **`git status --short`** après ces commandes : seul `CasaEngine` est modifié ; aucun fichier de `Content` ni de code.
> - **E2 est tenu.**

---

## Phase 2 — Le banc

### ✅ T2.1 — Parité avec la référence (E3)

- Objectif : prouver que le jeu rend les mêmes images sur `aac8911cf` que sur `164f8d680`.
- Fichiers : ce plan ; sorties sous `artifacts/worlds-as-assets/e5/`.
- Étapes :
  1. `dotnet build RacingGame.slnx` à jour (0 erreur), puis `python -I -B artifacts/engine-simple-cameras/parity_runs.py e5` : douze rejeux (six genres, `-a` et `-b`), `smoke`, `profile`, export, deux audits de voitures, chacun sous le verrou ; le lanceur vérifie les mondes du dossier de build et les réglages de l'utilisateur (R5).
  2. `python -B artifacts/worlds-as-assets/scripts/slice_check.py e5 --ref worlds-as-assets/e2/ref`, sortie gardée dans `artifacts/worlds-as-assets/e5/slice_check.txt`.
  3. Chercher dans les journaux de la tranche toute ligne qui contient `depth range` : aucune attendue.
- Validation : `RESULT: PASS` (R3) ; aucune ligne `depth range` ; réglages de l'utilisateur aux empreintes de R5 avant et après.
- Commit : `docs(ai-agent): record the parity of the game on the engine's camera depth range`

> Validation (2026-11-22, jeu `f724ee4`, moteur `aac8911cf`), sorties dans `artifacts/worlds-as-assets/e5/` et `artifacts/worlds-as-assets/e5-runs.txt` :
> - Build du jeu à jour (celui de T1.2, la recuisson n'ayant rien changé) ; le lanceur a constaté que les mondes du dossier de build sont ceux de `Content`, et les réglages de l'utilisateur aux empreintes de M1 avant et après (R5).
> - Les 14 lancements du banc, l'export et les deux audits de voitures sortent en `exit 0`, avec 0 ligne `[Warning]`/`[Error]` au journal de chacun.
> - `slice_check.py e5 --ref worlds-as-assets/e2/ref` : **`RESULT: PASS`**. Point 3 : 76 PNG comparés dans les six genres, identiques à l'octet entre `-a`, `-b` et `e2/ref`. Point 4 : export sans enregistrement sans pair (1437, 2033, 4058). Point 5 : les deux rapports d'audit identiques à ceux de M1 hors la ligne `Generated: `. Point 6 : `--verify`, `--check`, catalogue à 0. Point 1 : 0 collision.
> - Aucun fichier de la tranche ne contient `depth range` (`rg -l -i`).
> - **E3 est tenu.** Sur ce que le banc exerce, les 8 commits du moteur ne changent aucune image du jeu.

---

## Phase 3 — L'éditeur du nouveau moteur (E4)

### ✅ T3.1 — Ouvertures retenues et aller-retour

- Objectif : rejouer, sur l'éditeur de `aac8911cf`, le contrôle d'ouverture sur les 22 assets de P2 et l'aller-retour complet.
- Fichiers : ce plan ; sorties sous `artifacts/engine-depth-range/{open-worlds,open-kinds,roundtrip}/`.
- Étapes :
  1. Builds du jeu, de l'outil et de l'éditeur à jour, 0 erreur ; copier `RacingGameCasaEngine/Content` dans `D:/development/rgce-e5/content` (P3).
  2. `python -I -B scripts/check_editor_opens_assets.py --editor D:/development/repo/RacingGame/CasaEngine/CasaEngine.Editor/bin/Debug/net9.0-windows/CasaEngine.Editor.exe --project D:/development/rgce-e5/content/RacingGameCasaEngine.json --out artifacts/engine-depth-range/open-worlds --timeout 120 --type world --type entity` (8 ouvertures).
  3. Après 60 secondes de verrou libre (R1), le même script avec `--out artifacts/engine-depth-range/open-kinds` et 14 `--only`, chaque nom entre guillemets (l'un contient des espaces) : `Models/CombiBuildings.CombiModel`, `Tracks/TrackBeginner.Track`, `LandscapeHeights.data`, `Textures/NormalizeCubeMap.dds`, `UI\Fonts\GameFont.fnt`, `Models/AlphaDeadTree.gltf`, `Materials/Models/AlphaDeadTree/PDX01 - Defaultb.material`, `Textures/background.png`, `Audio/Beep.sound`, `UI/Sprites/Ui.Menu.Background.sprite`, `Models/AlphaDeadTree.staticModel`, `UI/Sprites/Ui.Menu.BackgroundTexture.texture`, `UI/Screens/Splash/Splash.uiscreen`, `Audio/Beep.wav` (le premier du catalogue pour chaque genre). Si une option du script ne sélectionne pas ce qui est attendu (nombre d'assets jugés différent de 8 puis de 14), ⚠️ Blocked.
  4. `RgceAssetCooker.exe --roundtrip --content D:/development/rgce-e5/content --out artifacts/engine-depth-range/roundtrip`, puis `--roundtrip-controls`.
  5. Lire les avertissements distincts des deux `summary.json` et les comparer à ceux de `artifacts/engine-simple-cameras/open/summary.json` : tout avertissement nouveau est cité ; s'il vient du moteur, R2.
- Validation : 8 sur 8 puis 14 sur 14 ouvertures réussies ; aucun avertissement distinct absent du relevé précédent, aucun qui parle de `depth range` ; aller-retour 622 sur 622 (372 éditables sans perte, 250 en-têtes lisibles) ; cinq témoins détectés.
- Commit : `docs(ai-agent): record the editor checks on the engine's camera depth range`

> Validation (2026-11-22, jeu `f724ee4`, moteur et éditeur `aac8911cf`), lanceur `artifacts/engine-depth-range/t31_run.py`, sorties dans `artifacts/engine-depth-range/` :
> - Builds du jeu, de l'outil (T1.2) et de l'éditeur (T1.1) à jour, 0 erreur ; `RacingGameCasaEngine/Content` copié dans `D:/development/rgce-e5/content` (939 fichiers, P3) ; la DLL de gameplay de la copie a l'empreinte de celle du dossier de build du jeu.
> - **Mondes et voiture** (`open-worlds/summary.json`) : `--type world --type entity` sélectionne 8 assets ; **8 jugés, 8 passent** (7 mondes, 1 entité), 78,6 s d'éditeur.
> - **Un asset par autre genre** (`open-kinds/summary.json`), après 60 secondes de verrou libre : 14 sélectionnés, **14 jugés, 14 passent**, un par genre (`sound`, `wav`, `data`, `material`, `gltf`, `staticmodel`, `combimodel`, `dds`, `png`, `track`, `uiscreen`, `sprite`, `texture`, `fnt`), 114,4 s d'éditeur.
> - **Avertissements distincts** : les trois du relevé précédent (`artifacts/engine-simple-cameras/open/summary.json`) et aucun autre : les deux de déchargement de scripts (une fois par lancement) et celui d'`OverlayViewPipeline` (3 fois, sur des mondes). Aucun fichier des deux dossiers ne contient `depth range`.
> - **Aller-retour** (`roundtrip/roundtrip.json`, `roundtrip-stdout.txt`) : la sortie est celle du plan précédent ligne pour ligne, hors durées et chemin du rapport : **622 sur 622**, aucun échec. `--roundtrip-controls` : **les cinq témoins détectés sous leur nom**.
> - Aucun verrou ni processus d'éditeur à la fin.

### 🚧 T3.2 — Échantillon piloté sur un monde à caméra

- Objectif : rejouer l'échantillon piloté du plan précédent (modifier, enregistrer, rétablir par l'éditeur piloté) : c'est le contrôle qui fait écrire par l'éditeur les plans d'une caméra dans un fichier de monde.
- Fichiers : ce plan ; `artifacts/engine-depth-range/driven/driven_run.py`, copie de `artifacts/engine-simple-cameras/driven/driven_run.py` (P6) ; l'original et ses sorties ne sont pas touchés.
- Étapes : même déroulé qu'en T3.3 du plan précédent, sur la copie de contenu, sous le verrou ; comparaison des fichiers enregistrés à ceux du plan précédent (`artifacts/engine-simple-cameras/driven/`), fichier par fichier.
- Validation : pour les six assets hors monde, mêmes écarts qu'au plan précédent et pas d'autre. Pour `Worlds/FrontEnd.world`, après le rétablissement : les cinq différences du plan précédent, toutes dans le nœud de caméra (`view_distance` 0 → 999, `viewport` `w` et `h` à la taille de la fenêtre, `min_depth` 0 → 1, `max_depth` 0 → 1000), et pas d'autre. Ces plans, 1 et 1000, sont valides et égaux aux valeurs par défaut du moteur (`CameraComponent.DefaultNearPlane`, `DefaultFarPlane`, lus dans `NEW`) : un monde enregistré par l'éditeur se rend donc comme le fichier de l'outil, ce qui est déduit du code et non mesuré par une image. À la fin, la copie de contenu est identique à la source, aucun verrou, aucun processus d'éditeur. Toute autre différence : citée et jugée, ou ⚠️ Blocked.
- Commit : `docs(ai-agent): record the driven editor sample on the engine's camera depth range`

---

## Phase 4 — Documents, vérification, clôture

### ⏳ T4.1 — Documents (E5)

- Objectif : les documents du jeu disent ce que le moteur fait à `aac8911cf`.
- Fichiers : `RacingGameCasaEngine/Assets/README.md` (ligne 111 : l'initialisation de la caméra remplace la taille et recalcule la distance ; elle garde les plans du fichier quand ils sont valides, ce qui est le cas des 1 et 1000 que l'éditeur écrit, et met 1 et 1000 quand le fichier porte des zéros, ADR-0153 du moteur) ; `scripts/RgceAssetCooker/README.md` (lignes 88 et 89 : le jeu lit les plans du fichier quand ils sont valides ; ceux de l'outil sont à zéro) ; `ai-agent/HANDOVER.md` (section 17 : le moteur à `aac8911cf`, ce qui est mesuré, la phrase de l'ADR-0042 qui décrit le moteur d'avant, le point ouvert O1) ; `ai-agent/README.md` (ligne de ce plan) ; ce plan. Pas d'ADR (P4).
- Validation : `rg "overwrites the size, the depths|never reads those values"` hors `artifacts/`, `ai-agent/` et `docs/decisions/` : aucune occurrence ; chaque phrase nouvelle est relue contre `NEW/CasaEngine/Framework/Scene/Entities/Components/CameraComponent.cs`.
- Commit : `docs(racing-casa): say what the engine keeps of a camera's depth range`

### ⏳ T4.2 — Vérification indépendante (E6)

- Objectif : un `verifier` frais juge E1 à E5 sur l'état du commit de T4.1 ; il reçoit les critères, les chemins des sorties et ce plan ; il rejoue ce qu'il juge utile sous le verrou.
- Validation : **CONFIRMED** ; un constat P0 à P2 introduit est traité ou arrête le plan ; P3 et P4 sont notés.
- Commit : `docs(ai-agent): record the closing verification of the camera depth range integration`

### ⏳ T4.3 — Clôture

- Étapes : regarder puis supprimer `D:/development/rgce-e5/` (P3) ; vérifier qu'il ne reste ni verrou ni processus de cette session ; `git status --short` ; état final dans `ai-agent/README.md` et `ai-agent/HANDOVER.md` ; prévenir la session de rendu de la nouvelle base (`aac8911cf`), à titre d'information.
- Validation : aucune tâche en 🚧 ; rien de poussé ; `remaster` inchangé (P8).
- Commit : `docs(ai-agent): close the camera depth range integration plan`

---

## Relectures du plan

- **2026-11-22, `plan-verifier` frais : READY**, sur le plan entier jugé comme une seule unité.
- **Deux contrôles de faits en lecture seule, en parallèle** (`scout`, côté moteur et côté jeu) : toutes les affirmations de « État vérifié » relues sont confirmées, ainsi que les noms, options et chemins des scripts que le plan appelle (`--type` répété fait une union, `--only` et `--type` ensemble une intersection ; les 14 noms sont dans le catalogue ; `parity_runs.py` prend n'importe quel nom de tranche ; `slice_check.py --ref` ne déplace que la référence des images et des journaux, l'export et les audits de voitures restant comparés à M1). Recherche contradictoire : aucun autre code du jeu ou de l'outil ne règle les plans d'une caméra, ne lit ceux d'une caméra du moteur, ni ne fait rejoindre un monde deux fois à une caméra ; dans le moteur, aucun réglage du seul plan proche avant l'entrée dans un monde (le cas qui produirait le nouvel avertissement).
- Après ces relectures, quatre précisions de forme ont été portées au plan (« hors tests », les arguments de `measure_images.py`, le chemin de l'éditeur, les guillemets des `--only`) et le point O2 ajouté ; ni le périmètre, ni les critères, ni les étapes ne changent.

## Hors périmètre

- **Donner aux caméras de course les plans de l'original** (voir O1) : aucune modification du code ni des mondes du jeu.
- Les 622 ouvertures de l'éditeur et le contrôle des composants proposables (P2).
- La scène d'exemple et les tests du moteur ajoutés par les 8 commits, au-delà de leur exécution en T1.1.
- Le push, l'avance de `remaster` (P8), et la réunion avec les branches de rendu, qui se fait dans la session de rendu.
- L'attente après relâchement du verrou dans `scripts/check_editor_opens_assets.py`, toujours notée et non faite.

## Points ouverts

| Réf | Point | Tâche |
|---|---|---|
| O1 | **Information, ne bloque pas ce plan.** En course, la caméra du jeu rend entre 1 et 1000 (valeurs par défaut du moteur) : aucun code ne lui donne d'autres plans. L'original rend entre 0,5 et 1750 (`RacingGameOriginal/Core/Graphics/BaseGame.cs:47-48`) ; seuls le carrousel et les vues de référence du banc reprennent ces valeurs, par code. Depuis l'ADR-0153 du moteur, ces plans pourraient s'écrire dans les mondes. Ce serait un changement d'image et une décision de l'auteur : non traité ici. | — |
| O2 | **Information pour le dépôt du moteur, non vérifiée par la session principale.** L'ADR-0153 du moteur dit que 16 caméras des données du moteur portent 1 et 1000 et 2 portent 0 et 0 ; le contrôle de faits en compte 17 et 2 dans l'arbre à `164f8d680`. Sans effet sur ce plan ; à regarder dans le moteur si l'auteur le souhaite. | — |
