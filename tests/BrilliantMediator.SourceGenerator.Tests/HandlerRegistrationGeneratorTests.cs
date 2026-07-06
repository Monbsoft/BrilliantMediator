using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Monbsoft.BrilliantMediator.SourceGenerator;

namespace Monbsoft.BrilliantMediator.SourceGenerator.Tests;

public class HandlerRegistrationGeneratorTests
{
    private static Compilation CreateCompilation(string source)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => a.Location)
            // Add BrilliantMediator reference
            .Append(typeof(global::Monbsoft.BrilliantMediator.BrilliantMediatorGeneratorAttribute).Assembly.Location)
            // Add Microsoft.Extensions.DependencyInjection.Abstractions so that
            // generated code referencing MediatorBuilder can compile
            .Append(typeof(Microsoft.Extensions.DependencyInjection.IServiceCollection).Assembly.Location)
            .Distinct()
            .Select(location => MetadataReference.CreateFromFile(location))
            .Cast<MetadataReference>()
            .ToList();

        return CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static GeneratorRunResult RunGenerator(Compilation compilation)
    {
        var generator = new HandlerRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        var result = driver.RunGenerators(compilation).GetRunResult();
        return result.Results[0];
    }

    [Fact]
    public void Generator_NoHandlers_GeneratesEmptyAddGeneratedHandlers()
    {
        var source = """
            namespace TestAssembly;
            public class NotAHandler { }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single();
        Assert.Contains("AddGeneratedHandlers", generated.SourceText.ToString());
        Assert.Contains("return builder;", generated.SourceText.ToString());
    }

    [Fact]
    public void Generator_CommandHandlerNoResponse_GeneratesCorrectRegistration()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using System.Threading;

            namespace TestAssembly;

            public class MyCommand : ICommand { }

            public class MyCommandHandler : ICommandHandler<MyCommand>
            {
                public System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddCommandHandler<TestAssembly.MyCommand, TestAssembly.MyCommandHandler>()", generated);
    }

    [Fact]
    public void Generator_CommandHandlerWithResponse_GeneratesCorrectRegistration()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using System.Threading;

            namespace TestAssembly;

            public class MyResult { }
            public class MyCommand : ICommand<MyResult> { }

            public class MyCommandHandler : ICommandHandler<MyCommand, MyResult>
            {
                public System.Threading.Tasks.Task<MyResult> Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.FromResult(new MyResult());
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddCommandHandler<TestAssembly.MyCommand, TestAssembly.MyResult, TestAssembly.MyCommandHandler>()", generated);
    }

    [Fact]
    public void Generator_AbstractHandler_IsIgnored()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using System.Threading;

            namespace TestAssembly;

            public class MyCommand : ICommand { }

            public abstract class AbstractHandler : ICommandHandler<MyCommand>
            {
                public abstract System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default);
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.DoesNotContain("AbstractHandler", generated);
    }

    [Fact]
    public void Generator_GeneratedFile_HasCorrectNamingConvention()
    {
        var source = "namespace TestAssembly; public class Dummy { }";
        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        var generatedFile = result.GeneratedSources.Single();
        Assert.Contains("Infrastructure.Generated.g.cs", generatedFile.HintName);
    }

    [Fact]
    public void Generator_QueryHandler_GeneratesCorrectRegistration()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Queries;
            using System.Threading;

            namespace TestAssembly;

            public class MyResult { }
            public class MyQuery : IQuery<MyResult> { }

            public class MyQueryHandler : IQueryHandler<MyQuery, MyResult>
            {
                public System.Threading.Tasks.Task<MyResult> Handle(MyQuery query, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.FromResult(new MyResult());
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddQueryHandler<TestAssembly.MyQuery, TestAssembly.MyResult, TestAssembly.MyQueryHandler>()", generated);
    }

    [Fact]
    public void Generator_EventHandler_GeneratesCorrectRegistration()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Events;
            using System.Threading;

            namespace TestAssembly;

            public class MyEvent : IEvent { }

            public class MyEventHandler : IEventHandler<MyEvent>
            {
                public System.Threading.Tasks.Task Handle(MyEvent @event, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddEventHandler<TestAssembly.MyEvent, TestAssembly.MyEventHandler>()", generated);
    }

    [Fact]
    public void Generator_MultipleHandlers_GeneratesAllRegistrations()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using Monbsoft.BrilliantMediator.Abstractions.Events;
            using Monbsoft.BrilliantMediator.Abstractions.Queries;
            using System.Threading;

            namespace TestAssembly;

            public class MyCommand : ICommand { }
            public class MyResult { }
            public class MyQuery : IQuery<MyResult> { }
            public class MyEvent : IEvent { }

            public class MyCommandHandler : ICommandHandler<MyCommand>
            {
                public System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }

            public class MyQueryHandler : IQueryHandler<MyQuery, MyResult>
            {
                public System.Threading.Tasks.Task<MyResult> Handle(MyQuery query, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.FromResult(new MyResult());
            }

            public class MyEventHandler : IEventHandler<MyEvent>
            {
                public System.Threading.Tasks.Task Handle(MyEvent @event, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddCommandHandler<TestAssembly.MyCommand, TestAssembly.MyCommandHandler>()", generated);
        Assert.Contains("AddQueryHandler<TestAssembly.MyQuery, TestAssembly.MyResult, TestAssembly.MyQueryHandler>()", generated);
        Assert.Contains("AddEventHandler<TestAssembly.MyEvent, TestAssembly.MyEventHandler>()", generated);
    }

    [Fact]
    public void Generator_HandlerImplementingMultipleInterfaces_GeneratesAllRegistrations()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using Monbsoft.BrilliantMediator.Abstractions.Events;
            using System.Threading;

            namespace TestAssembly;

            public class MyCommand : ICommand { }
            public class MyEvent : IEvent { }

            public class CompositeHandler : ICommandHandler<MyCommand>, IEventHandler<MyEvent>
            {
                public System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;

                public System.Threading.Tasks.Task Handle(MyEvent @event, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddCommandHandler<TestAssembly.MyCommand, TestAssembly.CompositeHandler>()", generated);
        Assert.Contains("AddEventHandler<TestAssembly.MyEvent, TestAssembly.CompositeHandler>()", generated);
    }

    [Fact]
    public void Generator_CustomNamespace_UsesNamespaceFromAttribute()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using System.Threading;

            [assembly: Monbsoft.BrilliantMediator.BrilliantMediatorGenerator(Namespace = "Custom.Generated")]

            namespace TestAssembly;

            public class MyCommand : ICommand { }

            public class MyCommandHandler : ICommandHandler<MyCommand>
            {
                public System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("namespace Custom.Generated;", generated);
    }

    [Fact]
    public void Generator_HandlerInNestedNamespace_IsFound()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using System.Threading;

            namespace TestAssembly.Deep.Nested;

            public class MyCommand : ICommand { }

            public class MyCommandHandler : ICommandHandler<MyCommand>
            {
                public System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddCommandHandler<TestAssembly.Deep.Nested.MyCommand, TestAssembly.Deep.Nested.MyCommandHandler>()", generated);
    }

    [Fact]
    public void Generator_StaticOrInterfaceTypes_AreIgnored()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using System.Threading;

            namespace TestAssembly;

            public class MyCommand : ICommand { }

            public interface IDerivedHandler : ICommandHandler<MyCommand> { }

            public static class StaticHelper
            {
                public static System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var result = RunGenerator(compilation);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.DoesNotContain("IDerivedHandler", generated);
        Assert.DoesNotContain("StaticHelper", generated);
    }

    [Fact]
    public void Generator_GeneratedCode_CompilesWithoutErrors()
    {
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;
            using Monbsoft.BrilliantMediator.Abstractions.Events;
            using Monbsoft.BrilliantMediator.Abstractions.Queries;
            using System.Threading;

            namespace TestAssembly;

            public class MyCommand : ICommand { }
            public class MyResult { }
            public class MyQuery : IQuery<MyResult> { }
            public class MyEvent : IEvent { }

            public class MyCommandHandler : ICommandHandler<MyCommand>
            {
                public System.Threading.Tasks.Task Handle(MyCommand command, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }

            public class MyQueryHandler : IQueryHandler<MyQuery, MyResult>
            {
                public System.Threading.Tasks.Task<MyResult> Handle(MyQuery query, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.FromResult(new MyResult());
            }

            public class MyEventHandler : IEventHandler<MyEvent>
            {
                public System.Threading.Tasks.Task Handle(MyEvent @event, CancellationToken cancellationToken = default)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);
        var generator = new HandlerRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var errors = outputCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
        Assert.Empty(errors);
    }
}
