using System.Text.Json;
using CloudRestaurent.Application.Common.Modules;

namespace CloudRestaurent.Infrastructure.Modules;

/// <summary>
/// Reads <c>modules_statuses.json</c> at the content root and filters the supplied
/// installers to only those marked <c>true</c>. Mirrors UltimatePOS's
/// <c>FileActivator</c> from <c>nwidart/laravel-modules</c>.
/// </summary>
public sealed class FileModuleRegistry : IModuleRegistry
{
    private readonly Dictionary<string, IModuleInstaller> _enabled;

    public FileModuleRegistry(IEnumerable<IModuleInstaller> allInstallers, string statusFilePath)
    {
        var statuses = LoadStatuses(statusFilePath);
        _enabled = allInstallers
            .Where(i => statuses.GetValueOrDefault(i.Manifest.Id, true))
            .ToDictionary(i => i.Manifest.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IModuleInstaller> EnabledModules => _enabled.Values.ToList();

    public bool IsEnabled(string moduleId) => _enabled.ContainsKey(moduleId);

    private static Dictionary<string, bool> LoadStatuses(string path)
    {
        if (!File.Exists(path))
            return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, bool>>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
    }
}
