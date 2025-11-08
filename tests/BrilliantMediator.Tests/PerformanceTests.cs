using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
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

// High-performance handlers (minimal overhead)
public class FastCommandHandler : ICommandHandler<PerfTestCommand>
{
    public Task Handle(PerfTestCommand command)
    {
        // Minimal processing to test pure mediator overhead
        return Task.CompletedTask;
    }
}

public class FastCommandWithResponseHandler : ICommandHandler<PerfTestCommandWithResponse, PerfTestResult>
{
    public Task<PerfTestResult> Handle(PerfTestCommandWithResponse command)
    {
        // Minimal processing
        return Task.FromResult(new PerfTestResult { ProcessedValue = command.Value });
    }
}

public class FastQueryHandler : IQueryHandler<PerfTestQuery, PerfTestQueryResult>
{
    public Task<PerfTestQueryResult> Handle(PerfTestQuery query)
    {
        // Minimal processing
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
  
        _mediator.RegisterCommandHandler<PerfTestCommand>(fastCommandHandler);
   _mediator.RegisterCommandHandler<PerfTestCommandWithResponse, PerfTestResult>(fastCommandWithResponseHandler);
      _mediator.RegisterQueryHandler<PerfTestQuery, PerfTestQueryResult>(fastQueryHandler);
    }

    [Fact]
    public async Task Command_SingleExecution_HasMinimalOverhead()
    {
        // Arrange
        var command = new PerfTestCommand { Id = 1, Data = "test" };
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _mediator.DispatchAsync(command);

        // Assert
        stopwatch.Stop();

        // Should complete in less than 1ms for single operation
        Assert.True(stopwatch.ElapsedMilliseconds < 1,
  $"Single command execution took {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public async Task CommandWithResponse_SingleExecution_HasMinimalOverhead()
    {
        // Arrange
        var command = new PerfTestCommandWithResponse { Value = 42 };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _mediator.DispatchAsync<PerfTestCommandWithResponse, PerfTestResult>(command);

        // Assert
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.Equal(42, result.ProcessedValue);

        // Should complete in less than 1ms for single operation
        Assert.True(stopwatch.ElapsedMilliseconds < 1,
    $"Single command with response execution took {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public async Task Query_SingleExecution_HasMinimalOverhead()
    {
        // Arrange
        var query = new PerfTestQuery { QueryId = 100 };
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _mediator.SendAsync<PerfTestQuery, PerfTestQueryResult>(query);

        // Assert
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.Equal(100, result.QueryId);

        // Should complete in less than 1ms for single operation
        Assert.True(stopwatch.ElapsedMilliseconds < 1,
      $"Single query execution took {stopwatch.ElapsedTicks} ticks ({stopwatch.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public async Task Command_HighThroughput_MeetsPerformanceTarget()
    {
        // Arrange
        const int iterations = 100000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            await _mediator.DispatchAsync(new PerfTestCommand { Id = i });
        }

        // Assert
        stopwatch.Stop();

        var operationsPerSecond = iterations / (stopwatch.ElapsedMilliseconds / 1000.0);

        // Should handle at least 50,000 operations per second
        Assert.True(operationsPerSecond > 50000,
    $"Expected > 50,000 ops/sec, got {operationsPerSecond:F0} ops/sec in {stopwatch.ElapsedMilliseconds}ms");

        // Should complete 100k operations in less than 5 seconds
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
            $"100k operations took {stopwatch.ElapsedMilliseconds}ms (expected < 5000ms)");
    }

    [Fact]
    public async Task CommandWithResponse_HighThroughput_MeetsPerformanceTarget()
    {
        // Arrange
        const int iterations = 50000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var result = await _mediator.DispatchAsync<PerfTestCommandWithResponse, PerfTestResult>(
                  new PerfTestCommandWithResponse { Value = i % 1000 });

            // Verify result to ensure no shortcuts
            Assert.NotNull(result);
        }

        // Assert
        stopwatch.Stop();

        var operationsPerSecond = iterations / (stopwatch.ElapsedMilliseconds / 1000.0);

        // Should handle at least 25,000 operations per second (allowing for response overhead)
        Assert.True(operationsPerSecond > 25000,
    $"Expected > 25,000 ops/sec, got {operationsPerSecond:F0} ops/sec in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task Query_HighThroughput_MeetsPerformanceTarget()
    {
        // Arrange
        const int iterations = 50000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var result = await _mediator.SendAsync<PerfTestQuery, PerfTestQueryResult>(
    new PerfTestQuery { QueryId = i });

            // Verify result to ensure no shortcuts
            Assert.NotNull(result);
            Assert.Equal(i, result.QueryId);
        }

        // Assert
        stopwatch.Stop();

        var operationsPerSecond = iterations / (stopwatch.ElapsedMilliseconds / 1000.0);

        // Should handle at least 25,000 operations per second
        Assert.True(operationsPerSecond > 25000,
       $"Expected > 25,000 ops/sec, got {operationsPerSecond:F0} ops/sec in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task MixedOperations_ConcurrentExecution_PerformsWell()
    {
        // Arrange
        const int concurrentTasks = 1000;
        var tasks = new List<Task>(concurrentTasks * 3);
        var stopwatch = Stopwatch.StartNew();

        // Act - Mix of all operation types concurrently
        for (int i = 0; i < concurrentTasks; i++)
        {
            var index = i; // Capture for closure

            tasks.Add(_mediator.DispatchAsync(new PerfTestCommand { Id = index }));

            tasks.Add(_mediator.DispatchAsync<PerfTestCommandWithResponse, PerfTestResult>(
        new PerfTestCommandWithResponse { Value = index }));

            tasks.Add(_mediator.SendAsync<PerfTestQuery, PerfTestQueryResult>(
            new PerfTestQuery { QueryId = index }));
        }

        await Task.WhenAll(tasks);

        // Assert
        stopwatch.Stop();

        var totalOperations = concurrentTasks * 3;
        var operationsPerSecond = totalOperations / (stopwatch.ElapsedMilliseconds / 1000.0);

        // Should handle concurrent operations efficiently
        Assert.True(operationsPerSecond > 10000,
               $"Expected > 10,000 ops/sec for concurrent operations, got {operationsPerSecond:F0} ops/sec");

        // Should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 5000,
          $"Concurrent operations took {stopwatch.ElapsedMilliseconds}ms (expected < 5000ms)");
    }

    [Fact]
    public void HandlerRegistration_BulkRegistration_PerformsQuickly()
    {
        // Arrange
    const int registrations = 1000;
        var stopwatch = Stopwatch.StartNew();

// Act
    for (int i = 0; i < registrations; i++)
        {
  var (mediator, serviceProvider) = TestMediatorFactory.Create();
var handler1 = new FastCommandHandler();
      var handler2 = new FastCommandWithResponseHandler();
      var handler3 = new FastQueryHandler();
        
            serviceProvider.AddCommandHandler(handler1);
       serviceProvider.AddCommandHandler<PerfTestCommandWithResponse, PerfTestResult>(handler2);
       serviceProvider.AddQueryHandler<PerfTestQuery, PerfTestQueryResult>(handler3);
    
    mediator.RegisterCommandHandler<PerfTestCommand>(handler1);
        mediator.RegisterCommandHandler<PerfTestCommandWithResponse, PerfTestResult>(handler2);
  mediator.RegisterQueryHandler<PerfTestQuery, PerfTestQueryResult>(handler3);
  }

        // Assert
        stopwatch.Stop();

    // Handler registration should be very fast (O(1))
    Assert.True(stopwatch.ElapsedMilliseconds < 100,
     $"1000 handler registrations took {stopwatch.ElapsedMilliseconds}ms (expected < 100ms)");
    }

    [Fact]
    public async Task MemoryAllocation_MinimalGarbageCollection()
    {
        // Arrange
        const int iterations = 10000;
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        for (int i = 0; i < iterations; i++)
        {
            await _mediator.DispatchAsync(new PerfTestCommand { Id = i % 100 });

            // Occasionally check memory to detect leaks
            if (i % 1000 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                var memoryIncrease = currentMemory - initialMemory;

                // Memory increase should be reasonable (less than 10MB for 10k operations)
                Assert.True(memoryIncrease < 10 * 1024 * 1024,
                   $"Memory increased by {memoryIncrease / 1024.0 / 1024.0:F2}MB after {i + 1} operations");
            }
        }

        // Final memory check
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var totalIncrease = finalMemory - initialMemory;

        // Total memory increase should be minimal
        Assert.True(totalIncrease < 5 * 1024 * 1024,
      $"Total memory increased by {totalIncrease / 1024.0 / 1024.0:F2}MB after {iterations} operations");
    }

    [Fact]
    public async Task LongRunningTest_SustainedPerformance()
    {
        // Arrange
        const int duration = 2000; // 2 seconds
        var endTime = DateTime.UtcNow.AddMilliseconds(duration);
        var operationCount = 0;
        var stopwatch = Stopwatch.StartNew();

        // Act - Run operations continuously for specified duration
        while (DateTime.UtcNow < endTime)
        {
            await _mediator.DispatchAsync(new PerfTestCommand { Id = operationCount });
            operationCount++;
        }

        // Assert
        stopwatch.Stop();

        var actualDuration = stopwatch.ElapsedMilliseconds;
        var operationsPerSecond = operationCount / (actualDuration / 1000.0);

        // Should maintain consistent performance over time
        Assert.True(operationsPerSecond > 10000,
  $"Sustained performance: {operationsPerSecond:F0} ops/sec over {actualDuration}ms ({operationCount} total operations)");

        // Should have processed a reasonable number of operations
        Assert.True(operationCount > 10000,
      $"Only processed {operationCount} operations in {actualDuration}ms");
    }
}