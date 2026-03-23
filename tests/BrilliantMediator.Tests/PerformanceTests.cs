using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Core;
using System.Diagnostics;

namespace Monbsoft.BrilliantMediator.Tests;

// ============================================================================
// PERFORMANCE TEST FIXTURES
// ============================================================================

public class PerfTestCommand : ICommand
{
    public int Id { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class PerfTestCommandWithResponse : ICommand<PerfTestResult>
{
    public int Value { get; set; }
}

public class PerfTestQuery : IQuery<PerfTestQueryResult>
{
    public int QueryId { get; set; }
}

public class PerfTestResult
{
    public int ProcessedValue { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PerfTestQueryResult
{
    public int QueryId { get; set; }
    public string Result { get; set; } = string.Empty;
}

public class FastCommandHandler : ICommandHandler<PerfTestCommand>
{
    public Task Handle(PerfTestCommand command, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

public class FastCommandWithResponseHandler : ICommandHandler<PerfTestCommandWithResponse, PerfTestResult>
{
    public Task<PerfTestResult> Handle(PerfTestCommandWithResponse command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PerfTestResult { ProcessedValue = command.Value });
    }
}

public class FastQueryHandler : IQueryHandler<PerfTestQuery, PerfTestQueryResult>
{
    public Task<PerfTestQueryResult> Handle(PerfTestQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PerfTestQueryResult
        {
            QueryId = query.QueryId,
            Result = "Fast"
        });
    }
}

// ============================================================================
// PERFORMANCE BENCHMARK TESTS
// ============================================================================

public class PerformanceBenchmarkTests
{
    private readonly Mediator _mediator;
    private readonly TestServiceProvider _serviceProvider;

    public PerformanceBenchmarkTests()
    {
        (_mediator, _serviceProvider) = TestMediatorFactory.Create();

        var fastCommandHandler = new FastCommandHandler();
        var fastCommandWithResponseHandler = new FastCommandWithResponseHandler();
        var fastQueryHandler = new FastQueryHandler();

        _serviceProvider.AddCommandHandler(fastCommandHandler);
        _serviceProvider.AddCommandHandler<PerfTestCommandWithResponse, PerfTestResult>(fastCommandWithResponseHandler);
        _serviceProvider.AddQueryHandler<PerfTestQuery, PerfTestQueryResult>(fastQueryHandler);

        _mediator.RegisterCommandHandler<PerfTestCommand>();
        _mediator.RegisterCommandHandler<PerfTestCommandWithResponse, PerfTestResult>();
        _mediator.RegisterQueryHandler<PerfTestQuery, PerfTestQueryResult>();
    }

    [Fact]
    public async Task Command_SingleExecution_HasMinimalOverhead()
    {
        var command = new PerfTestCommand { Id = 1, Data = "test" };
        var stopwatch = Stopwatch.StartNew();

        await _mediator.DispatchAsync(command);

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 1,
            $"Single command execution took {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public async Task CommandWithResponse_SingleExecution_HasMinimalOverhead()
    {
        var command = new PerfTestCommandWithResponse { Value = 42 };
        var stopwatch = Stopwatch.StartNew();

        var result = await _mediator.DispatchAsync<PerfTestCommandWithResponse, PerfTestResult>(command);

        stopwatch.Stop();
        Assert.NotNull(result);
        Assert.Equal(42, result.ProcessedValue);
        Assert.True(stopwatch.ElapsedMilliseconds < 1,
            $"Single command with response execution took {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public async Task Query_SingleExecution_HasMinimalOverhead()
    {
        var query = new PerfTestQuery { QueryId = 100 };
        var stopwatch = Stopwatch.StartNew();

        var result = await _mediator.SendAsync<PerfTestQuery, PerfTestQueryResult>(query);

        stopwatch.Stop();
        Assert.NotNull(result);
        Assert.Equal(100, result.QueryId);
        Assert.True(stopwatch.ElapsedMilliseconds < 1,
            $"Single query execution took {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public async Task Command_HighThroughput_MeetsPerformanceTarget()
    {
        const int iterations = 100000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            await _mediator.DispatchAsync(new PerfTestCommand { Id = i });
        }

        stopwatch.Stop();
        var operationsPerSecond = iterations / (stopwatch.ElapsedMilliseconds / 1000.0);

        Assert.True(operationsPerSecond > 50000,
            $"Expected > 50,000 ops/sec, got {operationsPerSecond:F0} ops/sec in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"100k operations took {stopwatch.ElapsedMilliseconds}ms (expected < 5000ms)");
    }

    [Fact]
    public async Task CommandWithResponse_HighThroughput_MeetsPerformanceTarget()
    {
        const int iterations = 50000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var result = await _mediator.DispatchAsync<PerfTestCommandWithResponse, PerfTestResult>(
                new PerfTestCommandWithResponse { Value = i % 1000 });
            Assert.NotNull(result);
        }

        stopwatch.Stop();
        var operationsPerSecond = iterations / (stopwatch.ElapsedMilliseconds / 1000.0);

        Assert.True(operationsPerSecond > 25000,
            $"Expected > 25,000 ops/sec, got {operationsPerSecond:F0} ops/sec in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Query_HighThroughput_MeetsPerformanceTarget()
    {
        const int iterations = 50000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var result = await _mediator.SendAsync<PerfTestQuery, PerfTestQueryResult>(
                new PerfTestQuery { QueryId = i });
            Assert.NotNull(result);
            Assert.Equal(i, result.QueryId);
        }

        stopwatch.Stop();
        var operationsPerSecond = iterations / (stopwatch.ElapsedMilliseconds / 1000.0);

        Assert.True(operationsPerSecond > 25000,
            $"Expected > 25,000 ops/sec, got {operationsPerSecond:F0} ops/sec in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task MixedOperations_ConcurrentExecution_PerformsWell()
    {
        const int concurrentTasks = 1000;
        var tasks = new List<Task>(concurrentTasks * 3);
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < concurrentTasks; i++)
        {
            var index = i;
            tasks.Add(_mediator.DispatchAsync(new PerfTestCommand { Id = index }));
            tasks.Add(_mediator.DispatchAsync<PerfTestCommandWithResponse, PerfTestResult>(
                new PerfTestCommandWithResponse { Value = index }));
            tasks.Add(_mediator.SendAsync<PerfTestQuery, PerfTestQueryResult>(
                new PerfTestQuery { QueryId = index }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var totalOperations = concurrentTasks * 3;
        var operationsPerSecond = totalOperations / (stopwatch.ElapsedMilliseconds / 1000.0);

        Assert.True(operationsPerSecond > 10000,
            $"Expected > 10,000 ops/sec for concurrent operations, got {operationsPerSecond:F0} ops/sec");
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"Concurrent operations took {stopwatch.ElapsedMilliseconds}ms (expected < 5000ms)");
    }

    [Fact]
    public void HandlerRegistration_BulkRegistration_PerformsQuickly()
    {
        const int registrations = 1000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < registrations; i++)
        {
            var (mediator, serviceProvider) = TestMediatorFactory.Create();
            var handler1 = new FastCommandHandler();
            var handler2 = new FastCommandWithResponseHandler();
            var handler3 = new FastQueryHandler();

            serviceProvider.AddCommandHandler(handler1);
            serviceProvider.AddCommandHandler<PerfTestCommandWithResponse, PerfTestResult>(handler2);
            serviceProvider.AddQueryHandler<PerfTestQuery, PerfTestQueryResult>(handler3);

            mediator.RegisterCommandHandler<PerfTestCommand>();
            mediator.RegisterCommandHandler<PerfTestCommandWithResponse, PerfTestResult>();
            mediator.RegisterQueryHandler<PerfTestQuery, PerfTestQueryResult>();
        }

        stopwatch.Stop();
        Assert.True(stopwatch.ElapsedMilliseconds < 100,
            $"1000 handler registrations took {stopwatch.ElapsedMilliseconds}ms (expected < 100ms)");
    }

    [Fact]
    public async Task MemoryAllocation_MinimalGarbageCollection()
    {
        const int iterations = 10000;
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            await _mediator.DispatchAsync(new PerfTestCommand { Id = i % 100 });

            if (i % 1000 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                var memoryIncrease = currentMemory - initialMemory;

                Assert.True(memoryIncrease < 10 * 1024 * 1024,
                    $"Memory increased by {memoryIncrease / 1024.0 / 1024.0:F2}MB after {i + 1} operations");
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var totalIncrease = finalMemory - initialMemory;

        Assert.True(totalIncrease < 5 * 1024 * 1024,
            $"Total memory increased by {totalIncrease / 1024.0 / 1024.0:F2}MB after {iterations} operations");
    }

    [Fact]
    public async Task LongRunningTest_SustainedPerformance()
    {
        const int duration = 2000;
        var endTime = DateTime.UtcNow.AddMilliseconds(duration);
        var operationCount = 0;
        var stopwatch = Stopwatch.StartNew();

        while (DateTime.UtcNow < endTime)
        {
            await _mediator.DispatchAsync(new PerfTestCommand { Id = operationCount });
            operationCount++;
        }

        stopwatch.Stop();
        var actualDuration = stopwatch.ElapsedMilliseconds;
        var operationsPerSecond = operationCount / (actualDuration / 1000.0);

        Assert.True(operationsPerSecond > 10000,
            $"Sustained performance: {operationsPerSecond:F0} ops/sec over {actualDuration}ms ({operationCount} total operations)");
        Assert.True(operationCount > 10000,
            $"Only processed {operationCount} operations in {actualDuration}ms");
    }
}
