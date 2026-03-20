using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Monbsoft.BrilliantMediator.SourceGenerator;

[Generator]
public sealed class HandlerRegistrationGenerator : IIncrementalGenerator
{
    private const string CommandHandlerNoResponseMetadataName =
        "Monbsoft.BrilliantMediator.Abstractions.Commands.ICommandHandler`1";
    private const string CommandHandlerWithResponseMetadataName =
        "Monbsoft.BrilliantMediator.Abstractions.Commands.ICommandHandler`2";
    private const string QueryHandlerMetadataName =
        "Monbsoft.BrilliantMediator.Abstractions.Handlers.IQueryHandler`2";
    private const string EventHandlerMetadataName =
        "Monbsoft.BrilliantMediator.Abstractions.Events.IEventHandler`1";
    private const string GeneratorAttributeMetadataName =
        "Monbsoft.BrilliantMediator.BrilliantMediatorGeneratorAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(
            context.CompilationProvider,
            (ctx, compilation) => Execute(ctx, compilation));
    }

    private static void Execute(SourceProductionContext ctx, Compilation compilation)
    {
        var (handlers, targetNamespace) = CollectHandlers(compilation, ctx);
        var source = GenerateSource(targetNamespace, handlers);
        var assemblyName = compilation.AssemblyName ?? "Generated";
        ctx.AddSource(
            $"{assemblyName}.Infrastructure.Generated.g.cs",
            SourceText.From(source, Encoding.UTF8));
    }

    private static (List<HandlerInfo> handlers, string targetNamespace) CollectHandlers(
        Compilation compilation, SourceProductionContext ctx)
    {
        var handlers = new List<HandlerInfo>();

        var commandHandlerT = compilation.GetTypeByMetadataName(CommandHandlerNoResponseMetadataName);
        var commandHandlerTR = compilation.GetTypeByMetadataName(CommandHandlerWithResponseMetadataName);
        var queryHandlerTR = compilation.GetTypeByMetadataName(QueryHandlerMetadataName);
        var eventHandlerT = compilation.GetTypeByMetadataName(EventHandlerMetadataName);

        if (commandHandlerT is null && commandHandlerTR is null && queryHandlerTR is null && eventHandlerT is null)
            return (handlers, compilation.AssemblyName ?? "Generated");

        // Read [assembly: BrilliantMediatorGenerator(...)]
        string targetNamespace = compilation.AssemblyName ?? "Generated";
        var assembliesToScan = new HashSet<IAssemblySymbol>(SymbolEqualityComparer.Default)
        {
            compilation.Assembly
        };

        var generatorAttrSymbol = compilation.GetTypeByMetadataName(GeneratorAttributeMetadataName);
        if (generatorAttrSymbol is not null)
        {
            foreach (var attr in compilation.Assembly.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatorAttrSymbol))
                    continue;

                // Read Namespace property
                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "Namespace"
                        && namedArg.Value.Value is string ns
                        && !string.IsNullOrWhiteSpace(ns))
                    {
                        targetNamespace = ns;
                    }

                    // Read Assemblies = [typeof(T1), typeof(T2)]
                    if (namedArg.Key == "Assemblies"
                        && namedArg.Value.Kind == TypedConstantKind.Array)
                    {
                        foreach (var element in namedArg.Value.Values)
                        {
                            if (element.Value is INamedTypeSymbol markerType)
                                assembliesToScan.Add(markerType.ContainingAssembly);
                            else
                                ctx.ReportDiagnostic(Diagnostic.Create(
                                    Diagnostics.InvalidAssemblyMarker, Location.None));
                        }
                    }
                }
            }
        }

        foreach (var assembly in assembliesToScan)
        {
            ScanNamespace(
                assembly.GlobalNamespace,
                handlers,
                commandHandlerT,
                commandHandlerTR,
                queryHandlerTR,
                eventHandlerT);
        }

        return (handlers, targetNamespace);
    }

    private static void ScanNamespace(
        INamespaceSymbol ns,
        List<HandlerInfo> handlers,
        INamedTypeSymbol? commandHandlerT,
        INamedTypeSymbol? commandHandlerTR,
        INamedTypeSymbol? queryHandlerTR,
        INamedTypeSymbol? eventHandlerT)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (type.IsAbstract || type.TypeKind == TypeKind.Interface || type.IsStatic)
                continue;

            foreach (var iface in type.AllInterfaces)
            {
                if (!iface.IsGenericType) continue;

                var def = iface.OriginalDefinition;

                if (commandHandlerT is not null
                    && SymbolEqualityComparer.Default.Equals(def, commandHandlerT))
                {
                    handlers.Add(new HandlerInfo(HandlerKind.CommandNoResponse, type, iface.TypeArguments));
                }
                else if (commandHandlerTR is not null
                    && SymbolEqualityComparer.Default.Equals(def, commandHandlerTR))
                {
                    handlers.Add(new HandlerInfo(HandlerKind.CommandWithResponse, type, iface.TypeArguments));
                }
                else if (queryHandlerTR is not null
                    && SymbolEqualityComparer.Default.Equals(def, queryHandlerTR))
                {
                    handlers.Add(new HandlerInfo(HandlerKind.Query, type, iface.TypeArguments));
                }
                else if (eventHandlerT is not null
                    && SymbolEqualityComparer.Default.Equals(def, eventHandlerT))
                {
                    handlers.Add(new HandlerInfo(HandlerKind.Event, type, iface.TypeArguments));
                }
            }
        }

        foreach (var nestedNs in ns.GetNamespaceMembers())
        {
            ScanNamespace(nestedNs, handlers, commandHandlerT, commandHandlerTR, queryHandlerTR, eventHandlerT);
        }
    }

    private static string GenerateSource(string targetNamespace, List<HandlerInfo> handlers)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// Generated by BrilliantMediator.SourceGenerator — do not edit.");
        sb.AppendLine();
        sb.AppendLine("using Monbsoft.BrilliantMediator.Extensions;");
        sb.AppendLine();
        sb.AppendLine($"namespace {targetNamespace};");
        sb.AppendLine();
        sb.AppendLine("public static partial class MediatorHandlersRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    public static MediatorBuilder AddGeneratedHandlers(this MediatorBuilder builder)");
        sb.AppendLine("    {");

        foreach (var handler in handlers)
        {
            var line = handler.Kind switch
            {
                HandlerKind.CommandNoResponse =>
                    $"        builder.AddCommandHandler<{handler.TypeArgs[0].ToDisplayString()}, {handler.HandlerType.ToDisplayString()}>();",
                HandlerKind.CommandWithResponse =>
                    $"        builder.AddCommandHandler<{handler.TypeArgs[0].ToDisplayString()}, {handler.TypeArgs[1].ToDisplayString()}, {handler.HandlerType.ToDisplayString()}>();",
                HandlerKind.Query =>
                    $"        builder.AddQueryHandler<{handler.TypeArgs[0].ToDisplayString()}, {handler.TypeArgs[1].ToDisplayString()}, {handler.HandlerType.ToDisplayString()}>();",
                HandlerKind.Event =>
                    $"        builder.AddEventHandler<{handler.TypeArgs[0].ToDisplayString()}, {handler.HandlerType.ToDisplayString()}>();",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(line))
                sb.AppendLine(line);
        }

        sb.AppendLine("        return builder;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}
