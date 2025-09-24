using McduDotNet;
using NLog;

namespace WWCduDcsBiosBridge;

/// <summary>
/// Manages CDU device detection and connection
/// </summary>
public class DeviceManager
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Detects and connects to all available CDU devices
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
    /// Gets a friendly name for a device
    /// </summary>
    /// <param name="deviceId">The device identifier</param>
    /// <returns>Device display name</returns>
    public static string GetDeviceName(DeviceIdentifier deviceId)
    {
        return deviceId.Description;
    }

    /// <summary>
    /// Tests a device display by showing a test pattern
    /// </summary>
    /// <param name="device">The device to test</param>
    public static void TestDeviceDisplay(ICdu device)
    {
        try
        {
            device.Output.Clear().Green()
                .Line(0).Centered($"TEST DEVICE {device.DeviceId}")
                .Line(2).Yellow().Centered("Display Test")
                .Line(4).White().WriteLine("Device is working!");

            device.RefreshDisplay();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to test device {device.DeviceId} display");
            throw;
        }
    }

    /// <summary>
    /// Clears a device display
    /// </summary>
    /// <param name="device">The device to clear</param>
    public static void ClearDeviceDisplay(ICdu device)
    {
        try
        {
            device.Output.Clear();
            device.RefreshDisplay();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to clear device display");
            throw;
        }
    }

    /// <summary>
    /// Disposes a list of devices safely
    /// </summary>
    /// <param name="devices">Devices to dispose</param>
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