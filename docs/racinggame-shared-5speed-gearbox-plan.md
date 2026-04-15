# Plan IA - Boite de vitesse 5 rapports partagee Arcade / Simulation

## Objectif

Introduire une vraie boite automatique a `5` rapports partagee entre `Arcade` et `Simulation` dans `RacingGameCasaEngine`, avec une logique commune de rapports, regime moteur, coupure de couple pendant les changements de rapport et telemetrie coherente pour le HUD, le debug et l'audio.

## Legende de statut

- `⏳` a faire
- `🚧` en cours
- `✅` termine
- `🧪` a valider
- `⚠️` bloque

## Contrat de travail de l'agent

1. Chaque tache ci-dessous doit etre livree par un commit dedie.
2. Apres chaque commit, ce fichier doit etre mis a jour pour refleter le statut reel de la tache.
3. Une tache ne passe a `✅` que si le perimetre touche compile au minimum.
4. Si une tache est codee mais pas encore verifiee, elle passe temporairement a `🧪`.
5. Si un blocage apparait, l'agent passe la tache concernee a `⚠️`, ajoute une tache corrective dans ce plan, la traite, puis reprend le flux principal.
6. Le chantier doit preserver les contrats deja lus par `RacingCarPawn`, `VehicleDynamicsComponent`, `RaceHudScreen` et le debug runtime.
7. La boite `5` rapports doit etre partagee par `Arcade` et `Simulation`; aucune duplication divergente de ratios, seuils de changement de rapport ou calcul de regime n'est autorisee.

## Validation minimale

- Build borne obligatoire : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/`
- Si le ressenti runtime change : `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --smoke-frontend`
- Si la conduite ou la progression piste change : `dotnet run --project RacingGameCasaEngine/RacingGameCasaEngine.csproj -p:BaseOutputPath=artifacts/verify-build/ -- --capture-track-audit`

## Commits recommandes

- `docs(racing-casa): add shared gearbox implementation plan`
- `feat(racing-casa): add shared vehicle transmission model`
- `feat(racing-casa): drive arcade mode with shared gearbox`
- `feat(racing-casa): drive simulation mode with shared gearbox`
- `test(racing-casa): validate shared gearbox integration`

## Taches committables

- `✅ T1` Creer le plan markdown dedie au chantier boite `5` rapports et geler les regles d'execution de l'agent
- `✅ T2` Introduire le contrat partage de transmission : configuration `5` rapports, etat runtime, calcul de regime, logique de changement auto et coupure de couple
- `✅ T3` Brancher la transmission partagee dans `ArcadeVehicleDynamicsSolver` pour remplacer l'acceleration a rapport synthetique
- `🚧 T4` Brancher la meme transmission dans `SimulationVehicleDynamicsSolver` pour remplacer les rapports derives de la vitesse seule
- `⏳ T5` Valider le chantier, mettre a jour ce plan avec le statut final et consigner les commandes de verification executees

## Resultat attendu

- Les deux solveurs utilisent la meme definition de boite `5` rapports.
- `CurrentGear` et `EngineRpm` ne sont plus des valeurs purement synthetiques derivees de la vitesse normalisee.
- Un changement de rapport provoque une courte baisse de couple perceptible mais non bloquante.
- Le comportement `Arcade` reste jouable et le mode `Simulation` garde son identite physique.