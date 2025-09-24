using McduDotNet;

namespace WWCduDcsBiosBridge;

/// <summary>
/// Contains information about a detected CDU device
/// </summary>
public class DeviceInfo
{
    public ICdu Cdu { get; set; }
    public DeviceIdentifier DeviceId { get; set; }
    public string DisplayName { get; set; }

    public DeviceInfo(ICdu cdu, DeviceIdentifier deviceId, string displayName)
    {
        Cdu = cdu;
        DeviceId = deviceId;
        DisplayName = displayName;
    }
}