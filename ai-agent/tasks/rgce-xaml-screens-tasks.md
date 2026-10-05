# Plan agent IA — Écrans MGUI de RacingGameCasaEngine en assets `.uiscreen` liés à des view models

Plan d'exécution du chantier « modifier les écrans MGUI pour qu'ils utilisent un fichier XAML » (demande de l'auteur, 2026-10-05).
Les décisions D1 → D17 ci-dessous ont été arbitrées avec l'auteur le 2026-10-05 : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

> **Quand écrire un plan** : dès que le travail demande plus d'un commit. En dessous, exécution directe avec le rapport de fin de tâche.
> **Avant d'écrire le plan** : poser toutes les questions en une seule fois ; ne rien inventer, ne rien supposer.
> **Après approbation** : exécution autonome, tâche par tâche ; arrêt uniquement sur ⚠️ Blocked.

## Objectif

Les 9 écrans MGUI de `RacingGameCasaEngine` (RGCE) ne sont plus construits en C#.
- Chaque écran devient un asset CasaEngine `.uiscreen` avec son `.xaml`, catalogué dans `Content/AssetInfos.json` et chargé par un écran dérivé de `XamlUIScreenBase`.
- Les valeurs affichées passent par des view models liés en `{dataBinding:MGBinding}`.
- Les images d'atlas deviennent des assets `.sprite`, générés par un script versionné.
- Le rendu et le comportement restent ceux d'aujourd'hui, états de survol et de focus compris.
- Les manques de MGUI rencontrés sont consignés dans un rapport destiné à faire évoluer MGUI.

Ce que le chantier ne livre pas est dans « Hors périmètre ».

## État vérifié du dépôt (2026-10-05)

- Branche `remaster`, HEAD `53dba60` ; arbre propre hormis `.serena/` et `log.txt`, non suivis, à l'auteur, jamais indexés (`git status --short`).
- 9 écrans, tous construits en C# :
  - 8 écrans enregistrés par `RaceFrontEndFlow.RegisterFactories` (`RacingGameCasaEngine/Bootstrap/RaceFrontEndFlow.cs:54-65`) : Splash, MainMenu, CarSelection, TrackSelection, Options, Help, Highscores, RaceHud ;
  - `PauseScreen`, poussé par `RaceRuntimeUiCoordinator` (`Bootstrap/RaceRuntimeUiCoordinator.cs:28-66`) ;
  - base commune `Screens/RaceFrontEndScreenBase.cs` (UIScreenBase, 3 fenêtres : fond, logo, premier plan) ;
  - fabriques `UI/LegacyMenuUiTheme.cs`, `UI/RaceUiTheme.cs` ; rectangles d'atlas `UI/LegacyMenuUiAtlas.cs` ; rendu 3D de l'aperçu voiture `UI/CarSelectionPreviewRenderer.cs` ;
  - 2 634 lignes dans `Screens/` et `UI/` (`wc -l`).
- Aucun fichier `.xaml`, `.uiscreen`, `.sprite` ni `.texture` dans RGCE (`fd`).
- `Content/AssetInfos.json` : 5 entrées. `MenuBackground` (`Textures/background.png`) et `MenuButtons` (`Textures/buttons.png`) sont de type `png` ; les 3 autres sont de type `track`. `ingame.png` (HUD) n'est pas catalogué : il est chargé par `Texture2D.FromFile` (`Bootstrap/RacingGameCasaEngineGame.cs:195-210`).
- `RacingGameCasaEngine.csproj:27-55` : aucune règle de copie pour `.xaml`, `.uiscreen`, `.sprite`, `.texture`. Les PNG d'atlas sont liés depuis `..\RacingGame\Content\Textures\*.*`.
- `RacingGameCasaEngine/Assets/README.md` réserve `Content/UI/` aux assets MGUI ; les noms d'assets doivent rester stables et uniques.
- Moteur CasaEngine (sous-module, ADR-0035 et ADR-0038 dans `CasaEngine/docs/decisions/`) :
  - `XamlUIScreenBase(AssetContentManager, nomOuId)` (`CasaEngine/CasaEngine/Framework/UI/XamlUIScreenBase.cs:70`) charge le `.uiscreen` en mode Strict ; racine `<Window>` obligatoire ;
  - `OnWindowLoaded(MGWindow)` est abstrait ; `FindControl<T>(nom)` ; `Dispose()` retire les bindings et libère l'enveloppe ; `ScreenStack` n'appelle jamais `Dispose` (`ScreenStack.cs:79-107`) ;
  - modèle de bout en bout : RPGDemo, `CasaEngine/Projects/CasaEngine.RPGDemo/Scripts/Screens/TitleScreen.cs`.
- MGUI (sous-module) :
  - pas d'attribut d'événement en XAML : seul `Button.CommandName` existe, sinon `AddCommandHandler` en code ;
  - pas de rectangle source sur `Image` : `SourceName` ne résout que des assets `sprite`/`anim2d` (`CasaUIAssetProvider.cs:129-157`) ;
  - chemins de binding = propriétés publiques pointées, sans indexeur ;
  - l'épaisseur de bordure n'est pas une cible d'état visuel ; le focus d'un élément ne restyle ni ses enfants ni un voisin (`MGElement.cs:3971-3984`) ;
  - `ViewModelBase` existe dans `MGUI.Shared.Helpers`.
- Sonde jetable (scratchpad `xamlspike/`, dépôt intact, 2026-10-05) sur une copie de RGCE :
  - un `.uiscreen` catalogué (`Screen.ProbeHud`), chargé par nom en mode Strict, sans avertissement ;
  - chaîne `png` → `.texture` → `.sprite` résolue avec les bons rectangles (aiguille 347,0,28,186 ; chiffres) ;
  - view model `ViewModelBase` mis à jour à chaque image :
    - `TextBlock.Text` lié ;
    - `Image.SourceName` lié : le sprite du chiffre change ;
    - `RenderTransform.Rotation` lié par `TargetPathOverride=RenderTransform.Rotation` : rotation visible à l'écran autour de `Origin="0.5,0.93"` ;
  - fenêtre plein écran par `ScreenHorizontalAlignment/ScreenVerticalAlignment="Stretch"`.
- Validateurs RGCE (`--smoke-frontend`, `--capture-track-audit`, …) : ils ne touchent aucun membre d'écran ni élément MGUI. Ils n'exigent que les noms d'état, `RaceFrontEndFlow.*ForAutomation` et un `RaceFrontEndState` partagé et modifiable (`Bootstrap/FrontEndNavigationSmokeValidator.cs:77-197`). Aucun ne clique sur un bouton.
- Aucun projet de test pour RGCE dans `RacingGame.slnx`.

## Décisions verrouillées

| Réf | Décision |
|---|---|
| D1 | Périmètre : les 9 écrans de RGCE seulement (Splash, MainMenu, CarSelection, TrackSelection, Options, Help, Highscores, RaceHud, Pause). Le jeu legacy (`RacingGame`, `RacingGame.Shared`) ne change pas (auteur, 2026-10-05). |
| D2 | Mécanisme : assets CasaEngine `.uiscreen` + `.xaml` catalogués dans `Content/AssetInfos.json`, écrans dérivés de `XamlUIScreenBase` acquis par `AssetContentManager` (convention ADR-0038) (auteur, 2026-10-05). |
| D3 | Valeurs affichées liées à des view models par `{dataBinding:MGBinding}` (ADR-0038), non poussées dans les éléments par le code (auteur, 2026-10-05). |
| D4 | HUD de course entièrement en XAML : emplacements `Image` liés à des noms de sprites pour les chiffres, aiguille liée en rotation, textes liés ; plus de dessin libre `OnEndingDraw` (auteur, 2026-10-05 ; faisabilité prouvée par la sonde). |
| D5 | Images d'atlas : assets `.sprite` (et leurs `.texture`) générés par un script versionné dans `scripts/`, nommés en XAML par `Image.SourceName` (auteur, 2026-10-05). |
| D6 | États visuels survol/focus/sélection : parité exacte gardée en code (restylage à chaque image des éléments trouvés par nom), et rapport des manques de MGUI pour le faire évoluer (auteur, 2026-10-05). |
| D7 | Commits sur `remaster`, pas de nouvelle branche ; jamais de push (auteur, 2026-10-05). |
| D8 | CasaEngine et MGUI (sous-modules) ne sont pas modifiés ; tout manque est consigné dans le rapport (D6), jamais contourné dans le sous-module (règle conservée du chantier précédent, `docs/racinggame-casaengine-update-tasks.md`, « Hors périmètre »). |
| D9 | Plan dans `ai-agent/tasks/` selon le modèle `ai-agent/plan-template.md` (skill `plan` de l'auteur). |
| D10 | Ex-O1 corrigé : le panneau de fin de course du HUD affiche toutes les lignes, soit une ligne par tour terminé puis « Rank ». Sur Expert (4 tours), le rang n'est plus perdu (`RaceHudScreen.cs:164-195`) (auteur, 2026-10-05). |
| D11 | Ex-O2, « Shadows » : l'option pilote les ombres en course, par `ShadowSettings.Enabled` du monde de course et `CastShadows` de la lumière principale. L'option étant cochée par défaut, le rendu par défaut de la course change (auteur, 2026-10-05). |
| D12 | Ex-O2, « High Detail » : retirée de l'écran Options, de `RaceFrontEndState` et de la sauvegarde ; une clé `EnableHighDetail` présente dans un ancien fichier est ignorée (auteur, 2026-10-05). |
| D13 | Ex-O2, « Gamepad Vibration » : portage de la règle legacy (`RacingGame.Shared/GameLogic/CarPhysics.cs:313-319, 1003-1016, 1206-1262`), vibration de la manette du joueur à chaque contact avec une glissière.<br>- Choc rasant : intensité 0,35 pendant 0,25 s ; choc frontal : intensité 0,85 pendant 0,40 s.<br>- Classement legacy : rasant si l'angle entre la droite de la voiture et la normale de la glissière est inférieur à 45°. Dans RGCE, `ImpactStrength` = \|avant · normale\| (`ArcadeCarMovementComponent.cs:362-379`, `ArcadeVehicleDynamicsSolver.cs:354-371`), donc rasant si `ImpactStrength` < √2/2.<br>- Une vibration en cours est prolongée si la nouvelle est plus forte, et coupée à expiration, en pause, en fin de course et au retour au menu.<br>- Uniquement si l'option est cochée (auteur, 2026-10-05). |
| D14 | Ex-O2, « Post Screen Effects » : option gardée, sans effet (CasaEngine n'offre que fondu et teinte, `ScreenEffectService`) ; son absence d'effet est documentée dans le rapport (auteur, 2026-10-05). |
| D15 | Ex-O3 accepté : le HUD XAML peut rendre ses textes un peu différemment de `DrawShadowedText`, à condition d'un rapport chiffré de ces différences (captures, écarts) (auteur, 2026-10-05). |
| D16 | Ex-P14 : créer le projet CasaEngine complet de RGCE pour que l'éditeur CasaEngine ouvre son contenu et prévisualise et édite les écrans, avec données de conception (auteur, 2026-10-05). |
| D17 | Exécution en mode AUTO : toutes les tâches enchaînées sans demander, arrêt seulement sur ⚠️ Blocked ; jamais de push (auteur, 2026-10-05). |

## Points à valider (propositions de l'agent)

| Réf | Proposition |
|---|---|
| P1 | **View models** : une classe par écran dans RGCE (`RacingGameCasaEngine/UI/ViewModels/`), dérivée de `MGUI.Shared.Helpers.ViewModelBase`, setters gardés par comparaison (règle ADR-0038 : notifier seulement ce qui change). `RaceFrontEndState` reste un objet simple partagé (les validateurs le modifient directement) ; chaque écran rafraîchit son view model depuis l'état ou la session dans `Update`. Options : les liaisons à double sens écrivent dans le view model, qui écrit immédiatement dans `RaceFrontEndState` (même sémantique qu'aujourd'hui) ; l'application des réglages reste au bouton Back. |
| P2 | **Actions** : modèle RPGDemo, `FindControl<MGButton>(nom).AddCommandHandler(...)` dans `OnWindowLoaded`, qui appelle les délégués actuels ; pas de `CommandName` (enregistrement au niveau du bureau partagé, doublons interdits). Le focus initial reste un `Focus()` en code dans `Show()`, comme aujourd'hui. |
| P3 | **Base commune** `RaceXamlScreenBase : XamlUIScreenBase` (`Screens/`) : appel de `Desktop.LoadDefaultResources()` avant le chargement si la texture `CheckMark_64x64` manque (comme `RaceUiTheme.cs:27-32`), `Dispose()` dans `Hide()` (écrans à usage unique, recréés à chaque transition), affectation de `WindowDataContext`. |
| P4 | **Décor des menus** (fond et logo) : fenêtre unique par écran (`XamlUIScreenBase` n'a qu'une fenêtre), fond et logo en `Image` non cliquables au fond de l'`OverlayPanel`. Rectangle du logo (référence 1024×640, proportionnel à la fenêtre) et rebond (formule actuelle) calculés à chaque image par un `MenuDecorationViewModel` partagé et liés à `Margin`/`Width`/`Height` du logo, pour garder la parité. |
| P5 | **Assets** : script Python `scripts/generate_rgce_ui_assets.py`, bibliothèque standard seulement (`scripts/` contient déjà des scripts Python sans dépendance). Il contient la table des rectangles actuels (`LegacyMenuUiAtlas.cs`, `RaceHudScreen.cs:30-50`). Il écrit `Content/UI/Sprites/*.sprite`, 3 `.texture` (fond, boutons, HUD) et leurs entrées de catalogue, dont `ingame.png`, avec des GUID stables (uuid5). Noms préfixés `Ui.` pour éviter les noms de textures par défaut de MGUI. Écrans dans `Content/UI/Screens/<Nom>/<Nom>.uiscreen` + `.xaml`, nommés `Screen.<Nom>`, entrées de catalogue écrites à la main. Règle csproj `None Update="Content\UI\**\*.*"` en `PreserveNewest`.<br>**Contrat du script sur `Content/AssetInfos.json`** :<br>- il ne possède que les entrées dont le nom commence par `Ui.` ;<br>- il met à jour une entrée existante en place et ajoute les nouvelles à la fin, dans l'ordre de sa table ;<br>- il laisse toutes les autres entrées (les 5 actuelles, les `Screen.*`) inchangées et à leur place ;<br>- il écrit le fichier dans son format actuel : indentation de 2 espaces, ordre des clés `id`, `name`, `file_name`, `asset_type`, fins de ligne CRLF, pas de saut de ligne final ;<br>- l'option `--catalog <chemin>` permet de le lancer sur une copie. |
| P6 | **Mise en page dépendante de la résolution** (Options, bouton Back « responsive ») : valeurs calculées par le view model à partir de `Root.Metrics`, comme aujourd'hui, et liées (`Margin`, `Width`, `Height`, `Padding`). Fenêtres en `Stretch` : elles suivent désormais un redimensionnement, alors qu'aujourd'hui elles sont figées à la construction (amélioration, à confirmer). |
| P7 | **Répétitions** (5 icônes, 6 sections d'aide, 3 onglets, 10 lignes de scores, 5 résolutions, 2 modes, 3 pistes, 11 couleurs, 4 statistiques, 5 temps, 4 lignes de fin, chiffres) : emplacements fixes en XAML liés à des propriétés plates ou imbriquées (`Track0.Name`), faute d'indexeur et de liaison de collection éprouvée. Données issues du catalogue (`RaceFrontEndCatalog`) liées, pas recopiées en XAML ; textes d'interface fixes en littéraux XAML. |
| P8 | **Aperçu 3D de la voiture** : `CarSelectionPreviewRenderer` reste en code (rendu GPU à chaque image) ; sa cible de rendu est exposée en `MGTextureData` par le view model et liée à `Image.Source` ; le renderer est libéré par le `Dispose` de l'écran (il ne l'est jamais aujourd'hui). |
| P9 | **HUD** (D4) : chiffres en emplacements `Image` dont `SourceName`, `Visibility` et `Width` viennent du view model ; le view model reproduit l'ajustement de largeur actuel (`DrawBigNumber`). Temps et noms en `TextBlock` gras ombrés. Aiguille liée par `TargetPathOverride=RenderTransform.Rotation` (degrés : −133,5 + v × 143,2). Mise à jour par image depuis `RuntimeRaceSession`, notification seulement si la valeur change. La saisie qui ferme l'écran de fin reste en code. |
| P10 | **Validation par écran** :<br>- build ; `--smoke-frontend` code 0, journal sans `[Warning]` ni `[Error]` (une image non résolue y laisse un avertissement) ;<br>- comparaison de captures, outillage versionné en T0.2 :<br>  - **mode `--capture-ui-screens` de RGCE**, sur le modèle de `--capture-track-audit` (`Bootstrap/TrackMigrationCaptureValidator.cs`). Il parcourt 10 états : Splash, MainMenu, Highscores, Options, Help, CarSelection, TrackSelection, RaceHud en course, Pause et RaceHud en fin de course. Il passe par les points d'entrée existants (`RaceFrontEndFlow.*ForAutomation`, `RaceGameMode.TogglePause`, `CompleteRaceForAutomation`) et attend un nombre fixe d'images après chaque transition. Il capture le back buffer par `CaptureScreenshotWithStem("ui-<état>")` (`RacingGameCasaEngineGame.cs:421`), qui écrit dans `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots`. Il range ensuite les captures d'un run dans un dossier propre, `ui-run-<horodatage>`, avec un fichier `ui-<état>.png` par état : c'est le dossier « après » de la comparaison (T0.2) ;<br>  - **outil `scripts/UiCaptureCompare`**, console C# `net9.0-windows` utilisant `System.Drawing` (même cadre que `scripts/LegacyModelGltfConverter`, aucun paquet ajouté). Il compare chaque capture `ui-<état>` d'un dossier « après » à celle d'un dossier « références ». Il refuse deux images de tailles différentes et produit une planche côte à côte par état ;<br>  - **mesure** : écart absolu moyen par canal R, G, B (sRVB 8 bits, 0-255) sur toute l'image ; un « niveau » vaut 1 sur cette échelle ;<br>  - **critère** : écart ≤ 2,0 sur chaque canal, et contrôle visuel de la planche (même mise en page, mêmes textes, mêmes images, mêmes états). Les zones animées (rebond du logo, rotation de l'aperçu 3D, chronomètre) sont jugées à l'œil et notées ;<br>- **références durables** : prises en T0.2 depuis le commit qui ajoute l'outillage, avant tout changement d'écran ; le SHA est noté sous T0.2. Régénérables dans n'importe quelle session : `git worktree add <dossier> <SHA>`, `git submodule update --init --recursive`, build, `--capture-ui-screens`. Le validateur impose un back buffer fenêtré de 1920×1080 sans l'enregistrer : les runs ne dépendent pas des réglages d'affichage persistés ;<br>- aucun projet de test créé (aucun n'existe pour RGCE ; en ajouter un serait une nouvelle dépendance). |
| P11 | **Rapport MGUI** (D6) : `docs/mgui-gaps-from-rgce-xaml-screens.md`, en anglais. Chaque manque rencontré y figure avec preuve et effet sur RGCE : épaisseur de bordure hors états visuels, focus non propagé, absence d'événements, pas de rectangle source, rotation absente des correspondances de binding, collections, `LoadDefaultResources`, libération des écrans, `UIResponsiveSettings` code seulement, etc. Chaque manque y est relié aux audits existants du moteur quand ils existent. |
| P12 | **ADR** : la décision « écrans RGCE = assets `.uiscreen` liés à des view models » est consignée dans `docs/decisions/` avec le skill `adr` (le dossier n'existe pas encore). |
| P13 | **Nettoyage** : une fois les 9 écrans migrés, suppression du code devenu mort : `RaceFrontEndScreenBase`, fabriques de `LegacyMenuUiTheme` et `RaceUiTheme`, `LegacyMenuUiAtlas` (rectangles repris par le script), propriétés et handles `MenuBackgroundTexture`/`MenuButtonsTexture` si plus utilisés. Les fonctions d'état visuel gardées par D6 restent. |
| P14 | **Éditeur CasaEngine** : tranché par D16 (projet complet, phase 5). |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (D7). Ne jamais committer sur `master`.
- **Une seule tâche à la fois.** Avant de commencer une tâche, remplacer son icône `⏳` par `🚧`. À la fin, lancer la validation indiquée, remplacer l'icône par `✅`, `🧪` ou `⚠️`, ajouter une courte note de validation sous la tâche, puis **créer un commit dédié** qui inclut la mise à jour de ce fichier.
- **Un commit par tâche**, atomique et compilable, message en anglais au format `type(area): summary`, terminé par `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Le message suggéré est donné dans chaque tâche.
- **Ne jamais pousser.**
- **Ne rien inventer** : toute API, tout fichier, toute règle utilisée existe dans le dépôt, vient d'une réponse de l'auteur, ou d'une doc officielle citée (URL). Sinon : passer la tâche en ⚠️ Blocked, écrire la question dans « Points ouverts », et **s'arrêter**.
- **Build obligatoire** avant ✅ dès que du code est touché : `dotnet build RacingGame.slnx` (0 erreur). Aucun projet de test n'existe pour RGCE (P10).
- **Ne jamais modifier** `CasaEngine/` ni `CasaEngine/MGUI/` (D8).
- Si le code est écrit mais qu'une vérification visuelle ou manuelle manque, utiliser `🧪 Needs testing` et noter précisément ce qui manque.
- **Ne jamais laisser une tâche en 🚧** à la fin d'une session.
- **Ne jamais indexer** les modifications préexistantes de l'auteur : `git add` fichier par fichier, jamais `git add -A` ni `git add .` ; `.serena/` et `log.txt` ne sont jamais indexés.
- **Langue** : ce plan en français ; code, messages de commit, `docs/` et ADR en anglais.
- **Sondes** : uniquement dans le scratchpad, sur une copie ; jamais d'envoi de touches simulées. L'outillage dont dépend une validation est versionné (T0.2), jamais laissé dans le scratchpad.
- **Budget** : au plus 3 cycles « correction → build ou validation » par tâche. Un 4e échec passe la tâche en ⚠️ Blocked, avec l'erreur et la question dans « Points ouverts », puis arrêt.
- **Retour arrière** :
  - avant le commit d'une tâche : supprimer les seuls fichiers créés par la tâche et `git restore` des seuls fichiers suivis qu'elle a modifiés ;
  - après le commit : `git revert <SHA de la tâche>` ;
  - jamais de `git checkout`, `git reset --hard`, `git clean` ni `git stash` ; jamais toucher `.serena/` ni `log.txt`.

## Légende des statuts

- ⏳ Todo : pas encore commencé.
- 🚧 In progress : en cours de modification locale.
- 🧪 Needs testing : code écrit, validation incomplète ou en attente.
- ✅ Done : code validé, build/tests OK, commit effectué.
- ⚠️ Blocked : bloqué par une erreur non résolue ou une décision manquante.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur.
- `RacingGameCasaEngine.exe --smoke-frontend`, `--verify-legacy-import-profile` et `--capture-track-audit` : code 0, journaux sans `[Warning]` ni `[Error]`.
- Aucune construction d'arbre MGUI restante dans `Screens/` (garde `rg -n "new MG(Window|StackPanel|Button|TextBlock|Border|Image|OverlayPanel|ScrollViewer)" RacingGameCasaEngine/Screens RacingGameCasaEngine/UI` vide, hors exceptions notées).
- `--capture-ui-screens` puis `scripts/UiCaptureCompare` contre les références de T0.2 : 10 états, écart ≤ 2,0 par canal et planches contrôlées (P10).
- Passe `verifier` indépendante sur le résultat final.
- Vérification manuelle par l'auteur :
  - clics sur tous les boutons ;
  - navigation au clavier et à la manette ;
  - saisie du nom du joueur ;
  - curseurs ;
  - pause et reprise ;
  - fin de course et retour au menu.

---

## Phase 0 — Préparation

### ✅ T0.1 — Plan, modèle, index et ADR

> Validation (2026-10-05) : plan approuvé par l'auteur (D10 → D17 ajoutées à l'approbation) ; `ai-agent/plan-template.md`, `ai-agent/README.md`, `docs/decisions/README.md`, `docs/decisions/template.md` et `docs/decisions/0001-rgce-screens-as-casaengine-screen-assets.md` présents ; liens de l'index vérifiés. Pas d'index de documentation dans `docs/` à compléter.

- Objectif : versionner le plan et consigner la décision d'architecture.
- Fichiers :
  - `ai-agent/plan-template.md` (copie du modèle du skill) ;
  - `ai-agent/tasks/rgce-xaml-screens-tasks.md` ;
  - `ai-agent/README.md` (index) ;
  - `docs/decisions/` (créé par le skill `adr` : index, modèle, ADR « RGCE screens are CasaEngine .uiscreen assets bound to view models »).
- Étapes :
  1. Créer l'index `ai-agent/README.md` (fichier, sujet, reste à faire).
  2. Écrire l'ADR avec le skill `adr` : décisions D1 à D6, sources ADR-0035 et ADR-0038 de CasaEngine.
- Validation : fichiers présents ; liens de l'index valides.
- Commit : `docs(racing-casa): plan XAML screen assets and record the decision`

### ✅ T0.2 — Outillage de capture et de comparaison, références

> Validation (2026-10-05) : `dotnet build RacingGame.slnx` 0 erreur ; `scripts/UiCaptureCompare` 0 erreur. `--capture-ui-screens` : code 0, 10 captures en 20 s dans `ui-run-20261005-161221`, journal sans `[Warning]` ni `[Error]`, back buffer 1674×1150 (réglages d'affichage courants). 2e run du même build (`ui-run-20261005-161307`) comparé au 1er : écart 0,00 sur 9 états et 0,01 sur `race-hud` (rendu déterministe). Copie du 1er run avec un 2e `ui-pause-….png` : code 1, « duplicate state 'pause' ». `--smoke-frontend` : code 0, aucun avertissement. Références : le 1er run, capturé sur l'arbre de travail dont le contenu est celui de ce commit, est renommé `references-<SHA court>` dans `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots` ; le SHA est ajouté ici dans le commit de T1.1.
>
> Correctif (2026-10-05) : le 1er run de T1.2 sortait en 1280×720 contre 1674×1150 pour les références, et l'outil a refusé la comparaison. Cause : chaque `--smoke-frontend` bascule et enregistre la résolution (1280×720 ↔ 1920×1080, `FrontEndNavigationSmokeValidator.cs:95-103`). `--capture-ui-screens` applique donc désormais 1920×1080 fenêtré par `ApplyDisplaySettings(..., persistToProjectSettings: false)`, sans rien enregistrer, et attend cette taille avant la 1re capture. Les références sont régénérées depuis le commit du correctif, dont les écrans sont ceux d'origine ; son SHA est noté ici dans le commit de T1.2.
>
> Références en vigueur : commit `579e1d7`, régénérées par `git archive 579e1d7 RacingGameCasaEngine` dans un dossier temporaire (références de projet `CasaEngine` et `RacingGame` du csproj rendues absolues vers ce dépôt, le sous-module CasaEngine étant inchangé depuis `3ed325f`), build, `--capture-ui-screens`. Dossier `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots\references-579e1d7`, 10 captures en 1920×1080.
>
> Anciennes références (avant correctif, caduques) : commit `50fb3b0`, dossier `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots\references-50fb3b0` (10 captures, 1674×1150).

- Objectif : outillage versionné et références durables pour la comparaison avant/après (P10), avant tout changement d'écran.
- Fichiers :
  - `RacingGameCasaEngine/Bootstrap/UiScreenCaptureValidator.cs` ;
  - `RacingGameCasaEngine/Program.cs` et `Bootstrap/RaceLaunchOptions.cs` (option `--capture-ui-screens`, sur le modèle de `--capture-track-audit`) ;
  - `RacingGameCasaEngine/Bootstrap/RacingGameCasaEngineGame.cs` (création du validateur, comme les autres) ;
  - `scripts/UiCaptureCompare/` (console C#, `System.Drawing`) ;
  - `scripts/UiCaptureCompare/README.md`.
- Étapes :
  1. Validateur : parcourt les 10 états (P10). Il attend un nombre fixe d'images et capture chaque état par `CaptureScreenshotWithStem("ui-<état>")`, qui renvoie le chemin du fichier horodaté (`RacingGameCasaEngineGame.cs:421-445`). Il déplace ce fichier vers un dossier propre au run, `Screenshots\ui-run-<yyyyMMdd-HHmmss>\ui-<état>.png`, sans horodatage. Il journalise ce dossier et écrit le code de sortie 0 avec le message « UI screen capture completed (10 screenshot(s)) in <dossier> », ou 1 avec l'état fautif. Le dossier du run est l'argument « après » de toute comparaison.
  2. Outil : `UiCaptureCompare <références> <après> <sortie>`. Chaque dossier doit contenir exactement un fichier `ui-<état>.png` pour chacun des 10 états. Il sort en code 1 :
     - s'il manque un état ;
     - s'il trouve plusieurs fichiers `ui-<état>*.png` pour un même état ;
     - si deux tailles diffèrent.

     Sinon, il calcule l'écart moyen par canal et écrit un rapport texte et une planche par état.
  3. Commit, puis références : `--capture-ui-screens` sur ce commit ; le dossier `ui-run-…` produit est renommé `references-<SHA court>` dans `%LOCALAPPDATA%\CasaEngine\RacingGameCasaEngine\Screenshots`.
  4. Noter sous cette tâche le dossier, la taille du back buffer et la commande de régénération : `git worktree add <dossier> <SHA>`, puis `git submodule update --init --recursive` dans le worktree, build, `--capture-ui-screens`. Un commit ne pouvant contenir son propre SHA, celui du commit `test(racing-casa): capture and compare UI screens` est ajouté ici dans le commit de T1.1.
- Validation :
  - build 0 erreur ;
  - `--capture-ui-screens` code 0, 10 captures dans un dossier `ui-run-…` ;
  - `UiCaptureCompare` sur une copie du dossier de références à laquelle on ajoute un 2e fichier pour un état : code 1, état en double nommé ;
  - `UiCaptureCompare` des références contre une 2e capture du même commit : écart ≤ 2,0 par canal sur les états statiques, écarts des états animés notés ;
  - `--smoke-frontend` code 0, aucun avertissement.
- Retour arrière : `git revert` du commit de la tâche.
- Commit : `test(racing-casa): capture and compare UI screens`

## Phase 1 — Infrastructure et écran pilote

### ✅ T1.1 — Script et assets d'images

> Validation (2026-10-05) : `python scripts/generate_rgce_ui_assets.py` écrit 3 `.texture`, 29 `.sprite` (`Content/UI/Sprites/`) et 33 entrées `Ui.*` (dont `Ui.Hud.IngameImage` pour `Textures/ingame.png`). 2e exécution sur le dépôt : empreinte SHA-1 du catalogue et des 32 fichiers identique. Contrat du catalogue sur une copie avec les 5 entrées et une entrée étrangère `Foreign.Entry` : 2e exécution sans diff, les 6 entrées d'origine identiques et à leur place, octets d'origine conservés en tête du fichier, CRLF seul, pas de saut de ligne final. `dotnet build RacingGame.slnx` 0 erreur ; 32 fichiers copiés dans `Content/UI/Sprites` du build ; `--smoke-frontend` code 0, catalogue de 38 assets chargé, aucun avertissement. Résolution des sprites : vérifiée en T1.2 et par chaque écran migré.

- Objectif : les sprites et textures de l'interface existent et sont catalogués (D5, P5).
- Fichiers :
  - `scripts/generate_rgce_ui_assets.py` ;
  - `RacingGameCasaEngine/Content/UI/Sprites/*.sprite` et `*.texture` (générés) ;
  - `RacingGameCasaEngine/Content/AssetInfos.json` ;
  - `RacingGameCasaEngine/RacingGameCasaEngine.csproj` (règle de copie `Content\UI\**`).
- Étapes :
  1. Table des 29 rectangles, issue de `UI/LegacyMenuUiAtlas.cs` et `Screens/RaceHudScreen.cs:30-50` :
     - fond de menu, logo et fond entier ;
     - 5 icônes : recadrage intérieur de 112×112, comme `LegacyMenuUiTheme.cs:222` ;
     - 3 pistes et 2 boutons A/B ;
     - 6 éléments du HUD et 10 chiffres.
  2. Écrire les `.texture`, les `.sprite` et les entrées de catalogue avec des GUID uuid5 stables.
  3. Ajouter l'entrée de catalogue de `ingame.png`.
- Validation :
  - build 0 erreur ;
  - 2e exécution du script : aucun diff par rapport à l'état après la 1re exécution ;
  - contrat du catalogue (P5) : sur une copie de `AssetInfos.json` contenant les 5 entrées actuelles et une entrée étrangère sans préfixe `Ui.`, 2 exécutions avec `--catalog`. Après la 2e : aucun diff, et les entrées étrangère et existantes sont identiques octet pour octet à l'original ;
  - résolution des sprites : contrôlée en T1.2 par le Splash, qui en affiche un. Les autres sprites le sont par chaque écran qui les utilise, une image non résolue laissant un avertissement dans le journal de `--smoke-frontend`.
- Retour arrière : suppression des fichiers générés et `git restore` de `AssetInfos.json` et du csproj avant commit ; `git revert` après.
- Commit : `feat(racing-casa): generate UI sprite assets for XAML screens`

### ✅ T1.2 — Base XAML commune et Splash

> Validation (2026-10-05) : `dotnet build RacingGame.slnx` 0 erreur ; `--smoke-frontend` code 0, aucun avertissement. `--capture-ui-screens` code 0, aucun avertissement. `UiCaptureCompare` contre `references-579e1d7` : `splash` à 0,00, rendu XAML identique au pixel près à l'écran construit en code ; les 6 autres écrans de menu et `race-finished` à 0,00 ; `race-hud` 0,94/1,17/1,40 et `pause` 0,29/0,37/0,44. Ces deux écarts de course viennent du décor (O4), le HUD étant identique sur la planche. Base `RaceXamlScreenBase` : `LoadDefaultResources` avant chargement, `Dispose` dans `Hide`, sûr car `ScreenStack` n'appelle `Hide` qu'au `Pop`/`Remove` et relit `GetWindows()` ensuite (`ScreenStack.cs:79-107`). 🧪 non requis : le Splash n'a qu'un bouton, que la sonde de capture n'actionne pas ; sa vérification au clic reste dans la vérification manuelle globale.

- Objectif : la base `RaceXamlScreenBase` (P3) existe et Splash est le premier écran migré, de bout en bout.
- Fichiers :
  - `Screens/RaceXamlScreenBase.cs` ;
  - `Screens/SplashScreen.cs` ;
  - `Content/UI/Screens/Splash/Splash.uiscreen` + `.xaml` ;
  - `Content/AssetInfos.json` ;
  - `Bootstrap/RaceFrontEndFlow.cs` (fabrique).
- Étapes :
  1. Base : `LoadDefaultResources` avant chargement, `Dispose` dans `Hide`, affectation de `WindowDataContext`.
  2. Splash : arbre en XAML (fond en sprite entier, panneau, titre, texte, bouton Continue) ; action par `AddCommandHandler` ; focus initial en code.
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` code 0, aucun avertissement ;
  - `--capture-ui-screens`, puis `UiCaptureCompare` contre les références de T0.2 : Splash à ≤ 2,0 par canal et planche contrôlée ; les 9 autres états inchangés, à ≤ 2,0 hors zones animées.
- Retour arrière : avant commit, suppression des fichiers créés et `git restore` des fichiers modifiés ; après, `git revert`.
- Commit : `feat(racing-casa): load the splash screen from a XAML screen asset`

## Phase 2 — Écrans de menu

Chaque tâche de cette phase :
- crée `Content/UI/Screens/<Nom>/<Nom>.uiscreen` + `.xaml` et l'entrée de catalogue ;
- crée le view model de l'écran (P1) ;
- réécrit l'écran sur `RaceXamlScreenBase` : actions (P2), restylage par image des éléments nommés (D6), décor lié (P4) ;
- met à jour la fabrique de `RaceFrontEndFlow`.

Validation commune :
- build 0 erreur ;
- `--smoke-frontend` code 0, aucun avertissement ;
- `--capture-ui-screens` puis `UiCaptureCompare` contre les références de T0.2 : l'écran migré à ≤ 2,0 par canal (zones animées jugées à l'œil) et planche contrôlée ; les autres états sans régression ;
- 🧪 tant que les clics et la navigation au clavier n'ont pas été vérifiés par l'auteur.

Retour arrière commun : avant commit, suppression des fichiers créés par la tâche et `git restore` des fichiers modifiés ; après, `git revert`.

### ⏳ T2.1 — MainMenu et décor partagé

- Objectif : `MenuDecorationViewModel` (fond, rectangle et rebond du logo, P4) et menu principal à 5 icônes (sprites `Ui.Menu.*`).
- Commit : `feat(racing-casa): load the main menu from a XAML screen asset`

### ⏳ T2.2 — Help

- Objectif : sections d'aide liées au catalogue (`RaceFrontEndCatalog.HelpSections`, emplacements fixes, P7), bouton Back.
- Commit : `feat(racing-casa): load the help screen from a XAML screen asset`

### ⏳ T2.3 — Highscores

- Objectif : 3 onglets et 10 lignes liés au view model, niveau sélectionné local à l'écran.
- Commit : `feat(racing-casa): load the highscores screen from a XAML screen asset`

### ⏳ T2.4 — TrackSelection

- Objectif : 3 pistes (sprites), noms liés, sélection et boutons A/B.
- Commit : `feat(racing-casa): load the track selection from a XAML screen asset`

### ⏳ T2.5 — CarSelection

- Objectif : résumé, 4 statistiques (barres de progression liées), 11 pastilles de couleur, flèches, aperçu 3D lié (P8) et libéré.
- Commit : `feat(racing-casa): load the car selection from a XAML screen asset`

### ⏳ T2.6 — Options

- Objectif :
  - formulaire lié à double sens : nom (`TextBox`), 7 cases, 3 curseurs avec libellés de valeur, 5 résolutions, 2 modes ;
  - mise en page calculée à partir de `Root.Metrics` (P6) ;
  - application des réglages au bouton Back inchangée ;
  - « High Detail » retirée de l'écran, de `RaceFrontEndState` et de `Persistence/FrontEndOptionsPersistence.cs` (D12).
- Commit : `feat(racing-casa): load the options screen from a XAML screen asset`

## Phase 3 — Écrans de course

### ⏳ T3.1 — Pause

- Objectif : résumé de course lié (piste, tour, temps total), boutons Reprendre et Retour au menu.
- Validation : celle de la phase 2, pause comprise dans la capture.
- Commit : `feat(racing-casa): load the pause screen from a XAML screen asset`

### ⏳ T3.2 — HUD de course entièrement en XAML

- Objectif :
  - D4 et P9 : panneaux en sprites, chiffres en emplacements `Image` liés, temps et nom de piste en texte ombré, top 5, compte-tours et aiguille liée en rotation ;
  - panneau de fin de course lié, une ligne par tour terminé puis « Rank » (D10 : 5 emplacements, Expert compris) ;
  - fermeture par saisie en code.
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` et `--capture-track-audit` code 0, aucun avertissement ;
  - captures en course et en fin de course comparées aux références ;
  - 🧪 pour la conduite réelle (aiguille, vitesse, rapport) par l'auteur.
- Commit : `feat(racing-casa): load the race HUD from a XAML screen asset`

## Phase 4 — Options de jeu effectives

### ⏳ T4.1 — Option « Shadows »

- Objectif : D11, ombres du monde de course pilotées par `RaceFrontEndState.EnableShadows`.
- Fichiers : `RacingGameCasaEngine/Worlds/RaceWorldFactory.cs` (lumière principale `Light.Key`, `ShadowSettings` du monde), lecture de l'état à la création du monde de course.
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` et `--capture-track-audit` code 0, aucun avertissement ;
  - captures de course avec l'option cochée (ombres visibles) et décochée (rendu identique aux références de T0.2 pour la scène) ;
  - nouvelles références de course notées ici, car le rendu par défaut change.
- Commit : `feat(racing-casa): drive race shadows from the Shadows option`

### ⏳ T4.2 — Option « Gamepad Vibration »

- Objectif : D13, portage de la règle legacy.
- Fichiers :
  - composant ou service de vibration dans RGCE ;
  - points de contact glissière des deux modes de conduite (`Components/ArcadeCarMovementComponent.cs`, `Components/ArcadeVehicleDynamicsSolver.cs`) ;
  - arrêt en pause, en fin de course et au retour au menu (`Bootstrap/RaceRuntimeUiCoordinator.cs`, `GameFramework/RaceGameMode.cs` selon le point de passage établi) ;
  - manette du joueur local par l'API d'entrée de CasaEngine (`CasaEngine/CasaEngine/Engine/Input/GamePad.cs:105`).
- Validation :
  - build 0 erreur ;
  - `--smoke-frontend` code 0 ;
  - journal de trace des demandes de vibration (type de choc, intensité, durée) lors d'une course de l'audit ;
  - 🧪 ressenti réel à la manette par l'auteur.
- Commit : `feat(racing-casa): vibrate the gamepad on barrier hits`

## Phase 5 — Projet CasaEngine pour l'éditeur

> Détaillée à la fin de la découverte en cours (fichier projet, monde `.world`, assembly des view models, données de conception, vérification d'ouverture). Les tâches T5.x sont ajoutées ici avant T2.1, premier écran doté d'un view model, car l'emplacement des view models en dépend (T1.2 n'en crée aucun : le Splash n'a pas de valeur dynamique).

## Phase 6 — Nettoyage, rapports et clôture

### ⏳ T6.1 — Suppression du code de construction devenu mort

- Objectif : P13.
- Validation :
  - build 0 erreur ;
  - garde `rg` de la validation globale ;
  - `--smoke-frontend` code 0.
- Commit : `refactor(racing-casa): remove code-built screen factories`

### ⏳ T6.2 — Rapports

- Objectif :
  - `docs/mgui-gaps-from-rgce-xaml-screens.md` (P11), à partir des contournements effectivement faits pendant T1.2 → T3.2 ;
  - section chiffrée sur le rendu des textes du HUD XAML comparé à `DrawShadowedText` (D15) ;
  - absence d'effet de « Post Screen Effects » et la raison (D14).
- Commit : `docs(racing-casa): report MGUI gaps met by the XAML screens`

### ⏳ T6.3 — Validation globale et rapport de fin

- Objectif : validation globale, passe `verifier` indépendante, rapport de fin, index `ai-agent/README.md` à jour.
- Commit : `docs(racing-casa): close the XAML screens plan`

---

## Points ouverts

À trancher pendant l'exécution, ou à remonter en ⚠️ Blocked si la réponse manque.

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | **Tranché (D10).** Ligne « Rank » perdue sur Expert. | T3.2 |
| O2 | **Tranché (D11 → D14).** Options sans effet (`PostEffects`, `Shadows`, `HighDetail`, `Vibration`). | T2.6, T4.1, T4.2, T6.2 |
| O3 | **Tranché (D15).** Rendu des textes du HUD (`DrawShadowedText` contre `TextBlock`). | T3.2, T6.2 |
| O4 | Bruit de décor dans les états de course : `CreateDeterministicTrackRandom` (`Worlds/LegacyTrackSceneFactory.cs:767-773`) amorce son `Random` avec `System.HashCode`, aléatoire d'un processus à l'autre ; le choix des panneaux (`Track.Scenery.Banner*`) change donc à chaque lancement. Écart observé jusqu'à 1,40 sur `race-hud` entre deux runs, HUD identique. Bug antérieur, hors périmètre : signalé à l'auteur dans une tâche séparée. Les états de course sont jugés sur la planche, en ignorant le décor. | T1.2 → T3.2 |

## Hors périmètre

- Le jeu legacy (`RacingGame`, `RacingGame.Shared`) (D1).
- Toute modification de CasaEngine ou de MGUI (D8).
- Un projet de test RGCE (P10).
- Push, merge, PR.
