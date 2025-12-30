using McduDotNet;
using Newtonsoft.Json;
using NLog;
using System.IO;

namespace WWCduDcsBiosBridge;

/// <summary>
/// Manages CDU device detection and connection
/// </summary>
public class DeviceManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Progress info for async device detection
    /// </summary>
    public sealed record DeviceDetectionProgress(int Current, int Total, string Message);

    /// <summary>
    /// Detects and connects to all available CDU devices synchronously
    /// </summary>
    /// <returns>List of connected devices</returns>
    public static List<DeviceInfo> DetectAndConnectDevices()
    {
        var detectedDevices = new List<DeviceInfo>();
        try
        {
            var deviceIdentifiers = CduFactory.FindLocalDevices().ToList();
            for (int i = 0; i < deviceIdentifiers.Count; i++)
            {
                var deviceId = deviceIdentifiers[i];
                try
                {
                    var cdu = CduFactory.ConnectLocal(deviceId);
                    initCdu(cdu);
                    var displayName = GetDeviceName(deviceId);
                    var deviceInfo = new DeviceInfo(cdu, deviceId, displayName);
                    detectedDevices.Add(deviceInfo);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to connect to device {i + 1}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to detect devices");
            throw;
        }
        return detectedDevices;
    }

    /// <summary>
    /// Asynchronously detects and connects to all available CDU devices with progress reporting
    /// </summary>
    public static async Task<List<DeviceInfo>> DetectAndConnectDevicesAsync(
        IProgress<DeviceDetectionProgress>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        var detectedDevices = new List<DeviceInfo>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // If FindLocalDevicesAsync exists, use it. Otherwise, wrap in Task.Run.
            var deviceIdentifiers = await Task.Run(() => CduFactory.FindLocalDevices().ToList(), cancellationToken).ConfigureAwait(false);
            progress?.Report(new DeviceDetectionProgress(0, deviceIdentifiers.Count, deviceIdentifiers.Count == 0 ? "No devices found" : $"Found {deviceIdentifiers.Count} device(s). Connecting..."));

            for (int i = 0; i < deviceIdentifiers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var deviceId = deviceIdentifiers[i];
                progress?.Report(new DeviceDetectionProgress(i, deviceIdentifiers.Count, $"Connecting device {i + 1}/{deviceIdentifiers.Count}..."));
                try
                {
                    // If ConnectLocalAsync exists, use it. Otherwise, wrap in Task.Run.
                    var cdu = await Task.Run(() => CduFactory.ConnectLocal(deviceId), cancellationToken).ConfigureAwait(false);
                    initCdu(cdu);
                    var displayName = GetDeviceName(deviceId);
                    var deviceInfo = new DeviceInfo(cdu, deviceId, displayName);
                    detectedDevices.Add(deviceInfo);
                    progress?.Report(new DeviceDetectionProgress(i + 1, deviceIdentifiers.Count, $"Connected {displayName} ({i + 1}/{deviceIdentifiers.Count})"));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Failed to connect to device {i + 1}");
                    progress?.Report(new DeviceDetectionProgress(i + 1, deviceIdentifiers.Count, $"Failed to connect device {i + 1}: {ex.Message}"));
                }
            }

            progress?.Report(new DeviceDetectionProgress(deviceIdentifiers.Count, deviceIdentifiers.Count, $"Detection complete. {detectedDevices.Count} connected."));
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new DeviceDetectionProgress(0, 0, "Device detection cancelled"));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to detect devices");
            progress?.Report(new DeviceDetectionProgress(0, 0, $"Detection error: {ex.Message}"));
        }
        return detectedDevices;
    }

    private static void initCdu(ICdu mcdu)
    {
        using var fileStream = new FileStream("resources/a10c-font-21x31.json", FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream);
        var fontJson = reader.ReadToEnd();
        mcdu.UseFont(JsonConvert.DeserializeObject<McduFontFile>(fontJson), true);
    }

    /// <summary>
    /// Gets a friendly name for a device
    /// </summary>
    public static string GetDeviceName(DeviceIdentifier deviceId) => deviceId.Description;

    /// <summary>
    /// Disposes a list of devices safely
    /// </summary>
    public static void DisposeDevices(IEnumerable<DeviceInfo> devices)
    {
        if (devices == null) return;
        foreach (var deviceInfo in devices)
        {
            try
            {
                deviceInfo.Cdu?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error disposing device");
            }
        }
    }
}