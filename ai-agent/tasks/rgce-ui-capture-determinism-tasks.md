# Plan agent IA — Captures UI déterministes (O4)

Plan court issu de l'enquête sur le point ouvert O4 de [rgce-race-finished-capture-tasks.md](rgce-race-finished-capture-tasks.md) (2026-10-15).
Les décisions D1 → D4 ont été arbitrées par l'auteur le 2026-10-15 (« fais le, options recommandées ») : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

Les 10 états de `--capture-ui-screens` donnent la même image d'un lancement à l'autre, quelle que soit la cadence d'affichage de la machine. Une nouvelle référence des 10 états est prise, et O4 est clos avec sa cause.

Ce plan ne livre pas : le traitement des entrées réelles reçues pendant une automation (O3 du plan `race-finished`), ni la capture en fin de `Draw` dans les autres validateurs.

## Cause établie (enquête du 2026-10-15)

Sonde : copie de `RacingGameCasaEngine` bâtie dans un dossier temporaire sur le bin du dépôt (recette de `o2-race-finished/`), étendue.
Elle journalise chaque update (rectangle du logo, souris, voiture, caméra, chrono), marque chaque draw de son numéro et de celui de sa dernière update, et lit l'image en fin de `Draw` pour les 8 dernières updates avant chaque capture.
Sonde, scripts et tableaux : dossier de notes `o4-pacing-variants/`, à côté de `o2-race-finished/`.

Notations : `c` est l'update dans laquelle le validateur capture un état. Elle vaut 76, 166, 256, 346, 436, 526, 616, 871, 961 et 1066 pour les 10 états, dans tous les lancements.

- **La capture montre l'update `c − 2`, ou une plus ancienne quand la cadence baisse.**
  - À cadence normale, l'image lue est celle de `c − 2` : c'est le draw qui précède le dernier, comme établi pour O2.
  - Quand les draws ralentissent, MonoGame enchaîne des updates de rattrapage sans draw (pas fixe à 60 Hz, `MaxElapsedTime` par défaut de 500 ms). L'image lue recule alors à `c − 3` ou `c − 4` (16 et 20 ms de pause par draw).
  - L'historique de `Screenshots/` (lancements du 2026-10-08 au 2026-10-09) se range de même : chaque capture est l'image d'une update de `c − 2` à `c − 7`.
- **L'état du jeu à une update donnée est identique d'un lancement à l'autre**, quelle que soit la cadence : la voiture, la caméra et le chrono sont égaux au bit près aux updates 869 et 959 dans 8 lancements de 0 à 20 ms, et le rectangle du logo à l'update 166 dans les 5 de ces lancements où il n'est pas figé. Seul le numéro de l'update lue varie.
- **Chaque variante d'O4 est l'image d'une update voisine.** La référence `artifacts/element-naming/verify/ui` est exactement (0,00) l'image de `c − 2` pour les 9 états. Les valeurs d'O4 sont exactement celles des updates voisines :

  | État | `c − 3` | `c − 4` | `c − 5` | `c − 6` |
  |---|---|---|---|---|
  | splash | 0,62 | 0,64 | | |
  | main-menu | 0,67 | 0,75 | 0,82 | |
  | car-selection | 0,72 | 0,83 | | |
  | track-selection | 0,23 | 0,23 | | |
  | race-hud | 0,65 | 1,20 | 1,67 | 2,05 |
  | options | 0,34 | 0,39 | | |
  | help | 0,18 | 0,57 | | |

  Écart moyen absolu RGB contre `verify/ui`, coin du marqueur exclu. Marqueur compris, race-hud à `c − 6` vaut 2,06 (2,09 sur le canal R) : c'est la valeur d'O4, celle du lancement de sonde du 2026-10-09 à 14:32, dont le marqueur montre l'update 865.

- **Ce qui change encore pendant le délai de stabilisation.** Les images de fin de `Draw` de deux updates consécutives ont été comparées pour chaque état.
  1. **Le logo « Racing Game » rebondit sur les 7 écrans du front-end** (splash, main-menu, highscores, options, help, car-selection, track-selection).
     - `RaceMenuDecorationViewModel.Update` (`RacingGame.Gameplay/ViewModels/RaceMenuDecorationViewModel.cs:63-71`) calcule sa taille avec `Math.Sin(totalSeconds / 0.46f) * 0.045f * Math.Cos(totalSeconds / 0.285f)`.
     - `RaceXamlScreenBase.UpdateMenuDecoration` (`RacingGameCasaEngine/Screens/RaceXamlScreenBase.cs:44-47`) l'appelle avec le temps total du jeu.
     - Rien ne le fige sous capture : ADR-0009 (`:40`) fige le clignotement, la rotation, le tour du carrousel et le balancement des flèches, pas le logo.
     - Tous les pixels qui changent entre deux updates sont dans le rectangle du logo (x 662 → 1806, y 55 → 433 à 1920×1080). Le reste de ces écrans est figé.
     - highscores n'est stable que par chance : à sa capture, le logo est près d'un extrême de son rebond, et son rectangle arrondi ne bouge pas de `c − 5` à `c − 2`.
  2. **race-hud : la voiture, la caméra et le chrono bougent encore.** Toute l'image change d'une update à l'autre (0,6 à 0,9).
     - Le décompte fini, le solveur de la voiture reprend la main (`RaceFlowCoordinatorComponent.cs:54-58`, `VehicleDynamicsComponent.cs:100-104`). Sans aucune entrée, la voiture se déplace encore : x passe de −72,08 à −73,79 en 60 updates, d'un pas qui décroît de 40 % toutes les 100 ms.
     - La caméra de poursuite la suit avec lissage (`ChaseCameraRigComponent.cs:117-121`).
     - Le chrono « Current » avance de 16,7 ms par update (`RaceHudScreen.cs:70`).
     - C'est le comportement du jeu : le figer changerait ce que la capture montre.
  3. **pause : rien ne change au moment de la capture.** La voiture est arrêtée. La caméra converge encore pendant la pause, mais seulement jusqu'à l'update 942 environ, bien avant la capture (961). Les images de `c − 8` à `c` sont identiques.
  4. **race-finished : figé** depuis `c887e95`.
- **La variante pause 0,33 d'O4 ne vient pas de la cadence.** Dans le lancement U12 du 2026-10-15, le bouton « Back to main menu » est en survol : le vrai curseur était sur la fenêtre du jeu. C'est le mécanisme d'O3 (entrées réelles pendant une automation). Pendant cette enquête, 4 lancements sur 14 ont reçu de vraies entrées (voir O2) ; leurs états d'avant la perturbation restent exploitables.
- **Résidu sans rapport avec la cadence :** à la même update, car-selection diffère parfois de quelques pixels isolés d'un lancement à l'autre (1 à 7 pendant l'enquête, jusqu'à 9 lors de la vérification ; écart maximal 26, dans le carrousel 3D). Moyenne 0,00, loin du seuil.

## Preuves des correctifs (sonde, même bin)

Chaque expérience est un interrupteur de la sonde ; le reste du code est celui du dépôt. Détail : `o4-pacing-variants/results/experiments.txt`.

| Expérience | Pause par draw | Résultat |
|---|---|---|
| Témoin, aucun changement | 0, 12, 16, 20 ms | Captures à `c − 2` à 0 et 12 ms ; de `c − 2` à `c − 4` à 16 et 20 ms. Les variantes d'O4 reviennent (splash 0,62, main-menu 0,67, options 0,34, car-selection 0,72, race-hud 0,65). |
| Logo figé au temps 0 | 0, 16, 20 ms | Les 7 écrans du front-end identiques (0,00) entre les 3 lancements, même quand la capture recule à `c − 3`. race-hud varie encore (0,65 à `c − 3`). |
| Une update par draw (`MaxElapsedTime = TargetElapsedTime`) | 0, 16, 20 ms | 1 079 draws pour 1 080 updates : aucune update de rattrapage. Toutes les captures à `c − 2` ; 9 états à 0,00 contre `verify/ui`, race-finished à 0,00 contre `ref-a` (absent à 0 ms : le lancement a reçu de vraies entrées, voir O2). |
| Option C (lecture en fin du `Draw` qui suit l'update `c`) et une update par draw | 0, 16, 20 ms | Chaque capture est l'image de l'update `c` (journal : « requested in update 871, read in draw 870 after update 871 »). Les 10 états identiques (0,00) entre 0 et 16 ms, et les 8 premiers à 20 ms (la suite de ce lancement a reçu de vraies entrées, voir O2). |
| Option C seule | journaux des 7 lancements sans « une update par draw » | Le draw qui suit l'update `c` montre `c + 1` pour 1 à 4 états dans 5 lancements sur 7, dont race-hud à 20 ms (deux updates consécutives de race-hud diffèrent de 0,6 à 0,9). Même à cadence normale, 16 draws sur 827 suivent au moins deux updates. |

## État vérifié du dépôt (2026-10-15)

- Branche `remaster`, HEAD `200a66d`. Arbre de travail principal : sous-module `CasaEngine` modifié (pointeur à `87fc5f01b`, autre session), `.serena/` et `log.txt` non suivis. **Ne pas les indexer.**
- Ce plan est commité sur la branche `claude/interesting-chatterjee-f24e78` : `remaster` en avance rapide, plus ce plan.
- Le worktree de cette session n'avait pas le sous-module `CasaEngine` : il y est cloné depuis le dépôt local, au commit `87fc5f01b` de l'arbre principal (MGUI `4e94d7e8`, NvgSharp `9c0da031`), sans changer le pointeur enregistré ni la configuration du dépôt.
- `PinScreenAnimations` est lu par les écrans Splash, Options, CarSelection et TrackSelection, et par la caméra d'arrivée. HelpScreen et HighscoresScreen ne reçoivent pas l'état du front-end (`RaceFrontEndFlow.cs:63-64`).
- `RaceTitleViewModel.Update(…, bool pinBlink)` (`RacingGame.Gameplay/ViewModels/RaceTitleViewModel.cs:113`) est le modèle d'un paramètre de figeage.
- `MaxElapsedTime` n'est réglé nulle part dans RacingGameCasaEngine ni CasaEngine : la sonde lit 500 ms, pour un pas fixe de 16,6667 ms (`RacingGameCasaEngineGame.cs:195`).
- La capture lit le back buffer pendant un `Update` (`RacingGameCasaEngineGame.cs:480-490`). Le commentaire de `DrawCount` (`:402`) dit « the last presented frame » ; c'est en fait l'avant-dernière.
- Une autre session travaille sur O3 (worktree `jolly-clarke`) et lance aussi le jeu : les lancements doivent rester un à la fois.

## Décisions verrouillées

| Réf | Décision | Alternatives écartées |
|---|---|---|
| D1 | **Figer le rebond du logo sous capture** : quand `PinScreenAnimations` est levé, le logo garde la taille du temps 0 (facteur 1,005, celle de la première image du menu). Application de la règle d'ADR-0009. Suffit pour les 7 écrans du front-end, quelle que soit l'image lue. | Ne pas figer : les 7 écrans ne sont déterministes qu'avec D2, et sur une autre machine (autre tampon d'échange) ils redeviendraient variables. |
| D2 | **Capturer l'image de l'update de capture** : lire le back buffer en fin du `Draw` qui suit l'update de capture (option C de D1 du plan `race-finished`). Sous `--capture-ui-screens` seulement, une update par draw (`MaxElapsedTime = TargetElapsedTime`) : le jeu ralentit au lieu de rattraper. Seule voie qui rende race-hud déterministe sans changer ce qu'il montre. | B : une update par draw seulement, lecture inchangée. Une ligne, et déterministe sur cette machine (expérience ci-dessus), mais l'image reste celle de `c − 2`, un décalage qui dépend du tampon d'échange du pilote (avant-dernier draw avec un double tampon). C : option C seule : insuffisante, une update de rattrapage entre l'update `c` et son draw donne `c + 1`. D : figer la course sous capture (voiture tenue, chrono arrêté) : change ce que race-hud montre. |
| D3 | **Une nouvelle référence des 10 états** (`artifacts/ui-capture-determinism/ref-a/ui`) remplace `artifacts/element-naming/verify/ui` et `artifacts/race-finished-capture/ref-a/ui`. Les 7 écrans du front-end changent une fois (logo du temps 0), race-hud aussi (update `c` au lieu de `c − 2`). | Garder deux dossiers de référence. |
| D4 | **Pas de nouvelle ADR.** D1 applique la règle d'ADR-0009 (doc XML de `PinScreenAnimations`). D2 est un détail de l'outil de capture, documenté dans la doc XML du validateur et le README de `UiCaptureCompare`. | ADR-0021 courte pour D2. |

## Règles d'exécution pour l'agent

- **Branche `claude/interesting-chatterjee-f24e78`** (worktree de cette session : `remaster` + ce plan). L'auteur avance `remaster` dessus en fin de plan. Ne jamais committer sur `main` ni pousser.
  - Écart au plan proposé, qui prévoyait `remaster` directement : l'arbre de travail principal reste intact (modifications de l'auteur et d'autres sessions).
- **Une seule tâche à la fois**, icône `🚧` puis `✅`, `🧪` ou `⚠️`, note de validation sous la tâche, **un commit par tâche** incluant ce fichier.
- **Un seul processus de jeu ou d'éditeur à la fois**, lancé par un helper qui sauvegarde et restaure `display-settings.json` et `front-end-options.json`, et qui attend qu'aucun autre processus du jeu ou de l'éditeur ne tourne. Aucune entrée clavier ou souris simulée.
- Build : `dotnet build RacingGame.slnx`. `git add` fichier par fichier.
- Langue : ce plan en français ; code et commits en anglais.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur.
- Sonde reconstruite sur le code corrigé (recette de `o4-pacing-variants/`), pause en fin de `Draw` de 0, 12, 16 et 20 ms : les 10 états identiques (0,00) entre les 4 lancements. Avant correctif, ces lancements donnent les variantes du tableau de la section Cause.
- `--capture-ui-screens` du build du dépôt, deux lancements : 10 états à 0,00 entre eux.
- `--smoke-frontend` : code 0, aucun avertissement ni erreur.

---

## Phase 1 — Captures déterministes

### ✅ T1.1 — Figer le rebond du logo sous capture

- Objectif : sous `PinScreenAnimations`, le logo des 7 écrans du front-end garde la taille du temps 0 (D1).
- Fichiers :
  - `RacingGame.Gameplay/ViewModels/RaceMenuDecorationViewModel.cs` : paramètre `pinBounce` d'`Update`, sur le modèle de `pinBlink`.
  - `RacingGameCasaEngine/Screens/RaceXamlScreenBase.cs` : `UpdateMenuDecoration` le transmet.
  - Les 7 écrans qui l'appellent : `SplashScreen.cs:73`, `MainMenuScreen.cs:63` et `:81`, `HighscoresScreen.cs:206`, `OptionsScreen.cs:352`, `HelpScreen.cs:130`, `CarSelectionScreen.cs:232`, `TrackSelectionScreen.cs:176`.
  - `RacingGameCasaEngine/Bootstrap/RaceFrontEndFlow.cs:63-64` : HelpScreen et HighscoresScreen reçoivent l'état du front-end.
  - `RacingGame.Gameplay/Race/RaceFrontEndState.cs` : doc XML de `PinScreenAnimations`, qui cite le rebond du logo.
- Étapes :
  1. `RaceMenuDecorationViewModel.Update(int, int, double, bool pinBounce)` : si `pinBounce`, la taille est celle du temps 0.
  2. `UpdateMenuDecoration` et les 7 écrans passent `_state.PinScreenAnimations`. Help et Highscores reçoivent `RaceFrontEndState` dans leur constructeur.
  3. Doc XML de `PinScreenAnimations`.
- Validation : build ; sonde à 0, 16 et 20 ms : les 7 écrans identiques (0,00) entre les lancements ; `--smoke-frontend` code 0.
- Commit : `fix(racing-casa): pin the menu logo bounce under the UI capture`

> Validation (2026-10-15) :
> - `dotnet build RacingGame.slnx` : 0 erreur. Aucun avertissement dans `RacingGameCasaEngine` ni `RacingGame.Gameplay` ; ceux du build viennent du sous-module, de `RacingGame.PipelineExtension`, `RacingGame` et `RacingGame.Shared`, déjà présents.
> - Sonde reconstruite sur ce build, pause en fin de `Draw` de 0, 16 et 20 ms : les 7 écrans du front-end identiques (0,00) entre les 3 lancements, alors que la capture lit `c − 2`, `c − 3` ou `c − 4` selon l'état et la cadence. Ils sont aussi identiques à l'expérience « logo figé » de l'enquête. race-hud varie encore (1,67 à `c − 5`) : c'est T1.2.
> - `--smoke-frontend` : code 0, « Smoke validation completed successfully », aucun avertissement ni erreur.
> - Relecture adverse en lecture seule (3 angles : exhaustivité, conventions, régressions ; 3 contradicteurs par constat) : aucun défaut fonctionnel.
>   - Corrigés (P4) : `pinBounce` documenté par une balise `<param>` dans `UpdateMenuDecoration`, comme les autres paramètres de figeage ; ligne de doc de `PinScreenAnimations` remise à la largeur du bloc ; fins de ligne CRLF rétablies dans la copie de travail de 2 fichiers (le contenu commité n'en dépend pas).
>   - Reporté à T1.2, qui touche ce fichier : le README de `UiCaptureCompare` cite encore le rebond du logo parmi les zones qui diffèrent d'un lancement à l'autre.
>   - Rejeté : modifier ADR-0009 (décision D4, comme pour l'orbite de `c887e95`).
> - Le lancement à 20 ms a reçu une vraie entrée après l'arrivée (retour au menu, voir O2) : ses 9 premiers états restent valides.
> - Pas de test unitaire : aucun projet de test ne couvre `RacingGame.Gameplay` ni `RacingGameCasaEngine`.

### ✅ T1.2 — Capturer l'image de l'update de capture

- Objectif : chaque capture est l'image de l'update où le validateur la demande, quelle que soit la cadence (D2).
- Fichiers :
  - `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` : demande de capture lue en fin de `Draw`, avant `Present`. L'écriture du PNG est partagée avec `CaptureScreenshotWithStem`, qui reste utilisée par F2 et les autres validateurs. Commentaire de `DrawCount` corrigé.
  - `RacingGameCasaEngine/Bootstrap/UiScreenCaptureValidator.cs` : la capture est demandée, puis le validateur attend le fichier avant d'avancer. `MaxElapsedTime = TargetElapsedTime` au démarrage. Doc XML de la classe.
  - `scripts/UiCaptureCompare/README.md` : sections Capture et Metric (le rebond du logo, la rotation de l'aperçu 3D et le chrono n'y diffèrent plus d'un lancement à l'autre).
- Étapes :
  1. Jeu : une demande de capture en attente, servie à la fin du prochain `Draw` ; le chemin du fichier est rendu au validateur.
  2. Validateur : `CaptureAndAdvance` demande la capture ; l'update suivante déplace le fichier et avance l'étape. Le délai global existant couvre une capture jamais servie.
  3. Démarrage du validateur : `MaxElapsedTime = TargetElapsedTime`, avec un commentaire (une update par draw, pour que le draw qui suit l'update de capture la montre).
  4. Doc XML et README.
- Validation : build ; sonde reconstruite à 0, 12, 16 et 20 ms : les 10 états identiques (0,00) entre les lancements, autant de draws que d'updates dans le journal ; `--smoke-frontend` code 0.
- Commit : `fix(racing-casa): capture each UI state at the end of the draw of its update`

> Validation (2026-10-15) :
> - `dotnet build RacingGame.slnx` : 0 erreur ; aucun avertissement dans `RacingGameCasaEngine` ni `RacingGame.Gameplay`.
> - Sonde reconstruite sur ce build, pause en fin de `Draw` de 0, 12, 16 et 20 ms. Elle ne garde que le marqueur et le journal, posés après la capture.
>   - 1 079 draws pour 1 080 updates dans chaque lancement : aucune update de rattrapage. Chaque capture est l'image de l'update `c`.
>   - Les 10 états sont identiques au pixel entre les 4 lancements, sauf 1 à 3 pixels isolés de car-selection dans 2 lancements (le résidu du carrousel 3D, O3).
>   - Ils sont aussi identiques au pixel aux prototypes de l'enquête : race-hud, pause et race-finished à l'expérience « option C et une update par draw », les 7 écrans du front-end à l'expérience « logo figé ». race-finished vaut 0,00 contre `ref-a`.
> - `--smoke-frontend` : code 0, « Smoke validation completed successfully », aucun avertissement ni erreur.
> - Relecture adverse en lecture seule (3 angles : déroulé de la capture, régressions, documentation ; 3 contradicteurs par constat) : rien sur le chemin normal.
>   - Corrigé (P3, régression de la détection d'échec) : un PNG dont l'écriture échoue une fois le fichier créé aurait été compté comme une capture réussie, car le validateur testait l'existence du fichier. Le jeu expose maintenant le résultat de la sauvegarde (`IsEndOfDrawScreenshotSaved`), que le validateur lit. Ce chemin d'échec n'a pas été provoqué ; le chemin de réussite a servi dans chaque lancement (10 captures comptées).
>   - Corrigés (P4) : `cref` vers `PinScreenAnimations` qualifiée ; doc de `DrawCount` ramenée à la machine mesurée (le décalage dépend du pilote) ; doc de la classe du validateur nuancée par les résidus du README.
>   - Rejetés (0 contradicteur sur 3) : 5 remarques de style ou de placement.
> - Vraies entrées (O2) : à 0 et 12 ms, les premiers lancements ont quitté `race-finished` juste après l'arrivée, et l'un s'est fermé. Leurs 9 premiers états sont identiques aux autres ; relancés, ils sont propres.
> - `CaptureScreenshotWithStem` garde son comportement pour F2 et `TrackMigrationCaptureValidator`. `MaxElapsedTime` n'est changé que par le validateur de capture UI.
> - Écart aux étapes 1 et 2 (relevé par la critique de complétude de T1.3) : le validateur n'attend pas le fichier pour avancer. Il avance l'étape dès la demande, ce qui garde la chronologie d'avant et celle des prototypes, et compte la capture à l'update suivante. Une capture jamais servie fait échouer la demande suivante (« still pending ») ou le compte final de 10, en plus du délai global. Le message « Captured <état> » est écrit à la demande.

### ✅ T1.3 — Nouvelle référence et clôture d'O4

- Objectif : une référence des 10 états reproductible, et O4 clos avec sa cause (D3).
- Fichiers : ce plan, `ai-agent/tasks/rgce-race-finished-capture-tasks.md` (O4 : cause et lien vers ce plan), `ai-agent/README.md`.
- Étapes :
  1. Deux lancements de `--capture-ui-screens` du build du dépôt copiés dans `artifacts/ui-capture-determinism/{ref-a,ref-b}/ui` (ignoré par git) : 10 états à 0,00 entre eux.
  2. `UiCaptureCompare` de l'ancienne référence contre ref-a : pause et race-finished à 0,00 contre leurs références actuelles ; les 7 écrans du front-end et race-hud changent une fois.
  3. O4 du plan `race-finished` : cause, correctifs, référence à utiliser désormais.
  4. Passe `verifier` fraîche sur T1.1 → T1.3.
- Validation : comparaisons ci-dessus ; verdict CONFIRMED.
- Commit : `docs(racing-casa): record the deterministic UI captures`

> Validation (2026-10-15) :
> - Références : deux lancements de `--capture-ui-screens` du build de cette branche (`ui-run-20261015-103915` et `-104320`), copiés dans `artifacts/ui-capture-determinism/{ref-a,ref-b}/ui`. Rapports : `artifacts/ui-capture-determinism/compare-*`.
>   - `UiCaptureCompare` ref-a contre ref-b : 10 états à 0,00 sur les 3 canaux. Au pixel, identiques, sauf 6 pixels isolés du carrousel de car-selection (O3).
>   - `verify/ui` contre ref-a : pause à 0,00. Changent une fois, comme prévu (D3) : les 7 écrans du front-end (logo du temps 0 : splash 2,04 et main-menu 3,05 sur le canal R, au-dessus du seuil ; les autres de 0,45 à 1,21), race-hud 1,17 (update `c` au lieu de `c − 2`) et race-finished 33,14 (l'ancienne pose d'orbite de `verify/ui`).
>   - `race-finished-capture/ref-a` contre ref-a : race-finished et pause à 0,00.
>   - ref-a est identique au pixel aux lancements de la sonde de T1.2 à 0 et 16 ms.
> - Vraies entrées (O2) : 2 lancements sur 4 ont quitté `race-finished` juste après l'arrivée ; leurs 9 premiers états sont identiques à ref-a (au carrousel près). J'ai relancé jusqu'à obtenir deux lancements propres.
> - O4 du plan `race-finished` clos : cause, correctifs, référence à utiliser désormais. Lignes de `ai-agent/README.md` mises à jour.
> - Passe `verifier` fraîche : **CONFIRMED** sur 6 affirmations (déterminisme, références, jeu normal et autres appelants inchangés, gestion des échecs, smoke et build, documentation).
>   - Reproduit indépendamment : un lancement de sonde à 33 ms par draw, une cadence jamais utilisée. 1 079 draws pour 1 080 updates, chaque capture à l'update `c`, 9 états identiques au pixel à ref-a, car-selection à 9 pixels isolés près. Aussi `--smoke-frontend` (code 0) et le build (0 erreur).
>   - Remarques P4 : le résidu du carrousel atteint 9 pixels (texte corrigé : « quelques pixels isolés ») ; les chemins d'échec de la sauvegarde ne sont vérifiés que par lecture du code (reporté : pas d'injection de faute).
>   - Critique de complétude en parallèle, 3 contradicteurs par constat : 1 constat retenu (P4 : l'écart entre les étapes de T1.2 et le code, noté sous T1.2) ; 7 réfutés, dont la section « Reference captures » du README de `UiCaptureCompare` et les références du plan de l'éditeur.
> - Docs seulement dans cette tâche : pas de build.

---

## Intégration à `remaster` (2026-10-15)

- À la demande de l'auteur (« fais l'avance rapide de remaster »). `remaster` avait reçu entre-temps les 8 commits du plan de l'entrée en automation (`642cbc9` à `8f9a39d`). Les 4 commits de ce plan ont été rebasés dessus, conflits de documentation seulement (`ai-agent/README.md`, O3 et O4 du plan `race-finished`), puis `remaster` a été avancé en avance rapide.
- Revalidation sur la branche rebasée :
  - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans `RacingGameCasaEngine` ni `RacingGame.Gameplay`.
  - Sonde à 0 et 20 ms par draw, et un lancement du build : 3 lancements propres, identiques au pixel à `artifacts/ui-capture-determinism/ref-a/ui`, sauf 1 à 3 pixels isolés du carrousel (O3). Chaque capture de la sonde est l'update `c`, 1 079 draws pour 1 080 updates.
  - `--smoke-frontend` : code 0, « Automation mode: the player input is ignored. », aucun avertissement ni erreur.
  - La référence `ref-a` reste valable.

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | ~~Arbitrage de D1 → D4 par l'auteur.~~ Réglé le 2026-10-15 : options recommandées. | T1.1 → T1.3 |
| O2 | Entrées réelles pendant la capture (O3 du plan `race-finished`). Un curseur sur la fenêtre change le survol (variante pause 0,33). Le 2026-10-15 entre 09:04 et 09:19, 4 lancements sur 14 de cette enquête ont reçu de vraies entrées, aucune venant de la sonde :<br>- S20 et M0 ont quitté `race-finished` environ 1 s après l'arrivée ;<br>- C20 a repris la course après la pause (choc contre une barrière), est revenu aux menus et a changé 4 fois la couleur de la voiture ;<br>- X20 a quitté l'écran titre 1 s après le démarrage, puis la fenêtre s'est fermée (code 0).<br>Hors périmètre ; une autre session y travaille. Tant que ce n'est pas réglé, ne pas toucher la souris ni le clavier pendant les lancements de référence.<br>**Réglé** par le plan de l'entrée en automation ([rgce-automation-input-tasks.md](rgce-automation-input-tasks.md), `d89ddf4` et `5f78ac2`, ADR-0021 et ADR-0022), intégré à `remaster` avant ce plan : sous les modes automation, aucune entrée réelle n'atteint le jeu. | T1.3 |
| O3 | car-selection : quelques pixels isolés (1 à 9 observés) varient d'un lancement à l'autre à la même update, dans le carrousel 3D. Sans rapport avec la cadence, sous le seuil. À reprendre seulement si une égalité exacte est exigée. | T1.3 |

## Hors périmètre

- Les entrées réelles pendant une automation (O3 du plan `race-finished`).
- La capture en fin de `Draw` pour F2 et pour les autres validateurs (`TrackMigrationCaptureValidator`, audits).
- Figer la course sous capture.
