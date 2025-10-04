using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using McduDotNet;
using WWCduDcsBiosBridge.Aircrafts;
using WWCduDcsBiosBridge.Config;


namespace WWCduDcsBiosBridge;

internal class DeviceContext : IDisposable
{
    public ICdu Mcdu { get; }
    public AircraftSelection SelectedAircraft { get; private set; }
    public bool IsSelectedAircraft { get => isSelectedAircraft; }
    public bool Pilot { get; private set; } = true;
    
    private readonly DcsBiosConfig? config;
    private readonly UserOptions options;
    private readonly AircraftSelectionMenu menu;
    private AircraftListener? listener;
    private bool isSelectedAircraft = false;

    public DeviceContext(ICdu mcdu, UserOptions options, DcsBiosConfig? config)
    {
        Mcdu = mcdu;
        this.options = options;
        this.config = config;
        menu = new AircraftSelectionMenu(mcdu);
        menu.OnAircraftSelected += OnAircraftSelected;
    }

    public void ShowStartupScreen()
    {
        menu.Show();
    }

    private void OnAircraftSelected(object? sender, AircraftSelectedEventArgs e)
    {
        isSelectedAircraft = true;
        SelectedAircraft = e.Selection;
    }

    public void StartBridge()
    {
        if (!isSelectedAircraft) return;

        DCSAircraft.Init();
        DCSAircraft.FillModulesListFromDcsBios(config!.DcsBiosJsonLocation, true);
        DCSBIOSControlLocator.JSONDirectory = config.DcsBiosJsonLocation;
        
        try
        {
            listener = new AircraftListenerFactory().CreateListener(SelectedAircraft, Mcdu, options);
            listener.Start();
        }
        catch (NotSupportedException ex)
        {
            Mcdu.Output.Newline().Red().WriteLine(ex.Message);
            Mcdu.RefreshDisplay();
        }
    }

    public void Dispose()
    {
        menu?.Dispose();
        listener?.Dispose();
    }
}