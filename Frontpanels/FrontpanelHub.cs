using System;
using System.Collections.Generic;
using System.Linq;
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

    /// <summary>
    /// Gets the combined capabilities of all connected frontpanels.
    /// Returns a capability as true if at least one adapter supports it.
    /// </summary>
    public IFrontpanelCapabilities Capabilities => new AggregatedCapabilities(_adapters);

    public FrontpanelHub(IEnumerable<IFrontpanelAdapter> adapters)
    {
        _adapters = new List<IFrontpanelAdapter>(adapters ?? throw new ArgumentNullException(nameof(adapters)));
    }

    /// <summary>
    /// Updates the display on all connected frontpanels.
    /// </summary>
    public void UpdateDisplay(IFrontpanelState? state)
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
    public void UpdateLeds(IFrontpanelLeds? leds)
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

    /// <summary>
    /// Aggregates capabilities from multiple frontpanel adapters.
    /// A capability is considered supported if at least one adapter supports it.
    /// </summary>
    private class AggregatedCapabilities : IFrontpanelCapabilities
    {
        private readonly IEnumerable<IFrontpanelAdapter> _adapters;

        public AggregatedCapabilities(IEnumerable<IFrontpanelAdapter> adapters)
        {
            _adapters = adapters;
        }

        public bool HasSpeedDisplay => _adapters.Any(a => a.Capabilities.HasSpeedDisplay);
        public bool HasHeadingDisplay => _adapters.Any(a => a.Capabilities.HasHeadingDisplay);
        public bool HasAltitudeDisplay => _adapters.Any(a => a.Capabilities.HasAltitudeDisplay);
        public bool HasVerticalSpeedDisplay => _adapters.Any(a => a.Capabilities.HasVerticalSpeedDisplay);
        public bool CanDisplayBarometricPressure => _adapters.Any(a => a.Capabilities.CanDisplayBarometricPressure);
        public bool CanDisplayQnhQfe => _adapters.Any(a => a.Capabilities.CanDisplayQnhQfe);
        public bool HasPilotCourseDisplay => _adapters.Any(a => a.Capabilities.HasPilotCourseDisplay);
        public bool HasCopilotCourseDisplay => _adapters.Any(a => a.Capabilities.HasCopilotCourseDisplay);
        public bool SupportsAlphanumericDisplay => _adapters.Any(a => a.Capabilities.SupportsAlphanumericDisplay);
        public bool HasFlightLevelMode => _adapters.Any(a => a.Capabilities.HasFlightLevelMode);
        public bool HasMachSpeedMode => _adapters.Any(a => a.Capabilities.HasMachSpeedMode);
    }
}
