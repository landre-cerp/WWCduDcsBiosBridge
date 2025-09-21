﻿using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;

namespace WWCduDcsBiosBridge
{
    internal class F15E_Listener : AircraftListener
    {
        private DCSBIOSOutput? F_UFC_LINE1_DISPLAY;
        private DCSBIOSOutput? F_UFC_LINE2_DISPLAY;
        private DCSBIOSOutput? F_UFC_LINE3_DISPLAY;
        private DCSBIOSOutput? F_UFC_LINE4_DISPLAY;
        private DCSBIOSOutput? F_UFC_LINE5_DISPLAY;
        private DCSBIOSOutput? F_UFC_LINE6_DISPLAY;

        protected override string GetFontFile() => "resources/a10c-font-21x31.json";
        protected override string GetAircraftName() => "F-15E";

        const int _AircraftNumber = 44;

        public F15E_Listener(ICdu mcdu, UserOptions options) : base(mcdu, _AircraftNumber, options)
        {
        }

        protected override void initBiosControls()
        {
            F_UFC_LINE1_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("F_UFC_LINE1_DISPLAY");
            F_UFC_LINE2_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("F_UFC_LINE2_DISPLAY");
            F_UFC_LINE3_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("F_UFC_LINE3_DISPLAY");
            F_UFC_LINE4_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("F_UFC_LINE4_DISPLAY");
            F_UFC_LINE5_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("F_UFC_LINE5_DISPLAY");
            F_UFC_LINE6_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("F_UFC_LINE6_DISPLAY");
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
            var output = new Compositor(pages[DEFAULT_PAGE]);
            try
            {
                UpdateLine(output.Line(2).White(), F_UFC_LINE1_DISPLAY, e);
                UpdateLine(output.Line(4).Red(), F_UFC_LINE2_DISPLAY, e);
                UpdateLine(output.Line(6).Red(), F_UFC_LINE3_DISPLAY, e);
                UpdateLine(output.Line(8).Red(), F_UFC_LINE4_DISPLAY, e);
                UpdateLine(output.Line(10).White(), F_UFC_LINE5_DISPLAY, e);
                UpdateLine(output.Line(12).White(), F_UFC_LINE6_DISPLAY, e);
            }
            catch (Exception)
            {
            }
        }

        private void UpdateLine(Compositor display, DCSBIOSOutput? output, DCSBIOSStringDataEventArgs e)
        {
            if (output == null || e.Address != output.Address) return;
            string data = e.StringData;
            display.Centered(data);
        }
    }
}
