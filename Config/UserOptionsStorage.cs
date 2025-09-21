using System;
using System.IO;
using System.Text.Json;

namespace WWCduDcsBiosBridge.Config;

public static class UserOptionsStorage
{
    private static string ConfigFilePath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "useroptions.json");

    public static UserOptions GetDefaultOptions() => new();

    public static UserOptions Load()
    {
        if (!File.Exists(ConfigFilePath))
        {
            var defaultOptions = GetDefaultOptions();
            Save(defaultOptions);
            return defaultOptions;
        }

        var json = File.ReadAllText(ConfigFilePath);
        return JsonSerializer.Deserialize<UserOptions>(json) ?? GetDefaultOptions();
    }

    public static void Save(UserOptions? options)
    {
        if (options is null) return;
        
        var dir = Path.GetDirectoryName(ConfigFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(options, jsonOptions);
        File.WriteAllText(ConfigFilePath, json);
    }
}