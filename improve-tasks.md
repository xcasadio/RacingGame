# RacingGame - Améliorations et Corrections de Bugs

**Légende** : ✅ Implémenté | 📋 Tâche future (voir [future-tasks.md](future-tasks.md))

**Résumé** : 21 tâches ✅ implémentées (12 bugs, 5 robustesse, 2 performance, 2 qualité) • 22 tâches 📋 déférées

---

## Bugs

### ✅ BUG-001 : GameSettings Save/Load entièrement commenté (critique)
- **Fichier** : `RacingGame.Shared/GameSettings.cs` (lignes 95-145, 170-195)
- **Description** : Tout le code de sérialisation/désérialisation XML dans `Load()` et `Save()` est commenté. Les paramètres du jeu (résolution, volume, highscores, etc.) ne sont jamais persistés sur disque. Chaque redémarrage remet les valeurs par défaut.
- **Correction** : Implémenter la sauvegarde/chargement avec `System.Xml.Serialization.XmlSerializer` et `IsolatedStorage` ou un fichier local dans le répertoire utilisateur.

### ✅ BUG-002 : Replay Save entièrement commenté
- **Fichier** : `RacingGame.Shared/GameLogic/Replay.cs` (méthode `Save()`)
- **Description** : La méthode `Save()` du replay ne fait rien — le code `StorageDevice` est commenté. Les meilleurs replays ne sont jamais sauvegardés entre sessions.
- **Correction** : Implémenter la sauvegarde binaire des replays dans un fichier local.

### ✅ BUG-003 : LoadEvent déclenché avec un cast invalide
- **Fichier** : `RacingGame.Shared/RacingGameManager.cs` (ligne ~378)
- **Description** : `LoadEvent("Models...", null)` envoie un `string` comme `sender`, mais le handler dans `LoadingScreen.cs` fait `(string)sender`. Ce pattern est fragile et non-conventionnel (le `sender` d'un event devrait être l'objet source, pas les données).
- **Correction** : Utiliser un `EventArgs` dérivé contenant le message status, ou un `EventHandler<string>` / `Action<string>`.

### ✅ BUG-004 : SpriteBatch créé à chaque frame dans LoadingScreen
- **Fichier** : `RacingGame.Shared/GameScreens/LoadingScreen.cs` (méthode `Render()`)
- **Description** : Un nouveau `SpriteBatch` est instancié à chaque appel de `Render()` sans être utilisé ni disposé. Fuite de mémoire potentielle.
- **Correction** : Supprimer la variable `textBatch` inutilisée ou la créer une seule fois dans le constructeur.

### ✅ BUG-005 : MouseInBoxRelative calcule un Rectangle incorrect
- **Fichier** : `RacingGame.Shared/GameLogic/Input.cs` (méthode `MouseInBoxRelative`)
- **Description** : Le `Rectangle` passé à `MouseInBox` utilise `rect.Right` et `rect.Bottom` comme largeur/hauteur au lieu de calculer la vraie largeur/hauteur. `rect.Right = rect.X + rect.Width`, ce qui double les coordonnées lors du scaling.
- **Correction** : Utiliser `rect.Width` et `rect.Height` au lieu de `rect.Right` et `rect.Bottom`.

### ✅ BUG-006 : KeyToChar inverse les caractères ',' et '.'
- **Fichier** : `RacingGame.Shared/GameLogic/Input.cs` (méthode `KeyToChar`)
- **Description** : `Keys.OemComma` retourne `'.'` en mode normal et `'<'` avec Shift. `Keys.OemPeriod` retourne `','` en mode normal et `'>'` avec Shift. Les deux sont inversés.
- **Correction** : `OemComma` → `','` (sans shift) / `'<'` (shift) ; `OemPeriod` → `'.'` (sans shift) / `'>'` (shift).

### ✅ BUG-007 : Fullscreen toujours forcé à false
- **Fichier** : `RacingGame.Shared/Graphics/BaseGame.cs` (méthode `ApplyResolutionChange`)
- **Description** : `graphicsManager.IsFullScreen = false;` est hardcodé avec le commentaire `//GameSettings.Default.Fullscreen;`. L'option fullscreen dans le menu Options n'a aucun effet.
- **Correction** : Restaurer `graphicsManager.IsFullScreen = GameSettings.Default.Fullscreen;`.

### ✅ BUG-008 : Variable `trackNum` shadow dans Replay.GetCarMatrixAtTime
- **Fichier** : `RacingGame.Shared/GameLogic/Replay.cs` (méthode `GetCarMatrixAtTime`)
- **Description** : La variable locale `trackNum` dans `GetCarMatrixAtTime` masque le champ `trackNum` de la classe. Pas un bug fonctionnel dans ce cas, mais source de confusion.
- **Correction** : Renommer la variable locale (ex: `matrixIndex`).

### ✅ BUG-009 : Typo "WobbelCamera" au lieu de "WobbleCamera"
- **Fichiers** : `RacingGame.Shared/GameLogic/ChaseCamera.cs`, `CarPhysics.cs`
- **Description** : La méthode `WobbelCamera` et les variables associées (`cameraWobbelTimeoutMs`, `cameraWobbelFactor`) contiennent une faute d'orthographe.
- **Correction** : Renommer en `WobbleCamera`, `cameraWobbleTimeoutMs`, `cameraWobbleFactor`.

### ✅ BUG-010 : Player.Update() contient du code de rendu
- **Fichier** : `RacingGame.Shared/GameLogic/Player.cs` (méthode `Update()`)
- **Description** : La méthode `Update()` appelle `TextureFont.WriteText/WriteTextCentered` pour afficher le texte de victoire/défaite. Cela viole la séparation Update/Render et peut produire des résultats imprévisibles si `Update()` est appelé à fréquence différente de `Render()`.
- **Correction** : Déplacer l'affichage du texte de victoire/game-over dans une méthode `Render()` dédiée.

### ✅ BUG-011 : downPressed redéfini dans le scope de braking
- **Fichier** : `RacingGame.Shared/GameLogic/CarPhysics.cs` (dans la section Handle speed)
- **Description** : La variable `downPressed` est d'abord déclarée comme `bool` dans le bloc du freinage, puis réassignée à `true` à la fin du `if`. La réassignation n'a aucun effet car `downPressed` est déjà vérifié plus haut et le scope finit juste après.
- **Correction** : Supprimer la réassignation inutile `downPressed = true;`.

### ✅ BUG-012 : ScreenshotCapturer.Update() ne fait rien
- **Fichier** : `RacingGame.Shared/GameLogic/ScreenshotCapturer.cs`
- **Description** : La méthode `Update()` ne contient que `base.Update(gameTime)`. Il n'y a aucun code pour capturer des screenshots. La fonctionnalité screenshot est incomplète/cassée.
- **Correction** : Implémenter la capture d'écran réelle ou supprimer la classe si non utilisée.

---

## Améliorations d'Architecture

### 📋 ARCH-001 : Trop de champs et méthodes statiques
- **Fichiers** : `RacingGameManager.cs`, `BaseGame.cs`, `Input.cs`, `Sound.cs`, `ShaderEffect.cs`
- **Description** : Quasiment tout est statique : `player`, `landscape`, `carModel`, `gameScreens`, les matrices, l'UI, etc. Cela rend le code non-testable, crée un couplage fort entre toutes les classes et empêche tout scénario multi-instance.
- **Amélioration** : Introduire un pattern de service locator ou d'injection de dépendances. Convertir les champs statiques en champs d'instance avec accès via un contexte de jeu partagé.

### 📋 ARCH-002 : Héritage profond BasePlayer → CarPhysics → ChaseCamera → Player
- **Fichiers** : `BasePlayer.cs`, `CarPhysics.cs`, `ChaseCamera.cs`, `Player.cs`
- **Description** : Chaîne d'héritage à 4 niveaux. La caméra hérite de la physique de la voiture, ce qui est conceptuellement incorrect. La caméra devrait suivre la voiture, pas en hériter.
- **Amélioration** : Favoriser la composition. Séparer `CarPhysics`, `ChaseCamera` et `Player` en passant des références entre eux.

### 📋 ARCH-003 : Pas de séparation MVC/game loop dans les GameScreens
- **Fichiers** : Tous les fichiers dans `GameScreens/`
- **Description** : Les méthodes `Render()` des GameScreens gèrent à la fois l'input utilisateur, la logique de jeu et le rendu. Par exemple, `MainMenu.Render()` gère la navigation clavier/souris ET dessine les boutons.
- **Amélioration** : Séparer la logique d'input/état dans `Update()` et le dessin pur dans `Render()`.

### 📋 ARCH-004 : Classe Sound monolithique
- **Fichier** : `RacingGame.Shared/Sounds/Sound.cs`
- **Description** : Tout le code audio (musique, effets, gestion du son moteur) est dans une seule classe statique. Mélange responsabilités de gestion des fichiers et de playback.
- **Amélioration** : Séparer en `MusicManager`, `SfxManager`, `EngineSound`, etc.

### 📋 ARCH-005 : Classe Landscape trop large
- **Fichier** : `RacingGame.Shared/Landscapes/Landscape.cs` (~1425 lignes)
- **Description** : Gère le terrain, les objets, les traces de frein, les replays, les checkpoints, la physique de positionnement, le rendu 3D, les ombres...
- **Amélioration** : Extraire en sous-classes : `TerrainRenderer`, `TrackObjectManager`, `BrakeTrackManager`, `ReplayManager`.

---

## Améliorations de Performance

### 📋 PERF-001 : Stack<IGameScreen> non thread-safe
- **Fichier** : `RacingGame.Shared/RacingGameManager.cs`
- **Description** : `gameScreens` est un `Stack<IGameScreen>` statique accédé sans synchronisation depuis le thread principal et potentiellement d'autres (loading thread).
- **Amélioration** : Utiliser `ConcurrentStack<T>` ou ajouter un mécanisme de verrou.

### 📋 PERF-002 : Allocations fréquentes dans la boucle de rendu
- **Fichiers** : `CarPhysics.cs`, `Input.cs`, `UIRenderer.cs`
- **Description** : Création de `Vector3[]`, `new List<Keys>()`, `new SpriteBatch()` dans les méthodes appelées chaque frame. Cela cause une pression GC inutile.
- **Amélioration** : Pré-allouer les tableaux et listes comme champs de classe et les réutiliser.

### ✅ PERF-003 : keysPressedLastFrame recréé chaque frame
- **Fichier** : `RacingGame.Shared/GameLogic/Input.cs` (méthode `Update()`)
- **Description** : `keysPressedLastFrame = new List<Keys>(keyboardState.GetPressedKeys())` alloue un nouveau `List<Keys>` à chaque frame.
- **Amélioration** : Réutiliser un `HashSet<Keys>` pré-alloué et le remplir via `Clear()` + `AddRange()`.

### ✅ PERF-004 : GetAngleBetweenVectors ne clamp pas le dot product
- **Fichier** : `RacingGame.Shared/Helpers/Vector3Helper.cs`
- **Description** : `Math.Acos(Vector3.Dot(vec1, vec2))` peut retourner `NaN` si le dot product dépasse [-1, 1] à cause d'erreurs de précision flottante. Cela peut causer des comportements imprévisibles dans la collision.
- **Amélioration** : Clamper le résultat : `Math.Acos(MathHelper.Clamp(Vector3.Dot(vec1, vec2), -1f, 1f))`.

### 📋 PERF-005 : RenderToTexture utilise Rgba64 par défaut
- **Fichier** : `RacingGame.Shared/Shaders/RenderToTexture.cs` (méthode `Create()`)
- **Description** : Le format `SurfaceFormat.Rgba64` est utilisé pour tous les render targets, même les targets de post-processing à basse résolution. Cela double la mémoire GPU utilisée par rapport à `SurfaceFormat.Color`.
- **Amélioration** : Utiliser `SurfaceFormat.Color` pour les targets non-HDR et `Rgba64`/`HalfVector4` uniquement pour le HDR.

---

## Améliorations de Code Quality

### 📋 QUAL-001 : Précompilateur XBOX360 obsolète
- **Fichiers** : `Input.cs`, `FileHelper.cs`, `Log.cs`, `ScreenshotCapturer.cs`, etc.
- **Description** : De nombreux blocs `#if !XBOX360`, `#if GAMERSERVICES`, `#if NETFX_CORE` encombrent le code. Ces plateformes ne sont plus pertinentes avec MonoGame.
- **Amélioration** : Supprimer tous les blocs de compilation conditionnelle Xbox 360, GamerServices et NETFX_CORE. Garder uniquement le code Windows/Desktop.

### 📋 QUAL-002 : Champs publics au lieu de propriétés
- **Fichiers** : `RacingGameManager.cs` (`currentCarNumber`, `currentCarColor`, `colorSelectionTexture`), `SpringPhysicsObject.cs` (`pos`, `velocity`, `force`), `RandomHelper.cs` (`globalRandomGenerator`)
- **Description** : Des champs sont exposés publiquement au lieu d'utiliser des propriétés, violant l'encapsulation.
- **Amélioration** : Convertir les champs publics en propriétés avec accesseurs appropriés.

### 📋 QUAL-003 : Regions excessives
- **Fichiers** : Tous
- **Description** : Utilisation excessive de `#region` / `#endregion` pour structurer le code. Signe que les classes sont trop grandes.
- **Amélioration** : Réduire les regions en extrayant des classes/méthodes plus petites.

### 📋 QUAL-004 : Magic numbers omniprésents
- **Fichiers** : `CarPhysics.cs`, `ChaseCamera.cs`, `Player.cs`, `CarSelection.cs`
- **Description** : Nombreuses constantes numériques en dur : `0.93f`, `2.5f`, `1.125f`, `17.523456789f`, `2593.0f`, etc. Très difficile à comprendre ou ajuster.
- **Amélioration** : Extraire chaque magic number en constante nommée avec une documentation claire.

### 📋 QUAL-005 : Méthodes trop longues
- **Fichiers** : `CarPhysics.Update()` (~350 lignes), `Options.Render()` (~300 lignes), `Landscape.Render()`, `Track.GenerateVertices()`
- **Description** : Des méthodes monolithiques de plusieurs centaines de lignes, difficiles à lire, tester et maintenir.
- **Amélioration** : Extraire en sous-méthodes : `HandleSteering()`, `HandleAcceleration()`, `HandleBraking()`, `HandleCollision()`, etc.

### 📋 QUAL-006 : Commentaires inutiles et code mort
- **Fichiers** : `GameSettings.cs`, `BaseGame.cs`, `CarSelection.cs`, `Help.cs`, `Options.cs`
- **Description** : Grands blocs de code commenté (ex: `// UWP COMMENT OUT`, ancien code StorageDevice), commentaires type `/// <returns>Bool</returns>` qui n'ajoutent rien.
- **Amélioration** : Supprimer tout le code mort commenté. Améliorer la documentation XML pour qu'elle soit descriptive.

### 📋 QUAL-007 : Pas de gestion du Dispose/IDisposable
- **Fichiers** : `RacingGameManager.cs`, `ShaderEffect.cs`, `RenderToTexture.cs`
- **Description** : `ShaderEffect` implémente `IDisposable` mais les instances statiques (`lineRendering`, `lighting`, `normalMapping`, etc.) ne sont jamais disposées. Les `RenderTarget2D` et `Effect` ne sont pas correctement nettoyés.
- **Amélioration** : Implémenter un cycle de vie propre avec `Dispose()` appelé à la fermeture du jeu.

### ✅ QUAL-008 : Classe Directories et Vector3Helper non-static mais constructeur privé
- **Fichiers** : `RacingGame.Shared/Helpers/Directories.cs`, `RacingGame.Shared/Helpers/Vector3Helper.cs`
- **Description** : Classes non-statiques avec constructeur privé pour empêcher l'instanciation — c'est un pattern obsolète en C# moderne.
- **Amélioration** : Déclarer les classes comme `static class`.

### ✅ QUAL-009 : SoundsDirectory utilise des chemins hardcodés Windows
- **Fichier** : `RacingGame.Shared/Helpers/Directories.cs`
- **Description** : `Path.Combine(GameBaseDirectory, "Content\\Audio")` utilise le séparateur backslash Windows.
- **Amélioration** : Utiliser `Path.Combine("Content", "Audio")` pour la portabilité.

---

## Améliorations Fonctionnelles

### ✅ FEAT-001 : Ajouter la sauvegarde/chargement réel des paramètres
- **Description** : Réimplémenter `GameSettings.Load()` et `GameSettings.Save()` avec sérialisation JSON ou XML vers un fichier local.
- **Priorité** : Haute

### ✅ FEAT-002 : Ajouter la sauvegarde/chargement des replays
- **Description** : Réimplémenter `Replay.Save()` pour persister les meilleurs replays entre sessions.
- **Priorité** : Haute

### 📋 FEAT-003 : Ajouter un système de pause en jeu
- **Description** : Actuellement, ESC quitte directement vers le menu. Ajouter un écran de pause avec reprise, redémarrage et retour au menu.
- **Priorité** : Moyenne

### 📋 FEAT-004 : Supporter des résolutions modernes dans les Options
- **Fichier** : `RacingGame.Shared/GameScreens/Options.cs`
- **Description** : Les résolutions proposées sont 640x480, 800x600, 1024x768, 1280x1024, Auto. Pas de support pour 1920x1080, 2560x1440, 3840x2160 ni les ratios 16:9/21:9.
- **Amélioration** : Détecter dynamiquement les résolutions supportées par le moniteur et les proposer dans le menu.

### 📋 FEAT-005 : Ajouter un compteur FPS en option
- **Description** : Le code calcule déjà `fpsLastSecond` dans `BaseGame` mais ne l'affiche nulle part.
- **Amélioration** : Ajouter une option pour afficher le FPS dans un coin de l'écran.

### 📋 FEAT-006 : Améliorer la prise en charge des manettes modernes
- **Fichier** : `RacingGame.Shared/GameLogic/Input.cs`
- **Description** : Le code ne gère qu'un seul gamepad (`PlayerIndex.One`). Pas de support pour le remapping des touches, ni pour les gamepads multiples, ni vibration (rumble).
- **Amélioration** : Ajouter un système de mapping d'input configurable et le support de la vibration.

### ✅ FEAT-007 : Implémenter la fonctionnalité de capture d'écran
- **Fichier** : `RacingGame.Shared/GameLogic/ScreenshotCapturer.cs`
- **Description** : La classe est un squelette sans fonctionnalité. La recherche de numéro de fichier existe, mais rien ne capture réellement l'écran.
- **Amélioration** : Implémenter la capture via `RenderTarget2D.SaveAsPng()` ou `SaveAsJpeg()` déclenchée par une touche (ex: F12).

### 📋 FEAT-008 : Ajouter un mode de difficulté / IA adversaire
- **Description** : Le jeu ne propose qu'une voiture fantôme (replay). Pas d'adversaires IA ni de mode multijoueur local.
- **Amélioration** : Ajouter des voitures IA simples qui suivent le tracé du circuit.

### 📋 FEAT-009 : Améliorer le feedback de collision
- **Fichier** : `RacingGame.Shared/GameLogic/CarPhysics.cs`
- **Description** : Les collisions avec les guard rails ne produisent qu'un son et un léger changement de direction. Pas de particle effects, pas de vibration manette.
- **Amélioration** : Ajouter des effets visuels (particules, étincelles) et vibration gamepad lors des collisions.

### 📋 FEAT-010 : Améliorer l'écran de chargement
- **Fichier** : `RacingGame.Shared/GameScreens/LoadingScreen.cs`
- **Description** : L'écran de chargement ne montre qu'un texte animé et un statut textuel. Pas de barre de progression.
- **Amélioration** : Ajouter une barre de progression et potentiellement un aperçu du circuit sélectionné.

---

## Améliorations de Robustesse

### ✅ ROB-001 : Pas de validation des index dans les highscores
- **Fichier** : `RacingGame.Shared/GameScreens/Highscores.cs`
- **Description** : `ReadHighscoresFromSettings()` ne vérifie pas correctement le format des données sérialisées. Un fichier corrompu peut causer un crash avec un `IndexOutOfRangeException` ou `FormatException`.
- **Amélioration** : Ajouter un try/catch autour du parsing et valider les champs avant utilisation.

### ✅ ROB-002 : ManualResetEvent peut causer des deadlocks
- **Fichier** : `RacingGame.Shared/Helpers/FileHelper.cs`
- **Description** : `StorageContainerMRE.WaitOne()` suivi de `StorageContainerMRE.Reset()` dans `GameSettings.Load/Save` et `Replay.Load/Save`. Si une exception est levée entre `Reset()` et `Set()`, le MRE reste non-signalé → deadlock.
- **Amélioration** : Utiliser un pattern `try/finally` pour garantir l'appel à `Set()`, ou remplacer par un `SemaphoreSlim` / `lock`.

### ✅ ROB-003 : Thread de chargement sans gestion d'erreur
- **Fichier** : `RacingGame.Shared/RacingGameManager.cs`
- **Description** : `LoadResources()` est exécuté sur un thread séparé sans mécanisme de capture d'exceptions. Une erreur de chargement de modèle/texture crashera silencieusement le thread et le jeu restera bloqué sur l'écran de chargement.
- **Amélioration** : Envelopper le contenu du thread dans un try/catch, stocker l'exception et la signaler au thread principal.

### ✅ ROB-004 : Pas de vérification de nullité pour carTextures
- **Fichier** : `RacingGame.Shared/RacingGameManager.cs` (propriété `NumberOfCarTextureTypes`)
- **Description** : `carTextures.Length` sera `NullReferenceException` si appelé avant la fin du chargement.
- **Amélioration** : Ajouter une vérification null, ou retarder l'accès jusqu'à la fin du chargement.

### ✅ ROB-005 : Conversion MphToMeterPerSec incorrecte
- **Fichier** : `RacingGame.Shared/GameLogic/CarPhysics.cs`
- **Description** : `MeterPerSecToMph = 1.609344f * ((60*60)/1000)` = `1.609344 * 3.6` ≈ `5.794` ce qui fait de `MphToMeterPerSec` ≈ `0.1726`. La conversion réelle de mph vers m/s est `0.44704`. Le facteur semble être un ratio personnalisé pour le gameplay, non une vraie conversion physique. Ceci est trompeur car les noms suggèrent une conversion réelle.
- **Amélioration** : Documenter clairement que ce sont des facteurs de gameplay et non des conversions physiques réelles, ou corriger pour utiliser les vraies valeurs si l'intention est d'être physiquement réaliste.

---

## Améliorations de Build & Projet

### 📋 BUILD-001 : Mettre à jour vers .NET 8+ (ou .NET 9)
- **Fichiers** : tous les `.csproj`
- **Description** : Les projets ciblent `net8.0-windows`. Envisager la mise à jour vers la dernière version LTS.
- **Amélioration** : Mettre à jour le `TargetFramework` et les packages NuGet.

### 📋 BUILD-002 : Pas de fichier .editorconfig
- **Description** : Pas de configuration de style de code partagée. Le code mixe des styles d'indentation et de formatage.
- **Amélioration** : Ajouter un `.editorconfig` avec les conventions C# standard.

### 📋 BUILD-003 : Pas de tests unitaires
- **Description** : Le constructeur `RacingGameManager(string unitTestName)` existe mais aucun projet de test n'est présent.
- **Amélioration** : Créer un projet de tests (xUnit/NUnit) pour la physique, les highscores, les replays, etc.

### 📋 BUILD-004 : PipelineExtension référence System.Numerics.Vector2/3/4
- **Fichier** : `RacingGame.PipelineExtension/RacingGameModelProcessor.cs`
- **Description** : Le fichier utilise `using System.Numerics;` et `ConvertFloatsToBestType` retourne `Vector2`, `Vector3`, `Vector4` depuis `System.Numerics` au lieu des types MonoGame (`Microsoft.Xna.Framework`). Cela peut causer des erreurs de sérialisation au runtime.
- **Amélioration** : Utiliser les types `Microsoft.Xna.Framework.Vector2/3/4` dans le content pipeline.
