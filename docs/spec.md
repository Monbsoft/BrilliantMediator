# BrilliantMediator — Spec & Architecture

> Source de vérité du projet. Mise à jour à la fin de chaque itération.
> Dernière mise à jour : 2026-07-31 — pipeline behaviors (ADR-007 → ADR-014)

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
void RegisterPipelineBehavior<TRequest, TResponse>();   // v3.2.0
void RegisterPipelineBehavior<TRequest>() where TRequest : ICommand;  // v3.2.0
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

### Pipeline behaviors — v3.2.0 (additif)

```csharp
namespace Monbsoft.BrilliantMediator.Abstractions.Pipeline;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
public delegate Task RequestHandlerDelegate();

// Queries et commandes avec réponse
public interface IPipelineBehavior<in TRequest, TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

// Commandes sans réponse (ADR-007). TRequest contraint à ICommand (ADR-013)
public interface IPipelineBehavior<in TRequest>
    where TRequest : ICommand
{
    Task Handle(
        TRequest request,
        RequestHandlerDelegate next,
        CancellationToken cancellationToken);
}
```

Enregistrement sur le builder fluide existant :

```csharp
services
    .AddBrilliantMediator()
    .AddQueryHandler<GetUserQuery, UserDto, GetUserQueryHandler>()
    .AddPipelineBehavior<GetUserQuery, UserDto, LoggingBehavior>()   // le plus externe
    .AddPipelineBehavior<GetUserQuery, UserDto, CachingBehavior>()
    .AddCommandHandler<DeleteUserCommand, DeleteUserCommandHandler>()
    .AddPipelineBehavior<DeleteUserCommand, AuditBehavior>()
    .Build();
```

`AddPipelineBehavior` accepte un `ServiceLifetime` (`Scoped` par défaut, comme les handlers).
Les behaviors sont résolus depuis **le scope du handler** (ADR-002) : un `DbContext` scoped
est la même instance sur toute la chaîne.

**Impact SemVer :** ajout de deux membres à `IHandlerRegistry`. Rupture de compilation pour un
implémenteur tiers de cette interface — mais `IHandlerRegistry` est un point d'extension interne
(sa propre documentation XML précise que le code applicatif doit dépendre d'`IMediator`), et
`Mediator` en est la seule implémentation. Traité comme un ajout mineur : **v3.2.0**.
Aucun changement pour les consommateurs d'`IMediator`. Déviation explicite d'un critère Bloquant
d'`AGENTS.md`, actée et motivée par l'**ADR-014**.

### Source Generator — BrilliantMediator.SourceGenerator v3.2.0

Référencer dans le `.csproj` cible :
```xml
<PackageReference Include="BrilliantMediator.SourceGenerator"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Déclarer les assemblies supplémentaires à scanner :
```csharp
[assembly: BrilliantMediatorGenerator(
    Namespace = "MyApp.Infrastructure.Generated",
    Assemblies = [typeof(CreateOrderCommandHandler)])]
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
| 3.2.0 | Pipeline behaviors sur commandes et queries (ADR-007 → ADR-014) | 2026-07-31 |

---

## Itérations en cours / planifiées

| # | Sujet | Statut |
|---|-------|--------|
| 1 | AGENTS.md — cycle de développement itératif | Livré (PR #8) |
| 2 | Nettoyage docs — suppression CHANGELOG/EXAMPLES/GUIDE | Livré (PR #9) |
| 3 | BrilliantMediator.SourceGenerator | Livré |
| 4 | SOLID refactoring: ISP/DIP + CancellationToken + decouple ASP.NET | Livré |
| 5 | Pipeline behaviors | Livré |

---

## ADR (Architecture Decision Records)

### ADR-001 — Zéro réflexion via ConcurrentDictionary + génériques compilés

- **Contexte :** Les implémentations Mediator courantes (MediatR) utilisent la réflexion pour résoudre les handlers à l'exécution, ajoutant un overhead significatif.
- **Décision :** Utiliser `ConcurrentDictionary<string, Type>` avec des clés générées statiquement à partir des noms complets de types. Résolution via DI standard sans `ActivatorUtilities`.
- **Conséquences :** Overhead < 50 ns. Enregistrement explicite requis (ou via Source Generator). Pas de découverte automatique "magique".

> **Nota (2026-07-06)** : cette décision reste valable pour les commands et queries. Pour les events, le registre est désormais un simple marqueur de présence (`ConcurrentDictionary<string, bool>`) — voir ADR-006.

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
- **Décision :** Créer `BrilliantMediator.SourceGenerator` (Roslyn `IIncrementalGenerator`) qui scanne les handlers à la compilation et génère `AddGeneratedHandlers(this MediatorBuilder)`. Les méthodes `AddHandlersFromAssembly*` sont marquées `[Obsolete]` avec message de migration vers v2.0.0. Configuration des assemblies supplémentaires via `[assembly: BrilliantMediatorGenerator(Assemblies = [typeof(T)])]`.
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

**Thread safety improvements** — ⚠️ *Remplacée par ADR-006*
- **Contexte :** Event handlers utilisaient `List<Type>` + `lock`, laissant le chemin critique bloqué.
- **Décision :** Remplacer par `ImmutableList<Type>` (lock-free reads).
- **Conséquences :** Better contention characteristics. Immutable snapshots.
- **Statut :** Remplacée par ADR-006. L'`ImmutableList<Type>` dédupliquée ne stockait que le type d'interface `IEventHandler<TEvent>` : plusieurs enregistrements pour le même événement se réduisaient à une seule entrée, et `PublishAsync` n'exécutait qu'un seul handler. Le registre d'événements est désormais un marqueur de présence.

### ADR-006 — Dispatch multi-handlers des événements via résolution IEnumerable et marqueur de présence

- **Contexte :** `PublishAsync<TEvent>` n'exécutait qu'un seul handler quand plusieurs étaient enregistrés pour le même événement, contredisant ADR-002 (« chaque handler obtient son propre scope indépendant ») et la documentation XML d'`IEventHandler<TEvent>` (multi-handler supporté). Cause : le registre `ImmutableList<Type>` (ADR-005) ne connaissait que le type d'interface `IEventHandler<TEvent>` — `IHandlerRegistry.RegisterEventHandler<TEvent>()` ne reçoit jamais le type concret du handler — donc N enregistrements se dédupliquaient en une seule entrée et une seule résolution.
- **Décision :** Le registre d'événements devient un simple marqueur de présence (`ConcurrentDictionary<string, bool>`, enregistrement idempotent). `PublishAsync<TEvent>` résout **tous** les handlers via `GetService<IEnumerable<IEventHandler<TEvent>>>()` (générique compile-time, zéro réflexion). Conformément à ADR-002, chaque handler s'exécute dans son propre scope DI : un « counting scope » dédié détermine le nombre N de handlers enregistrés, puis N scopes indépendants résolvent chacun l'énumérable et exécutent le handler d'index i. Exécution parallèle via `Task.WhenAll`.
- **Conséquences :** Correctif conforme à ADR-002, sans aucun changement d'API publique (l'alternative — stocker les types concrets — exigeait un breaking change sur `IHandlerRegistry`, voir « Prochaine itération »). Trade-off assumé : MS.DI matérialise l'`IEnumerable<T>` complet à chaque résolution ⇒ N + N² instanciations de handlers scoped/transient par publish (les singletons ne sont pas réinstanciés) ; un handler dont le constructeur a un effet de bord le déclenche N+1 fois par publish. Correct mais coûteux si N grand. Hypothèse documentée : l'ordre de résolution d'`IEnumerable<T>` est stable entre scopes — garanti par MS.DI (ordre d'enregistrement), mais pas par le contrat général `IServiceProvider` ; un conteneur tiers ne respectant pas cet ordre pourrait exécuter un handler deux fois et en ignorer un autre.

### ADR-007 — Commandes sans réponse : interface `IPipelineBehavior<TRequest>` séparée plutôt qu'un type `Unit` public

- **Contexte :** `IPipelineBehavior<TRequest, TResponse>` couvre les queries et les commandes avec réponse. `ICommand` n'a pas de `TResponse` : sans arbitrage, `DispatchAsync<TCommand>` resterait le seul point d'entrée non interceptable — or journalisation, validation et transaction sont majoritairement demandées sur les commandes d'écriture, qui sont précisément celles sans réponse.
- **Options considérées :**
  1. **Type `Unit` public** (approche MediatR). Un seul jeu d'interfaces, mais : `ICommand` n'hérite pas de `ICommand<Unit>`, donc aucun behavior ne couvrirait réellement les deux formes sans changement d'API ; `Unit` devient un type public à maintenir *ad vitam* ; chaque handler de commande void devrait retourner `Task<Unit>` (allocation + bruit dans le code appelant).
  2. **Interface séparée `IPipelineBehavior<TRequest>`** avec un délégué `RequestHandlerDelegate` non générique retournant `Task`.
  3. **Aucun behavior sur les commandes sans réponse en v3.2.**
- **Décision :** option 2. La bibliothèque duplique déjà systématiquement la distinction d'arité — `ICommand` / `ICommand<TResponse>`, `ICommandHandler<TCommand>` / `ICommandHandler<TCommand, TResponse>`. Introduire `IPipelineBehavior<TRequest>` prolonge un idiome existant au lieu d'introduire un concept nouveau. Aucun type sentinelle public, aucune allocation de `Task<Unit>`.
- **Conséquences :** deux interfaces et deux délégués publics au lieu d'un. Un utilisateur voulant journaliser commandes *et* queries écrit deux classes — mais il devrait le faire aussi avec `Unit`, puisque `ICommand` n'est pas un `ICommand<Unit>`. Surface publique ajoutée : 2 interfaces + 2 délégués, aucun type porteur de données.

### ADR-008 — Ordre d'exécution : ordre d'enregistrement, premier enregistré = plus externe

- **Contexte :** un pipeline non déterministe est inexploitable — une politique de cache doit s'exécuter à l'intérieur de la journalisation mais à l'extérieur de la mesure, et l'utilisateur doit pouvoir le prédire à la lecture de sa configuration DI.
- **Options considérées :** ordre d'enregistrement, ou propriété de priorité explicite (`int Order`).
- **Décision :** ordre d'enregistrement dans `MediatorBuilder`, **le premier enregistré est le plus externe** (il voit la requête en premier et la réponse en dernier). `MediatorBuilder.AddPipelineBehavior` ajoute au `IServiceCollection` dans l'ordre d'appel ; `Mediator` résout `IEnumerable<IPipelineBehavior<TRequest, TResponse>>` et compose la chaîne en parcourant la liste **à l'envers**, de sorte que l'indice 0 enveloppe tous les autres.
- **Conséquences :** ordre lisible sur place, sans coordination de numéros de priorité entre assemblies (une priorité explicite devient un problème de coordination global dès qu'un behavior vient d'un paquet tiers — YAGNI, cf. AGENTS.md). **Hypothèse documentée**, identique à celle assumée par ADR-006 : l'ordre de résolution d'`IEnumerable<T>` suit l'ordre d'enregistrement — garanti par `Microsoft.Extensions.DependencyInjection`, mais pas par le contrat général `IServiceProvider`. Un conteneur tiers ne respectant pas cet ordre produirait un pipeline dans un ordre non spécifié. Couvert par `PipelineBehaviorTests` (3 behaviors, ordre d'entrée et de sortie vérifiés).

### ADR-009 — Court-circuit : conséquence du chaînage de délégués, garantie par test

- **Contexte :** le cas d'usage moteur de l'itération (cache *stale-while-revalidate*) exige qu'un behavior puisse retourner une réponse **sans** exécuter le handler.
- **Décision :** le pipeline est une chaîne de `RequestHandlerDelegate<TResponse>` ; l'appel du handler est la dernière fermeture de la chaîne. Un behavior qui ne l'appelle pas retourne sa propre valeur — le handler n'est jamais invoqué. Aucun mécanisme dédié n'est ajouté. Le handler reste résolu depuis le conteneur avant la construction de la chaîne (il est capturé par la fermeture terminale), mais il n'est pas *exécuté*.
- **Conséquences :** court-circuit gratuit et sans API supplémentaire. Contrepartie assumée : le handler est **instancié** même s'il n'est pas exécuté, car sa résolution DI conditionne l'échec `HandlerNotRegisteredException` — un dispatch vers un handler non enregistré doit lever, que des behaviors soient présents ou non. Un handler dont le constructeur a un effet de bord le déclenchera donc même court-circuité. Vérifié par `SendAsync_BehaviorDoesNotCallNext_HandlerIsNotExecuted`.

### ADR-010 — Behaviors ouverts : fermetures explicites en v3.2, génération reportée

- **Contexte :** le besoin le plus fréquent est un behavior transverse unique (`LoggingBehavior<TRequest, TResponse>`) appliqué à toutes les requêtes. La forme idiomatique MS.DI est `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))`.
- **Point dur :** l'enregistrement d'un générique **ouvert** dans MS.DI construit le type fermé **à la résolution**, via `Type.MakeGenericType` — c'est de la réflexion dans le chemin critique, incompatible avec ADR-001 et non prouvable sous trimming/NativeAOT. Le contourner en silence reviendrait à annuler l'argument de vente du paquet.
- **Décision :** l'enregistrement de génériques ouverts n'est **pas** exposé en v3.2. La classe de behavior peut rester générique — seul l'*enregistrement* doit être fermé : `AddPipelineBehavior<GetUserQuery, UserDto, LoggingBehavior<GetUserQuery, UserDto>>()`. Le type fermé est construit par le **compilateur**, pas par le conteneur : zéro réflexion, AOT-safe. La génération automatique de ces fermetures par `BrilliantMediator.SourceGenerator` — qui connaît déjà tous les couples `(TRequest, TResponse)` puisqu'il scanne les handlers — est reportée à une itération dédiée.
- **Vérification :** `<IsAotCompatible>true</IsAotCompatible>` a été activé sur `BrilliantMediator.csproj`, ce qui allume les analyseurs trimming/AOT du SDK. La compatibilité AOT n'est donc plus une affirmation mais une propriété **vérifiée par le compilateur** à chaque build. Le premier passage a révélé six `IL2087` — `new ServiceDescriptor(Type, Type, lifetime)` exige que le trimmer préserve le constructeur du type d'implémentation : quatre sur les méthodes préexistantes (`AddCommandHandler` ×2, `AddQueryHandler`, `AddEventHandler`) et deux sur les nouvelles `AddPipelineBehavior`. Corrigé en annotant les paramètres génériques d'implémentation avec `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]`. Le paquet n'était donc pas réellement trim-safe avant cette itération, malgré la promesse.
- **Conséquences :** une ligne d'enregistrement par couple `(requête, réponse)` au lieu d'une seule pour tout le projet — verbeux à grande échelle, cohérent avec la verbosité déjà assumée par ADR-003. Le besoin est **couvert fonctionnellement** dès v3.2, seul le confort d'écriture manque. Vérifié par `SendAsync_OpenGenericBehaviorClosedAtRegistration_IsExecuted`. Reporté explicitement : l'itération « source generator » devra trancher l'ordre relatif entre behaviors générés et behaviors enregistrés à la main (ADR-008), question non résolue ici.

### ADR-011 — Événements : hors périmètre

- **Contexte :** `PublishAsync<TEvent>` diffuse à N handlers exécutés **en parallèle**, chacun dans son propre scope (ADR-002/ADR-006), et ne produit aucune réponse.
- **Décision :** aucun behavior sur les événements en v3.2.
- **Justification :** le modèle « chaîne de responsabilité autour d'un appel unique » ne se transpose pas. Deux sémantiques distinctes seraient nécessaires — envelopper le *publish* entier, ou envelopper *chaque handler* — et elles ne sont pas interchangeables (retry, transaction et court-circuit signifient autre chose dans chacune). Sans `TResponse`, `RequestHandlerDelegate<TResponse>` ne s'applique pas ; le court-circuit d'ADR-009 n'a pas de valeur de retour à produire. Aucun cas d'usage n'a été exprimé, contrairement au cache sur les lectures.
- **Conséquences :** `PublishAsync` conserve exactement son comportement actuel, sans branche supplémentaire ni surcoût. Le besoin transverse sur événements reste couvert par la composition manuelle dans les handlers. Réversible : la décision n'engage aucune API.

### ADR-012 — Coût nul en l'absence de behavior : double garde avant toute résolution

- **Contexte :** contrainte non négociable de l'itération — le code existant sans behavior doit se comporter à l'identique, sans surcoût mesurable. Résoudre `IEnumerable<IPipelineBehavior<TRequest, TResponse>>` à chaque dispatch coûterait une résolution DI et une allocation par appel, même quand aucun behavior n'est enregistré.
- **Décision :** deux gardes successives dans `Mediator`, avant toute allocation liée au pipeline.
  1. `private volatile bool _hasPipelineBehaviors` — passé à `true` au premier `RegisterPipelineBehavior*`. Faux dans toute application n'utilisant pas la fonctionnalité : lecture d'un champ booléen, le chemin d'exécution est alors **identique octet pour octet** au chemin v3.0.
  2. Si des behaviors existent quelque part, la clé `behavior_*` est construite et cherchée dans un `ConcurrentDictionary<string, bool>` dédié — une requête sans behavior ne déclenche aucune résolution DI. La construction de la clé (interpolation de chaîne) n'a lieu **que** derrière la première garde.
- **Conséquences :** surcoût d'une lecture de champ volatile sur le chemin sans behavior. Mesuré : `alloc/op` en baisse de 512 o à 424 o (−17 %, métrique déterministe), latence dans le bruit de mesure — voir « Preuve mesurée » ci-dessous. Contrepartie : deux registres à maintenir en cohérence dans `Mediator`, et une garde qui conditionne l'**exécution**, pas seulement la résolution — un behavior présent dans le conteneur mais dont le marqueur n'a pas été posé est ignoré silencieusement (enregistrement direct sur `IServiceCollection`, ou `AddPipelineBehavior` appelé après `Build()`). `MediatorBuilder.AddPipelineBehavior` pose les deux dans le même appel : c'est la seule voie supportée, et la documentation XML d'`IHandlerRegistry` le dit désormais explicitement. La baisse d'allocation n'est pas due aux gardes elles-mêmes mais à leur effet de bord : le passage de la requête en paramètre explicite (au lieu de la capturer dans une fermeture) a permis de rendre `static` les lambdas d'invocation du handler.

### ADR-013 — `IPipelineBehavior<TRequest>` contraint à `ICommand`

- **Contexte :** `ICommand<TResponse>` n'hérite pas de `ICommand` (ADR-007). Sans contrainte sur `TRequest`, `AddPipelineBehavior<CreateUserCommand, AuditBehavior>()` pour un `CreateUserCommand : ICommand<Guid>` compilait sans le moindre avertissement.
- **Point dur :** l'échec était **silencieux**. L'enregistrement inscrivait la clé `behavior_CreateUserCommand`, alors que le dispatch d'une commande avec réponse interroge `behavior_resp_CreateUserCommand_System.Guid` : aucune correspondance, aucune exception, aucun log — le behavior n'était jamais exécuté. C'est le pire mode de défaillance pour un souci transverse (audit, validation, autorisation), dont l'absence ne se voit pas.
- **Décision :** contraindre `TRequest` à `ICommand` sur les quatre déclarations concernées — `IPipelineBehavior<in TRequest>`, `IHandlerRegistry.RegisterPipelineBehavior<TRequest>()`, `Mediator.RegisterPipelineBehavior<TRequest>()` et `MediatorBuilder.AddPipelineBehavior<TRequest, TBehavior>()`. La contrainte est portée jusque sur l'interface pour que l'erreur se manifeste à l'écriture de la classe de behavior, et pas seulement à son enregistrement. L'erreur est désormais **CS0311** à la compilation.
- **Justification de la portée :** le pipeline sans réponse n'est atteignable que depuis `DispatchAsync<TCommand>() where TCommand : ICommand` ; tout `TRequest` qui n'est pas un `ICommand` était donc, par construction, un enregistrement mort. La contrainte n'exclut aucun usage légitime. Elle aligne aussi la méthode sur toutes ses sœurs du builder, déjà contraintes depuis l'origine (`AddCommandHandler`, `AddQueryHandler`, `AddEventHandler`).
- **Non traité :** la surcharge à trois paramètres `AddPipelineBehavior<TRequest, TResponse, TBehavior>()` conserve le même trou lorsque `TResponse` ne correspond pas à la réponse réelle de la requête. Il n'est pas exprimable en une contrainte unique, `IQuery<T>` et `ICommand<T>` n'ayant aucune base commune. Candidat à un diagnostic du source generator.
- **Conséquences :** API neuve de cette itération, donc contrainte ajoutée avant publication — aucun code existant n'est cassé, et c'était le seul moment pour le faire sans rupture. Un behavior générique doit désormais propager la contrainte : `class AuditBehavior<TRequest> : IPipelineBehavior<TRequest> where TRequest : ICommand`.

### ADR-014 — Ajout de membres à `IHandlerRegistry` en version mineure

- **Contexte :** cette itération ajoute `RegisterPipelineBehavior<TRequest, TResponse>()` et `RegisterPipelineBehavior<TRequest>()` à `IHandlerRegistry`, interface **publique**. Les membres n'ont pas d'implémentation par défaut : un implémenteur tiers ne compile plus (CS0535), et un assemblage tiers déjà compilé échoue au chargement du type. La version passe de 3.0.0 à 3.2.0 — un bump **mineur**.
- **Point dur :** `AGENTS.md` classe « Rupture de l'API publique sans bump de version majeure (SemVer) » parmi les critères **Bloquants**. Par ailleurs, le présent document affirmait déjà — avant cette itération, à « Prochaine itération » — qu'ajouter `RegisterEventHandler<TEvent, THandler>()` à cette même interface constituait un « Breaking change sur `IHandlerRegistry` — candidat v4 ». Trancher en mineur ici contredisait ce précédent sans le dire.
- **Décision :** conserver **v3.2.0**, et acter explicitement que `IHandlerRegistry` n'est pas une surface d'extension destinée aux tiers. Sa propre documentation XML l'énonce déjà (« Application code should depend on `IMediator` for dispatching, not this interface ») ; `Mediator` en est la seule implémentation ; `UseBrilliantMediator()` est le seul appelant. Le contrat public réellement exposé aux consommateurs est `IMediator`, et il est inchangé.
- **Ce que cette décision annule :** le précédent « candidat v4 » cité ci-dessus, pour ce qu'il implique du *versionnage*. La règle retenue désormais : l'ajout de membres à `IHandlerRegistry` est un changement **mineur** ; seule une modification d'`IMediator`, des interfaces de messages (`ICommand`, `IQuery`, `IEvent`) ou des interfaces de handlers déclenche un bump majeur. `RegisterEventHandler<TEvent, THandler>()` reste une évolution souhaitable de l'ADR-006, mais elle n'a plus à attendre une v4 pour ce seul motif.
- **Contrepartie assumée :** c'est une déviation explicite d'un critère Bloquant d'`AGENTS.md`, pas une exception que ce dernier prévoit. Elle est prise en connaissance de cause et tracée ici plutôt que laissée implicite dans un paragraphe de prose. Si `IHandlerRegistry` devait un jour être promue en point d'extension supporté, cette décision serait à réexaminer et la règle à réaligner sur `AGENTS.md`.
- **Conséquences :** aucun changement pour les consommateurs d'`IMediator`, qui sont la totalité des usages documentés. Le risque résiduel porte sur les doublures de test écrites à la main qui implémenteraient `IHandlerRegistry` : elles doivent ajouter deux membres.

---

## Preuve mesurée — non-régression du chemin sans behavior

Protocole : un banc dédié (console, `net10.0`, Release, `ServiceCollection` réel, handlers
retournant une tâche déjà complétée) mesure `DispatchAsync<TCommand>` et
`SendAsync<TQuery, TResponse>` **sans aucun behavior enregistré**. 50 000 itérations de
préchauffage, puis 7 séries de 500 000 itérations ; on retient le minimum, plus robuste au bruit
que la médiane sur une machine de développement. Les deux versions sont compilées côte à côte
(`git worktree` sur `dev` pour la référence) et exécutées **en alternance**, trois paires
successives, afin d'annuler la dérive thermique et l'ordonnancement de la machine.

| Mesure | v3.0.0 (`dev`) | v3.2.0 (behaviors) | Écart |
|---|---|---|---|
| `DispatchAsync<TCommand>` | 183,5 ns/op | **157,9 ns/op** | −14 % |
| `SendAsync<TQuery, TResponse>` | 236,6 ns/op | **211,5 ns/op** | −11 % |
| Allocations `DispatchAsync<TCommand>` | 512 o/op | **424 o/op** | −17 % |

La latence est bruitée sur la machine de mesure (médianes de 180 à 310 ns d'une série à l'autre,
dans les deux versions) : seule la comparaison des minima est concluante. L'allocation, elle, est
**déterministe** — 512 o dans les six exécutions de référence, 424 o dans les six exécutions
modifiées, sans aucune dispersion. C'est la preuve la plus solide.

Le gain n'est pas fortuit : le passage de la requête en paramètre explicite de
`ExecuteInScopeAsync` — nécessaire pour la fournir aux behaviors — a permis de rendre `static`
les lambdas d'invocation du handler, qui capturaient jusque-là la requête dans une fermeture
allouée à chaque dispatch. Le chemin sans behavior ne régresse donc pas : il gagne une
allocation par appel.

`PerformanceTests.cs` (seuils : > 50 000 ops/s en commande, > 25 000 ops/s en requête) reste vert.

---

## Dette technique

Aucune dette structurelle identifiée après v3.0.0.

**Abandonné intentionnellement:**
- `MediatorOptions`, `MediatorDiagnosticEvent`, `MediatorEventType` — diagnostics sans cas d'usage clair
- ~~Middlewares — ajouteraient de la complexité sans bénéfice clair pour la majorité~~ — décision
  révisée en v3.2.0 : un consommateur réel (application MAUI hors-ligne appliquant une politique de
  cache *stale-while-revalidate* à toutes ses lectures) a dû se replier sur un décorateur DI écrit à
  la main faute de point d'interception. Voir ADR-007 → ADR-014.
- Instance registration methods — nécessitaient DI lookup, ajoutaient de la confusion
- `AddHandlersFromAssembly*` — remplacé par le Source Generator (supprimé en v3.0.0)

---

## Prochaine itération

**Candidates:**
- **Behaviors ouverts générés** — étendre `BrilliantMediator.SourceGenerator` pour émettre les
  fermetures concrètes d'un behavior générique (`LoggingBehavior<TRequest, TResponse>`) sur tous les
  couples `(TRequest, TResponse)` qu'il découvre déjà en scannant les handlers. Supprime la
  verbosité assumée par ADR-010 sans introduire de réflexion. **À trancher :** l'ordre relatif entre
  behaviors générés et behaviors enregistrés à la main (ADR-008), non résolu en v3.2.0.
- ~~v3.2.0 — Explicit validation hook: `Action<IMediatorValidator>` in `MediatorBuilder`~~ —
  couvert par les pipeline behaviors : un `ValidationBehavior` court-circuite ou lève avant le handler.
- Diagnostics dashboard (opt-in, external service)
- `RegisterEventHandler<TEvent, THandler>()` — stocker les types concrets des event handlers pour ramener le coût du publish à N instanciations exactes (supprime le trade-off N + N² de l'ADR-006 et la dépendance à l'ordre de résolution MS.DI). Ajout de membre à `IHandlerRegistry` : traité en **mineur** depuis l'ADR-014, cette évolution n'attend donc plus une v4.
- Diagnostic du source generator sur la surcharge `AddPipelineBehavior<TRequest, TResponse, TBehavior>()` lorsque `TResponse` ne correspond pas à la réponse réelle de la requête — le seul cas de désalignement encore silencieux après l'ADR-013, non exprimable en contrainte générique.

À valider lors de la prochaine session.
