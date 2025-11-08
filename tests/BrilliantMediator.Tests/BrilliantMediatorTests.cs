using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Core;
using Monbsoft.BrilliantMediator.Exceptions;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// TEST FIXTURES
// ============================================================================

public class TestCommand : ICommand
{
}

public class TestUnregisteredCommand : ICommand
{
}

public class TestCommandWithResponse : ICommand<TestResult>
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
}

public class QueryResult
{
    public int Id { get; set; }
    public string Data { get; set; }
}

public class TestCommandHandler : ICommandHandler<TestCommand>
{
    public bool Executed { get; set; }

    public Task Handle(TestCommand command)
    {
        Executed = true;
        return Task.CompletedTask;
    }
}

public class TestCommandWithResponseHandler : ICommandHandler<TestCommandWithResponse, TestResult>
{
    public async Task<TestResult> Handle(TestCommandWithResponse command)
    {
        await Task.Delay(10); // Simulate some work
        return new TestResult { Executed = true, Value = command.Value * 2 };
    }
}

public class TestQueryHandler : IQueryHandler<TestQuery, QueryResult>
{
    public async Task<QueryResult> Handle(TestQuery query)
    {
        await Task.Delay(10); // Simulate some work
        return new QueryResult { Id = query.Id, Data = $"Query result for {query.Id}" };
    }
}

// ============================================================================
// TESTS
// ============================================================================

public class MediatorTests
{
    [Fact]
    public async Task Send_CommandWithoutResponse_ExecutesHandler()
    {
        // Arrange
        var mediator = new Mediator();
        var handler = new TestCommandHandler();
        mediator.RegisterCommandHandler(handler);

        // Act
        await mediator.DispatchAsync(new TestCommand());

        // Assert
        Assert.True(handler.Executed);
    }

    [Fact]
    public async Task Send_CommandWithResponse_ReturnsCorrectResult()
    {
        // Arrange
        var mediator = new Mediator();
        var handler = new TestCommandWithResponseHandler();
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>(handler);

        // Act
        var result = await mediator.DispatchAsync<TestCommandWithResponse, TestResult>(
            new TestCommandWithResponse { Value = 5 });

        // Assert
        Assert.True(result.Executed);
        Assert.Equal(10, result.Value); // 5 * 2
    }

    [Fact]
    public async Task Send_Query_ReturnsCorrectResult()
    {
        // Arrange
        var mediator = new Mediator();
        var handler = new TestQueryHandler();
        mediator.RegisterQueryHandler(handler);

        // Act
        var result = await mediator.SendAsync<TestQuery, QueryResult>(
            new TestQuery { Id = 42 });

        // Assert
        Assert.Equal(42, result.Id);
        Assert.Equal("Query result for 42", result.Data);
    }

    [Fact]
    public async Task Send_UnregisteredCommand_ThrowsException()
    {
        // Arrange
        var mediator = new Mediator();

        // Act & Assert
        await Assert.ThrowsAsync<HandlerNotRegisteredException>(
            () => mediator.DispatchAsync(new TestUnregisteredCommand()));
    }

    [Fact]
    public async Task Send_UnregisteredQuery_ThrowsException()
    {
        // Arrange
        var mediator = new Mediator();

        // Act & Assert
        await Assert.ThrowsAsync<HandlerNotRegisteredException>(
            () => mediator.SendAsync<TestUnregisteredQuery, QueryResult>(new TestUnregisteredQuery { Id = 1 }));
    }

    [Fact]
    public void RegisterCommandHandler_WithNullHandler_ThrowsException()
    {
        // Arrange
        var mediator = new Mediator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => mediator.RegisterCommandHandler<TestCommand>(null));
    }

    [Fact]
    public void RegisterQueryHandler_WithNullHandler_ThrowsException()
    {
        // Arrange
        var mediator = new Mediator();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => mediator.RegisterQueryHandler<TestQuery, QueryResult>(null));
    }

    [Fact]
    public async Task Send_MultipleCommands_AllExecuteCorrectly()
    {
        // Arrange
        var mediator = new Mediator();
        var handler1 = new TestCommandHandler();
        var handler2 = new TestCommandWithResponseHandler();

        mediator.RegisterCommandHandler(handler1);
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>(handler2);

        // Act
        await mediator.DispatchAsync(new TestCommand());
        var result = await mediator.DispatchAsync<TestCommandWithResponse, TestResult>(
            new TestCommandWithResponse { Value = 3 });

        // Assert
        Assert.True(handler1.Executed);
        Assert.True(result.Executed);
        Assert.Equal(6, result.Value);
    }

    [Fact]
    public async Task Send_CommandReplacesPreviousHandler()
    {
        // Arrange
        var mediator = new Mediator();
        var handler1 = new TestCommandWithResponseHandler();
        var handler2 = new TestCommandWithResponseHandler();

        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>(handler1);
        mediator.RegisterCommandHandler<TestCommandWithResponse, TestResult>(handler2);

        // Act
        var result = await mediator.DispatchAsync<TestCommandWithResponse, TestResult>(
            new TestCommandWithResponse { Value = 10 });

        // Assert - The second handler should be used
        Assert.Equal(20, result.Value); // 10 * 2
    }
}