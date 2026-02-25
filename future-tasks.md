# RacingGame — Tâches Futures (Refactorisation Lourde)

**Légende** : ✅ Implémenté | 📋 Tâche future | 🔄 En cours

Ces tâches sont trop complexes pour être implémentées en une seule passe sans risque de régression majeure.
Elles constituent une feuille de route pour améliorer progressivement la qualité et les fonctionnalités du jeu.

---

## Architecture (ARCH)

### 📋 ARCH-001 : Refactoriser les classes statiques
- **Priorité** : Haute
- **Description** : Quasiment tout est statique (`RacingGameManager`, `BaseGame`, `Input`, `Sound`, `ShaderEffect`). Cela rend le code non-testable et crée un couplage fort.
- **Plan** :
  1. Créer une interface `IGameContext` ou `IServiceLocator` avec les dépendances essentielles
  2. Convertir chaque classe statique en classe d'instance
  3. Passer les dépendances via constructeur ou service locator
  4. Mettre à jour tous les sites d'appel
- **Risque** : Très élevé — implique des changements dans tous les fichiers

### ✅ ARCH-002 : Remplacer l'héritage profond par la composition
- **Statut** : Implémenté — `ChaseCamera` n'hérite plus de `CarPhysics`. `Player` hérite uniquement de `CarPhysics` et possède un `ChaseCamera Camera` en composition. Chaîne réduite de 4 à 3 niveaux (`BasePlayer → CarPhysics → Player`).

### ✅ ARCH-003 : Séparer Input/Logique/Rendu dans les GameScreens
- **Statut** : Implémenté — `IGameScreen` étendu avec `Update(GameTime)` et `Render()`. Tous les 9 écrans migrés. `RacingGameManager` appelle `Update()` puis `Render()` séparément.

### ✅ ARCH-004 : Décomposer la classe Sound monolithique
- **Statut** : Implémenté — `Sound.cs` décomposé en `MusicManager`, `SfxManager` et `EngineSound`. Façade `Sound` statique conservée pour compatibilité.

### 📋 ARCH-005 : Décomposer la classe Landscape (~1425 lignes)
- **Priorité** : Moyenne
- **Description** : `Landscape.cs` gère le terrain, les objets, les traces de frein, les replays, les checkpoints, la physique et le rendu.
- **Plan** :
  1. `TerrainRenderer` : rendu du terrain et textures
  2. `TrackObjectManager` : placement/rendu des objets de bord de route
  3. `BrakeTrackManager` : gestion des traces de pneus
  4. `CheckpointSystem` : gestion des checkpoints et des tours

---

## Performance (PERF)

### ✅ PERF-001 : Stack<IGameScreen> non thread-safe
- **Statut** : Implémenté — verrou `_screenLock` ajouté + pattern snapshot dans Update/Render.

### ✅ PERF-002 : Allocations fréquentes dans la boucle de rendu
- **Statut** : Implémenté — `_carCorners` pré-alloué en champ `readonly Vector3[4]` dans `CarPhysics`.

### ✅ PERF-005 : RenderToTexture utilise Rgba64 par défaut
- **Statut** : Implémenté — paramètre `bool isHDR = false` ajouté ; `SurfaceFormat.Color` par défaut, `Rgba64` pour ShadowMap/HDR.

---

## Code Quality (QUAL)

### ✅ QUAL-001 : Supprimer les directives préprocesseur obsolètes
- **Priorité** : Haute
- **Statut** : Implémenté — Tous les blocs `#if XBOX360`, `#if GAMERSERVICES`, `#if NETFX_CORE`, `#if XBOXONE` supprimés de 11 fichiers (203 suppressions). Commit `546fa65`.

### ✅ QUAL-002 : Convertir les champs publics en propriétés
- **Priorité** : Moyenne
- **Statut** : Implémenté — `currentCarNumber/Color/colorSelectionTexture` → `CurrentCarNumber/CurrentCarColor/ColorSelectionTexture`, `globalRandomGenerator` → `GlobalRandomGenerator`, `SpringPhysicsObject.pos/velocity/force` → `Pos/Velocity/Force`. Commit `34f96a1`.

### ✅ QUAL-003 : Réduire l'utilisation excessive des #region
- **Priorité** : Basse
- **Statut** : Implémenté — Blocs `#region File Description ... #endregion` et `#region Using directives` supprimés de 55 fichiers via `scripts/clean_regions.py` (709 suppressions). Commit `c8e1c35`.

### ✅ QUAL-004 : Extraire les magic numbers en constantes nommées
- **Priorité** : Haute
- **Statut** : Implémenté — 30+ constantes nommées ajoutées dans `CarPhysics.cs` (#region Constants) : `PitchSpringFriction`, `RotationFrictionFactor`, `KeyboardRotationDivisor`, `GlancingCollisionWobbleFactor`, etc. `GameOverCameraRotationPeriodMs` ajouté dans `Player.cs`. Commit `04ab63b`.

### ✅ QUAL-005 : Décomposer les méthodes trop longues
- **Priorité** : Haute
- **Statut** : Implémenté — `CarPhysics.Update()` → `HandleRotations()`, `HandleViewDistance()`, `HandleSpeed()`, `UpdateTrackAndPhysics()`. `Options.Render()` → `RenderMenuBackground()`, `RenderResolutionOptions()`, `RenderGraphicsOptions()`, `RenderAudioSliders()`, `RenderSelectionArrow()`. Commit `a25550a`.

### ✅ QUAL-006 : Nettoyer le code mort et les commentaires inutiles
- **Priorité** : Moyenne
- **Statut** : Implémenté — Suppression UWP blocks dans `BaseGame.cs` (handler `graphics_PrepareDevice` vide + enregistrement, champ `CurrentPlatform`, `//TODO:` etc.), alternative `//try1:` dans `CarSelection.cs`. Commit `b315d35`.

### ✅ QUAL-007 : Implémenter IDisposable correctement
- **Priorité** : Haute
- **Statut** : Implémenté — `ShaderEffect.DisposeAll()` dispose les 5 shaders statiques. `BaseGame.Dispose(bool)` dispose `ui`, `lineManager2D`, `lineManager3D` et appelle `DisposeAll()`. `RacingGameManager.Dispose(bool)` dispose `landscape`. Commit `d9ca42a`.

---

## Fonctionnalités (FEAT)

### 📋 FEAT-003 : Écran de pause en jeu
- **Priorité** : Haute
- **Description** : ESC quitte directement vers le menu. Ajouter un écran de pause.
- **Plan** :
  1. Créer `PauseScreen : IGameScreen`
  2. ESC en jeu → push `PauseScreen` sur le stack
  3. Options : Reprendre, Recommencer, Menu principal
  4. Mettre en pause le son moteur et les positions de physique

### 📋 FEAT-004 : Résolutions modernes dans Options
- **Priorité** : Moyenne
- **Description** : Résolutions obsolètes (640x480, 800x600, 1024x768).
- **Plan** :
  1. Utiliser `GraphicsAdapter.DefaultAdapter.SupportedDisplayModes` pour lister les résolutions disponibles
  2. Filtrer les doublons et trier par taille
  3. Afficher dynamiquement dans le menu Options

### 📋 FEAT-005 : Afficher le FPS en option
- **Priorité** : Basse
- **Description** : `BaseGame.FPS` est calculé mais jamais affiché.
- **Plan** : Ajouter un booléen `GameSettings.ShowFPS` et afficher `TextureFont.WriteText(fps, ...)` dans une position fixe si activé.

### 📋 FEAT-006 : Support des manettes modernes
- **Priorité** : Moyenne
- **Description** : Un seul gamepad (`PlayerIndex.One`), pas de vibration, pas de remapping.
- **Plan** :
  1. Ajouter `GamePad.SetVibration()` lors des collisions
  2. Créer un `InputMapper` pour le remapping
  3. Persister le mapping dans `GameSettings`

### 📋 FEAT-008 : Voitures IA adversaires
- **Priorité** : Basse
- **Description** : Jeu solo uniquement. Pas de compétiteurs.
- **Plan** :
  1. Créer `AIPlayer : CarPhysics` qui suit le tracé du circuit
  2. Ajouter un paramètre de difficulté (vitesse max IA, agressivité)
  3. Afficher les voitures IA avec `CarModel.RenderCar()`

### 📋 FEAT-009 : Effets visuels de collision
- **Priorité** : Basse
- **Description** : Collisions sans effets visuels.
- **Plan** :
  1. Intégrer un système de particules simple (sparks)
  2. `GamePad.SetVibration()` proportionnel à la force de collision
  3. Flash d'écran lors d'une collision forte

### 📋 FEAT-010 : Barre de progression sur l'écran de chargement
- **Priorité** : Basse
- **Description** : Écran de chargement sans feedback quantitatif.
- **Plan** :
  1. Ajouter une propriété `LoadProgress` (0..1) dans `RacingGameManager`
  2. `LoadResources()` met à jour `LoadProgress` à chaque étape
  3. `LoadingScreen.Render()` dessine une barre proportionnelle

---

## Build & Projet (BUILD)

### 📋 BUILD-001 : Mettre à jour le TargetFramework
- **Priorité** : Basse
- **Description** : Cibles `net8.0-windows`. Évaluer la mise à jour vers `net9.0-windows` ou suivre les futures LTS.
- **Plan** : Tester la compatibilité MonoGame + mettre à jour les packages NuGet. Corriger les avertissements éventuels.

### 📋 BUILD-002 : Ajouter un fichier .editorconfig
- **Priorité** : Moyenne
- **Description** : Pas de style de code uniforme. Mélange tabs/espaces, styles de nommage.
- **Plan** : Créer `.editorconfig` avec règles C# standard, forcer `dotnet format` dans le CI.

### 📋 BUILD-003 : Ajouter des tests unitaires
- **Priorité** : Haute
- **Description** : Constructeur `RacingGameManager(string unitTestName)` présent mais aucun projet de test.
- **Plan** :
  1. Créer `RacingGame.Tests` (xUnit)
  2. Tester en priorité : `Highscores`, `Replay` (parse/serialize), `Vector3Helper`, `Input.KeyToChar`, `CarPhysics` (formules)
  3. Mocker `BaseGame` via des interfaces ou abstractions pour tester la physique sans GPU

### 📋 BUILD-004 : PipelineExtension — remplacer System.Numerics par Microsoft.Xna.Framework
- **Priorité** : Moyenne
- **Description** : `RacingGameModelProcessor.cs` utilise `System.Numerics.Vector2/3/4` au lieu de `Microsoft.Xna.Framework.Vector2/3/4`. Cela peut provoquer des erreurs de désérialisation au runtime car le content pipeline attend les types XNA.
- **Raison du report** : Le projet `RacingGame.PipelineExtension` cible `net8.0` (sans suffixe `-windows`). Dans ce contexte, `Microsoft.Xna.Framework` ne rend pas les types `Vector2/3/4` accessibles directement.
- **Plan** :
  1. Vérifier si passer `RacingGame.PipelineExtension` de `net8.0` à `net8.0-windows` est possible sans casser la chaîne de build
  2. Ou utiliser les types `Microsoft.Xna.Framework.Content.Pipeline.Graphics` équivalents déjà disponibles dans le pipeline
  3. Tester que le content pipeline construit et produit des assets valides après le changement

---

*Fichier généré le : 2025*  
*Voir aussi : [improve-tasks.md](improve-tasks.md) pour le statut complet de toutes les tâches.*
