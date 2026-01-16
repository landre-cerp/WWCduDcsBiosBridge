using WwDevicesDotNet;
using WwDevicesDotNet.WinWing.FcuAndEfis;

namespace WWCduDcsBiosBridge.Frontpanels;

/// <summary>
/// Adapter for FCU/EFIS devices that implements the capability-based pattern.
/// </summary>
public class FcuEfisAdapter : IFrontpanelAdapter
{
    private readonly FcuEfisDevice _device;

    public IFrontpanel Device => _device;
    public string DisplayName { get; }
    public bool IsConnected => _device.IsConnected;

    public FcuEfisAdapter(FcuEfisDevice device, string displayName)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }

    public void UpdateDisplay(IFrontpanelState state)
    {
        _device.UpdateDisplay(state);
    }

    public void UpdateLeds(IFrontpanelLeds leds)
    {
        _device.UpdateLeds(leds);
    }

    public void SetBrightness(byte panelBacklight, byte lcdBacklight, byte ledBacklight)
    {
        _device.SetBrightness(panelBacklight, lcdBacklight, ledBacklight);
    }
}
