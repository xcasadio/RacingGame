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

### ⏳ T3.1 — Mise en page, palette, entrées et sons

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

## Phase 4 — Carrousel 3D et peinture

### ⏳ T4.1 — Peinture de la voiture

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

### ⏳ T4.2 — Carrousel dans une vue du moteur

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

### ⏳ T4.3 — Calage sur la capture et flèches

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

## Phase 5 — Clôture

### ⏳ T5.1 — Validation globale, documentation et rapport de fin

- Objectif : validation globale, nouvelles références, `docs/mgui-gaps-from-rgce-xaml-screens.md` (manques rencontrés), passe de vérification indépendante, rapport de fin et index.
- Validation : liste « Validation globale ».
- Commit : `docs(racing-casa): close the title and car selection plan`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Alpha de la voiture dans une texture effacée en transparent : les shaders du moteur sortent l'alpha de la texture (le masque de peinture), alors que l'original forçait 1. La peinture P9 force l'alpha à 255 dans la texture ; l'essai de T4.2 confirme si cela suffit, verre compris. | T4.1, T4.2 |
| O2 | **Réglé en T1.2.** Mise à l'échelle des textes GameFont : le `RenderTransform` non uniforme s'applique au texte. | T1.2 |
| O3 | Miroir de la flèche gauche : `RenderTransform` d'échelle −1 en x. Si MGUI ne dessine pas une échelle négative, une image miroir est générée par un outil versionné, comme `MenuIconExtractor`. | T3.1 |
| O4 | Raison de la désactivation des reflets pour `NormalMapping.fx` (`RacingGameLegacyMaterialTuning.cs:94-99`, commits `6a4f898`, `8bbaae0`) non trouvée. Le reflet du plateau (P7) ne touche que la vue du carrousel ; en course, rien ne change. | T4.2 |

## Hors périmètre

- Le post-effet de l'original (teinte chaude, flou lumineux, rayures de `PostScreenMenu.fx`), comme pour le menu principal (ADR-0007) : sans lui, le texte Press START est jaune doré et non crème comme sur la capture.
- La scène 3D survolée derrière les menus (ciel, piste, voiture en replay) et le curseur de souris de l'original.
- La pastille web et le retour au titre après 60 s (D6).
- Les couleurs différentes des voitures arrière de la capture (P7).
- Les autres écrans (sélection de piste, options, aide, meilleurs scores, pause) et les sons du menu principal.
- Toute modification de CasaEngine ou de MGUI (D7).
