# Plan agent IA — Menu principal au look du jeu XNA d'origine (RacingGameCasaEngine)

Plan d'exécution de la demande de l'auteur du 2026-10-05, faite en comparant le menu principal du jeu XNA d'origine (capture de l'auteur) à celui de RacingGameCasaEngine (RGCE) :
- « la bande de bouton doit être plus basse et plus grande » ;
- « Les boutons doivent être dessinés différemment. MGUI a des dégradés. Dans l'image 1 on voit le dégradé vertical des boutons avec des bordures dégradées aussi » ;
- « On voit aussi le contour des boutons qui permet d'entourer le bouton sélectionné ».

Les décisions D1 → D6 ci-dessous ont été arbitrées avec l'auteur le 2026-10-05 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

Le menu principal de RGCE retrouve la mise en page et l'allure du menu du jeu XNA d'origine :
- la bande sombre est plus basse et plus haute, aux proportions d'origine, à toutes les résolutions ;
- les boutons ont la face en dégradé vertical, le rebord et le biseau de l'original, dessinés avec les brosses de MGUI ;
- le bouton sélectionné est entouré de l'anneau orange, agrandi, porte son libellé, et les autres sont atténués.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-05)

Découverte en lecture seule (workflow de 3 agents), faits porteurs revérifiés directement.

- **Branche** : `remaster`, HEAD `ed3c47a`. Une autre session a commité sur la même branche (`31f7f83` → `0c48fc3`, plan des réglages de l'auteur, ADR-0004). `.serena/` et `log.txt`, non suivis, sont à l'auteur.
- **Menu XNA d'origine** (`git show 4f840a3^:RacingGame.Shared/GameScreens/MainMenu.cs` et `…/Graphics/UIRenderer.cs`, `…/Graphics/BaseGame.cs`) :
  - repère de 1024×640 : `XToRes(x) = round(x·W/1024)`, `YToRes(y) = round(y·H/640)` (`BaseGame.cs:403-417`) ;
  - 5 boutons dans l'ordre Play, Highscores, Options, Help, Quit (`MainMenu.cs:12-19`). La cellule « Credits » de `buttons.png` (0, 240) n'est utilisée par aucun code ;
  - bande : `RenderBlackBar(280, 192)` (`MainMenu.cs:248`), texel `BlackBarGfxRect` (99, 999) tiré sur toute la largeur, teinte blanche à 0,85 (`UIRenderer.cs:584-590`). Le texel est rgba(0,0,0,205), donc la bande est noire à 0,683 d'opacité, de 43,75 % à 73,75 % de la hauteur ;
  - boutons carrés, de hauteur 108 unités non sélectionnés et 132 sélectionné, écart 14 en largeur (`MainMenu.cs:28-31`). La rangée est centrée en x ; le haut des boutons non sélectionnés est à `YToRes(316)`, et le sélectionné grandit autour du même centre (`MainMenu.cs:251-269`) ;
  - dessin (`MainMenu.cs:271-284`) :
    - le non sélectionné est tracé avec la teinte (192,192,192,192) : `buttons.png` est importé en alpha prémultiplié (`RacingGame/Content/Content.mgcb:1607`), donc environ 75 % de luminosité et d'opacité ;
    - le sélectionné est en blanc, avec par-dessus le sprite d'anneau `MenuButtonSelectionGfxRect` (636, 240, 212, 212) ;
    - le libellé du seul bouton sélectionné est un sprite de `buttons.png`, `YToRes(5)` sous le bouton, à la largeur du bouton et à la hauteur bouton × 24/212 ;
  - animation : la taille de chaque bouton passe de l'une à l'autre en 0,5 s (`MainMenu.cs:120-123`, `InterpolateRect` `:223-236`) ;
  - en 1920×1080 : bande y 472 → 796 (324), boutons 182 et 223, écart 26, haut des non sélectionnés y 533, rangée de x 433 à 1488 quand Play est sélectionné.
- **Art de `buttons.png`** (mesuré, `RacingGame/Content/Textures/buttons.png`) :
  - cellules de bouton de 212×212, partie visible de 1 à 210 ;
  - rebord plat gris 193 (#C1C1C1) de 10 px, rayon extérieur d'environ 38,5 px (18 %) ;
  - biseau intérieur de 10 px allant du sombre (87 en haut, 70 sur les côtés, 55 en bas, mesuré à x ou y = 106) à la couleur de la face ;
  - face en dégradé vertical #F8F8F8 → #A0A0A0, plate en horizontal ;
  - icône noire pure, centrée ;
  - anneau de sélection plat #FF9C00 de 10 px, même empreinte que le rebord ;
  - libellés blancs sur fond transparent : `MenuText*GfxRect` (`UIRenderer.cs:39-43`, « START RACE », « HIGHSCORES », « OPTIONS », « HELP », « QUIT »).
- **Menu actuel de RGCE** :
  - `Content/UI/Screens/MainMenu/MainMenu.xaml` :
    - bande `Border` de fond rgba(0,0,0,132), marge haute 315, hauteur 216, en pixels fixes : aucun `UseResponsiveLayout`. Ce sont les proportions d'origine en 1280×720, donc juste à cette seule résolution ;
    - 5 boutons de 105×105 en couleurs plates imbriquées, libellés sous chaque bouton ;
    - en 1920×1080, la bande couvre le bas du logo.
  - `Screens/MainMenuScreen.cs:61-70` : chaque image, `LegacyMenuUiTheme.ApplyMainMenuButtonState` restyle un bouton « actif » quand il a le focus ou est survolé (D6 du plan précédent).
  - Le focus n'est dessiné que hors du mode pointeur (`MGElement.cs:3969-3971`, `MGDesktop.cs:457`) : à l'ouverture, aucun bouton n'apparaît sélectionné.
  - Icônes : `Ui.Menu.Icon*` sont des découpes de 112×112 des cellules, face grise comprise (`scripts/generate_rgce_ui_assets.py:37-42`), et coupent les étoiles et les engrenages.
- **MGUI** (sous-module, à ne pas modifier) :
  - dégradé de remplissage à 4 coins, `MGGradientFillBrush`, y compris sur une forme arrondie (`MGGradientFillBrush.cs:63-91`). En XAML, 4 couleurs « H|H|B|B » donnent un dégradé vertical ; 2 couleurs donnent un dégradé diagonal (`XAML/Brushes.cs:118-144`) ;
  - pas de bordure arrondie en dégradé : un remplissage non plein sur une bordure arrondie retombe en bandes rectangulaires (`MGUniformBorderBrush.cs:103-107`) ;
  - bandes concentriques pleines possibles (`MGBandedBorderBrush`), avec épaisseur tronquée à l'entier (`MGBandedBorderBrush.cs:104-128` pour une forme arrondie) ;
  - pas de contour de focus ; un bouton peut recevoir en code n'importe quelle brosse de bordure (`MGButton.cs:38-53`).
- **Validation existante** :
  - `--smoke-frontend` ;
  - `--capture-ui-screens` (état `main-menu`, back buffer forcé à 1920×1080) et `scripts/UiCaptureCompare` ;
  - `scripts/capture_editor_screen.ps1` ;
  - dernières références `references-f94ed3e`.
- **Outils d'image du dépôt** : `scripts/UiCaptureCompare` et `scripts/LegacyModelGltfConverter` sont des consoles C# `net9.0-windows` avec `System.Drawing`, sans paquet ajouté. Les scripts Python du dépôt n'utilisent que la bibliothèque standard.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | 5 boutons, sans Crédits, comme le code d'origine (auteur, 2026-10-05). |
| D2 | Un seul libellé, sous le bouton sélectionné, comme l'original : « START RACE », « HIGHSCORES », « OPTIONS », « HELP », « QUIT » (auteur, 2026-10-05). |
| D3 | Sélection fidèle à l'original : le sélectionné grossit de 22 % en 0,5 s et porte l'anneau orange ; les autres sont atténués à 75 % ; la sélection est toujours visible, dès l'ouverture, au clavier, à la manette et au survol de la souris (auteur, 2026-10-05). |
| D4 | Les boutons sont dessinés avec les brosses de MGUI (dégradés), pas avec les images de boutons de `buttons.png` (demande de l'auteur). |
| D5 | CasaEngine et MGUI ne sont pas modifiés ; un manque de MGUI est documenté (`docs/mgui-gaps-from-rgce-xaml-screens.md`). |
| D6 | Plan dans `ai-agent/tasks/` selon `ai-agent/plan-template.md`. |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **Branche** `remaster`, sans push. `git log` est relu avant chaque commit. |
| P2 | **Mise en page** aux formules d'origine, en unités de conception 1920×1080, avec la mise à l'échelle responsive de MGUI sur la bande et la rangée seulement : le logo garde son calcul actuel en pixels de l'écran.<br>- Bande : y 472, hauteur 324, pleine largeur, rgba(0,0,0,174), soit l'opacité de 0,683 de l'original.<br>- Boutons de 182 et 223, écart 26, centre de la rangée à y 624.<br>- Libellé 8 sous le bouton sélectionné, hauteur bouton × 24/212.<br>- À d'autres rapports d'aspect que 16:9, l'échelle uniforme de MGUI remplace le mélange largeur/hauteur de l'original. |
| P3 | **Bouton dessiné avec MGUI**, toutes les cotes proportionnelles à la taille du bouton (base 210) :<br>- face en dégradé vertical #F8F8F8 → #A0A0A0 (`MGGradientFillBrush`, coins arrondis) ;<br>- rebord plat #C1C1C1 d'environ 4,8 % ;<br>- biseau intérieur approché par des bandes concentriques pleines, du sombre vers la face (`MGBandedBorderBrush`), puisque MGUI n'a pas de bordure arrondie en dégradé ;<br>- rayon extérieur d'environ 18 %.<br>Les icônes deviennent des glyphes noirs sur fond transparent, extraits de `buttons.png` par un petit outil C# versionné (`System.Drawing`, comme `UiCaptureCompare`, sans paquet). L'alpha de chaque pixel est retrouvé contre la couleur connue de la face sur sa ligne. Le générateur `scripts/generate_rgce_ui_assets.py` catalogue ces glyphes et les 5 sprites de libellé. |
| P4 | **Sélection gérée par l'écran** :<br>- sélectionné = Play à l'ouverture ;<br>- gauche/droite au clavier et à la manette, en boucle comme l'original ;<br>- la souris sélectionne au survol, une fois qu'elle a bougé ;<br>- clic, Entrée, Espace ou A activent le sélectionné.<br>Le sélectionné est dessiné sans dépendre de la règle de MGUI qui cache le focus en mode pointeur. L'anneau #FF9C00 remplace le rebord du sélectionné, appliqué en code chaque image comme aujourd'hui. L'opacité des autres est de 0,75, et la taille est animée linéairement en 0,5 s par le view model. |
| P5 | **ADR-0005**, « RacingGameCasaEngine's main menu reproduces the original XNA menu with MGUI brushes » : le menu principal prend pour référence l'original XNA et non plus le menu construit en code (parité D6 de l'ADR-0001). Les nouveaux manques de MGUI vont dans `docs/mgui-gaps-from-rgce-xaml-screens.md`. |
| P6 | **Validation par tâche** :<br>- `dotnet build RacingGame.slnx` 0 erreur ;<br>- `--smoke-frontend` code 0 sans avertissement ;<br>- `--capture-ui-screens` contre les dernières références : seul `main-menu` change ;<br>- mesures sur capture de la bande et des boutons contre les formules d'origine, en 1920×1080 et en 1280×720 (sonde de taille de capture, comme pour le HUD) ;<br>- planches avec chacun des 5 boutons sélectionné (sonde dans le scratchpad) ;<br>- éditeur, `capture_editor_screen.ps1` ;<br>- fichiers de réglages de l'auteur sauvegardés puis restaurés à l'identique ;<br>- 🧪 pour l'auteur : rendu, navigation au clavier, à la manette et à la souris, animation. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (P1). Ne jamais committer sur `master`.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- **Ne jamais pousser.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Aucun projet de test n'existe pour RGCE.
- **Ne jamais modifier** `CasaEngine/` ni `CasaEngine/MGUI/` (D5).
- Si le code est écrit mais qu'une vérification visuelle ou manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** les modifications préexistantes de l'auteur ni celles d'autres sessions : `git add` fichier par fichier ; `.serena/` et `log.txt` ne sont jamais indexés.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- **Sondes** : uniquement dans le scratchpad, sur une copie ; jamais d'envoi de touches ni de mouvements de souris simulés. Un survol par la souris réelle peut fausser une capture de menu : un écart localisé sous le curseur se confirme par un second run.
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
- `--smoke-frontend` et `--capture-ui-screens` : code 0, journaux sans `[Warning]` ni `[Error]`.
- `--capture-ui-screens` contre les dernières références : seul `main-menu` change ; les autres états à 0,00, hors survol de la souris.
- Mesures de la bande, des boutons, de l'anneau et du libellé contre les formules d'origine, en 1920×1080 et en 1280×720.
- `scripts/capture_editor_screen.ps1` sur `MainMenu.uiscreen` : code 0, sans erreur.
- Passe de vérification indépendante sur le résultat final.
- Vérification manuelle par l'auteur :
  - rendu ;
  - navigation au clavier, à la manette et à la souris ;
  - animation ;
  - activation de chaque bouton.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan, index et ADR

- Objectif : versionner ce plan, l'indexer, consigner P5 en ADR-0005.
- Fichiers : `ai-agent/tasks/rgce-main-menu-xna-look-tasks.md`, `ai-agent/README.md`, `docs/decisions/0005-rgce-main-menu-reproduces-the-xna-menu.md`, `docs/decisions/README.md`.
- Validation : fichiers présents, liens valides.
- Commit : `docs(racing-casa): plan the main menu with the original XNA look`
- Note de validation : plan relu par un `plan-verifier` (READY), approuvé par l'auteur avec P1 → P6, mode AUTO (2026-10-05). ADR-0005 écrite et indexée ; liens vérifiés.

## Phase 1 — Images

### ✅ T1.1 — Glyphes d'icônes transparents et libellés

- Objectif : P3, partie images ; libellés de D2.
- Fichiers :
  - `scripts/MenuIconExtractor/` (console C# `net9.0-windows`, `System.Drawing`, avec un `README.md` comme `UiCaptureCompare`) ;
  - l'image produite sous `RacingGameCasaEngine/Content/UI/Sprites/` ;
  - `scripts/generate_rgce_ui_assets.py` (texture et sprites des glyphes, 5 sprites `Ui.Menu.Label*` taillés dans `buttons.png`) ;
  - les `.texture` et `.sprite` générés, et `Content/AssetInfos.json`.
- Étapes :
  1. L'outil lit les 5 cellules de `buttons.png`, estime la couleur de la face ligne par ligne hors de l'icône, et écrit chaque glyphe en noir avec l'alpha retrouvé.
  2. Le générateur pointe les sprites d'icône sur cette image, et ajoute les libellés.
- Validation :
  - outil relancé : image identique au bit près ;
  - générateur relancé : aucun changement ;
  - planche des glyphes sur fond clair et sombre, sans halo gris ni bord coupé ;
  - build 0 erreur ;
  - `--smoke-frontend` sans avertissement (sprites résolus).
- Commit : `feat(racing-casa): extract the main menu icon glyphs and labels`

> Validation (2026-10-05) :
> - Outil `scripts/MenuIconExtractor` (console `net9.0-windows`, `System.Drawing`, sans paquet, README).
>   - L'image écrite, `Content/UI/Sprites/Ui.Menu.Glyphs.png` (1060×212), est identique au bit près d'un run à l'autre (SHA-1).
>   - Boîtes des glyphes (pixels couverts à moitié) égales aux mesures de la découverte : Play x 47..163 y 49..164 ; Highscores 34..182 / 33..172 ; Options 37..175 / 52..166 ; Help 70..143 / 59..156 ; Quit 60..150 / 60..156.
>   - Aucun pixel hors de la boîte élargie de 3 px : les traces des coins arrondis du biseau, vues au 1er essai, sont écartées.
>   - Planche sur dégradé clair et sur fond sombre : glyphes noirs nets, sans halo gris.
> - Générateur (`scripts/generate_rgce_ui_assets.py`) :
>   - texture `Ui.Menu.GlyphsTexture` avec son png, 5 sprites `Ui.Menu.Glyph*` (cadres de 212×212) et 5 sprites `Ui.Menu.Label*` (rectangles `MenuText*GfxRect`) ;
>   - 2e exécution sans changement (même SHA-1 du catalogue) ; 12 entrées ajoutées en fin de catalogue, aucune autre modifiée ;
>   - chaînes sprite → texture → png résolues ; planche des libellés « START RACE », « HIGHSCORES », « OPTIONS », « HELP », « QUIT » sur fond sombre, nette.
>   - Les anciens `Ui.Menu.Icon*` restent jusqu'à T2.1, qui passe le menu aux glyphes.
> - `dotnet build RacingGame.slnx` : 0 erreur, aucun avertissement RGCE ; les 12 fichiers sont copiés dans la sortie.
> - `--smoke-frontend` : code 0, catalogue de 64 assets, aucun avertissement ; réglages de l'auteur inchangés (même SHA-1).

## Phase 2 — Menu

### ✅ T2.1 — Bande et boutons en dégradés MGUI

- Objectif : P2 et P3, à l'état « aucun sélectionné » des formules (tous les boutons à 182) ou avec Play sélectionné sans animation, pour valider la mise en page.
- Fichiers : `Content/UI/Screens/MainMenu/MainMenu.xaml`, `MainMenu.design.json`, et si besoin `RacingGameCasaEngine.UI/ViewModels/RaceMainMenuViewModel.cs`.
- Validation :
  - build 0 erreur, smoke code 0 ;
  - capture en 1920×1080 et sonde en 1280×720 : bande et boutons mesurés contre P2 (±2 px) ;
  - planche comparée à la capture de l'original et au rendu des formules ;
  - éditeur sans erreur.
- Commit : `feat(racing-casa): draw the main menu band and buttons like the original`

> Validation (2026-10-05) :
> - Écarts au plan, avec leurs raisons :
>   - mise en page : `RaceMainMenuViewModel.Update` calcule chaque image, en pixels de l'écran, les formules de l'original (`CalcRectangle`, `CalcRectangleCenteredWithGivenHeight`, `XToRes`, `YToRes`, arrondi au pair), au lieu de la mise à l'échelle responsive de MGUI proposée en P2. Le résultat est exact à toute résolution, y compris le mélange largeur/hauteur de l'original en 16:10, et le logo garde son calcul ;
>   - l'anneau orange, l'opacité de 0,75 et le libellé sont déjà dessinés, liés au view model, Play étant sélectionné ; T2.2 rend la sélection dynamique.
> - Dessin (`MainMenu.xaml`), proportionnel à chaque bouton :
>   - face en dégradé vertical sur le fond du `Button` (255 → 147, que MGUI dessine à l'intérieur du rebord : environ 250 en haut de la face et 152 en bas, contre #F8F8F8 → #A0A0A0 dans l'art, soit 8 niveaux plus sombre en bas) ;
>   - rebord #C1C1C1 de 10/212 ;
>   - biseau en 4 bandes concentriques (`BandedBorderBrush`) de bordures ancrées pleines, une couleur par côté, mesurées dans l'art (plus sombres en bas) ;
>   - glyphe `Ui.Menu.Glyph*` étiré sur le bouton ;
>   - anneau #FF9C00 superposé au rebord du sélectionné ;
>   - libellé `Ui.Menu.Label*` sous le sélectionné.
>
>   Le `MainMenuScreen` ne restyle plus le menu ; `ApplyMainMenuButtonState` et ses brosses sont supprimés. Les anciens sprites `Ui.Menu.Icon*` sont retirés du générateur, du catalogue (59 entrées) et du contenu ; une 2e exécution du générateur ne change rien.
> - Correctif dans la tâche : sans `HorizontalContentAlignment`/`VerticalContentAlignment="Stretch"` sur le bouton, le `Border` du biseau, vide, restait centré à sa taille minimale et dessinait une pastille au centre de chaque bouton. Trouvé sur la 1re capture, confirmé par une sonde dans le dossier de build.
> - O1 : le biseau garde une couleur par côté (plus sombre en bas), en 4 marches de 2 px ; seul le dégradé continu de l'original manque (profil mesuré en 1920×1080 : 112, 158, 199, 231 en haut ; 88, 138, 172, 197 sur le côté ; face à 250).
> - `dotnet build RacingGame.slnx` : 0 erreur, 0 avertissement. `--smoke-frontend` et `--capture-ui-screens` : code 0, aucun avertissement.
> - Mesures contre les formules d'origine (`measure_menu.py`, sonde de taille) : bande et 5 boutons au pixel près, écart maximal 0 px, en 1920×1080 (bande 472..795, boutons 223 et 182 à x 433, 682, 890, 1098, 1306), en 1280×720 et en 1680×1050.
> - Captures contre le run fait après `ed3c47a` : seul `main-menu` change (32,65/31,30/29,95), les 9 autres états à 0,00.
> - Planche du bouton Play contre la cellule de `buttons.png` avec l'anneau, à la même taille : même anatomie ; coins sans anticrénelage dans MGUI.
> - Éditeur : `capture_editor_screen.ps1` sur `MainMenu.uiscreen` code 0, diagnostics sans erreur, aperçu avec les données de conception de 1280×720.
> - Réglages de l'auteur inchangés (même SHA-1).

### 🧪 T2.2 — Sélection, anneau, animation et libellé

- Objectif : D2, D3 et P4.
- Fichiers : `Screens/MainMenuScreen.cs`, `RacingGameCasaEngine.UI/ViewModels/RaceMainMenuViewModel.cs`, `Bootstrap/RaceFrontEndState.cs`, `Bootstrap/RaceFrontEndFlow.cs` (`ApplyMainMenuButtonState` est déjà retiré en T2.1).
- Validation :
  - build 0 erreur, smoke code 0 ;
  - capture : Play sélectionné dès l'ouverture, avec anneau, taille de 223, libellé « START RACE », autres à 0,75 ;
  - sonde : une planche par bouton sélectionné ;
  - mesures de l'anneau et du libellé ;
  - 🧪 navigation et animation par l'auteur.
- Commit : `feat(racing-casa): select main menu buttons like the original`

> Validation (2026-10-05) :
> - Code : `MainMenuScreen` gère la sélection comme le `MainMenu.Update` d'origine.
>   - Gauche/droite au clavier, au D-pad ou au stick gauche, en boucle ; un pas à l'appui, puis, la direction tenue, un 1er pas répété au bout d'environ 0,5 s et un tous les 250 ms ensuite, comme l'original. Le seuil du stick est de 0,5, alors que l'original utilisait 0,75 (`git show 4f840a3^:RacingGame.Shared/GameLogic/Input.cs:732, 745`) : écart relevé par la vérification de T3.1, reporté.
>   - La souris sélectionne au survol, une fois qu'elle a bougé depuis le dernier pas au clavier ou à la manette. Les deltas sont testés en valeur absolue : le `MouseManager.HasMoved` de CasaEngine ne compte que les mouvements vers la droite ou le bas.
>   - Espace, Entrée ou A activent le sélectionné, un clic le bouton cliqué.
>   - Les boutons ne prennent plus le focus de MGUI, pour que sa navigation aux flèches ne s'ajoute pas.
>   - La 1re image après l'ouverture ignore l'activation, la touche qui a ouvert le menu pouvant être encore « juste pressée ».
>   - Écart au plan (P4, D3 : « Play à l'ouverture ») : la sélection est gardée en mémoire dans `RaceFrontEndState.SelectedMainMenuButton`, sans être enregistrée. Play est sélectionné à la 1re ouverture, et le menu rouvre ensuite sur le dernier bouton, comme l'original, dont le menu restait sous les autres écrans. Consigné dans l'ADR-0006.
>   - `RaceMainMenuViewModel.Reset` ouvre le menu sur ce bouton déjà agrandi ; `Update` anime les tailles à 2 par seconde (0,5 s).
> - `dotnet build RacingGame.slnx` : 0 erreur, 0 avertissement. `--smoke-frontend` et `--capture-ui-screens` : code 0, aucun avertissement.
> - Captures contre le run de T2.1 : 0,00 sur les 10 états, Play sélectionné à l'ouverture comme avant.
> - Sonde (sélection initiale lue dans une variable d'environnement, copie dans le scratchpad) :
>   - menu ouvert sur chacun des boutons 1 à 4 ;
>   - mesures contre les formules d'origine : écart maximal 0 px pour les 5 états en 1920×1080, et pour Options en 1280×720 ;
>   - planche : anneau, agrandissement et libellé du bon bouton (START RACE, HIGHSCORES, OPTIONS, HELP, QUIT), les autres atténués.
> - Réglages de l'auteur inchangés.
> - 🧪 Reste, faute d'entrée simulée permise : navigation au clavier, à la manette et à la souris, répétition, animation et activation, par l'auteur.

## Phase 3 — Clôture

### 🧪 T3.1 — Validation globale, documentation et rapport de fin

> Validation (2026-10-05) :
> - Références de T2.2 : `references-32d0422` (run de T2.2, identique à celui de T2.1 au pixel près).
> - **Build** : `dotnet build RacingGame.slnx --no-incremental` sans avertissement dans les projets RGCE ; 0 erreur.
> - **Jeu** : `--smoke-frontend`, `--verify-legacy-import-profile`, `--capture-track-audit` et `--capture-ui-screens` donnent le code 0, sans `[Warning]` ni `[Error]`.
> - **Captures** : contre `references-32d0422`, `main-menu` et tous les états à 0,00, hors les écarts de survol de la souris réelle (`help` 0,18, `track-selection` 0,03, curseur au centre de l'écran).
> - **Correctif trouvé par la validation globale** : avec le curseur réel sur le bouton Options, MGUI l'éclaircissait d'environ 5 à 10 niveaux au survol, ce que l'original ne faisait pas. Les boutons ont maintenant une `BackgroundFocusedColor` transparente ; capture suivante à 0,00, curseur au même endroit. MGUI assombrit encore de 6 % un bouton tenu enfoncé.
> - **Sous-module** : aucun chemin `CasaEngine/` dans les commits du chantier ; `git -C CasaEngine status --porcelain` vide.
> - **Réglages de l'auteur** inchangés (même SHA-1).
> - **Doc** : `docs/mgui-gaps-from-rgce-xaml-screens.md` (M1 et M2 mis à jour, nouveaux M13, M14, M15, comptes de M4) ; ADR-0006 remplace ADR-0005 (Superseded).
> - **Vérification indépendante** (workflow de 3 angles) :
>   - pixels : CONFIRMED ; géométrie exacte (0 px) sur 9 captures, couleur de la bande, anneau, glyphes (IoU 0,96-0,99) et libellés conformes ;
>   - code et comportement : CONFIRMED ; formules, animation, répétition, règle de la souris conformes au `MainMenu.Update` d'origine ;
>   - doc : REFUTED par un P2, corrigé :
>     - l'ADR-0005 disait le biseau uniforme ;
>     - P3/P4 corrigés : réouverture sur le dernier bouton non consignée, menu d'avant « juste en 1920×1080 » (c'était 1280×720), dégradé décrit comme couvrant tout le bouton, M4 périmé, citations décalées, O1 périmé.
>   - Passe de clôture de la doc sur l'état final (relecteur neuf) : **CONFIRMED**, les 13 points corrigés sans nouvelle affirmation fausse. Ses deux remarques (A1 : l'ADR-0006 ne relevait pas l'erreur « 1920×1080 » de l'ADR-0005 ; A2 : le chiffre de survol) sont corrigées.
> - P3/P4 reportés (règle : un résultat confirmé n'est pas modifié pour eux) :
>   - glyphes des boutons non sélectionnés gris foncé (≈ 35-41) au lieu de presque noirs (≈ 8-13), l'opacité de 0,75 s'appliquant à chaque couche ;
>   - liseré clair de 1 px entre le rebord et le biseau des boutons non sélectionnés (le rebord du bouton est dessiné 1 px plus fin que son épaisseur) ;
>   - bas de la face 8 niveaux plus sombre que l'art ;
>   - seuil du stick 0,5 au lieu de 0,75 ;
>   - clic au relâchement au lieu de l'appui ;
>   - bords arrondis sans anticrénelage ;
>   - empreinte de 1 px plus large que la partie visible du sprite d'origine (marge transparente de 1 px).
> - 🧪 Reste la vérification manuelle par l'auteur (liste « Validation globale »).
>
> Rapport de fin :
> - **Menu** : bande et boutons aux formules du menu XNA, au pixel près à toute résolution. Boutons dessinés avec les brosses de MGUI : face en dégradé, rebord, biseau par côté, glyphe extrait de l'art, anneau orange. Sélection fidèle : agrandissement en 0,5 s, autres atténués, libellé sous le sélectionné, clavier, manette et survol.
> - **Outils** : `scripts/MenuIconExtractor` (glyphes transparents, déterministe) ; générateur d'assets étendu (glyphes, libellés) ; anciens `Ui.Menu.Icon*` retirés.
> - **Décisions** : ADR-0006 (remplace ADR-0005).
> - **Points ouverts pour l'auteur** :
>   - les P3/P4 reportés ci-dessus ;
>   - comportements d'origine hors périmètre (Échap/Retour, retour au splash, son) ;
>   - `MouseManager.HasMoved` de CasaEngine ne compte que les mouvements vers la droite ou le bas : tâche séparée proposée. ✅ Réglé le 2026-10-06 : corrigé dans CasaEngine (`5882bb72c`, mergé dans `main` par `d79ed169b`), sous-module mis à jour, et `MainMenuScreen` utilise `HasMoved` à la place des deltas en valeur absolue.

> Retour de l'auteur (2026-10-06) : corriger trois des écarts reportés (icônes grises des boutons non sélectionnés, bas de la face plus sombre que l'art, biseau en 4 marches).
> - **Glyphes** : le glyphe sort de la couche atténuée et prend sa propre opacité, 0,93 pour un bouton non sélectionné. L'encre d'origine vaut 0,247 × U (U de 31 à 62 sur la bande, 44 en moyenne) ; ici, la face à 0,75 donne environ 165 au milieu, d'où 1 − 0,247 × 44 / 165 = 0,93. Encre mesurée : 10 (8 à 15 dans l'original), contre 35 à 41 avant.
> - **Face** : dégradé 255 → 153, que MGUI dessine à l'intérieur du rebord ; il suit la droite de l'art. Mesuré en 1920×1080 : 249 en haut de la face et 159 en bas, contre 152 avant.
> - **Biseau** : `RaceMainMenuButtonViewModel.BevelBrush` construit un `BandedBorderBrush` d'une bande de 1 px par pixel d'épaisseur, chacune une bordure ancrée avec la couleur de l'art interpolée par côté. Mesuré en 1920×1080 : haut 87, 109, 134 … 246 (art 87 … 246), côté 70 … 202 (identique), bas 55 … 157 (identique), puis la face.
> - Validation :
>   - build sans avertissement dans RGCE ;
>   - `--smoke-frontend` et `--capture-ui-screens` : code 0, aucun avertissement ;
>   - captures contre `references-32d0422` : seul `main-menu` change (0,46) ;
>   - géométrie toujours au pixel près ;
>   - éditeur sans erreur ;
>   - réglages de l'auteur inchangés.
> - Décision : ADR-0007 (remplace ADR-0006) ; M13 et M14 mis à jour.

- Objectif :
  - validation globale ;
  - nouvelles références ;
  - `docs/mgui-gaps-from-rgce-xaml-screens.md` (manques rencontrés) ;
  - passe de vérification indépendante ;
  - rapport de fin et index.
- Commit : `docs(racing-casa): close the main menu plan`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | **Résolu en T2.1.** Le biseau d'origine n'est pas uniforme : 87 en haut, 70 sur les côtés, 55 en bas. Les bandes concentriques gardent une couleur par côté (bordures ancrées pleines) ; seul le dégradé continu manque, remplacé par 4 marches (M13, M14). Depuis le 2026-10-06, une bande par pixel donne un dégradé continu (ADR-0007). | T2.1 |

## Hors périmètre

- Bouton et écran Crédits (D1).
- La teinte chaude et le flou lumineux du shader de post-traitement du menu d'origine (`PostScreenMenu.fx`) : l'option Post Screen Effects reste sans effet dans RGCE (D14 du plan des écrans XAML).
- Les autres écrans de menu et leurs boutons.
- Les autres comportements du `MainMenu.Update` d'origine : Échap ou Retour pour quitter, retour au splash après 60 s d'inactivité, son « Highlight » à chaque pas. Le menu de RGCE ne les avait pas avant ce chantier.
- Toute modification de CasaEngine ou de MGUI (D5).
- Push, merge, PR.
