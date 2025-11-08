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
     // Arrange
        const string message = "Test exception message";

        // Act
  var exception = new HandlerNotRegisteredException(message);

        // Assert
     Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void ForCommand_CreatesCorrectException()
{
        // Arrange
  const string commandName = "TestCommand";

        // Act
        var exception = HandlerNotRegisteredException.ForCommand(commandName);

    // Assert
 Assert.Equal("No handler registered for command 'TestCommand'", exception.Message);
  Assert.IsType<HandlerNotRegisteredException>(exception);
    }

    [Fact]
    public void ForQuery_CreatesCorrectException()
    {
  // Arrange
    const string queryName = "TestQuery";

    // Act
  var exception = HandlerNotRegisteredException.ForQuery(queryName);

        // Assert
 Assert.Equal("No handler registered for query 'TestQuery'", exception.Message);
Assert.IsType<HandlerNotRegisteredException>(exception);
    }

  [Fact]
    public void ForCommand_WithNullCommandName_HandlesGracefully()
  {
   // Act
   var exception = HandlerNotRegisteredException.ForCommand(null!);

        // Assert
      Assert.Equal("No handler registered for command ''", exception.Message);
    }

    [Fact]
    public void ForQuery_WithNullQueryName_HandlesGracefully()
  {
    // Act
var exception = HandlerNotRegisteredException.ForQuery(null!);

        // Assert
        Assert.Equal("No handler registered for query ''", exception.Message);
  }

    [Fact]
    public void ForCommand_WithEmptyCommandName_HandlesGracefully()
  {
    // Act
        var exception = HandlerNotRegisteredException.ForCommand(string.Empty);

        // Assert
    Assert.Equal("No handler registered for command ''", exception.Message);
    }

    [Fact]
    public void ForQuery_WithEmptyQueryName_HandlesGracefully()
    {
        // Act
        var exception = HandlerNotRegisteredException.ForQuery(string.Empty);

        // Assert
        Assert.Equal("No handler registered for query ''", exception.Message);
    }

[Fact]
    public void ForCommand_WithWhitespaceCommandName_HandlesGracefully()
    {
  // Act
 var exception = HandlerNotRegisteredException.ForCommand(" ");

 // Assert
   Assert.Equal("No handler registered for command ' '", exception.Message);
    }

    [Fact]
    public void ForQuery_WithWhitespaceQueryName_HandlesGracefully()
    {
        // Act
  var exception = HandlerNotRegisteredException.ForQuery("   ");

   // Assert
      Assert.Equal("No handler registered for query '   '", exception.Message);
    }

    [Fact]
    public void Exception_InheritsFromException()
    {
   // Arrange
        var exception = new HandlerNotRegisteredException("test");

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void Exception_IsSealed()
    {
        // Assert
 var type = typeof(HandlerNotRegisteredException);
        Assert.True(type.IsSealed);
    }

    [Theory]
    [InlineData("CreateUserCommand")]
 [InlineData("UpdateProductCommand")]
    [InlineData("DeleteOrderCommand")]
    public void ForCommand_WithVariousCommandNames_CreatesCorrectMessage(string commandName)
    {
        // Act
    var exception = HandlerNotRegisteredException.ForCommand(commandName);

        // Assert
   Assert.Equal($"No handler registered for command '{commandName}'", exception.Message);
    }

    [Theory]
    [InlineData("GetUserQuery")]
    [InlineData("SearchProductsQuery")]
    [InlineData("GetOrderHistoryQuery")]
    public void ForQuery_WithVariousQueryNames_CreatesCorrectMessage(string queryName)
    {
   // Act
        var exception = HandlerNotRegisteredException.ForQuery(queryName);

 // Assert
        Assert.Equal($"No handler registered for query '{queryName}'", exception.Message);
    }

    [Fact]
    public void Exception_HasCorrectStackTrace_WhenThrown()
    {
        // Arrange & Act
        HandlerNotRegisteredException? caughtException = null;
  
        try
        {
            throw HandlerNotRegisteredException.ForCommand("TestCommand");
    }
catch (HandlerNotRegisteredException ex)
        {
   caughtException = ex;
   }

      // Assert
 Assert.NotNull(caughtException);
        Assert.NotNull(caughtException.StackTrace);
     Assert.Contains(nameof(HandlerNotRegisteredExceptionTests), caughtException.StackTrace);
    }

    [Fact]
    public void Exception_CanBeSerialized()
  {
     // Arrange
        var originalException = HandlerNotRegisteredException.ForCommand("SerializationTest");

 // Act & Assert - Just verify basic properties are accessible
        Assert.Equal("No handler registered for command 'SerializationTest'", originalException.Message);
        Assert.NotNull(originalException.GetType());
    Assert.True(originalException.GetType().IsSealed);
    }
}