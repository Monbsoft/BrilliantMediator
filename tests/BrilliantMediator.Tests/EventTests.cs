using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Exceptions;
using Monbsoft.BrilliantMediator.Extensions;
using System.Collections.Concurrent;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// TEST FIXTURES
// ============================================================================

public class TestEvent : IEvent
{
    public string Payload { get; set; } = string.Empty;
}

public class TestUnregisteredEvent : IEvent
{
}

public class MultiHandlerEvent : IEvent
{
}

public class ScopedHandlersEvent : IEvent
{
}

public class RecordingEventHandler : IEventHandler<TestEvent>
{
    public bool Executed { get; private set; }
    public string? ReceivedPayload { get; private set; }

    public Task Handle(TestEvent @event, CancellationToken cancellationToken = default)
    {
        Executed = true;
        ReceivedPayload = @event.Payload;
        return Task.CompletedTask;
    }
}

public class ThrowingEventHandler : IEventHandler<TestEvent>
{
    public Task Handle(TestEvent @event, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception from event handler");
    }
}

public class CountingEventHandler : IEventHandler<TestEvent>
{
    private int _count;

    public int Count => _count;

    public Task Handle(TestEvent @event, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _count);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Singleton recorder shared by DI-resolved handlers, so executions can be
/// observed even when handler instances are created per scope.
/// </summary>
public class EventExecutionRecorder
{
    private readonly ConcurrentBag<string> _handlerNames = new();
    private readonly ConcurrentBag<Guid> _dependencyIds = new();

    public void RecordHandler(string handlerName) => _handlerNames.Add(handlerName);

    public void RecordDependency(Guid dependencyId) => _dependencyIds.Add(dependencyId);

    public IReadOnlyCollection<string> HandlerNames => _handlerNames;

    public IReadOnlyCollection<Guid> DependencyIds => _dependencyIds;
}

public class FirstRecordingEventHandler : IEventHandler<MultiHandlerEvent>
{
    private readonly EventExecutionRecorder _recorder;

    public FirstRecordingEventHandler(EventExecutionRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Handle(MultiHandlerEvent @event, CancellationToken cancellationToken = default)
    {
        _recorder.RecordHandler(nameof(FirstRecordingEventHandler));
        return Task.CompletedTask;
    }
}

public class SecondRecordingEventHandler : IEventHandler<MultiHandlerEvent>
{
    private readonly EventExecutionRecorder _recorder;

    public SecondRecordingEventHandler(EventExecutionRecorder recorder)
    {
        _recorder = recorder;
    }

    public Task Handle(MultiHandlerEvent @event, CancellationToken cancellationToken = default)
    {
        _recorder.RecordHandler(nameof(SecondRecordingEventHandler));
        return Task.CompletedTask;
    }
}

public class ThrowingMultiHandlerEventHandler : IEventHandler<MultiHandlerEvent>
{
    public Task Handle(MultiHandlerEvent @event, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception from multi-handler event handler");
    }
}

/// <summary>
/// Scoped dependency whose instance identity reveals which DI scope it was resolved from.
/// </summary>
public class ScopedEventDependency
{
    public Guid InstanceId { get; } = Guid.NewGuid();
}

public class FirstScopeCapturingEventHandler : IEventHandler<ScopedHandlersEvent>
{
    private readonly ScopedEventDependency _dependency;
    private readonly EventExecutionRecorder _recorder;

    public FirstScopeCapturingEventHandler(ScopedEventDependency dependency, EventExecutionRecorder recorder)
    {
        _dependency = dependency;
        _recorder = recorder;
    }

    public Task Handle(ScopedHandlersEvent @event, CancellationToken cancellationToken = default)
    {
        _recorder.RecordDependency(_dependency.InstanceId);
        return Task.CompletedTask;
    }
}

public class SecondScopeCapturingEventHandler : IEventHandler<ScopedHandlersEvent>
{
    private readonly ScopedEventDependency _dependency;
    private readonly EventExecutionRecorder _recorder;

    public SecondScopeCapturingEventHandler(ScopedEventDependency dependency, EventExecutionRecorder recorder)
    {
        _dependency = dependency;
        _recorder = recorder;
    }

    public Task Handle(ScopedHandlersEvent @event, CancellationToken cancellationToken = default)
    {
        _recorder.RecordDependency(_dependency.InstanceId);
        return Task.CompletedTask;
    }
}

// ============================================================================
// EVENT PUBLISHING TESTS
// ============================================================================

public class EventPublishingTests
{
    [Fact]
    public async Task PublishAsync_RegisteredEvent_ExecutesHandler()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new RecordingEventHandler();
        serviceProvider.AddEventHandler<TestEvent>(handler);
        mediator.RegisterEventHandler<TestEvent>();

        await mediator.PublishAsync(new TestEvent { Payload = "event data" });

        Assert.True(handler.Executed);
        Assert.Equal("event data", handler.ReceivedPayload);
    }

    [Fact]
    public async Task PublishAsync_NoRegisteredHandler_CompletesWithoutError()
    {
        var (mediator, _) = TestMediatorFactory.Create();

        var task = mediator.PublishAsync(new TestUnregisteredEvent());
        await task;

        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task PublishAsync_EventRegisteredButNoHandlerInDI_ThrowsHandlerNotRegisteredException()
    {
        var (mediator, _) = TestMediatorFactory.Create();
        // Event is registered on the mediator, but no handler is added to the
        // service provider: DI resolution yields no handlers and must throw.
        mediator.RegisterEventHandler<TestEvent>();

        var exception = await Assert.ThrowsAsync<HandlerNotRegisteredException>(
            () => mediator.PublishAsync(new TestEvent()));

        Assert.Equal("No handler registered for event 'TestEvent'", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_MultipleHandlersForSameEvent_AllHandlersExecute()
    {
        var services = new ServiceCollection();
        var recorder = new EventExecutionRecorder();
        services.AddSingleton(recorder);
        services
            .AddBrilliantMediator()
            .AddEventHandler<MultiHandlerEvent, FirstRecordingEventHandler>()
            .AddEventHandler<MultiHandlerEvent, SecondRecordingEventHandler>()
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new MultiHandlerEvent());

        Assert.Equal(2, recorder.HandlerNames.Count);
        Assert.Contains(nameof(FirstRecordingEventHandler), recorder.HandlerNames);
        Assert.Contains(nameof(SecondRecordingEventHandler), recorder.HandlerNames);
    }

    [Fact]
    public async Task PublishAsync_HandlerThrows_PropagatesException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        serviceProvider.AddEventHandler<TestEvent>(new ThrowingEventHandler());
        mediator.RegisterEventHandler<TestEvent>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(new TestEvent()));

        Assert.Equal("Test exception from event handler", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_OneHandlerThrowsAmongMultiple_OtherHandlersStillExecute()
    {
        var services = new ServiceCollection();
        var recorder = new EventExecutionRecorder();
        services.AddSingleton(recorder);
        services
            .AddBrilliantMediator()
            .AddEventHandler<MultiHandlerEvent, ThrowingMultiHandlerEventHandler>()
            .AddEventHandler<MultiHandlerEvent, FirstRecordingEventHandler>()
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Task.WhenAll: all handlers are started; the exception surfaces once
        // every handler task has completed.
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.PublishAsync(new MultiHandlerEvent()));

        Assert.Equal("Test exception from multi-handler event handler", exception.Message);
        Assert.Contains(nameof(FirstRecordingEventHandler), recorder.HandlerNames);
    }

    [Fact]
    public async Task PublishAsync_ConcurrentPublishes_AllExecuteCorrectly()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new CountingEventHandler();
        serviceProvider.AddEventHandler<TestEvent>(handler);
        mediator.RegisterEventHandler<TestEvent>();

        const int concurrentPublishes = 1000;
        var tasks = new Task[concurrentPublishes];
        for (var i = 0; i < concurrentPublishes; i++)
        {
            tasks[i] = mediator.PublishAsync(new TestEvent { Payload = $"event-{i}" });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentPublishes, handler.Count);
    }

    [Fact]
    public async Task RegisterEventHandler_CalledTwiceForSameEvent_HandlersNotExecutedTwice()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new CountingEventHandler();
        serviceProvider.AddEventHandler<TestEvent>(handler);
        mediator.RegisterEventHandler<TestEvent>();
        mediator.RegisterEventHandler<TestEvent>();

        await mediator.PublishAsync(new TestEvent());

        Assert.Equal(1, handler.Count);
    }

    [Fact]
    public async Task PublishAsync_ScopedEventHandlers_EachHandlerGetsOwnScope()
    {
        var services = new ServiceCollection();
        var recorder = new EventExecutionRecorder();
        services.AddSingleton(recorder);
        services.AddScoped<ScopedEventDependency>();
        services
            .AddBrilliantMediator()
            .AddEventHandler<ScopedHandlersEvent, FirstScopeCapturingEventHandler>()
            .AddEventHandler<ScopedHandlersEvent, SecondScopeCapturingEventHandler>()
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new ScopedHandlersEvent());

        // ADR-002: each handler runs in its own scope, so the scoped dependency
        // must be a different instance for each handler.
        Assert.Equal(2, recorder.DependencyIds.Count);
        Assert.Equal(2, recorder.DependencyIds.Distinct().Count());
    }
}
