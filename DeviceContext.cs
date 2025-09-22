using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using McduDotNet;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using WWCduDcsBiosBridge.Aircrafts;
using WWCduDcsBiosBridge.Config;


namespace WWCduDcsBiosBridge;

internal class DeviceContext: IDisposable
{
    const int NO_AIRCRAFT_SELECTED = -1;
    public ICdu Mcdu { get; }
    public int SelectedAircraft { get; private set; } = NO_AIRCRAFT_SELECTED;
    public bool Pilot { get; private set; } = true;
    private readonly DcsBiosConfig? config;
    private readonly UserOptions options;
    private AircraftListener? listener;


    public DeviceContext(ICdu mcdu,
        UserOptions options,
        DcsBiosConfig? config)
    {
        Mcdu = mcdu;
        this.options = options;
        this.config = config;
    }

    public void ShowStartupScreen()
    {
        using var fileStream = new FileStream("resources/a10c-font-21x31.json", FileMode.Open, FileAccess.Read);
        using var reader = new StreamReader(fileStream);
        var fontJson = reader.ReadToEnd();

        Mcdu.UseFont(JsonConvert.DeserializeObject<McduFontFile>(fontJson), true);
        Mcdu.Output.Clear().Green()
            .Line(0).Centered("DCSbios/WWCDU Bridge")
            .NewLine().Large().Yellow().Centered("by Cerppo")
            .White()
            .LeftLabel(2, SupportedAircrafts.A10C_Name )
            .RightLabel(2, SupportedAircrafts.AH64D_Name)
            .LeftLabel(3, SupportedAircrafts.FA18C_Name)
            .RightLabel(3,$"{SupportedAircrafts.CH47_Name} (PLT)")
            .LeftLabel(4, SupportedAircrafts.F15E_Name)
            .RightLabel(4, $"{SupportedAircrafts.CH47_Name} (CPLT)")
            .BottomLine().WriteLine("Close app to exit");
        Mcdu.RefreshDisplay();
        Mcdu.KeyDown += ReadMenu;
    }

    private void ReadMenu(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.LineSelectLeft2: SelectedAircraft = SupportedAircrafts.A10C ; break;  
            case Key.LineSelectRight2: SelectedAircraft = SupportedAircrafts.AH64D; break; 
            case Key.LineSelectLeft3: SelectedAircraft = SupportedAircrafts.FA18C; break;  
            case Key.LineSelectRight3: SelectedAircraft = SupportedAircrafts.CH47; Pilot = true; break; 
            case Key.LineSelectLeft4: SelectedAircraft = SupportedAircrafts.F15E; break;  
            case Key.LineSelectRight4: SelectedAircraft = SupportedAircrafts.CH47; Pilot = false; break; 
        }

        if (SelectedAircraft is NO_AIRCRAFT_SELECTED) return;

        // Aircraft selected, remove this handler
        Mcdu.KeyDown -= ReadMenu;

    }

    public void StartBridge()
    {

        DCSAircraft.Init();
        DCSAircraft.FillModulesListFromDcsBios(config!.DcsBiosJsonLocation, true);
        DCSBIOSControlLocator.JSONDirectory = config.DcsBiosJsonLocation;
        try
        {
            listener = new AircraftListenerFactory().CreateListener(SelectedAircraft, Mcdu, options, Pilot);
            listener.Start();
        }
        catch (NotSupportedException ex) {             
            Mcdu.Output.Newline().Red().WriteLine(ex.Message);
            Mcdu.RefreshDisplay();
            return;
        }
    }

    public void Dispose()
    {
        listener?.Dispose();
    }
}