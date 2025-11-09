using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Core;
using Monbsoft.BrilliantMediator.Extensions;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// TEST FIXTURES FOR MEDIATOR BUILDER
// ============================================================================

public class BuilderTestCommand : ICommand
{
    public string Message { get; set; } = string.Empty;
}

public class BuilderTestCommandWithResponse : ICommand<BuilderTestResult>
{
    public int Value { get; set; }
}

public class BuilderTestQuery : IQuery<BuilderTestQueryResult>
{
    public string Filter { get; set; } = string.Empty;
}

public class BuilderTestResult
{
    public bool Success { get; set; }
    public int ProcessedValue { get; set; }
}

public class BuilderTestQueryResult
{
    public string Data { get; set; } = string.Empty;
    public int Count { get; set; }
}

// Test handlers for DI registration
public class BuilderTestCommandHandler : ICommandHandler<BuilderTestCommand>
{
    public bool WasCalled { get; set; }
    public string? ReceivedMessage { get; set; }

    public Task Handle(BuilderTestCommand command)
    {
        WasCalled = true;
        ReceivedMessage = command.Message;
        return Task.CompletedTask;
    }
}

public class BuilderTestCommandWithResponseHandler : ICommandHandler<BuilderTestCommandWithResponse, BuilderTestResult>
{
    public async Task<BuilderTestResult> Handle(BuilderTestCommandWithResponse command)
    {
        await Task.Delay(1); // Simulate async work
        return new BuilderTestResult
        {
            Success = true,
            ProcessedValue = command.Value * 3
        };
    }
}

public class BuilderTestQueryHandler : IQueryHandler<BuilderTestQuery, BuilderTestQueryResult>
{
    public async Task<BuilderTestQueryResult> Handle(BuilderTestQuery query)
    {
        await Task.Delay(1); // Simulate async work
        return new BuilderTestQueryResult
        {
            Data = $"Filtered by: {query.Filter}",
            Count = query.Filter.Length
        };
    }
}

// Test handlers with dependencies
public interface ITestService
{
    string GetData();
}

public class TestService : ITestService
{
    public string GetData() => "Service Data";
}

public class HandlerWithDependency : ICommandHandler<BuilderTestCommand>
{
    private readonly ITestService _service;

    public HandlerWithDependency(ITestService service)
    {
        _service = service;
    }

    public bool WasCalled { get; set; }
    public string? ServiceData { get; set; }

    public Task Handle(BuilderTestCommand command)
    {
        WasCalled = true;
        ServiceData = _service.GetData();
        return Task.CompletedTask;
    }
}

// ============================================================================
// MEDIATOR BUILDER EXTENSION TESTS
// ============================================================================

public class MediatorBuilderExtensionTests
{
    [Fact]
    public void AddBrilliantMediator_WithNullServices_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(
       () => BrilliantMediatorExtensions.AddBrilliantMediator(null!));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddBrilliantMediator_ReturnsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddBrilliantMediator();

        // Assert
        Assert.NotNull(builder);
        Assert.IsType<MediatorBuilder>(builder);
    }

    [Fact]
    public void AddBrilliantMediator_RegistersMediatorAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBrilliantMediator().Build();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Get mediator twice and verify it's the same instance
        var mediator1 = serviceProvider.GetRequiredService<IMediator>();
        var mediator2 = serviceProvider.GetRequiredService<IMediator>();

        Assert.NotNull(mediator1);
        Assert.NotNull(mediator2);
        Assert.Same(mediator1, mediator2); // Singleton verification
    }

    [Fact]
    public void AddBrilliantMediator_RegistersMediatorAsIMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBrilliantMediator().Build();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
    }
}

// ============================================================================
// HANDLER REGISTRATION TESTS
// ============================================================================

public class MediatorBuilderHandlerRegistrationTests
{
    [Fact]
    public void AddCommandHandler_RegistersHandlerInDI()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
               .AddBrilliantMediator()
               .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
               .Build();

        var serviceProvider = result.BuildServiceProvider();

        // Assert
        var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
        Assert.NotNull(handler);
        Assert.IsType<BuilderTestCommandHandler>(handler);
    }

    [Fact]
    public void AddCommandHandler_WithResponse_RegistersHandlerInDI()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
      .AddBrilliantMediator()
               .AddCommandHandler<BuilderTestCommandWithResponse, BuilderTestResult, BuilderTestCommandWithResponseHandler>()
      .Build();

        var serviceProvider = result.BuildServiceProvider();

        // Assert
        var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommandWithResponse, BuilderTestResult>>();
        Assert.NotNull(handler);
        Assert.IsType<BuilderTestCommandWithResponseHandler>(handler);
    }

    [Fact]
    public void AddQueryHandler_RegistersHandlerInDI()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services
         .AddBrilliantMediator()
       .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
     .Build();

        var serviceProvider = result.BuildServiceProvider();

        // Assert
        var handler = serviceProvider.GetService<IQueryHandler<BuilderTestQuery, BuilderTestQueryResult>>();
        Assert.NotNull(handler);
        Assert.IsType<BuilderTestQueryHandler>(handler);
    }

    [Fact]
    public async Task AddCommandHandlerInstance_RegistersInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services
            .AddBrilliantMediator()
            .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
            .Build();

        var serviceProvider = services.BuildServiceProvider();

        // Initialize mediator
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var initializer = serviceProvider.GetRequiredService<IMediatorInitializer>();
        initializer.Initialize(mediator);

        // Act - Execute command
        await mediator.DispatchAsync(new BuilderTestCommand { Message = "test" });

        // Assert - Verify the handler was registered and executed
        // Since handlers are scoped, we verify by checking that no exception was thrown
        // The fact that DispatchAsync completed successfully means the handler was found and executed
        var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task AddQueryHandlerInstance_RegistersInstance()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services
        .AddBrilliantMediator()
        .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
                  .Build();

        var serviceProvider = services.BuildServiceProvider();

        // Initialize mediator
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var initializer = serviceProvider.GetRequiredService<IMediatorInitializer>();
        initializer.Initialize(mediator);

        // Assert - Verify the instance was actually registered
        var result = await mediator.SendAsync<BuilderTestQuery, BuilderTestQueryResult>(
     new BuilderTestQuery { Filter = "test" });

        Assert.NotNull(result);
        Assert.Equal("Filtered by: test", result.Data);
        Assert.Equal(4, result.Count);
    }

    // ============================================================================
    // FLUENT BUILDER TESTS
    // ============================================================================

    public class MediatorBuilderFluentTests
    {
        [Fact]
        public void FluentBuilder_CanChainMultipleRegistrations()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert - Should not throw
            var result = services
                   .AddBrilliantMediator()
                    .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
              .AddCommandHandler<BuilderTestCommandWithResponse, BuilderTestResult, BuilderTestCommandWithResponseHandler>()
                .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
                           .Build();

            Assert.NotNull(result);
        }

        [Fact]
        public void FluentBuilder_ReturnsBuilderForChaining()
        {
            // Arrange
            var services = new ServiceCollection();
            var builder = services.AddBrilliantMediator();

            // Act
            var chainedBuilder = builder.AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>();

            // Assert
            Assert.NotNull(chainedBuilder);
            Assert.Same(builder, chainedBuilder); // Should return the same builder instance
        }

        [Fact]
        public void Build_ReturnsServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            var result = services.AddBrilliantMediator().Build();

            // Assert
            Assert.NotNull(result);
            Assert.Same(result, services); // Build should return the same collection
        }
    }

    // ============================================================================
    // SERVICE LIFETIME TESTS
    // ============================================================================

    public class MediatorBuilderServiceLifetimeTests
    {
        [Fact]
        public void ServiceLifetime_Singleton_ReturnsSameInstance()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
                       .AddBrilliantMediator()
              .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>(ServiceLifetime.Singleton)
                       .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Assert - Get same instance twice for singleton
            var handler1 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            var handler2 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

            Assert.NotNull(handler1);
            Assert.NotNull(handler2);
            Assert.Same(handler1, handler2);
        }

        [Fact]
        public void ServiceLifetime_Transient_ReturnsDifferentInstance()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
                   .AddBrilliantMediator()
                   .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>(ServiceLifetime.Transient)
            .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Assert - Get different instances for transient
            var handler1 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            var handler2 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

            Assert.NotNull(handler1);
            Assert.NotNull(handler2);
            Assert.NotSame(handler1, handler2); // Should be different instances
        }

        [Fact]
        public void ServiceLifetime_Scoped_ReturnsSameInstanceWithinScope()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>(ServiceLifetime.Scoped)
       .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Assert - Same instance within same scope
            using (var scope = serviceProvider.CreateScope())
            {
                var handler1 = scope.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
                var handler2 = scope.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

                Assert.NotNull(handler1);
                Assert.NotNull(handler2);
                Assert.Same(handler1, handler2);
            }

            // Different instance in different scope
            using (var scope1 = serviceProvider.CreateScope())
            using (var scope2 = serviceProvider.CreateScope())
            {
                var handler1 = scope1.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
                var handler2 = scope2.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

                Assert.NotNull(handler1);
                Assert.NotNull(handler2);
                Assert.NotSame(handler1, handler2); // Different scopes should have different instances
            }
        }
    }

    // ============================================================================
    // HANDLER DEPENDENCY INJECTION TESTS
    // ============================================================================

    public class MediatorBuilderDependencyInjectionTests
    {
        [Fact]
        public void HandlerWithDependencies_CanBeResolved()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            // Act
            services
                  .AddBrilliantMediator()
                    .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
                    .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            Assert.NotNull(handler);
            Assert.IsType<HandlerWithDependency>(handler);
        }

        [Fact]
        public void HandlerWithDependencies_DependenciesAreInjected()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            // Act
            services
        .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>() as HandlerWithDependency;

            // Assert
            Assert.NotNull(handler);
            // Service should be injected (we'll verify this by executing the handler)
            var task = handler.Handle(new BuilderTestCommand());
            task.Wait();
        }

        [Fact]
        public void HandlerWithMissingDependencies_ThrowsOnResolution()
        {
            // Arrange
            var services = new ServiceCollection();
            // Don't register ITestService

            // Act
            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => serviceProvider.GetRequiredService<ICommandHandler<BuilderTestCommand>>());

            Assert.Contains("ITestService", exception.Message);
        }
    }

    // ============================================================================
    // INTEGRATION TESTS FOR MEDIATOR BUILDER
    // ============================================================================

    public class MediatorBuilderIntegrationTests
    {
        [Fact]
        public async Task FullIntegration_AllHandlerTypes_WorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            services
                .AddBrilliantMediator()
              .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
                .AddCommandHandler<BuilderTestCommandWithResponse, BuilderTestResult, BuilderTestCommandWithResponseHandler>()
            .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Initialize mediator
            var mediator = serviceProvider.GetRequiredService<IMediator>();
            var initializer = serviceProvider.GetRequiredService<IMediatorInitializer>();
            initializer.Initialize(mediator);

            // Act & Assert - Command without response
            await mediator.DispatchAsync(new BuilderTestCommand { Message = "test message" });

            // Act & Assert - Command with response
            var commandResult = await mediator.DispatchAsync<BuilderTestCommandWithResponse, BuilderTestResult>(
           new BuilderTestCommandWithResponse { Value = 5 });

            Assert.True(commandResult.Success);
            Assert.Equal(15, commandResult.ProcessedValue);

            // Act & Assert - Query
            var queryResult = await mediator.SendAsync<BuilderTestQuery, BuilderTestQueryResult>(
   new BuilderTestQuery { Filter = "test filter" });

            Assert.Equal("Filtered by: test filter", queryResult.Data);
            Assert.Equal(11, queryResult.Count); // "test filter".Length
        }

        [Fact]
        public async Task HandlerWithDependencies_Integration_WorksCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            services
  .AddBrilliantMediator()
   .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
 .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Initialize mediator
            var mediator = serviceProvider.GetRequiredService<IMediator>();
            var initializer = serviceProvider.GetRequiredService<IMediatorInitializer>();
            initializer.Initialize(mediator);

            // Act - Execute command
            await mediator.DispatchAsync(new BuilderTestCommand { Message = "dependency test" });

            // Assert - Since handlers are resolved per scope, we can't check state directly
            // Instead, verify that the handler can be resolved with its dependencies
            var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            Assert.NotNull(handler);
            Assert.IsType<HandlerWithDependency>(handler);

            // Execute the handler directly to verify dependencies are injected
            await handler.Handle(new BuilderTestCommand { Message = "test" });
            var handlerWithDep = (HandlerWithDependency)handler;
            Assert.True(handlerWithDep.WasCalled);
            Assert.Equal("Service Data", handlerWithDep.ServiceData);
        }

        [Fact]
        public void MediatorInitialization_OnlyHappensOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            services
            .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
               .Build();

            var serviceProvider = services.BuildServiceProvider();

            // Act - Get mediator multiple times
            var mediator1 = serviceProvider.GetRequiredService<IMediator>();
            var mediator2 = serviceProvider.GetRequiredService<IMediator>();

            // Assert - Should be the same instance (singleton)
            Assert.Same(mediator1, mediator2);
        }

        [Fact]
        public void MultipleHandlers_AllRegisteredCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services
               .AddBrilliantMediator()
             .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
                    .AddCommandHandler<BuilderTestCommandWithResponse, BuilderTestResult, BuilderTestCommandWithResponseHandler>()
                .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
             .Build();

   var serviceProvider = services.BuildServiceProvider();

            // Assert - Verify all handlers are properly registered by checking DI
            var commandHandler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            var commandWithResponseHandler = serviceProvider.GetService<ICommandHandler<BuilderTestCommandWithResponse, BuilderTestResult>>();
            var queryHandler = serviceProvider.GetService<IQueryHandler<BuilderTestQuery, BuilderTestQueryResult>>();

            Assert.NotNull(commandHandler);
            Assert.NotNull(commandWithResponseHandler);
            Assert.NotNull(queryHandler);
        }
    }
}