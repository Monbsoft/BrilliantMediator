using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Pipeline;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Extensions;
using System.Collections.Concurrent;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// TEST FIXTURES
// ============================================================================

/// <summary>
/// Singleton recorder collecting an ordered trace of pipeline steps.
/// </summary>
public class PipelineTrace
{
    private readonly ConcurrentQueue<string> _steps = new();

    public void Record(string step) => _steps.Enqueue(step);

    public IReadOnlyList<string> Steps => _steps.ToArray();
}

public class PipelineQuery : IQuery<PipelineResponse>
{
    public string Value { get; set; } = string.Empty;
}

public class PipelineResponse
{
    public string Value { get; set; } = string.Empty;
}

public class PipelineCommand : ICommand
{
    public string Value { get; set; } = string.Empty;
}

public class PipelineQueryHandler : IQueryHandler<PipelineQuery, PipelineResponse>
{
    private readonly PipelineTrace _trace;

    public PipelineQueryHandler(PipelineTrace trace) => _trace = trace;

    public Task<PipelineResponse> Handle(PipelineQuery query, CancellationToken cancellationToken = default)
    {
        _trace.Record("handler");
        return Task.FromResult(new PipelineResponse { Value = $"handled:{query.Value}" });
    }
}

public class PipelineCommandHandler : ICommandHandler<PipelineCommand>
{
    private readonly PipelineTrace _trace;

    public PipelineCommandHandler(PipelineTrace trace) => _trace = trace;

    public Task Handle(PipelineCommand command, CancellationToken cancellationToken = default)
    {
        _trace.Record("handler");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Base class recording entry and exit around <c>next</c>, so that both the
/// inbound order and the outbound (unwinding) order can be asserted.
/// </summary>
public abstract class TracingQueryBehavior : IPipelineBehavior<PipelineQuery, PipelineResponse>
{
    private readonly PipelineTrace _trace;

    protected TracingQueryBehavior(PipelineTrace trace) => _trace = trace;

    protected abstract string Name { get; }

    public async Task<PipelineResponse> Handle(
        PipelineQuery request,
        RequestHandlerDelegate<PipelineResponse> next,
        CancellationToken cancellationToken)
    {
        _trace.Record($"{Name}:before");
        var response = await next();
        _trace.Record($"{Name}:after");
        return response;
    }
}

public class FirstQueryBehavior : TracingQueryBehavior
{
    public FirstQueryBehavior(PipelineTrace trace) : base(trace) { }
    protected override string Name => "first";
}

public class SecondQueryBehavior : TracingQueryBehavior
{
    public SecondQueryBehavior(PipelineTrace trace) : base(trace) { }
    protected override string Name => "second";
}

public class ThirdQueryBehavior : TracingQueryBehavior
{
    public ThirdQueryBehavior(PipelineTrace trace) : base(trace) { }
    protected override string Name => "third";
}

/// <summary>
/// Cache-style behavior: returns without ever calling <c>next</c> (ADR-009).
/// </summary>
public class ShortCircuitQueryBehavior : IPipelineBehavior<PipelineQuery, PipelineResponse>
{
    public const string CachedValue = "from-cache";

    public Task<PipelineResponse> Handle(
        PipelineQuery request,
        RequestHandlerDelegate<PipelineResponse> next,
        CancellationToken cancellationToken)
        => Task.FromResult(new PipelineResponse { Value = CachedValue });
}

public class PipelineBehaviorException : Exception
{
    public PipelineBehaviorException(string message) : base(message) { }
}

public class ThrowingQueryBehavior : IPipelineBehavior<PipelineQuery, PipelineResponse>
{
    public const string Message = "behavior failed";

    public Task<PipelineResponse> Handle(
        PipelineQuery request,
        RequestHandlerDelegate<PipelineResponse> next,
        CancellationToken cancellationToken)
        => throw new PipelineBehaviorException(Message);
}

/// <summary>
/// Records the <see cref="CancellationToken"/> observed by the behavior so the
/// test can assert the caller's token reaches the pipeline unchanged.
/// </summary>
public class TokenCapturingQueryBehavior : IPipelineBehavior<PipelineQuery, PipelineResponse>
{
    private readonly TokenCapture _capture;

    public TokenCapturingQueryBehavior(TokenCapture capture) => _capture = capture;

    public Task<PipelineResponse> Handle(
        PipelineQuery request,
        RequestHandlerDelegate<PipelineResponse> next,
        CancellationToken cancellationToken)
    {
        _capture.BehaviorToken = cancellationToken;
        return next();
    }
}

public class TokenCapture
{
    public CancellationToken BehaviorToken { get; set; }
    public CancellationToken HandlerToken { get; set; }
}

public class PipelineTokenCapturingQueryHandler : IQueryHandler<PipelineQuery, PipelineResponse>
{
    private readonly TokenCapture _capture;

    public PipelineTokenCapturingQueryHandler(TokenCapture capture) => _capture = capture;

    public Task<PipelineResponse> Handle(PipelineQuery query, CancellationToken cancellationToken = default)
    {
        _capture.HandlerToken = cancellationToken;
        return Task.FromResult(new PipelineResponse { Value = query.Value });
    }
}

/// <summary>
/// Behavior that honours cancellation before delegating, mirroring what a
/// real retry or timeout behavior would do.
/// </summary>
public class CancellationObservingQueryBehavior : IPipelineBehavior<PipelineQuery, PipelineResponse>
{
    public Task<PipelineResponse> Handle(
        PipelineQuery request,
        RequestHandlerDelegate<PipelineResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return next();
    }
}

/// <summary>
/// Scoped dependency used to prove behavior and handler share one DI scope.
/// </summary>
public class ScopedPipelineDependency : IDisposable
{
    public Guid InstanceId { get; } = Guid.NewGuid();
    public bool IsDisposed { get; private set; }
    public void Dispose() => IsDisposed = true;
}

public class ScopeCapture
{
    public ScopedPipelineDependency? BehaviorDependency { get; set; }
    public ScopedPipelineDependency? HandlerDependency { get; set; }
}

public class ScopeCapturingQueryBehavior : IPipelineBehavior<PipelineQuery, PipelineResponse>
{
    private readonly ScopedPipelineDependency _dependency;
    private readonly ScopeCapture _capture;

    public ScopeCapturingQueryBehavior(ScopedPipelineDependency dependency, ScopeCapture capture)
    {
        _dependency = dependency;
        _capture = capture;
    }

    public Task<PipelineResponse> Handle(
        PipelineQuery request,
        RequestHandlerDelegate<PipelineResponse> next,
        CancellationToken cancellationToken)
    {
        _capture.BehaviorDependency = _dependency;
        return next();
    }
}

public class ScopeCapturingQueryHandler : IQueryHandler<PipelineQuery, PipelineResponse>
{
    private readonly ScopedPipelineDependency _dependency;
    private readonly ScopeCapture _capture;

    public ScopeCapturingQueryHandler(ScopedPipelineDependency dependency, ScopeCapture capture)
    {
        _dependency = dependency;
        _capture = capture;
    }

    public Task<PipelineResponse> Handle(PipelineQuery query, CancellationToken cancellationToken = default)
    {
        _capture.HandlerDependency = _dependency;
        return Task.FromResult(new PipelineResponse { Value = query.Value });
    }
}

/// <summary>
/// Generic behavior class. ADR-010: the class may be generic, only the DI
/// registration must be closed by the compiler.
/// </summary>
public class GenericTracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly PipelineTrace _trace;

    public GenericTracingBehavior(PipelineTrace trace) => _trace = trace;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _trace.Record($"generic<{typeof(TRequest).Name}>:before");
        var response = await next();
        _trace.Record($"generic<{typeof(TRequest).Name}>:after");
        return response;
    }
}

/// <summary>
/// Behavior for a command without response (ADR-007).
/// </summary>
public class TracingCommandBehavior : IPipelineBehavior<PipelineCommand>
{
    private readonly PipelineTrace _trace;

    public TracingCommandBehavior(PipelineTrace trace) => _trace = trace;

    public async Task Handle(
        PipelineCommand request,
        RequestHandlerDelegate next,
        CancellationToken cancellationToken)
    {
        _trace.Record("command-behavior:before");
        await next();
        _trace.Record("command-behavior:after");
    }
}

public class ShortCircuitCommandBehavior : IPipelineBehavior<PipelineCommand>
{
    public Task Handle(
        PipelineCommand request,
        RequestHandlerDelegate next,
        CancellationToken cancellationToken)
        => Task.CompletedTask;
}

/// <summary>
/// Command with response (ADR-007): goes through the same two-parameter behavior
/// interface as queries, but is dispatched via <c>DispatchAsync</c>.
/// </summary>
public class PipelineResponseCommand : ICommand<PipelineResponse>
{
    public string Value { get; set; } = string.Empty;
}

public class PipelineResponseCommandHandler : ICommandHandler<PipelineResponseCommand, PipelineResponse>
{
    private readonly PipelineTrace _trace;

    public PipelineResponseCommandHandler(PipelineTrace trace) => _trace = trace;

    public Task<PipelineResponse> Handle(PipelineResponseCommand command, CancellationToken cancellationToken = default)
    {
        _trace.Record("handler");
        return Task.FromResult(new PipelineResponse { Value = $"handled:{command.Value}" });
    }
}

public class TracingResponseCommandBehavior : IPipelineBehavior<PipelineResponseCommand, PipelineResponse>
{
    private readonly PipelineTrace _trace;

    public TracingResponseCommandBehavior(PipelineTrace trace) => _trace = trace;

    public async Task<PipelineResponse> Handle(
        PipelineResponseCommand request,
        RequestHandlerDelegate<PipelineResponse> next,
        CancellationToken cancellationToken)
    {
        _trace.Record("response-command-behavior:before");
        var response = await next();
        _trace.Record("response-command-behavior:after");
        return response;
    }
}

/// <summary>
/// Second query type, used to prove behaviors do not leak across request types.
/// </summary>
public class OtherPipelineQuery : IQuery<PipelineResponse>
{
}

public class OtherPipelineQueryHandler : IQueryHandler<OtherPipelineQuery, PipelineResponse>
{
    private readonly PipelineTrace _trace;

    public OtherPipelineQueryHandler(PipelineTrace trace) => _trace = trace;

    public Task<PipelineResponse> Handle(OtherPipelineQuery query, CancellationToken cancellationToken = default)
    {
        _trace.Record("other-handler");
        return Task.FromResult(new PipelineResponse { Value = "other" });
    }
}

// ============================================================================
// PIPELINE BEHAVIOR TESTS (real ServiceCollection)
// ============================================================================

public class PipelineBehaviorTests
{
    private static (IMediator mediator, IServiceProvider provider) Build(
        Action<MediatorBuilder> configure,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<PipelineTrace>();
        configureServices?.Invoke(services);

        var builder = services.AddBrilliantMediator();
        configure(builder);
        builder.Build();

        var provider = services.BuildServiceProvider();
        provider.UseBrilliantMediator();
        return (provider.GetRequiredService<IMediator>(), provider);
    }

    // ------------------------------------------------------------------
    // Execution order (ADR-008)
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_ThreeBehaviors_ExecutesInRegistrationOrderAroundHandler()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, SecondQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, ThirdQueryBehavior>());

        var response = await mediator.SendAsync<PipelineQuery, PipelineResponse>(
            new PipelineQuery { Value = "x" });

        Assert.Equal("handled:x", response.Value);
        Assert.Equal(
            new[]
            {
                "first:before",
                "second:before",
                "third:before",
                "handler",
                "third:after",
                "second:after",
                "first:after"
            },
            provider.GetRequiredService<PipelineTrace>().Steps);
    }

    [Fact]
    public async Task SendAsync_ThreeBehaviors_FirstRegisteredIsOutermost()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, ThirdQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, SecondQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>());

        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());

        var steps = provider.GetRequiredService<PipelineTrace>().Steps;
        Assert.Equal("third:before", steps[0]);
        Assert.Equal("third:after", steps[^1]);
    }

    // ------------------------------------------------------------------
    // Short-circuit (ADR-009)
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_BehaviorDoesNotCallNext_HandlerIsNotExecuted()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, ShortCircuitQueryBehavior>());

        var response = await mediator.SendAsync<PipelineQuery, PipelineResponse>(
            new PipelineQuery { Value = "x" });

        Assert.Equal(ShortCircuitQueryBehavior.CachedValue, response.Value);
        Assert.DoesNotContain("handler", provider.GetRequiredService<PipelineTrace>().Steps);
    }

    [Fact]
    public async Task SendAsync_ShortCircuitBeforeInnerBehaviors_SkipsRemainingPipeline()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, ShortCircuitQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, SecondQueryBehavior>());

        var response = await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());

        Assert.Equal(ShortCircuitQueryBehavior.CachedValue, response.Value);
        Assert.Equal(
            new[] { "first:before", "first:after" },
            provider.GetRequiredService<PipelineTrace>().Steps);
    }

    [Fact]
    public async Task DispatchAsync_CommandBehaviorDoesNotCallNext_HandlerIsNotExecuted()
    {
        var (mediator, provider) = Build(builder => builder
            .AddCommandHandler<PipelineCommand, PipelineCommandHandler>()
            .AddPipelineBehavior<PipelineCommand, ShortCircuitCommandBehavior>());

        await mediator.DispatchAsync(new PipelineCommand());

        Assert.Empty(provider.GetRequiredService<PipelineTrace>().Steps);
    }

    // ------------------------------------------------------------------
    // CancellationToken propagation
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_WithCancellationToken_PropagatesSameTokenToBehaviorAndHandler()
    {
        var capture = new TokenCapture();
        var (mediator, _) = Build(
            builder => builder
                .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineTokenCapturingQueryHandler>()
                .AddPipelineBehavior<PipelineQuery, PipelineResponse, TokenCapturingQueryBehavior>(),
            services => services.AddSingleton(capture));

        using var cts = new CancellationTokenSource();
        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery(), cts.Token);

        Assert.Equal(cts.Token, capture.BehaviorToken);
        Assert.Equal(cts.Token, capture.HandlerToken);
    }

    [Fact]
    public async Task SendAsync_CancelledToken_BehaviorObservesCancellationAndThrows()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, CancellationObservingQueryBehavior>());

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery(), cts.Token));

        Assert.DoesNotContain("handler", provider.GetRequiredService<PipelineTrace>().Steps);
    }

    // ------------------------------------------------------------------
    // Exceptions
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_BehaviorThrows_ExceptionPropagatesToCaller()
    {
        var (mediator, _) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, ThrowingQueryBehavior>());

        var exception = await Assert.ThrowsAsync<PipelineBehaviorException>(() =>
            mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery()));

        Assert.Equal(ThrowingQueryBehavior.Message, exception.Message);
    }

    [Fact]
    public async Task SendAsync_InnerBehaviorThrows_OuterBehaviorCanObserveIt()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, ThrowingQueryBehavior>());

        await Assert.ThrowsAsync<PipelineBehaviorException>(() =>
            mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery()));

        var steps = provider.GetRequiredService<PipelineTrace>().Steps;
        Assert.Equal(new[] { "first:before" }, steps);
    }

    [Fact]
    public async Task SendAsync_UnregisteredHandlerWithBehaviorRegistered_ThrowsHandlerNotRegisteredException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<PipelineTrace>();
        services
            .AddBrilliantMediator()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>()
            .Build();

        var provider = services.BuildServiceProvider();
        provider.UseBrilliantMediator();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<Exceptions.HandlerNotRegisteredException>(() =>
            mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery()));

        Assert.Empty(provider.GetRequiredService<PipelineTrace>().Steps);
    }

    // ------------------------------------------------------------------
    // No behavior registered (ADR-012)
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_NoBehaviorRegistered_InvokesHandlerDirectly()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>());

        var response = await mediator.SendAsync<PipelineQuery, PipelineResponse>(
            new PipelineQuery { Value = "x" });

        Assert.Equal("handled:x", response.Value);
        Assert.Equal(new[] { "handler" }, provider.GetRequiredService<PipelineTrace>().Steps);
    }

    [Fact]
    public async Task DispatchAsync_NoBehaviorRegistered_InvokesHandlerDirectly()
    {
        var (mediator, provider) = Build(builder => builder
            .AddCommandHandler<PipelineCommand, PipelineCommandHandler>());

        await mediator.DispatchAsync(new PipelineCommand());

        Assert.Equal(new[] { "handler" }, provider.GetRequiredService<PipelineTrace>().Steps);
    }

    [Fact]
    public async Task SendAsync_BehaviorRegisteredForAnotherRequest_IsNotApplied()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddQueryHandler<OtherPipelineQuery, PipelineResponse, OtherPipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>());

        await mediator.SendAsync<OtherPipelineQuery, PipelineResponse>(new OtherPipelineQuery());

        Assert.Equal(new[] { "other-handler" }, provider.GetRequiredService<PipelineTrace>().Steps);
    }

    // ------------------------------------------------------------------
    // Scoped resolution (ADR-002)
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_ScopedDependency_BehaviorAndHandlerShareTheSameScope()
    {
        var capture = new ScopeCapture();
        var (mediator, _) = Build(
            builder => builder
                .AddQueryHandler<PipelineQuery, PipelineResponse, ScopeCapturingQueryHandler>()
                .AddPipelineBehavior<PipelineQuery, PipelineResponse, ScopeCapturingQueryBehavior>(),
            services =>
            {
                services.AddSingleton(capture);
                services.AddScoped<ScopedPipelineDependency>();
            });

        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());

        Assert.NotNull(capture.BehaviorDependency);
        Assert.NotNull(capture.HandlerDependency);
        Assert.Same(capture.BehaviorDependency, capture.HandlerDependency);
    }

    [Fact]
    public async Task SendAsync_ScopedDependency_DisposedAfterPipelineCompletes()
    {
        var capture = new ScopeCapture();
        var (mediator, _) = Build(
            builder => builder
                .AddQueryHandler<PipelineQuery, PipelineResponse, ScopeCapturingQueryHandler>()
                .AddPipelineBehavior<PipelineQuery, PipelineResponse, ScopeCapturingQueryBehavior>(),
            services =>
            {
                services.AddSingleton(capture);
                services.AddScoped<ScopedPipelineDependency>();
            });

        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());

        Assert.True(capture.BehaviorDependency!.IsDisposed);
    }

    [Fact]
    public async Task SendAsync_ScopedBehavior_NewInstancePerDispatch()
    {
        var capture = new ScopeCapture();
        var instanceIds = new List<Guid>();
        var (mediator, _) = Build(
            builder => builder
                .AddQueryHandler<PipelineQuery, PipelineResponse, ScopeCapturingQueryHandler>()
                .AddPipelineBehavior<PipelineQuery, PipelineResponse, ScopeCapturingQueryBehavior>(),
            services =>
            {
                services.AddSingleton(capture);
                services.AddScoped<ScopedPipelineDependency>();
            });

        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());
        instanceIds.Add(capture.BehaviorDependency!.InstanceId);
        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());
        instanceIds.Add(capture.BehaviorDependency!.InstanceId);

        Assert.Equal(2, instanceIds.Distinct().Count());
    }

    // ------------------------------------------------------------------
    // Commands without response (ADR-007)
    // ------------------------------------------------------------------

    [Fact]
    public async Task DispatchAsync_CommandWithBehavior_ExecutesBehaviorAroundHandler()
    {
        var (mediator, provider) = Build(builder => builder
            .AddCommandHandler<PipelineCommand, PipelineCommandHandler>()
            .AddPipelineBehavior<PipelineCommand, TracingCommandBehavior>());

        await mediator.DispatchAsync(new PipelineCommand { Value = "x" });

        Assert.Equal(
            new[] { "command-behavior:before", "handler", "command-behavior:after" },
            provider.GetRequiredService<PipelineTrace>().Steps);
    }

    // ------------------------------------------------------------------
    // Commands with response (ADR-007)
    // ------------------------------------------------------------------

    [Fact]
    public async Task DispatchAsync_CommandWithResponseAndBehavior_ExecutesBehaviorAroundHandler()
    {
        var (mediator, provider) = Build(builder => builder
            .AddCommandHandler<PipelineResponseCommand, PipelineResponse, PipelineResponseCommandHandler>()
            .AddPipelineBehavior<PipelineResponseCommand, PipelineResponse, TracingResponseCommandBehavior>());

        var response = await mediator.DispatchAsync<PipelineResponseCommand, PipelineResponse>(
            new PipelineResponseCommand { Value = "x" });

        Assert.Equal("handled:x", response.Value);
        Assert.Equal(
            new[] { "response-command-behavior:before", "handler", "response-command-behavior:after" },
            provider.GetRequiredService<PipelineTrace>().Steps);
    }

    // ------------------------------------------------------------------
    // Open generic behaviors closed at registration (ADR-010)
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_OpenGenericBehaviorClosedAtRegistration_IsExecuted()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse,
                GenericTracingBehavior<PipelineQuery, PipelineResponse>>());

        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());

        Assert.Equal(
            new[] { "generic<PipelineQuery>:before", "handler", "generic<PipelineQuery>:after" },
            provider.GetRequiredService<PipelineTrace>().Steps);
    }

    // ------------------------------------------------------------------
    // Lifetimes
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_SingletonBehavior_ReusedAcrossDispatches()
    {
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>(
                ServiceLifetime.Singleton));

        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());
        await mediator.SendAsync<PipelineQuery, PipelineResponse>(new PipelineQuery());

        Assert.Equal(6, provider.GetRequiredService<PipelineTrace>().Steps.Count);
    }

    // ------------------------------------------------------------------
    // Concurrency
    // ------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_ConcurrentDispatches_AllPipelinesComplete()
    {
        const int concurrency = 1000;
        var (mediator, provider) = Build(builder => builder
            .AddQueryHandler<PipelineQuery, PipelineResponse, PipelineQueryHandler>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, FirstQueryBehavior>()
            .AddPipelineBehavior<PipelineQuery, PipelineResponse, SecondQueryBehavior>());

        var tasks = Enumerable.Range(0, concurrency)
            .Select(i => mediator.SendAsync<PipelineQuery, PipelineResponse>(
                new PipelineQuery { Value = i.ToString() }))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, response => Assert.StartsWith("handled:", response.Value));
        // 2 behaviors x 2 steps + 1 handler step per dispatch
        Assert.Equal(concurrency * 5, provider.GetRequiredService<PipelineTrace>().Steps.Count);
    }
}
