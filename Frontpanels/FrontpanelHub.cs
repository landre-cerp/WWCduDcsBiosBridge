using WwDevicesDotNet;

namespace WWCduDcsBiosBridge.Frontpanels;

/// <summary>
/// Hub that aggregates multiple frontpanel adapters and provides a unified API
/// for broadcasting updates to all connected frontpanels.
/// </summary>
public class FrontpanelHub
{
    private readonly List<IFrontpanelAdapter> _adapters;

    /// <summary>
    /// Gets the collection of frontpanel adapters.
    /// </summary>
    public IReadOnlyList<IFrontpanelAdapter> Adapters => _adapters.AsReadOnly();

    /// <summary>
    /// Gets a value indicating whether any frontpanels are connected.
    /// </summary>
    public bool HasFrontpanels => _adapters.Count > 0;

    /// <summary>
    /// Gets the number of connected frontpanels.
    /// </summary>
    public int Count => _adapters.Count;

    public FrontpanelHub(IEnumerable<IFrontpanelAdapter> adapters)
    {
        _adapters = new List<IFrontpanelAdapter>(adapters ?? throw new ArgumentNullException(nameof(adapters)));
    }

    /// <summary>
    /// Updates the display on all connected frontpanels.
    /// </summary>
    public void UpdateDisplay(IFrontpanelState state)
    {
        if (state == null) return;

        foreach (var adapter in _adapters.Where(a => a.IsConnected))
        {
            try
            {
                adapter.UpdateDisplay(state);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, $"Failed to update display on {adapter.DisplayName}");
            }
        }
    }

    /// <summary>
    /// Updates the LEDs on all connected frontpanels.
    /// </summary>
    public void UpdateLeds(IFrontpanelLeds leds)
    {
        if (leds == null) return;

        foreach (var adapter in _adapters.Where(a => a.IsConnected))
        {
            try
            {
                adapter.UpdateLeds(leds);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, $"Failed to update LEDs on {adapter.DisplayName}");
            }
        }
    }

    /// <summary>
    /// Sets the brightness on all connected frontpanels.
    /// </summary>
    public void SetBrightness(byte panelBacklight, byte lcdBacklight, byte ledBacklight)
    {
        foreach (var adapter in _adapters.Where(a => a.IsConnected))
        {
            try
            {
                adapter.SetBrightness(panelBacklight, lcdBacklight, ledBacklight);
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, $"Failed to set brightness on {adapter.DisplayName}");
            }
        }
    }

    /// <summary>
    /// Creates an empty hub with no frontpanels.
    /// </summary>
    public static FrontpanelHub CreateEmpty() => new FrontpanelHub(Enumerable.Empty<IFrontpanelAdapter>());
}
