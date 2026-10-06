# Plan agent IA — Volume de Beep et Bleep, scintillement des ombres de course (RacingGameCasaEngine et CasaEngine)

Plan d'exécution de la demande de l'auteur du 2026-10-06 : « corriges : Beep et Bleep ; les ombres ». Ces deux points étaient restés ouverts dans les chantiers précédents :
- volume de Beep et Bleep (T1.3 de `rgce-title-car-selection-xna-look-tasks.md`) ;
- scintillement des ombres en mouvement (O1 de `rgce-hud-shadows-start-tasks.md`).

Les décisions D1 → D4 ci-dessous ont été arbitrées avec l'auteur le 2026-10-06 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

- Beep et Bleep, les sons du feu de départ, sont joués au volume de l'original.
- Les ombres de course ne rampent plus quand la caméra avance. Le correctif est dans CasaEngine : la carte d'ombres suit la caméra par pas entiers de texel.
- Le sol ne projette plus d'ombre, il la reçoit seulement, comme dans l'original.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-06)

Découverte en lecture seule : workflow de 2 agents, chacun contre-vérifié par un agent adverse. Les faits porteurs ont été relus directement.

- **Dépôts** :
  - RacingGame est sur la branche `remaster`, HEAD `f42c0c7`, 18 commits en avance sur `origin/remaster`. `.serena/` et `log.txt`, non suivis, sont à l'auteur.
  - Le pointeur du sous-module `CasaEngine` enregistré est `d79ed16` ; l'arbre de travail extrait `dd91efe` (`git ls-tree HEAD CasaEngine`, `git -C CasaEngine rev-parse HEAD`). `d79ed16` est un ancêtre de `dd91efe`, 49 commits plus tôt. Aucun fichier d'ombre ne diffère entre les deux.
  - Le sous-module est sur `main`, propre, au niveau d'`origin/main`.
  - Règles moteur (`CasaEngine/AGENTS.md`) :
    - branche par chantier créée depuis `main` (nommage `chantier/…`, ex. `chantier/mouse-has-moved-abs`) ; jamais de commit sur `main` (hook) ; jamais de push ;
    - plan dès plus d'un commit ;
    - `dotnet build CasaEngine.MonoGame.sln` et `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj` ;
    - documents de `docs/` en anglais ; ADR pour une décision d'architecture, de format, d'API publique ou de backend (§10) ;
    - lancer une fois la démo de la zone touchée (§6).
- **Beep et Bleep** :
  - Le projet XACT d'origine les joue à `Volume = -1200`, en centièmes de dB (`RacingGame/Content/Audio/RacingGame.xap:2271` pour Bleep, `:2318` pour Beep). Leur catégorie `Default` et leur piste sont à 0.
  - −12 dB donnent un volume linéaire de 10^(−12/20) = 0,251, avec la conversion des sons de menu (−600 → 0,501).
  - `RacingGameCasaEngine/Content/Audio/{Beep,Bleep}.sound` sont à `"volume": 1.0`. Le moteur lit ce champ (`CasaEngine/CasaEngine/Framework/Audio/SoundAsset.cs:74`) et l'applique à la voix (`:63`). `RaceStartLight.cs:112` les joue sans surcharge de volume.
- **Ombres, moteur** (`CasaEngine/CasaEngine/Framework/Rendering/Draw/ShadowPass.cs`) :
  - **Construction de la carte** :
    - une seule carte orthographique de 2 × `MaxDistance` de côté ;
    - centrée sur `context.Frame.CameraPosition` (`:59-62`, `:239-240`), donc sur l'œil de la caméra, et recalculée à chaque image ;
    - base de la lumière tirée de sa seule direction, avec un `up` fixe (`:241`) ;
    - aucun arrondi ni alignement sur les texels (`:226-246`).
  - La résolution est une variable locale (`:50`), et l'atlas fait exactement cette taille (`:165-176`).
  - RGCE règle `Resolution` 4096, `MaxDistance` 150, `DepthBias` 0,001 et `NormalBias` 0,4 (`RacingGameCasaEngine/Worlds/RaceWorldFactory.cs:41-44`, `:106-110`). Un texel couvre donc 300 / 4096 = 0,073 unité.
  - **Cause du scintillement** :
    - à 170 mph, la caméra avance d'environ 0,37 unité par image, soit 5 texels, jamais un nombre entier ;
    - chaque image décale donc la grille de la carte d'une fraction de texel par rapport au monde, et les bords d'ombre fixes rampent.
  - **Ce qui ne bouge pas** : la lumière qui projette (`Light.Key`, `RaceWorldFactory.cs:168-175`, entité statique) ; la base, l'emprise et la plage de profondeur, qui ne dépendent pas de l'orientation de la caméra.
  - Le correctif minimal tient dans `BuildDirectionalShadowViewProjection` (`:226-246`) et son seul appel. Cette matrice sert aussi aux maillages skinnés et au binder des receveurs.
- **Tests moteur** :
  - `CasaEngine.Tests/Rendering/ShadowPipelineCoverageTests.cs:38-47` vérifie des chaînes littérales du source de `ShadowPass.cs` (`EffectiveCastShadows`, `ShadowLightType.Directional`, `!context.Shadows.Settings.Enabled`, `SurfaceFormat.Single`…) ;
  - aucun test ne couvre le calcul de la matrice ;
  - `CasaEngine/InternalsVisibleTo.Tests.cs:3` ouvre les membres `internal` à `CasaEngine.Tests`.
- **Démo moteur** : `CasaEngine.Demos/Demos/StaticShadowValidationDemo.cs`. `DemosGame` la lance sans interaction avec `CASAENGINE_START_DEMO`, et capture puis quitte avec `CASAENGINE_CAPTURE_SCREENSHOT_PATH` et `CASAENGINE_CAPTURE_SCREENSHOT_DELAY_MS` (`DemosGame.cs:107`, `:315`, `:326`).
- **Doc moteur** : `docs/engine/light-component.md`, section « Limites V1 du workflow shadows » (`:43-49`). Elle est rédigée en français sans accents, alors que la règle moteur demande l'anglais pour `docs/`.
- **Sol** :
  - RGCE crée `Track.Ground.<piste>` à deux endroits :
    - le terrain (`LegacyTrackSceneFactory.Terrain.cs:48`) ;
    - une boîte de repli (`LegacyTrackSceneFactory.cs:395-410`).
  - Les deux passent par `CreateStaticModelEntity`, dont le composant `StaticModelComponent` projette par défaut (`PrimitiveComponent.cs:14`).
  - Le carrousel coupe déjà ses plateaux de cette façon (`CarSelectionCarousel.cs:124`, `CastShadows = false`).
  - L'original ne faisait projeter que la route, les objets proches et la voiture : « Don't generate shadow for the landscape, it only receives shadow! » (`git show 4f840a3^:RacingGame.Shared/Landscapes/Landscape.cs`, `GenerateShadow`).
- **Effets que ce chantier ne corrige pas** :
  - Le moteur élimine par le cadre de la caméra les objets avant de dessiner les ombres (`World.cs:816-817`) : l'ombre d'un objet hors champ peut apparaître ou disparaître.
  - Le filtrage est un PCF fixe de 4 échantillons (`Lighting.fxh:129-139`), alors que l'original floutait son masque d'ombre à l'écran (`PostScreenShadowBlur.fx`) et l'estompait au bord (`ShadowMap.fx:221`).
- **Plan des ombres précédent** (`rgce-hud-shadows-start-tasks.md`) : son état vérifié (lignes 43-47 : « RGCE ne règle que Enabled », « carte 1024 floutée ») est périmé ou inexact. L'original utilise une carte 2048 par défaut, et c'est son masque d'ombre à l'écran qui est flouté. Son O1 et sa D1 sont repris ici.
- **Validation RGCE** : pas de projet de test. Les modes `--smoke-frontend`, `--capture-ui-screens` et `--capture-track-audit` (`Program.cs:22-28`) existent, mais aucun ne rend d'images successives avec une caméra qui bouge.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | **Beep et Bleep au volume de l'original**, −12 dB, soit 0,251 (auteur, 2026-10-06). |
| D2 | **Scintillement corrigé dans CasaEngine** (auteur, 2026-10-06), sur une branche du dépôt moteur. Le pointeur du sous-module de RacingGame est ensuite déplacé ; il enregistre aussi les 49 commits `d79ed16` → `dd91efe`, conséquence présentée à l'auteur avec l'option. Pour ce chantier, cela remplace la règle « ne jamais modifier CasaEngine » des plans précédents (D6 de la sélection de piste, D4 des ombres). |
| D3 | **Le sol reçoit les ombres sans en projeter**, comme l'original ; effet mesuré avant et après (auteur, 2026-10-06). Remplace en partie D1 de `rgce-hud-shadows-start-tasks.md` (« tous les objets gardent leur ombre »). |
| D4 | **Rien de plus sur les ombres** : l'apparition et la disparition d'ombres d'objets hors champ, les bords durs et la direction de la lumière restent tels quels (auteur, 2026-10-06, option non retenue). |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **Branches** :<br>- RacingGame : `remaster` ;<br>- CasaEngine : `chantier/shadow-texel-snapping`, créée depuis `main` (`dd91efe`).<br>Sans push : le commit RacingGame qui déplace le pointeur désigne un commit moteur local tant que l'auteur ne pousse pas la branche moteur. Le pointeur n'est indexé que dans ce commit (T2.1), jamais ailleurs. |
| P2 | **Correctif moteur** :<br>- `BuildDirectionalShadowViewProjection` reçoit la résolution ;<br>- il arrondit la translation de la vue de lumière (`view.M41`, `view.M42`) au multiple le plus proche de la taille d'un texel, `2 · max(1, MaxDistance) / Resolution`.<br>Base, emprise et profondeur sont inchangées. Pas de nouveau réglage ni de sérialisation. La méthode passe de `private` à `internal` pour le test. |
| P3 | **Un seul commit moteur**, code, test et doc ensemble : `fix(rendering): snap the directional shadow map to whole texels`. Donc ni plan moteur, ni ADR moteur : c'est une correction de comportement, sans API publique, format ni backend (§10). Dans `docs/engine/light-component.md`, une ligne est ajoutée aux « Limites V1 », dans la langue de la section (français sans accents) pour rester homogène. |
| P4 | **Test moteur** `CasaEngine.Tests/Rendering/ShadowPassTexelSnappingTests.cs` :<br>- pour des déplacements de caméra sous le texel, de plusieurs texels et sur les trois axes, la position en texels d'un point fixe du monde change d'un nombre entier (partie fractionnaire identique à 1e-3 près) ;<br>- le test est écrit avant le correctif, et il échoue sans lui ;<br>- `ShadowPipelineCoverageTests` reste vert. |
| P5 | **Preuve de bout en bout**, deux mesures avant et après le correctif :<br>- **Sonde** dans le scratchpad, jamais commitée : une copie de RGCE, course en pause, dont un pipeline de vue décale seulement l'ancre de la carte d'ombres (`CameraPosition`) de 0 ; 0 (répété) ; 0,25 ; 0,5 ; 0,75 ; 1,25 et 2,5 texels dans le plan de la lumière. La vue réelle reste fixe. On compte les pixels qui changent de plus de 16 niveaux.<br>  - Avant : changements aux bords d'ombre.<br>  - Après : quasi nuls. Il reste au plus un léger écart spéculaire, puisque l'œil se décale de l'arrondi.<br>- **Démo moteur** `StaticShadowValidationDemo`, capturée par ses variables d'environnement : écarts limités aux bords d'ombre (décalage d'un demi-texel au plus). |
| P6 | **Sol receveur** (D3) : `CastShadows = false` sur le composant des deux entités `Track.Ground.*`, comme les plateaux du carrousel. La route, les rails, le décor et la voiture projettent toujours. |
| P7 | **Biais après D3** : `NormalBias` 0,4 avait été choisi contre l'acné du sol (T3.1 du plan des ombres), au prix d'ombres environ 10 à 17 % plus fines. Une fois le sol receveur seul, comparer 0,4, 0,2 et 0,1 avec `--capture-track-audit`, sur des planches recadrées sur l'ombre du rail et de la voiture, en cherchant l'acné sur la route, les rails et le décor. Garder la plus petite valeur sans acné ; sinon garder 0,4. |
| P8 | **Validation par tâche** :<br>- `dotnet build RacingGame.slnx`, qui compile aussi le moteur du sous-module : 0 erreur, aucun avertissement dans RGCE ;<br>- `--smoke-frontend` code 0 sans avertissement ;<br>- `--capture-ui-screens` contre `references-504c603`, écarts attendus nommés par tâche ;<br>- réglages de l'auteur restaurés à l'identique ;<br>- en plus pour T2.1 : build et tests du moteur, démo ;<br>- 🧪 pour l'auteur : écoute des bips, ombres en mouvement. |

## Règles d'exécution pour l'agent

- **Branches** (P1) : `remaster` pour RacingGame, `chantier/shadow-texel-snapping` pour CasaEngine. Jamais de commit sur `master` ni sur le `main` du moteur.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
  - Exception : T2.1 a un commit dans chaque dépôt. La mise à jour du plan va dans le commit RacingGame.
- **Commits** atomiques et compilables, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Les commits commencent par `cd D:/development/repo/RacingGame &&` (RacingGame) ou `cd D:/development/repo/RacingGame/CasaEngine &&` (moteur) : le hook de commit de l'auteur résout le dépôt par `git -C` sous PowerShell, qui ne lit pas les chemins `/d/...`.
- **Ne jamais pousser**, dans aucun des deux dépôts.
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Pour T2.1, s'y ajoutent `dotnet build CasaEngine.MonoGame.sln` et `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj`.
  - Les échecs de test déjà présents sur `main` sont relevés avant la modification et ne bloquent pas ; un nouvel échec bloque.
- **CasaEngine** : seuls `ShadowPass.cs`, le nouveau test et `docs/engine/light-component.md` sont modifiés (D2, P2-P4). MGUI n'est jamais modifié.
- Si le code est écrit mais qu'une vérification visuelle ou manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** les modifications préexistantes de l'auteur : `git add` fichier par fichier. `.serena/` et `log.txt` ne sont jamais indexés ; le pointeur `CasaEngine` ne l'est qu'en T2.1.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais, sauf la ligne de P3.
- **Moteur** : pas d'allocation, de LINQ ni de closure dans la passe d'ombres (chemin chaud, `AGENTS.md` du moteur §9.3).
- **Sondes** : uniquement dans le scratchpad, sur une copie ; jamais d'envoi de touches ni de mouvements de souris simulés.
- **Réglages de l'auteur** (`display-settings.json`, `front-end-options.json`) : sauvegardés avant les runs et restaurés à l'identique après (même SHA-1).
- **Budget** : au plus 3 cycles « correction → build ou validation » par tâche. Un 4e échec passe la tâche en ⚠️ Blocked, avec l'erreur et la question dans « Points ouverts », puis arrêt.
- **Retour arrière** :
  - avant commit : supprimer les seuls fichiers créés par la tâche et faire un `git restore` des seuls fichiers suivis qu'elle a modifiés ;
  - après commit : `git revert <SHA>` dans le dépôt concerné ;
  - jamais de `git checkout` de fichiers, `git reset --hard`, `git clean` ni `git stash`. Le seul `git checkout` permis est la création de la branche moteur (`git -C CasaEngine checkout -b chantier/shadow-texel-snapping`).

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
- `dotnet build CasaEngine.MonoGame.sln` et `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj` sur la branche moteur : 0 erreur, aucun nouvel échec.
- `--smoke-frontend` et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]`. Deux runs de capture identiques ; contre `references-504c603`, seuls les états de course changent, aux ombres.
- Sonde de P5 et démo moteur : le scintillement mesuré disparaît.
- Passe de vérification indépendante sur le résultat final.
- Vérification manuelle par l'auteur :
  - les bips du feu de départ ;
  - les ombres en mouvement : route, rails, décor, et l'ombre de la voiture ;
  - plus d'ombre projetée par le sol.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan et index

- Objectif : ce plan, ajouté à `ai-agent/README.md`.
- Fichiers : `ai-agent/tasks/rgce-start-beeps-shadow-shimmer-tasks.md`, `ai-agent/README.md`.
- Validation : plan approuvé par l'auteur.
- Commit : `docs(racing-casa): plan the start beeps volume and the shadow shimmer fix`

> Validation (2026-10-06) : relecture indépendante du plan (`plan-verifier`) READY ; plan approuvé par l'auteur, mode AUTO.

## Phase 1 — Sons

### 🧪 T1.1 — Beep et Bleep à −12 dB

- Objectif : D1.
- Fichiers : `RacingGameCasaEngine/Content/Audio/Beep.sound`, `Bleep.sound`.
- Étapes : passer `"volume"` de `1.0` à `0.251`, comme les sons de menu.
- Validation :
  - build : les deux `.sound` copiés dans la sortie portent 0,251 ;
  - `--smoke-frontend` code 0 sans avertissement, et le journal montre les lignes « Start light: … played » ;
  - 🧪 écoute par l'auteur.
- Commit : `fix(racing-casa): play Beep and Bleep at the original -12 dB`

> Validation (2026-10-06) :
> - `Beep.sound` et `Bleep.sound` à `"volume": 0.251` ; les copies de la sortie de build portent 0,251.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
> - `--smoke-frontend` : code 0, journal sans `[Warning]` ni `[Error]` ; « Start light: red, beep played », « yellow, beep played », « green, bleep played » ; réglages restaurés à l'identique.
> - 🧪 Reste pour l'auteur : l'écoute des bips au départ d'une course.

## Phase 2 — Ombres

### 🧪 T2.1 — Carte d'ombres alignée sur les texels (CasaEngine)

- Objectif : D2, P2 à P5.
- Fichiers :
  - CasaEngine : `CasaEngine/Framework/Rendering/Draw/ShadowPass.cs`, `CasaEngine.Tests/Rendering/ShadowPassTexelSnappingTests.cs` (nouveau), `docs/engine/light-component.md` ;
  - RacingGame : le pointeur `CasaEngine`, ce plan.
- Étapes :
  1. **Mesures avant** :
     - tests du moteur sur `main`, échecs déjà présents relevés ;
     - sonde de P5 et capture de la démo.
  2. Créer la branche moteur (P1). Écrire le test (P4) et vérifier qu'il échoue.
  3. Corriger `ShadowPass.cs` (P2). Le test passe.
  4. Builds, tests et doc moteur (P3), puis commit moteur.
  5. **Mesures après** : sonde, démo, puis build, smoke et captures RGCE.
  6. Commit RacingGame : pointeur et plan.
- Validation :
  - tests du moteur : le nouveau test passe, aucun nouvel échec ;
  - `CasaEngine.MonoGame.sln` et `RacingGame.slnx` : 0 erreur ;
  - sonde : les pixels changés passent de « bords d'ombre » à quasi nuls ;
  - démo : écarts aux seuls bords d'ombre ;
  - captures contre `references-504c603` : seuls `race-hud`, `pause` et `race-finished` changent, aux bords d'ombre ;
  - 🧪 ombres en mouvement par l'auteur.
- Commits :
  - CasaEngine : `fix(rendering): snap the directional shadow map to whole texels`
  - RacingGame : `chore(racing-casa): move CasaEngine to the shadow texel snapping fix`

> Validation (2026-10-06) :
> - **Moteur**, branche `chantier/shadow-texel-snapping`, commit `7a44aba` :
>   - `BuildDirectionalShadowViewProjection` reçoit la résolution, devient `internal`, et arrondit `view.M41` et `view.M42` au multiple d'un texel ; ligne ajoutée aux « Limites V1 » de `docs/engine/light-component.md`.
>   - `ShadowPassTexelSnappingTests` (7 cas), écrit avant le correctif : 4 échecs sans lui (les 3 cas « caméra au centre de la carte » passaient déjà), 7 réussites avec.
>   - `dotnet test CasaEngine.Tests/CasaEngine.Tests.csproj` : 2 457 réussis, 0 échec (2 450 réussis sur `main` avant). `dotnet build CasaEngine.MonoGame.sln` : 0 erreur, aucun avertissement dans les fichiers touchés.
>   - Démo `StaticShadowValidationDemo` capturée sans interaction : deux captures « avant » identiques ; « après », 115 pixels changent, tous au bord de l'ombre de la colonne de gauche (décalage unique de moins d'un demi-texel).
> - **Sonde** (copie de RGCE, course en pause, vue fixe, ancre de la carte décalée de 0,25 à 2,5 texels). Pixels changés de plus de 16 niveaux hors de la voiture :
>   - avant : 190, 397, 491, 341 et 495 (ombres des rails sur la route, ombre de la voiture) ;
>   - après : 0 pour tous les décalages ;
>   - sur la voiture, 1 à 72 pixels, les mêmes avant et après : ses reflets dépendent de la position de l'œil, que la sonde déplace avec l'ancre.
>   - Premier essai écarté : la sonde attendait 20 mises à jour, mais l'encodage synchrone d'une capture fait enchaîner des mises à jour de rattrapage sans rendu. Elle attend désormais 5 rendus de la vue ; un décalage répété et le retour à 0 donnent alors 0 pixel changé.
> - **RGCE** : `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ; `--smoke-frontend` code 0 sans avertissement ; deux runs `--capture-ui-screens` identiques entre eux (à quelques pixels 3D isolés près, comme avant) ; réglages restaurés à l'identique.
> - **Captures** contre `references-504c603` :
>   - `race-hud`, `pause` et `race-finished` : 1,5 à 1,6 % des pixels, aux bords d'ombre (la grille de la carte se décale une fois) ;
>   - `car-selection` : 1 426 pixels, en liserés au bord des ombres des voitures sur les plateaux. Écart que la validation prévue n'annonçait pas : le carrousel a ses propres ombres (`CarSelectionCarousel.cs:104-107`) et profite du même alignement ;
>   - `options` : 6 042 pixels sur le champ « Player One », l'écart de survol déjà connu de cette référence ;
>   - les autres états sont identiques.
> - **Pointeur `CasaEngine`** : `d79ed16` → `7a44aba`, soit les 49 commits de `main` (`dd91efe`) plus le correctif. La branche moteur n'est pas poussée.
> - 🧪 Reste pour l'auteur : les ombres en mouvement (route, rails, décor, voiture).

### ⏳ T2.2 — Sol receveur seul

- Objectif : D3, P6, P7.
- Fichiers : `RacingGameCasaEngine/Worlds/LegacyTrackSceneFactory.cs`, `LegacyTrackSceneFactory.Terrain.cs`, et `RaceWorldFactory.cs` si P7 change le biais.
- Étapes :
  1. `--capture-track-audit` avant le changement.
  2. `CastShadows = false` sur le composant des deux entités `Track.Ground.*`.
  3. Audits avec `NormalBias` 0,4, 0,2 et 0,1 ; planches recadrées ; choix selon P7.
- Validation :
  - build ; smoke ;
  - planches : plus d'ombre projetée par le sol ; route, rails, décor et voiture projettent toujours ; pas d'acné ;
  - captures : seuls les états de course changent ;
  - 🧪 par l'auteur.
- Commit : `fix(racing-casa): let the ground only receive shadows, as RacingGame did`

## Phase 3 — Clôture

### ⏳ T3.1 — Validation globale, vérification indépendante et rapport

- Objectif : « Validation globale », nouvelles références de capture, rapport de fin, index.
- Fichiers :
  - ce plan, `ai-agent/README.md` ;
  - une note sous O1 et sous D1 de `rgce-hud-shadows-start-tasks.md`, renvoyant vers ce plan ;
  - une note sous T1.3 de `rgce-title-car-selection-xna-look-tasks.md`.
- Validation : liste « Validation globale ».
- Commit : `docs(racing-casa): close the start beeps and shadow shimmer plan`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| — | Aucun à l'écriture du plan. | — |

## Hors périmètre

- L'apparition et la disparition d'ombres d'objets hors champ, dues à l'élimination par le cadre de la caméra dans le moteur (D4).
- Le flou d'écran et l'estompe au bord de l'original, le filtrage des ombres, la direction de la lumière (D4).
- L'ombre des objets en mouvement, dont la voiture : leur position dans la grille de la carte change avec eux. C'est le comportement normal d'une carte d'ombres, que l'alignement ne vise pas.
- Le pointeur `CasaEngine` hors de T2.1, le push des deux branches et le merge du moteur sur `main` : décisions de l'auteur.
- L'ADR-0012 et les remarques reportées du plan de la sélection de piste.
