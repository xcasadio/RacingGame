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

### ⏳ T0.2 — Pointeur CasaEngine à `19aa21f`

- Objectif : D4.
- Fichiers : le pointeur `CasaEngine`, ce plan.
- Validation :
  - `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ;
  - `--smoke-frontend` code 0 sans avertissement ;
  - deux runs `--capture-ui-screens` identiques ; contre `references-15b13c0`, chaque écart est nommé et expliqué ;
  - nouvelles références `references-<sha>`.
  - Si le build ou le smoke échoue à cause du moteur : ⚠️ Blocked, question à l'auteur.
- Commit : `chore(racing-casa): move CasaEngine to 19aa21f`

## Phase 1 — Assets

### ⏳ T1.1 — Sprites et textures des trois écrans

- Objectif : P8.
- Fichiers : `RacingGameCasaEngine/Content/Textures/HelpScreenWindows.png` (copie), `scripts/UiTexturePremultiplier/{Program.cs,README.md}`, `RacingGameCasaEngine/Content/UI/Textures/HelpScreenWindows.png`, `RacingGameCasaEngine.csproj` (exclusion), `scripts/generate_rgce_ui_assets.py`, les `.texture`/`.sprite` générés, `AssetInfos.json`.
- Validation :
  - copie identique à l'original (même blob) ;
  - copie prémultipliée exacte, outil et générateur reproductibles au bit près ;
  - `--smoke-frontend` sans asset manquant ; éditeur ;
  - captures inchangées (aucun écran ne les utilise encore).
- Commit : `feat(racing-casa): add the Highscores, Options and Help sprites`

## Phase 2 — Écrans

### ⏳ T2.1 — Helper de mise en page et Help

- Objectif : P2, P3, P5, D1.
- Fichiers : `RacingGameCasaEngine.UI/LegacyScreenLayout.cs` (nouveau), `RaceHelpViewModel.cs`, `Help.xaml`, `Help.design.json`, `HelpScreen.cs`, `RaceFrontEndFlow.cs`, `RaceFrontEndCatalog.cs` (sections retirées).
- Validation :
  - P10 ;
  - en 1920×1080 : en-tête (12,16,614,90), panneau (0,176,1920,720), BACK (1639,951,187,81) ;
  - 🧪 fermeture et sons par l'auteur.
- Commit : `feat(racing-casa): draw the Help screen like the original`

### ⏳ T2.2 — Highscores

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

### ⏳ T2.3 — Options

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

## Phase 3 — Clôture

### ⏳ T3.1 — Validation globale, documentation et rapport de fin

- Objectif : « Validation globale », nouvelles références, passe de vérification indépendante, manques MGUI à jour, rapport de fin, index.
- Validation : liste « Validation globale ».
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
