# Plan agent IA — Capture `race-finished` déterministe

Plan court issu de l'enquête sur le point ouvert O2 de [rgce-element-naming-tasks.md](rgce-element-naming-tasks.md) (2026-10-09).
Les décisions D1 → D3 sont **proposées** ci-dessous : elles attendent l'arbitrage de l'auteur. Aucun code n'est modifié avant son approbation.

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
- **La cadence seule change le résultat.** Avec une pause artificielle en fin de `Draw` (sonde, rien d'autre ne change), le validateur capture 74 pas (0 ms), 71 (12 ms), 72 (16 ms, **identique à la référence de 09:46**) ou 73 pas (20 ms). Les 9 autres états sont statiques et restent à 0,00.
- **Le phénomène est ancien.** Historique de `Screenshots/` : entre 00:18 et 00:33, sans aucun commit entre-temps, `race-finished` a pris 4 images différentes en 16 lancements (dont des lancements concurrents) ; de 01:36 à 09:46, 4 lancements sur 5 donnent 72 pas ; de 10:13 à 10:51, les 12 lancements donnent 74 pas, comme les 3 de 14:27 à 14:31 (dont 2 instrumentés). Les journaux Windows ne montrent pas ce qui a changé la cadence entre 09:46 et 10:13. La cause *exacte* du basculement de ce matin n'est pas retrouvable ; le mécanisme, lui, est établi.

## État vérifié du dépôt (2026-10-09)

- Branche `remaster`, HEAD `20f8a1e`. Arbre de travail : sous-module `CasaEngine` modifié (pointeur, autre session), `.serena/` et `log.txt` non suivis. **Ne pas les indexer.**
- `RaceGameMode.Configure(RaceFrontEndState state)` reçoit l'état du front-end au lancement de la course (`RacingGame.Gameplay/GameFramework/RaceGameMode.cs:47-65`) ; `ChaseCameraRigComponent` lit déjà `session.GameMode` (`:99-119`).
- Précédent : sous `--capture-ui-screens`, le clignotement, la rotation et le balancement sont figés « so that two runs give the same image » (ADR-0008 `:38`, ADR-0009 `:40`).

## Décisions proposées (à arbitrer par l'auteur)

| Réf | Proposition | Alternatives |
|---|---|---|
| D1 | **Figer l'orbite d'arrivée sous capture** (recommandé) : quand `PinScreenAnimations` est levé, l'angle d'orbite reste fixe et la pose est appliquée sans lissage. L'image ne dépend plus du draw lu. | B : refaire seulement la référence (ne corrige rien : l'image rebasculera dès que la cadence change). C : en plus de A, capturer en fin de `Draw` dans tous les validateurs (supprime le décalage d'une image ; plus large, touche la piste d'audit). |
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

### ⏳ T1.1 — Figer l'orbite d'arrivée sous capture

- Objectif : sous `PinScreenAnimations`, la caméra d'arrivée prend une pose fixe (D1, D2), atteinte dès la première image.
- Fichiers : `RacingGame.Gameplay/GameFramework/RaceGameMode.cs` (garder `state.PinScreenAnimations` dans `Configure`), `RacingGame.Gameplay/Components/ChaseCameraRigComponent.cs` (angle fixe et pose sans lissage quand le mode est figé), `RacingGame.Gameplay/Race/RaceFrontEndState.cs` (doc XML).
- Étapes :
  1. `RaceGameMode` expose l'état figé reçu de `Configure`.
  2. `UpdateFinishOrbit` : si figé, ne pas avancer `_finishOrbitAngle` (valeur D2) et appliquer la pose désirée directement (pas de `Vector3.Lerp`).
  3. Doc XML de `PinScreenAnimations` : ajouter l'orbite d'arrivée.
- Validation : build ; sonde 0/12/16/20 ms (validation globale) ; `--smoke-frontend` code 0.
- Commit : `fix(racing-casa): pin the finish orbit camera under the UI capture`

### ⏳ T1.2 — Nouvelle référence et clôture de O2

- Objectif : une référence `race-finished` reproductible, et O2 clos avec sa cause.
- Fichiers : `ai-agent/tasks/rgce-element-naming-tasks.md` (O2 : cause et lien vers ce plan), `ai-agent/README.md`, ce plan.
- Étapes :
  1. Deux lancements de `--capture-ui-screens` copiés dans `artifacts/race-finished-capture/{ref-a,ref-b}/ui` (ignoré par git) : 10 états à 0,00 entre eux ; 9 états à 0,00 contre `artifacts/element-naming/verify/ui`.
  2. O2 du plan de nommage : cause (orbite animée, capture du draw précédent, cadence) et référence à utiliser désormais.
  3. Passe `verifier` fraîche sur T1.1 et T1.2.
- Validation : comparaisons ci-dessus ; verdict CONFIRMED.
- Commit : `docs(racing-casa): record the deterministic race-finished capture`

---

## Points ouverts

| Réf | Sujet | Tâche concernée |
|---|---|---|
| O1 | Arbitrage de D1, D2 et D3 par l'auteur. | T1.1, T1.2 |
| O2 | Observation hors plan : un lancement de sonde (pause de 8 ms) a quitté la course juste après l'arrivée, puis a échoué au délai de `race-finished`. Le journal montre un retour au menu sans action du validateur : cela correspond à une vraie touche ou un clic reçu par la fenêtre (« Press Space, Enter, … click … »). Non approfondi. | — |
