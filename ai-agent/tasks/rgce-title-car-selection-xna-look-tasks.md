# Plan agent IA — Écran titre et sélection de voiture au look du jeu XNA d'origine (RacingGameCasaEngine)

Plan d'exécution de la demande de l'auteur du 2026-10-06, faite avec deux captures vidéo du jeu d'origine :
- « image 2 : écran titre » (logo en haut à droite, bande sombre au milieu, « Press START to continue. » avec START en gras) ;
- « image 1 : sélection voiture. Les voitures tournent sur elles-mêmes. Appuyer sur gauche ou droite fait tourner le carrousel des voitures. La voiture devant étant celle sélectionnée. »

Les décisions D1 → D7 ci-dessous ont été arbitrées avec l'auteur le 2026-10-06 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

- **Écran titre** : il devient celui de la capture. Il montre :
  - le fond des menus et le logo qui rebondit ;
  - la bande sombre au milieu ;
  - le sprite « Press START to continue. » qui clignote.

  On en sort au clic, au clavier ou à la manette, avec le son de l'original.
- **Sélection de voiture** : elle devient celle de la capture. Elle montre :
  - l'en-tête « CHOOSE YOUR CAR » ;
  - un carrousel 3D de trois voitures sur leurs plateaux, qui tournent sur elles-mêmes ; gauche et droite font tourner le carrousel, et la voiture de devant est la sélectionnée ;
  - les flèches animées ;
  - les six barres de caractéristiques ;
  - la palette de la capture, avec la case sélectionnée agrandie ;
  - les boutons A SELECT et B BACK de l'original ;
  - les textes dans la police de l'original ;
  - les sons de menu de l'original.
- **Peinture** : la couleur choisie est peinte sur la voiture comme dans l'original, avec le masque alpha de sa texture, dans le carrousel et en course.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-06)

Découverte en lecture seule (workflow de 4 agents), faits porteurs revérifiés directement.

- **Branche** : `remaster`, HEAD `013020d`.
  - Le pointeur du sous-module `CasaEngine` est modifié dans l'arbre de travail (`dd91efe`, « Merge remote-tracking branch 'origin/main' »). Ce changement ne vient pas de cette session et n'est jamais indexé par ce chantier.
  - `.serena/` et `log.txt`, non suivis, sont à l'auteur.
- **Deux sources pour « l'original », qui divergent** :
  - le code du dépôt, `git show 4f840a3^:RacingGame.Shared/…`, est le portage MonoGame ;
  - les captures de l'auteur montrent une autre version : leur menu principal a 6 boutons, alors que le code en a 5.
  - Écran titre :
    - le code (`GameScreens/SplashScreen.cs:40-56`) dessine la scène 3D survolée, `RenderBlackBar(518, 61)` et le sprite centré en y 518 + 61/2, sans fond de menu ni logo ;
    - la capture montre le fond des menus, le logo qui rebondit et la bande à y 350, de 61 de haut. Le calage de la capture est mesuré : interface en 4:3, bande à 350,4 → 411,6 en unités de 640.
  - Sélection :
    - le code place le carrousel autour de (1,5 ; 0 ; 1) (`CarSelection.cs:162-168`), les flèches à x 35 et 636 (`:279-294`) et la palette `White, Yellow, Blue, Purple, Red, Green, Teal, Gray, Chocolate, MonoGameOrange, SeaGreen` (`RacingGameManager.cs:77-91`) ;
    - la capture a le carrousel centré, les flèches contre le plateau avant (pointes à x ≈ 281 et ≈ 741), une palette en cercle chromatique d'orange à or et une pastille web.
  - Tout le reste concorde à environ 3 px près une fois la capture recalée : bandes, en-tête, barres, cases, boutons A/B. Le recalage (x et y en unités de 1024×640) :
    - capture de la sélection (1193×899) : `x_cap = 1,171875·x − 9`, `y_cap = 1,40625·y − 2` ;
    - capture du titre (1189×771, bas rogné) : 1,1611 px par unité en x, 1,3934 en y.
- **Écran titre d'origine** (`SplashScreen.cs`, `UIRenderer.cs`, `BaseGame.cs` de `4f840a3^`) :
  - « Press START to continue. » est un sprite de `headers.png`, `PressStartGfxRect` (2, 1, 631, 45) (`UIRenderer.cs:25`). Le texte est jaune doré, START est blanc, avec un contour noir. Il est placé par `CalcRectangleCenteredWithGivenHeight(512, y, 26, rect)` (`BaseGame.cs:658-670`) : hauteur 26 unités, largeur au rapport du sprite, en pixels.
  - Il est visible quand `(int)(TotalTime / 0,375) % 3 != 0` (`SplashScreen.cs:51`) : caché 0,375 s, visible 0,75 s.
  - Entrées (`SplashScreen.cs:21-25`) : clic gauche n'importe où, Espace, Échap, Start de la manette. Entrée et A ne sont pas pris. La sortie joue `ScreenBack` (`RacingGameManager.cs:493-511`).
  - Bande : texel (99, 999) de `buttons.png` = rgba(0,0,0,205), teinté à 0,85, donc une opacité de 0,683 (`UIRenderer.cs:584-589`). Le menu principal de RGCE l'utilise déjà (rgba(0,0,0,174)).
- **Sélection d'origine** (`CarSelection.cs` de `4f840a3^`, relu directement pour les entrées et les flèches) :
  - **Entrées** (`:63-138`) :
    - gauche (clavier, manette, ou clic dans (562, 170, 362, 135)) → voiture (n+1) % 3 ;
    - droite (ou clic dans (100, 170, 312, 135)) → (n+2) % 3 ;
    - haut → couleur précédente, bas → couleur suivante, en boucle sur 11 ;
    - bouton gauche de la souris maintenu sur une case → cette couleur ;
    - A, Espace ou clic sur A → sélection de piste ;
    - Échap, B, Back ou clic sur B → retour ;
    - chaque changement joue `Highlight`.
  - **Carrousel** :
    - l'angle rejoint n·2π/3 à 5 rad/s constants, par le plus court chemin (`:65-71`, `:350-382`) ;
    - chaque voiture et son plateau tournent sur eux-mêmes à 1/3,9 rad/s (`:162-168`).
  - **Flèches** : sprite `SelectionArrowGfxRect` (874, 426, 53, 39) de `buttons.png` ; celle de gauche est en miroir.
    - Taille fixe (53, 39) en unités.
    - y = `YToRes(360) + YToRes(120)/3`.
    - Balancement en x de `XToRes(12)·sin(t/0,46)·cos(t/0,285)` (`:270-294`). Le facteur `arrowScale` ne touche que la grande flèche, qui n'est pas dessinée.
  - **Barres** (`:229-267`, `:396-404`) :
    - six lignes « Max Speed: NNNmph », « Acceleration: », « Car Mass: », « Braking: », « Friction: », « Engine: » ;
    - x 766, y 190, 235, 280, 335, 390, 445 ;
    - barre de 6 unités de haut à y + 29, largeur `(int)(192·valeur)` découpée dans `OptionsScreenWindows.png` (372, 297, 472, 6) ;
    - valeurs calculées par les formules du fichier à partir des tableaux de la voiture (calcul flottant de l'agent de découverte) : 288, 275 et 242 mph.
  - **Cases de couleur** : `ColorSelection.png` (64×64) teinté, de 46×46 à (250 + 50n, 500). La sélectionnée fait 58×58, décalée de −6 (`:210-221`). « Car Color: » est à (85, 512).
  - **En-tête** : `headers.png` (0, 212, 512, 100) en repère 1600×1200, à (10, 18) (`UIRenderer.cs:26`).
  - **Boutons A/B** : `CalcRectangleCenteredWithGivenHeight` de hauteur 48 en y 587, B à `XToRes(50)` du bord droit, A à `XToRes(80)` (`UIRenderer.cs:604-675`). Au survol, le bouton grandit de `XToRes(16)`×`YToRes(9)` et reçoit le contour (424, 240, 212, 92), avec le son `Highlight`.
  - **Ordre de dessin** : la 3D est dessinée après tous les sprites 2D et avant les textes (`PostUIRender`).
  - **Peinture** : `lerp(rgb, couleur, alpha de la texture)` ; la couleur 0 n'est pas teintée (`NormalMapping.fx:692`, `Model.cs:609-617`). Les trois voitures prennent la même couleur.
  - **Police** : bitmap `GameFont.png`. Avance des glyphes en `XToRes1400`, hauteur `YToRes1050(36)` (`TextureFont.cs`).
- **RGCE aujourd'hui** :
  - `Splash.xaml` est un panneau de remplacement (titre, paragraphe, bouton Continue) sur l'atlas `background.png` entier en `UniformToFill`. `SplashScreen.cs` n'a ni view model ni code d'entrée.
  - `CarSelection.xaml` est en pixels fixes de 1280×720 (bande rgba(0,0,0,132) y 191, hauteur 439). Il contient :
    - les boutons texte `<` et `>` ;
    - l'aperçu d'une seule voiture, rendu par `UI/CarSelectionPreviewRenderer.cs` (BasicEffect, cible de rendu fixe de 360×220) ;
    - 4 barres normalisées min-max ;
    - 11 cases MGUI ;
    - les boutons A/B.
  - La navigation se fait par le focus MGUI. Il n'y a aucun son.
  - Palette `RaceFrontEndCatalog.CarColors` (`RaceFrontEndCatalog.cs:16-28`) = celle du code d'origine (Orange à la place de MonoGameOrange).
  - **La couleur choisie n'est rendue nulle part dans le pipeline du moteur** :
    - `LegacyCarVisualFactory.ResolveTintParameters` (`:185`, `:213`) remplit `LitDiffuseMaterial.TintColor` et `TintStrength`, mais aucun code de `LitDiffuseMaterial.cs` ni aucun shader ne les lit (`rg -i tint` : seules les déclarations `:29-31`) ;
    - seul l'aperçu BasicEffect approche la teinte (`CarSelectionPreviewRenderer.cs:278-280`).
  - `Content/Models/CarSelectionPlate.gltf` existe et n'est chargé par aucun code. Le modèle et la voiture sont en Y vers le haut, à la même échelle.
  - `RacerCar*.tga`, `headers.png`, `ColorSelection.png`, `OptionsScreenWindows.png` et `GameFont.png` n'arrivent à la sortie que par le lien `None Include="..\RacingGame\Content\Textures\*.*"` du csproj (`RacingGameCasaEngine.csproj:48`, qui exclut `background`, `buttons` et `ingame`). Aucun n'est catalogué.
  - Sons : seuls `Beep` et `Bleep` existent (`Content/Audio/*.sound`, ADR-0003). `menu_highlight.wav`, `menu_screenclick.wav` et `menu_screenback.wav` sont dans `RacingGame/Content/Audio/Waves`.
  - Couleur d'effacement des mondes hors course : `CornflowerBlue` (`RacingGameCasaEngineGame.cs:209`). Sous le fond à 0,85, le haut des menus sort à (15,23,36) au lieu du noir de l'original (`BaseGame.cs:24`).
- **CasaEngine et MGUI** (sous-modules, à ne pas modifier) :
  - **Vue rendue dans une texture** :
    - `ViewManager.CreateView(ViewDefinition { World, Camera, Surface, ClearColor, EnvironmentOverride, … })` (`ViewManager.cs:108`, `ViewDefinition.cs:13-53`), et `RenderTargetSurface.Texture` (`RenderTargetSurface.cs:45`) ;
    - exemples : `CasaEngine.Editor/Controls/MaterialPreviewViewport.cs`, `CasaEngine.Demos/Demos/RenderToTextureDemo.cs`.
  - **Polices bitmap BMFont `.fnt`** :
    - chargées par `BitmapFontLoader` et tenues par `game.UIFonts.Acquire(id)`, sous le nom de famille de la police (`UIFontRegistry.cs:36-110`, ADR-0036 de CasaEngine) ;
    - **à taille fixe** : `exactScale: 1` (`FontStashSharpTextEngine.cs:538-556`).
  - **Mise à l'échelle au rendu** : MGUI a `RenderTransform` avec une échelle `Vector2` non uniforme et une origine (`UIRenderTransform.cs:47, 78`), sans effet sur la mise en page.
- **Validation existante** :
  - `--smoke-frontend` ;
  - `--capture-ui-screens` (10 états dont `splash` et `car-selection`, back buffer forcé à 1920×1080) et `scripts/UiCaptureCompare` ;
  - sonde de taille de capture du scratchpad ;
  - `scripts/capture_editor_screen.ps1` ;
  - dernières références `references-32d0422`.

  Il n'existe aucun projet de test pour RGCE.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Quand la capture et le code du dépôt divergent, **la capture fait foi**. Les positions qui divergent sont recalées sur la capture, en unités 1024×640, et tout ce qui concorde reste aux formules du code (auteur, 2026-10-06). |
| D2 | **La palette de la capture** remplace l'actuelle, en course aussi (auteur, 2026-10-06, option choisie). |
| D3 | **Carrousel rendu par le pipeline de CasaEngine** : un monde de prévisualisation rendu dans une texture plein écran, affichée par MGUI au-dessus de la bande. Éclairage, normal maps, ombre sur le plateau et reflet chromé du plateau ; peinture par texture recalculée avec le masque alpha (auteur, 2026-10-06). |
| D4 | **Police bitmap GameFont** pour les textes de la sélection, convertie en police bitmap de CasaEngine (auteur, 2026-10-06). |
| D5 | **Sons de menu** de l'original (Highlight, ScreenClick, ScreenBack) sur ces deux écrans, par la voie de l'ADR-0003 (auteur, 2026-10-06). |
| D6 | Pas de pastille « www.XNARACINGGAME.com », pas de retour au titre après 60 s : non retenus par l'auteur (2026-10-06). |
| D7 | CasaEngine et MGUI ne sont pas modifiés ; un manque est documenté (`docs/mgui-gaps-from-rgce-xaml-screens.md`). Plan dans `ai-agent/tasks/` selon `ai-agent/plan-template.md`. |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **Branche** `remaster`, sans push. Le pointeur `CasaEngine` modifié dans l'arbre de travail n'est jamais indexé. |
| P2 | **Mise en page en pixels d'écran, calculée chaque image par un view model** avec les formules d'origine en repère 1024×640 (arrondi au pair, comme le menu principal, ADR-0007). Les textes GameFont sont dessinés à la taille native de la police, puis mis à l'échelle au rendu par `RenderTransform` (W/1400 en x, H/1050 en y, comme `TextureFont`). Si MGUI ne l'applique pas au texte, l'écart est mesuré et documenté. |
| P3 | **Écran titre**, recalé sur la capture (D1) :<br>- fond `Ui.Menu.Background` à 0,85 et logo qui rebondit, comme le menu principal ;<br>- bande (0, 350, 1024, 61) rgba(0,0,0,174) ;<br>- sprite Press START centré en (512, 380), hauteur 26.<br>En 1920×1080 : bande y 591 → 694, sprite (652, 619, 617×44).<br>Clignotement de l'original.<br>Entrées de l'original (clic n'importe où, Espace, Échap, Start à l'appui), **plus Entrée et A**, que le menu principal accepte déjà ; la touche qui ferme le titre n'active rien dans le menu (garde existante). Son `ScreenBack` à la sortie.<br>Le panneau de remplacement et le bouton Continue disparaissent ; le nom d'état `Splash` et l'asset `Screen.Splash` restent. |
| P4 | **Fond noir** : la couleur d'effacement des mondes hors course passe de `CornflowerBlue` au noir de l'original. Le haut de tous les menus devient (0,0,0) au lieu de (15,23,36), donc toutes les captures de menu changent légèrement. |
| P5 | **Sélection, partie 2D**, aux formules du code (concordantes avec la capture) :<br>- bande (0, 170, 1024, 390) ;<br>- en-tête ;<br>- six barres aux formules d'origine, calculées depuis les tableaux d'origine : « Max Speed » affiche 288, 275 et 242 mph, alors que le catalogue de RGCE dit 240 pour la voiture 3 ;<br>- cases `ColorSelection` teintées, la sélectionnée agrandie ;<br>- boutons A/B avec grossissement, contour orange et son au survol.<br>Les flèches suivent l'original (sprite, miroir, balancement), mais leur x vient de la capture, contre le plateau avant (P8).<br>Disparaissent : le nom, le résumé et la ligne « Handling » ; le catalogue des voitures reste inchangé pour la course.<br>Palette (D2) : médianes de l'intérieur des 11 cases de la capture, sans correction du post-effet, puisqu'il n'est pas reproduit. Valeurs relevées par la découverte : #FE5E00, #FF1D3A, #FF23B0, #B81DFC, #411CFF, #006FFE, #00BFD5, #00DF65, #2CDD03, #9CE900, #FFB704 ; elles sont remesurées en T3.1. |
| P6 | **Entrées de la sélection**, celles de l'original : gauche → (n+1) % 3, droite → (n+2) % 3, haut et bas pour la couleur, zones de clic, glisser sur les cases, A ou Espace pour valider, Échap, B ou Back pour revenir. **Entrée valide aussi**, comme dans le menu principal. Seuil du stick de 0,5 comme le menu principal, sans répétition. Les boutons ne prennent pas le focus de MGUI. Sons : `Highlight` à chaque changement et au survol de A/B, `ScreenClick` à la validation, `ScreenBack` au retour. |
| P7 | **Carrousel 3D (D3)** :<br>- **Monde et vue** : un monde de prévisualisation propre à l'écran, rendu dans une texture de la taille de la fenêtre, effacée en transparent, par une vue créée avec `ViewManager.CreateView` et retirée à la fermeture de l'écran ou au départ de la course.<br>- **Image** : une `Image` plein écran, placée après la bande et les sprites, avant les textes et les boutons A/B, sans capter la souris.<br>- **Objets** : 3 plateaux `CarSelectionPlate` et 3 voitures. Rotation propre à 1/3,9 rad/s ; carrousel à 5 rad/s par le plus court chemin.<br>- **Éclairage** : lumière directionnelle de l'original convertie en Y vers le haut. Ombres du moteur sur la vue, avec un PCF 4 points : l'ombre floue de l'original n'est pas reproductible.<br>- **Reflet** : sur le seul matériau du plateau, dans cette vue, avec `SkyCubeMap`. Les voitures sont rendues comme en course.<br>- **Les trois voitures prennent la même couleur**, comme le code : les voitures arrière de couleurs différentes de la capture ne sont pas reproductibles avec les textures du dépôt (gris et masque).<br>- **Nettoyage** : `CarSelectionPreviewRenderer` (BasicEffect) est supprimé. |
| P8 | **Calage 3D sur la capture** : rayon, centre du carrousel et caméra sont ajustés pour qu'à 1024×768 (4:3, l'aspect de la capture) le plateau avant et les voitures arrière tombent sur la capture (tolérances en T4.3). Aux autres aspects, le champ vertical est gardé, comme l'original : en 16:9, le carrousel garde sa part de la hauteur et paraît plus étroit. Les flèches restent contre le plateau avant, comme dans la capture : leur x suit le bord projeté du plateau. |
| P9 | **Peinture (D2, D3)** : `LegacyCarVisualFactory` peint la texture diffuse de la voiture en CPU, avec `rgb = lerp(rgb, couleur, alpha)` et l'alpha forcé à 255. La même texture sert à la course et au carrousel : **la couleur choisie apparaît désormais en course**, ce qui n'était pas le cas. Une seule texture peinte par voiture et par couleur, mise en cache ; une voiture n'est plus rechargée par couleur, seule la texture change. |
| P10 | **Automatisation de capture déterministe** : sous `--capture-ui-screens`, le clignotement du titre est figé visible, et la rotation des voitures et le balancement des flèches à une pose fixe. Les comparaisons entre deux runs restent ainsi à 0,00. |
| P11 | **ADR-0008**, « RacingGameCasaEngine's title screen and car selection reproduce the original XNA screens as captured ». Contenu :<br>- D1 à D5 et P2 à P9 ;<br>- l'ADR-0001 cède sa parité d'états visuels pour ces deux écrans ;<br>- les écarts connus.<br>Les nouveaux manques de MGUI vont dans `docs/mgui-gaps-from-rgce-xaml-screens.md`. |
| P12 | **Validation par tâche** :<br>- `dotnet build RacingGame.slnx` 0 erreur ;<br>- `--smoke-frontend` code 0 sans avertissement ;<br>- `--capture-ui-screens` contre les dernières références : seuls changent les états annoncés par la tâche ;<br>- mesures sur capture contre les formules, en 1920×1080 et en 1280×720 (sonde de taille) ;<br>- superposition sur les captures de l'auteur en 1024×768 pour ce qui est recalé ;<br>- éditeur, `capture_editor_screen.ps1` sur les deux `.uiscreen` ;<br>- réglages de l'auteur sauvegardés puis restaurés à l'identique ;<br>- 🧪 pour l'auteur : rendu, animation, entrées et sons. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (P1). Ne jamais committer sur `master`.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Ne jamais pousser.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Aucun projet de test n'existe pour RGCE.
- **Ne jamais modifier** `CasaEngine/` ni `CasaEngine/MGUI/` (D7).
- Si le code est écrit mais qu'une vérification visuelle ou manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** les modifications préexistantes de l'auteur ni celles d'autres sessions : `git add` fichier par fichier ; `.serena/`, `log.txt` et le pointeur `CasaEngine` ne sont jamais indexés.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- **Sondes** : uniquement dans le scratchpad, sur une copie ; jamais d'envoi de touches ni de mouvements de souris simulés. Un survol par la souris réelle peut fausser une capture : un écart localisé sous le curseur se confirme par un second run.
- **Réglages de l'auteur** (`%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\display-settings.json`, `front-end-options.json`) : sauvegardés avant les runs, restaurés à l'identique après (même SHA-1).
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
- `--smoke-frontend` et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]` ; deux runs successifs identiques (P10).
- Contre `references-32d0422` :
  - `splash` et `car-selection` changent ;
  - les autres menus changent seulement par le fond noir (P4), d'un écart uniforme et faible ;
  - les états de course changent seulement par la peinture de la voiture (P9).
- Mesures des rectangles contre les formules d'origine et le calage de la capture, en 1920×1080 et en 1280×720.
- Superposition à 1024×768 sur les deux captures de l'auteur : bande, sprite et logo du titre ; plateau avant, voitures arrière et flèches de la sélection.
- `scripts/capture_editor_screen.ps1` sur `Splash.uiscreen` et `CarSelection.uiscreen` : code 0, sans erreur.
- Passe de vérification indépendante sur le résultat final.
- Vérification manuelle par l'auteur :
  - rendu ;
  - clignotement, rebond, carrousel et flèches ;
  - entrées au clavier, à la manette et à la souris ;
  - sons ;
  - peinture en course.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan, index et ADR

- Objectif : versionner ce plan, l'indexer, consigner P11 en ADR-0008.
- Fichiers : ce plan, `ai-agent/README.md`, `docs/decisions/0008-rgce-title-and-car-selection-as-captured.md`, `docs/decisions/README.md`.
- Validation : fichiers présents, liens valides.
- Commit : `docs(racing-casa): plan the title screen and car selection with the original look`

> Validation (2026-10-06) : plan revu par un `plan-verifier` (READY), approuvé par l'auteur en mode AUTO ; ADR-0008 et index écrits.

## Phase 1 — Assets

### ✅ T1.1 — Textures et sprites de l'original

- Objectif : rendre disponibles, catalogués, les éléments d'art des deux écrans.
- Fichiers : `RacingGameCasaEngine/Content/Textures/{headers,ColorSelection,OptionsScreenWindows}.png` (copies de `RacingGame/Content/Textures`), `RacingGameCasaEngine/RacingGameCasaEngine.csproj` (exclusion du lien pour ces fichiers, comme `ingame.png`), `scripts/generate_rgce_ui_assets.py`, `RacingGameCasaEngine/Content/UI/Sprites/*`, `RacingGameCasaEngine/Content/AssetInfos.json`.
- Étapes :
  1. Copier les trois PNG et exclure leurs liens du csproj, pour qu'un seul fichier arrive à la sortie.
  2. Ajouter au générateur :
     - les textures `Ui.Title.HeadersTexture`, `Ui.CarSelection.ColorSelectionTexture` et `Ui.CarSelection.OptionsWindowsTexture` ;
     - les sprites `Ui.Title.PressStart` (2, 1, 631, 45), `Ui.CarSelection.Header` (0, 212, 512, 100), `Ui.CarSelection.Swatch` (0, 0, 64, 64) et `Ui.CarSelection.StatBar` (372, 297, 472, 6) ;
     - dans `buttons.png`, les sprites `Ui.CarSelection.Arrow` (874, 426, 53, 39) et `Ui.Button.Highlight` (424, 240, 212, 92).
  3. Lancer le générateur deux fois : la seconde exécution ne change rien.
- Validation :
  - build ;
  - fichiers identiques à la sortie ;
  - éditeur : les sprites s'ouvrent ;
  - `--smoke-frontend` code 0.
- Commit : `feat(racing-casa): catalogue the title and car selection art of the original`

> Validation (2026-10-06) :
> - 3 PNG copiés, liens du csproj exclus : un seul fichier par nom à la sortie, identique à la source (`cmp`) ;
> - générateur : 7 textures, 40 sprites ; seconde exécution sans changement ; catalogue à 71 entrées ;
> - chaque nouveau sprite remonte à une image existante et son rectangle tient dans l'image (contrôle par script) ;
> - `dotnet build RacingGame.slnx` 0 erreur ; `--smoke-frontend` code 0, journal sans avertissement ; réglages restaurés à l'identique ;
> - l'ouverture dans l'éditeur se vérifie en T2.1 et T3.1, quand les écrans utilisent ces sprites.

### ✅ T1.2 — Police GameFont

- Objectif : la police bitmap de l'original, utilisable en XAML par son nom de famille (D4).
- Fichiers : `scripts/generate_rgce_gamefont.py` (nouveau, bibliothèque standard), `RacingGameCasaEngine/Content/UI/Fonts/GameFont.{fnt,png}`, `AssetInfos.json`, code de démarrage de RGCE qui tient la police (`game.UIFonts.Acquire`).
- Étapes :
  1. Relire `TextureFont.cs` de `4f840a3^` (tableau des glyphes de `GameFont.png`, avance, hauteur de cellule 36).
  2. Écrire un `.fnt` BMFont texte avec ces glyphes, la page `GameFont.png`, et un nom de famille `GameFont`. Le script est déterministe.
  3. Cataloguer la page et la police, puis acquérir la police au démarrage pour toute la vie du jeu.
  4. Vérifier, par une sonde de capture, une ligne en `FontFamily="GameFont"` à la taille native, puis avec un `RenderTransform` d'échelle (1920/1400, 1080/1050). La largeur de « Max Speed: 288mph » est comparée à celle de l'original (213 unités de 1024, soit 399 px en 1920).
- Validation :
  - build ;
  - sonde mesurée ;
  - si l'échelle au rendu ne s'applique pas au texte : écart mesuré, noté en M16 de `docs/mgui-gaps-from-rgce-xaml-screens.md`, et la tâche reste ✅ avec le texte à taille native.
- Commit : `feat(racing-casa): add the original GameFont as a bitmap font`

> Validation (2026-10-06) :
> - `scripts/generate_rgce_gamefont.py` écrit `Content/UI/Fonts/GameFont.fnt` (95 glyphes de `TextureFont.CharRects`, lus en (x, y + 1, largeur, 36), avance = 4e champ) et copie `GameFont.png` ; seconde exécution sans changement.
> - La page est cataloguée avec des `\` (`UI\Fonts\GameFont.png`), comme le chargeur de CasaEngine la cherche (`BitmapFontDescriptor.CatalogFileNameNextTo`). La police (`Font.GameFont`, type `fnt`) est tenue par le jeu dès `LoadContentPrivate`, libérée à sa destruction.
> - Le décalage de 5 unités vers le haut de `TextureFont` (`SubRenderHeight`) est laissé à la mise en page des écrans (`yoffset` 0).
> - Sonde (copie du scratchpad, textes sur l'écran titre, capture en 1920×1080) :
>   - la police se charge (journal : `GameFont.fnt` puis `GameFont.png`) et s'affiche ;
>   - à la taille native, la boîte de « Max Speed: 288mph » fait 297 px de large ;
>   - avec un `RenderTransform` d'échelle (1920/1400, 1080/1050) et d'origine (0, 0), elle fait 405 px, la largeur de l'original (somme des avances `XToRes1400`, 405 px) ; hauteur de glyphe 37 px comme `YToRes1050(36)`.
>   - O2 réglé : l'échelle au rendu s'applique au texte.
> - `dotnet build RacingGame.slnx` 0 erreur, 0 avertissement.

### ✅ T1.3 — Sons de menu

- Objectif : `Highlight`, `ScreenClick` et `ScreenBack` jouables par l'`AudioService`, au volume des options (D5, ADR-0003).
- Fichiers : `RacingGameCasaEngine/Content/Audio/menu_{highlight,screenclick,screenback}.wav` (copies), leurs `.sound`, `AssetInfos.json`, et le point d'accès que RGCE utilise déjà pour Beep et Bleep (à relire : `RaceStartLight.cs:20-21`).
- Étapes :
  1. Relire comment Beep et Bleep sont catalogués et joués.
  2. Faire de même pour les trois sons. Le lien entre le nom de son (`Sound.cs:21-31`) et le fichier `menu_*.wav` se fait par le nom ; le projet XACT compilé n'est pas lisible.
  3. Ajouter un petit point d'accès commun aux écrans.
- Validation :
  - build ;
  - `--smoke-frontend` sans avertissement ;
  - éditeur : les trois `.sound` se prévisualisent ;
  - 🧪 écoute par l'auteur.
- Commit : `feat(racing-casa): add the original menu sounds`

> Validation (2026-10-06) :
> - Le lien entre son et fichier est lu dans le projet XACT, lisible en texte (`RacingGame/Content/Audio/RacingGame.xap`) : cue `Highlight` → son `menu_highlight`, `ScreenClick` → `menu_screenclick`, `ScreenBack` → `menu_screenback`.
> - Volumes du projet XACT (centièmes de dB) convertis en volume linéaire des `.sound` : −600 → 0,501, 0 → 1,0, −100 → 0,891. Beep et Bleep (ADR-0003) étaient restés à 1,0 alors que l'original les jouait à −12 dB : écart signalé, non corrigé ici.
> - `Content/Audio/menu_{highlight,screenclick,screenback}.{wav,sound}` et 6 entrées de catalogue (ids uuid5 du nom), bus `Sfx`.
> - `UI/MenuSounds.cs` : tenus par le jeu (`RacingGameCasaEngineGame.MenuSounds`) pour toute sa vie, joués par l'`AudioService` avec une trace « played / not played ».
> - `dotnet build RacingGame.slnx` 0 erreur, aucun avertissement dans RGCE ; `--smoke-frontend` code 0 sans avertissement, les trois `.sound` se chargent au démarrage ; réglages restaurés à l'identique.
> - Lecture effective vérifiée en T2.1, au premier écran qui joue un son. 🧪 écoute par l'auteur ; la prévisualisation dans l'éditeur demande une manipulation de l'interface, non faite.

## Phase 2 — Écran titre

### 🧪 T2.1 — Écran titre de la capture

- Objectif : P3 et P4.
- Fichiers :
  - `RacingGameCasaEngine/Content/UI/Screens/Splash/{Splash.xaml,Splash.uiscreen,Splash.design.json}` ;
  - `RacingGameCasaEngine.UI/ViewModels/RaceTitleViewModel.cs` (nouveau) ;
  - `RacingGameCasaEngine/Screens/SplashScreen.cs` ;
  - `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` (couleur d'effacement, P4) ;
  - `RacingGameCasaEngine/Bootstrap/UiScreenCaptureValidator.cs` (P10).
- Étapes :
  1. View model : bande, sprite et décoration (logo existant) en pixels d'écran, aux formules d'origine (`CalcRectangle`, `CalcRectangleCenteredWithGivenHeight`), plus la visibilité du clignotement.
  2. XAML : fond à 0,85, logo, bande, sprite ; plus de panneau ni de bouton.
  3. Écran : entrées de P3 (`KeyboardManager`, `GamePadManager`, `MouseManager`, comme `MainMenuScreen`), son `ScreenBack`, puis `OpenMainMenu`.
  4. Fond noir hors course.
  5. Pose figée sous capture.
- Validation :
  - build ;
  - `--smoke-frontend` ;
  - `--capture-ui-screens` : `splash` change, les autres menus par le seul fond ;
  - mesures en 1920×1080 et 1280×720 ;
  - superposition sur la capture à 1024×768 ;
  - éditeur ;
  - 🧪 entrées et son.
- Commit : `feat(racing-casa): draw the title screen like the original`

> Validation (2026-10-06) :
> - `RaceTitleViewModel` : décoration, bande (0, 350, 1024, 61) et sprite Press START par `CalcRectangleCenteredWithGivenHeight(512, 380, 26, …)`, clignotement `(int)(t / 0,375) % 3 != 0`, figé visible sous capture (`RaceFrontEndState.PinScreenAnimations`, posé par `UiScreenCaptureValidator`). `Splash.xaml` : fond à 0,85, logo, bande, sprite ; plus de panneau ni de bouton ; données de conception `Splash.design.json` (1280×720).
> - `SplashScreen` : clic gauche, Espace, Échap, Entrée, Start ou A (à l'appui) → son `ScreenBack` puis menu principal. Un clic ne déclenche pas de bouton du menu au relâchement : une commande MGUI exige l'appui sur le bouton (`MGButton.cs:194-219`).
> - Fond noir hors course (`RacingGameCasaEngineGame.ConfigureCurrentWorldSky`).
> - Mesures (`--capture-ui-screens`, et sonde de taille pour 1280×720 et 1024×768) :
>   - 1920×1080 : bande y 591 → 694, texte x 658 → 1261, y 625 → 655, identiques aux formules ;
>   - 1280×720 : bande y 394 → 463, texte x 441 → 838, y 418 → 437 (formules 441 → 839, 418 → 438, à 1 px d'antialiasing) ;
>   - 1024×768, ramené au repère de la capture de l'auteur : bords de bande 488 et 573 (capture 488,7 et 574,2), texte x → 841, y 517 (capture x 346 → 840, y 517 → 543) ; superposition dans le scratchpad (`title-overlay-1024x768.png`).
> - Écart visible avec la capture : son fond est flou et sépia (post-effet et scène 3D de l'original, hors périmètre) ; ici, le fond seul sur du noir. Press START reste jaune doré, comme le sprite d'origine.
> - Comparaison avec `references-32d0422` (run `ui-run-20261006-081420`) :
>   - `splash` change ;
>   - `highscores`, `options`, `help`, `car-selection` et `track-selection` ne diffèrent que du bleu d'effacement d'avant : chaque pixel diffère de k × (99,147,234), k de 0 à 0,49, aucun pixel hors de cette droite ;
>   - `main-menu` : idem par rapport au run de la correction du menu (`ui-run-20261006-070223`), les références datant d'avant elle ;
>   - états de course à 0,00.
> - `--smoke-frontend` code 0, sans avertissement ; build 0 erreur, aucun avertissement dans RGCE ; réglages restaurés à l'identique.
> - Éditeur : `capture_editor_screen.ps1` sur `Splash.uiscreen`, code 0, sans erreur ; l'écran s'affiche avec ses données de conception.
> - 🧪 Reste pour l'auteur : chaque entrée qui quitte le titre, le son `ScreenBack`, le clignotement.

## Phase 3 — Sélection de voiture, partie 2D

### 🧪 T3.1 — Mise en page, palette, entrées et sons

- Objectif : P5 et P6, avec l'aperçu actuel gardé provisoirement, sans cadre, en attendant la phase 4.
- Fichiers :
  - `RacingGameCasaEngine/Content/UI/Screens/CarSelection/{CarSelection.xaml,CarSelection.design.json}` ;
  - `RacingGameCasaEngine.UI/ViewModels/RaceCarSelectionViewModel.cs` et un view model de ligne si besoin ;
  - `RacingGameCasaEngine/Screens/CarSelectionScreen.cs` ;
  - `RacingGameCasaEngine/Bootstrap/RaceFrontEndCatalog.cs` (palette, valeurs d'origine des barres) ;
  - `RacingGameCasaEngine/UI/LegacyMenuUiTheme.cs` (retrait des restyles devenus inutiles).
- Étapes :
  1. Remesurer la palette (médiane de l'intérieur de chaque case de la capture) et l'écrire dans le catalogue.
  2. Calculer les six valeurs d'origine par voiture, dans le catalogue, avec les tableaux et les formules de `CarSelection.cs:19-53, 229-245`.
  3. View model en pixels d'écran :
     - bande, en-tête, barres, cases (teinte par liaison de `TextureColor` si MGUI l'accepte, sinon appliquée en code), flèches, boutons A/B ;
     - textes GameFont avec l'échelle de T1.2.
  4. Entrées et sons de P6 ; le stick et la garde de première image suivent `MainMenuScreen`.
  5. Pose figée sous capture.
- Validation :
  - build ;
  - `--smoke-frontend` ;
  - capture `car-selection` ;
  - rectangles mesurés contre les formules en 1920×1080 et 1280×720 ;
  - largeurs des barres des trois voitures (planche par sonde) ;
  - éditeur ;
  - 🧪 entrées et sons.
- Commit : `feat(racing-casa): lay the car selection out like the original`

> Validation (2026-10-06) :
> - **Palette** remesurée (médiane du milieu de chaque case de la capture) : #FE5E00, #FF1D3A, #FF22AB, #B41CFA, #421DFF, #006BFF, #00BCD2, #00DD68, #2CDE02, #9BE900, #FCB501, dans `RaceFrontEndCatalog.CarColors`.
> - **Barres** : `OriginalCarSelection.CreateBars` reprend les tableaux et formules d'origine en float. Mesuré en 1920×1080, une sonde choisissant la voiture :
>   - voiture 1 : 288 mph, barres de 384, 114, 313, 114, 129 et 141 px ;
>   - voiture 2 : 275 mph, barres de 341, 349, 399, 218, 32 et 300 px ;
>   - voiture 3 : 242 mph, barres de 234, 216, 238, 141, 231 et 341 px ;
>   - largeurs égales à `XToRes((int)(192·v))` au pixel près.
>
>   L'ancien libellé, les barres min-max, « Handling » et le résumé disparaissent de l'écran ; `CarDefinition` porte `SelectionBars` à la place de `Stats` et `SelectionStats`.
> - **Mise en page** (`RaceCarSelectionViewModel`, rectangles `RaceScreenRectViewModel`, lignes `RaceCarStatViewModel`, cases `RaceCarSwatchViewModel` teintées par liaison de `TextureColor`) :
>   - en 1920×1080 : bande y 287 → 945 ; intérieur de la case 3 x 758 → 822, y 851 → 909 ; barres à x 1436, y 370, 446, 521, 614, 707, 800 ; tout est égal aux formules ;
>   - texte « Max Speed » à l'encre y 321 : cellule de glyphe en 316 = `YToRes(190) − YToRes1050(5)`, encre à la ligne 5 de la cellule comme dans `GameFont.png` ;
>   - en 1280×720 et 1024×768 (sonde de taille) : bande, première barre et case 3 égales aux formules, à 1 px d'antialiasing près.
> - **Superposition** sur la capture de l'auteur en 1024×768 (`carsel-overlay-1024x768.png` du scratchpad) : en-tête, logo, six lignes et barres, cases, flèches et boutons A/B tombent dessus. Restent : la case sélectionnée (la capture montre la 2e), la pastille web (D6) et le carrousel (phase 4).
> - **Textes GameFont** : en 1024×768, MGUI coupait « Max Speed: 288mph » sur deux lignes (`WrapText` vient du thème), puis le rognait à la largeur restante de la fenêtre. D'où `WrapText="False"` et `ClipToBounds="False"` sur ces textes ; écart noté pour `docs/mgui-gaps-from-rgce-xaml-screens.md` en T5.1.
> - **Flèche gauche** : `RenderTransform Scale="-1,1"` la dessine en miroir (O3 réglé ; le rastériseur de MGUI ne cache aucune face, `CasaMonoGameRenderInterop.cs:10`).
> - **Entrées et sons** (`CarSelectionScreen`), comme l'original plus Entrée :
>   - gauche → (n+1) % 3, droite → (n+2) % 3 ; haut et bas pour la couleur ;
>   - zones de clic, bouton gauche maintenu sur une case ;
>   - A, Espace, Entrée ou clic sur A → `ScreenClick` puis piste ; Échap, B, Back ou clic sur B → `ScreenBack` puis menu ;
>   - `Highlight` à chaque changement et quand la souris entre dans une case ou un bouton A/B (`Input.MouseInBox` de l'original) ;
>   - garde de première image.
>
>   Le bouton A ou B survolé grandit de `XToRes(16)`×`YToRes(9)` et reçoit le contour orange.
> - **Aperçu provisoire** : l'ancien rendu d'une voiture, sans cadre, en attendant la phase 4. Il montre un rectangle sombre derrière la voiture : c'est l'alpha de la cible de rendu (O1).
> - **Comparaison** avec le run de T2.1 (`ui-run-20261006-081420`) : seul `car-selection` change ; les états de course restent à 0,00 malgré la nouvelle couleur 0, puisque le moteur ne rend pas la teinte (P9, T4.1).
> - `--smoke-frontend` code 0 sans avertissement ; build 0 erreur, aucun avertissement dans RGCE ; réglages restaurés à l'identique.
> - **Éditeur** : `capture_editor_screen.ps1` sur `CarSelection.uiscreen`, code 0 sans erreur, avec les données de conception régénérées pour 1280×720. Le cadrage à 100 % ne montre que le haut de l'écran ; la police des textes dans l'éditeur n'est pas vérifiée (police tenue par le jeu seulement).
> - 🧪 Reste pour l'auteur : chaque entrée, les sons, le survol des boutons A/B.

## Phase 4 — Carrousel 3D et peinture

### ✅ T4.1 — Peinture de la voiture

- Objectif : P9.
- Fichiers : `RacingGameCasaEngine/Components/LegacyCarVisualFactory.cs`, et ses appelants si la signature change (`LegacyCarVisualComponent.cs`, `CarSelectionPreviewRenderer.cs`).
- Étapes :
  1. Peindre la texture diffuse décodée (`LoadCarDiffuseTexture`, `:367-390`) pour chaque (voiture, couleur) demandée, avec l'alpha forcé à 255, et mettre ces textures en cache.
  2. Le modèle par voiture n'est plus dupliqué par couleur : seule la texture du matériau peint change.
  3. Retirer le recours à `TintColor` et `TintStrength`, que le moteur ne lit pas.
- Validation :
  - build ;
  - `--capture-ui-screens` : la voiture des états de course porte la couleur 0 de la nouvelle palette sur son masque ;
  - planche par sonde des 3 voitures × 3 couleurs ;
  - pas d'avertissement ;
  - temps de peinture d'une texture mesuré et noté.
- Commit : `feat(racing-casa): paint the car with the original colour mask`

> Validation (2026-10-06) :
> - `LegacyCarVisualFactory` : un modèle par voiture (et non plus par voiture et par couleur). Une `CarPaint` par voiture décode la texture une fois et en fait deux textures :
>   - la texture telle quelle, pour le caoutchouc et le chrome ;
>   - la texture peinte `lerp(rgb, couleur, alpha)`, pour le matériau de peinture (`lack`), repeinte sur place (`SetData`) quand la couleur change.
>
>   `TintColor`, `TintStrength` et `TintMaskFromBaseAlpha` ne sont plus remplis.
> - **Écart au plan, corrigé pendant la tâche** : P9 prévoyait l'alpha forcé à 255. Un premier essai l'a fait : la carrosserie entière devenait brillante en course, parce que le moteur multiplie le spéculaire par l'alpha (`Lighting.fxh:11`, `AddSpecular`). Le shader d'origine faisait de même (`NormalMapping.fx`, `bump * spec * specularColor * diffusePixel.a`). L'alpha, masque de peinture, est donc gardé. L'opacité dans la texture du carrousel (O1) se traitera en T4.2, par l'état de mélange des matériaux (`MaterialBase.BlendState`, lu par `RenderStateCache.cs:51`).
> - **Comparaison** avec le run de T3.1 (`ui-run-20261006-082824`) :
>   - seuls changent `race-hud` (1,18), `pause`, `race-finished` et `car-selection` ;
>   - en course, seuls les pixels des bandes changent, du noir à l'orange de la couleur 0 (luminance moyenne avant : 14,8) ; l'ombrage de la carrosserie est identique.
> - **Planche** (sonde choisissant voiture et couleur, capture de course, scratchpad `paint-sheet.png`) : voiture 1 en orange et en indigo (bandes et flammes peintes), voiture 2 en cyan, voiture 3 en vert citron (carrosserie entière, selon leur masque).
> - **Temps de peinture d'une texture 2048×2048** : 62 à 69 ms (trace « Car paint »), une fois par changement de couleur et par voiture. 3 voitures font environ 200 ms au changement de couleur dans le carrousel ; à surveiller en T4.2.
> - `--smoke-frontend` code 0 sans avertissement ; build 0 erreur, aucun avertissement dans RGCE ; réglages restaurés à l'identique.
> - Les captures PNG gardent l'alpha du back buffer (nul sur la carrosserie hors masque) : à convertir en RGB avant tout redimensionnement.

### 🧪 T4.2 — Carrousel dans une vue du moteur

- Objectif : P7.
- Fichiers : `RacingGameCasaEngine/UI/CarSelectionCarousel.cs` (nouveau), `CarSelectionScreen.cs`, `CarSelection.xaml`, suppression de `RacingGameCasaEngine/UI/CarSelectionPreviewRenderer.cs`.
- Étapes :
  1. **Essai d'abord** : une voiture dans une vue rendue dans une texture et affichée en plein écran. Contrôles :
     - l'alpha sur la bande : la voiture opaque, sans transparence ;
     - le champ de vision réappliqué après redimensionnement ;
     - la mise à jour manuelle du monde ;
     - le retrait de la vue au départ de la course.

     Si l'alpha n'est pas opaque, appliquer la correction la plus locale trouvée dans RGCE, par exemple un pipeline de vue enveloppant comme `LegacySkyCubeViewPipeline`. Sinon, ⚠️ Blocked.
  2. Monde : 3 plateaux et 3 voitures, lumière et ombres, reflet du plateau.
  3. Rotation propre et rotation du carrousel, branchées sur la sélection.
  4. Image plein écran placée selon P7, sans capter la souris ; texture reliée de nouveau quand la cible de rendu change.
  5. Libération à la fermeture.
- Validation :
  - build ;
  - `--smoke-frontend` (aller-retour, départ de course) sans avertissement ;
  - capture `car-selection` ;
  - deux runs identiques (P10) ;
  - planche des 3 voitures devant ;
  - mémoire et durée d'image notées avant et après l'ouverture de l'écran ;
  - 🧪 rendu.
- Commit : `feat(racing-casa): show the cars on a 3D carousel like the original`

> Validation (2026-10-06) :
> - **`UI/CarSelectionCarousel.cs`** : un monde à part, rendu par CasaEngine dans une cible de rendu de la taille de l'écran, montrée par une `Image` plein écran entre les sprites et les textes. Il contient :
>   - 3 plateaux `CarSelectionPlate.gltf`, chargés sans la correction de repère des décors puisqu'ils sont déjà en Y vers le haut ;
>   - 3 voitures de `LegacyCarVisualFactory` ;
>   - la lumière de l'original ;
>   - la caméra de l'original : champ vertical de 90°, proche 0,5, lointain 1750.
>
>   Le repère Z-haut de l'original devient Y-haut par (x, y, z) → (x, z, −y), une rotation : les nombres de l'original passent tels quels (caméra (0, 2,75, −10,45) vers (0, −1, 0), centre (1,5, 1, 0), rayon 5).
> - **Mouvement** : rotation propre à 1/3,9 rad/s. Le carrousel rejoint la voiture choisie à 5 rad/s par le plus court chemin (`InterpolateRotation`) et part, comme l'original, de la voiture 1 à chaque ouverture. Tout est figé sous capture (P10).
> - **Ombres** (option Ombres) : carte de 2048, boîte de 16 unités, seules les voitures projettent. **Reflet** : sur le seul matériau du plateau (`UseSceneReflectionCube`, apport 0,35, cube `SkyCubeMap` du jeu) ; la course ne change pas (O4).
> - **O1 réglé** : `OpaqueGeometryAlphaViewPipeline` enveloppe le pipeline de la vue. Après le rendu, un quad plein écran à la profondeur 0,99999 n'écrit que l'alpha (1), là où la profondeur est plus proche : la voiture et le plateau sont opaques sur la bande, le fond reste transparent. Un état de mélange ne pouvait pas le faire : l'alpha obtenu dépend toujours de l'alpha source ou destination.
> - **Cycle de vie**, après un premier essai qui plantait (« Collection was modified ») : `GameScreenManager.TransitionTo` parcourt les vues sans copie et pousse chaque écran dans l'interface de chaque vue. Une vue ne peut donc être ni ajoutée ni retirée par un écran. D'où :
>   - le jeu possède le carrousel (`RacingGameCasaEngineGame.CarSelectionCarousel`, créé au premier besoin) ;
>   - l'écran le demande à chaque mise à jour (`Request`) ;
>   - le jeu crée, active ou désactive la vue après `base.Update` (`UpdateView`), la recrée après un chargement de monde (qui vide les vues), et lui retire son interface MGUI.
> - **Peinture** : 3 voitures repeintes à chaque changement de couleur. Boucle parallèle et tampon partagé : 9 à 16 ms par voiture au lieu de 62 à 69 (trace « Car paint »).
> - `CarSelectionPreviewRenderer` (BasicEffect) supprimé.
> - **Mesures** :
>   - `--capture-ui-screens` deux fois de suite : 0,00 sur les 10 états, carrousel compris ;
>   - `--smoke-frontend` code 0 sans avertissement ; build 0 erreur, aucun avertissement dans RGCE ; réglages restaurés à l'identique ;
>   - sonde « retour de course puis sélection » : la vue est recréée, l'écran est identique à la première ouverture au rebond du logo près ;
>   - planche des 3 voitures devant (sonde voiture et couleur, `carousel-three.png`) : voiture 1 orange, voiture 2 violette, voiture 3 vert printemps, la case choisie agrandie ;
>   - éditeur : `capture_editor_screen.ps1` sur `CarSelection.uiscreen`, code 0, sans erreur ;
>   - mémoire du processus (sonde) : de l'aide à la sélection, mémoire de travail 312 → 411 Mio, mémoire privée 420 → 720 Mio (textures des voitures, cible de rendu, carte d'ombre). Durée d'image non mesurée (VSync et pas fixe la plafonnent à 60 images/s).
> - **Cadrage** : celui du code, carrousel décalé à gauche ; en 16:9, la flèche gauche passe derrière le plateau avant. Calage sur la capture en T4.3.
> - 🧪 Reste pour l'auteur : le rendu en mouvement (rotation, changement de voiture et de couleur), l'ombre, le reflet.

### 🧪 T4.3 — Calage sur la capture et flèches

- Objectif : P8.
- Fichiers : `CarSelectionCarousel.cs`, le view model de la sélection.
- Étapes :
  1. Capturer en 1024×768 (sonde de taille) et superposer avec la capture recalée.
  2. Ajuster le rayon, le centre et la caméra jusqu'aux tolérances :
     - bords du plateau avant et bas du plateau à ±10 unités ;
     - centres des voitures arrière à ±15 unités.
  3. Placer les flèches contre le bord projeté du plateau, avec le même recouvrement que dans la capture.
  4. Contrôler en 1920×1080 et 1280×720.
- Validation :
  - superpositions jointes à la note de la tâche ;
  - capture `car-selection` ;
  - 🧪 rendu par l'auteur.
- Commit : `fix(racing-casa): fit the car carousel to the original capture`

> Validation (2026-10-06) :
> - **Relevés sur la capture** (pixels de `5.webp`) : plateau avant, bord gauche 368, bord droit 845, bas 698 ; centres des voitures arrière (430, 359) et (762, 358).
> - **Calage** (`fit_carousel.py` du scratchpad) : projection de XNA (`CreateLookAt`, `CreatePerspectiveFieldOfView`), rendu en 1024×768 ramené au repère de la capture. La caméra d'origine est gardée ; seuls le centre et le rayon du carrousel sont ajustés.
>   - Résultat : centre (−0,2, −0,3, 2,78) et rayon 6,74, contre (1,5, 1, 0) et 5 dans le code.
>   - Écarts : plateau gauche +3,8, droit −4,0, bas +5,1 ; voiture arrière gauche +5,8 / −10,4 ; droite −4,9 / −9,4 pixels de capture (×0,85 en unités de 1024). Moyenne quadratique 6,7, dans les tolérances (±10 unités pour le plateau, ±15 pour les voitures arrière).
>   - Avec les nombres du code, le bord gauche du plateau tombait 274 pixels trop à gauche.
> - **Flèches** : sur la capture, elles sont centrées sur les bords gauche et droit du plateau avant. Le carrousel projette ces bords avec la caméra de sa vue (`FrontPlateEdges`), et le view model y centre les flèches, balancement compris. Les x de la capture (279 et 691) restent en secours tant que le carrousel n'est pas posé.
> - **Superposition** sur la capture en 1024×768 (`fit-overlay.png` du scratchpad) : plateau avant, plateaux et voitures arrière et flèches tombent en place. Restent : l'orientation des voitures (la capture est prise en pleine rotation, la capture automatique fige la rotation) et les couleurs des voitures arrière (P7).
> - **En 1920×1080 et 1280×720** (`fit-169.png`) : le champ vertical est gardé, le carrousel garde sa part de la hauteur et paraît plus étroit qu'en 4:3 ; les flèches restent sur les bords du plateau.
> - **Comparaison** avec le run de T4.2 : seul `car-selection` change. `--smoke-frontend` code 0 sans avertissement ; build 0 erreur, aucun avertissement dans RGCE ; réglages restaurés à l'identique.
> - 🧪 Reste pour l'auteur : le rendu, en mouvement.

## Phase 5 — Clôture

### 🧪 T5.1 — Validation globale, documentation et rapport de fin

- Objectif : validation globale, nouvelles références, `docs/mgui-gaps-from-rgce-xaml-screens.md` (manques rencontrés), passe de vérification indépendante, rapport de fin et index.
- Validation : liste « Validation globale ».
- Commit : `docs(racing-casa): close the title and car selection plan`

> Validation globale (2026-10-06) :
> - `dotnet build RacingGame.slnx --no-incremental` : 0 erreur ; aucun avertissement dans `RacingGameCasaEngine` ni `RacingGameCasaEngine.UI` (les 124 avertissements viennent des sous-modules, de `RacingGame`, `RacingGame.Shared` et `RacingGame.PipelineExtension`).
> - `--smoke-frontend` et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]` ; réglages de l'auteur restaurés à l'identique à chaque run.
> - Deux runs de capture successifs (`ui-run-20261006-090001`, `ui-run-20261006-090021`) : 0,00 sur les 10 états (P10).
> - Contre `references-32d0422` :
>   - `splash` (107/88/87) et `car-selection` (64/64/67) changent ;
>   - `main-menu`, `highscores`, `options`, `help` et `track-selection` ne diffèrent que du bleu d'effacement d'avant : k × (99,147,234), vérifié en T2.1 ;
>   - `race-hud` (1,18), `pause` et `race-finished` ne changent que par la peinture des bandes de la voiture (T4.1).
> - Nouvelles références : `references-c0fdd87` (copie de `ui-run-20261006-090021`).
> - Mesures et superpositions : notes de T2.1, T3.1 et T4.3 (1920×1080, 1280×720, 1024×768 sur les captures de l'auteur).
> - Éditeur : `capture_editor_screen.ps1` sur `Splash.uiscreen` et `CarSelection.uiscreen`, code 0, sans erreur.
> - Documentation :
>   - `docs/mgui-gaps-from-rgce-xaml-screens.md` : M4 à 40 sprites sur 7 textures, M16 et M17 (police bitmap à taille fixe, retour à la ligne et rognage des textes), C1 à C4 (vues et transitions d'écran, teinte jamais rendue, alpha écrit par les shaders, page de police cherchée avec des `\`) ;
>   - ADR-0009 « as delivered » remplace l'ADR-0008 (O5) ; les références du code pointent vers l'ADR-0009.
> - Passe de vérification indépendante : CONFIRMED sur les 9 points (disposition, chiffres, entrées lues dans le code, carrousel, peinture, sous-modules intacts, build et runs, ADR-0009 et manques). Trois remarques P4, reportées sans modifier le code validé : A1, les preuves de build et d'exécution viennent du sous-module CasaEngine à `dd91efe` (pointeur de l'arbre de travail, changé hors de ce chantier) et non au `d79ed16` enregistré, les lignes du moteur citées étant identiques aux deux ; A2, la case choisie, agrandie, chevauche ses voisines d'environ 2 unités (5 px en 1920) et `GetSwatchAt` rend la première case trouvée là où l'original laissait gagner la dernière, d'où quelques pixels où le bouton maintenu ne change pas de case comme dans l'original ; A3, Start ne quitte le titre qu'à l'appui, quand l'original prenait Start maintenu, sans effet visible.
>
> Rapport de fin :
> - **Écran titre** : fond et logo qui rebondit, bande à y 350, Press START qui clignote, au pixel près des formules ; sorties de l'original plus Entrée et A, son ScreenBack ; fond noir hors course.
> - **Sélection** :
>   - en-tête, six lignes aux chiffres et barres de l'original (288, 275, 242 mph) ;
>   - palette de la capture, cases teintées, la choisie agrandie ;
>   - flèches qui se balancent, centrées sur les bords du plateau ;
>   - A et B avec leur contour au survol ;
>   - textes en GameFont à l'échelle de l'original ;
>   - entrées et sons de l'original, plus Entrée.
> - **Carrousel** : trois voitures sur leurs plateaux, rendues par CasaEngine, qui tournent sur elles-mêmes, avec le carrousel qui amène la voiture choisie devant. Ombres (option Ombres), reflet des plateaux, cadrage calé sur la capture.
> - **Peinture** : la couleur choisie se voit enfin en course, sur le seul masque de peinture.
> - **Outils et assets** :
>   - `scripts/generate_rgce_gamefont.py` (police bitmap) ;
>   - générateur de sprites étendu ;
>   - trois sons de menu ;
>   - sondes de mesure dans le scratchpad, hors dépôt.
> - **Décisions** : ADR-0008, puis ADR-0009 telle que livrée.
> - **Écarts connus** (ADR-0009) :
>   - post-effet de l'original non reproduit : texte doré et non crème, fond non flou ;
>   - les trois voitures ont la même couleur ;
>   - ombre plus dure ;
>   - carrousel plus étroit en 16:9 qu'en 4:3 ;
>   - pastille web, retour au titre après 60 s, scène 3D derrière les menus et curseur de l'original absents (D6 et hors périmètre).
> - **Points ouverts pour l'auteur** :
>   - vérifications manuelles (tâches 🧪 T2.1, T3.1, T4.2, T4.3 et T5.1, plus l'écoute des sons de T1.3) : rendu en mouvement, chaque entrée, les sons ;
>   - Beep et Bleep restent à volume 1,0 alors que l'original les jouait à −12 dB (relevé en T1.3, hors périmètre) ;
>   - la police des textes GameFont dans l'aperçu de l'éditeur n'est pas vérifiée (la police n'est tenue que par le jeu) ;
>   - durée d'image du carrousel non mesurée.

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | **Réglé en T4.2** (passe d'alpha après le rendu de la vue). Alpha de la voiture dans une texture effacée en transparent : les shaders du moteur sortent l'alpha de la texture (le masque de peinture), alors que l'original forçait 1. La peinture P9 force l'alpha à 255 dans la texture ; l'essai de T4.2 confirme si cela suffit, verre compris. | T4.1, T4.2 |
| O2 | **Réglé en T1.2.** Mise à l'échelle des textes GameFont : le `RenderTransform` non uniforme s'applique au texte. | T1.2 |
| O3 | **Réglé en T3.1.** Miroir de la flèche gauche : le `RenderTransform` d'échelle −1 en x se dessine. | T3.1 |
| O4 | **Contourné en T4.2** : le reflet n'est activé que sur le matériau du plateau, propre au carrousel. Raison de la désactivation des reflets pour `NormalMapping.fx` (`RacingGameLegacyMaterialTuning.cs:94-99`, commits `6a4f898`, `8bbaae0`) non trouvée. Le reflet du plateau (P7) ne touche que la vue du carrousel ; en course, rien ne change. | T4.2 |
| O5 | **Réglé en T5.1** (ADR-0009). L'ADR-0008 et P9 disent la texture peinte « alpha forcé à 255 » ; la livraison garde l'alpha (T4.1). Une ADR ne se réécrit pas : une ADR « telle que livrée » remplacera l'ADR-0008 en T5.1, avec les autres écarts de la livraison. | T5.1 |

## Hors périmètre

- Le post-effet de l'original (teinte chaude, flou lumineux, rayures de `PostScreenMenu.fx`), comme pour le menu principal (ADR-0007) : sans lui, le texte Press START est jaune doré et non crème comme sur la capture.
- La scène 3D survolée derrière les menus (ciel, piste, voiture en replay) et le curseur de souris de l'original.
- La pastille web et le retour au titre après 60 s (D6).
- Les couleurs différentes des voitures arrière de la capture (P7).
- Les autres écrans (sélection de piste, options, aide, meilleurs scores, pause) et les sons du menu principal.
- Toute modification de CasaEngine ou de MGUI (D7).
