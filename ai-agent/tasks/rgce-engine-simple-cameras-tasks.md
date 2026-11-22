# Plan agent IA — Le jeu sur le moteur d'`origin/main` (caméras simples)

Plan d'exécution de l'intégration, dans le jeu, des 85 commits que `origin/main` du moteur a reçus depuis un autre poste (`01cdf483f` → `164f8d680`). Demande de l'auteur du 2026-11-22 : « Oui » à « intégrer ces 85 commits du moteur et vérifier le jeu dessus (build, parité) ».
Les décisions D1 et D2 ci-dessous ont été arbitrées avec l'auteur le 2026-11-22 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Le jeu `RacingGameCasaEngine`, ses outils et son projet d'éditeur fonctionnent sur le moteur à `164f8d680`, avec la preuve de ce qui change et de ce qui ne change pas :

1. le `main` local du moteur et le pointeur du sous-module du jeu sont à `164f8d680` ;
2. le jeu compile et **rend les mêmes images, à l'octet, que la référence de M1** tant qu'il garde l'ancienne formule de vue (étape A) : cela prouve que rien d'autre, dans les 85 commits, ne change ce que le banc exerce (six genres de captures, `smoke`, `profile`, export, audits de voitures ; ce qu'il n'exerce pas est listé dans « État vérifié ») ;
3. le jeu passe à la caméra du moteur, `PerspectiveCameraComponent` (étape B), et l'écart d'image que cela produit est **mesuré**, puis soumis à l'auteur s'il n'est pas nul (D1) ;
4. les contrôles du projet d'éditeur de S7 tiennent sur l'éditeur du nouveau moteur.

Ce que le plan ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-11-22)

Relevé en lecture seule (quatre lecteurs indépendants, faits décisifs relus par la session principale). `OLD` = arbre du sous-module à `01cdf483f` ; `NEW` = fichiers à `164f8d680`, exportés par `git archive` hors du dépôt.

- **Dépôts.** Jeu : branche `chantier/rgce-engine-simple-cameras` créée depuis `d71dc65` (= `remaster` `3e33238`, poussé, plus un commit de note locale) ; arbre propre hors `.serena/` et `log.txt`, non suivis, jamais indexés. Moteur : `HEAD` sur `chantier/remote-path-segments` = `main` local = `01cdf483f` ; `origin/main` = `164f8d680` ; `git merge-base --is-ancestor main origin/main` : vrai ; `git rev-list --left-right --count main...origin/main` : `0 85`. Les 85 commits ne touchent ni `MGUI`, ni `NvgSharp`, ni un `.fx`, ni un `.props`/`.targets` (`git diff --name-status`, 208 fichiers).
- **Caméras supprimées par le moteur** (ADR-0149 du moteur) : `ArcBallCameraComponent`, `Camera3dComponent`, `CameraLookAtComponent`, `CameraTargeted2dComponent`. Le jeu n'utilise que `CameraLookAtComponent` : `RacingGame.Gameplay/Components/ChaseCameraRigComponent.cs` (75, 124, 137, 229), `DebugFreeCameraComponent.cs` (34, 125, 127), `RacingGame.Gameplay/Race/RaceWorldTemplates.cs` (273), `RacingGameCasaEngine/UI/CarSelectionCarousel.cs` (65, 111, 115), `RacingGameCasaEngine/Bootstrap/ReferenceViewCaptureValidator.cs` (538, 549, 562, 565), `TrackMigrationCaptureValidator.cs` (304, 349, 395, 436, 479, 484, 487, 497) ; et quatre mondes, nœud `"type": "CameraLookAtComponent"` : `Content/Worlds/FrontEnd.world` et `Content/Worlds/Race/Track{Beginner,Advanced,Expert}.world`.
- **Un type inconnu ne casse pas le chargement, il retire la caméra** : `ElementFactory.LoadComponent` (inchangé) journalise une erreur et rend un `UnresolvedComponent` ; il n'y a ni alias ni traduction (ADR-0149, `docs/engine/perspective-camera-component.md` : « Edit the file once »).
- **Vue de l'ancienne caméra** (`OLD CameraLookAtComponent.cs:54-63`) : `Matrix.CreateLookAt(Position, Target, Up)`, où `Up` est le haut de l'orientation courante du composant ; `SetPositionAndTarget` écrit `Position` et `Target`, pas l'orientation. `Target` n'est pas sérialisé.
- **Vue de la nouvelle caméra** (`NEW PerspectiveCameraComponent.cs:81-90, 131-148`) : `Matrix.Invert(WorldMatrixNoScale)`, recalculée quand la transformation change ; `SetPositionAndTarget` écrit `LocalPosition` et `LocalOrientation` à partir de `Invert(CreateLookAt(position, target, Vector3.Up))` et **lève une `ArgumentException`** si la cible est à la position ou à la verticale (|cos| > 0,99999). Il n'y a plus de `Target`. Le test du moteur garantit l'égalité avec `CreateLookAt` à `1e-5`, pas à l'octet. La projection est le même appel quand `PictureOffset` est nul. La classe n'est pas scellée ; `ComputeViewMatrix` et `IsViewOutOfDate` sont `protected override`/`virtual`.
- **Le reste de `CameraComponent` est inchangé** (`InitializeWithWorld`, `OnScreenResized`, `Load` : `view_distance`, `viewport`), sauf `GetBoundingBox`, qui suit maintenant la transformation de la caméra.
- **Erreur de compilation certaine hors caméra** : `GameplayExecutionPolicy` reçoit un membre `required bool RaiseGameplayNotifications` ; `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs:80-92` construit la politique champ par champ sans lui.
- **Autres changements du moteur que le jeu peut sentir sans rien demander** : `Entity.GetBoundingBox` inclut les composants dessinés enfants (ADR-0152), et ces boîtes servent au tri par frustum de `World.Draw` ; `StaticSpriteComponent` est dessiné avec sa transformation monde (ADR-0151) ; la politique d'exécution se lit par monde (ADR-0146). Le jeu n'a ni sprite, ni particule, ni composant de physique du moteur dans ses mondes (recherche dans le code et les données) ; `RacingGame.Gameplay/Components/RaceTrackPhysicsComponent.cs` dérive d'`EntityComponent`. `TrackRuntimeSceneExporter.cs:91` écrit `entity.GetBoundingBox()` dans l'export, mais `scripts/compare_race_scene_exports.py` ne compare pas les boîtes (clé : maillage, matériau, états ; appariement par matrice monde). La pose « vue d'ensemble du terrain » du genre `audit` se calcule à partir de boîtes d'entités fusionnées (`TrackMigrationCaptureValidator.cs:449-453`) : si l'ADR-0152 les change, les images de ce genre le montreront.
- **Ce que le banc n'exerce pas** : la caméra libre de débogage (F1) ; le mode jeu de l'éditeur ; `World.CreateDefaultCamera` ; la politique par monde ; les sprites de l'ADR-0151 ; les caméras 2D. Le jeu n'utilise que la première.
- **Comment le jeu pose sa caméra** : la poursuite (`ChaseCameraRigComponent.cs:242-243`) appelle `SetPositionAndTarget` puis écrit `LocalOrientation = CreateFromRotationMatrix(Matrix.CreateWorld(Zero, avant, haut lissé))` à chaque image ; la caméra libre (`DebugFreeCameraComponent.cs:120-138`) lit `Target` et `Position`, puis appelle `SetPositionAndTarget` sans toucher l'orientation (le haut reste celui que la poursuite a laissé) ; les deux validateurs de capture écrivent `LocalOrientation = Quaternion.Identity` puis `SetPositionAndTarget` ; le carrousel pose sa caméra une fois et lit `ViewMatrix * ProjectionMatrix` pour placer ses flèches ; la caméra du front-end n'est posée par aucun code, sa pose vient du fichier (position `(0, 6, -18)`, rotation identité, cible par défaut à l'origine).
- **L'outil `scripts/RgceAssetCooker` n'est pas dans `RacingGame.slnx`** (`RacingGame.slnx:1-15`) : il se construit à part, `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj` (son README, ligne 10), et son dossier `bin` porte ses propres copies du moteur et de `RacingGame.Gameplay`. Sans ce build, tout appel de l'outil décrirait l'ancien moteur.
- **Outils** : `scripts/RgceAssetCooker/WorldVerifier.cs:317-319, 333-341, 363-366, 373-397` compare type, propriétés sérialisées, `ViewDistance`, `Viewport` et transformation de chaque composant : des fichiers non recuits échouent. `RoundTripChecker.cs` n'a pas de code propre aux caméras. `scripts/check_gameplay_type_names.py` relève dynamiquement les noms du moteur.
- **Banc de parité** : `artifacts/worlds-as-assets/scripts/parity.py` exige, par genre, les mêmes PNG à l'octet dans `-a`, `-b` et la référence `artifacts/worlds-as-assets/m1/ref`, les mêmes `scene-measure-*.txt`, `exit 0`, et aucune ligne `[Warning]`/`[Error]` absente du journal de référence ; `slice_check.py <tranche>` ajoute l'export, les deux audits de voitures, `--verify`, `--check`, le catalogue et les noms de types. Dernier passage : S7, `RESULT: PASS` sur `14bc77c`.
- **Session de rendu** (`chantier/rgce-render-fidelity`, autre arbre de travail) : six commits du moteur posés sur `01cdf483f` dans sa propre branche du moteur. Elle lance aussi le jeu : verrou `D:/development/rgce-run.lock`.
- **ADR du jeu** : le dernier de cette branche est 0042 ; la branche de rendu porte déjà 0043 et 0044.
- Python : `numpy` 2.5.2 et `Pillow` 12.3.0 sont installés (visibles sans `-I`).

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | **Caméra : « Mesurer, puis décider ».** Deux étapes. A : le jeu sur le nouveau moteur garde l'ancienne formule de vue dans une classe du jeu ; les images doivent rester identiques à l'octet. B : passage à la caméra du moteur, mesure des écarts ; s'il y en a, arrêt pour la décision de l'auteur (garder B et refaire la référence, ou revenir à A). |
| D2 | **Mode AUTO** après approbation du plan : commits sur la branche de chantier seulement ; ni push, ni avance de `remaster` ou de `master` ; arrêt sur blocage, défaut du moteur, ou décision qui revient à l'auteur. |

## Points que l'approbation du plan valide

| Réf | Point |
|---|---|
| P1 | **Le `main` local du moteur est avancé** de `01cdf483f` à `164f8d680` par `git update-ref refs/heads/main 164f8d680 01cdf483f`, après `merge-base --is-ancestor`. C'est une avance rapide vers ce qui est déjà sur `origin/main` : aucun commit du moteur n'est créé, rien n'est poussé. Dans le sous-module : branche `chantier/rgce-on-origin-main-4` créée par `git branch` sur `01cdf483f`, `git symbolic-ref HEAD`, puis `git merge --ff-only refs/remotes/origin/main` pour mettre l'arbre à jour ; aucun `checkout`, `switch`, `reset`, `submodule update`. Aucune commande git dans `MGUI` ni `NvgSharp` (leurs pointeurs ne changent pas). |
| P2 | **Le pointeur et l'adaptation du jeu sont dans le même commit** (T1.2) : un commit du pointeur seul ne compilerait pas, ce qui contredirait « un commit atomique et compilable ». |
| P3 | **Classe de l'étape A** : `RacingGame.Gameplay/Components/RaceLookAtCameraComponent.cs`, `public sealed class RaceLookAtCameraComponent : PerspectiveCameraComponent`, qui reprend l'ancienne caméra : constructeur public sans paramètre ; propriété `Target` (non sérialisée, défaut `Vector3.Zero`) ; `SetPositionAndTarget` écrit `Position` et `Target` sans toucher l'orientation et sans lever d'exception ; `ComputeViewMatrix` calcule `Matrix.CreateLookAt(Position, Target, Up)` ; `IsViewOutOfDate` rend `true` (la vue est recalculée à chaque lecture, avec les valeurs du moment : c'est ce que l'ancienne obtenait, chaque poseur passant par `Target`, et une caméra immobile donnant la même matrice à chaque calcul). `Clone` est redéfini avec un retour covariant (`new RaceLookAtCameraComponent(this)`), sans quoi la copie d'un monde porterait une `PerspectiveCameraComponent` nue ; la classe porte son propre `[DisplayName("Race Look-At Camera")]`, celui de la classe mère étant hérité sinon. Le constructeur de copie copie `Target` (l'ancien écrivait dans la source, un défaut qui n'est pas reproduit ; si la parité de l'étape A échoue, ce point est le premier examiné). Elle existe pour la preuve de l'étape A ; elle disparaît à l'étape B, sauf décision contraire de l'auteur (D1). |
| P4 | **Étape B, par poseur** : mondes et carrousel, `new PerspectiveCameraComponent` + `SetPositionAndTarget` (recette de l'ADR-0149) ; poursuite, `LocalPosition` et `LocalOrientation` écrits directement, sans `SetPositionAndTarget` (l'orientation qu'elle écrit déjà est celle d'un repère « avant, haut lissé », mathématiquement la même vue que `CreateLookAt(Position, Target, haut)`) ; caméra libre, le haut de la caméra est relevé à l'activation et la pose est écrite par `LocalPosition` + `LocalOrientation = CreateFromRotationMatrix(Matrix.CreateWorld(Zero, avant, haut relevé))`, ce qui garde le comportement d'aujourd'hui (haut fixe laissé par la poursuite), et lacet et tangage initiaux viennent de `Forward` ; **écart admis par l'approbation** : si la caméra libre s'activait avant que la poursuite ait écrit une seule orientation, le haut relevé serait celui de la pose du fichier (incliné d'environ 18°) au lieu de la verticale d'aujourd'hui, ce qui donnerait du roulis après un lacet ; la poursuite écrivant l'orientation dès la première image d'une course, le cas n'est pas atteint au clavier, et c'est un outil de débogage que le banc n'exerce pas ; validateurs de capture, `SetPositionAndTarget` seul (il écrit l'orientation avec `Vector3.Up`, ce que faisait la remise à l'identité). |
| P5 | **Dossier hors dépôt** : une copie de `RacingGameCasaEngine/Content` dans `D:/development/rgce-e4/` pour les contrôles de l'éditeur, créée en T3.1 et **supprimée à la clôture** (T4.3), comme `rgce-s7` l'a été. Rien d'autre n'est créé ni supprimé hors du dépôt. |
| P6 | **Numéro d'ADR** : la décision de caméra du jeu est enregistrée en **ADR-0045**, les numéros 0043 et 0044 étant pris par la branche de rendu. Statut `Proposed`. |
| P7 | **Tests du moteur** : lancés sur `01cdf483f` avant l'avance, puis sur `164f8d680` (T1.1), à titre de contrôle de ce poste. Un échec présent dans les deux passages, avec le même nom de test, dépend du poste : il est noté et ne bloque pas. Un échec du seul second passage arrête le plan (règle R2) : il n'est ni corrigé ni contourné ici. |
| P8 | **Sorties non versionnées** sous `artifacts/engine-simple-cameras/` (scripts de lancement, mesures, rapports) et `artifacts/worlds-as-assets/{e1,e2,e3}/` (banc ; `e3` seulement si l'état A est retenu), dossiers ignorés par git. |
| P9 | **Retour en arrière** : tout le travail du jeu est sur la branche de chantier, que rien n'oblige à fusionner. Le `main` local du moteur ne revient à `01cdf483f` (`git update-ref refs/heads/main 01cdf483f 164f8d680`) que sur demande de l'auteur ; un arrêt du plan (R2, R3) le laisse à `164f8d680`, qui est ce que porte `origin/main`. |

## Règles d'exécution pour l'agent

- **Branche `chantier/rgce-engine-simple-cameras`** (jeu). Jamais de commit sur `master` ni `remaster` ; aucun commit dans le moteur (R2).
- **Une seule tâche à la fois**, icône `🚧` avant, `✅`/`🧪`/`⚠️` après, note de validation sous la tâche, **un commit par tâche** qui inclut ce fichier. Message en anglais, `type(area): summary`, par fichier et `git commit -F`, commande commençant par `cd D:/development/repo/RacingGame`.
- **Ne jamais pousser.** `git add` fichier par fichier ; jamais `.serena/`, `.codegraph/`, `log.txt`.
- **R1 — Un lancement à la fois.** Tout lancement du jeu, de l'éditeur ou des tests du moteur se fait sous le verrou `D:/development/rgce-run.lock` (création exclusive, relecture une seconde après, suppression seulement s'il porte encore notre ligne), jamais pendant un build, et après un build à jour. **Le jeu lit son contenu dans `RacingGameCasaEngine/bin/Debug/net9.0-windows/Content`, que seul `dotnet build RacingGame.slnx` met à jour** (`RacingGameCasaEngine.csproj:30-31`, copie `PreserveNewest` ; `Program.cs:38`) : tout lancement du jeu suit un build du jeu fait après le dernier changement du code **ou de `Content`** (une recuisson des mondes en est un). Le lanceur du banc refuse de démarrer si un `.world` de ce dossier diffère, à l'octet, de celui de `RacingGameCasaEngine/Content`. Le verrou d'une autre session n'est ni volé ni supprimé : on attend.
- **R2 — Défaut du moteur.** Si un comportement du moteur à `164f8d680` est en cause (échec de ses tests, exception, régression d'image que le jeu ne provoque pas), la tâche passe en ⚠️ Blocked avec le constat et la reproduction dans « Points ouverts », et le plan s'arrête. Aucun contournement dans le jeu, aucun commit dans le moteur.
- **R3 — Parité de l'étape A.** Critère : `slice_check.py e1` rend `RESULT: PASS` contre la référence de M1. Sinon : relever quelles images ou quels points diffèrent et si `-a` et `-b` sont identiques entre eux, examiner d'abord le point P3, puis ⚠️ Blocked. Une ligne `[Warning]`/`[Error]` nouvelle dans un journal est un échec de ce critère, pas un détail.
- **R4 — Mesure de l'étape B.** `-a` et `-b` doivent être identiques entre eux à l'octet (sinon ⚠️ Blocked : ce n'est plus un arrondi). L'écart à la référence de M1 est mesuré image par image (T2.2). Écart nul : l'étape B est acquise sans question. Écart non nul : le plan continue jusqu'à T3.3 sur l'état B, puis s'arrête en T4.1 pour la décision de l'auteur. Aucun seuil n'autorise l'agent à admettre un écart (D1).
- **R5 — Réglages de l'utilisateur** : `front-end-options.json` (SHA-1 `84abb0c0a6bf4d495295b61a6c7e1a43b5040301`) et `display-settings.json` (SHA-1 `18eafcb4c8df587195def3f11aead8a887090e1a`) sont vérifiés avant et après chaque série de lancements et ne sont jamais modifiés.
- **R6 — Mondes.** Le code fait foi : on ne retouche pas un `.world` à la main ; `RgceAssetCooker --worlds`, puis `python scripts/generate_rgce_content_assets.py`, puis `--worlds --verify`. L'outil lancé sans option écrit dans `Content` : il n'est jamais lancé ainsi.
- **R7 — Build de l'outil.** `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj` à 0 erreur, après `dotnet build RacingGame.slnx`, avant tout appel de `RgceAssetCooker.exe` qui suit un changement du moteur, de `RacingGame.Gameplay` ou de l'outil. Une erreur de compilation de l'outil est traitée comme celle du jeu (T1.2).
- **Ne rien inventer.** Toute API vient du code lu à `164f8d680`. Information manquante : ⚠️ Blocked, question dans « Points ouverts », arrêt.
- **Langue** : ce plan en français ; code, commentaires, commits, README, ADR en anglais. Fins de ligne préservées. Python lancé avec `-B` (et `-I` quand `numpy` n'est pas requis).
- Sous-agents : rôles nommés seulement ; relecture du plan par `plan-verifier` avant approbation, vérification de clôture par un `verifier` frais (T4.2).

## Légende des statuts

- ⏳ Todo · 🚧 In progress · 🧪 Needs testing · ✅ Done · ⚠️ Blocked

## Critères d'acceptation

| Réf | Critère | Preuve |
|---|---|---|
| E1 | Le moteur local est à `164f8d680` et ses tests n'échouent nulle part où ils passaient sur `01cdf483f` sur ce poste. | T1.1 : les deux sorties de `dotnet test` et leur comparaison. |
| E2 | Étape A : le jeu compile sur `164f8d680` et le contrôle commun de parité passe contre la référence de M1 (76 images identiques à l'octet, export, audits, outils). | T1.3 : `slice_check.py e1`, `RESULT: PASS`. |
| E3 | Étape B : le jeu n'utilise plus que la caméra du moteur ; deux rejeux identiques entre eux ; écart à la référence mesuré image par image. | T2.2 : rapport `artifacts/engine-simple-cameras/b-measure/report.md`. |
| E4 | Les contrôles du projet d'éditeur tiennent sur l'éditeur du nouveau moteur, dans l'état retenu. | T3.1 à T3.3 : 622 ouvertures, aller-retour 622 sur 622 et cinq témoins, composants proposables, échantillon piloté. |
| E5 | La décision de caméra est celle de l'auteur quand l'écart n'est pas nul, et elle est enregistrée. | T4.1 : ADR-0045, documents. |
| E6 | Vérification indépendante de l'ensemble. | T4.2 : `verifier` frais, **CONFIRMED**. |

---

## Phase 1 — Le moteur avancé, le jeu inchangé à l'image (étape A)

### ✅ T1.1 — Avancer le moteur local et le contrôler

- Objectif : sous-module et `main` local du moteur à `164f8d680` ; build et tests du moteur relevés sur ce poste.
- Fichiers : aucun fichier versionné du jeu, hors ce plan (le pointeur est indexé en T1.2).
- Étapes :
  0. Témoin du poste (P7) : sous le verrou (R1), `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj` sur `01cdf483f`, avant toute avance ; relever le nombre de tests et le nom de chaque échec dans `artifacts/engine-simple-cameras/engine-tests-old.txt`.
  1. Dans `CasaEngine` : vérifier que la branche `chantier/rgce-on-origin-main-4` n'existe pas (sinon ⚠️ Blocked), arbre propre et `merge-base --is-ancestor refs/heads/main refs/remotes/origin/main` ; `git branch chantier/rgce-on-origin-main-4 01cdf483f` ; `git symbolic-ref HEAD refs/heads/chantier/rgce-on-origin-main-4` ; `git merge --ff-only refs/remotes/origin/main` ; `git update-ref refs/heads/main 164f8d680 01cdf483f` (P1). Vérifier `git status` propre et que `MGUI` et `NvgSharp` ne sont pas signalés modifiés.
  2. `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln` : 0 erreur.
  3. Sous le verrou (R1) : `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj` (avec build). Relever le nombre de tests et le nom de chaque échec dans `artifacts/engine-simple-cameras/engine-tests-new.txt`, et les comparer à ceux de l'étape 0.
- Validation : `git -C CasaEngine rev-parse HEAD refs/heads/main refs/remotes/origin/main` rend trois fois `164f8d680…` ; build de l'éditeur à 0 erreur ; aucun échec de test qui ne soit déjà dans le témoin de l'étape 0 (sinon R2).
- Commit : `docs(ai-agent): record the engine advance to origin/main`

> Approbation de l'auteur (2026-11-22) : « Approuvé AUTO ».
>
> Validation (2026-11-22), sorties dans `artifacts/engine-simple-cameras/` :
> - **Témoin du poste** (étape 0), sur `01cdf483f`, sous le verrou : 8314 tests, 0 échec (`engine-tests-old.txt`, `engine-tests-old/old.trx`).
> - **Avance** (étape 1, P1) : branche `chantier/rgce-on-origin-main-4` créée sur `01cdf483f` (le nom était libre, l'arbre propre), `git merge --ff-only refs/remotes/origin/main`, `git update-ref refs/heads/main 164f8d680… 01cdf483f…`. `git rev-parse HEAD refs/heads/main refs/remotes/origin/main` rend trois fois `164f8d6809f6293405a81458016bbc5ae2212076` ; `git status` propre ; `MGUI` (`2f9773d05`) et `NvgSharp` (`9c0da031d`) non signalés modifiés. Aucun commit du moteur, rien de poussé.
> - **Build de l'éditeur** (étape 2) : `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln`, 0 erreur (`build/editor-new.txt`).
> - **Tests sur `164f8d680`** (étape 3), sous le verrou : 8533 tests, 0 échec (`engine-tests-new.txt`, `engine-tests-new/new.trx`). Aucun échec dans l'un ni l'autre passage : rien à comparer. **E1 est tenu.**

### ✅ T1.2 — Le jeu compile sur le nouveau moteur, avec l'ancienne formule de vue

- Objectif : le jeu, l'outil et les mondes sont adaptés au minimum pour compiler et se charger sur `164f8d680`, sans changer une image.
- Fichiers : `CasaEngine` (pointeur) ; nouveau `RacingGame.Gameplay/Components/RaceLookAtCameraComponent.cs` (P3) ; `ChaseCameraRigComponent.cs`, `DebugFreeCameraComponent.cs`, `RaceWorldTemplates.cs`, `CarSelectionCarousel.cs`, `ReferenceViewCaptureValidator.cs`, `TrackMigrationCaptureValidator.cs`, et les sources de `scripts/RgceAssetCooker` si leur build l'exige (le type `CameraLookAtComponent` devient `RaceLookAtCameraComponent`, rien d'autre ; dans `RaceWorldTemplates.cs`, `Name = nameof(RaceLookAtCameraComponent)`) ; `RacingGameCasaEngineGame.cs` (`RaiseGameplayNotifications = GameplayExecutionPolicies.Runtime.RaiseGameplayNotifications`) ; les quatre `.world` et `Content/AssetInfos.json` s'il change, régénérés (R6) ; tout autre fichier que le compilateur désigne, à condition que la correction soit un renommage ou l'ajout d'un membre devenu obligatoire, sinon ⚠️ Blocked.
- Étapes :
  1. Écrire la classe (P3), remplacer le type dans les six fichiers, ajouter le membre de la politique.
  2. `dotnet build RacingGame.slnx` : 0 erreur, puis `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj` : 0 erreur (R7). Lister dans la note toute erreur rencontrée, du jeu ou de l'outil, que le relevé n'avait pas prévue.
  3. `RgceAssetCooker --worlds`, `python scripts/generate_rgce_content_assets.py`, puis `--worlds --verify`, `--verify`, `--check` : 0 ; `generate_rgce_content_assets.py --check` : 0 ; `python scripts/check_gameplay_type_names.py` : 0 collision.
  4. `git diff --stat` des mondes : seul le nœud de caméra change (son `type`, son `name`, son `id` s'il dérive du nom, et la clé `picture_offset` si l'écrivain l'ajoute) ; toute autre différence est relevée et, si elle ne s'explique pas par l'écrivain du moteur, ⚠️ Blocked.
  5. `rg CameraLookAtComponent` hors `CasaEngine`, `docs`, `ai-agent`, `artifacts` : plus aucune occurrence dans le code ni dans `Content`.
- Validation : les commandes des étapes 2, 3 et 5 aux résultats dits, les deux builds à 0 erreur notés ; diff des mondes conforme à l'étape 4 ; les quatre mondes nomment `RaceLookAtCameraComponent`.
- Commit : `chore(casaengine): move the game to the engine's origin/main and keep its look-at view`

> Validation (2026-11-22), sorties dans `artifacts/engine-simple-cameras/build/` et `artifacts/engine-simple-cameras/t12/` :
> - **Code** : `RaceLookAtCameraComponent.cs` écrite selon P3 ; le type remplacé dans les six fichiers (4, 2, 2, 3, 3 et 7 occurrences, rien d'autre) ; `RaiseGameplayNotifications` ajouté à la politique d'exécution. **Aucune erreur de compilation que le relevé n'avait pas prévue**, ni dans le jeu ni dans l'outil : les sources de `scripts/RgceAssetCooker` ne changent pas.
> - **Builds** : `dotnet build RacingGame.slnx`, 0 erreur ; `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj`, 0 erreur (R7). Les binaires du jeu portent bien le nouveau moteur (`CasaEngine.dll` du dossier de build contient `PerspectiveCameraComponent` et plus `CameraLookAtComponent`).
> - **Mondes** (R6) : `--worlds` écrit 4 fichiers, 2 inchangés (`CarSelection.world`, `PlayerCar.entity`) ; catalogue inchangé (622 entrées) ; `--worlds --verify` : 6 fichiers, 3341 entités, 3364 composants, 0 écart ; `--verify` : 0 ; `--check` : 0 ; `generate_rgce_content_assets.py --check` : 0 ; `check_gameplay_type_names.py` : 0 collision.
> - **Diff des mondes** : dans chacun des quatre, 8 lignes ajoutées et 4 retirées, toutes dans le nœud de caméra : `id`, `name`, `type`, et la clé `picture_offset` (`x` 0, `y` 0) que l'écrivain du moteur ajoute. Rien d'autre.
> - **Restes** : `rg CameraLookAtComponent` hors `CasaEngine`, `docs`, `ai-agent`, `artifacts` : aucune occurrence.

### ✅ T1.3 — Parité de l'étape A (E2)

- Objectif : prouver que le jeu rend les mêmes images sur le nouveau moteur.
- Fichiers : ce plan ; sorties sous `artifacts/worlds-as-assets/e1/` ; lanceur `artifacts/engine-simple-cameras/parity_runs.py <tranche>` (copie de `artifacts/editor-closure/s7_parity_runs.py` dont la tranche devient un argument ; même verrou, même contrôle R5, Git Bash par son chemin complet ; il compare d'abord, par empreinte, tous les `.world` du dossier `bin/…/Content/Worlds` à ceux de `RacingGameCasaEngine/Content/Worlds` et s'arrête s'ils diffèrent, R1).
- Étapes :
  1. `dotnet build RacingGame.slnx` (0 erreur) après la recuisson de T1.2, puis `python -B artifacts/engine-simple-cameras/parity_runs.py e1` : douze rejeux (six genres, `-a` et `-b`), `smoke`, `profile`, export, deux audits de voitures.
  2. `python -B artifacts/worlds-as-assets/scripts/slice_check.py e1`, sortie gardée dans `artifacts/worlds-as-assets/e1/slice_check.txt`.
- Validation : `RESULT: PASS` (R3) ; réglages de l'utilisateur aux empreintes de R5 avant et après.
- Commit : `docs(ai-agent): record the parity of the game on the engine's origin/main`

> Validation (2026-11-22, jeu `20e1aff`, moteur `164f8d680`), sorties dans `artifacts/worlds-as-assets/e1/` et `artifacts/worlds-as-assets/e1-runs.txt` :
> - `dotnet build RacingGame.slnx` refait après la recuisson, 0 erreur ; le lanceur a constaté que les mondes du dossier de build sont ceux de `Content`, et les réglages de l'utilisateur aux empreintes de M1 avant et après (R5).
> - Les 14 lancements du banc, l'export et les deux audits de voitures sortent en `exit 0`, sans `[Warning]` ni `[Error]` au journal.
> - `slice_check.py e1` : **`RESULT: PASS`**. Point 3 : 76 PNG comparés dans les six genres, identiques à l'octet entre `-a`, `-b` et la référence de M1. Point 4 : export sans enregistrement sans pair (1437, 2033, 4058). Point 5 : les deux rapports d'audit identiques à ceux de M1 hors la ligne `Generated: `. Point 6 : `--verify`, `--check`, catalogue à 0. Point 1 : 0 collision.
> - **E2 est tenu.** Sur ce que le banc exerce, les 85 commits du moteur ne changent rien : ni l'ADR-0152 (boîtes englobantes), ni la pose « vue d'ensemble » du genre `audit`, ni la politique par monde.

---

## Phase 2 — La caméra du moteur (étape B)

### ✅ T2.1 — Passer à `PerspectiveCameraComponent`

- Objectif : le jeu n'a plus de caméra propre ; chaque poseur suit P4.
- Fichiers : suppression de `RaceLookAtCameraComponent.cs` ; les six fichiers de T1.2 ; les quatre `.world` régénérés (R6) ; commentaire de `CarSelectionCarousel.cs:285` (il nomme `Camera3dComponent`).
- Étapes :
  1. Appliquer P4 poseur par poseur. Dans la poursuite, garder `CreateCameraOrientation` telle quelle. Dans les validateurs, retirer la remise à l'identité, garder l'ordre champ de vision, plans, pose.
  2. Avant de lancer quoi que ce soit, vérifier par lecture qu'aucune pose passée à `SetPositionAndTarget` n'est verticale ni nulle : les vues de `artifacts/pbr-handover/s1c/ref/views.json` et de `artifacts/pbr-handover/s3/ref-tunnel/views.json` (ces fichiers sont en repère « racinggame-z-up » : le cosinus avec la verticale se calcule sur les valeurs converties comme le validateur les convertit avant de poser la caméra, conversion relue dans son code, ou sur l'axe Z des valeurs brutes si elle se réduit à un changement d'axes ; sortie dans la note), les quatre poses de `TrackMigrationCaptureValidator.cs`, la pose du carrousel, celle des mondes. Une pose verticale : ⚠️ Blocked.
  3. Les deux builds (R7), recuisson et contrôles de T1.2 (étapes 2 et 3). Diff des mondes : le nœud de caméra change de `type`, de `name`, d'`id` (il dérive du nom) et de rotation (la pose écrite par `SetPositionAndTarget`) ; rien d'autre.
  4. `rg "RaceLookAtCameraComponent|CameraLookAtComponent|\.Target\b"` dans les six fichiers : aucune occurrence qui vise une caméra.
- Validation : les deux builds à 0 erreur ; `--worlds --verify`, `--verify`, `--check`, catalogue, noms de types à 0 ; étapes 2 et 4 conformes.
- Commit : `refactor(racing-casa): pose the cameras of the game through the engine's perspective camera`

> Validation (2026-11-22), sorties dans `artifacts/engine-simple-cameras/build/` et `artifacts/engine-simple-cameras/t21/` :
> - **Code, selon P4** : `RaceLookAtCameraComponent.cs` supprimée. Poursuite : `LocalPosition` puis `LocalOrientation = CreateCameraOrientation(…)`, celle-ci inchangée. Caméra libre : le haut (`camera.Up`) et la direction (`camera.Forward`) sont relevés à l'activation, la pose est écrite par `LocalPosition` et `LocalOrientation = CreateFromRotationMatrix(Matrix.CreateWorld(Zero, avant, haut relevé))` ; la constante `TargetDistance`, devenue inutile, est retirée. Validateurs : la remise à l'identité est retirée, l'ordre champ de vision, plans, pose est gardé. Mondes et carrousel : `new PerspectiveCameraComponent` et `SetPositionAndTarget`. Le commentaire du carrousel ne nomme plus `Camera3dComponent`.
> - **Poses verticales ou nulles** (étape 2), par lecture et calcul : les 15 vues de `s1c/ref/views.json` ont au plus un cosinus de 0,716 avec la verticale (`sector-50`), les 3 vues du tunnel 0,375 ; le calcul est fait sur l'axe Z des valeurs brutes, la conversion du validateur étant un changement d'axes suivi d'une translation et de la matrice du générateur de route, que les journaux du banc donnent à l'identité pour les trois circuits (`identity True`). Mondes : direction `(0, -6, 18)`, cosinus 0,316. Carrousel : `(0, -3,75, 10,45)`, cosinus 0,338. Les quatre poses de `TrackMigrationCaptureValidator.cs` sont obliques par construction (hauteur de 10 à 12 pour un recul de 18 à 20 ; vue d'ensemble décalée de `0,6 d` en X et en Z). Aucune pose verticale ni nulle.
> - **Builds** : `dotnet build RacingGame.slnx`, 0 erreur ; `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj`, 0 erreur (R7).
> - **Mondes** (R6) : `--worlds` écrit 4 fichiers, 2 inchangés ; catalogue inchangé (622) ; `--worlds --verify`, `--verify`, `--check`, catalogue, noms de types : 0.
> - **Diff des mondes** : dans chacun des quatre, 6 lignes changées, toutes dans le nœud de caméra : `id`, `name`, `type`, et trois composantes de la rotation (`y` 0,9870874, `z` 0,16018224, `w` 0 : la pose écrite par `SetPositionAndTarget`). Rien d'autre.
> - **Restes** : plus aucune occurrence de `RaceLookAtCameraComponent` ni de `CameraLookAtComponent` hors `CasaEngine`, `docs`, `ai-agent`, `artifacts` ; les deux `.Target` restants de `ReferenceViewCaptureValidator.cs` (472, 548) sont le champ d'une vue de référence, pas d'une caméra.

### ✅ T2.2 — Mesure de l'étape B (E3)

- Objectif : dire exactement ce que la caméra du moteur change à l'image.
- Fichiers : ce plan ; sorties sous `artifacts/worlds-as-assets/e2/` et `artifacts/engine-simple-cameras/b-measure/` ; script de mesure `artifacts/engine-simple-cameras/measure_images.py` (Pillow et numpy).
- Étapes :
  1. `dotnet build RacingGame.slnx` (0 erreur) après la recuisson de T2.1 (R1), puis `parity_runs.py e2`, puis `slice_check.py e2` (sortie gardée). Les points autres que les images (export, audits, outils, journaux, `exit 0`, identité de `-a` et `-b`) doivent tenir comme en T1.3 ; sinon R3 s'applique à ces points.
  2. Si `RESULT: PASS` : écart nul, noter et passer à T3.1.
  3. Sinon, `measure_images.py` compare, pour chaque PNG des six genres, `e2/ref/<genre>-a` à `m1/ref/<genre>-a` (les noms des genres `ui` et `audit` portent un horodatage : il est retiré pour apparier, comme le fait `parity.py:43`) : identique ou non à l'octet ; nombre et part de pixels différents ; plus grand écart par canal ; écart absolu moyen ; boîte englobante des pixels différents ; et une planche référence, candidat, différence amplifiée pour chaque image différente. Il vérifie d'abord sur lui-même deux témoins : une image comparée à elle-même rend zéro, une image dont un pixel est modifié rend un pixel. Rapport : `b-measure/report.md` (une ligne par image, totaux par genre).
  4. Lire les planches des images les plus différentes de chaque genre et décrire ce qu'on y voit (contours seulement, ou objets déplacés, manquants).
- Validation : `-a` et `-b` identiques à l'octet (R4) ; rapport présent, 76 lignes ; témoins du script justes.
- Commit : `docs(ai-agent): record the image measure of the engine's perspective camera`

> Validation (2026-11-22, jeu `8d31748`, moteur `164f8d680`), sorties dans `artifacts/worlds-as-assets/e2/`, `artifacts/worlds-as-assets/e2-runs.txt` et `artifacts/engine-simple-cameras/b-measure/` :
> - `dotnet build RacingGame.slnx` refait après la recuisson, 0 erreur ; mondes du dossier de build identiques à ceux de `Content` ; réglages de l'utilisateur aux empreintes de M1 avant et après.
> - **Tout ce qui n'est pas image tient comme à l'étape A** : les 14 lancements, l'export et les deux audits en `exit 0`, aucun `[Warning]` ni `[Error]` ; export sans enregistrement sans pair (1437, 2033, 4058) ; audits de voitures identiques à M1 ; `--verify`, `--check`, catalogue, noms de types à 0.
> - **Les deux rejeux sont identiques entre eux à l'octet** (`parity.py` de `e2/ref` contre lui-même : 76 PNG, PASS) : l'écart est déterministe (R4).
> - **Écart à la référence de M1** (`slice_check.py e2` : 1 échec, le point 3) : **70 images sur 76 diffèrent** ; les 6 identiques à l'octet sont des écrans sans scène 3D (`ui-help`, `ui-highscores`, `ui-main-menu`, `ui-options`, `ui-splash`, `ui-track-selection`).
> - **Mesure** (`measure_images.py`, témoins justes, `report.md`, 76 lignes, 70 planches) :
>
>   | Genre | Images | Identiques | Pixels différents (part) | Plus grand écart d'un canal |
>   |---|---|---|---|---|
>   | `views-linear` | 15 | 0 | 628 919 (4,5 %) | 112 |
>   | `views-legacy` | 15 | 0 | 496 457 (3,6 %) | 201 |
>   | `tunnel-linear` | 3 | 0 | 168 391 (6,1 %) | 95 |
>   | `tunnel-legacy` | 3 | 0 | 126 163 (4,6 %) | 109 |
>   | `ui` | 10 | 6 | 154 377 (0,7 %) | 111 |
>   | `audit` | 30 | 0 | 4 814 484 (7,7 %) | 120 |
>
>   L'essentiel des pixels différents l'est d'**un seul niveau sur 255** (par exemple `views-linear/Advanced-sector-20` : 37 973 pixels différents, dont 2 553 de plus d'un niveau, 254 de plus de 8, 9 de plus de 32). L'écart absolu moyen par canal va de 0,002 à 0,38 niveau selon l'image. Les deux images les plus touchées sont `audit-Expert-helper-laterns` (40,8 % des pixels, dont 13 % de plus d'un niveau) et `audit-Beginner-helper-palms` (34,8 %) ; `ui-car-selection` ne change que de 54 pixels.
> - **Ce qu'on voit** (étape 4, planches et agrandissements `crop-*.png`, cartes `map-*.png`) : rien à l'œil sur les images côte à côte. La différence amplifiée seize fois est un grain réparti sur toute la surface texturée (pavés du sol, route), plus dense là où la texture est contrastée, avec quelques pixels forts sur les contours et les bords d'ombre. **Aucun objet déplacé, ajouté ou manquant, aucune ombre déplacée.**
> - **Ordre de grandeur du déplacement** : un décalage global ajusté image par image (`fit_shift.py`, `shift-fit.txt`) vaut 0,0005 pixel en médiane et 0,028 pixel au plus ; l'écart quadratique moyen de luminance vaut 0,27 niveau en médiane et 1,23 au plus. Estimation faite hors du jeu, en simple précision, des deux formules de vue pour les 18 vues de référence (script ponctuel, non gardé) : les deux matrices diffèrent d'au plus 9e-5 sur un élément (la translation, les positions de caméra allant jusqu'à 1010 unités), soit 0,001 pixel en médiane et 0,006 au plus pour un point entre 5 et 100 unités devant la caméra. C'est l'ordre de grandeur d'un arrondi de simple précision, pas celui d'une pose différente.
> - **Attribution** : entre l'étape A (`20e1aff`, identique à M1 à l'octet) et l'étape B (`8d31748`), seul le calcul de la vue change ; l'écart mesuré lui est donc dû en entier.
> - **E3 est tenu.** L'écart n'étant pas nul, R4 s'applique : la suite (T3.1 à T3.3) se fait sur l'état B, puis T4.1 s'arrête pour la décision de l'auteur.

---

## Phase 3 — Le projet d'éditeur sur le nouveau moteur (E4)

Sur l'état B (R4). Si l'auteur choisit ensuite de revenir à l'état A (T4.1), les tâches T3.1 à T3.3 sont rejouées sur cet état avant la clôture ; le type de caméra attendu dans leurs validations est alors celui de l'état retenu (`RaceLookAtCameraComponent`, affiché « Race Look-At Camera »).

### 🚧 T3.1 — Ouverture des 622 assets et aller-retour

- Objectif : rejouer les deux contrôles versionnés de S7 sur l'éditeur de `164f8d680`.
- Fichiers : ce plan ; aucun fichier versionné d'outil (une adaptation de `RoundTripChecker.cs` exigée par le compilateur est faite en T1.2) ; sorties sous `artifacts/engine-simple-cameras/{open,roundtrip}/`.
- Étapes :
  1. `dotnet build RacingGame.slnx`, `dotnet build scripts/RgceAssetCooker/RgceAssetCooker.csproj` (R7) et `dotnet build CasaEngine/CasaEngine.Editor.MonoGame.sln` à jour, 0 erreur ; copier `RacingGameCasaEngine/Content` dans `D:/development/rgce-e4/content` (P5).
  2. `python -I -B scripts/check_editor_opens_assets.py --editor <…>/CasaEngine.Editor.exe --project D:/development/rgce-e4/content/RacingGameCasaEngine.json --out artifacts/engine-simple-cameras/open --timeout 120` (le script tient le verrou par lancement ; environ 80 minutes, en arrière-plan).
  3. `RgceAssetCooker.exe --roundtrip --content D:/development/rgce-e4/content --out …/roundtrip` puis `--roundtrip-controls`.
  4. Lire les avertissements distincts de `summary.json` et les comparer à ceux de S7 (`artifacts/editor-closure/a2/summary.json`) : tout avertissement nouveau est cité dans la note ; s'il nomme un défaut du projet, il est corrigé dans le jeu si la correction est du même ordre que celle de S7 (une donnée du projet à mettre à jour), sinon ⚠️ Blocked ; s'il vient du moteur, R2.
- Validation : 622 sur 622 ouvertures ; aller-retour 622 sur 622 (372 éditables sans perte, 250 en-têtes lisibles) ; cinq témoins détectés.
- Commit : `docs(ai-agent): record the editor checks on the engine's origin/main`

### ⏳ T3.2 — Composants proposables et monde de course

- Objectif : rejouer le contrôle A1 de S7.
- Fichiers : ce plan ; sorties sous `artifacts/engine-simple-cameras/a1/` ; `artifacts/editor-closure/a1_run.py` réutilisé, copié si un chemin doit changer.
- Étapes : éditeur lancé avec `--remote` sur la copie, sous le verrou ; `component.types` ; ouverture de `Worlds/Race/TrackBeginner.world` ; lecture de tout le journal, page par page, par niveau.
- Validation : les 16 composants du jeu attendus en S7 sont proposés (l'état B n'en ajoute ni n'en retire) ; le monde s'ouvre ; 0 entrée de niveau erreur ; l'entité `BootstrapCamera` porte une caméra résolue, que `entity.get` rend sous son nom d'affichage (« Perspective Camera » dans l'état B) et non comme composant non résolu.
- Commit : `docs(ai-agent): record the offered components on the engine's origin/main`

### ⏳ T3.3 — Échantillon piloté

- Objectif : rejouer l'échantillon piloté de S7 (modifier, enregistrer, rétablir par l'éditeur piloté) sur sept assets, le monde étant cette fois **`Worlds/FrontEnd.world`**, qui porte une caméra (celui de S7, `Worlds/CarSelection.world`, n'en a pas) : c'est la seule preuve qu'un nœud de `PerspectiveCameraComponent` passe par l'enregistrement de l'éditeur.
- Fichiers : ce plan ; `artifacts/engine-simple-cameras/driven/driven_run.py`, **copie** de `artifacts/editor-closure/driven/driven_run.py` où `COPY = 'D:/development/rgce-e4/content'`, `OUT = REPO + '/artifacts/engine-simple-cameras/driven'`, `WORLD = 'Worlds/FrontEnd.world'`, et où l'étape du monde ne cherche plus une propriété mais prend celle nommée ci-dessous ; l'original et ses sorties de S7 ne sont pas touchés.
- Étapes : même déroulé qu'en T7.4 du plan du projet d'éditeur, sur la copie de contenu. Les six autres assets sont ceux de S7 (un matériau, un son, un écran, la voiture, une texture, un modèle). Pour le monde, `Worlds/FrontEnd.world` n'a que deux entités et aucune propriété booléenne ou entière hors caméra (`artifacts/editor-closure/verif/c4/c4.json`) : la recherche du script de S7 (`first_property`, `different`) y échouerait. La copie modifie donc explicitement ce que le vérificateur de S7 a modifié sur ce même monde : entité `PlayerStart`, composant affiché « Player Start », propriété énumérée `PlayerIndex`, de `One` à `Two`, puis de `Two` à `One`, par `property.set`, avec `project.save` après chaque changement. Si l'entité, le composant ou la propriété manque dans la réponse d'`entity.get`, ou si `property.set` est refusé : ⚠️ Blocked.
- Validation : pour les six assets de S7, mêmes écarts admis qu'en S7 et pas d'autre (les huit valeurs de `BodyVisual` de la voiture, fins de ligne d'un `.sound`). Pour `Worlds/FrontEnd.world` : après le premier enregistrement, le fichier porte `PlayerIndex` à `Two` ; après le rétablissement et le second enregistrement, il ne diffère de celui du dépôt que par `view_distance` et `viewport` du nœud de caméra (ce que l'ancien éditeur écrivait déjà, sortie de S7 `artifacts/editor-closure/verif/c4/FrontEnd.after-restore.world`), le nœud gardant `"type": "PerspectiveCameraComponent"` et sa pose ; toute autre différence est citée et jugée : écart de l'écrivain du moteur documenté par ses ADR-0147 à 0150, ou ⚠️ Blocked. Aucun fichier de `artifacts/editor-closure/driven/` n'est modifié.
- Commit : `docs(ai-agent): record the driven editor sample on the engine's origin/main`

---

## Phase 4 — Décision, documents, clôture

### ⏳ T4.1 — Décision de l'auteur et documents (E5)

- Objectif : arrêter l'état final et l'écrire.
- Fichiers : `docs/decisions/0045-…md` et `docs/decisions/README.md` (skill `adr`, P6) ; `RacingGameCasaEngine/Assets/README.md` (phrases qui nomment la caméra ou ses clés, relevé du 2026-11-22 : lignes 111, 138, 165) ; `scripts/RgceAssetCooker/README.md` (lignes 67-68, 86-92) ; `ai-agent/README.md`, `ai-agent/HANDOVER.md` (section 16) ; ce plan.
- Étapes :
  1. Écart nul en T2.2 : l'état B est retenu sans question. Écart non nul : ⚠️ Blocked, question à l'auteur avec le rapport et les planches : garder B et faire des images de `e2` la référence, ou revenir à A.
  2. Selon la réponse : B retenu, la référence du banc devient `artifacts/worlds-as-assets/e2/ref` (écrit dans le HANDOVER et dans l'ADR ; `m1/ref` reste en place) ; A retenu, `git revert --no-commit` du commit de T2.1 puis ce fichier de plan rétabli à son état courant avant de committer (le commit de T2.1 le porte aussi : seuls le code et le contenu sont défaits), les deux builds (R7), recuisson et contrôles de T1.2, de nouveau `dotnet build RacingGame.slnx` après la recuisson (R1), parité `e3` identique à M1, T3.1 à T3.3 rejouées ; l'ensemble de composants proposables attendu est alors redérivé comme en S7 (sources du jeu et règle du moteur), `RaceLookAtCameraComponent` comprise si cette règle la retient.
  3. ADR-0045 (`Proposed`) : contexte (suppression de la caméra par le moteur, ADR-0149), décision, mesures, conséquences. Mettre à jour les documents ; ne réécrire aucun ADR existant (l'ADR-0042 ne nomme pas la classe).
- Validation : `rg "CameraLookAt|Camera3dComponent"` dans `RacingGameCasaEngine/Assets/README.md`, `scripts/RgceAssetCooker/README.md` et le code : aucune phrase fausse ; liens des documents valides.
- Commits : si A est retenu, d'abord `refactor(racing-casa): return to the look-at camera of the game` ; puis `docs(adr): record the camera of the game on the engine's simple cameras` ; `docs(racing-casa): describe the game on the engine's origin/main`

### ⏳ T4.2 — Vérification indépendante (E6)

- Objectif : un `verifier` frais confirme ou réfute E1 à E5 sur l'état final.
- Étapes : lui donner les affirmations exactes, le diff `d71dc65..HEAD`, les chemins des sorties ; il rejoue ce qu'il choisit sous le verrou (au moins : les builds du jeu et de l'outil, `--worlds --verify`, un genre du banc, un échantillon d'ouvertures dans l'éditeur, le recalcul des empreintes des images) et pose ses propres témoins.
- Validation : **CONFIRMED**. Constat P0/P1 : arrêt. P2 introduit : corrigé dans le périmètre puis une revérification. P3/P4 : notés.
- Commit : `docs(ai-agent): record the closing verification of the engine integration`

### ⏳ T4.3 — Clôture

- Objectif : état final écrit, dossier hors dépôt supprimé.
- Étapes : regarder puis supprimer `D:/development/rgce-e4/` (P5) ; vérifier qu'aucun verrou ni processus ne reste ; mettre à jour `ai-agent/README.md` et `ai-agent/HANDOVER.md` ; rapport de fin de tâche. Prévenir la session de rendu que le moteur local et le jeu de cette branche sont sur `164f8d680` : un message d'information envoyé à sa session par l'outil de messages entre sessions de l'application, sans rien lui demander ; si l'outil n'est pas disponible, l'information est donnée à l'auteur dans le rapport de fin.
- Validation : `git status` propre hors `.serena/`, `log.txt` ; aucune tâche en 🚧.
- Commit : `docs(ai-agent): close the engine integration plan`

---

## Relectures du plan

- 2026-11-22, première relecture (`plan-verifier`) : **REVISE**, un blocage (l'outil `RgceAssetCooker` n'est construit par aucune étape : R7 et les builds ajoutés) et huit remarques, toutes intégrées.
- 2026-11-22, seconde relecture (relecteur neuf) : **REVISE**, deux blocages, tous deux **corrigés** : T3.3 nommait « un monde de course » alors que le script de S7 pilote un monde sans caméra (le monde est maintenant `Worlds/FrontEnd.world`, le script est copié) ; un rejeu pouvait partir avec des mondes périmés dans le dossier de build (R1 complétée, builds ajoutés après chaque recuisson, contrôle par empreinte dans le lanceur). Six remarques intégrées.
- 2026-11-22, relecture de clôture (relecteur neuf) : **REVISE**, un blocage, **corrigé** : sur `Worlds/FrontEnd.world`, le script de S7 n'aurait trouvé aucune propriété à modifier (T3.3 nomme maintenant `PlayerStart` / `PlayerIndex`, ce que le vérificateur de S7 avait modifié). Le relecteur confirme le reste : builds de l'outil avant chaque appel, build du jeu après chaque recuisson, écarts admis de T3.3, aucune étape qui pousse, committe dans le moteur ou avance `remaster`. Trois remarques intégrées. **Cette dernière correction n'a pas été relue par un relecteur** : après deux REVISE puis celui de la relecture de clôture, la règle interdit de relancer une relecture d'office ; la suite revient à l'auteur (approuver tel quel, ou demander une relecture de plus).

## Hors périmètre

- Pousser quoi que ce soit, avancer `remaster` ou `master` : à la demande de l'auteur, après la clôture.
- Rapprocher la branche de rendu (`chantier/rgce-render-fidelity` du jeu, ses six commits du moteur) du nouveau moteur : elle touche aussi `RaceWorldTemplates.cs` ; c'est un travail à part, après sa clôture.
- Utiliser les nouveautés du moteur dans le jeu (`OrbitCameraComponent`, décalage d'image, politique par monde, sprites orientés).
- Corriger le moteur (R2) ; corriger la phrase de l'ADR-0149 du moteur qui dit qu'aucune donnée ne nomme `CameraLookAtComponent`.
- Le document `docs/casaengine-entity-update-strategy-agent-plan.md:464`, qui nomme l'ancienne caméra : document d'analyse daté, laissé tel quel.
- Les points ouverts des autres plans (O3 à O5 du projet d'éditeur, ADR-0041 et 0042 `Proposed`).

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Écart d'image de l'étape B : à mesurer en T2.2, à trancher par l'auteur s'il n'est pas nul (D1). | T4.1 |
