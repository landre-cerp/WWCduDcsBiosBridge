using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using WwDevicesDotNet;
using WwDevicesDotNet.WinWing.FcuAndEfis;
using WwDevicesDotNet.WinWing.Pap3;
using WWCduDcsBiosBridge.Frontpanels;

namespace WWCduDcsBiosBridge.Aircrafts;

internal class A10C_Listener : AircraftListener
{
    private const int BRT_STEP = 5;
    private readonly DCSBIOSOutput?[] cduLines = new DCSBIOSOutput?[10];

    private DCSBIOSOutput? _CDU_BRT; 
    private DCSBIOSOutput? _MASTER_CAUTION; 

    private DCSBIOSOutput? _CONSOLE_BRT; 
    private DCSBIOSOutput? _NOSE_SW_GREENLIGHT;
    private DCSBIOSOutput? _CANOPY_LED; 
    private DCSBIOSOutput? _GUN_READY;

    private DCSBIOSOutput? _CMSP1;
    private DCSBIOSOutput? _CMSP2;

    private DCSBIOSOutput? _HEADING;
    private DCSBIOSOutput? _IAS;
    private DCSBIOSOutput? _VS;
    private DCSBIOSOutput? _ALT_PRESSURE0;
    private DCSBIOSOutput? _ALT_PRESSURE1;
    private DCSBIOSOutput? _ALT_PRESSURE2;
    private DCSBIOSOutput? _ALT_PRESSURE3;

    private DCSBIOSOutput? _ALTITUDE_10000ft;
    private DCSBIOSOutput? _ALTITUDE_1000ft;
    private DCSBIOSOutput? _ALTITUDE_100ft;

    private int? speed;
    private int? heading;
    private int? altitude;
    private int? verticalSpeed;
    private int? baroPressure;
    private int[] pressureDigits = new int[4];
    private int[] altitudeDigits = new int[3];

    protected override string GetAircraftName() => SupportedAircrafts.A10C_Name;
    protected override string GetFontFile() => "resources/a10c-font-21x31.json";

    public A10C_Listener(
        ICdu? mcdu, 
        UserOptions options,
        FrontpanelHub frontpanelHub) : base(mcdu, SupportedAircrafts.A10C, options, frontpanelHub) {
    }

    ~A10C_Listener()
    {
        Dispose(false);
    }

    protected override void InitializeDcsBiosControls()
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

        _ALTITUDE_10000ft = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("ALT_10000FT_CNT");
        _ALTITUDE_1000ft = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("ALT_1000FT_CNT");
        _ALTITUDE_100ft = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("ALT_100FT_CNT");

        _HEADING = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("HDG_DEG_MAG");
        _IAS = DCSBIOSControlLocator.GetStringDCSBIOSOutput("IAS_US");
        _VS = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("VVI");

        _ALT_PRESSURE0 = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("ALT_PRESSURE0");
        _ALT_PRESSURE1 = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("ALT_PRESSURE1");
        _ALT_PRESSURE2 = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("ALT_PRESSURE2");
        _ALT_PRESSURE3 = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("ALT_PRESSURE3");
    }

    public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
    {
        try
        {
            bool refresh = false;
            bool refresh_frontpanel = false;
            UpdateCounter(e.Address, e.Data);

            if (mcdu != null && !options.DisableLightingManagement)
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
                        mcdu.DisplayBrightnessPercent = Math.Min(100, mcdu.DisplayBrightnessPercent - BRT_STEP);
                    else if (val == 2)
                        mcdu.DisplayBrightnessPercent = Math.Min(100, mcdu.DisplayBrightnessPercent + BRT_STEP);
                    // Always refresh Brightness. 
                    refresh = true;
                }
            }
            
            if (frontpanelHub.HasFrontpanels && !options.DisableLightingManagement)
            {
                if (e.Address == _CONSOLE_BRT!.Address)
                {
                    var rawBrightness = _CONSOLE_BRT!.GetUIntValue(e.Data);
                    // Convert to byte range (0-255) directly, not percentage
                    var brightness = (byte)(rawBrightness * 255 / _CONSOLE_BRT.MaxValue);
                    frontpanelHub.SetBrightness(brightness, brightness, brightness);
                    refresh_frontpanel = true;
                }
            }

            if (mcdu != null)
            {
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
            }
            
            if (e.Address == _HEADING!.Address)
            {
                refresh_frontpanel = true;
                heading = (int) _HEADING!.GetUIntValue(e.Data);
            }

            if (frontpanelState != null)
            {
                if (e.Address == _VS!.Address)
                {
                    refresh_frontpanel = true;
                    var rawValue = (int)_VS!.GetUIntValue(e.Data);
                    verticalSpeed = ConvertVviToVerticalSpeed(rawValue);
                }

                if (e.Address == _ALT_PRESSURE0!.Address)
                {
                    pressureDigits[0] = ConvertDrumPositionToDigit(_ALT_PRESSURE0!.GetUIntValue(e.Data), _ALT_PRESSURE0!.MaxValue);
                    refresh_frontpanel = true;
                }
                if (e.Address == _ALT_PRESSURE1!.Address)
                {
                    pressureDigits[1] = ConvertDrumPositionToDigit(_ALT_PRESSURE1!.GetUIntValue(e.Data), _ALT_PRESSURE1!.MaxValue);
                    refresh_frontpanel = true;
                }
                if (e.Address == _ALT_PRESSURE2!.Address)
                {
                    pressureDigits[2] = ConvertDrumPositionToDigit(_ALT_PRESSURE2!.GetUIntValue(e.Data), _ALT_PRESSURE2!.MaxValue);
                    refresh_frontpanel = true;
                }
                if (e.Address == _ALT_PRESSURE3!.Address)
                {
                    pressureDigits[3] = ConvertDrumPositionToDigit(_ALT_PRESSURE3!.GetUIntValue(e.Data), _ALT_PRESSURE3!.MaxValue);
                    refresh_frontpanel = true;
                }

                if (e.Address == _ALTITUDE_10000ft!.Address)
                {
                    altitudeDigits[2] = ConvertDrumPositionToDigit(_ALTITUDE_10000ft!.GetUIntValue(e.Data), _ALTITUDE_10000ft!.MaxValue);
                    UpdateAltitude();
                    refresh_frontpanel = true;
                }
                if (e.Address == _ALTITUDE_1000ft!.Address)
                {
                    altitudeDigits[1] = ConvertDrumPositionToDigit(_ALTITUDE_1000ft!.GetUIntValue(e.Data), _ALTITUDE_1000ft!.MaxValue);
                    UpdateAltitude();
                    refresh_frontpanel = true;
                }
                if (e.Address == _ALTITUDE_100ft!.Address)
                {
                    altitudeDigits[0] = ConvertDrumPositionToAltitude100ft(_ALTITUDE_100ft!.GetUIntValue(e.Data), _ALTITUDE_100ft!.MaxValue);
                    UpdateAltitude();
                    refresh_frontpanel = true;
                }
                
                if (refresh_frontpanel)
                {
                    frontpanelState.Speed = speed;
                    frontpanelState.Heading = heading;
                    frontpanelState.Altitude = altitude;
                    frontpanelState.VerticalSpeed = verticalSpeed;

                    if (frontpanelState is FcuEfisState fcuState)
                    {
                        UpdateBaroPressure();
                        fcuState.LeftBaroPressure = baroPressure;
                    }
                }
            }

            if (refresh && mcdu != null)
            {
                if (!options.DisableLightingManagement) mcdu.RefreshBrightnesses();
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
        var output = GetCompositor(DEFAULT_PAGE);

        try
        {

            string data = e.StringData
                .Replace("»", ">")
                .Replace("«", "<")
                .Replace("¡", "}")
                .Replace("®", "{")
                .Replace("©", "^")
                .Replace("±", "_")
                .Replace("?", "%");

            output.Green();

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
                    output.Line(lineIndex).WriteLine(data);
                }
            }

            if (options.DisplayCMS)
            {
                output.Line(options.DisplayBottomAligned ? 2 : 11).Amber().WriteLine("------------------------");
            }

            if (e.Address == _IAS!.Address)
            {
                // there's a bug? in DCS-BIOS A-10C module where IAS is 2 knots below the actual value
                var trimmedSpeed = e.StringData.Trim();
                speed = trimmedSpeed == "" ? 0 : int.Parse(trimmedSpeed)+2;

                if (frontpanelState != null)
                {
                    frontpanelState.Speed = speed;
                }
            }

        }
        catch (Exception ex)
        {
            App.Logger.Error(ex, "Failed to process DCS-BIOS string data");
        }
    }
    
    private void UpdateBaroPressure()
    {
        // Combine the four digits into inHg format (e.g., 3000 for 30.00 inHg)
        // The FCU expects values >= 2000 for inHg mode (representing 20.00-32.00)
        baroPressure = pressureDigits[3] * 1000 + 
                       pressureDigits[2] * 100 + 
                       pressureDigits[1] * 10 + 
                       pressureDigits[0];
    }
    
    private void UpdateAltitude()
    {
        // Combine altitude components
        // altitudeDigits[0] now contains the precise 100s value (0-999)
        // altitudeDigits[1] and [2] contain single digits (0-9)
        altitude = altitudeDigits[2] * 10000 + 
                   altitudeDigits[1] * 1000 + 
                   altitudeDigits[0];
    }
    
    private int ConvertDrumPositionToDigit(uint position, int maxValue)
    {
        // A-10C altimeter uses rotating drum displays
        // Each drum rotates through digits 0-9 as the value changes
        // The position value (0-maxValue) represents the angular position of the drum
        // We need to map this to which digit (0-9) is currently visible
        
        // Calculate percentage of full rotation (0.0 to 1.0)
        double rotationPercentage = (double)position / maxValue;
        
        // Each digit occupies 10% of the rotation (0-9 = 10 digits)
        // Use rounding to get the closest digit
        int digit = (int)Math.Round(rotationPercentage * 10) % 10;
        
        return digit;
    }
    
    private int ConvertDrumPositionToAltitude100ft(uint position, int maxValue)
    {
        // The 100ft drum provides continuous position data
        // We can use the fractional position to get 20ft precision
        // Position 0-maxValue maps to 0-1000 feet (full rotation shows 0,1,2...9 then back to 0)
        double rotationPercentage = (double)position / maxValue;
        
        // Convert to altitude: 0.0 = 0ft, 1.0 = 1000ft
        // But we only want the hundreds portion (0-900)
        int altitude100 = (int)(rotationPercentage * 1000) % 1000;
        
        return altitude100;
    }
    
    private int ConvertVviToVerticalSpeed(int rawValue)
    {
        
        float percent = (float)(100.0 * rawValue / 65536);

        int verticalSpeed = (int)(0.0209 * Math.Pow(percent, 3) - 3.1435 * Math.Pow(percent, 2) + 228.09 * percent - 6161.6);
        
        return Math.Clamp(verticalSpeed, -6000, 6000);
    }
}
