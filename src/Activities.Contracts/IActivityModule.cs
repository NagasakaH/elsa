namespace NagasakaEventSystem.Activities.Contracts;

/// <summary>
/// Optional entry point implemented by custom activity assemblies.
/// The loader will invoke this to allow the assembly to register activities into Elsa.
/// </summary>
public interface IActivityModule
{
    /// <summary>
    /// Register exported activity types into Elsa.
    /// </summary>
    ValueTask RegisterAsync(ActivityModuleContext context, CancellationToken cancellationToken);
}
