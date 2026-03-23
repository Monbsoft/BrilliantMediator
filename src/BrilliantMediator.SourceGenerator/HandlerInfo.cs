using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Monbsoft.BrilliantMediator.SourceGenerator;

internal sealed class HandlerInfo
{
    public HandlerKind Kind { get; }
    public INamedTypeSymbol HandlerType { get; }
    public ImmutableArray<ITypeSymbol> TypeArgs { get; }

    public HandlerInfo(HandlerKind kind, INamedTypeSymbol handlerType, ImmutableArray<ITypeSymbol> typeArgs)
    {
        Kind = kind;
        HandlerType = handlerType;
        TypeArgs = typeArgs;
    }
}

internal enum HandlerKind
{
    CommandNoResponse,
    CommandWithResponse,
    Query,
    Event
}
