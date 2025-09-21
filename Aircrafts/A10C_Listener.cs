using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;

namespace WWCduDcsBiosBridge.Aircrafts;

internal class A10C_Listener : AircraftListener
{
    private readonly DCSBIOSOutput?[] cduLines = new DCSBIOSOutput?[10];

    private DCSBIOSOutput? _CDU_BRT; 
    private DCSBIOSOutput? _MASTER_CAUTION; 

    private DCSBIOSOutput? _CONSOLE_BRT; 
    private DCSBIOSOutput? _NOSE_SW_GREENLIGHT;
    private DCSBIOSOutput? _CANOPY_LED; 
    private DCSBIOSOutput? _GUN_READY;

    private DCSBIOSOutput? _CMSP1;
    private DCSBIOSOutput? _CMSP2;

    protected override string GetAircraftName() => SupportedAircrafts.A10C_Name;
    protected override string GetFontFile() => "resources/a10c-font-21x31.json";

    public A10C_Listener(
        ICdu mcdu, 
        UserOptions options) : base(mcdu, SupportedAircrafts.A10C, options) {
    }


    ~A10C_Listener()
    {
        Dispose(false);
    }

    protected override void initBiosControls()
    {

        for (int i = 0; i < 10; i++)
        {
            cduLines[i] = DCSBIOSControlLocator.GetStringDCSBIOSOutput($"CDU_LINE{i}");
        }

        _CDU_BRT = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("CDU_BRT");
        _MASTER_CAUTION = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("MASTER_CAUTION");

        _CONSOLE_BRT = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("INT_CONSOLE_L_BRIGHT");
        _NOSE_SW_GREENLIGHT= DCSBIOSControlLocator.GetUIntDCSBIOSOutput("NOSEWHEEL_STEERING");
        _CANOPY_LED = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("CANOPY_UNLOCKED");
        _GUN_READY = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("GUN_READY");

        _CMSP1 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CMSP1");
        _CMSP2 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CMSP2");

    }

    public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
    {
        try
        {
            bool refresh = false;
            UpdateCounter(e.Address, e.Data);

            if ( ! options.DisableLightingManagement)
            {
                if (e.Address == _CONSOLE_BRT!.Address)
                {
                    mcdu.BacklightBrightnessPercent =
                        (int)(_CONSOLE_BRT!.GetUIntValue(e.Data) * 100 / _CONSOLE_BRT.MaxValue);
                    refresh = true;
                }

                if (e.Address == _CDU_BRT!.Address)
                {
                    int val = (int)_CDU_BRT.GetUIntValue(e.Data);
                    if (val == 0)
                        mcdu.DisplayBrightnessPercent = Math.Min(100, mcdu.DisplayBrightnessPercent - 5);
                    else if (val == 2)
                        mcdu.DisplayBrightnessPercent = Math.Min(100, mcdu.DisplayBrightnessPercent + 5);
                    // Always refresh Brightness. 
                    refresh = true;
                }

            }

            if (e.Address == _CANOPY_LED!.Address)
            {
                mcdu.Leds.Fm2 = _CANOPY_LED!.GetUIntValue(e.Data) == 1;
                refresh = true;
            }
            if (e.Address == _NOSE_SW_GREENLIGHT!.Address)
            {
                mcdu.Leds.Ind = _NOSE_SW_GREENLIGHT!.GetUIntValue(e.Data) == 1;
                refresh = true;
            }
            if (e.Address == _GUN_READY!.Address)
            {
                mcdu.Leds.Fm1 = _GUN_READY.GetUIntValue(e.Data) == 1;
                refresh = true;
            }
            if (e.Address == _MASTER_CAUTION!.Address)
            {
                mcdu.Leds.Fail = _MASTER_CAUTION.GetUIntValue(e.Data) == 1;
                refresh = true;
            }

            if (refresh)
            {
                if ( ! options.DisableLightingManagement) mcdu.RefreshBrightnesses();
                mcdu.RefreshLeds();
            }
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS data");
        }
    }


    public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
    {
        try
        {

            string data = e.StringData
                .Replace("»", "→")
                .Replace("«", "←")
                .Replace("¡", "☐")
                .Replace("®", "Δ")
                .Replace("©", "^")
                .Replace("±", "_")
                .Replace("?", "%");

            mcdu.Output.Green();

            Dictionary<uint,int> lineMap; 

            if (options.DisplayBottomAligned)
            {
                lineMap = new Dictionary<uint, int>
                {
                    { _CMSP1!.Address, 0 },
                    { _CMSP2!.Address, 1 },
                    { cduLines[0]!.Address, 4 },
                    { cduLines[1]!.Address, 5 },
                    { cduLines[2]!.Address, 6 },
                    { cduLines[3]!.Address, 7 },
                    { cduLines[4]!.Address, 8 },
                    { cduLines[5]!.Address, 9 },
                    { cduLines[6]!.Address, 10 },
                    { cduLines[7]!.Address, 11 },
                    { cduLines[8]!.Address, 12 },
                    { cduLines[9]!.Address, 13 },
                };
            }
            else
            {
                lineMap = new Dictionary<uint, int>
                {
                    { cduLines[0]!.Address, 0},
                    { cduLines[1]!.Address, 1 },
                    { cduLines[2]!.Address, 2},
                    { cduLines[3]!.Address, 3 },
                    { cduLines[4]!.Address, 4 },
                    { cduLines[5]!.Address, 5 },
                    { cduLines[6]!.Address, 6 },
                    { cduLines[7]!.Address, 7 },
                    { cduLines[8]!.Address, 8 },
                    { cduLines[9]!.Address, 9 },
                    { _CMSP1!.Address, 12 },
                    { _CMSP2!.Address, 13 },
                };
            }

            if (lineMap.TryGetValue(e.Address, out int lineIndex))
            {
                if (options.DisplayCMS || (_CMSP1!.Address != e.Address && _CMSP2!.Address != e.Address))
                {
                    mcdu.Output.Line(lineIndex).WriteLine(data);
                }
            }

            if (options.DisplayCMS)
            {
                mcdu.Output.Line(options.DisplayBottomAligned ? 2 : 11).Amber().WriteLine("------------------------");
            }
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS string data");
        }
    }
}
