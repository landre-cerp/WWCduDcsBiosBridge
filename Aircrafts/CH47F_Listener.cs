using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WWCduDcsBiosBridge.Aircrafts;

internal class CH47F_Listener : AircraftListener
{
    protected const int MAX_CDU_LINES = 14;

    // Buffers for CDU lines and colors
    private readonly DCSBIOSOutput?[] cduLines = new DCSBIOSOutput?[MAX_CDU_LINES];
    private readonly DCSBIOSOutput?[] cduColorLines = new DCSBIOSOutput?[MAX_CDU_LINES];

    private DCSBIOSOutput? _MSTR_CAUTION;
    private DCSBIOSOutput? _CDU_BACKLIGHT;

    // Which address maps to which line
    private Dictionary<uint, int>? lineMap;
    private Dictionary<uint, int>? colorLines;

    private static readonly string[] ColorMap = Enumerable.Range(0, 14)
        .Select(_ => new string(' ', 24))
        .ToArray();

    protected override string GetAircraftName() => SupportedAircrafts.CH47_Name;

    protected override string GetFontFile() => "resources/ch47f-font-21x31.json";

    protected string prefix;

    private readonly Dictionary<string, Colour> _Colours = new()
    {
        [" "] = Colour.Black,
        ["g"] = Colour.Green,
        ["p"] = Colour.Magenta,
        ["w"] = Colour.White
    };

    public CH47F_Listener(ICdu mcdu, UserOptions options,  bool pilot=true) : base(mcdu, SupportedAircrafts.CH47, options)
    {
        prefix = pilot ? "PLT_": "CPLT_";
    }

    protected override void InitializeDcsBiosControls()
    {
        for (int i = 0; i < MAX_CDU_LINES; i++)
        {
            cduLines[i] = DCSBIOSControlLocator.GetStringDCSBIOSOutput($"{prefix}CDU_LINE{i+1}");
            cduColorLines[i] = DCSBIOSControlLocator.GetStringDCSBIOSOutput($"{prefix}CDU_LINE{i+1}_COLOR");
        }

        _MSTR_CAUTION = DCSBIOSControlLocator.GetUIntDCSBIOSOutput(prefix + "MASTER_CAUTION_LIGHT");
        _CDU_BACKLIGHT = DCSBIOSControlLocator.GetUIntDCSBIOSOutput(prefix + "INT_LIGHT_CDU");

        lineMap = new Dictionary<uint, int>();
        for (int i = 0; i < MAX_CDU_LINES; i++)
        {
            if (cduLines[i] != null)
            {
                lineMap.Add(cduLines[i]!.Address, i + 1);
            }
        }
        colorLines = new Dictionary<uint, int>();
        for (int i = 0; i < MAX_CDU_LINES; i++)
        {
            if (cduColorLines[i] != null)
            {
                colorLines.Add(cduColorLines[i]!.Address, i + 1);
            }
        }
    }

    public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
    {
        var output = GetCompositor(DEFAULT_PAGE);

        try
        {
            string data = e.StringData
                .Replace("»", "→")
                .Replace("«", "←")
                .Replace("¡", "☐")
                .Replace("}", "↓")
                .Replace("{", "↑")
                .Replace("®", "Δ");

            output.White();

            if (colorLines!.TryGetValue(e.Address, out int colorLine))
            {
                ColorMap[colorLine - 1] = data;
            }

            if (lineMap!.TryGetValue(e.Address, out int lineIndex))
            {
                // update line with this fast method 
                var screen = pages[DEFAULT_PAGE];  
                var row = screen.Rows[lineIndex - 1];
                var color = ColorMap[lineIndex - 1];
                for (var cellIdx = 0; cellIdx < row.Cells.Length; ++cellIdx)
                {
                    var cell = row.Cells[cellIdx];
                    cell.Character = cellIdx < data.Length ? data[cellIdx] : ' ';
                    _Colours.TryGetValue(color[cellIdx].ToString(), out Colour value);
                    cell.Colour = value;
                    cell.Small = lineIndex % 2 == 0 && lineIndex != 14;
                }

            }
        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS string data");
        }
    }

    public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
    {
        var refresh = false;
        if (e.Address == _MSTR_CAUTION!.Address)
        {
            mcdu.Leds.Fail = _MSTR_CAUTION.GetUIntValue(e.Data) != 0;
            refresh = true;
        }

        if (!options.DisableLightingManagement)
        {

            if (e.Address == _CDU_BACKLIGHT!.Address)
            {
                int bright = (int)_CDU_BACKLIGHT.GetUIntValue(e.Data);

                bright = bright * 100 / 65536;
                mcdu.BacklightBrightnessPercent = bright;
                if (options.LinkedScreenBrightness)
                {
                    mcdu.DisplayBrightnessPercent = bright;
                }

                mcdu.LedBrightnessPercent = bright;
                refresh = true;
            }
        }

        if (refresh)
        {
            if ( ! options.DisableLightingManagement ) mcdu.RefreshBrightnesses();
            mcdu.RefreshLeds();
        }
    }
}