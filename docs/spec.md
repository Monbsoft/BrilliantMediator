# BrilliantMediator — Spec & Architecture

> Source de vérité du projet. Mise à jour à la fin de chaque itération.
> Dernière mise à jour : 2026-03-23 — itération #4 (Refactoring SOLID v3.0.0)

---

## Vision produit

BrilliantMediator est une bibliothèque .NET ultra-légère implémentant le pattern Mediator sans réflexion runtime.
Elle cible les développeurs qui veulent les bénéfices du CQRS (séparation Commands / Queries / Events)
sans la surcharge de performance ni la complexité des bibliothèques existantes.

**Proposition de valeur :** zéro réflexion, overhead < 50 ns, API type-safe au compile-time.

---

## API publique — v3.0.0 (BREAKING CHANGE)

### IMediator — dispatch only

```csharp
// Commands
Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
  where TCommand : ICommand;
Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
  where TCommand : ICommand<TResponse>;

// Queries
Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
  where TQuery : IQuery<TResponse>;

// Events
Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
  where TEvent : IEvent;
```

### IHandlerRegistry — registration only (NEW)

```csharp
void RegisterCommandHandler<TCommand>() where TCommand : ICommand;
void RegisterCommandHandler<TCommand, TResponse>() where TCommand : ICommand<TResponse>;
void RegisterQueryHandler<TQuery, TResponse>() where TQuery : IQuery<TResponse>;
void RegisterEventHandler<TEvent>() where TEvent : IEvent;
```

**Nota bene:**
- Instance registration methods removed (LSP violation)
- Mediator implements both IMediator + IHandlerRegistry
- Application code depends only on IMediator (dispatch)

### Abstractions — v3.0.0

| Interface | Rôle | Namespace |
|-----------|------|-----------|
| `ICommand` | Commande sans réponse | `Abstractions.Commands` |
| `ICommand<TResponse>` | Commande avec réponse | `Abstractions.Commands` |
| `ICommandHandler<TCommand>` | Handler de commande sans réponse (+ CancellationToken) | `Abstractions.Commands` |
| `ICommandHandler<TCommand, TResponse>` | Handler de commande avec réponse (+ CancellationToken) | `Abstractions.Commands` |
| `IQuery<TResponse>` | Requête | `Abstractions.Queries` |
| `IQueryHandler<TQuery, TResponse>` | Handler de requête (+ CancellationToken) | `Abstractions.Queries` |
| `IEvent` | Événement domaine | `Abstractions.Events` |
| `IEventHandler<TEvent>` | Handler d'événement (multi-handler supporté, + CancellationToken) | `Abstractions.Events` |
| `IMediatorInitializer` | Initialization bridge (NEW) | `Abstractions` |

**Breaking changes v3.0.0:**
- All handler `Handle()` methods now have `CancellationToken cancellationToken = default` parameter
- `IQueryHandler` moved from `Abstractions.Handlers` → `Abstractions.Queries`
- `IMediatorInitializer.Initialize(IHandlerRegistry)` instead of `Initialize(IMediator)`

### Configuration DI — v3.0.0

```csharp
// Setup
var services = new ServiceCollection();
services
  .AddBrilliantMediator()  // returns MediatorBuilder
  .AddCommandHandler<CreateOrderCommand, CreateOrderCommandHandler>()
  .AddQueryHandler<GetOrderQuery, OrderDto, GetOrderQueryHandler>()
  .AddEventHandler<OrderConfirmedEvent, OrderConfirmedEventHandler>()
  .Build();  // returns IServiceCollection

var serviceProvider = services.BuildServiceProvider();

// Initialization (after DI container built)
serviceProvider.UseBrilliantMediator();  // IServiceProvider extension

// Usage
var mediator = serviceProvider.GetRequiredService<IMediator>();
await mediator.DispatchAsync(command, cancellationToken);
```

**Works everywhere:** Console app, Worker Service, ASP.NET Core, etc. (no coupling to IApplicationBuilder)

### Source Generator — BrilliantMediator.SourceGenerator v1.3.0

Référencer dans le `.csproj` cible :
```xml
<PackageReference Include="BrilliantMediator.SourceGenerator"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Déclarer les assemblies supplémentaires à scanner :
```csharp
[assembly: ScanHandlersFrom(typeof(CreateOrderCommandHandler))]
```

Le générateur produit `{AssemblyName}.Infrastructure.Generated.g.cs` contenant `AddGeneratedHandlers(this MediatorBuilder)`.

---

## Fonctionnalités livrées

| Version | Fonctionnalité | Date |
|---------|---------------|------|
| 1.0.0-beta.1 | Commands (avec/sans réponse) + Queries — architecture zéro réflexion | 2025-11-04 |
| 1.0.0-beta.2 | Events (PublishAsync parallèle) + DI Extension + MediatorBuilder | 2025-11-05 |
| 1.0.0 | Version stable. Exemple EcommerceDDD | 2024-11-06 |
| 1.1.0 | Scoping DI renforcé par handler d'événement | 2025-11-09 |
| 1.2.0 | Migration .NET 10 | 2026-03-20 |
| 1.3.0 | BrilliantMediator.SourceGenerator — enregistrement zéro réflexion à la compilation | 2026-03-20 |
| 3.0.0 | SOLID refactoring: ISP split (IMediator/IHandlerRegistry), CancellationToken, decouple ASP.NET | 2026-03-23 |

---

## Itérations en cours / planifiées

| # | Sujet | Statut |
|---|-------|--------|
| 1 | AGENTS.md — cycle de développement itératif | Livré (PR #8) |
| 2 | Nettoyage docs — suppression CHANGELOG/EXAMPLES/GUIDE | Livré (PR #9) |
| 3 | BrilliantMediator.SourceGenerator | Livré |
| 4 | SOLID refactoring: ISP/DIP + CancellationToken + decouple ASP.NET | Livré |

---

## ADR (Architecture Decision Records)

### ADR-001 — Zéro réflexion via ConcurrentDictionary + génériques compilés

- **Contexte :** Les implémentations Mediator courantes (MediatR) utilisent la réflexion pour résoudre les handlers à l'exécution, ajoutant un overhead significatif.
- **Décision :** Utiliser `ConcurrentDictionary<string, Type>` avec des clés générées statiquement à partir des noms complets de types. Résolution via DI standard sans `ActivatorUtilities`.
- **Conséquences :** Overhead < 50 ns. Enregistrement explicite requis (ou via Source Generator). Pas de découverte automatique "magique".

### ADR-002 — Scope DI par appel dans DispatchAsync / SendAsync / PublishAsync

- **Contexte :** Les handlers peuvent dépendre de services scoped (ex. `DbContext` EF Core). Un singleton partagé provoquerait des corruptions de données.
- **Décision :** Chaque appel crée son propre `IServiceScope` via `_serviceProvider.CreateScope()`. Pour les events : chaque handler obtient son propre scope indépendant.
- **Conséquences :** Isolation garantie. Légère surcharge de création de scope (< 1 µs, négligeable).

### ADR-003 — Enregistrement explicite des handlers (Register*)

- **Contexte :** La découverte automatique par réflexion viole le principe zéro réflexion.
- **Décision :** Enregistrement explicite via `RegisterCommandHandler<T>()` ou via `MediatorBuilder.AddHandlersFromAssembly()` (scan assemblies au démarrage uniquement, pas à l'exécution).
- **Conséquences :** Verbosité accrue vs. MediatR. Contrepartie : erreurs détectées au démarrage, pas à l'exécution.

### ADR-004 — Source Generator pour l'enregistrement zéro réflexion

- **Contexte :** `AddHandlersFromAssembly()` utilise la réflexion au démarrage (`GetTypes()`, `MakeGenericMethod`), ce qui contredit la promesse "zéro réflexion" de la bibliothèque.
- **Décision :** Créer `BrilliantMediator.SourceGenerator` (Roslyn `IIncrementalGenerator`) qui scanne les handlers à la compilation et génère `AddGeneratedHandlers(this MediatorBuilder)`. Les méthodes `AddHandlersFromAssembly*` sont marquées `[Obsolete]` avec message de migration vers v2.0.0. Configuration des assemblies supplémentaires via `[assembly: ScanHandlersFrom(typeof(T))]`.
- **Conséquences :** Zéro réflexion à l'exécution y compris au démarrage. Erreurs de configuration détectées à la compilation. `AddHandlersFromAssembly*` restent fonctionnelles jusqu'à v2.0.0 pour la compatibilité.

### ADR-005 — SOLID refactoring v3.0.0

**Interface Segregation Principle (ISP)**
- **Contexte :** `IMediator` contenait 8 méthodes (4 dispatch + 4 registration), mélangeant deux responsabilités distinctes.
- **Décision :** Scinder en `IMediator` (dispatch: 4 méthodes) + `IHandlerRegistry` (registration: 4 méthodes). `Mediator` implémente les deux; application code dépend uniquement de `IMediator`.
- **Conséquences :** API plus claire. Chaque interface a une seule raison de changer. Tests simplifiés.

**Dependency Inversion Principle (DIP)**
- **Contexte :** `UseBrilliantMediator()` sur `IApplicationBuilder` couplait la lib à ASP.NET Core.
- **Décision :** Déplacer sur `IServiceProvider` (déjà une dépendance minimale). Works: Console, Worker Service, ASP.NET Core, Unity, etc.
- **Conséquences :** Zero ASP.NET coupling. Flexible deployment.

**Liskov Substitution Principle (LSP)**
- **Contexte :** Méthodes `RegisterCommandHandler(handler)` acceptaient une instance mais jetaient silencieusement le handler (stockaient juste le Type pour DI lookup).
- **Décision :** Supprimer les surcharges instance. Enregistrement via `Register*<T>()` DI-based uniquement.
- **Conséquences :** Comportement prévisible. Moins de confusion tests.

**CancellationToken support**
- **Contexte :** Async handlers sans `CancellationToken` ne respectent pas les bonnes pratiques async .NET.
- **Décision :** Ajouter `CancellationToken cancellationToken = default` sur tous les handlers et méthodes dispatch.
- **Conséquences :** Full async support. Graceful shutdown + timeout handling.

**Thread safety improvements**
- **Contexte :** Event handlers utilisaient `List<Type>` + `lock`, laissant le chemin critique bloqué.
- **Décision :** Remplacer par `ImmutableList<Type>` (lock-free reads).
- **Conséquences :** Better contention characteristics. Immutable snapshots.

---

## Dette technique

Aucune dette structurelle identifiée après v3.0.0.

**Abandonné intentionnellement:**
- `MediatorOptions`, `MediatorDiagnosticEvent`, `MediatorEventType` — diagnostics sans cas d'usage clair
- Middlewares — ajouteraient de la complexité sans bénéfice clair pour la majorité
- Instance registration methods — nécessitaient DI lookup, ajoutaient de la confusion
- `AddHandlersFromAssembly*` — préfacer le Source Generator (Obsolete en v3.0.0, sera supprimé en v4.0.0)

---

## Prochaine itération

**Candidates:**
- v3.1.0 — Explicit validation hook: `Action<IMediatorValidator>` in `MediatorBuilder`
- v4.0.0 — Remove `[Obsolete] AddHandlersFromAssembly*` methods
- Diagnostics dashboard (opt-in, external service)

À valider lors de la prochaine session.
