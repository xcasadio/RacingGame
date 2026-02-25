# RacingGame
MonoGame port (3.8.*) of the Racing Game starter kit

>Note: All content and source code downloaded from this page is bound to the Microsoft Permissive License (Ms-PL).

## Améliorations apportées

Ce fork apporte une série de corrections et d'améliorations par rapport au portage MonoGame original.

**Corrections de bugs**
- Sauvegarde/chargement des paramètres (résolution, volumes, highscores) désormais fonctionnels via XML.
- Sauvegarde des meilleurs replays entre sessions.
- Mode plein écran réellement appliqué depuis les Options.
- `MouseInBoxRelative` corrigé (calcul de Rectangle erroné).
- `KeyToChar` : inversion des caractères `,` et `.` corrigée.
- Suppression des allocations mémoire par frame (`Vector3[]` de collision, `List<Keys>` d'input).
- `GetAngleBetweenVectors` : dot product clampé pour éviter les `NaN`.
- Séparation Update/Render dans `Player` (le texte victoire/game-over n'est plus dessiné dans `Update`).
- Gestion du `ManualResetEvent` avec `try/finally` pour éviter les deadlocks.
- Gestion des exceptions sur le thread de chargement.
- Capture d'écran implémentée (touche F12 → PNG horodaté).

**Architecture & qualité**
- Chaîne d'héritage réduite : `ChaseCamera` extrait de `CarPhysics`, devenu composition dans `Player`.
- Séparation `Update(GameTime)` / `Render()` dans tous les GameScreens via `IGameScreen`.
- `Sound` décomposé en `MusicManager`, `SfxManager` et `EngineSound`.
- Stack des écrans protégée par verrou (`_screenLock`) pour la thread-safety.
- Suppression de tous les blocs préprocesseur Xbox 360 / GamerServices / UWP obsolètes.
- Champs publics convertis en propriétés ; magic numbers extraits en constantes nommées.
- `IDisposable` correctement implémenté (`ShaderEffect`, `BaseGame`, `RacingGameManager`).
- `RenderToTexture` : `SurfaceFormat.Color` par défaut au lieu de `Rgba64` (moitié moins de mémoire GPU).

**Fonctionnalités**
- Résolutions dynamiques dans les Options (tirées de `GraphicsAdapter.SupportedDisplayModes` ; préfère le 16:9 moderne).
- Affichage du FPS configurable (option dans le menu, activable en jeu).
- Vibration manette sur les collisions (glancing 0,35 / frontal 0,85, durée variable, option On/Off).
- Barre de progression sur l'écran de chargement avec animation de vague sur tous les textes.

## Description

The Racing Game Starter Kit is a complete game. This sample comes ready to compile and run, and it's easy to customize with a little bit of C# programming. You are free to use the source code as the basis for your own XNA Game Studio projects, and to share your work with others.
Racing Game is a 3D auto racing game that features advanced graphics, audio, and input processing. Race around the track and try to beat the ghost car to achieve the best time.

## Screenshot
![image 1](/github/XNA_Racing-Game_01_small.jpg)
![image 1](/github/XNA_Racing-Game_02_small.jpg)
![image 1](/github/XNA_Racing-Game_03_small.jpg)

