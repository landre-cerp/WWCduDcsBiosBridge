using System;
using System.IO;
using System.Text.Json;

namespace WWCduDcsBiosBridge
{
    public static class UserOptionsStorage
    {
        public static string ConfigFilePath => Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "useroptions.json");

        public static UserOptions GetDefaultOptions()
        {
            return new UserOptions
            {
                DisplayBottomAligned = false,
                DisplayCMS = false,
                LinkedScreenBrightness = false,
                DisableLightingManagement = false
            };
        }

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
            if (options == null) return;
            var dir = Path.GetDirectoryName(ConfigFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}