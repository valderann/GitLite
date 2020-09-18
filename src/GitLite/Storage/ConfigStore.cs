using System;
using System.IO;
using System.Text.Json;

namespace GitLite.Storage;

public class ConfigStore
{
    public void Store(Settings data)
    {
        var rootPath = GetRootPath();
        var location = GetSettingsPath();
        if (rootPath == null) return;
        if (!Directory.Exists(rootPath)) Directory.CreateDirectory(rootPath);

        var jsonString = JsonSerializer.Serialize(data);
        File.WriteAllText(location, jsonString);
    }

    public Settings Load()
    {
        var location = GetSettingsPath();
        if (!File.Exists(location)) Store(new Settings());

        return JsonSerializer.Deserialize<Settings>(File.ReadAllText(location));
    }

    public static string GetRootPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GitStat");

    public static string GetSettingsPath()
        => Path.Combine(GetRootPath(), "Settings.json");
}