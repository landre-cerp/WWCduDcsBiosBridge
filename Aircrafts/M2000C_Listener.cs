using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;
using System;

namespace WWCduDcsBiosBridge.Aircrafts;

internal class M2000C_Listener : AircraftListener
{
    private DCSBIOSOutput? PCN_DISP_L;
    private DCSBIOSOutput? PCN_DISP_R;
    private DCSBIOSOutput? PCN_DISP_PREP;
    private DCSBIOSOutput? PCN_DISP_DEST;

    protected override string GetFontFile() => "resources/ah64d-font-21x31.json";
    protected override string GetAircraftName() => SupportedAircrafts.M2000C_Name;

    public M2000C_Listener(ICdu mcdu, UserOptions options) : base(mcdu, SupportedAircrafts.M2000C, options)
    {
    }

    protected override void InitializeDcsBiosControls()
    {
        PCN_DISP_L = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_L");
        PCN_DISP_R = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_R");
        PCN_DISP_PREP = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_PREP");
        PCN_DISP_DEST = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PCN_DISP_DEST");
    }

    public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
    {
        try
        {
            UpdateCounter(e.Address, e.Data);
        }
        catch (Exception)
        {
        }
    }

    public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
    {
        var output = GetCompositor(DEFAULT_PAGE);
        try
        {
            UpdateLine(output.Line(2).Green(), PCN_DISP_L, e);
            UpdateLine(output.Line(3).Green(), PCN_DISP_R, e);
            UpdateLine(output.Line(4).Green(), PCN_DISP_PREP, e);
            UpdateLine(output.Line(5).Green(), PCN_DISP_DEST, e);
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS string data");
        }
    }

    private void UpdateLine(Compositor display, DCSBIOSOutput? output, DCSBIOSStringDataEventArgs e)
    {
        if (output == null || e.Address != output.Address) return;
        string data = e.StringData;
        display.Centered(data);
    }
}
