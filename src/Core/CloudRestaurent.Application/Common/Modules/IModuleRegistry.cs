namespace CloudRestaurent.Application.Common.Modules;

/// <summary>
/// Resolved at startup. Lets the rest of the system enumerate enabled modules.
/// </summary>
public interface IModuleRegistry
{
    IReadOnlyList<IModuleInstaller> EnabledModules { get; }
    bool IsEnabled(string moduleId);
}
