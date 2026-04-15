# Plan IA - Voiture arcade et simulation pour RacingGameCasaEngine

## Objectif

Permettre a `RacingGameCasaEngine` de proposer deux styles de conduite, `Arcade` et `Simulation`, sans dupliquer l'architecture de la voiture ni casser le flow de course, le HUD, la camera, le rendu legacy ou les validations bornees deja en place.

## Cible d'architecture retenue

- une seule entite `RacingCarPawn` comme agregat runtime stable ;
- un seul chassis logique pour la physique du vehicule ;
- quatre roues logiques partagees entre les deux modes ;
- un point d'entree unique pour la dynamique du vehicule ;
- deux solveurs interchangeables : `Arcade` et `Simulation` ;
- pas de version initiale avec `4` rigid bodies et `4` contraintes physiques reelles.

## Hors perimetre de cette iteration

- simulation mecanique detaillee des triangles de suspension ;
- destruction avancee du chassis ou des roues ;
- replication reseau dediee au vehicule ;
- refonte complete du front-end si un simple selecteur de mode suffit.

## Legende de statut

- `⏳` a faire
- `🚧` en cours
- `✅` termine
- `🧪` a valider
- `⚠️` bloque

## Contrat de travail de l'agent

1. Chaque sous-etape de ce plan est une tache committable seule.
2. L'agent doit faire un commit a la fin de chaque sous-etape terminee.
3. Apres chaque commit, l'agent met a jour ce fichier : icone, notes, date si utile.
4. Une sous-etape ne passe a `✅` que si le code compile au minimum sur le perimetre touche.
5. Si une sous-etape est codee mais pas encore verifiee, elle doit passer temporairement a `🧪`.
6. Si une sous-etape revele un manque bloquant, l'agent doit la passer a `⚠️`, creer une sous-etape corrective dans ce plan, traiter ce blocage, committer, puis reprendre la sous-etape initiale.
7. L'agent ne doit pas casser les contrats deja consommes par `RaceHudScreen`, `ChaseCameraRigComponent`, `RaceGameMode`, `RuntimeRaceSession` et `RaceWorldFactory` sans couche de compatibilite.
8. L'agent doit privilegier une migration incrementale avec preservation du comportement `Arcade` existant avant d'introduire le mode `Simulation`.

## Validation minimale transversale

- Build borne obligatoire : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/`
- Si une etape touche le flow de course, le HUD, la camera ou l'initialisation runtime : `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --smoke-frontend`
- Si une etape touche le contact piste, la progression sur circuit ou le ressenti de conduite : `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --capture-track-audit`
- Ne pas utiliser la tache VS Code `Build RacingGame.Shared` pour valider : elle est mal configuree dans ce workspace.

## Notes techniques importantes

- L'architecture a conserver cote jeu est `RacingCarPawn` + composants, pas une refonte vers quatre entites physiques autonomes.
- Le mode `Simulation` vise une approche `chassis unique + roues logiques + suspension`, pas une simulation mecanique exhaustive.
- Les wrappers `IPhysicsWorldContext.WorldRayCast(...)` et `NearBodyWorldRayCast(...)` deleguent actuellement vers des stubs non implementes dans `PhysicsEngine`; si le solveur `Simulation` a besoin de raycasts monde, utiliser directement `world.PhysicsWorldContext.PhysicsEngine.Raycast(...)`.
- Les proprietes runtime deja lues par le HUD et la camera (`CurrentSpeedMph`, `CurrentGear`, `SteeringInput`, `TachometerAcceleration`, ancrages du pawn) doivent rester valides pendant toute la migration.

## Format de commit recommande

- `refactor(racing-casa): extract shared vehicle dynamics contract`
- `refactor(racing-casa): move arcade driving into dedicated solver`
- `feat(racing-casa): add logical wheel model`
- `feat(racing-casa): add simulation vehicle solver foundation`
- `feat(racing-casa): expose driving mode selection`
- `feat(racing-casa): animate wheels from shared vehicle state`
- `test(racing-casa): validate arcade and simulation vehicle modes`

## Plan committable

## ✅ Etape 1 - Geler le contrat commun de la voiture

**But**

Separer l'architecture du vehicule du modele de conduite avant toute nouvelle physique.

**Travail**

- Figer le principe : un seul `RacingCarPawn`, un seul agregat runtime, deux solveurs de conduite.
- Introduire les types de donnees communs a la conduite : input, telemetrie, mode de conduite, definitions de roues, etat runtime minimal.
- Identifier ce qui reste expose par le pawn pour le HUD, la camera, l'audio et le debug.
- Documenter explicitement qu'en `V1`, le mode `Simulation` ne passe pas par `4` rigid bodies relies par contraintes.

**Validation**

- Le projet compile sans changement de comportement visible.
- Le contrat de donnees commun est stable et reutilisable par les deux modes.

**Commits recommandes**

- `refactor(racing-casa): extract shared vehicle dynamics contract`
- `docs(racing-casa): freeze dual-mode vehicle architecture`

**Sous-etapes**

- `✅ 1.1` Introduire `VehicleDrivingMode` et les types de donnees communs du vehicule
- `✅ 1.2` Definir le point d'entree unique de la dynamique du vehicule
- `✅ 1.3` Figer le contrat de telemetrie consomme par le HUD, la camera et le debug runtime

**Notes**

- Realise via `VehicleDynamicsComponent`, `VehicleDrivingMode`, `VehicleControlInput`, `VehicleTelemetrySnapshot`, `VehicleWheelDefinition`, `VehicleWheelRuntimeState` et `VehicleChassisRuntimeState`.
- Le contrat de telemetrie runtime preserve les proprietes deja lues par `RaceHudScreen`, `ChaseCameraRigComponent` et `RuntimeRaceSession`.

## ✅ Etape 2 - Extraire la conduite arcade actuelle derriere un solveur dedie

**But**

Conserver le ressenti actuel tout en supprimant le couplage direct entre `RacingCarPawn` et `ArcadeCarMovementComponent` comme implementation unique.

**Travail**

- Introduire un composant orchestrateur de dynamique vehicule ou une abstraction equivalente.
- Deplacer la logique de `ArcadeCarMovementComponent` dans un solveur `Arcade` dedie.
- Garder le meme branchement des inputs, de la telemetrie, de la camera et du flow de course.
- Preserver le comportement actuel sur piste autant que possible avant tout ajout du mode `Simulation`.

**Validation**

- Build borne du projet.
- Smoke front-end reussi.
- La voiture reste pilotable en mode `Arcade` avec un comportement equivalent au port actuel.

**Commits recommandes**

- `refactor(racing-casa): add vehicle dynamics component`
- `refactor(racing-casa): move arcade driving into dedicated solver`

**Sous-etapes**

- `✅ 2.1` Introduire le composant orchestrateur de dynamique vehicule
- `✅ 2.2` Deplacer la logique arcade existante dans un solveur dedie
- `✅ 2.3` Rebrancher proprement le pawn, les inputs et la telemetrie sur ce nouveau point d'entree
- `✅ 2.4` Verifier la parite de comportement du mode `Arcade`

**Notes**

- `RacingCarPawn` ne branche plus directement `ArcadeCarMovementComponent` ; il expose un point d'entree unique via `VehicleDynamicsComponent`.
- Le solveur `ArcadeVehicleDynamicsSolver` conserve la logique de suivi de piste et remplit aussi l'etat partage des roues et de la telemetrie.

## ✅ Etape 3 - Introduire le modele de roues logiques partage

**But**

Ajouter la couche de donnees necessaire aux roues sans imposer tout de suite une simulation physique complete.

**Travail**

- Creer `4` descripteurs de roue logiques avec position relative, rayon, suspension, braquage, freinage et traction.
- Creer l'etat runtime par roue : contact, compression, rotation, angle de braquage, slip, charge approximate.
- Raccorder ces etats au pawn ou au composant de dynamique sans casser le mode `Arcade`.
- Prevoir un composant visuel dedie a l'animation des roues a partir de cet etat partage.

**Validation**

- Le projet compile.
- Le mode `Arcade` continue de fonctionner.
- Les quatre roues logiques existent au runtime, meme si certaines valeurs sont encore synthetiques au debut.

**Commits recommandes**

- `feat(racing-casa): add logical wheel descriptors`
- `feat(racing-casa): add shared wheel runtime state`

**Sous-etapes**

- `✅ 3.1` Introduire les descripteurs de roues et leur etat runtime partage
- `✅ 3.2` Brancher le mode `Arcade` sur un premier remplissage de cet etat de roue
- `✅ 3.3` Preparer l'ancrage du futur composant d'animation visuelle des roues

**Notes**

- Les `4` roues logiques sont maintenant partagees entre `Arcade` et `Simulation`, avec contact, compression, rotation, braquage, slip et charge approximate.
- `VehicleWheelVisualComponent` exploite les frames `WheelFrontRight`, `WheelBackRight`, `WheelBackLeft` et `WheelFrontLeft` du modele `Car.x` quand elles sont presentes.

## ✅ Etape 4 - Poser la base du solveur simulation

**But**

Introduire une vraie dynamique vehicule simplifiee, partageant la meme architecture de voiture et les memes roues logiques.

**Travail**

- Ajouter un etat dynamique de chassis unique : position, orientation, vitesse lineaire, vitesse angulaire, masse simplifiee.
- Echantillonner le sol sous chaque roue via le profil de piste existant en premiere intention.
- Calculer la suspension par roue : longueur utile, compression, ressort, amortissement.
- Calculer les forces longitudinales et laterales de base puis les agreger sur le chassis.
- Garder une strategie de fallback claire si une roue perd le contact ou si une zone du circuit n'est pas echantillonnable.

**Validation**

- Build borne du projet.
- Audit capture de piste borne si le contact piste evolue.
- Le mode `Simulation` est activable localement et fait rouler la voiture sans casser le flow de course.

**Commits recommandes**

- `feat(racing-casa): add simulation chassis state`
- `feat(racing-casa): add suspension wheel sampling`
- `feat(racing-casa): add simulation tire force solver`

**Sous-etapes**

- `✅ 4.1` Introduire l'etat dynamique du chassis pour le mode `Simulation`
- `✅ 4.2` Echantillonner la piste sous chaque roue et calculer la suspension
- `✅ 4.3` Calculer et appliquer les forces longitudinales et laterales au chassis logique
- `✅ 4.4` Definir les fallbacks hors piste ou en absence de contact exploitable
- `✅ 4.5` Valider le roulage borne du mode `Simulation` sur une piste simple

**Notes**

- Le solveur `SimulationVehicleDynamicsSolver` reste volontairement en `V1` sur `chassis unique + roues logiques + suspension + forces simplifiees`.
- Aucune version de cette iteration ne passe par `4` rigid bodies relies par contraintes physiques reelles ; le contact sol repose sur `RaceTrackPhysicsProfile` et `RaceTrackPhysicsComponent`.

## ✅ Etape 5 - Brancher la configuration de mode de conduite

**But**

Permettre de choisir proprement entre `Arcade` et `Simulation` sans bifurcation sauvage dans le code gameplay.

**Travail**

- Ajouter la configuration de mode dans l'etat front-end ou une configuration runtime equivalente.
- Permettre un choix par defaut stable si aucune option explicite n'est selectionnee.
- Proteger le flow de chargement de course pour qu'un mode invalide ne casse pas la session.
- Prevoir la persistance de l'option si cela reste peu intrusif.

**Validation**

- Build borne du projet.
- Smoke front-end reussi si l'option est exposee a l'interface.
- Le monde de course instancie le solveur attendu selon la configuration.

**Commits recommandes**

- `feat(racing-casa): expose driving mode in runtime state`
- `feat(racing-casa): persist driving mode selection`

**Sous-etapes**

- `✅ 5.1` Introduire la configuration runtime du mode de conduite
- `✅ 5.2` Brancher `RaceWorldFactory` et la creation du pawn sur cette configuration
- `✅ 5.3` Exposer ou persister l'option sans casser le front-end existant

**Notes**

- Le mode de conduite est expose dans `OptionsScreen`, persiste dans `front-end-options.json` et est rebranche jusqu'a `RaceWorldFactory`, `RuntimeRaceWorldBinder` et `RaceGameMode`.
- Le choix par defaut reste `Arcade` si aucune option persistante n'est presente.

## ✅ Etape 6 - Raccorder les roues visuelles et le ressenti commun

**But**

Faire converger rendu et physique autour d'un etat de roue partage, quel que soit le mode de conduite.

**Travail**

- Animer les roues visuelles a partir de la compression, du braquage et de la rotation runtime.
- Garder le corps principal, la camera et le HUD independants du solveur concret.
- Si utile, enrichir la telemetrie commune avec quelques donnees compatibles `Arcade` et `Simulation`.
- Verifier que l'audio moteur futur ou existant pourra lire la meme telemetrie partagée.

**Validation**

- Build borne du projet.
- Smoke front-end reussi.
- Le rendu des roues suit correctement l'etat runtime en `Arcade`, puis en `Simulation`.

**Commits recommandes**

- `feat(racing-casa): animate wheels from shared runtime state`
- `refactor(racing-casa): align camera and hud with shared vehicle telemetry`

**Sous-etapes**

- `✅ 6.1` Introduire le composant d'animation visuelle des roues
- `✅ 6.2` Brancher ce composant sur l'etat de roue partage
- `✅ 6.3` Verifier la neutralite du HUD et de la camera vis-a-vis du mode actif

**Notes**

- Les roues visuelles suivent desormais la compression, le braquage et la rotation fournis par l'etat runtime partage quand le modele legacy est disponible.
- Le HUD et la camera continuent d'utiliser les memes points d'acces du pawn ; ils restent donc neutres par rapport au solveur concret actif.

## ✅ Etape 7 - Stabiliser, comparer et fermer le chantier

**But**

Terminer la mise en place en garantissant que les deux modes sont exploitables et que le mode `Arcade` n'a pas regresse.

**Travail**

- Comparer la telemetrie et le ressenti de base entre `Arcade` et `Simulation`.
- Ajouter un minimum de debug utile pour diagnostiquer contact roue, compression, slip et mode actif.
- Ajuster les valeurs par defaut pour obtenir un mode `Arcade` proche du port actuel et un mode `Simulation` clairement distinct sans etre injouable.
- Mettre a jour ce plan avec les statuts finaux, les notes de validation et les limites connues.

**Validation**

- Build borne du projet.
- Smoke front-end reussi.
- Audit capture de piste borne si les changements de conduite impactent clairement la trajectoire ou la stabilite.

**Commits recommandes**

- `test(racing-casa): validate arcade and simulation vehicle modes`
- `docs(racing-casa): close dual-mode vehicle implementation plan`

**Sous-etapes**

- `✅ 7.1` Ajouter le debug minimum de comparaison entre les deux modes
- `✅ 7.2` Ajuster les valeurs par defaut et verifier l'absence de regression majeure en `Arcade`
- `✅ 7.3` Valider le mode `Simulation` sur le circuit cible de reference
- `✅ 7.4` Clore le plan avec notes de validation, limites connues et suites eventuelles

**Notes**

- Le debug runtime inclut maintenant le mode actif et un resume des `4` roues dans `RuntimeRaceSession.BuildMovementDebugReport()` et dans les echantillons de log de mouvement.
- Validation realisee le `2026-04-15` avec : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/`, `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --smoke-frontend` en `Arcade`, `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --smoke-frontend` en `Simulation`, `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --capture-track-audit` en `Arcade` et en `Simulation`.
- L'option utilisateur persistante a ete remise a `Arcade` apres la validation des deux modes.

## Questions a trancher avant ou pendant l'execution

- Le choix `Arcade` / `Simulation` doit-il etre expose a l'utilisateur final des maintenant ou rester un switch de dev pour la premiere iteration ?
- Le mode `Simulation` doit-il rester contraint au profil de piste existant dans un premier temps, ou faut-il investir tout de suite dans des raycasts monde ?
- Le HUD doit-il rester strictement identique entre les deux modes ou afficher quelques indicateurs supplementaires en `Simulation` ?

## Criteres de fin de chantier

- Le repo propose deux modes de conduite branches sur la meme architecture de voiture.
- `RacingCarPawn` reste l'agregat stable consomme par le reste du runtime.
- Le mode `Arcade` preserve le comportement actuel a ecart borne.
- Le mode `Simulation` apporte un comportement distinct base sur chassis unique + roues logiques.
- Le HUD, la camera, le flow de course et les validations bornees restent operationnels.