using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
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

public class BuilderTestCommandHandler : ICommandHandler<BuilderTestCommand>
{
    public bool WasCalled { get; set; }
    public string? ReceivedMessage { get; set; }

    public Task Handle(BuilderTestCommand command, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        ReceivedMessage = command.Message;
        return Task.CompletedTask;
    }
}

public class BuilderTestCommandWithResponseHandler : ICommandHandler<BuilderTestCommandWithResponse, BuilderTestResult>
{
    public async Task<BuilderTestResult> Handle(BuilderTestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return new BuilderTestResult
        {
            Success = true,
            ProcessedValue = command.Value * 3
        };
    }
}

public class BuilderTestQueryHandler : IQueryHandler<BuilderTestQuery, BuilderTestQueryResult>
{
    public async Task<BuilderTestQueryResult> Handle(BuilderTestQuery query, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken);
        return new BuilderTestQueryResult
        {
            Data = $"Filtered by: {query.Filter}",
            Count = query.Filter.Length
        };
    }
}

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

    public Task Handle(BuilderTestCommand command, CancellationToken cancellationToken = default)
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
        var exception = Assert.Throws<ArgumentNullException>(
            () => BrilliantMediatorExtensions.AddBrilliantMediator(null!));

        Assert.Equal("services", exception.ParamName);
    }

    [Fact]
    public void AddBrilliantMediator_ReturnsBuilder()
    {
        var services = new ServiceCollection();
        var builder = services.AddBrilliantMediator();

        Assert.NotNull(builder);
        Assert.IsType<MediatorBuilder>(builder);
    }

    [Fact]
    public void AddBrilliantMediator_RegistersMediatorAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddBrilliantMediator().Build();
        var serviceProvider = services.BuildServiceProvider();

        var mediator1 = serviceProvider.GetRequiredService<IMediator>();
        var mediator2 = serviceProvider.GetRequiredService<IMediator>();

        Assert.NotNull(mediator1);
        Assert.NotNull(mediator2);
        Assert.Same(mediator1, mediator2);
    }

    [Fact]
    public void AddBrilliantMediator_RegistersMediatorAsIMediator()
    {
        var services = new ServiceCollection();
        services.AddBrilliantMediator().Build();
        var serviceProvider = services.BuildServiceProvider();

        var mediator = serviceProvider.GetService<IMediator>();
        Assert.NotNull(mediator);
        Assert.IsType<Mediator>(mediator);
    }

    [Fact]
    public void AddBrilliantMediator_RegistersHandlerRegistry()
    {
        var services = new ServiceCollection();
        services.AddBrilliantMediator().Build();
        var serviceProvider = services.BuildServiceProvider();

        var registry = serviceProvider.GetService<IHandlerRegistry>();
        Assert.NotNull(registry);
        Assert.IsType<Mediator>(registry);

        // Same instance as IMediator
        var mediator = serviceProvider.GetService<IMediator>();
        Assert.Same(registry, mediator);
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
        var services = new ServiceCollection();
        var result = services
            .AddBrilliantMediator()
            .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
            .Build();

        var serviceProvider = result.BuildServiceProvider();
        var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
        Assert.NotNull(handler);
        Assert.IsType<BuilderTestCommandHandler>(handler);
    }

    [Fact]
    public void AddCommandHandler_WithResponse_RegistersHandlerInDI()
    {
        var services = new ServiceCollection();
        var result = services
            .AddBrilliantMediator()
            .AddCommandHandler<BuilderTestCommandWithResponse, BuilderTestResult, BuilderTestCommandWithResponseHandler>()
            .Build();

        var serviceProvider = result.BuildServiceProvider();
        var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommandWithResponse, BuilderTestResult>>();
        Assert.NotNull(handler);
        Assert.IsType<BuilderTestCommandWithResponseHandler>(handler);
    }

    [Fact]
    public void AddQueryHandler_RegistersHandlerInDI()
    {
        var services = new ServiceCollection();
        var result = services
            .AddBrilliantMediator()
            .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
            .Build();

        var serviceProvider = result.BuildServiceProvider();
        var handler = serviceProvider.GetService<IQueryHandler<BuilderTestQuery, BuilderTestQueryResult>>();
        Assert.NotNull(handler);
        Assert.IsType<BuilderTestQueryHandler>(handler);
    }

    [Fact]
    public async Task AddCommandHandlerInstance_RegistersInstance()
    {
        var services = new ServiceCollection();
        services
            .AddBrilliantMediator()
            .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();

        var mediator = serviceProvider.GetRequiredService<IMediator>();
        await mediator.DispatchAsync(new BuilderTestCommand { Message = "test" });

        var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task AddQueryHandlerInstance_RegistersInstance()
    {
        var services = new ServiceCollection();
        services
            .AddBrilliantMediator()
            .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
            .Build();

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseBrilliantMediator();

        var mediator = serviceProvider.GetRequiredService<IMediator>();
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
            var services = new ServiceCollection();
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
            var services = new ServiceCollection();
            var builder = services.AddBrilliantMediator();
            var chainedBuilder = builder.AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>();

            Assert.NotNull(chainedBuilder);
            Assert.Same(builder, chainedBuilder);
        }

        [Fact]
        public void Build_ReturnsServiceCollection()
        {
            var services = new ServiceCollection();
            var result = services.AddBrilliantMediator().Build();

            Assert.NotNull(result);
            Assert.Same(result, services);
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
            var services = new ServiceCollection();
            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>(ServiceLifetime.Singleton)
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            var handler1 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            var handler2 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

            Assert.NotNull(handler1);
            Assert.NotNull(handler2);
            Assert.Same(handler1, handler2);
        }

        [Fact]
        public void ServiceLifetime_Transient_ReturnsDifferentInstance()
        {
            var services = new ServiceCollection();
            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>(ServiceLifetime.Transient)
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            var handler1 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            var handler2 = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

            Assert.NotNull(handler1);
            Assert.NotNull(handler2);
            Assert.NotSame(handler1, handler2);
        }

        [Fact]
        public void ServiceLifetime_Scoped_ReturnsSameInstanceWithinScope()
        {
            var services = new ServiceCollection();
            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>(ServiceLifetime.Scoped)
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var handler1 = scope.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
                var handler2 = scope.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

                Assert.NotNull(handler1);
                Assert.NotNull(handler2);
                Assert.Same(handler1, handler2);
            }

            using (var scope1 = serviceProvider.CreateScope())
            using (var scope2 = serviceProvider.CreateScope())
            {
                var handler1 = scope1.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
                var handler2 = scope2.ServiceProvider.GetService<ICommandHandler<BuilderTestCommand>>();

                Assert.NotNull(handler1);
                Assert.NotNull(handler2);
                Assert.NotSame(handler1, handler2);
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
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            Assert.NotNull(handler);
            Assert.IsType<HandlerWithDependency>(handler);
        }

        [Fact]
        public void HandlerWithDependencies_DependenciesAreInjected()
        {
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>() as HandlerWithDependency;

            Assert.NotNull(handler);
            var task = handler.Handle(new BuilderTestCommand());
            task.Wait();
        }

        [Fact]
        public void HandlerWithMissingDependencies_ThrowsOnResolution()
        {
            var services = new ServiceCollection();

            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();

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
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
                .AddCommandHandler<BuilderTestCommandWithResponse, BuilderTestResult, BuilderTestCommandWithResponseHandler>()
                .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.UseBrilliantMediator();

            var mediator = serviceProvider.GetRequiredService<IMediator>();

            await mediator.DispatchAsync(new BuilderTestCommand { Message = "test message" });

            var commandResult = await mediator.DispatchAsync<BuilderTestCommandWithResponse, BuilderTestResult>(
                new BuilderTestCommandWithResponse { Value = 5 });

            Assert.True(commandResult.Success);
            Assert.Equal(15, commandResult.ProcessedValue);

            var queryResult = await mediator.SendAsync<BuilderTestQuery, BuilderTestQueryResult>(
                new BuilderTestQuery { Filter = "test filter" });

            Assert.Equal("Filtered by: test filter", queryResult.Data);
            Assert.Equal(11, queryResult.Count);
        }

        [Fact]
        public async Task HandlerWithDependencies_Integration_WorksCorrectly()
        {
            var services = new ServiceCollection();
            services.AddScoped<ITestService, TestService>();

            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, HandlerWithDependency>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.UseBrilliantMediator();

            var mediator = serviceProvider.GetRequiredService<IMediator>();
            await mediator.DispatchAsync(new BuilderTestCommand { Message = "dependency test" });

            var handler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            Assert.NotNull(handler);
            Assert.IsType<HandlerWithDependency>(handler);

            await handler.Handle(new BuilderTestCommand { Message = "test" });
            var handlerWithDep = (HandlerWithDependency)handler;
            Assert.True(handlerWithDep.WasCalled);
            Assert.Equal("Service Data", handlerWithDep.ServiceData);
        }

        [Fact]
        public void MediatorInitialization_OnlyHappensOnce()
        {
            var services = new ServiceCollection();
            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            var mediator1 = serviceProvider.GetRequiredService<IMediator>();
            var mediator2 = serviceProvider.GetRequiredService<IMediator>();

            Assert.Same(mediator1, mediator2);
        }

        [Fact]
        public void MultipleHandlers_AllRegisteredCorrectly()
        {
            var services = new ServiceCollection();

            services
                .AddBrilliantMediator()
                .AddCommandHandler<BuilderTestCommand, BuilderTestCommandHandler>()
                .AddCommandHandler<BuilderTestCommandWithResponse, BuilderTestResult, BuilderTestCommandWithResponseHandler>()
                .AddQueryHandler<BuilderTestQuery, BuilderTestQueryResult, BuilderTestQueryHandler>()
                .Build();

            var serviceProvider = services.BuildServiceProvider();

            var commandHandler = serviceProvider.GetService<ICommandHandler<BuilderTestCommand>>();
            var commandWithResponseHandler = serviceProvider.GetService<ICommandHandler<BuilderTestCommandWithResponse, BuilderTestResult>>();
            var queryHandler = serviceProvider.GetService<IQueryHandler<BuilderTestQuery, BuilderTestQueryResult>>();

            Assert.NotNull(commandHandler);
            Assert.NotNull(commandWithResponseHandler);
            Assert.NotNull(queryHandler);
        }
    }
}
