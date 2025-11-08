using Monbsoft.BrilliantMediator.Core;

namespace Monbsoft.BrilliantMediator;

/// <summary>
/// Configuration options for BrilliantMediator.
/// </summary>
public sealed class MediatorOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to throw an exception
    /// when a handler is not registered.
    /// Default: true
    /// </summary>
    public bool ThrowOnUnregisteredHandler { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable diagnostics/logging.
    /// Default: false
    /// </summary>
    public bool EnableDiagnostics { get; set; } = false;

    /// <summary>
    /// Gets or sets the callback for diagnostic events.
    /// Called when ThrowOnUnregisteredHandler is true and a handler is not found.
    /// </summary>
    public Action<MediatorDiagnosticEvent>? OnDiagnosticEvent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow handler replacement.
    /// If false, registering a handler twice will throw an exception.
    /// Default: true
    /// </summary>
    public bool AllowHandlerReplacement { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of command/query types to cache.
    /// Used for diagnostic purposes only.
    /// Default: 1000
    /// </summary>
    public int MaxCachedTypes { get; set; } = 1000;

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if options are invalid.</exception>
    public void Validate()
    {
        if (MaxCachedTypes <= 0)
            throw new ArgumentException("MaxCachedTypes must be greater than 0", nameof(MaxCachedTypes));
    }
}