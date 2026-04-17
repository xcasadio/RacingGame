# Plan IA - Stratégie d'update des entités CasaEngine

## Objectif

Implémenter dans `CasaEngine` une stratégie d'update d'entités explicite, stable et extensible, pour remplacer le pilotage ad hoc actuel et éviter que des entités immobiles paient un coût CPU à chaque frame.

Le modèle cible doit s'appuyer sur les axes suivants :

- `Mobility` : `Static`, `Movable`
- `TickPolicy` : `Never`, `Conditional`, `EveryFrame`
- `SpatialPolicy` : `StaticIndex`, `DynamicIndex`
- `RenderDynamicPolicy` : `Static`, `MaterialAnimated`, `GeometryAnimated`

## Motivation

Le cas du circuit Expert a montré que le moteur parcourait et entretenait trop d'entités statiques dans `World.Update(...)`, alors que leur transform, leur index spatial et leur rendu ne changeaient pas d'une frame à l'autre.

Le but de ce plan n'est pas d'ajouter une heuristique fragile basée sur quelques propriétés, mais de mettre en place un modèle moteur où :

1. l'auteur décrit l'intention de l'objet ;
2. le moteur compile cette intention en stratégie runtime ;
3. les boucles d'update, de spatial et de rendu utilisent cette stratégie de manière cohérente.

## Principes directeurs

- Les politiques runtime doivent être explicites et orthogonales.
- `RenderDynamicPolicy` ne doit pas, à lui seul, forcer un tick gameplay.
- `TickPolicy.Conditional` doit reposer sur des signaux moteur explicites, pas sur une déduction implicite du type "matériau inchangé donc pas d'update".
- Le moteur peut proposer des valeurs par défaut intelligentes, mais une intention explicite doit primer.
- Les optimisations doivent préserver le comportement observable avant d'optimiser l'architecture plus loin.
- Si une limitation structurelle apparaît en cours de route, l'agent doit créer une tâche dédiée plutôt que de diluer le problème dans une étape plus large.

## Légende de statut

- `⬜` à faire
- `🟨` en cours
- `✅` terminé
- `⛔` bloqué

## Contrat de travail de l'agent

1. L'agent doit faire un commit à la fin de chaque tâche terminée.
2. Immédiatement après chaque commit, l'agent doit mettre à jour ce fichier :
   - l'icône de statut de la tâche ;
   - les notes utiles si le périmètre a changé ;
   - les sous-tâches créées en cours de route si nécessaire.
3. Une tâche ne passe à `✅` que si la validation bornée définie pour cette tâche est passée.
4. Si une tâche révèle un prérequis ou un blocage, l'agent doit :
   - passer la tâche à `⛔` ou la laisser à `🟨` avec une note explicite ;
   - créer une nouvelle sous-tâche ou une nouvelle étape dédiée ;
   - traiter ce prérequis via un commit séparé.
5. Les commits doivent rester petits, lisibles et réversibles.
6. L'agent ne doit pas batcher plusieurs tâches de ce plan dans un seul commit.
7. Si une étape touche à la fois le moteur et `RacingGameCasaEngine`, le commit doit rester centré sur une intention unique.

## Format de commit recommandé

- `docs(entity-update): define policy invariants`
- `feat(entity-update): add policy enums and entity state`
- `feat(entity-update): route world update through tick policy`
- `feat(entity-update): split static and dynamic spatial maintenance`
- `feat(entity-update): migrate race entities to semantic policies`
- `docs(entity-update): record validation guidance`

## Validation minimale transversale

- Build borné moteur : `dotnet build CasaEngine/CasaEngine/CasaEngine.csproj`
- Build borné jeu d'intégration : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj`
- Si une étape ajoute des tests, l'agent doit lancer un test filtré et borné sur la classe de test créée ou modifiée.
- Si une étape modifie le comportement runtime visible sur le circuit Expert, l'agent doit demander ou produire une validation overlay ciblée au lieu d'un run ouvert et non borné.

## Règles d'architecture à geler dès le début

- `Mobility` décrit l'hypothèse de mouvement de transform, pas le besoin de rendu dynamique.
- `TickPolicy` décrit la participation à la boucle `World.Update(...)`.
- `SpatialPolicy` décrit la stratégie d'indexation et de maintenance spatiale.
- `RenderDynamicPolicy` décrit le besoin de traitement dynamique côté rendu, pas la mobilité de l'entité.
- Une voiture peut donc être `Movable + EveryFrame + DynamicIndex + Static` si sa géométrie et son matériau sont stables mais que son transform bouge.
- Un mesh de décor peut être `Static + Never + StaticIndex + Static`.
- Un objet à matériau animé peut être `Static + Never + StaticIndex + MaterialAnimated`.
- Ne pas introduire d'inférence opaque du type : physique statique + matériau constant = plus de tick. Cela peut servir de défaut, pas de vérité moteur implicite.

## Plan committable

## ✅ Étape 1 - Auditer l'état actuel et figer les invariants

**But**

Transformer le booléen ad hoc actuel en modèle cible clair, sans perdre de comportement ni multiplier les heuristiques.

**Travail**

- Cartographier le chemin actuel de l'update dans :
  - `Entity`
  - `World`
  - `SceneComponent`
  - composants de rendu / spatial influents
- Recenser les usages actuels du contournement existant (`UpdatesEnabled` ou équivalent transitoire).
- Définir une table d'intention cible pour les principaux types d'entités :
  - décor statique
  - checkpoints fixes
  - player start
  - voiture joueur
  - caméra de poursuite
  - composants de debug
- Geler la sémantique exacte des quatre axes (`Mobility`, `TickPolicy`, `SpatialPolicy`, `RenderDynamicPolicy`).
- Documenter explicitement ce que `Conditional` veut dire dans ce plan.

**Validation**

- Il n'y a plus d'ambiguïté architecturale sur le sens des quatre politiques.
- Les cas d'usage RacingGameCasaEngine principaux sont couverts par une table d'intention simple.

**Commit**

- `docs(entity-update): define policy invariants`

**Sous-tâches**

- `✅ 1.1` Inventorier la boucle d'update actuelle et ses points de coût
- `✅ 1.2` Recenser les usages du contournement runtime existant
- `✅ 1.3` Figer la sémantique des quatre politiques
- `✅ 1.4` Définir la table d'intention des entités RacingGameCasaEngine

## ✅ Étape 2 - Introduire les types de politique et l'état porté par l'entité

**But**

Créer un vocabulaire moteur explicite au lieu d'un booléen monolithique.

**Travail**

- Ajouter les enums cibles dans le moteur.
- Ajouter à l'entité un état structuré de politique runtime.
- Définir des valeurs par défaut stables pour éviter une régression immédiate.
- Prévoir une compatibilité transitoire avec le mécanisme actuel si nécessaire, mais en le traitant comme une étape de migration, pas comme l'API finale.
- S'assurer que le clonage, le chargement, l'initialisation et les factories propagent correctement ces politiques.

**Validation**

- `CasaEngine` compile.
- `RacingGameCasaEngine` compile.
- Le runtime démarre avec les valeurs par défaut sans changement fonctionnel non voulu.

**Commit**

- `feat(entity-update): add policy enums and entity state`

**Sous-tâches**

- `✅ 2.1` Ajouter `Mobility`, `TickPolicy`, `SpatialPolicy`, `RenderDynamicPolicy`
- `✅ 2.2` Ajouter l'état de politique sur `Entity`
- `✅ 2.3` Propager les politiques au clonage et à l'initialisation
- `✅ 2.4` Ajouter un shim de compatibilité transitoire si indispensable

## ✅ Étape 3 - Définir la résolution runtime effective des politiques

**But**

Éviter que les boucles runtime dépendent directement d'un mélange de champs hétérogènes et centraliser la décision d'exécution.

**Travail**

- Introduire une résolution explicite du comportement effectif de l'entité :
  - update ce frame ou non ;
  - maintenance spatiale dynamique ou non ;
  - besoin de chemin de rendu dynamique ou non.
- Définir le contrat de `TickPolicy.Conditional` :
  - par exemple via des signaux exposés par les composants ;
  - ou via un agrégat explicite de besoins runtime.
- Geler les combinaisons valides, tolérées ou suspectes.
- Interdire conceptuellement le couplage implicite entre rendu dynamique et tick gameplay.

**Validation**

- La logique de résolution est centralisée et testable.
- Les combinaisons de politiques ont un comportement déterministe documenté.

**Commit**

- `feat(entity-update): add effective runtime policy resolution`

**Sous-tâches**

- `✅ 3.1` Créer l'API de résolution des politiques effectives
- `✅ 3.2` Définir le contrat de `Conditional`
- `✅ 3.3` Geler la matrice des combinaisons valides et suspectes
- `✅ 3.4` Vérifier que `RenderDynamicPolicy` reste orthogonal au tick gameplay

## ✅ Étape 4 - Refactorer `World.Update(...)` autour de `TickPolicy`

**But**

Sortir les entités statiques et les entités sans besoin d'update de la boucle chaude principale.

**Travail**

- Modifier `World.Update(...)` pour passer par la résolution effective des politiques.
- Préserver les comportements de destruction, d'ajout différé, de `BeginPlay` et de scripts gameplay.
- S'assurer qu'une entité `Never` ne paie pas le coût d'un `Update(...)` frame par frame.
- Introduire si nécessaire des chemins séparés pour :
  - `EveryFrame`
  - `Conditional`
  - `Never`

**Validation**

- Les builds moteur et jeu passent.
- Les comportements gameplay dynamiques connus restent fonctionnels.
- Une entité marquée `Never` n'est plus parcourue dans la boucle d'update active.

**Commit**

- `feat(entity-update): route world update through tick policy`

**Sous-tâches**

- `✅ 4.1` Remplacer la décision booléenne actuelle par la résolution de `TickPolicy`
- `✅ 4.2` Préserver les chemins de destruction et d'ajout différé
- `✅ 4.3` Introduire un traitement distinct pour `Conditional`
- `✅ 4.4` Vérifier qu'aucune entité `Never` ne ticke encore par erreur

## ✅ Étape 5 - Refactorer la maintenance spatiale autour de `SpatialPolicy`

**But**

Éviter les recalculs et déplacements d'index spatial inutiles pour les entités statiques.

**Travail**

- Distinguer la maintenance des entités `StaticIndex` et `DynamicIndex`.
- Réserver les `Move(...)` et recalculs fréquents aux entités dynamiques ou conditionnelles réellement sales.
- Vérifier l'impact sur :
  - insertion initiale dans l'index ;
  - déplacement ;
  - suppression ;
  - parenting / hiérarchie si concerné.
- Clarifier l'interaction entre `Mobility` et `SpatialPolicy` sans les confondre.

**Validation**

- Les entités de décor statique ne déclenchent plus de maintenance spatiale frame par frame.
- Les entités dynamiques continuent à être correctement déplacées dans l'index.

**Commit**

- `feat(entity-update): split static and dynamic spatial maintenance`

**Sous-tâches**

- `✅ 5.1` Séparer la maintenance `StaticIndex` / `DynamicIndex`
- `✅ 5.2` Réduire les recalculs de bounds inutiles pour le statique
- `✅ 5.3` Vérifier ajout, move et remove dans l'index spatial
- `✅ 5.4` Documenter l'articulation entre `Mobility` et `SpatialPolicy`

## ✅ Étape 6 - Brancher les composants et la signalisation de `Conditional`

**But**

Faire de `Conditional` un contrat moteur explicite plutôt qu'une valeur décorative.

**Travail**

- Permettre à des composants ou à l'entité de signaler un besoin d'update conditionnel.
- Migrer les composants moteur évidents :
  - caméras qui suivent un acteur ;
  - composants physiques ;
  - composants à animation géométrique ;
  - composants purement statiques.
- Éviter qu'un composant purement visuel statique force un tick gameplay.
- Documenter la règle : quel type de composant demande `EveryFrame`, `Conditional` ou `Never`.

**Validation**

- `Conditional` correspond à un besoin runtime observable, pas à une heuristique floue.
- Les composants dynamiques critiques gardent leur comportement.

**Commit**

- `feat(entity-update): wire component conditional tick signals`

**Sous-tâches**

- `✅ 6.1` Ajouter la signalisation conditionnelle côté composant ou entité
- `✅ 6.2` Migrer les composants runtime dynamiques du moteur
- `✅ 6.3` Vérifier que les composants purement statiques ne forcent pas de tick
- `✅ 6.4` Documenter la règle de choix entre `Never`, `Conditional` et `EveryFrame`

## ✅ Étape 7 - Brancher `RenderDynamicPolicy` sans recoupler le gameplay

**But**

Permettre au rendu de distinguer statique, animation de matériau et animation de géométrie, sans réintroduire un coût de tick monde inutile.

**Travail**

- Définir comment `RenderDynamicPolicy` influence le pipeline de rendu, les invalidations ou les caches.
- S'assurer qu'un objet `Static + Never + StaticIndex + MaterialAnimated` reste possible.
- Traiter les cas où la géométrie ou le matériau changent sans mouvement de transform.
- Vérifier que les composants `StaticModel` statiques restent sur un chemin de rendu compatible avec l'optimisation CPU côté `World.Update(...)`.

**Validation**

- Le rendu conserve un chemin clair pour les objets statiques et les objets visuellement dynamiques.
- Le choix du chemin de rendu ne force pas un tick gameplay global.

**Commit**

- `feat(entity-update): integrate render dynamic policy`

**Sous-tâches**

- `✅ 7.1` Définir l'effet concret de `RenderDynamicPolicy` sur le runtime de rendu
- `✅ 7.2` Vérifier le cas `MaterialAnimated` sans tick gameplay
- `✅ 7.3` Vérifier le cas `GeometryAnimated`
- `✅ 7.4` Garantir l'orthogonalité entre rendu dynamique et update monde

## ✅ Étape 8 - Migrer les call sites RacingGameCasaEngine vers les politiques sémantiques

**But**

Remplacer les contournements spécifiques au jeu par des déclarations d'intention exploitables par le moteur.

**Travail**

- Migrer les factories et constructeurs du jeu qui manipulent aujourd'hui le comportement d'update de manière ad hoc.
- Définir explicitement les politiques des objets principaux du circuit :
  - route, terrain, garde-fous, colonnes, décor : `Static + Never + StaticIndex + Static`
  - checkpoints et player start : politiques explicites cohérentes avec leurs usages runtime
  - voiture joueur : politique dynamique complète
  - caméra de poursuite et composants debug : politiques adaptées à leur comportement réel
- Supprimer les usages jeu du vieux contournement dès qu'ils sont couverts par le nouveau modèle.

**Validation**

- `RacingGameCasaEngine` compile.
- Les call sites du jeu expriment une intention métier claire.
- Le circuit Expert ne dépend plus d'un booléen local pour éviter son coût CPU principal.

**Commit**

- `feat(entity-update): migrate race entities to semantic policies`

**Sous-tâches**

- `✅ 8.1` Migrer `LegacyTrackSceneFactory`
- `✅ 8.2` Migrer `RaceWorldFactory`
- `✅ 8.3` Migrer les entités dynamiques du gameplay course
- `✅ 8.4` Retirer les usages jeu du contournement transitoire

## ✅ Étape 9 - Exposer les politiques à l'authoring et à la persistance

**But**

Respecter le modèle des moteurs modernes où l'intention peut être posée explicitement dans les données d'authoring, puis conservée au chargement et à la sauvegarde.

**Travail**

- Ajouter une surface d'authoring claire pour les politiques runtime :
  - soit directement sur `Entity` dans l'inspector du world editor ;
  - soit via une surface équivalente explicitement justifiée si l'intégration éditeur complète doit être différée.
- Brancher la sérialisation et le chargement des politiques dans les assets de monde / entité.
- Définir les valeurs par défaut proposées par le moteur pour les familles de composants ou d'entités courantes.
- Vérifier que l'intention explicite de l'auteur prime toujours sur les défauts moteur.
- Documenter la frontière entre :
  - intention d'authoring ;
  - défauts proposés par le moteur ;
  - comportement runtime effectivement résolu.

**Validation**

- Les politiques sont éditables ou au minimum sérialisables via un chemin d'authoring explicite.
- Les entités sauvegardées et rechargées conservent leurs politiques.
- Les défauts moteur existent, mais ne masquent pas une intention explicitement définie.

**Commit**

- `feat(entity-update): expose policies to authoring and persistence`

**Sous-tâches**

- `✅ 9.1` Ajouter la surface d'édition des politiques côté éditeur ou authoring
- `✅ 9.2` Sérialiser et recharger les politiques dans les assets de monde / entité
- `✅ 9.3` Définir les défauts moteur par famille d'entités ou de composants
- `✅ 9.4` Garantir que l'intention explicite prime sur les défauts moteur

## ✅ Étape 10 - Ajouter les garde-fous de diagnostic et de cohérence

**But**

Rendre les erreurs de configuration visibles et faciliter la validation des gains CPU.

**Travail**

- Ajouter des compteurs ou diagnostics de debug pour visualiser la répartition des entités par politique.
- Ajouter des warnings ou assertions sur les combinaisons incohérentes ou suspectes.
- Éviter les erreurs silencieuses où une entité reste en `EveryFrame` faute de policy explicite.
- Préparer un support minimal de comparaison avant / après sur le circuit Expert.

**Validation**

- Le debug runtime permet de voir combien d'entités relèvent de chaque classe de politique.
- Les combinaisons suspectes sont détectées tôt.

**Commit**

- `feat(entity-update): add policy diagnostics and guardrails`

**Sous-tâches**

- `✅ 10.1` Ajouter les compteurs de diagnostic par politique
- `✅ 10.2` Ajouter les warnings/assertions sur les combinaisons suspectes
- `✅ 10.3` Préparer la comparaison runtime sur le circuit Expert

## ✅ Étape 11 - Nettoyer la transition et documenter l'usage final

**But**

Sortir du mode transitoire et laisser un modèle moteur compréhensible pour les prochaines migrations.

**Travail**

- Supprimer ou déprécier le shim transitoire une fois tous les call sites migrés.
- Documenter la politique par défaut attendue pour les principaux types d'entités.
- Ajouter des exemples d'usage côté moteur et côté jeu.
- Écrire une note finale de validation indiquant le résultat attendu sur le circuit Expert et les limites restantes.

**Validation**

- Le contournement transitoire n'est plus la voie normale d'utilisation.
- La documentation finale permet de créer une entité statique ou dynamique sans ambiguïté.

**Commit**

- `docs(entity-update): finalize policy migration guidance`

**Sous-tâches**

- `✅ 11.1` Supprimer ou déprécier le shim transitoire
- `✅ 11.2` Documenter les politiques par défaut par famille d'entités
- `✅ 11.3` Ajouter des exemples d'usage moteur et jeu
- `✅ 11.4` Rédiger la note finale de validation Expert

## Résultat attendu à la fin du plan

- Les entités immobiles ne participent plus inutilement à `World.Update(...)`.
- La maintenance spatiale statique et dynamique est séparée proprement.
- Le rendu dynamique n'impose plus un tick gameplay par couplage implicite.
- `RacingGameCasaEngine` exprime l'intention de ses entités de manière sémantique.
- Le circuit Expert devient un cas de validation normal du moteur, pas un cas spécial traité par un booléen local.

## Notes de clôture

- Le shim `UpdatesEnabled` est conservé uniquement comme compatibilité transitoire et marqué obsolète ; les call sites `RacingGameCasaEngine` ont été migrés vers `ApplyExplicitPolicies(...)`.
- Le stockage et l'édition des policies passent désormais par `Entity.Policies` ; la façade historique sur `Entity` est conservée pour compatibilité, mais n'est plus la voie privilégiée en interne.
- `Conditional` est maintenant piloté par des signaux explicites (`IConditionalEntityUpdateSource`) au lieu d'une déduction implicite côté moteur.
- `RenderDynamicPolicy` est branché au runtime de rendu via l'invalidation des vues `OnDemand`, sans forcer de tick gameplay global.

### Défauts moteur documentés

- `StaticModelComponent` et `PlayerStartComponent` : `Static + Never + StaticIndex + Static`
- `AnimatedSpriteComponent` et `TileMapComponent` : `Static + Conditional + StaticIndex + MaterialAnimated`
- `SkinnedMeshComponent` : `Movable + EveryFrame + DynamicIndex + GeometryAnimated`
- `PhysicsBaseComponent`, `CameraLookAtComponent`, `CameraTargeted2dComponent`, `SteeringAgentComponent`, `SteeringPhysicsBridgeComponent`, `StaticSpriteComponent` : `Movable + EveryFrame + DynamicIndex + Static`
- Fallback moteur sans signal spécifique : `Movable + EveryFrame + DynamicIndex + Static`

### Exemples d'usage

- Statique explicite côté moteur ou jeu : `entity.ApplyExplicitPolicies(EntityPolicySet.StaticDecoration);`
- Dynamique explicite côté jeu : `entity.ApplyExplicitPolicies(EntityPolicySet.DynamicDefault);`
- Retour aux défauts moteur : `entity.PolicySourceMode = EntityPolicySourceMode.EngineDefault;`

### Validation bornée réalisée

- `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj --filter FullyQualifiedName~EntityPolicyResolverTests` : `6/6` tests réussis
- `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj` : réussi
- `dotnet build CasaEngine/CasaEngine/CasaEngine.csproj` : réussi
- `dotnet build CasaEngine/CasaEngine.Editor/CasaEngine.Editor.csproj` : réussi