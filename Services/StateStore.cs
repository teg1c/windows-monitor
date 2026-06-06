using System.Text.Json;

namespace MhxyNotify.Services;

public sealed class StateStore
{
    public string LastHash { get; set; } = "";
    public string LastOcrText { get; set; } = "";
    public DateTimeOffset LastChangedAt { get; set; }

    public static string DefaultPath => AppPaths.StatePath;

    public static StateStore Load()
    {
        AppPaths.CopyLegacyFileIfNeeded(AppPaths.LegacyStatePath, DefaultPath);
        if (!File.Exists(DefaultPath))
        {
            return new StateStore();
        }

        var json = File.ReadAllText(DefaultPath);
        return JsonSerializer.Deserialize<StateStore>(json) ?? new StateStore();
    }

    public void Save()
    {
        AppPaths.EnsureDataDirectory();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(DefaultPath, json);
    }
}
