# RacingGame — Tâches Futures (Refactorisation Lourde)

Ces tâches sont trop complexes pour être implémentées en une seule passe sans risque de régression majeure.
Elles constituent une feuille de route pour améliorer progressivement la qualité et les fonctionnalités du jeu.

---

## Architecture (ARCH)

### ARCH-001 : Refactoriser les classes statiques
- **Priorité** : Haute
- **Description** : Quasiment tout est statique (`RacingGameManager`, `BaseGame`, `Input`, `Sound`, `ShaderEffect`). Cela rend le code non-testable et crée un couplage fort.
- **Plan** :
  1. Créer une interface `IGameContext` ou `IServiceLocator` avec les dépendances essentielles
  2. Convertir chaque classe statique en classe d'instance
  3. Passer les dépendances via constructeur ou service locator
  4. Mettre à jour tous les sites d'appel
- **Risque** : Très élevé — implique des changements dans tous les fichiers

### ARCH-002 : Remplacer l'héritage profond par la composition
- **Priorité** : Haute  
- **Description** : Chaîne `BasePlayer → CarPhysics → ChaseCamera → Player` (4 niveaux). Conceptuellement incorrect : la caméra ne devrait pas hériter de la physique voiture.
- **Plan** :
  1. Extraire `CarPhysics` comme composant autonome avec une interface `ICarPhysics`
  2. Extraire `ChaseCamera` comme composant autonome avec une interface `ICamera`
  3. `Player` contient `CarPhysics` et `ChaseCamera` au lieu d'en hériter
  4. `BasePlayer` peut devenir une dataclass simple
- **Risque** : Élevé — implique `CarPhysics.cs` (~1380 lignes) et touts les appelants

### ARCH-003 : Séparer Input/Logique/Rendu dans les GameScreens
- **Priorité** : Moyenne
- **Description** : `Render()` gère à la fois l'affichage, la navigation et les transitions. Exemple : `MainMenu.Render()` gère clavier + souris + rendu dans la même méthode.
- **Plan** :
  1. Définir dans `IGameScreen` : `void ProcessInput()`, `void UpdateLogic(GameTime)`, `void Draw()`
  2. Migrer chaque screen vers ce pattern
  3. `RacingGameManager.Update()` appelle `ProcessInput()` + `UpdateLogic()`
  4. `RacingGameManager.Render()` appelle `Draw()`

### ARCH-004 : Décomposer la classe Sound monolithique
- **Priorité** : Moyenne
- **Description** : `Sound.cs` gère la musique, les effets sonores, le son moteur avec engrenages, les sons de frein — tout en statique.
- **Plan** :
  1. `MusicManager` : gestion de la musique de fond
  2. `SfxManager` : effets sonores ponctuels
  3. `EngineSound` : son moteur avec gestion des engrenages
  4. Conserver une façade `Sound` statique si nécessaire pour la compatibilité

### ARCH-005 : Décomposer la classe Landscape (~1425 lignes)
- **Priorité** : Moyenne
- **Description** : `Landscape.cs` gère le terrain, les objets, les traces de frein, les replays, les checkpoints, la physique et le rendu.
- **Plan** :
  1. `TerrainRenderer` : rendu du terrain et textures
  2. `TrackObjectManager` : placement/rendu des objets de bord de route
  3. `BrakeTrackManager` : gestion des traces de pneus
  4. `CheckpointSystem` : gestion des checkpoints et des tours

---

## Performance (PERF)

### PERF-001 : Stack<IGameScreen> non thread-safe
- **Priorité** : Haute
- **Description** : `gameScreens` (Stack<IGameScreen> statique) est accédé depuis le thread principal et potentiellement modifié depuis d'autres threads sans synchronisation.
- **Plan** : Remplacer par un `ConcurrentStack<T>` ou utiliser `lock` sur toutes les opérations de lecture/écriture.
- **Note** : La situation actuelle est probablement safe car le loading thread ne touche pas `gameScreens`, mais c'est fragile.

### PERF-002 : Allocations fréquentes dans la boucle de rendu
- **Priorité** : Moyenne
- **Description** : `Vector3[]`, `List<...>` créés chaque frame dans `CarPhysics`, `UIRenderer`.
- **Plan** :
  1. Profiler avec PerfView ou dotTrace pour identifier les allocations réelles
  2. Pré-allouer les tableaux en champs de classe
  3. Réutiliser via `Array.Clear()` avant chaque usage

### PERF-005 : RenderToTexture utilise Rgba64 par défaut
- **Priorité** : Basse
- **Description** : Format `SurfaceFormat.Rgba64` pour tous les render targets, même non-HDR.
- **Plan** : Ajouter un paramètre `bool isHDR = false` à `Create()`. Utiliser `SurfaceFormat.Color` par défaut et `Rgba64` uniquement si `isHDR = true`.

---

## Code Quality (QUAL)

### QUAL-001 : Supprimer les directives préprocesseur obsolètes
- **Priorité** : Haute
- **Description** : Blocs `#if !XBOX360`, `#if GAMERSERVICES`, `#if NETFX_CORE`, `#if XBOXONE` dans de nombreux fichiers.
- **Plan** :
  1. Identifier tous les blocs conditionnels avec grep
  2. Supprimer les branches Xbox/GamerServices/NETFX_CORE qui ne s'appliquent plus à MonoGame Desktop
  3. Conserver uniquement `#if WINDOWS` si nécessaire
- **Fichiers concernés** : `Input.cs`, `FileHelper.cs`, `Log.cs`, `ScreenshotCapturer.cs`, `BaseGame.cs`, `GameSettings.cs`, `Replay.cs`

### QUAL-002 : Convertir les champs publics en propriétés
- **Priorité** : Moyenne
- **Description** : Champs publics dans `RacingGameManager.cs` (`currentCarNumber`, `currentCarColor`), `SpringPhysicsObject.cs` (`pos`, `velocity`, `force`), `RandomHelper.cs` (`globalRandomGenerator`).
- **Plan** : Encapsuler chaque champ public en propriété avec getter/setter appropriés.

### QUAL-003 : Réduire l'utilisation excessive des #region
- **Priorité** : Basse
- **Description** : Usage massif de `#region` dans toutes les classes, symptôme de classes trop grandes.
- **Plan** : En parallèle avec ARCH-001/002, extraire des classes plus petites. Les `#region` disparaîtront naturellement.

### QUAL-004 : Extraire les magic numbers en constantes nommées
- **Priorité** : Haute
- **Description** : Centaines de valeurs magiques dans `CarPhysics.cs`, `ChaseCamera.cs`, `Player.cs`, `Landscape.cs`.
- **Plan** :
  1. Identifier chaque valeur magique significative
  2. La remplacer par une constante nommée avec un commentaire explicatif
  3. Valeurs candidates : `2593.0f` (GameOverCameraRotationDivisor), `17.523456789f`, `0.93f` (braking friction), etc.

### QUAL-005 : Décomposer les méthodes trop longues
- **Priorité** : Haute
- **Description** : `CarPhysics.Update()` (~350 lignes), `Options.Render()` (~300 lignes), `Landscape.Render()`, `Track.GenerateVertices()`.
- **Plan** :
  1. `CarPhysics.Update()` → `HandleSteering()`, `HandleAcceleration()`, `HandleBraking()`, `HandleCollisionDetection()`, `UpdatePosition()`
  2. `Options.Render()` → `RenderResolutionOptions()`, `RenderGraphicsOptions()`, `RenderAudioOptions()`

### QUAL-006 : Nettoyer le code mort et les commentaires inutiles
- **Priorité** : Moyenne
- **Description** : Grands blocs de code commenté dans `BaseGame.cs`, `CarSelection.cs`, `Help.cs`, commentaires XML vides.
- **Plan** : Passer en revue chaque fichier et supprimer le code mort identifié.

### QUAL-007 : Implémenter IDisposable correctement
- **Priorité** : Haute
- **Description** : `ShaderEffect` instances statiques jamais disposées, `RenderTarget2D` et `Effect` non nettoyés.
- **Plan** :
  1. Identifier toutes les ressources GPU allouées
  2. Implémenter `Dispose()` dans `BaseGame` avec appel en override de `UnloadContent()`
  3. Utiliser `using` statements ou `Dispose()` explicite pour les effets

---

## Fonctionnalités (FEAT)

### FEAT-003 : Écran de pause en jeu
- **Priorité** : Haute
- **Description** : ESC quitte directement vers le menu. Ajouter un écran de pause.
- **Plan** :
  1. Créer `PauseScreen : IGameScreen`
  2. ESC en jeu → push `PauseScreen` sur le stack
  3. Options : Reprendre, Recommencer, Menu principal
  4. Mettre en pause le son moteur et les positions de physique

### FEAT-004 : Résolutions modernes dans Options
- **Priorité** : Moyenne
- **Description** : Résolutions obsolètes (640x480, 800x600, 1024x768).
- **Plan** :
  1. Utiliser `GraphicsAdapter.DefaultAdapter.SupportedDisplayModes` pour lister les résolutions disponibles
  2. Filtrer les doublons et trier par taille
  3. Afficher dynamiquement dans le menu Options

### FEAT-005 : Afficher le FPS en option
- **Priorité** : Basse
- **Description** : `BaseGame.FPS` est calculé mais jamais affiché.
- **Plan** : Ajouter un booléen `GameSettings.ShowFPS` et afficher `TextureFont.WriteText(fps, ...)` dans une position fixe si activé.

### FEAT-006 : Support des manettes modernes
- **Priorité** : Moyenne
- **Description** : Un seul gamepad (`PlayerIndex.One`), pas de vibration, pas de remapping.
- **Plan** :
  1. Ajouter `GamePad.SetVibration()` lors des collisions
  2. Créer un `InputMapper` pour le remapping
  3. Persister le mapping dans `GameSettings`

### FEAT-008 : Voitures IA adversaires
- **Priorité** : Basse
- **Description** : Jeu solo uniquement. Pas de compétiteurs.
- **Plan** :
  1. Créer `AIPlayer : CarPhysics` qui suit le tracé du circuit
  2. Ajouter un paramètre de difficulté (vitesse max IA, agressivité)
  3. Afficher les voitures IA avec `CarModel.RenderCar()`

### FEAT-009 : Effets visuels de collision
- **Priorité** : Basse
- **Description** : Collisions sans effets visuels.
- **Plan** :
  1. Intégrer un système de particules simple (sparks)
  2. `GamePad.SetVibration()` proportionnel à la force de collision
  3. Flash d'écran lors d'une collision forte

### FEAT-010 : Barre de progression sur l'écran de chargement
- **Priorité** : Basse
- **Description** : Écran de chargement sans feedback quantitatif.
- **Plan** :
  1. Ajouter une propriété `LoadProgress` (0..1) dans `RacingGameManager`
  2. `LoadResources()` met à jour `LoadProgress` à chaque étape
  3. `LoadingScreen.Render()` dessine une barre proportionnelle

---

## Build & Projet (BUILD)

### BUILD-001 : Mettre à jour le TargetFramework
- **Priorité** : Basse
- **Description** : Cibles `net8.0-windows`. Évaluer la mise à jour vers `net9.0-windows` ou suivre les futures LTS.
- **Plan** : Tester la compatibilité MonoGame + mettre à jour les packages NuGet. Corriger les avertissements éventuels.

### BUILD-002 : Ajouter un fichier .editorconfig
- **Priorité** : Moyenne
- **Description** : Pas de style de code uniforme. Mélange tabs/espaces, styles de nommage.
- **Plan** : Créer `.editorconfig` avec règles C# standard, forcer `dotnet format` dans le CI.

### BUILD-003 : Ajouter des tests unitaires
- **Priorité** : Haute
- **Description** : Constructeur `RacingGameManager(string unitTestName)` présent mais aucun projet de test.
- **Plan** :
  1. Créer `RacingGame.Tests` (xUnit)
  2. Tester en priorité : `Highscores`, `Replay` (parse/serialize), `Vector3Helper`, `Input.KeyToChar`, `CarPhysics` (formules)
  3. Mocker `BaseGame` via des interfaces ou abstractions pour tester la physique sans GPU

### BUILD-004 : PipelineExtension — remplacer System.Numerics par Microsoft.Xna.Framework
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
