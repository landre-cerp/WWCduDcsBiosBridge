using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using WwDevicesDotNet;
using WWCduDcsBiosBridge.Aircrafts;
using WWCduDcsBiosBridge.Config;


namespace WWCduDcsBiosBridge;

/// <summary>
/// Represents the context for a device (CDU or Frontpanel) within the bridge.
/// CDU devices show an aircraft selection menu, while Frontpanel devices automatically
/// participate once an aircraft is selected globally.
/// </summary>
internal class DeviceContext : IDisposable
{
    public ICdu? Mcdu { get; }
    public IFrontpanel? Frontpanel { get; }
    public bool IsCduDevice => Mcdu != null;
    public bool IsFrontpanelDevice => Frontpanel != null;
    
    public AircraftSelection? SelectedAircraft { get; private set; }
    public bool IsSelectedAircraft { get => isSelectedAircraft; }
    public bool Pilot { get; private set; } = true;
    
    private readonly DcsBiosConfig? config;
    private readonly UserOptions options;
    private readonly AircraftSelectionMenu? menu;
    private AircraftListener? listener;
    private bool isSelectedAircraft = false;

    /// <summary>
    /// Creates a context for a CDU device with aircraft selection menu
    /// </summary>
    public DeviceContext(ICdu mcdu, UserOptions options, DcsBiosConfig? config)
    {
        Mcdu = mcdu;
        this.options = options;
        this.config = config;
        menu = new AircraftSelectionMenu(mcdu);
        menu.AircraftSelected += OnAircraftSelected;
    }

    /// <summary>
    /// Creates a context for a Frontpanel device without aircraft selection menu
    /// </summary>
    public DeviceContext(IFrontpanel frontpanel, UserOptions options, DcsBiosConfig? config)
    {
        Frontpanel = frontpanel;
        this.options = options;
        this.config = config;
        // Frontpanel devices don't show aircraft selection menu
        // They wait for global aircraft selection to be propagated
    }

    public void ShowStartupScreen()
    {
        if (IsCduDevice)
        {
            menu?.Show();
        }
        // Frontpanel devices don't show a startup screen
    }

    /// <summary>
    /// Sets the aircraft selection for this device context
    /// </summary>
    public void SetAircraftSelection(AircraftSelection selection)
    {
        isSelectedAircraft = true;
        SelectedAircraft = selection;
    }

    private void OnAircraftSelected(object? sender, AircraftSelectedEventArgs e)
    {
        SetAircraftSelection(e.Selection);
    }

    public void StartBridge(IFrontpanel? frontpanel = null)
    {
        if (!isSelectedAircraft || SelectedAircraft == null) return;

        DCSAircraft.Init();
        DCSAircraft.FillModulesListFromDcsBios(config!.DcsBiosJsonLocation, true);
        DCSBIOSControlLocator.JSONDirectory = config.DcsBiosJsonLocation;
        
        try
        {
            if (IsCduDevice)
            {
                // CDU device: create listener with CDU display
                listener = new AircraftListenerFactory().CreateListener(SelectedAircraft, Mcdu!, options, frontpanel);
                listener.Start();
            }
            else if (IsFrontpanelDevice)
            {
                // Frontpanel-only device: create listener without CDU (pass null for mcdu, pass this frontpanel)
                listener = new AircraftListenerFactory().CreateListener(SelectedAircraft, null, options, Frontpanel);
                listener.Start();
            }
        }
        catch (NotSupportedException ex)
        {
            if (Mcdu != null)
            {
                Mcdu.Output.Newline().Red().WriteLine(ex.Message);
                Mcdu.RefreshDisplay();
            }
        }
    }

    public void Dispose()
    {
        menu?.Dispose();
        listener?.Dispose();
    }
}