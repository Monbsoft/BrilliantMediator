using EcommerceDDD.Application;
using Monbsoft.BrilliantMediator;

// Instruit BrilliantMediator.SourceGenerator de scanner EcommerceDDD.Application pour les handlers.
[assembly: BrilliantMediatorGenerator(
    Namespace = "EcommerceDDD.Web.Infrastructure.Generated",
    Assemblies = [typeof(IAssemblyMarker)])]
