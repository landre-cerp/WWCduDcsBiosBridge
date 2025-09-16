using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;

namespace WWCduDcsBiosBridge
{
    internal class A10C_Listener : AircraftListener
    {

        private DCSBIOSOutput? _CDU_LINE_0;
        private DCSBIOSOutput? _CDU_LINE_1;
        private DCSBIOSOutput? _CDU_LINE_2;
        private DCSBIOSOutput? _CDU_LINE_3;
        private DCSBIOSOutput? _CDU_LINE_4;
        private DCSBIOSOutput? _CDU_LINE_5;
        private DCSBIOSOutput? _CDU_LINE_6;
        private DCSBIOSOutput? _CDU_LINE_7;
        private DCSBIOSOutput? _CDU_LINE_8;
        private DCSBIOSOutput? _CDU_LINE_9;

        private DCSBIOSOutput? _CDU_BRT; 
        private DCSBIOSOutput? _MASTER_CAUTION; 

        private DCSBIOSOutput? _CONSOLE_BRT; 
        private DCSBIOSOutput? _NOSE_SW_GREENLIGHT;
        private DCSBIOSOutput? _CANOPY_LED; 
        private DCSBIOSOutput? _GUN_READY;

        private DCSBIOSOutput? _CMSP1;
        private DCSBIOSOutput? _CMSP2;

        protected override string GetAircraftName() => "A-10C";
        protected override string GetFontFile() => "resources/a10c-font-21x31.json";
        const int _AircraftNumber = 5;

        const string TAKEOFF_PAGE = "Takeoff";
        const string LANDING_PAGE= "Landing";

        public A10C_Listener(
            ICdu mcdu, 
            UserOptions options) : base(mcdu, _AircraftNumber, options) 
        {
            pages.Add(TAKEOFF_PAGE, new Screen());
            pages.Add(LANDING_PAGE, new Screen());

            var takeoff = new Compositor(pages[TAKEOFF_PAGE]);
            takeoff.Line(0).White().Centered("TAKEOFF PAGE");

            var landing = new Compositor(pages[LANDING_PAGE]);
            landing.Line(0).Yellow().Centered("LANDING PAGE");

            mcdu.KeyDown += KeyDown;

        }


        ~A10C_Listener()
        {
            mcdu.KeyDown -= KeyDown;
            Dispose(false);
        }

        private void KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is Key.Fix)
            {
                _currentPage = TAKEOFF_PAGE;
            }
            if (e.Key is Key.Legs)
            {
                _currentPage = LANDING_PAGE;
            }
            if (e.Key is Key.InitRef or Key.Rte or Key.DepArr or Key.Altn or Key.VNav)
            {
                _currentPage = DEFAULT_PAGE;
            }
        }

        protected override void initBiosControls()
        {
            _CDU_LINE_0 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE0");
            _CDU_LINE_1 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE1");
            _CDU_LINE_2 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE2");
            _CDU_LINE_3 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE3");
            _CDU_LINE_4 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE4");
            _CDU_LINE_5 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE5");
            _CDU_LINE_6 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE6");
            _CDU_LINE_7 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE7");
            _CDU_LINE_8 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE8");
            _CDU_LINE_9 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("CDU_LINE9");

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
            catch
            {
                // Optionnel : log error
            }
        }

        public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
        {
            
            var output = new Compositor(pages[DEFAULT_PAGE]);

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

                output.Green();

                Dictionary<uint,int> lineMap; 

                if (options.DisplayBottomAligned)
                {
                    lineMap = new Dictionary<uint, int>
                    {
                        { _CMSP1!.Address, 0 },
                        { _CMSP2!.Address, 1 },
                        { _CDU_LINE_0!.Address, 4 },
                        { _CDU_LINE_1!.Address, 5 },
                        { _CDU_LINE_2!.Address, 6 },
                        { _CDU_LINE_3!.Address, 7 },
                        { _CDU_LINE_4!.Address, 8 },
                        { _CDU_LINE_5!.Address, 9 },
                        { _CDU_LINE_6!.Address, 10 },
                        { _CDU_LINE_7!.Address, 11 },
                        { _CDU_LINE_8!.Address, 12 },
                        { _CDU_LINE_9!.Address, 13 },
                    };
                }
                else
                {
                    lineMap = new Dictionary<uint, int>
                    {
                        { _CDU_LINE_0!.Address, 0},
                        { _CDU_LINE_1!.Address, 1 },
                        { _CDU_LINE_2!.Address, 2},
                        { _CDU_LINE_3!.Address, 3 },
                        { _CDU_LINE_4!.Address, 4 },
                        { _CDU_LINE_5!.Address, 5 },
                        { _CDU_LINE_6!.Address, 6 },
                        { _CDU_LINE_7!.Address, 7 },
                        { _CDU_LINE_8!.Address, 8 },
                        { _CDU_LINE_9!.Address, 9 },
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

            }
            catch
            {
                // Optionnel : log error
            }
        }
    }

}
