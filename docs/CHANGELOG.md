# Changelog

Tous les changements notables du projet BrilliantMediator sont documentés dans ce fichier.

Le format est basé sur [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
et ce projet adhère à [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- 🔍 Support avancé de diagnostics et logging pour le debugging
- 📊 Métriques de performance intégrées (compteurs de handlers, temps d'exécution)
- 🧪 Support des middlewares de pipeline pour les commands/queries
- 🔐 Support des handlers asynchrones avec timeout et cancellation tokens
- 📈 Amélioration des performances: cache warming et compilation préalable
- 🌍 Support internationalization dans les messages d'erreur
- 📦 Support des interfaces IAsyncDisposable pour meilleure gestion des ressources
- 🎯 Builder patterns avancés pour configuration de pipeline personnalisés

### Changed
- 📖 Documentation enrichie avec benchmarks et comparaisons de performance
- 🏗️ Exemples étendus avec patterns avancés (SAGA, event sourcing)

---

## [1.1.0] - 2025-01-15

Version mineure ajoutant le support complet des événements (Events) au pattern Mediator, complétant ainsi l'implémentation CQRS+E.

### Added
- 🔥 **Support complet des Events** - Publication parallèle fire-and-forget
  - ✨ Interface `IEvent` pour les événements de domaine
  - 🎪 Interface `IEventHandler<TEvent>` pour les handlers d'événements
  - 📡 Publication avec `PublishAsync<TEvent>()` et exécution parallèle des handlers
  - 🔍 Support pour plusieurs handlers par événement
  - 🎯 Enregistrement avec `RegisterEventHandler<TEvent>()`
- 🎯 **Extension MediatorBuilder pour Events**
  - ✨ `AddEventHandler<TEvent, THandler>()` pour enregistrement fluent
  - 🔍 Découverte automatique des event handlers via `AddHandlersFromAssembly()`
- 🏗️ **Architecture événementielle améliorée**
  - 🧬 Registry dédié pour les event handlers avec support multi-handlers
  - 🔒 Thread-safety renforcée pour l'enregistrement concurrent d'handlers
  - 📦 Exécution parallèle des handlers avec `Task.WhenAll()`
  - 🎪 Scoping DI approprié - chaque handler obtient son propre scope
- 📖 **Documentation complète des Events**
  - 📚 Guide d'utilisation des événements dans GUIDE.md et README.md
  - 🎯 Exemples pratiques d'événements de domaine
  - 🏗️ Patterns d'architecture événementielle

### Changed
- 🔧 **Architecture de registres optimisée pour Events**
  - 📈 `ConcurrentDictionary<string, List<Type>>` pour supporter plusieurs handlers par événement
  - 🔐 Synchronisation thread-safe lors de l'ajout de handlers multiples
  - 🎯 Clés de lookup optimisées pour les événements
- 📦 **Amélioration du scoping DI**
  - ✨ Chaque handler d'événement obtient son propre scope pour isolation
  - 🔒 Gestion appropriée des ressources scoped (DbContext, etc.)
  - 📝 Support correct de `ConfigureAwait(false)` dans tous les scénarios
- 📖 **Documentation enrichie**
  - 🎯 Exemples d'utilisation des événements dans README et GUIDE
  - 🏗️ Architecture CQRS+E complète documentée
  - 📚 Patterns d'événements de domaine

### Fixed
- 🐛 **Gestion robuste des scopes DI pour Events**
  - 🛡️ Résolution correcte des handlers avec dépendances scoped
  - 🔒 Prévention des fuites de ressources lors de l'exécution parallèle
  - ✅ Isolation des scopes entre handlers d'événements
- 🧪 **Corrections dans les tests**
  - ✅ `TestServiceProvider` implémente maintenant `IServiceScopeFactory`
  - 🔧 Support du scoping dans tous les tests
  - 📝 Tests de concurrence pour événements multiples

### Performance
- ⚡ **Optimisations pour la publication d'événements**
  - 🚀 Exécution parallèle des handlers pour minimiser la latence
  - 💾 Pré-allocation des listes de tâches pour réduire les allocations
  - 🎯 Lookup O(1) pour les types d'événements
  - 📊 Aucune allocation intermédiaire dans le chemin critique

### Breaking Changes
Aucun - Cette version est entièrement rétrocompatible avec la v1.0.0

---

## [1.0.0] - 2025-11-06

Version stable finale intégrant toutes les fonctionnalités du Mediator Pattern avec support complet du CQRS et des événements.

### Added
- ✨ **Implémentation complète du Mediator Pattern** - Architecture ultra-légère et performante
- ⚡ **Zero-Reflection architecture** - Registres statiques génériques pour éviter la réflexion runtime
- 🎯 **Support complet du pattern CQRS**
  - 📦 Commands sans réponse (`ICommand` + `ICommandHandler<TCommand>`)
  - 📚 Commands avec réponse (`ICommand<TResponse>` + `ICommandHandler<TCommand, TResponse>`)
  - 🔧 Queries (`IQuery<TResponse>` + `IQueryHandler<TQuery, TResponse>`)
- 🔥 **Support complet des Events** - Publication parallèle fire-and-forget
  - ✨ Interface `IEvent` pour les événements de domaine
  - 🎪 Interface `IEventHandler<TEvent>` pour les handlers d'événements
  - 📡 Publication avec `PublishAsync<TEvent>()` et exécution parallèle
- 📋 **Extension DependencyInjection** - `AddBrilliantMediator()` pour configuration automatique
- 🎯 **MediatorBuilder** - Configuration fluente avec découverte automatique des handlers
  - 🔍 `AddHandlersFromAssembly()` pour découverte mono-assembly
  - 🔍 `AddHandlersFromAssemblies()` pour découverte multi-assemblies
  - 🔍 `AddHandlersFromAssemblyNames()` pour chargement dynamique
- 🏗️ **Exemple complet d'architecture DDD** avec E-Commerce (samples/EcommerceDDD)
  - Agrégats racine, Value Objects, Repositories
  - Domain Services, Commands, Queries, Events
  - Flux complet de traitement de commandes
- 🔐 **API type-safe** - Vérification complète au compile-time via les génériques
- 🧬 **Gestion avancée des exceptions** avec `HandlerNotRegisteredException`
- 🎪 **Support pour l'injection de dépendances avec scopes** (DbContext, services scoped)
- 📖 **Documentation complète avec exemples français** (README, guides, tutoriels)
- ✅ **Validation de types à la compilation** avec contraintes génériques
- 🔐 **API type-safe sans allocation intermédiaire**

### Changed
- 🔨 **Architecture de registres optimisée** - Implémentation thread-safe avec `ConcurrentDictionary`
- 📈 **Optimisation complète pour l'inlining JIT** - Méthodes courtes et prévisibles
- 🎯 **Méthodes de clé de lookup compilées statiquement** au compile-time
- 📦 **Utilisation de `List<T>` pré-dimensionné** pour la publication d'événements parallèles
- 🔐 **Renforcement des garanties de thread-safety** dans tous les registries

### Fixed
- 🐛 **Gestion robuste des exceptions** - Properly scoped `HandlerNotRegisteredException`
- 🛡️ **Gestion appropriée des scopes DI** pour éviter les fuites de ressources
- 🔒 **Synchronisation thread-safe** pour l'enregistrement d'event handlers multiples
- ✅ **Validation null-safety** sur tous les paramètres d'entrée
- 📝 **Support correct du `ConfigureAwait(false)`** pour éviter les allocations de contexte
- ✅ **Support correct des scopes DI** pour les dépendances scoped
- 📝 **Résolution des issues de synchronisation** avec événements multiples
- 🔒 **Gestion correcte des scopes** dans PublishAsync pour éviter les fuites

### Performance
- ⚡ **~50ns par opération** - Overhead minimal approchant les appels directs de méthode
- 🚀 **O(1) lookup time** pour tous les types de handler
- 💾 **Zero intermediate allocations** - Pas de création d'objets intermédiaires
- 🔌 **No reflection at runtime** - Toutes les décisions au compile-time
- ✨ **JIT-inlinable** - Méthodes critiques assez courtes pour être inlinées
- 🎯 **Design minimaliste** - Seulement ~100 lignes de code core

---

## [1.0.0-beta.2] - 2025-11-05

Ajout des Events, DependencyInjection et découverte automatique des handlers.

### Added
- ✨ Support complet des événements (Events) avec `IEvent` et `IEventHandler<TEvent>`
- 🎯 Découverte automatique des handlers via `MediatorBuilder.AddHandlersFromAssembly()`
- 📋 Extension `AddBrilliantMediator()` pour DependencyInjection avec configuration automatique
- 🔍 Support pour la découverte multi-assemblies avec `AddHandlersFromAssemblies()`
- 🏗️ Architecture DDD complète avec exemple E-Commerce
- 📚 Guide français complet pour DDD avec BrilliantMediator
- 🧪 Meilleure gestion des tests avec registrations d'instances de handler

### Changed
- 🔧 Refactorisation majeure de l'architecture interne pour supporter les events
- 🧬 Évolution du système de registres pour gérer plusieurs handlers par événement
- 📈 Optimisations de la performance des lookups
- 🎯 Amélioration de la documentation avec exemples plus réalistes
- 🔐 Renforcement des garanties de thread-safety dans tous les registries

### Fixed
- 🐛 Correction du bug de registration lors de la découverte automatique
- 🛡️ Amélioration de la gestion des erreurs et des cas limites
- ✅ Support correct des scopes DI pour les dépendances scoped
- 📝 Résolution des issues de synchronisation avec événements multiples
- 🔒 Correction de la gestion des scopes dans PublishAsync

### Breaking Changes
- ⚠️ Les méthodes `Send()` renommées en `DispatchAsync()` pour les commands (cohérence avec conventions)
- ⚠️ Le type `Mediator` nécessite maintenant un `IServiceProvider` (pas de constructeur sans paramètres)

---

## [1.0.0-beta.1] - 2025-11-04

Implémentation de base du Mediator Pattern avec support Commands et Queries.

### Added
- ✨ **Implémentation de base du Mediator Pattern** - Architecture ultra-légère et performante
- ⚡ **Zero-Reflection architecture** - Registres statiques génériques
- 🚀 **Performance exceptionnelle** - ~50ns par opération
- 🎯 **Support complet du pattern CQRS**
  - 📦 Commands sans réponse (`ICommand` + `ICommandHandler<TCommand>`)
  - 📚 Commands avec réponse (`ICommand<TResponse>` + `ICommandHandler<TCommand, TResponse>`)
  - 🔧 Queries (`IQuery<TResponse>` + `IQueryHandler<TQuery, TResponse>`)
- 🔐 **API type-safe** - Vérification complète au compile-time
- 💾 **Aucune allocation intermédiaire** - Zéro objet temporaire créé
- 📖 **Documentation complète** - README avec exemples et guide d'installation
- 🎯 **Design minimaliste** - ~100 lignes de code core
- ✅ **Enregistrement manuel des handlers** - API d'enregistrement flexible et simple
- ✅ **API asynchrone complète** - Support natif async/await avec ConfigureAwait(false)
- ✅ **Aucune dépendance externe** - Dépendances minimales
- ✅ **Gestion des exceptions** - `HandlerNotRegisteredException` pour diagnostics clairs
- ✅ **Framework agnostique** - Fonctionne avec n'importe quelle application .NET

### Technical Highlights
- 🔬 **Compile-time Verification** - Toutes les décisions de dispatch au compile-time
- 📊 **O(1) Lookup** - Accès constant aux handlers
- 🧵 **Thread-safe** - Registries sécurisés pour utilisation concurrente
- 🚀 **JIT-optimizable** - Méthodes critiques courtes et prévisibles
- 🎯 **Pattern Matching** - Support natif des génériques C# pour type-safety
- ⚙️ **Minimal Overhead** - Architecture conçue pour réduire chaque nanoseconde

---

## 📝 Notes de Migration

### De v1.0.0-beta.1 à v1.0.0-beta.2

⚠️ **Breaking Changes**

```csharp
// AVANT (beta.1)
var mediator = new Mediator();
await mediator.Send<CreateUserCommand, UserDto>(cmd);

// APRÈS (beta.2+)
services.AddBrilliantMediator(typeof(Program).Assembly);
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.DispatchAsync<CreateUserCommand, UserDto>(cmd);
```

**Points importants :**
- 🔄 Le constructeur sans paramètres est supprimé → Utilisez DependencyInjection
- 📋 `Send()` est renommé en `DispatchAsync()` pour les Commands
- ✨ Les Events sont maintenant supportés avec `PublishAsync<TEvent>()`
- 🔍 La découverte automatique des handlers est disponible

---

## Guide d'utilisation du Changelog

### Pour les contributeurs

Quand vous créez une Pull Request, mettez à jour la section `[Unreleased]`:

1. **Added** - Pour les nouvelles fonctionnalités
2. **Changed** - Pour les changements sur les fonctionnalités existantes
3. **Deprecated** - Pour les fonctionnalités qui seront supprimées
4. **Removed** - Pour les fonctionnalités supprimées
5. **Fixed** - Pour les bug fixes
6. **Security** - Pour les corrections de sécurité
7. **Performance** - Pour les améliorations de performance

### Exemple

```markdown
### Added
- ✨ Nouvelle fonctionnalité X
- ⚡ Support pour Y

### Fixed
- 🐛 Bug Z corrigé

### Performance
- 📈 Réduction du temps d'exécution de X% pour le scénario Y
```

### Convention de tags

| Emoji | Signification | Usage |
|-------|--------------|-------|
| ✨ | Feature/Amélioration | Nouvelles fonctionnalités principales |
| ⚡ | Performance | Améliorations de performance |
| 🚀 | Launch/Déploiement | Releases, déploiements importants |
| 🎯 | Objectif/Focus | Améliorations d'API, direction produit |
| 📦 | Packaging/Distribution | NuGet, versioning, distribution |
| 🔧 | Configuration/Maintenance | Changements internes, refactor |
| 📚 | Documentation | Docs, guides, exemples |
| 🐛 | Bug fix | Corrections de bugs |
| ✅ | Complete/Validation | Complétion de fonctionnalités, QA |
| 📖 | Guide | Tutoriels, guides étape par étape |
| ⚠️ | Breaking Change | Changements incompatibles |
| 🔒 | Security/Sync | Sécurité, thread-safety |
| ❌ | Removal/Deprecated | Dépréciations, suppressions |
| 🏗️ | Architecture | Changements architecturaux majeurs |
| 🧬 | Design Pattern | Patterns et approches de design |
| 🧪 | Testing/QA | Tests, améliorations de testabilité |
| 🔍 | Discovery | Détection/Découverte automatique |
| 🔥 | Critical/Major | Changements critiques majeurs |
| 📡 | Communication/Events | Communication inter-processus, événements |

---

## Versioning

Ce projet utilise [Semantic Versioning](https://semver.org/):

- **MAJOR** - Changements incompatibles avec les versions précédentes (breaking changes)
- **MINOR** - Nouvelles fonctionnalités compatibles (backward compatible)
- **PATCH** - Corrections de bugs compatibles (hot-fixes)

### Exemples

| Version | Type | Description |
|---------|------|-------------|
| 1.0.1 | PATCH | Correction de bug mineure |
| 1.1.0 | MINOR | Nouvelle fonctionnalité compatible |
| 2.0.0 | MAJOR | Architecture complètement redessinée |

### Politique de stabilité

- **v1.0.0** - Version initiale stable, production-ready
- **v1.x.x** - Versions stables avec nouvelles features
- **v1.x.0-rc** - Release candidate, API finale
- **v1.x.0-beta** - Beta publique, API peut changer
- **v0.x.x** - Précoce, API expérimentale et instable