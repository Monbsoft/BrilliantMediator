using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// TEST FIXTURES
// ============================================================================

public class CancellationTestCommand : ICommand
{
}

public class CancellationTestCommandWithResponse : ICommand<CancellationTestResult>
{
}

public class CancellationTestQuery : IQuery<CancellationTestResult>
{
}

public class CancellationTestEvent : IEvent
{
}

public class CancellationTestResult
{
}

public class TokenCapturingCommandHandler : ICommandHandler<CancellationTestCommand>
{
    public CancellationToken ReceivedToken { get; private set; }

    public Task Handle(CancellationTestCommand command, CancellationToken cancellationToken = default)
    {
        ReceivedToken = cancellationToken;
        return Task.CompletedTask;
    }
}

public class CancellationHonoringCommandHandler : ICommandHandler<CancellationTestCommand>
{
    public Task Handle(CancellationTestCommand command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

public class TokenCapturingCommandWithResponseHandler : ICommandHandler<CancellationTestCommandWithResponse, CancellationTestResult>
{
    public CancellationToken ReceivedToken { get; private set; }

    public Task<CancellationTestResult> Handle(CancellationTestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        ReceivedToken = cancellationToken;
        return Task.FromResult(new CancellationTestResult());
    }
}

public class CancellationHonoringCommandWithResponseHandler : ICommandHandler<CancellationTestCommandWithResponse, CancellationTestResult>
{
    public Task<CancellationTestResult> Handle(CancellationTestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CancellationTestResult());
    }
}

public class TokenCapturingQueryHandler : IQueryHandler<CancellationTestQuery, CancellationTestResult>
{
    public CancellationToken ReceivedToken { get; private set; }

    public Task<CancellationTestResult> Handle(CancellationTestQuery query, CancellationToken cancellationToken = default)
    {
        ReceivedToken = cancellationToken;
        return Task.FromResult(new CancellationTestResult());
    }
}

public class CancellationHonoringQueryHandler : IQueryHandler<CancellationTestQuery, CancellationTestResult>
{
    public Task<CancellationTestResult> Handle(CancellationTestQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CancellationTestResult());
    }
}

public class TokenCapturingEventHandler : IEventHandler<CancellationTestEvent>
{
    public CancellationToken ReceivedToken { get; private set; }

    public Task Handle(CancellationTestEvent @event, CancellationToken cancellationToken = default)
    {
        ReceivedToken = cancellationToken;
        return Task.CompletedTask;
    }
}

public class CancellationHonoringEventHandler : IEventHandler<CancellationTestEvent>
{
    public Task Handle(CancellationTestEvent @event, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}

// ============================================================================
// CANCELLATION TOKEN PROPAGATION TESTS
// ============================================================================

public class CancellationTests
{
    [Fact]
    public async Task DispatchAsync_CancelledToken_TokenIsPropagatedToHandler()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new TokenCapturingCommandHandler();
        serviceProvider.AddCommandHandler(handler);
        mediator.RegisterCommandHandler<CancellationTestCommand>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await mediator.DispatchAsync(new CancellationTestCommand(), cts.Token);

        Assert.True(handler.ReceivedToken.IsCancellationRequested);
    }

    [Fact]
    public async Task DispatchAsync_HandlerHonorsCancellation_ThrowsOperationCanceledException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        serviceProvider.AddCommandHandler(new CancellationHonoringCommandHandler());
        mediator.RegisterCommandHandler<CancellationTestCommand>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mediator.DispatchAsync(new CancellationTestCommand(), cts.Token));
    }

    [Fact]
    public async Task DispatchAsync_CancelledTokenWithResponse_TokenIsPropagatedToHandler()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new TokenCapturingCommandWithResponseHandler();
        serviceProvider.AddCommandHandler<CancellationTestCommandWithResponse, CancellationTestResult>(handler);
        mediator.RegisterCommandHandler<CancellationTestCommandWithResponse, CancellationTestResult>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await mediator.DispatchAsync<CancellationTestCommandWithResponse, CancellationTestResult>(
            new CancellationTestCommandWithResponse(), cts.Token);

        Assert.True(handler.ReceivedToken.IsCancellationRequested);
    }

    [Fact]
    public async Task DispatchAsync_HandlerWithResponseHonorsCancellation_ThrowsOperationCanceledException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        serviceProvider.AddCommandHandler<CancellationTestCommandWithResponse, CancellationTestResult>(
            new CancellationHonoringCommandWithResponseHandler());
        mediator.RegisterCommandHandler<CancellationTestCommandWithResponse, CancellationTestResult>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mediator.DispatchAsync<CancellationTestCommandWithResponse, CancellationTestResult>(
                new CancellationTestCommandWithResponse(), cts.Token));
    }

    [Fact]
    public async Task SendAsync_CancelledToken_TokenIsPropagatedToHandler()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new TokenCapturingQueryHandler();
        serviceProvider.AddQueryHandler<CancellationTestQuery, CancellationTestResult>(handler);
        mediator.RegisterQueryHandler<CancellationTestQuery, CancellationTestResult>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await mediator.SendAsync<CancellationTestQuery, CancellationTestResult>(
            new CancellationTestQuery(), cts.Token);

        Assert.True(handler.ReceivedToken.IsCancellationRequested);
    }

    [Fact]
    public async Task SendAsync_HandlerHonorsCancellation_ThrowsOperationCanceledException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        serviceProvider.AddQueryHandler<CancellationTestQuery, CancellationTestResult>(
            new CancellationHonoringQueryHandler());
        mediator.RegisterQueryHandler<CancellationTestQuery, CancellationTestResult>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mediator.SendAsync<CancellationTestQuery, CancellationTestResult>(
                new CancellationTestQuery(), cts.Token));
    }

    [Fact]
    public async Task PublishAsync_CancelledToken_TokenIsPropagatedToHandler()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new TokenCapturingEventHandler();
        serviceProvider.AddEventHandler<CancellationTestEvent>(handler);
        mediator.RegisterEventHandler<CancellationTestEvent>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await mediator.PublishAsync(new CancellationTestEvent(), cts.Token);

        Assert.True(handler.ReceivedToken.IsCancellationRequested);
    }

    [Fact]
    public async Task PublishAsync_HandlerHonorsCancellation_ThrowsOperationCanceledException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        serviceProvider.AddEventHandler<CancellationTestEvent>(new CancellationHonoringEventHandler());
        mediator.RegisterEventHandler<CancellationTestEvent>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mediator.PublishAsync(new CancellationTestEvent(), cts.Token));
    }
}
