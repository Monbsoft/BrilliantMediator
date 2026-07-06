using Monbsoft.BrilliantMediator.Exceptions;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// EXCEPTION TESTS
// ============================================================================

public class HandlerNotRegisteredExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        const string message = "Test exception message";
        var exception = new HandlerNotRegisteredException(message);

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ForCommand_CreatesCorrectException()
    {
        var exception = HandlerNotRegisteredException.ForCommand("TestCommand");

        Assert.Equal("No handler registered for command 'TestCommand'", exception.Message);
        Assert.IsType<HandlerNotRegisteredException>(exception);
    }

    [Fact]
    public void ForQuery_CreatesCorrectException()
    {
        var exception = HandlerNotRegisteredException.ForQuery("TestQuery");

        Assert.Equal("No handler registered for query 'TestQuery'", exception.Message);
        Assert.IsType<HandlerNotRegisteredException>(exception);
    }

    [Fact]
    public void ForEvent_CreatesCorrectException()
    {
        var exception = HandlerNotRegisteredException.ForEvent("TestEvent");

        Assert.Equal("No handler registered for event 'TestEvent'", exception.Message);
        Assert.IsType<HandlerNotRegisteredException>(exception);
    }

    [Fact]
    public void ForCommand_WithNullCommandName_HandlesGracefully()
    {
        var exception = HandlerNotRegisteredException.ForCommand(null!);
        Assert.Equal("No handler registered for command ''", exception.Message);
    }

    [Fact]
    public void ForQuery_WithNullQueryName_HandlesGracefully()
    {
        var exception = HandlerNotRegisteredException.ForQuery(null!);
        Assert.Equal("No handler registered for query ''", exception.Message);
    }

    [Fact]
    public void ForCommand_WithEmptyCommandName_HandlesGracefully()
    {
        var exception = HandlerNotRegisteredException.ForCommand(string.Empty);
        Assert.Equal("No handler registered for command ''", exception.Message);
    }

    [Fact]
    public void ForQuery_WithEmptyQueryName_HandlesGracefully()
    {
        var exception = HandlerNotRegisteredException.ForQuery(string.Empty);
        Assert.Equal("No handler registered for query ''", exception.Message);
    }

    [Fact]
    public void ForCommand_WithWhitespaceCommandName_HandlesGracefully()
    {
        var exception = HandlerNotRegisteredException.ForCommand(" ");
        Assert.Equal("No handler registered for command ' '", exception.Message);
    }

    [Fact]
    public void ForQuery_WithWhitespaceQueryName_HandlesGracefully()
    {
        var exception = HandlerNotRegisteredException.ForQuery("   ");
        Assert.Equal("No handler registered for query '   '", exception.Message);
    }

    [Fact]
    public void Exception_InheritsFromException()
    {
        var exception = new HandlerNotRegisteredException("test");
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Exception_IsSealed()
    {
        var type = typeof(HandlerNotRegisteredException);
        Assert.True(type.IsSealed);
    }

    [Theory]
    [InlineData("CreateUserCommand")]
    [InlineData("UpdateProductCommand")]
    [InlineData("DeleteOrderCommand")]
    public void ForCommand_WithVariousCommandNames_CreatesCorrectMessage(string commandName)
    {
        var exception = HandlerNotRegisteredException.ForCommand(commandName);
        Assert.Equal($"No handler registered for command '{commandName}'", exception.Message);
    }

    [Theory]
    [InlineData("GetUserQuery")]
    [InlineData("SearchProductsQuery")]
    [InlineData("GetOrderHistoryQuery")]
    public void ForQuery_WithVariousQueryNames_CreatesCorrectMessage(string queryName)
    {
        var exception = HandlerNotRegisteredException.ForQuery(queryName);
        Assert.Equal($"No handler registered for query '{queryName}'", exception.Message);
    }
}
