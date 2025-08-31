using System.Net;
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
                throw new ConfigException($"Configuration file not found. A default config has been created at {ConfigFile}. Please review and update it as necessary.");
            }

            var json = File.ReadAllText(ConfigFile);
            var config = JsonSerializer.Deserialize<DcsBiosConfig>(json) ?? new DcsBiosConfig();

            checkIsValid(config);
                                
            return config;
        }

        public static void Save(DcsBiosConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }

        private static void checkIsValid(DcsBiosConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.dcsBiosJsonLocation))
            {
                throw new ConfigException("The 'dcsBiosJsonLocation' field in the configuration file cannot be empty. Please specify a valid path to the DCS-BIOS JSON file.");
            }

            if (!Directory.Exists(config.dcsBiosJsonLocation))
            {
                throw new ConfigException($"The folder specified by 'dcsBiosJsonLocation' does not exist: {config.dcsBiosJsonLocation}");
            }

            if (Directory.GetFiles(config.dcsBiosJsonLocation).Length == 0)
            {
                throw new ConfigException($"The folder specified by 'dcsBiosJsonLocation' is empty: {config.dcsBiosJsonLocation}");
            }

            if (config.ReceivePortUdp < 1 || config.ReceivePortUdp > 65535)
            {
                throw new ConfigException("ReceivePortUdp must be between 1 and 65535.");
            }
            if (config.SendPortUdp < 1 || config.SendPortUdp > 65535)
            {
                throw new ConfigException("SendPortUdp must be between 1 and 65535.");
            }
            if (!IPAddress.TryParse(config.ReceiveFromIpUdp, out var receiveIp))
            {
                throw new ConfigException("ReceiveFromIpUdp is not a valid IP address.");
            }
            if (!IPAddress.TryParse(config.SendToIpUdp, out var sendIp))
            {
                throw new ConfigException("SendToIpUdp is not a valid IP address.");
            }
            if (receiveIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                throw new ConfigException("ReceiveFromIpUdp must be an IPv4 address.");
            }
            if (sendIp.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                throw new ConfigException("SendToIpUdp must be an IPv4 address.");
            }
        }
    }
}
