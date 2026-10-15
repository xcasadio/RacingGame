# Plan agent IA — Captures UI déterministes (O4)

Plan court issu de l'enquête sur le point ouvert O4 de [rgce-race-finished-capture-tasks.md](rgce-race-finished-capture-tasks.md) (2026-10-15).
Les décisions D1 → D4 sont **proposées** ci-dessous : elles attendent l'arbitrage de l'auteur. Aucun code n'est modifié avant son approbation.

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
- **Résidu sans rapport avec la cadence :** à la même update, car-selection diffère parfois de 1 à 7 pixels isolés d'un lancement à l'autre (écart maximal 26, dans le carrousel 3D). Moyenne 0,00, loin du seuil.

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
- Ce plan est commité sur la branche `claude/interesting-chatterjee-f24e78` : `remaster` en avance rapide, plus ce plan. L'auteur l'intègre à `remaster` par avance rapide, ou indique une autre branche.
- `PinScreenAnimations` est lu par les écrans Splash, Options, CarSelection et TrackSelection, et par la caméra d'arrivée. HelpScreen et HighscoresScreen ne reçoivent pas l'état du front-end (`RaceFrontEndFlow.cs:63-64`).
- `RaceTitleViewModel.Update(…, bool pinBlink)` (`RacingGame.Gameplay/ViewModels/RaceTitleViewModel.cs:113`) est le modèle d'un paramètre de figeage.
- `MaxElapsedTime` n'est réglé nulle part dans RacingGameCasaEngine ni CasaEngine : la sonde lit 500 ms, pour un pas fixe de 16,6667 ms (`RacingGameCasaEngineGame.cs:195`).
- La capture lit le back buffer pendant un `Update` (`RacingGameCasaEngineGame.cs:480-490`). Le commentaire de `DrawCount` (`:402`) dit « the last presented frame » ; c'est en fait l'avant-dernière.
- Une autre session travaille sur O3 (worktree `jolly-clarke`) et lance aussi le jeu : les lancements doivent rester un à la fois.

## Décisions proposées (à arbitrer par l'auteur)

| Réf | Décision | Alternatives écartées |
|---|---|---|
| D1 | **Figer le rebond du logo sous capture** (recommandé) : quand `PinScreenAnimations` est levé, le logo garde la taille du temps 0 (facteur 1,005, celle de la première image du menu). Application de la règle d'ADR-0009. Suffit pour les 7 écrans du front-end, quelle que soit l'image lue. | Ne pas figer : les 7 écrans ne sont déterministes qu'avec D2, et sur une autre machine (autre tampon d'échange) ils redeviendraient variables. |
| D2 | **Capturer l'image de l'update de capture** (recommandé) : lire le back buffer en fin du `Draw` qui suit l'update de capture (option C de D1 du plan `race-finished`). Sous `--capture-ui-screens` seulement, une update par draw (`MaxElapsedTime = TargetElapsedTime`) : le jeu ralentit au lieu de rattraper. Seule voie qui rende race-hud déterministe sans changer ce qu'il montre. | B : une update par draw seulement, lecture inchangée. Une ligne, et déterministe sur cette machine (expérience ci-dessus), mais l'image reste celle de `c − 2`, un décalage qui dépend du tampon d'échange du pilote (avant-dernier draw avec un double tampon). C : option C seule : insuffisante, une update de rattrapage entre l'update `c` et son draw donne `c + 1`. D : figer la course sous capture (voiture tenue, chrono arrêté) : change ce que race-hud montre. |
| D3 | **Une nouvelle référence des 10 états** (`artifacts/ui-capture-determinism/ref-a/ui`) remplace `artifacts/element-naming/verify/ui` et `artifacts/race-finished-capture/ref-a/ui`. Les 7 écrans du front-end changent une fois (logo du temps 0), race-hud aussi (update `c` au lieu de `c − 2`). | Garder deux dossiers de référence. |
| D4 | **Pas de nouvelle ADR.** D1 applique la règle d'ADR-0009 (doc XML de `PinScreenAnimations`). D2 est un détail de l'outil de capture, documenté dans la doc XML du validateur et le README de `UiCaptureCompare`. | ADR-0021 courte pour D2. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (celle des plans RGCE en cours), après intégration de ce plan. Ne jamais committer sur `main` ni pousser.
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

### ⏳ T1.1 — Figer le rebond du logo sous capture

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

### ⏳ T1.2 — Capturer l'image de l'update de capture

- Objectif : chaque capture est l'image de l'update où le validateur la demande, quelle que soit la cadence (D2).
- Fichiers :
  - `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` : demande de capture lue en fin de `Draw`, avant `Present`. L'écriture du PNG est partagée avec `CaptureScreenshotWithStem`, qui reste utilisée par F2 et les autres validateurs. Commentaire de `DrawCount` corrigé.
  - `RacingGameCasaEngine/Bootstrap/UiScreenCaptureValidator.cs` : la capture est demandée, puis le validateur attend le fichier avant d'avancer. `MaxElapsedTime = TargetElapsedTime` au démarrage. Doc XML de la classe.
  - `scripts/UiCaptureCompare/README.md` : section Capture.
- Étapes :
  1. Jeu : une demande de capture en attente, servie à la fin du prochain `Draw` ; le chemin du fichier est rendu au validateur.
  2. Validateur : `CaptureAndAdvance` demande la capture ; l'update suivante déplace le fichier et avance l'étape. Le délai global existant couvre une capture jamais servie.
  3. Démarrage du validateur : `MaxElapsedTime = TargetElapsedTime`, avec un commentaire (une update par draw, pour que le draw qui suit l'update de capture la montre).
  4. Doc XML et README.
- Validation : build ; sonde reconstruite à 0, 12, 16 et 20 ms : les 10 états identiques (0,00) entre les lancements, autant de draws que d'updates dans le journal ; `--smoke-frontend` code 0.
- Commit : `fix(racing-casa): capture each UI state at the end of the draw of its update`

### ⏳ T1.3 — Nouvelle référence et clôture d'O4

- Objectif : une référence des 10 états reproductible, et O4 clos avec sa cause (D3).
- Fichiers : ce plan, `ai-agent/tasks/rgce-race-finished-capture-tasks.md` (O4 : cause et lien vers ce plan), `ai-agent/README.md`.
- Étapes :
  1. Deux lancements de `--capture-ui-screens` du build du dépôt copiés dans `artifacts/ui-capture-determinism/{ref-a,ref-b}/ui` (ignoré par git) : 10 états à 0,00 entre eux.
  2. `UiCaptureCompare` de l'ancienne référence contre ref-a : pause et race-finished à 0,00 contre leurs références actuelles ; les 7 écrans du front-end et race-hud changent une fois.
  3. O4 du plan `race-finished` : cause, correctifs, référence à utiliser désormais.
  4. Passe `verifier` fraîche sur T1.1 → T1.3.
- Validation : comparaisons ci-dessus ; verdict CONFIRMED.
- Commit : `docs(racing-casa): record the deterministic UI captures`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Arbitrage de D1 → D4 par l'auteur. | T1.1 → T1.3 |
| O2 | Entrées réelles pendant la capture (O3 du plan `race-finished`). Un curseur sur la fenêtre change le survol (variante pause 0,33). Le 2026-10-15 entre 09:04 et 09:19, 4 lancements sur 14 de cette enquête ont reçu de vraies entrées, aucune venant de la sonde :<br>- S20 et M0 ont quitté `race-finished` environ 1 s après l'arrivée ;<br>- C20 a repris la course après la pause (choc contre une barrière), est revenu aux menus et a changé 4 fois la couleur de la voiture ;<br>- X20 a quitté l'écran titre 1 s après le démarrage, puis la fenêtre s'est fermée (code 0).<br>Hors périmètre ; une autre session y travaille. Tant que ce n'est pas réglé, ne pas toucher la souris ni le clavier pendant les lancements de référence. | T1.3 |
| O3 | car-selection : 1 à 7 pixels isolés varient d'un lancement à l'autre à la même update, dans le carrousel 3D. Sans rapport avec la cadence, sous le seuil. À reprendre seulement si une égalité exacte est exigée. | T1.3 |

## Hors périmètre

- Les entrées réelles pendant une automation (O3 du plan `race-finished`).
- La capture en fin de `Draw` pour F2 et pour les autres validateurs (`TrackMigrationCaptureValidator`, audits).
- Figer la course sous capture.
