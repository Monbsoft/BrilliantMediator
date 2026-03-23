using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Core;
using Monbsoft.BrilliantMediator.Exceptions;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// TEST FIXTURES
// ============================================================================

public class TestCommand : ICommand
{
    public string Data { get; set; } = string.Empty;
}

public class TestUnregisteredCommand : ICommand
{
}

public class TestCommandWithResponse : ICommand<TestResult>
{
    public int Value { get; set; }
}

public class TestUnregisteredCommandWithResponse : ICommand<TestResult>
{
    public int Value { get; set; }
}

public class TestQuery : IQuery<QueryResult>
{
    public int Id { get; set; }
}

public class TestUnregisteredQuery : IQuery<QueryResult>
{
    public int Id { get; set; }
}

public class TestResult
{
    public bool Executed { get; set; }
    public int Value { get; set; }
    public string? Message { get; set; }
}

public class QueryResult
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

// Test handlers
public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public bool Executed { get; set; }
    public string? ReceivedData { get; set; }

    public Task Handle(TestCommand command, CancellationToken cancellationToken = default)
    {
        Executed = true;
        ReceivedData = command.Data;
        return Task.CompletedTask;
    }
}

public class ThrowingCommandHandler : ICommandHandler<TestCommand>
{
    public Task Handle(TestCommand command, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception from command handler");
    }
}

public class TestCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, TestResult>
{
    public async Task<TestResult> Handle(TestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return new TestResult
        {
            Executed = true,
            Value = command.Value * 2,
            Message = "Processed successfully"
        };
    }
}

public class OptimizedCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, TestResult>
{
    public Task<TestResult> Handle(TestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TestResult
        {
            Executed = true,
            Value = command.Value * 2,
            Message = "Processed successfully"
        });
    }
}

public class ThrowingCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, TestResult>
{
    public Task<TestResult> Handle(TestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception from command with response handler");
    }
}

public class TestQueryHandler : IQueryHandler<TestQuery, QueryResult>
{
    public async Task<QueryResult> Handle(TestQuery query, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return new QueryResult { Id = query.Id, Data = $"Query result for {query.Id}" };
    }
}

public class OptimizedQueryHandler : IQueryHandler<TestQuery, QueryResult>
{
    public Task<QueryResult> Handle(TestQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new QueryResult { Id = query.Id, Data = $"Query result for {query.Id}" });
    }
}

public class ThrowingQueryHandler : IQueryHandler<TestQuery, QueryResult>
{
    public Task<QueryResult> Handle(TestQuery query, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Test exception from query handler");
    }
}

// ============================================================================
// MEDIATOR CORE FUNCTIONALITY TESTS
// ============================================================================

public class MediatorCoreTests
{
    [Fact]
    public async Task DispatchAsync_CommandWithoutResponse_ExecutesHandler()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new TestCommandHandler();
        serviceProvider.AddCommandHandler(handler);
        mediator.RegisterCommandHandler<TestCommand>();
        var command = new TestCommand { Data = "test data" };

        await mediator.DispatchAsync(command);

        Assert.True(handler.Executed);
        Assert.Equal("test data", handler.ReceivedData);
    }

    [Fact]
    public async Task DispatchAsync_CommandWithResponse_ReturnsCorrectResult()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new TestCommandWithResponseHandler();
        serviceProvider.AddCommandHandler<TestCommandWithResponse, TestResult>(handler);
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>();
        var command = new TestCommandWithResponse { Value = 5 };

        var result = await mediator.DispatchAsync<TestCommandWithResponse, TestResult>(command);

        Assert.NotNull(result);
        Assert.True(result.Executed);
        Assert.Equal(10, result.Value);
        Assert.Equal("Processed successfully", result.Message);
    }

    [Fact]
    public async Task SendAsync_Query_ReturnsCorrectResult()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new TestQueryHandler();
        serviceProvider.AddQueryHandler<TestQuery, QueryResult>(handler);
        mediator.RegisterQueryHandler<TestQuery, QueryResult>();
        var query = new TestQuery { Id = 42 };

        var result = await mediator.SendAsync<TestQuery, QueryResult>(query);

        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Query result for 42", result.Data);
    }

    [Fact]
    public async Task DispatchAsync_UnregisteredCommand_ThrowsHandlerNotRegisteredException()
    {
        var (mediator, _) = TestMediatorFactory.Create();
        var command = new TestUnregisteredCommand();

        var exception = await Assert.ThrowsAsync<HandlerNotRegisteredException>(
            () => mediator.DispatchAsync(command));

        Assert.Contains("TestUnregisteredCommand", exception.Message);
        Assert.Contains("No handler registered for command", exception.Message);
    }

    [Fact]
    public async Task DispatchAsync_UnregisteredCommandWithResponse_ThrowsHandlerNotRegisteredException()
    {
        var (mediator, _) = TestMediatorFactory.Create();
        var command = new TestUnregisteredCommandWithResponse { Value = 5 };

        var exception = await Assert.ThrowsAsync<HandlerNotRegisteredException>(
            () => mediator.DispatchAsync<TestUnregisteredCommandWithResponse, TestResult>(command));

        Assert.Contains("TestUnregisteredCommandWithResponse", exception.Message);
        Assert.Contains("No handler registered for command", exception.Message);
    }

    [Fact]
    public async Task SendAsync_UnregisteredQuery_ThrowsHandlerNotRegisteredException()
    {
        var (mediator, _) = TestMediatorFactory.Create();
        var query = new TestUnregisteredQuery { Id = 1 };

        var exception = await Assert.ThrowsAsync<HandlerNotRegisteredException>(
            () => mediator.SendAsync<TestUnregisteredQuery, QueryResult>(query));

        Assert.Contains("TestUnregisteredQuery", exception.Message);
        Assert.Contains("No handler registered for query", exception.Message);
    }
}

// ============================================================================
// HANDLER REGISTRATION TESTS
// ============================================================================

public class HandlerRegistrationTests
{
    [Fact]
    public async Task RegisterCommandHandler_ReplacesPreviousHandler()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler1 = new TestCommandWithResponseHandler();
        var handler2 = new TestCommandWithResponseHandler();

        serviceProvider.AddCommandHandler<TestCommandWithResponse, TestResult>(handler1);
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>();
        serviceProvider.AddCommandHandler<TestCommandWithResponse, TestResult>(handler2);
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>();

        var command = new TestCommandWithResponse { Value = 10 };
        var result = await mediator.DispatchAsync<TestCommandWithResponse, TestResult>(command);

        Assert.Equal(20, result.Value);
    }

    [Fact]
    public async Task RegisterMultipleHandlers_AllTypesWork()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var commandHandler = new TestCommandHandler();
        var commandWithResponseHandler = new TestCommandWithResponseHandler();
        var queryHandler = new TestQueryHandler();

        serviceProvider.AddCommandHandler(commandHandler);
        serviceProvider.AddCommandHandler<TestCommandWithResponse, TestResult>(commandWithResponseHandler);
        serviceProvider.AddQueryHandler<TestQuery, QueryResult>(queryHandler);

        mediator.RegisterCommandHandler<TestCommand>();
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>();
        mediator.RegisterQueryHandler<TestQuery, QueryResult>();

        await mediator.DispatchAsync(new TestCommand { Data = "test" });
        Assert.True(commandHandler.Executed);
        Assert.Equal("test", commandHandler.ReceivedData);

        var commandResult = await mediator.DispatchAsync<TestCommandWithResponse, TestResult>(
            new TestCommandWithResponse { Value = 3 });
        Assert.True(commandResult.Executed);
        Assert.Equal(6, commandResult.Value);

        var queryResult = await mediator.SendAsync<TestQuery, QueryResult>(
            new TestQuery { Id = 123 });
        Assert.Equal(123, queryResult.Id);
        Assert.Equal("Query result for 123", queryResult.Data);
    }
}

// ============================================================================
// ERROR HANDLING TESTS
// ============================================================================

public class ErrorHandlingTests
{
    [Fact]
    public async Task DispatchAsync_HandlerThrowsException_PropagatesException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new ThrowingCommandHandler();
        serviceProvider.AddCommandHandler(handler);
        mediator.RegisterCommandHandler<TestCommand>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.DispatchAsync(new TestCommand()));

        Assert.Equal("Test exception from command handler", exception.Message);
    }

    [Fact]
    public async Task DispatchAsync_CommandWithResponseHandlerThrowsException_PropagatesException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new ThrowingCommandWithResponseHandler();
        serviceProvider.AddCommandHandler<TestCommandWithResponse, TestResult>(handler);
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.DispatchAsync<TestCommandWithResponse, TestResult>(
                new TestCommandWithResponse { Value = 5 }));

        Assert.Equal("Test exception from command with response handler", exception.Message);
    }

    [Fact]
    public async Task SendAsync_QueryHandlerThrowsException_PropagatesException()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new ThrowingQueryHandler();
        serviceProvider.AddQueryHandler<TestQuery, QueryResult>(handler);
        mediator.RegisterQueryHandler<TestQuery, QueryResult>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.SendAsync<TestQuery, QueryResult>(new TestQuery { Id = 1 }));

        Assert.Equal("Test exception from query handler", exception.Message);
    }
}

// ============================================================================
// CONCURRENCY TESTS
// ============================================================================

public class ConcurrencyTests
{
    [Fact]
    public async Task DispatchAsync_MultipleConcurrentRequests_AllExecuteCorrectly()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new OptimizedCommandWithResponseHandler();
        serviceProvider.AddCommandHandler<TestCommandWithResponse, TestResult>(handler);
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>();

        var tasks = new List<Task<TestResult>>();
        const int concurrentRequests = 1000;

        for (int i = 0; i < concurrentRequests; i++)
        {
            var command = new TestCommandWithResponse { Value = i };
            tasks.Add(mediator.DispatchAsync<TestCommandWithResponse, TestResult>(command));
        }

        var results = await Task.WhenAll(tasks);

        Assert.Equal(concurrentRequests, results.Length);
        for (int i = 0; i < concurrentRequests; i++)
        {
            Assert.True(results[i].Executed);
            Assert.Equal(i * 2, results[i].Value);
        }
    }

    [Fact]
    public async Task SendAsync_MultipleConcurrentQueries_AllExecuteCorrectly()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var handler = new OptimizedQueryHandler();
        serviceProvider.AddQueryHandler<TestQuery, QueryResult>(handler);
        mediator.RegisterQueryHandler<TestQuery, QueryResult>();

        var tasks = new List<Task<QueryResult>>();
        const int concurrentRequests = 1000;

        for (int i = 0; i < concurrentRequests; i++)
        {
            var query = new TestQuery { Id = i };
            tasks.Add(mediator.SendAsync<TestQuery, QueryResult>(query));
        }

        var results = await Task.WhenAll(tasks);

        Assert.Equal(concurrentRequests, results.Length);
        for (int i = 0; i < concurrentRequests; i++)
        {
            Assert.Equal(i, results[i].Id);
            Assert.Equal($"Query result for {i}", results[i].Data);
        }
    }

    [Fact]
    public async Task MixedConcurrentOperations_AllTypesExecuteCorrectly()
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var commandHandler = new TestCommandHandler();
        var commandWithResponseHandler = new OptimizedCommandWithResponseHandler();
        var queryHandler = new OptimizedQueryHandler();

        serviceProvider.AddCommandHandler(commandHandler);
        serviceProvider.AddCommandHandler<TestCommandWithResponse, TestResult>(commandWithResponseHandler);
        serviceProvider.AddQueryHandler<TestQuery, QueryResult>(queryHandler);

        mediator.RegisterCommandHandler<TestCommand>();
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>();
        mediator.RegisterQueryHandler<TestQuery, QueryResult>();

        const int operationsPerType = 100;
        var allTasks = new List<Task>();

        for (int i = 0; i < operationsPerType; i++)
        {
            allTasks.Add(mediator.DispatchAsync(new TestCommand { Data = $"cmd-{i}" }));
        }

        var commandTasks = new List<Task<TestResult>>();
        for (int i = 0; i < operationsPerType; i++)
        {
            var task = mediator.DispatchAsync<TestCommandWithResponse, TestResult>(
                new TestCommandWithResponse { Value = i });
            commandTasks.Add(task);
            allTasks.Add(task);
        }

        var queryTasks = new List<Task<QueryResult>>();
        for (int i = 0; i < operationsPerType; i++)
        {
            var task = mediator.SendAsync<TestQuery, QueryResult>(new TestQuery { Id = i });
            queryTasks.Add(task);
            allTasks.Add(task);
        }

        await Task.WhenAll(allTasks);

        Assert.True(commandHandler.Executed);

        var commandResults = await Task.WhenAll(commandTasks);
        Assert.All(commandResults, result => Assert.True(result.Executed));

        var queryResults = await Task.WhenAll(queryTasks);
        Assert.Equal(operationsPerType, queryResults.Length);
        for (int i = 0; i < operationsPerType; i++)
        {
            Assert.Equal(i, queryResults[i].Id);
        }
    }
}
