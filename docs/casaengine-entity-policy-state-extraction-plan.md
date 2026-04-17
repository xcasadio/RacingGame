# Plan IA - Extraction de l'etat de policy hors de Entity

## Objectif

Extraire hors de `Entity` tout ce qui releve de l'etat mutable et du cache des policies dans un objet dedie accessible via `Entity.Policies`, sans partial class, sans manager global, et sans changer le comportement observable de la strategie d'update deja mise en place.

Le modele cible doit respecter les contraintes suivantes :

- l'API reste centree sur l'entite ;
- `EntityPolicyResolver` reste stateless ;
- `Entity` conserve son role de facade metier et de proprietaire ;
- l'editeur et la serialisation lisent et ecrivent les policies via `Entity.Policies` ;
- le shim legacy `UpdatesEnabled` reste transitoirement disponible, mais il delegue a l'etat extrait.

## Motivation

L'extraction actuelle a deja separe :

- le vocabulaire des policies ;
- la logique de resolution runtime.

En revanche, `Entity` porte encore :

- les champs d'authoring des policies ;
- le cache de `EntityPolicySet` configure ;
- l'override legacy `UpdatesEnabled` ;
- le flag de demande d'update conditionnelle ;
- les helpers de dirtying et de clear.

Cette responsabilite est coherente fonctionnellement, mais elle alourdit `Entity` alors que cet etat peut etre regroupe dans un objet dedie et cohesif.

## Principes directeurs

- Pas de partial class.
- Pas de service global ou de manager externe.
- `EntityPolicyState` est un state object, pas un deuxieme resolver.
- `EntityPolicyResolver` continue a calculer le comportement effectif a partir d'une `Entity`.
- `Entity` garde la facade publique de haut niveau pour eviter une rupture brutale de l'API.
- Le nouvel acces recommande aux donnees brutes de policy passe par `Entity.Policies`.
- Le refactor doit preserver strictement :
  - la resolution runtime ;
  - l'editeur ;
  - la serialisation ;
  - les tests existants ;
  - les builds bornes.

## Legende de statut

- `⬜` a faire
- `🟨` en cours
- `✅` termine
- `⛔` bloque

## Validation minimale transversale

- Build borne moteur : `dotnet build CasaEngine/CasaEngine/CasaEngine.csproj`
- Build borne editeur : `dotnet build CasaEngine/CasaEngine.Editor/CasaEngine.Editor.csproj`
- Build borne jeu d'integration : `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj`
- Test filtre minimal : `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj --filter FullyQualifiedName~EntityPolicyResolverTests`

## Architecture cible

### Nouveau type cible

Introduire une nouvelle classe :

- `EntityPolicyState`

Cette classe porte uniquement :

- les donnees de policy configurees ;
- l'etat runtime transitoire lie aux policies ;
- le cache du `ConfiguredPolicySet`.

### Ce qui doit rester dans `Entity`

`Entity` doit rester proprietaire de :

- son cycle de vie ;
- sa hierarchie ;
- ses composants ;
- son `World` ;
- sa facade publique de haut niveau.

`Entity` doit continuer a exposer :

- `Policies`
- `ApplyExplicitPolicies(...)`
- `UseEnginePolicyDefaults()`
- `RequestConditionalUpdate()`
- `GetEffectivePolicySet()`
- `GetResolvedPolicies()`
- `UpdatesEnabled` en facade obsolete de compatibilite

### Ce qui doit sortir de `Entity`

Deplacer dans `EntityPolicyState` :

- `PolicySourceMode`
- `Mobility`
- `TickPolicy`
- `SpatialPolicy`
- `RenderDynamicPolicy`
- `LegacyUpdatesEnabledOverride`
- `HasPendingConditionalUpdateRequest`
- le dirty flag du cache
- le `ConfiguredPolicySet` mis en cache
- les helpers associes

### Ce qui ne doit pas etre fait

- Ne pas stocker un manager global des policies.
- Ne pas faire de partial class `Entity`.
- Ne pas deplacer dans `EntityPolicyState` la logique de parcours des composants.
- Ne pas faire de `EntityPolicyState` un objet autonome qui connait le monde entier.
- Ne pas casser la facade actuelle de `Entity` dans le meme refactor.

## Plan committable

## ✅ Etape 1 - Introduire `EntityPolicyState`

**But**

Creer le nouvel objet de state avant de rebrancher `Entity` dessus.

**Travail**

- Creer un nouveau fichier `EntityPolicyState.cs`.
- Deplacer dans cette classe l'etat brut des policies et le cache associe.
- Definir les valeurs par defaut identiques au comportement actuel.
- Ajouter les helpers de mutation locale :
  - application d'un `EntityPolicySet` explicite ;
  - retour au mode `EngineDefault` ;
  - demande et clear d'update conditionnel ;
  - override legacy `UpdatesEnabled` ;
  - invalidation du cache.
- Ajouter un mecanisme de clonage du state.
- Verifier que la classe reste independante de l'editeur et du monde.

**Validation**

- `CasaEngine` compile.
- Les tests existants compilent.
- Aucun changement fonctionnel n'est encore visible.

**Sous-taches**

- `✅ 1.1` Creer `EntityPolicyState`
- `✅ 1.2` Deplacer l'etat brut et le cache
- `✅ 1.3` Ajouter les helpers de mutation
- `✅ 1.4` Ajouter le clonage du state

## ✅ Etape 2 - Rebrancher `Entity` sur `Policies`

**But**

Faire de `Entity` une facade qui delegue le stockage et le cache des policies au nouvel objet.

**Travail**

- Ajouter une propriete `Policies` sur `Entity`.
- Initialiser `Policies` dans le constructeur par defaut.
- Copier `Policies` dans le constructeur de clonage.
- Remplacer les champs de policy actuels de `Entity` par des delegations vers `Policies`.
- Conserver la facade publique existante :
  - `PolicySourceMode`
  - `Mobility`
  - `TickPolicy`
  - `SpatialPolicy`
  - `RenderDynamicPolicy`
  - `UpdatesEnabled`
  - `ApplyExplicitPolicies(...)`
  - `UseEnginePolicyDefaults()`
  - `RequestConditionalUpdate()`
  - `GetEffectivePolicySet()`
  - `GetResolvedPolicies()`
- Supprimer de `Entity` les helpers internes qui n'ont plus lieu d'etre une fois la delegation faite.
- Garder `Entity` comme point d'entree ergonomique pour le reste du moteur.

**Validation**

- `CasaEngine` compile.
- Le comportement public de `Entity` est identique pour les call sites existants.
- Le clone d'une entite conserve correctement ses policies.

**Sous-taches**

- `✅ 2.1` Ajouter `Entity.Policies`
- `✅ 2.2` Deleguer les proprietes publiques
- `✅ 2.3` Deleguer les methodes publiques de policy
- `✅ 2.4` Nettoyer les champs et helpers supprimes de `Entity`

## ✅ Etape 3 - Rebrancher le runtime sur `Entity.Policies`

**But**

Faire en sorte que le runtime lise l'etat extrait au lieu des anciens membres internes de `Entity`.

**Travail**

- Modifier `EntityPolicyResolver` pour lire :
  - `entity.Policies.SourceMode`
  - `entity.Policies.LegacyUpdatesEnabledOverride`
  - `entity.Policies.HasPendingConditionalUpdateRequest`
  - `entity.Policies.GetConfiguredPolicySet(entity)`
- Retirer les acces internes devenus obsoletes sur `Entity`.
- Rebrancher `World` et l'update des enfants pour clear l'update conditionnel via `Policies`.
- Verifier que les diagnostics et warnings conservent le meme comportement.
- Verifier que l'invalidation de cache se produit toujours quand :
  - le root component change ;
  - un composant est ajoute ;
  - un composant est supprime ;
  - une valeur explicite de policy change.

**Validation**

- `EntityPolicyResolverTests` passent.
- `CasaEngine` compile.
- `RacingGameCasaEngine` compile.
- Aucun changement de comportement sur la decision `ShouldUpdateThisFrame`.

**Sous-taches**

- `✅ 3.1` Migrer `EntityPolicyResolver`
- `✅ 3.2` Migrer `World` et les clears conditionnels
- `✅ 3.3` Verifier l'invalidation du cache
- `✅ 3.4` Supprimer les acces internes obsoletes sur `Entity`

## ✅ Etape 4 - Rebrancher la serialisation et l'editeur

**But**

Faire en sorte que l'authoring lise et ecrive les policies via `Entity.Policies`, pas via des champs disperses sur `Entity`.

**Travail**

- Modifier `EditorEntityJsonSerializer` pour lire les valeurs depuis `entity.Policies`.
- Modifier `Entity.Load(...)` pour recharger les valeurs dans `Policies`.
- Modifier `EntityDetailsPanel` pour :
  - afficher `entity.Policies.*`
  - ecrire `entity.Policies.*`
  - garder l'affichage du runtime effectif via `entity.GetResolvedPolicies()`
- Verifier que le mode `Explicit` continue a activer ou desactiver les combos comme aujourd'hui.
- Verifier que les assets existants restent compatibles.

**Validation**

- `CasaEngine.Editor` compile.
- Les entites sauvegardees et rechargees conservent les memes policies.
- L'inspector continue a refleter correctement l'etat configure et l'etat effectif.

**Sous-taches**

- `✅ 4.1` Migrer la sauvegarde
- `✅ 4.2` Migrer le chargement
- `✅ 4.3` Migrer l'inspector entite
- `✅ 4.4` Verifier la compatibilite des donnees existantes

## ✅ Etape 5 - Renforcer les tests de non-regression

**But**

Proteger le refactor contre les regressions de cache, de clonage et de compatibilite.

**Travail**

- Etendre `EntityPolicyResolverTests`.
- Ajouter des tests cibles pour verifier :
  - qu'un `EntityPolicyState` clone ne partage pas son etat mutable ;
  - que `Entity.UpdatesEnabled` delegue correctement a l'override legacy extrait ;
  - que `Entity.GetEffectivePolicySet()` et `Entity.GetResolvedPolicies()` continuent a fonctionner ;
  - que le clear de demande conditionnelle se fait toujours apres update ;
  - que le chargement JSON restaure correctement `Policies`.
- Garder les tests bornes et filtres.

**Validation**

- `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj --filter FullyQualifiedName~EntityPolicyResolverTests` passe.
- Les nouveaux tests couvrent explicitement l'objet extrait.

**Sous-taches**

- `✅ 5.1` Ajouter un test de clonage du state
- `✅ 5.2` Ajouter un test de delegation legacy
- `✅ 5.3` Ajouter un test de clear conditionnel
- `✅ 5.4` Ajouter un test de restauration JSON apres extraction

## Resultat attendu a la fin du plan

- `Entity` est sensiblement plus cohesive.
- L'etat des policies est regroupe dans un objet dedie accessible via `Entity.Policies`.
- Le resolver reste stateless et centre sur le calcul, pas sur le stockage.
- L'editeur et la serialisation utilisent le nouvel objet de state.
- La separation est faite sans partial class et sans manager global.
- Le comportement runtime observable reste inchange.

## Notes de cloture

- La separation a ete faite via une nouvelle classe `EntityPolicyState` accessible par `Entity.Policies`.
- `Entity` conserve une facade de compatibilite (`PolicySourceMode`, `Mobility`, `TickPolicy`, `SpatialPolicy`, `RenderDynamicPolicy`, `UpdatesEnabled`, `ApplyExplicitPolicies(...)`, `UseEnginePolicyDefaults()`, `RequestConditionalUpdate()`, `GetEffectivePolicySet()`, `GetResolvedPolicies()`).
- `EntityPolicyResolver` reste stateless et calcule toujours le comportement effectif a partir de l'entite et de ses composants.
- Le runtime (`World` et l'update des enfants), la serialisation editeur et l'inspector ont ete recables sur `entity.Policies`.
- Aucun partial class n'a ete introduit.

## Validation bornee realisee

- `dotnet test CasaEngine/CasaEngine.Tests/CasaEngine.Tests.csproj --filter FullyQualifiedName~EntityPolicyResolverTests` : `6/6` tests reussis
- `dotnet build CasaEngine/CasaEngine/CasaEngine.csproj` : reussi
- `dotnet build CasaEngine/CasaEngine.Editor/CasaEngine.Editor.csproj` : reussi
- `dotnet build RacingGameCasaEngine/RacingGameCasaEngine.csproj` : reussi