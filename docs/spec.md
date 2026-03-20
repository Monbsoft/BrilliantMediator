# BrilliantMediator — Spec & Architecture

> Source de vérité du projet. Mise à jour à la fin de chaque itération.
> Dernière mise à jour : 2026-03-20 — itération #1 (AGENTS.md)

---

## Vision produit

BrilliantMediator est une bibliothèque .NET ultra-légère implémentant le pattern Mediator sans réflexion runtime.
Elle cible les développeurs qui veulent les bénéfices du CQRS (séparation Commands / Queries / Events)
sans la surcharge de performance ni la complexité des bibliothèques existantes.

**Proposition de valeur :** zéro réflexion, overhead < 50 ns, API type-safe au compile-time.

---

## API publique — v1.2.0

### IMediator

```csharp
// Commands
Task DispatchAsync<TCommand>(TCommand command) where TCommand : ICommand;
Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command) where TCommand : ICommand<TResponse>;

// Queries
Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query) where TQuery : IQuery<TResponse>;

// Events
Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent;

// Registration (DI type)
void RegisterCommandHandler<TCommand>() where TCommand : ICommand;
void RegisterCommandHandler<TCommand, TResponse>() where TCommand : ICommand<TResponse>;
void RegisterQueryHandler<TQuery, TResponse>() where TQuery : IQuery<TResponse>;
void RegisterEventHandler<TEvent>() where TEvent : IEvent;

// Registration (instance — tests)
void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler);
void RegisterCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler);
void RegisterQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler);
void RegisterEventHandler<TEvent>(IEventHandler<TEvent> handler);
```

### Abstractions

| Interface | Rôle |
|-----------|------|
| `ICommand` | Commande sans réponse |
| `ICommand<TResponse>` | Commande avec réponse |
| `ICommandHandler<TCommand>` | Handler de commande sans réponse |
| `ICommandHandler<TCommand, TResponse>` | Handler de commande avec réponse |
| `IQuery<TResponse>` | Requête |
| `IQueryHandler<TQuery, TResponse>` | Handler de requête |
| `IEvent` | Événement domaine |
| `IEventHandler<TEvent>` | Handler d'événement (multi-handler supporté) |

### Configuration DI

```csharp
services.AddBrilliantMediator(assembly);
// ou
services.AddBrilliantMediator(builder => builder.AddHandlersFromAssembly(assembly));
```

---

## Fonctionnalités livrées

| Version | Fonctionnalité | Date |
|---------|---------------|------|
| 1.0.0-beta.1 | Commands (avec/sans réponse) + Queries — architecture zéro réflexion | 2025-11-04 |
| 1.0.0-beta.2 | Events (PublishAsync parallèle) + DI Extension + MediatorBuilder | 2025-11-05 |
| 1.0.0 | Version stable. Exemple EcommerceDDD | 2024-11-06 |
| 1.1.0 | Scoping DI renforcé par handler d'événement | 2025-11-09 |
| 1.2.0 | Migration .NET 10 | 2026-03-20 |

---

## Itérations en cours / planifiées

| # | Sujet | Statut |
|---|-------|--------|
| 1 | AGENTS.md — cycle de développement itératif | Livré (PR #8) |
| 2 | Réduction verbosité API (génériques) | En discussion |

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

---

## Dette technique

- `PublishAsync` utilise un `lock` sur `List<Type>` lors de la copie des handlers — pourrait être remplacé par `ImmutableList<T>` pour éliminer le lock dans le chemin chaud.
- Les handlers d'instances dans `RegisterCommandHandler(handler)` ne stockent que le type — l'instance n'est pas utilisée lors du dispatch DI (comportement potentiellement surprenant en tests).
- Des fonctionnalités listées comme "à venir" (diagnostics, middlewares, CancellationToken, i18n) ne sont pas implémentées — à créer en issues GitHub ou à abandonner explicitement.

---

## Prochaine itération

**#2 — Réduction verbosité API publique**

Objectif : réduire les paramètres de type redondants dans les appels `DispatchAsync` et `SendAsync`.

```csharp
// Actuel — TResponse doit être répété
var result = await mediator.DispatchAsync<CreateOrderCommand, OrderId>(command);

// Cible envisagée — TResponse inféré depuis ICommand<TResponse>
var result = await mediator.DispatchAsync(command);
```

> Statut : en discussion — clarification besoin/périmètre/contraintes en cours.
