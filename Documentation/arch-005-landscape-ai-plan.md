# ARCH-005 - Plan d'action IA pour decomposer Landscape

## Constat

- Statut actuel : non implemente.
- La classe `Landscape` centralise encore plusieurs responsabilites dans `RacingGame.Shared/Landscapes/Landscape.cs` :
  - gestion des objets de decor et du feu de depart,
  - chargement et rendu du terrain,
  - replays et checkpoints,
  - traces de frein,
  - coordination avec `Track` et positionnement voiture.
- Aucune classe `TerrainRenderer`, `TrackObjectManager`, `BrakeTrackManager` ou `ReplayManager` n'existe aujourd'hui dans `RacingGame.Shared`.

## Preuves a verifier par l'agent

- `AddObjectToRender`, `ReplaceStartLightObject`, `KillAllLoadedObjects` sont encore dans `Landscape`.
- `CompareCheckpointTime`, `StartNewLap`, `BestReplay`, `NewReplay` sont encore dans `Landscape`.
- `AddBrakeTrack` et `RenderBrakeTracks` sont encore dans `Landscape`.
- `GetMapHeight`, la creation des vertex/index buffers, `Render`, `GenerateShadow` et `UseShadow` sont encore dans `Landscape`.

## Objectif

Reduire `Landscape` a un role de facade et d'orchestrateur, tout en gardant un comportement identique et en limitant le rayon d'impact sur les appelants existants.

## Contraintes

- Garder l'API publique de `Landscape` stable pendant l'extraction.
- Ne pas introduire d'allocations evitables dans les hot paths `Update` et `Render`.
- Preserver l'ordre d'initialisation actuel : terrain, track, objets, replay, position de depart.
- Eviter toute refactorisation hors du perimetre `Landscape` sauf adaptation minimale des points d'appel.
- Valider chaque phase par un build cible de `RacingGame.Shared`.

## Couplages externes a preserver

- `CarPhysics` consomme `AddBrakeTrack`, `UpdateCarTrackPosition`, `GetTrackPositionMatrix`, `CheckpointSegmentPositions`, `CompareCheckpointTime`, `NewReplay`.
- `BasePlayer` consomme `StartNewLap` et `ReplaceStartLightObject`.
- `GameScreen` et `UIRenderer` consomment `Render`, `BestReplay`, `CurrentTrackName`.
- `ShadowMapShader` consomme `GenerateShadow` et `UseShadow`.
- `Track`, `TrackLine`, `GuardRail` et `TrackColumns` consomment `GetMapHeight`, `AddObjectToRender` et `KillAllLoadedObjects`.

## Architecture cible

`Landscape` doit devenir une facade qui coordonne les sous-composants suivants :

- `TerrainRenderer`
  - charge les hauteurs,
  - construit vertices, buffers, materiaux et `PlaneRenderer`,
  - expose `GetMapHeight`, `RenderTerrain`, `RenderShadowReceiver`, `Dispose`.
- `TrackObjectManager`
  - gere `LandscapeObject`, `landscapeModels`, `combos`, `autoGenerationNames`,
  - gere `AddObjectToRender`, `KillAllLoadedObjects`, `ReplaceStartLightObject`,
  - expose les objets proches de la piste pour les ombres et les gros batiments pour le city plane.
- `ReplayManager`
  - gere `bestReplay`, `newReplay`, `StartNewLap`, `CompareCheckpointTime`, `SaveReplay`,
  - orchestre la reinitialisation du replay lors d'un changement de piste,
  - laisse `Track` proprietaire de `CheckpointSegmentPositions`.
- `BrakeTrackManager`
  - gere les vertices de traces, le cache tableau, les limites et le rendu,
  - conserve le comportement actuel de filtrage de distance.

## Sequence recommandee pour l'agent

### ✅ Etape 1 - Geler l'API de facade

- Lister les membres publics et internes de `Landscape` qui sont utilises ailleurs.
- Ajouter les nouveaux sous-composants sans modifier les points d'appel existants.
- Garder `Landscape` comme point d'entree unique pendant toute la migration.
- Realise : points d'extension `TrackObjectManager`, `ReplayManager`, `BrakeTrackManager`, `TerrainRenderer` et `LandscapeObject` ajoutes comme base compilable avant extraction.

### ✅ Etape 2 - Extraire `TrackObjectManager`

- Deplacer `LandscapeObject`, `landscapeModels`, `combos`, `autoGenerationNames`.
- Deplacer `AddObjectToRender`, `KillAllLoadedObjects`, `ReplaceStartLightObject`.
- Exposer :
  - la liste complete des objets a rendre,
  - la sous-liste des objets proches de la piste pour les ombres,
  - l'acces au premier gros batiment pour initialiser le city plane.
- Garder le comportement de correction des noms de modeles et les sons du feu de depart.
- Realise : `LandscapeObject` et la gestion des objets/ombres/feu de depart vivent maintenant dans `TrackObjectManager`, avec delegation conservee dans `Landscape` et compatibilite maintenue pour `landscape.autoGenerationNames`.

### ✅ Etape 3 - Extraire `ReplayManager`

- Deplacer `bestReplay`, `newReplay`, `SaveReplay`, `StartNewLap`, `CompareCheckpointTime`.
- Introduire une methode `ResetForTrack(Level level, Track track)`.
- Laisser `Landscape` exposer `BestReplay`, `NewReplay` et `CompareCheckpointTime` en delegation pour limiter les changements externes.
- Verifier que la sauvegarde asynchrone du replay reste strictement equivalente.
- Realise : la gestion des replays et du changement de tour est deplacee dans `ReplayManager`, sans modifier les consommateurs de `Landscape.BestReplay`, `Landscape.NewReplay` ni `Landscape.CompareCheckpointTime`.

### ✅ Etape 4 - Extraire `BrakeTrackManager`

- Deplacer `brakeTracksVertices`, `brakeTracksVerticesArray`, `lastAddedTrackPos` et les constantes associees.
- Deplacer `AddBrakeTrack` et `RenderBrakeTracks`.
- Conserver le cache tableau actuel pour eviter toute regression dans le rendu.
- Realise : la generation et le rendu des traces sont maintenant portes par `BrakeTrackManager`, avec delegation simple conservee dans `Landscape`.

### ✅ Etape 5 - Extraire `TerrainRenderer`

- Deplacer le chargement de `LandscapeHeights.data` et la generation du maillage.
- Deplacer `mapHeights`, `vertices`, `vertexBuffer`, `indexBuffer`, `mat`, `cityMat`, `cityPlane`.
- Deplacer `GetMapHeight`, `CalcLandscapePos`, `RenderLandscapeVertices` et la logique de rendu du terrain.
- Ajouter une methode d'initialisation du city plane a partir des objets fournis par `TrackObjectManager` apres `ReloadLevel`.
- Realise : `TerrainRenderer` porte maintenant la lecture des hauteurs, le maillage, les buffers, le city plane et les requetes de hauteur; `Landscape` ne fait plus que relayer `GetMapHeight` et orchestrer le rendu global.

### ⏳ Etape 6 - Reduire `Landscape` au role d'orchestrateur

- Conserver dans `Landscape` :
  - `level`,
  - `track`,
  - `ReloadLevel`,
  - `SetCarToStartPosition`,
  - `GetTrackPositionMatrix`,
  - `UpdateCarTrackPosition`,
  - les delegations vers les managers,
  - `Dispose` avec un ordre de liberation explicite.
- A la fin de cette etape, `Landscape` doit surtout cabler les composants et non porter leur logique metier.

### 🧪 Etape 7 - Validation minimale

- Lancer la tache `Build RacingGame.Shared`.
- Verifier au minimum :
  - chargement du jeu sans blocage,
  - rendu du menu 3D ou de la scene de jeu,
  - changement de niveau via `LoadLevel`,
  - completion d'un tour,
  - apparition des traces de frein,
  - rendu d'ombres sans regression evidente.

### ⚠️ Etape 8 - Risques a surveiller

- `Track` et ses sous-types appellent directement `Landscape` pendant la generation des objets ; une extraction trop agressive peut casser cet enchainement.
- `CarPhysics` depend de plusieurs proprietes de `Landscape` dans la boucle de jeu ; il faut privilegier une facade stable avant de retoucher les appelants.
- `cityPlane` depend de la presence d'objets de type batiment ; l'ordre d'initialisation doit rester coherent.
- `Dispose` doit liberer buffers, materiaux, track et modeles dans un ordre stable.

## Definition of done

- `Landscape.cs` devient principalement une facade de coordination.
- Les responsabilites objets, replays, traces de frein et terrain sont chacune dans un fichier dedie.
- Les points d'appel publics existants continuent de fonctionner sans changement fonctionnel visible.
- Le build cible `RacingGame.Shared` passe.

## Livrable attendu de l'agent

- Une serie de petites passes de refactorisation, chacune compilable.
- Un court compte rendu final listant :
  - les classes creees,
  - les delegations conservees dans `Landscape`,
  - les risques restants,
  - le resultat du build cible.