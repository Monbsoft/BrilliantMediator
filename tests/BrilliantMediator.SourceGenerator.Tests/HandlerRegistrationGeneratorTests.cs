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
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        // Add BrilliantMediator reference
        references.Add(MetadataReference.CreateFromFile(
            typeof(global::Monbsoft.BrilliantMediator.BrilliantMediatorGeneratorAttribute).Assembly.Location));

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
        // Arrange
        var source = """
            namespace TestAssembly;
            public class NotAHandler { }
            """;

        var compilation = CreateCompilation(source);

        // Act
        var result = RunGenerator(compilation);

        // Assert
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single();
        Assert.Contains("AddGeneratedHandlers", generated.SourceText.ToString());
        Assert.Contains("return builder;", generated.SourceText.ToString());
    }

    [Fact]
    public void Generator_CommandHandlerNoResponse_GeneratesCorrectRegistration()
    {
        // Arrange
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;

            namespace TestAssembly;

            public class MyCommand : ICommand { }

            public class MyCommandHandler : ICommandHandler<MyCommand>
            {
                public System.Threading.Tasks.Task Handle(MyCommand command)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """;

        var compilation = CreateCompilation(source);

        // Act
        var result = RunGenerator(compilation);

        // Assert
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddCommandHandler<TestAssembly.MyCommand, TestAssembly.MyCommandHandler>()", generated);
    }

    [Fact]
    public void Generator_CommandHandlerWithResponse_GeneratesCorrectRegistration()
    {
        // Arrange
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;

            namespace TestAssembly;

            public class MyResult { }
            public class MyCommand : ICommand<MyResult> { }

            public class MyCommandHandler : ICommandHandler<MyCommand, MyResult>
            {
                public System.Threading.Tasks.Task<MyResult> Handle(MyCommand command)
                    => System.Threading.Tasks.Task.FromResult(new MyResult());
            }
            """;

        var compilation = CreateCompilation(source);

        // Act
        var result = RunGenerator(compilation);

        // Assert
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.Contains("AddCommandHandler<TestAssembly.MyCommand, TestAssembly.MyResult, TestAssembly.MyCommandHandler>()", generated);
    }

    [Fact]
    public void Generator_AbstractHandler_IsIgnored()
    {
        // Arrange
        var source = """
            using Monbsoft.BrilliantMediator.Abstractions.Commands;

            namespace TestAssembly;

            public class MyCommand : ICommand { }

            public abstract class AbstractHandler : ICommandHandler<MyCommand>
            {
                public abstract System.Threading.Tasks.Task Handle(MyCommand command);
            }
            """;

        var compilation = CreateCompilation(source);

        // Act
        var result = RunGenerator(compilation);

        // Assert
        Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var generated = result.GeneratedSources.Single().SourceText.ToString();
        Assert.DoesNotContain("AbstractHandler", generated);
    }

    [Fact]
    public void Generator_GeneratedFile_HasCorrectNamingConvention()
    {
        // Arrange
        var source = "namespace TestAssembly; public class Dummy { }";
        var compilation = CreateCompilation(source);

        // Act
        var result = RunGenerator(compilation);

        // Assert
        var generatedFile = result.GeneratedSources.Single();
        Assert.Contains("Infrastructure.Generated.g.cs", generatedFile.HintName);
    }
}
