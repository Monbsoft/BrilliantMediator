using Microsoft.CodeAnalysis;

namespace Monbsoft.BrilliantMediator.SourceGenerator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor InvalidAssemblyMarker = new(
        id: "BM001",
        title: "Invalid assembly marker in BrilliantMediatorGenerator attribute",
        messageFormat: "BrilliantMediatorGenerator.Assemblies requires valid Type arguments",
        category: "BrilliantMediator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
