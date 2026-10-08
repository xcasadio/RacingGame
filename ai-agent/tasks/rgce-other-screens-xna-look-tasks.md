# Plan agent IA — Highscores, Options et Help au look du jeu XNA d'origine (RacingGameCasaEngine)

Plan d'exécution de la demande de l'auteur du 2026-10-06 : « il faut harmoniser les autres écrans highscore, options et help ». Il fait suite aux chantiers du menu principal, de l'écran titre, de la sélection de voiture et de la sélection de piste (ADR-0007, ADR-0009, ADR-0011).

Les décisions D1 → D6 ci-dessous ont été arbitrées avec l'auteur le 2026-10-06 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

Highscores, Options et Help deviennent les écrans de l'original. Comme les quatre écrans déjà faits, ils gardent le fond, le logo qui rebondit, l'en-tête en image, le bouton BACK de l'original et les sons de menu, sans focus MGUI.
- **Highscores** : bande sombre, onglets Beginner / Advanced / Expert en GameFont, lignes de séparation, rang, nom et temps en colonnes. Les données sont celles d'aujourd'hui.
- **Options** : le panneau d'image de l'original, avec les lignes propres à RGCE ajoutées dans son style. Toute sortie applique et enregistre, et le volume change en direct.
- **Help** : l'image d'aide de l'original.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-06)

Découverte en lecture seule : workflow de 4 agents, chacun contre-vérifié par un agent adverse. Les faits porteurs ont été relus directement. Le code d'origine se lit par `git show 4f840a3^:<chemin>` ; abréviations : `UI` = `RacingGame.Shared/Graphics/UIRenderer.cs`, `BG` = `Graphics/BaseGame.cs`, `TF` = `Graphics/TextureFont.cs`.

- **Dépôts** :
  - RacingGame : branche `remaster`, HEAD `944cdb8`, égale à `master`. `.serena/` et `log.txt`, non suivis, sont à l'auteur.
  - CasaEngine : extrait à `19aa21f` (`main`, au niveau d'`origin/main`), qui contient le correctif des ombres `7a44aba` plus 158 commits, dont un slider MGUI. RacingGame enregistre encore `7a44aba` (`git ls-tree HEAD CasaEngine`).
- **Écrans RGCE actuels** (`RacingGameCasaEngine/Screens/{Highscores,Options,Help}Screen.cs`, `Content/UI/Screens/<Nom>/<Nom>.{xaml,uiscreen,design.json}`, view models `RaceHighscoresViewModel`, `RaceOptionsViewModel`, `RaceHelpViewModel`) :
  - fond et logo déjà harmonisés (`RaceMenuDecorationViewModel`) ;
  - le reste est une mise en page MGUI fixe dans une fenêtre 1280×720 étirée :
    - bande `rgba(0,0,0,132)` ;
    - titre en `TextBlock` orange ;
    - police MGUI ;
    - bouton « Back » MGUI restylé par `LegacyMenuUiTheme` ;
  - focus MGUI, ni Échap ni B ni Back, aucun son ;
  - les fabriques passent `(AssetContentManager, back)` à Highscores et Help (`Bootstrap/RaceFrontEndFlow.cs:62-63`).
- **Écrans déjà faits** (modèle à suivre : `TrackSelectionScreen.cs`, `RaceTrackSelectionViewModel.cs`, `TrackSelection.xaml`) :
  - un view model place tout en pixels d'écran à chaque image, avec les formules de l'original, arrondies au pair ;
  - en-tête en image par `RenderOnScreenRelative1600(10, 18)` ;
  - bouton BACK : `Ui.Button.Back` et `Ui.Button.Highlight`, grossi au survol ;
  - textes en GameFont (`FontFamily="GameFont"`, échelle `vpW/1400 × vpH/1050`) ;
  - entrées lues par l'écran, garde de première image, sons `MenuSounds`.
- **Highscores d'origine** (`GameScreens/Highscores.cs`) :
  - **Image** :
    - fond et logo ;
    - `RenderBlackBar(160, 338)`, soit `CalcRectangle(0,160,1024,338)` à 0,683 ;
    - en-tête `headers.png` (0,412,512,100) (`UI:30`) ;
    - bouton BACK seul.
  - **Textes GameFont** :
    - onglets à `y = YToRes(182)`, x à partir de `XToRes(512−160·3/2+25)`, puis `+XToRes(168)`, puis `+XToRes(182)` ; jaune (255,255,0) si sélectionné, blanc au survol, sinon (211,211,211) ;
    - deux lignes (192,192,192,128) à `YToRes(208)` et +1, de `XToRes(300)` à `XToRes(640)` + largeur de « 5:67:89 » ;
    - 10 lignes à partir de `YToRes(220)`, pas `YToRes(27)` ; rang à `XToRes(300)`, nom à `XToRes(350)`, temps jaune à `XToRes(640)` au format `m:ss.cc` ; ligne survolée en blanc, sinon (200,200,200).
  - **Entrées** :
    - gauche/droite (clavier, croix, stick au-delà de 0,75) font défiler les onglets avec ButtonClick ;
    - un clic sur un onglet le sélectionne (ButtonClick) ;
    - Échap, B, Back, un clic sous la dernière ligne (`y > YToRes(490)`) ou sur BACK font sortir ;
    - Highlight à l'entrée de la souris dans un onglet, une ligne ou BACK ; ScreenClick à l'ouverture, ScreenBack à la sortie.
  - Ouverture sur Advanced (`selectedLevel = 1`).
- **Options d'origine** (`GameScreens/Options.cs`, version modernisée de `4f840a3^`) :
  - **Image** : fond et logo, sans bande, puis l'en-tête `headers.png` (512,212,512,100) (`UI:27`). Viennent ensuite :
    - le panneau `OptionsScreenWindows.png` (1024×512, fond noir à alpha 204, libellés dessinés dans l'image) à `CalcRectangleKeep4To3(0,125,1024,512)`, base 1024×768 ;
    - le bouton BACK.
  - **Nom** : texte blanc à `(XToRes(352), YToRes768(170))`, avec un « | » qui clignote toutes les 0,35 s ; saisi au clavier sans focus ; 32 caractères au plus.
  - **Résolutions** : 5 cases (339|454|575|704|838, 112, …) ; chaque libellé dessiné est recouvert par la même zone teintée (15,15,15,230), puis réécrit centré, ambre (255,156,0) si sélectionné, sinon blanc.
  - **Cases Fullscreen, Post Screen Effects, Shadows, High Detail** : zone redessinée teintée (255,156,0,160) quand elle est active.
  - **Show FPS et Vibration** : pastille `buttons.png` (935,427,39,39), teintée si active, sinon (180,180,180,120), suivie d'un texte blanc.
  - **Curseurs Sound, Music, Sensitivity** : poignée = la même pastille, à `rect.X + (int)(rect.W·v) − XToRes(39)/2`.
  - **Flèche** de la ligne active : `buttons.png` (874,426,53,39), qui oscille de 0 à 16 unités ; elle ne va que sur les 3 curseurs.
  - **Entrées** :
    - haut/bas changent de curseur et gauche/droite le règlent de ±0,1, avec Highlight ;
    - un clic sur une case ou une résolution la change avec ButtonClick, un clic sur un curseur le règle avec Highlight ;
    - Échap, B, Back et BACK appliquent et enregistrent tout ;
    - les volumes s'appliquent à chaque image (`Sound.SetVolumes`).
- **Help d'origine** (`GameScreens/Help.cs`) :
  - fond et logo, sans bande ;
  - en-tête `headers.png` (512,312,512,100) (`UI:29`) ;
  - `HelpScreenWindows.png` (1024×512, alpha ≥ 204, pictogrammes ACCELERATE / BRAKE / NAVIGATE pour clavier et souris, manette, volant) à `CalcRectangleKeep4To3(0,125,1024,512)` ;
  - bouton BACK ;
  - aucun texte ;
  - Échap, B, Back ou un clic n'importe où ferment ; ScreenClick à l'ouverture, Highlight au survol de BACK, ScreenBack à la fermeture.
- **Commun aux trois** : bouton BACK `buttons.png` (212,872,212,92) à `CalcRectangleCenteredWithGivenHeight(0,587,48,…)`, `X = W − w − XToRes(50)`. Au survol, il grossit de `XToRes(16)` × `YToRes(9)` et reçoit le contour (424,240,212,92). Le jeu de base est déjà dans RGCE (`Ui.Button.Back`, `Ui.Button.Highlight`).
- **Assets** :
  - `headers.png`, `buttons.png` et `OptionsScreenWindows.png` ont leur copie prémultipliée dans `Content/UI/Textures/` ;
  - sprites existants : `Ui.Button.Back`, `Ui.Button.Highlight`, `Ui.CarSelection.Arrow` (874,426,53,39), et la texture `Ui.CarSelection.OptionsWindowsTexture` ;
  - manquent les en-têtes des trois écrans, le panneau d'Options et ses zones, la pastille (935,427,39,39), et `HelpScreenWindows.png`, ni copié ni catalogué (`scripts/generate_rgce_ui_assets.py`, `scripts/UiTexturePremultiplier/Program.cs:18-25`).
- **MGUI et moteur** :
  - `MGImage.TextureColor` multiplie les couleurs de l'image, aussi en XAML (`CasaEngine/MGUI/MGUI.Core/UI/XAML/Controls.cs:1274-1294`) ;
  - le jeu tient GameFont (`RacingGameCasaEngineGame.cs:195`), et `BitmapFont.Font` est un `SpriteFontBase` de FontStashSharp (`CasaEngine/CasaEngine/Framework/Assets/Fonts/BitmapFont.cs:31`) ;
  - le moteur reçoit la saisie de texte de la fenêtre (`TextInputEventArgs`, `Framework/Input/MonoGameWindowInputSource.cs:22`).
- **Comportements RGCE à garder** :
  - l'état est écrit à chaque changement, et `ApplyAndClose` applique puis enregistre (`OptionsScreen.cs:99-104`, ADR-0004) ;
  - 4 résolutions et Auto (`RacingGameCasaEngineGame.cs:27-33`, `:493-518`) ; Driving Mode et Vertical Sync existent dans RGCE, pas dans l'original ; High Detail a été retiré (D12 de `rgce-xaml-screens-tasks.md`) ;
  - les scores viennent de `RaceFrontEndCatalog.Highscores` : 5 entrées par piste, temps « mm:ss.cc », lus aussi par le HUD (`RuntimeRaceSession.cs:199-216`, `RaceHudScreen.cs:171-189`) ;
  - les validateurs pilotent ces écrans par nom d'état et par `ApplyOptionsAndReturnToMainMenuForAutomation`, sans nom de contrôle (`FrontEndNavigationSmokeValidator.cs`, `UiScreenCaptureValidator.cs:128-166`).
- **Code mort après le chantier** : `LegacyMenuUiTheme` et `RaceMenuTextButtonViewModel` ne servent qu'à ces trois écrans ; `RaceFrontEndCatalog.HelpSections` ne sert qu'à Help.
- **Décisions en vigueur** :
  - ADR-0001 : écrans en assets liés à des view models ; « Visual states… keep their exact current look » ; CasaEngine et MGUI non modifiés ;
  - ADR-0004 : enregistrement au seul bouton Back d'Options ;
  - ADR-0009 : la capture de l'auteur fait foi ; sans post-effet, sans scène 3D derrière les menus, sans curseur ni pastille web ;
  - ADR-0012 : images d'interface prémultipliées.
- Aucune capture de l'original pour ces trois écrans : le code fait foi.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | **Help = image d'origine** `HelpScreenWindows.png` sous l'en-tête HELP. Les sections de texte RGCE disparaissent (auteur, 2026-10-06). |
| D2 | **Options = panneau d'origine et lignes RGCE** (auteur, 2026-10-06) :<br>- libellés de résolution redessinés (1280x720 à 3840x2160, Auto) ;<br>- la case High Detail devient Vertical Sync ;<br>- Show FPS, Vibration et Driving Mode en pastilles avec un texte ;<br>- la flèche du clavier et de la manette parcourt toutes les lignes ;<br>- comme l'original, toute sortie applique et enregistre, et le volume change en direct. |
| D3 | **Highscores = données actuelles** (5 temps par piste, partagés avec le HUD), affichées au look d'origine, ouverture sur Advanced. Pas de nouvelle fonctionnalité (auteur, 2026-10-06). |
| D4 | **Pointeur CasaEngine** : le chantier enregistre `19aa21f` en premier commit, après build, smoke et captures (auteur, 2026-10-06). |
| D5 | Mêmes règles que les chantiers précédents : le code d'origine fait foi faute de capture ; post-effet, scène 3D derrière les menus, curseur et pastille web hors périmètre (ADR-0009). |
| D6 | CasaEngine et MGUI ne sont pas modifiés ; un manque est documenté (`docs/mgui-gaps-from-rgce-xaml-screens.md`). |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **Branche** `remaster`, sans push. Le pointeur `CasaEngine` n'est indexé qu'en T0.2 (D4). |
| P2 | **Mise en page** : un petit helper partagé `LegacyScreenLayout` dans `RacingGameCasaEngine.UI`, utilisé par les trois nouveaux view models :<br>- `XToRes`, `YToRes`, `YToRes768` ;<br>- `CalcRectangle`, `CalcRectangleKeep4To3`, `CalcRectangle1600`, `CalcRectangleCenteredWithGivenHeight` ;<br>- arrondi au pair, flottants simple précision comme l'original.<br>Les view models déjà faits ne sont pas touchés (pas de refactor hors tâche) ; leurs copies privées sont notées comme suite possible. |
| P3 | **Bouton BACK, survol et sons**, les mêmes sur les trois écrans :<br>- bouton BACK seul, grossi et cerclé au survol, comme la sélection de piste ;<br>- Highlight à l'entrée de la souris dans une zone testée ;<br>- ScreenClick à l'affichage de l'écran (l'original le jouait à l'ouverture) ;<br>- ScreenBack à la sortie ;<br>- garde de première image, pas de focus MGUI. |
| P4 | **Highscores** :<br>- bande `rgba(0,0,0,174)` sur (0,160,1024,338) ;<br>- en-tête `Ui.Highscores.Header` ;<br>- onglets, lignes de séparation (deux `Border` d'un pixel, couleur (192,192,192,128) passée telle quelle, comme l'original) et colonnes aux formules d'origine ;<br>- 5 lignes, une par entrée de la donnée actuelle, les 5 autres vides ;<br>- temps convertis de « 01:11.82 » en « 1:11.82 » pour l'affichage seulement ;<br>- entrées et sons de l'original, dont la sortie par un clic sous la dernière ligne. |
| P5 | **Help** : en-tête `Ui.Help.Header`, panneau `Ui.Help.Panel` à `CalcRectangleKeep4To3(0,125,1024,512)`, bouton BACK. Échap, B, Back ou un clic gauche n'importe où (à l'appui) ferment. `RaceHelpViewModel`, ses sections et `RaceFrontEndCatalog.HelpSections` sont retirés. |
| P6 | **Options, dessin** :<br>- panneau `Ui.Options.Panel` à `CalcRectangleKeep4To3(0,125,1024,512)` ;<br>- zones de l'image en sprites `Ui.Options.*`, redessinées par-dessus avec `TextureColor` : (255,156,0,160) pour une case active, (15,15,15,230) pour masquer un libellé de résolution ou « High Detail » ;<br>- textes en GameFont : nom et « | », libellés de résolution centrés, « Vertical Sync » centré dans l'ancienne case High Detail, textes des pastilles ;<br>- Driving Mode : une pastille « Simulation » (active = Simulation) dans la zone vide à droite de Vibration, à (670,262,160,32) en espace texture, à valider sur capture ;<br>- poignées et flèche aux formules d'origine.<br>Centrage par `BitmapFont.Font.MeasureString` (FontStashSharp), à confirmer au début de la tâche ; sinon somme des avances du `.fnt`. |
| P7 | **Options, entrées** :<br>- haut/bas : arrêts dans l'ordre résolution, Fullscreen, Post Screen Effects, Shadows, Vertical Sync, Show FPS, Vibration, Driving Mode, Sound, Music, Sensitivity ;<br>- gauche/droite : change la résolution, ou règle un curseur de ±10 ;<br>- Entrée ou A : bascule une case ;<br>- la flèche se place à gauche de l'arrêt actif ; pour les 3 curseurs, aux positions d'origine (Line4/5/6) ;<br>- nom : saisi par `GameWindow.TextInput`, caractères 32 à 126 (ceux de GameFont), Retour arrière, 24 caractères comme aujourd'hui. Espace s'écrit dans le nom, comme dans l'original, et ne sert donc pas à basculer ;<br>- souris comme l'original : un clic dans une case ou sur une résolution la change avec ButtonClick, un clic sur un curseur le règle avec Highlight, sans glisser. Un clic dans une pastille ne règle pas en plus le curseur Sound qu'elle chevauche (défaut de la version modernisée) ;<br>- toute sortie (Échap, B, Back, BACK) passe par `ApplyAndClose`, puis ScreenBack ;<br>- volumes appliqués à chaque changement par une méthode qui ne touche qu'au son, sans appliquer la résolution. |
| P8 | **Assets** :<br>- en-têtes `Ui.{Highscores,Options,Help}.Header` sur `headers.png` ;<br>- `Ui.Options.Panel` et ses zones (5 résolutions, 4 cases) sur `OptionsScreenWindows.png` ;<br>- `Ui.Button.Radio` (935,427,39,39) sur `buttons.png` ;<br>- `HelpScreenWindows.png` copié de `RacingGame/Content/Textures/` dans `RacingGameCasaEngine/Content/Textures/`, ajouté à `UiTexturePremultiplier` et à l'exclusion du csproj, puis `Ui.Help.PanelTexture` et `Ui.Help.Panel` ;<br>- la flèche réutilise `Ui.CarSelection.Arrow`. |
| P9 | **ADR-0013**, « RacingGameCasaEngine's Highscores, Options and Help screens reproduce the original XNA screens » :<br>- D1 à D5 et P2 à P8 ;<br>- l'ADR-0001 cède sa parité d'états visuels pour ces écrans, comme pour les quatre précédents ;<br>- elle remplace l'ADR-0004 : enregistrement à toute sortie d'Options, volumes en direct.<br>`LegacyMenuUiTheme` et `RaceMenuTextButtonViewModel`, devenus inutilisés, sont retirés. Les manques MGUI concernés (M1, M2, M5, M11, M15) sont mis à jour. |
| P10 | **Validation par tâche** :<br>- `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ;<br>- `--smoke-frontend` et `--capture-ui-screens` code 0 sans avertissement, deux runs identiques ;<br>- contre les références de T0.2, seul l'écran de la tâche change ;<br>- rectangles mesurés sur capture contre les formules en 1920×1080, et en 1280×720 et 1024×768 par sonde ;<br>- éditeur, `scripts/capture_editor_screen.ps1` ;<br>- réglages de l'auteur restaurés à l'identique ;<br>- 🧪 pour l'auteur : entrées, sons, saisie du nom, volume en direct, enregistrement à la sortie. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (P1). Ne jamais committer sur `master`.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Les commits commencent par `cd D:/development/repo/RacingGame &&` : le hook de commit de l'auteur ne lit pas les chemins `/d/...`.
- **Ne jamais pousser.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Aucun projet de test n'existe pour RGCE.
- **Ne jamais modifier** `CasaEngine/` ni `CasaEngine/MGUI/` (D6) ; seul le pointeur change, en T0.2.
- Si le code est écrit mais qu'une vérification visuelle ou manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** les modifications préexistantes de l'auteur : `git add` fichier par fichier ; `.serena/` et `log.txt` ne sont jamais indexés.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- **Sondes** : uniquement dans le scratchpad, sur une copie ; jamais d'envoi de touches ni de mouvements de souris simulés. Une capture prise juste après une autre attend que la vue ait été redessinée : après l'encodage synchrone d'un PNG, le jeu enchaîne des mises à jour sans rendu.
- **Réglages de l'auteur** (`display-settings.json`, `front-end-options.json`) : sauvegardés avant les runs, restaurés à l'identique après (même SHA-1).
- **Budget** : au plus 3 cycles « correction → build ou validation » par tâche. Un 4e échec passe la tâche en ⚠️ Blocked, avec l'erreur et la question dans « Points ouverts », puis arrêt.
- **Retour arrière** : avant commit, suppression des seuls fichiers créés par la tâche et `git restore` des seuls fichiers suivis qu'elle a modifiés ; après commit, `git revert <SHA>` ; jamais de `git checkout`, `git reset --hard`, `git clean` ni `git stash`.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
- `--smoke-frontend` et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]` ; deux runs de capture identiques.
- Contre les références de T0.2 : seuls `highscores`, `options` et `help` changent.
- Rectangles mesurés contre les formules en 1920×1080, 1280×720 et 1024×768.
- `scripts/capture_editor_screen.ps1` sur les trois `.uiscreen` : code 0, sans erreur.
- Passe de vérification indépendante sur le résultat final.
- Vérification manuelle par l'auteur :
  - **Highscores** : onglets et sorties ;
  - **Options** : chaque ligne au clavier, à la manette et à la souris, saisie du nom, volume en direct, enregistrement à chaque sortie (Échap, B, Back, BACK), résolution appliquée au retour au menu ;
  - **Help** : fermeture par chaque entrée ;
  - **Sons** des trois écrans.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan, index et ADR

- Objectif : ce plan, son entrée dans `ai-agent/README.md`, l'ADR-0013 (P9), l'ADR-0004 en « Superseded by ADR-0013 », l'index des ADR.
- Validation : plan approuvé par l'auteur.
- Commit : `docs(racing-casa): plan the Highscores, Options and Help screens in the original look`

> Validation (2026-10-06) : relecture indépendante du plan (`plan-verifier`) READY ; plan approuvé par l'auteur, mode AUTO. ADR-0013 écrite ; ADR-0004 passée en « Superseded by ADR-0013 ».

### ✅ T0.2 — Pointeur CasaEngine à `19aa21f`

- Objectif : D4.
- Fichiers : le pointeur `CasaEngine`, ce plan.
- Validation :
  - `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ;
  - `--smoke-frontend` code 0 sans avertissement ;
  - deux runs `--capture-ui-screens` identiques ; contre `references-15b13c0`, chaque écart est nommé et expliqué ;
  - nouvelles références `references-<sha>`.
  - Si le build ou le smoke échoue à cause du moteur : ⚠️ Blocked, question à l'auteur.
- Commit : `chore(racing-casa): move CasaEngine to 19aa21f`

> Validation (2026-10-06) :
> - Pointeur `CasaEngine` : `7a44aba` → `19aa21f`, le `main` du moteur poussé par l'auteur. Il contient `7a44aba` et 158 commits de plus.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
> - `--smoke-frontend` : code 0, journal sans `[Warning]` ni `[Error]`.
> - Deux runs `--capture-ui-screens` (`ui-run-20261006-203648`, `-203708`) identiques entre eux et à `references-15b13c0`, à quelques pixels 3D isolés près (au plus 11 sur `race-hud`), comme d'un run à l'autre.
> - Réglages restaurés à l'identique.
> - Nouvelles références : `references-19aa21f` (copie de `ui-run-20261006-203708`).

## Phase 1 — Assets

### ✅ T1.1 — Sprites et textures des trois écrans

- Objectif : P8.
- Fichiers : `RacingGameCasaEngine/Content/Textures/HelpScreenWindows.png` (copie), `scripts/UiTexturePremultiplier/{Program.cs,README.md}`, `RacingGameCasaEngine/Content/UI/Textures/HelpScreenWindows.png`, `RacingGameCasaEngine.csproj` (exclusion), `scripts/generate_rgce_ui_assets.py`, les `.texture`/`.sprite` générés, `AssetInfos.json`.
- Validation :
  - copie identique à l'original (même blob) ;
  - copie prémultipliée exacte, outil et générateur reproductibles au bit près ;
  - `--smoke-frontend` sans asset manquant ; éditeur ;
  - captures inchangées (aucun écran ne les utilise encore).
- Commit : `feat(racing-casa): add the Highscores, Options and Help sprites`

> Validation (2026-10-06) :
> - `HelpScreenWindows.png` copié de `RacingGame/Content/Textures/` (même blob `b938803`), ajouté à `UiTexturePremultiplier` (programme et README) et à l'exclusion du lien du csproj.
> - Copie prémultipliée exacte : alpha identique, chaque canal égal à `(c·a + 127) / 255`, écart maximal 0. Une seconde exécution de l'outil réécrit les mêmes octets pour toutes les images.
> - Générateur : 9 textures, 60 sprites (15 nouveaux), 77 entrées possédées ; le catalogue ne gagne que des entrées (102 lignes ajoutées, aucune retirée), et une seconde exécution ne change rien.
>   - En-têtes `Ui.Options.Header` (512,212), `Ui.Help.Header` (512,312), `Ui.Highscores.Header` (0,412), tous de 512×100.
>   - `Ui.Button.Radio` (935,427,39,39).
>   - `Ui.Options.Panel`, ainsi que `Ui.Options.Resolution0` à `4`, `Fullscreen`, `PostScreenEffects`, `Shadows` et `HighDetail`, aux rectangles d'`Options.cs`.
>   - `Ui.Help.PanelTexture` et `Ui.Help.Panel`.
> - `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ; la sortie contient la nouvelle texture et les sprites.
> - `--smoke-frontend` code 0 sans avertissement ; `--capture-ui-screens` identique à `references-19aa21f` (aucun écran ne les utilise encore) ; réglages restaurés à l'identique.
> - Éditeur recompilé sur le moteur `19aa21f` (`CasaEngine.Editor.MonoGame.sln`, 0 erreur) ; `capture_editor_screen.ps1` sur `Help.uiscreen` : code 0, sans erreur.
> - M4 de `docs/mgui-gaps-from-rgce-xaml-screens.md` : 60 sprites sur 9 textures.

## Phase 2 — Écrans

### 🧪 T2.1 — Helper de mise en page et Help

- Objectif : P2, P3, P5, D1.
- Fichiers : `RacingGameCasaEngine.UI/LegacyScreenLayout.cs` (nouveau), `RaceHelpViewModel.cs`, `Help.xaml`, `Help.design.json`, `HelpScreen.cs`, `RaceFrontEndFlow.cs`, `RaceFrontEndCatalog.cs` (sections retirées).
- Validation :
  - P10 ;
  - en 1920×1080 : en-tête (12,16,614,90), panneau (0,176,1920,720), BACK (1639,951,187,81) ;
  - 🧪 fermeture et sons par l'auteur.
- Commit : `feat(racing-casa): draw the Help screen like the original`

> Validation (2026-10-06) :
> - **Code** :
>   - `LegacyScreenLayout` reprend `XToRes`, `YToRes`, `YToRes768`, `XToRes1400`, `YToRes1050`, `CalcRectangle`, `CalcRectangleKeep4To3`, `CalcRectangle1600` et `CalcRectangleCenteredWithGivenHeight` de `BaseGame.cs`, avec le même ordre de calcul en flottants. Il ajoute le bouton BACK et sa croissance au survol. Il est placé dans `RacingGameCasaEngine.UI/ViewModels/`, le seul dossier du projet, plutôt qu'à sa racine.
>   - `RaceHelpViewModel` : en-tête, panneau, bouton BACK.
>   - `Help.xaml` : décor, en-tête, panneau, BACK et son contour ; données de conception en 1280×720.
>   - `HelpScreen` : ScreenClick à l'affichage, Highlight à l'entrée sur BACK, sortie par Échap, B, Back ou clic gauche avec ScreenBack, garde de première image, sans focus.
>   - Les sections de texte, `HelpSection` et `RaceFrontEndCatalog.HelpSections` sont retirées ; la fabrique passe le jeu.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
> - `--smoke-frontend` : code 0 sans avertissement ; le journal montre « Menu sound ScreenClick played » à l'ouverture de Help.
> - Deux runs `--capture-ui-screens` identiques ; contre `references-19aa21f`, seul `help` change.
> - **Mesures** : bords du panneau mesurés par la luminance des lignes ; bouton BACK comparé au pixel près à celui de la sélection de piste du même run.
>   - 1920×1080 : panneau y 176 → 896, BACK identique en (1639,951,187,81).
>   - 1280×720 : panneau y 117 → 597, BACK identique en (1094,633,124,54).
>   - 1024×768 : panneau y 125 → 637, BACK identique en (840,675,134,58).
>   - En-tête : sa partie claire mesurée en (36,25)–(263,90) en 1920×1080, dans le rectangle (12,16,614,90).
> - Éditeur : `capture_editor_screen.ps1` sur `Help.uiscreen`, code 0 sans erreur, aperçu conforme.
> - Réglages restaurés à l'identique après chaque run.
> - 🧪 Reste pour l'auteur : la fermeture par chaque entrée (Échap, B, Back, clic) et les sons.

### 🧪 T2.2 — Highscores

- Objectif : P3, P4, D3.
- Fichiers : `RaceHighscoresViewModel.cs`, `Highscores.xaml`, `Highscores.design.json`, `HighscoresScreen.cs`, `RaceFrontEndFlow.cs`.
- Validation :
  - P10 ;
  - en 1920×1080 :
    - bande (0,270,1920,570), en-tête (12,16,614,90) ;
    - onglets à x 557, 872, 1213 et y 307 ;
    - lignes de séparation à y 351 et 352 ;
    - lignes de score à partir de y 371, pas de 46 ;
    - colonnes à x 562, 656 et 1200 ;
  - ouverture sur Advanced ;
  - 🧪 onglets, sorties et sons par l'auteur.
- Commit : `feat(racing-casa): draw the Highscores screen like the original`

> Validation (2026-10-06) :
> - **Code** :
>   - `RaceHighscoresViewModel` : bande, en-tête, 3 onglets, ligne de séparation, 10 lignes de score (rang, nom, temps), bouton BACK, échelle des textes GameFont, couleurs.
>   - `Highscores.xaml` (généré : 10 lignes de 3 textes) et ses données de conception en 1280×720.
>   - `HighscoresScreen` :
>     - entrées et sons de l'original ; le Highlight des lignes ne joue que sur les lignes remplies ;
>     - couleurs des textes réglées par le code, MGUI ne liant pas `Foreground` (M7) ;
>     - ouverture sur Advanced ; temps « 01:48.22 » affichés « 1:48.22 ».
>   - `RacingGameCasaEngineGame.MeasureGameFontText` mesure un texte avec `BitmapFont.Font.MeasureString` (FontStashSharp), sur un handle de la police acquis par l'`AssetContentManager` ; la fin de la ligne de séparation en dépend.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE. `--smoke-frontend` : code 0 sans avertissement.
> - Deux runs `--capture-ui-screens` identiques ; contre `references-19aa21f`, seuls `help` et `highscores` changent.
> - **Mesures** (sonde de taille pour 1280×720 et 1024×768) :
>   - bande : 1920×1080 y 270 → 840 ; 1280×720 y 180 → 560 ; 1024×768 y 192 → 598, exacts.
>   - Lignes de séparation aux rangées exactes : 351-352, 234-235, 250-251. Leur couleur mesurée (201,199,197) sur fond (17,15,11) est celle de l'original : 0,753 + 0,498 × fond, la couleur passant telle quelle.
>   - Onglet « Advanced » : début x 873, 582 et 466, pour les plumes à 872, 581 et 465. Premier temps : 1204, 803 et 642, pour 1200, 800 et 640. Les glyphes commencent de 0 à 2 unités de police à droite de la plume.
>   - Haut des lettres à 1 pixel près de « cellule + 4/36 de cellule », la marge mesurée des glyphes de GameFont.
>   - Bouton BACK identique au pixel près à celui de la sélection de piste, aux trois tailles.
> - **Écart connu** : la ligne de séparation finit à x 1345, 897 et 718, contre 1347, 894 et 717 dans l'original. La largeur de « 5:67:89 » est mesurée sur la police mise à l'échelle, alors que l'original arrondissait l'avance de chaque glyphe (même limite que M16).
> - Éditeur : `capture_editor_screen.ps1` sur `Highscores.uiscreen`, code 0 sans erreur. L'aperçu garde la police de l'éditeur et des textes non mis à l'échelle (E1).
> - Réglages restaurés à l'identique après chaque run.
> - 🧪 Reste pour l'auteur : onglets (clic, gauche/droite), sorties (Échap, B, Back, clic sous les lignes, BACK) et sons.

### 🧪 T2.3 — Options

- Objectif : P3, P6, P7, D2.
- Fichiers :
  - `RaceOptionsViewModel.cs`, `Options.xaml`, `Options.design.json`, `OptionsScreen.cs` ;
  - `RacingGameCasaEngineGame.cs` (volumes seuls) ;
  - `UI/LegacyMenuUiTheme.cs` et `RaceMenuTextButtonViewModel.cs` (retirés), `docs/mgui-gaps-from-rgce-xaml-screens.md`.
- Validation :
  - P10 ;
  - en 1920×1080 :
    - panneau (0,176,1920,720) ;
    - cases de résolution à y 334, x 636, 851, 1078, 1320 et 1571 ;
    - curseurs à y 571, 674 et 778 ;
    - poignée Sound à x 1356 pour 0,8 ;
    - nom à (660, 239) ;
  - smoke : la résolution appliquée au retour au menu reste vérifiée ;
  - 🧪 chaque ligne, saisie du nom, volume en direct et enregistrement à chaque sortie, par l'auteur.
- Commit : `feat(racing-casa): draw the Options screen like the original`

> Validation (2026-10-06) :
> - **Code** :
>   - `RaceOptionsViewModel` place tout aux formules d'`Options.cs` : panneau, zones en `CalcRectangleKeep4To3` + `YToRes768(125)`, nom, poignées, flèche qui oscille, bouton BACK.
>   - `Options.xaml` (généré) redessine les zones avec `TextureColor` : (15,15,15,230) sur les libellés de résolution et « High Detail », (255,156,0,160) sur une case active, (180,180,180,120) sur une pastille inactive. Il est suivi de ses données de conception en 1280×720.
>   - `OptionsScreen` :
>     - le nom se tape par `GameWindow.TextInput` (caractères 32 à 126, Retour arrière, 24 caractères) ;
>     - 11 arrêts pour la flèche, qui part sur Sound comme l'original ;
>     - Entrée ou A bascule ; gauche et droite changent la résolution ou un curseur de ±10 ;
>     - un clic sur une option a priorité sur le curseur qu'elle chevauche ;
>     - Highlight à l'entrée de chaque zone ; toute sortie passe par `ApplyAndClose`, avec ScreenBack.
>   - `RacingGameCasaEngineGame.ApplyFrontEndVolumes` applique le volume en direct, sans la résolution ; `GetResolutionLabels` donne les libellés.
>   - `LegacyMenuUiTheme` et `RaceMenuTextButtonViewModel`, devenus inutilisés, sont retirés.
> - **Disposition des lignes ajoutées** (P6, validée sur capture), en unités de l'image :
>   - la mesure des textes en GameFont montre que « Show FPS » (104,6 unités depuis x 382) chevauchait la pastille Vibration de la version modernisée (x 470), et qu'aucune rangée ne tient trois pastilles avec leur flèche ;
>   - Driving Mode est donc une pastille « Simulation » en (616,182), sur la rangée GRAPHIC, et non en (670,262) comme proposé ;
>   - Gamepad Vibration passe en (616,262), même colonne que « Shadow » ; Show FPS reste en (339,262) ;
>   - Vertical Sync s'écrit centré dans la case High Detail, en ambre quand il est actif, comme le libellé de la résolution choisie.
>   - Flèche : devant « RESOLUTION » (78,110), devant « GRAPHIC » pour Fullscreen (125,180), devant l'option ailleurs, et aux positions d'origine Line4/5/6 pour les curseurs.
> - **Traits de la version modernisée reproduits** :
>   - les libellés de résolution, écrits sur la base 1400, débordent de leurs cases ;
>   - les caches (15,15,15,230) se voient légèrement en rectangles ;
>   - la pastille Show FPS recouvre l'icône du haut-parleur de Sound.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE.
> - `--smoke-frontend` : code 0 sans avertissement ; « Options -> MainMenu (apply resolution) » puis « Verified applied resolution on MainMenu ».
> - Deux runs `--capture-ui-screens` identiques ; contre `references-19aa21f`, seuls `help`, `highscores` et `options` changent.
> - **Mesures** (valeurs enregistrées : Sound 100, Sensitivity 60 ; sonde de taille pour 1280×720 et 1024×768) :
>   - panneau : y 176 → 896, 117 → 597, 125 → 637, exacts ;
>   - flèche et poignées : la partie orange du sprite tombe au pixel près où l'image de la flèche (874,426,53,39) et de la pastille (935,427,39,39) la place dans le rectangle calculé. En 1920×1080 : flèche (274,575,116,55), orange (281..378, 581..623) ; poignée Sound (1524,571,73,55), orange (1531..1587, 578..619) ; poignée Sensitivity (1188,778), orange dès x 1195 ;
>   - la case Post Screen Effects teintée commence à x 636 (luminance 9 dedans, 21 à gauche), 424 et 339 ;
>   - nom : plume (660,234), premier glyphe en x 663, haut 239 ; de même à 1 pixel près aux deux autres tailles.
> - Éditeur : `capture_editor_screen.ps1` sur `Options.uiscreen`, code 0 sans erreur (aperçu limité par E1).
> - Manques MGUI à jour : M1, M2, M5, M11, M15, M16 et M17.
> - Réglages restaurés à l'identique après chaque run.
> - 🧪 Reste pour l'auteur :
>   - chaque ligne au clavier, à la manette et à la souris ;
>   - la saisie du nom ;
>   - le volume en direct ;
>   - l'enregistrement à chaque sortie (Échap, B, Back, BACK) et la résolution appliquée au retour au menu ;
>   - la disposition des lignes ajoutées.

## Phase 3 — Clôture

### ⏳ T3.1 — Validation globale, documentation et rapport de fin

- Objectif : « Validation globale », nouvelles références, passe de vérification indépendante, manques MGUI à jour, rapport de fin, index.
- Validation : liste « Validation globale ».
- Commit : `docs(racing-casa): close the Highscores, Options and Help plan`

> Interrompue le 2026-10-06 par le retour de l'auteur (phase 4) :
> - références `references-7bbdd39` prises (copie de `ui-run-20261006-210034`) ;
> - vérification indépendante de l'état `7bbdd39` : CONFIRMED sur les 7 points (son bilan figure dans l'état vérifié de la phase 4).
>
> La clôture est reprise en T4.3.

---

## Phase 4 — Retour de l'auteur (2026-10-06)

Demande de l'auteur : « Désolé je me suis trompé pour l'écran help et option on va garder la disposition d'avant mais en uniformisant le style ». Highscores reste tel que livré en T2.2.

État vérifié (2026-10-06) :
- **Vérification indépendante de `7bbdd39`** (`verifier`, contexte neuf) : CONFIRMED sur les 7 points. Elle a refait les mesures, le build, le smoke, les captures et la reproductibilité du générateur. Ses remarques :
  - A1 (P3) : le seuil du stick est de 0,5, alors que l'original utilisait 0,75. C'est la convention des écrans RGCE depuis le menu principal (ADR-0007). À documenter ;
  - A2 (P4) : l'ordre des arrêts de P7 n'est plus à jour (Driving Mode est passé en troisième) ;
  - A3 (P4) : le commentaire de `SaveFrontEndOptions` cite encore l'ADR-0004.
- **Dispositions d'avant** (`references-19aa21f`, et le code de `944cdb8`) :
  - **Help** : bande, titre « Help », six sections (titre orange et deux lignes blanches) dans une zone à barre de défilement, bouton Back ;
  - **Options** : une colonne de libellés et une colonne de contrôles MGUI :
    - nom (zone de texte) ;
    - 5 boutons de résolution, 2 boutons Driving Mode ;
    - 6 cases : Fullscreen, Vertical Sync, Post Screen Effects, Shadows, Show FPS, Gamepad Vibration ;
    - 3 curseurs avec leur valeur : Sound Volume, Music Volume, Controller Sensitivity ;
    - bouton Back.
- **Textes des sections Help** (`git show e23d991:RacingGameCasaEngine/Bootstrap/RaceFrontEndCatalog.cs`) : Race Controls, Steering, Camera et Race Flow, deux lignes chacune. Menus et Session Flow sont des notes de développement.
- **Largeurs en GameFont**, en unités de la base 1024 (avances de `scripts/generate_rgce_gamefont.py`) :
  - la plus longue ligne d'aide fait 1 040 unités à l'échelle 1 et 780 à l'échelle 0,75 ;
  - le plus long libellé d'Options, « Controller Sensitivity », fait 227 unités ; les cinq résolutions font 517 unités en tout.
- **Art réutilisable de l'original** :
  - le trait des curseurs est le sprite `Ui.CarSelection.StatBar` (372,297,472,6) d'`OptionsScreenWindows.png` ;
  - le champ du nom occupe (346,27)–(891,76) dans la même image ;
  - pastille `Ui.Button.Radio`, flèche `Ui.CarSelection.Arrow`.
- **Assets devenus inutiles** : `Ui.Help.Panel`, `Ui.Help.PanelTexture` et la copie de `HelpScreenWindows.png` (avec sa ligne dans `UiTexturePremultiplier` et l'exclusion du csproj) ; `Ui.Options.Panel` et ses 9 zones. Le générateur ne retire pas les entrées de catalogue qu'il ne produit plus.

| Réf | Décision |
|---|---|
| D7 | **Help** : disposition d'avant, au style des autres écrans. Seules les quatre sections destinées au joueur sont gardées (Race Controls, Steering, Camera, Race Flow), textes inchangés, toutes visibles sans barre de défilement (auteur, 2026-10-06). |
| D8 | **Options** : disposition d'avant (une colonne de libellés, une colonne de contrôles) dans le vocabulaire de l'original (auteur, 2026-10-06) :<br>- pastilles rondes, orange si actives, grises sinon ;<br>- curseurs en trait avec poignée ronde ;<br>- résolutions et mode de conduite en libellés GameFont, en ambre si choisis ;<br>- nom en GameFont avec le curseur « \| ». |
| D9 | **Comportement d'Options** : celui livré en T2.3 est gardé (auteur, 2026-10-06) :<br>- sons ; flèche au clavier et à la manette sur chaque ligne ;<br>- Entrée ou A pour basculer, gauche et droite pour régler ;<br>- nom tapé directement ; volume en direct ;<br>- toute sortie applique et enregistre. |

| Réf | Proposition |
|---|---|
| P11 | **Help** :<br>- bande `rgba(0,0,0,174)` sur (0,150,1024,340) ; en-tête `Ui.Help.Header` ;<br>- pour chaque section, le titre en GameFont orange (255,156,0) à l'échelle 1, en x 120 ; les deux lignes en blanc à l'échelle 0,75, en x 140 ;<br>- de haut en bas à partir de y 170 : titre au pas de 27, lignes au pas de 20, 8 d'écart entre sections (fin vers y 462) ;<br>- bouton BACK, entrées et sons de T2.1 ;<br>- `RaceFrontEndCatalog.HelpSections` revient avec les quatre sections ;<br>- `Ui.Help.Panel`, `Ui.Help.PanelTexture` et `HelpScreenWindows.png` sont retirés (fichiers, outil, csproj, catalogue). |
| P12 | **Options, disposition** :<br>- bande (0,110,1024,370) et en-tête `Ui.Options.Header` ;<br>- 12 lignes au pas de 27 à partir de y 122, dans l'ordre d'avant : Player Name, Resolution, Driving Mode, Fullscreen, Vertical Sync, Post Screen Effects, Shadows, Show FPS, Gamepad Vibration, Sound Volume, Music Volume, Controller Sensitivity ;<br>- libellés en GameFont blanc, alignés à droite sur x 380 ; contrôles à partir de x 400 :<br>  - nom : le champ de l'original (`Ui.Options.NameField`, (346,27,546,50)) en (395, y − 3, 400, 25), le texte blanc en x 405, avec « \| » ;<br>  - résolutions : cinq libellés espacés de 16 ; Driving Mode : « Arcade » et « Simulation » espacés de 24 ; ambre (255,156,0) si choisi, blanc sinon ;<br>  - cases : pastille carrée de `YToRes(22)` pixels, teintée (255,156,0,160) si active, (180,180,180,120) sinon ;<br>  - curseurs : trait `Ui.CarSelection.StatBar` de 300 unités de large et `YToRes(6)` de haut, centré sur la ligne ; poignée pastille de `YToRes(22)` centrée sur la valeur ; valeur en blanc en x 720 ;<br>  - flèche : `Ui.CarSelection.Arrow` haute de `YToRes(22)`, au rapport 53/39, à `XToRes(8)` devant le libellé, qui oscille de 0 à 16 unités comme l'original. |
| P13 | **Options, entrées** (D9) :<br>- 11 arrêts dans l'ordre des lignes : Resolution, Driving Mode, Fullscreen, Vertical Sync, Post Screen Effects, Shadows, Show FPS, Gamepad Vibration, Sound, Music, Sensitivity. La flèche part sur Sound ;<br>- gauche et droite changent la résolution ou le mode de conduite (ButtonClick), ou règlent un curseur de ±10 (Highlight) ;<br>- Entrée ou A bascule une case ou le mode de conduite ;<br>- clic sur un libellé de résolution ou de mode, sur une ligne de case (pastille et libellé) ou sur le trait d'un curseur ;<br>- Highlight à l'entrée de chacune de ces zones ;<br>- seuil du stick à 0,5, convention RGCE (A1). |
| P14 | **ADR-0014**, qui remplace l'ADR-0013 :<br>- Highscores comme l'ADR-0013 ;<br>- Help et Options selon D7 à D9 et P11 à P13 ;<br>- enregistrement à toute sortie d'Options ;<br>- seuil du stick à 0,5.<br>Le commentaire de `SaveFrontEndOptions` cite l'ADR-0014 (A3). `Ui.Options.Panel` et ses 9 zones sont retirés, `Ui.Options.NameField` est ajouté. |
| P15 | **Validation par tâche** : celle de P10. Contre `references-7bbdd39`, seul l'écran de la tâche change ; les rectangles sont mesurés contre ces formules en 1920×1080, 1280×720 et 1024×768. 🧪 pour l'auteur : rendu, entrées et sons. |

### 🧪 T4.1 — Help, disposition d'avant au style unifié

- Objectif : D7, P11, et l'ADR-0014 (P14).
- Fichiers :
  - `RaceHelpViewModel.cs`, `Help.xaml`, `Help.design.json`, `HelpScreen.cs`, `RaceFrontEndCatalog.cs` ;
  - `scripts/generate_rgce_ui_assets.py`, `scripts/UiTexturePremultiplier/{Program.cs,README.md}`, `RacingGameCasaEngine.csproj`, `AssetInfos.json` ; les fichiers de `Ui.Help.Panel*` et `HelpScreenWindows.png` (retirés) ;
  - `docs/decisions/0014-…`, `0013-…` (statut), `docs/decisions/README.md`.
- Validation : P15 ; toutes les lignes visibles, sans barre de défilement, aux trois tailles.
- Commit : `feat(racing-casa): show the Help sections in the menus' style`

> Validation (2026-10-08) :
> - **Décision** : ADR-0014 écrite, ADR-0013 passée en « Superseded by ADR-0014 », index à jour.
> - **Code** :
>   - `RaceFrontEndCatalog.HelpSections` revient avec les quatre sections joueur, textes de `e23d991`.
>   - `RaceHelpViewModel` (P11) : bande, en-tête, 4 sections (titre, 2 lignes), bouton BACK.
>   - `Help.xaml` est généré, avec les données de conception en 1280×720.
>   - `HelpScreen` : titres à l'échelle 1, lignes à 0,75. Les entrées et sons de T2.1 sont inchangés.
> - **Assets retirés** : `Ui.Help.Panel`, `Ui.Help.PanelTexture` et les deux copies de `HelpScreenWindows.png`, ainsi que leurs lignes dans le générateur, l'outil de prémultiplication, son README et l'exclusion du csproj. Les 3 entrées de catalogue sont retirées par script, au format du générateur. Celui-ci donne ensuite 8 textures et 59 sprites, sans rien changer à une seconde exécution.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement dans RGCE. `--smoke-frontend` : code 0 sans avertissement.
> - **Captures** :
>   - contre `references-7bbdd39`, `help` change ; `options` diffère aussi de 0,3 %, l'écart de survol du champ « Player One » déjà connu.
>   - Trois runs ont été faits. Entre eux, `help`, `track-selection`, `car-selection`, `race-hud` et `race-finished` varient. Les écarts de `help` et `track-selection` sont tous dans la zone de rebond du logo (x 650..1833, y 52..438), dont l'animation n'est pas figée pendant les captures. Ceux de `race-hud` viennent d'une image de décalage du chronomètre (0:00.95 contre 0:00.96). Ce sont des écarts de timing, sans lien avec Help.
> - **Mesures** (sonde de taille pour 1280×720 et 1024×768) :
>   - bande : y 253 → 827, 169 → 551, 180 → 588, exacts ;
>   - premier titre et première ligne à 1 pixel près de « plume + marge du glyphe » ;
>   - la ligne la plus longue finit à x 1724 sur 1920, 1149 sur 1280, 919 sur 1024 : tout est visible sans barre de défilement ;
>   - bouton BACK identique au pixel près à celui de la sélection de piste, aux trois tailles.
> - Éditeur : `capture_editor_screen.ps1` sur `Help.uiscreen`, code 0 sans erreur.
> - Manques MGUI : M4 (59 sprites sur 8 textures), M5, M16 et M17 comptent de nouveau Help.
> - Réglages restaurés à l'identique après chaque run.
> - 🧪 Reste pour l'auteur : le rendu, la fermeture (Échap, B, Back, clic) et les sons.

### ⏳ T4.2 — Options, disposition d'avant au style unifié

- Objectif : D8, D9, P12, P13.
- Fichiers :
  - `RaceOptionsViewModel.cs`, `Options.xaml`, `Options.design.json`, `OptionsScreen.cs`, `RacingGameCasaEngineGame.cs` (commentaire A3) ;
  - le générateur et `AssetInfos.json` ; les sprites `Ui.Options.Panel` et ses 9 zones (retirés), `Ui.Options.NameField` (ajouté) ;
  - `docs/mgui-gaps-from-rgce-xaml-screens.md` (M4).
- Validation : P15 ; smoke : la résolution appliquée au retour au menu reste vérifiée.
- Commit : `feat(racing-casa): lay Options out as before in the menus' style`

### ⏳ T4.3 — Clôture

- Objectif : T3.1 sur l'état final (validation globale, nouvelles références, vérification indépendante, rapport de fin, index).
- Commit : `docs(racing-casa): close the Highscores, Options and Help plan`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| — | Aucun à l'écriture du plan. | — |

## Hors périmètre

- Une table de scores enregistrée et alimentée par les courses (D3).
- Le post-effet, la scène 3D derrière les menus, le curseur de l'original (D5). Sans scène derrière, les panneaux d'Options et de Help, sombres à 80 %, paraissent plus sombres que dans l'original.
- La souris et le volant montrés par l'image d'aide, s'ils ne sont pas gérés par RGCE.
- Les sons du menu principal, et la mise en commun des helpers de mise en page des écrans déjà faits (P2).
- Toute modification de CasaEngine ou de MGUI (D6).
