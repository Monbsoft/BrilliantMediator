using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Extensions;
using System.Collections.Concurrent;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// TEST FIXTURES
// ============================================================================

public class ScopingTestCommand : ICommand
{
}

/// <summary>
/// Singleton recorder collecting handler instance identities across dispatches.
/// </summary>
public class HandlerInstanceRecorder
{
    private readonly ConcurrentBag<Guid> _instanceIds = new();

    public void Record(Guid instanceId) => _instanceIds.Add(instanceId);

    public IReadOnlyCollection<Guid> InstanceIds => _instanceIds;
}

public class InstanceTrackingCommandHandler : ICommandHandler<ScopingTestCommand>
{
    private readonly HandlerInstanceRecorder _recorder;
    private readonly Guid _instanceId = Guid.NewGuid();

    public InstanceTrackingCommandHandler(HandlerInstanceRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Handle(ScopingTestCommand command, CancellationToken cancellationToken = default)
    {
        _recorder.Record(_instanceId);
        return Task.CompletedTask;
    }
}

public class DisposableScopedDependency : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

/// <summary>
/// Singleton tracker exposing the scoped dependency instance used during the last dispatch.
/// </summary>
public class ScopedDependencyTracker
{
    public DisposableScopedDependency? LastDependency { get; set; }
}

public class DependencyCapturingCommandHandler : ICommandHandler<ScopingTestCommand>
{
    private readonly DisposableScopedDependency _dependency;
    private readonly ScopedDependencyTracker _tracker;

    public DependencyCapturingCommandHandler(DisposableScopedDependency dependency, ScopedDependencyTracker tracker)
    {
        _dependency = dependency;
        _tracker = tracker;
    }

    public Task Handle(ScopingTestCommand command, CancellationToken cancellationToken = default)
    {
        _tracker.LastDependency = _dependency;
        return Task.CompletedTask;
    }
}

// ============================================================================
// DI SCOPING TESTS (real ServiceCollection)
// ============================================================================

public class ScopingTests
{
    [Fact]
    public async Task DispatchAsync_ScopedHandler_NewInstancePerDispatch()
    {
        var services = new ServiceCollection();
        var recorder = new HandlerInstanceRecorder();
        services.AddSingleton(recorder);
        services
            .AddBrilliantMediator()
            .AddCommandHandler<ScopingTestCommand, InstanceTrackingCommandHandler>(ServiceLifetime.Scoped)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.DispatchAsync(new ScopingTestCommand());
        await mediator.DispatchAsync(new ScopingTestCommand());

        Assert.Equal(2, recorder.InstanceIds.Count);
        Assert.Equal(2, recorder.InstanceIds.Distinct().Count());
    }

    [Fact]
    public async Task DispatchAsync_ScopedDependency_DisposedAfterDispatch()
    {
        var services = new ServiceCollection();
        var tracker = new ScopedDependencyTracker();
        services.AddSingleton(tracker);
        services.AddScoped<DisposableScopedDependency>();
        services
            .AddBrilliantMediator()
            .AddCommandHandler<ScopingTestCommand, DependencyCapturingCommandHandler>()
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.DispatchAsync(new ScopingTestCommand());

        // The dispatch scope is disposed after the handler completes,
        // so the scoped dependency must be disposed as well (ADR-002).
        Assert.NotNull(tracker.LastDependency);
        Assert.True(tracker.LastDependency!.IsDisposed);
    }

    [Fact]
    public async Task DispatchAsync_SingletonHandler_SameInstanceAcrossDispatches()
    {
        var services = new ServiceCollection();
        var recorder = new HandlerInstanceRecorder();
        services.AddSingleton(recorder);
        services
            .AddBrilliantMediator()
            .AddCommandHandler<ScopingTestCommand, InstanceTrackingCommandHandler>(ServiceLifetime.Singleton)
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.DispatchAsync(new ScopingTestCommand());
        await mediator.DispatchAsync(new ScopingTestCommand());

        Assert.Equal(2, recorder.InstanceIds.Count);
        Assert.Single(recorder.InstanceIds.Distinct());
    }
}
