using System.Text.Json;

namespace McduDcsBiosBridge
{
    public static class ConfigManager
    {
        private static readonly string ConfigFile =
            Path.Combine(AppContext.BaseDirectory, "config.json");


        public static DcsBiosConfig Load()
        {
            if (!File.Exists(ConfigFile))
            {
                var defaultConfig = new DcsBiosConfig();
                Save(defaultConfig);
                return defaultConfig;
            }

            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<DcsBiosConfig>(json) ?? new DcsBiosConfig();
        }

        public static void Save(DcsBiosConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
    }
}
