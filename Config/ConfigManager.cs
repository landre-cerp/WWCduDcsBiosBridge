using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace WWCduDcsBiosBridge.Config;

/// <summary>
/// Manages configuration loading, saving, and validation for the DCS-BIOS bridge application.
/// </summary>
public static class ConfigManager
{
    private static readonly string ConfigFile = Path.Combine(AppContext.BaseDirectory, "config.json");
    
    private const int MinPortNumber = 1;
    private const int MaxPortNumber = 65535;

    /// <summary>
    /// Loads the configuration from the config file, creating a default one if it doesn't exist.
    /// </summary>
    /// <returns>The loaded and validated configuration</returns>
    /// <exception cref="ConfigException">Thrown when configuration is invalid or missing</exception>
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

        CheckIsValid(config);
                            
        return config;
    }

    /// <summary>
    /// Saves the configuration to the config file.
    /// </summary>
    /// <param name="config">The configuration to save</param>
    public static void Save(DcsBiosConfig config)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(ConfigFile, json);
    }

    /// <summary>
    /// Validates the configuration settings to ensure they are correct.
    /// </summary>
    /// <param name="config">The configuration to validate</param>
    /// <exception cref="ConfigException">Thrown when configuration values are invalid</exception>
    private static void CheckIsValid(DcsBiosConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.DcsBiosJsonLocation))
        {
            throw new ConfigException("The 'DcsBiosJsonLocation' field in the configuration file cannot be empty. Please specify a valid path to the DCS-BIOS JSON file.");
        }

        if (!Directory.Exists(config.DcsBiosJsonLocation))
        {
            throw new ConfigException($"The folder specified by 'DcsBiosJsonLocation' does not exist: {config.DcsBiosJsonLocation}");
        }

        if (Directory.GetFiles(config.DcsBiosJsonLocation).Length == 0)
        {
            throw new ConfigException($"The folder specified by 'DcsBiosJsonLocation' is empty: {config.DcsBiosJsonLocation}");
        }

        ValidatePort(config.ReceivePortUdp, nameof(config.ReceivePortUdp));
        ValidatePort(config.SendPortUdp, nameof(config.SendPortUdp));
        ValidateIpAddress(config.ReceiveFromIpUdp, nameof(config.ReceiveFromIpUdp));
        ValidateIpAddress(config.SendToIpUdp, nameof(config.SendToIpUdp));
    }
    
    private static void ValidatePort(int port, string portName)
    {
        if (port is < MinPortNumber or > MaxPortNumber)
        {
            throw new ConfigException($"{portName} must be between {MinPortNumber} and {MaxPortNumber}.");
        }
    }
    
    private static void ValidateIpAddress(string ipAddress, string fieldName)
    {
        if (!IPAddress.TryParse(ipAddress, out var parsedIp))
        {
            throw new ConfigException($"{fieldName} is not a valid IP address.");
        }
        
        if (parsedIp.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ConfigException($"{fieldName} must be an IPv4 address.");
        }
    }
}
