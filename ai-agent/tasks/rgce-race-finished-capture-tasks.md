# Plan agent IA — Capture `race-finished` déterministe

Plan court issu de l'enquête sur le point ouvert O2 de [rgce-element-naming-tasks.md](rgce-element-naming-tasks.md) (2026-10-09).
Les décisions D1 → D3 ont été arbitrées par l'auteur le 2026-10-15 (« fais le » : options recommandées) : **ce plan les applique, il ne les rediscute pas**.

Ce fichier doit être mis à jour pendant le travail : l'icône au début de chaque tâche indique son statut courant.

## Objectif

L'état `race-finished` de `--capture-ui-screens` donne la même image d'un lancement à l'autre, quelle que soit la cadence d'affichage de la machine. Une nouvelle référence `race-finished` est prise, et O2 est clos avec sa cause.

Ce plan ne livre pas : la capture en fin de `Draw` pour tous les validateurs (option C de D1, hors périmètre sauf choix de l'auteur), ni la correction de l'ADR-0020 (question O4 du plan de nommage).

## Cause établie (enquête du 2026-10-09)

Preuves détaillées, sonde et journaux : dossier de notes partagé `o2-race-finished/` de la session principale (`evidence.txt`, `runs/`, `probe-src-changes/`).

- **La scène est animée.** Après l'arrivée, la caméra de poursuite tourne autour de la voiture : `_finishOrbitAngle += FinishOrbitSpeedRadiansPerSecond * elapsedTime` (`RacingGame.Gameplay/Components/ChaseCameraRigComponent.cs:137-152`), soit 0,0246 rad (1,41°) par update à 60 Hz, avec un lissage de position (`:220-235`). Rien ne la fige sous capture : `PinScreenAnimations` (`RaceFrontEndState.cs:15-21`, posé par `UiScreenCaptureValidator.cs:81`) ne concerne que les écrans.
- **La capture lit une image déjà présentée.** `CaptureScreenshotWithStem` appelle `GraphicsDevice.GetBackBufferData` pendant un `Update` (`RacingGameCasaEngineGame.cs:480-490`). Une sonde qui marque chaque draw de son numéro montre que l'image lue est **celle du draw précédant le dernier draw**, dans les 5 lancements instrumentés. (Le commentaire de `DrawCount`, `:402`, dit « the last presented frame » : sur cette machine, c'est l'avant-dernière.)
- **Le pas fixe découple updates et draws.** Le jeu est en pas fixe à 60 Hz (`RacingGameCasaEngineGame.cs:195`). En course, la cadence des draws varie d'un lancement à l'autre (de 75 draws pour 75 updates à 49 pour 75, `IsRunningSlowly`), et MonoGame enchaîne des updates de rattrapage sans draw. Le numéro d'update montré par l'avant-dernier draw dépend donc de cette cadence. Elle dépend elle-même de la machine : GPU, charge, VSync des réglages de l'auteur, écran à 100 Hz.
- **Les deux références sont deux pas de la même orbite.** Sur une capture faite en fin de `Draw`, pas par pas, avec le même code :
  - la référence de 09:46 (`artifacts/element-naming/before/ui`) est **identique (0,00)** à l'image après 72 pas ;
  - les captures depuis 10:13 (`artifacts/element-naming/verify/ui`) sont **identiques (0,00)** à l'image après 74 pas ;
  - l'écart de 13,88 entre elles est celui de 2 pas.
- **La cadence seule change le résultat.** Avec une pause artificielle en fin de `Draw` (sonde, rien d'autre ne change), le validateur capture 74 pas (0 ms), 71 (12 ms), 72 (16 ms, **identique à la référence de 09:46**) ou 73 pas (20 ms). Les 9 autres états ne dépendent pas de l'orbite ; ils ont leurs propres petites variantes de cadence, sous le seuil en temps normal (O4, constaté le 2026-10-15).
- **Le phénomène est ancien.** Historique de `Screenshots/` : entre 00:18 et 00:33, sans aucun commit entre-temps, `race-finished` a pris 4 images différentes en 16 lancements (dont des lancements concurrents) ; de 01:36 à 09:46, 4 lancements sur 5 donnent 72 pas ; de 10:13 à 10:51, les 12 lancements donnent 74 pas, comme les 3 de 14:27 à 14:31 (dont 2 instrumentés). Les journaux Windows ne montrent pas ce qui a changé la cadence entre 09:46 et 10:13. La cause *exacte* du basculement de ce matin n'est pas retrouvable ; le mécanisme, lui, est établi.

## État vérifié du dépôt (2026-10-09)

- Branche `remaster`, HEAD `20f8a1e`. Arbre de travail : sous-module `CasaEngine` modifié (pointeur, autre session), `.serena/` et `log.txt` non suivis. **Ne pas les indexer.**
- `RaceGameMode.Configure(RaceFrontEndState state)` reçoit l'état du front-end au lancement de la course (`RacingGame.Gameplay/GameFramework/RaceGameMode.cs:47-65`) ; `ChaseCameraRigComponent` lit déjà `session.GameMode` (`:99-119`).
- Précédent : sous `--capture-ui-screens`, le clignotement, la rotation et le balancement sont figés « so that two runs give the same image » (ADR-0008 `:38`, ADR-0009 `:40`).

## Décisions verrouillées

| Réf | Décision | Alternatives écartées |
|---|---|---|
| D1 | **Figer l'orbite d'arrivée sous capture** : quand `PinScreenAnimations` est levé, l'angle d'orbite reste fixe et la pose est appliquée sans lissage. L'image ne dépend plus du draw lu. | B : refaire seulement la référence (ne corrige rien : l'image rebasculera dès que la cadence change). C : en plus de A, capturer en fin de `Draw` dans tous les validateurs (supprime le décalage d'une image ; plus large, touche la piste d'audit). |
| D2 | Angle figé = **0** : caméra derrière la voiture, à `FinishOrbitDistance` et `FinishOrbitHeight`. | Un autre angle fixe (par exemple le côté de la voiture, comme les images actuelles, vers 1,8 rad). |
| D3 | **Pas de nouvelle ADR** : application de la règle d'ADR-0009 ; la doc XML de `PinScreenAnimations` cite l'orbite. | ADR-0021 courte. |

## Règles d'exécution pour l'agent

- **Branche `remaster`** (celle des plans RGCE en cours). Ne jamais committer sur `main` ni pousser.
- **Une seule tâche à la fois**, icône `🚧` puis `✅`, `🧪` ou `⚠️`, note de validation sous la tâche, **un commit par tâche** incluant ce fichier.
- **Un seul processus de jeu ou d'éditeur à la fois**, lancé par un helper qui sauvegarde et restaure `display-settings.json` et `front-end-options.json` ; aucune entrée clavier ou souris simulée.
- Build : `dotnet build RacingGame.slnx`. `git add` fichier par fichier.
- Langue : ce plan en français ; code et commits en anglais.

## Validation globale

- `dotnet build RacingGame.slnx` : 0 erreur.
- Sonde de bloc-notes reconstruite sur le code corrigé (copie des sources de `o2-race-finished/probe-src-changes/`), pause en fin de `Draw` à 0, 12, 16 et 20 ms : `race-finished` identique (0,00) dans les 4 lancements. Avant correctif, ces 4 lancements donnent 4 images différentes (74, 71, 72 et 73 pas).
- `--capture-ui-screens` contre `artifacts/element-naming/verify/ui` : 9 états à 0,00 ; `race-finished` change une fois (nouvelle pose), puis 0,00 entre deux lancements.
- `--smoke-frontend` : code 0, aucun avertissement ni erreur.

---

## Phase 1 — Capture déterministe

### ✅ T1.1 — Figer l'orbite d'arrivée sous capture

- Objectif : sous `PinScreenAnimations`, la caméra d'arrivée prend une pose fixe (D1, D2), atteinte dès la première image.
- Fichiers : `RacingGame.Gameplay/GameFramework/RaceGameMode.cs` (garder `state.PinScreenAnimations` dans `Configure`), `RacingGame.Gameplay/Components/ChaseCameraRigComponent.cs` (angle fixe et pose sans lissage quand le mode est figé), `RacingGame.Gameplay/Race/RaceFrontEndState.cs` (doc XML).
- Étapes :
  1. `RaceGameMode` expose l'état figé reçu de `Configure`.
  2. `UpdateFinishOrbit` : si figé, ne pas avancer `_finishOrbitAngle` (valeur D2) et appliquer la pose désirée directement (pas de `Vector3.Lerp`).
  3. Doc XML de `PinScreenAnimations` : ajouter l'orbite d'arrivée.
- Validation : build ; sonde 0/12/16/20 ms (validation globale) ; `--smoke-frontend` code 0.
- Commit : `fix(racing-casa): pin the finish orbit camera under the UI capture`

> Validation (2026-10-15) :
> - `dotnet build RacingGame.slnx` : 0 erreur. Avertissements seulement dans `RacingGame.PipelineExtension` et le sous-module, déjà présents avant.
> - Sonde reconstruite sur le nouveau bin (sous-module à `87fc5f01b`, comme l'arbre de travail), pause en fin de `Draw` de 0, 12, 16 et 20 ms : `race-finished` identique (0,00) dans les 4 lancements.
>   - Pourtant le draw lu montre chaque fois un autre pas : 73, 72, 71 et 70 pas après l'arrivée.
>   - Les images lues en fin de `Draw` de 70 à 76 pas sont toutes identiques (0,00).
>   - La position de la caméra se stabilise une dizaine d'updates après l'arrivée (variations de quelques centièmes de millimètre avant), puis reste identique bit à bit de l'update 1004 jusqu'à la fin du journal, soit les 62 updates qui précèdent la capture (update 1066).
> - Témoin sans correctif (même bin, ancienne `RacingGame.Gameplay.dll`, pause de 12 ms) : 71 pas. L'image est identique à celle de 71 pas du 2026-10-09, et s'écarte de 33,69 de l'image figée.
> - `--capture-ui-screens` du build du dépôt contre `artifacts/element-naming/verify/ui` : 9 états à 0,00. `race-finished` change une fois (33,22 : nouvelle pose, caméra derrière la voiture) et vaut exactement (0,00) l'image des lancements de sonde figés.
> - `--smoke-frontend` : code 0, `Smoke validation completed successfully`, aucun avertissement ni erreur.
> - Relecture adverse en lecture seule (4 angles : déterminisme, régression, conformité, cas limites ; 2 contradicteurs par constat) : aucun constat retenu.
>   - Le seul constat soulevé (P4 : citation d'ADR-0009 à côté de l'orbite) a été réfuté par les deux contradicteurs : c'est la décision D3.
>   - Vérifié aussi : en jeu normal, l'orbite et le lissage sont inchangés ; le seul endroit qui lève le drapeau est `UiScreenCaptureValidator` ; l'éditeur et les autres modes d'automation ne sont pas touchés.
> - Pas de test unitaire : aucun projet de test ne couvre `RacingGame.Gameplay` dans ce dépôt.
> - Deux lancements sur 9 ont expiré à `race-finished` : le témoin U0, sans correctif, et le premier lancement du dépôt. Le jeu est revenu au menu juste après l'arrivée, comme dans O2. Relancé : code 0. Voir O3.
> - Écart à la validation globale : le lancement retenu a ses 9 états à 0,00, mais d'autres lancements montrent de petites variantes dues à la cadence sur d'autres états. Voir O4.

### ✅ T1.2 — Nouvelle référence et clôture de O2

- Objectif : une référence `race-finished` reproductible, et O2 clos avec sa cause.
- Fichiers : `ai-agent/tasks/rgce-element-naming-tasks.md` (O2 : cause et lien vers ce plan), `ai-agent/README.md`, ce plan.
- Étapes :
  1. Deux lancements de `--capture-ui-screens` copiés dans `artifacts/race-finished-capture/{ref-a,ref-b}/ui` (ignoré par git) : 10 états à 0,00 entre eux ; 9 états à 0,00 contre `artifacts/element-naming/verify/ui`.
  2. O2 du plan de nommage : cause (orbite animée, capture du draw précédent, cadence) et référence à utiliser désormais.
  3. Passe `verifier` fraîche sur T1.1 et T1.2.
- Validation : comparaisons ci-dessus ; verdict CONFIRMED.
- Commit : `docs(racing-casa): record the deterministic race-finished capture`

> Validation (2026-10-15) :
> - Références : deux lancements de `--capture-ui-screens` du build du dépôt (`ui-run-20261015-080937` et `-080959`) copiés dans `artifacts/race-finished-capture/{ref-a,ref-b}/ui`.
>   - `UiCaptureCompare` ref-a contre ref-b : 10 états dans le seuil ; `race-finished` à 0,00.
>   - `UiCaptureCompare` `verify/ui` contre ref-a : 9 états dans le seuil, `race-finished` au-dessus (33,14 / 33,53 / 32,99 : la nouvelle pose, attendue une fois). Rapports dans `artifacts/race-finished-capture/compare-*`.
> - **Écart à l'étape 1** (« 10 états à 0,00 entre eux ; 9 états à 0,00 contre `verify/ui` ») : non tenu, à cause des variantes de cadence d'O4, qui existaient avant le correctif.
>   - ref-a contre ref-b : track-selection 0,23.
>   - ref-a contre `verify/ui` : options 0,34, help 0,18, car-selection 0,72, race-hud 0,65.
>   - Je n'ai pas relancé jusqu'à obtenir une paire à 0,00 : cela aurait masqué O4.
>   - Références retenues : ref-a pour `race-finished` seulement ; `artifacts/element-naming/verify/ui` reste la référence des 9 autres états, la variante la plus fréquente.
> - O2 du plan de nommage clos : cause, correctif `c887e95`, références à utiliser. Lignes de `ai-agent/README.md` mises à jour.
> - Passe `verifier` fraîche : **CONFIRMED** sur 5 affirmations.
>   - C1 : `race-finished` identique (0,00) à ref-a avec 5 et 16 ms de pause par draw, alors que la capture lit 74 et 71 pas.
>   - C2 : build du dépôt identique à ref-a, 9 états à 0,00 contre `verify/ui`.
>   - C3 : jeu normal inchangé, d'après la lecture de `c887e95`.
>   - C4 : `--smoke-frontend` à code 0, sans avertissement.
>   - C5 : notes exactes, sauf un décompte (77 au lieu de 62 updates), corrigé ci-dessus.
>   - Critique de complétude en parallèle, deux contradicteurs par constat : 3 constats retenus (décompte, phrase de la section Cause, libellés des points ouverts), tous corrigés ici, et 2 contestés, dont la précision d'O4, corrigée. Le dernier contesté vise le message du commit `c887e95`, « dès la première image » : je ne réécris pas un commit, et la note de T1.1 donne le comportement mesuré. 6 autres constats ont été réfutés.
> - Docs seulement dans cette tâche : pas de build.

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | ~~Arbitrage de D1, D2 et D3 par l'auteur.~~ Réglé le 2026-10-15 : options recommandées. | T1.1, T1.2 |
| O2 | Observation hors plan : un lancement de sonde (pause de 8 ms) a quitté la course juste après l'arrivée, puis a échoué au délai de `race-finished`. Le journal montre un retour au menu sans action du validateur : cela correspond à une vraie touche ou un clic reçu par la fenêtre (« Press Space, Enter, … click … »). Non approfondi ; repris et précisé par O3. | — |
| O3 | Lancements qui expirent à `race-finished` (1 sur 7 le 2026-10-09, 2 sur 9 le 2026-10-15, avec ou sans correctif) : le jeu revient au menu juste après l'arrivée. `RaceHudScreen.IsDismissPressed` (`RacingGameCasaEngine/Screens/RaceHudScreen.cs:98-116`) accepte un clic gauche. `MonoGameWindowInputSource` ignore le clavier quand la fenêtre est inactive (`GetKeyboardState`), mais pas la souris (`GetMouseState`) : un vrai clic sur la fenêtre du jeu suffit probablement. L'entrée exacte n'a pas été journalisée. Hors périmètre : à traiter à part (ignorer l'entrée réelle pendant une automation). | — |
| O4 | D'autres états ont aussi des variantes dues à la cadence. Valeurs en moyenne RGB des écarts absolus (la mesure de `cmp.py` ; `UiCaptureCompare` donne un écart par canal, seuil 2,0) :<br>- en temps normal, sous le seuil : main-menu 0,67, car-selection 0,72, track-selection 0,23, race-hud 0,65, options 0,34, help 0,18 à 0,19 ;<br>- sous ralentissement artificiel (sonde) : aussi splash 0,64, pause 0,33, main-menu 0,82, help 0,57, et race-hud jusqu'à 2,06, **au-dessus** du seuil (2,09 sur le canal R).<br>Chacune des valeurs de temps normal est identique au pixel à un lancement de l'ancien moteur du 2026-10-09 : ni ce correctif ni le sous-module ne les causent. Après correctif, 2 des 4 captures complètes du build du dépôt ont leurs 9 états à 0,00 contre `verify/ui`. Mécanisme probablement le même (image lue en retard, état pas tout à fait figé), non établi. Hors périmètre (option C de D1). | — |
