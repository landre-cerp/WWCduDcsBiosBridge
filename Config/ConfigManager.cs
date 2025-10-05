using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using WWCduDcsBiosBridge.Aircrafts;

namespace WWCduDcsBiosBridge.Config;

/// <summary>
/// Manages configuration loading, saving, and validation for the DCS-BIOS bridge application.
/// </summary>
public static class ConfigManager
{
    private static readonly string ConfigFile =
        Path.Combine(AppContext.BaseDirectory, "config.json");

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
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
    }

    /// <summary>
    /// Public validation entry point for UI and callers.
    /// Throws ConfigException if invalid.
    /// </summary>
    public static void Validate(DcsBiosConfig config) => CheckIsValid(config);

    /// <summary>
    /// Returns the list of expected DCS-BIOS JSON files that are missing under the provided directory.
    /// This is a soft check (no exceptions thrown).
    /// </summary>
    public static IReadOnlyList<string> GetMissingExpectedJsonFiles(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            return Array.Empty<string>();

        var present = new HashSet<string>(
            Directory.EnumerateFiles(rootDirectory, "*.json", SearchOption.AllDirectories)
                     .Select(f => Path.GetFileName(f)!.ToLowerInvariant())
        );

        return SupportedAircrafts.expected_json
            .Where(name => !present.Contains(name.ToLowerInvariant()))
            .ToArray();
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
            throw new ConfigException("The 'DcsBiosJsonLocation' field in the configuration file cannot be empty. Please specify a valid path to the DCS-BIOS JSON file(s).");
        }

        if (!Directory.Exists(config.DcsBiosJsonLocation))
        {
            throw new ConfigException($"The folder specified by 'DcsBiosJsonLocation' does not exist: {config.DcsBiosJsonLocation}");
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
