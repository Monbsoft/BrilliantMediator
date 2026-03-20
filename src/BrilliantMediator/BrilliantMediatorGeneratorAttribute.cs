namespace Monbsoft.BrilliantMediator;

/// <summary>
/// Configures BrilliantMediator.SourceGenerator for the target assembly.
/// Place this attribute at the assembly level in the project where code is generated (entry point).
/// </summary>
/// <example>
/// [assembly: BrilliantMediatorGenerator(
///     Namespace = "MyApp.Web.Infrastructure.Generated",
///     Assemblies = [typeof(MyCommandHandler), typeof(MyQueryHandler)])]
/// </example>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class BrilliantMediatorGeneratorAttribute : Attribute
{
    /// <summary>
    /// Namespace for the generated class.
    /// Defaults to the assembly name if not specified.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Marker types identifying the assemblies to scan for handler implementations.
    /// The generator uses each type to locate its containing assembly.
    /// The current assembly is always scanned.
    /// </summary>
    public Type[]? Assemblies { get; set; }
}
