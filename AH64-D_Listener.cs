using ClassLibraryCommon;
using DCS_BIOS;
using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Interfaces;
using DCS_BIOS.Serialized;
using McduDotNet;
using System.Timers;
using Timer = System.Timers.Timer;

namespace WWCduDcsBiosBridge
{
    internal class AH64D_Listener : AircraftListener
    {

        private DCSBIOSOutput _PLT_KU_DISPLAY;

        private DCSBIOSOutput _PLT_EUFD_LINE1;
        private DCSBIOSOutput _PLT_EUFD_LINE2;
        private DCSBIOSOutput _PLT_EUFD_LINE3;
        private DCSBIOSOutput _PLT_EUFD_LINE4;
        private DCSBIOSOutput _PLT_EUFD_LINE5;

        private DCSBIOSOutput _PLT_EUFD_LINE8;
        private DCSBIOSOutput _PLT_EUFD_LINE9;
        private DCSBIOSOutput _PLT_EUFD_LINE10;
        private DCSBIOSOutput _PLT_EUFD_LINE11;
        private DCSBIOSOutput _PLT_EUFD_LINE12;
        private DCSBIOSOutput _PLT_EUFD_LINE14;

        private DCSBIOSOutput _PLT_EUFD_BRT;

        // Lights

        private DCSBIOSOutput _PLT_MASTER_WARNING_L;


        protected override string GetFontFile() => "resources/ah64d-font-21x31.json";
        protected override string GetAircraftName() => "AH-64D";
        const int _AircraftNumber = 46;



#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public AH64D_Listener(ICdu mcdu, bool bottomAligned) : base(mcdu, _AircraftNumber, bottomAligned) {

        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        ~AH64D_Listener()
        {
            Dispose(false);
        }


        protected override void initBiosControls()
        {
            // PLT Keyboard display

            _PLT_KU_DISPLAY = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_KU_DISPLAY");

            _PLT_EUFD_BRT = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("PLT_EUFD_BRT");

            // UFD Upper status 

            _PLT_EUFD_LINE1 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE1");
            _PLT_EUFD_LINE2 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE2");
            _PLT_EUFD_LINE3 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE3");
            _PLT_EUFD_LINE4 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE4");
            _PLT_EUFD_LINE5 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE5");

            // UFD Frequency
            _PLT_EUFD_LINE8 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE8");

            _PLT_EUFD_LINE9 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE9");

            _PLT_EUFD_LINE10 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE10");

            _PLT_EUFD_LINE11 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE11");

            _PLT_EUFD_LINE12 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE12");

            _PLT_EUFD_LINE14 = DCSBIOSControlLocator.GetStringDCSBIOSOutput("PLT_EUFD_LINE14");

            _PLT_MASTER_WARNING_L = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("PLT_MASTER_WARNING_L");

        }


        public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {

            try
            {
                bool shouldUpdate;
                uint newValue;

                UpdateCounter(e.Address, e.Data);

                (shouldUpdate, newValue) = ShouldHandleDCSBiosData(e, _PLT_EUFD_BRT);

                if (shouldUpdate)
                {
                    int eufdBright = (int)newValue;
                    // MAX_BRIGHT is 256 , so 655356 / 256 is 256 , we need to divide by 2^8
                    eufdBright = eufdBright = 100 * eufdBright / 256;
                    mcdu.BacklightBrightnessPercent = eufdBright;
                    mcdu.DisplayBrightnessPercent = eufdBright;
                    mcdu.LedBrightnessPercent = eufdBright;
                    mcdu.RefreshLeds();


                }

                // AH - 64D / PLT_MASTER_WARNING_L
                (shouldUpdate, newValue) = ShouldHandleDCSBiosData(e, _PLT_MASTER_WARNING_L);
                if (shouldUpdate)
                {
                    if (newValue == 1)
                    {
                        mcdu.Leds.Fail = true;

                    }
                    else
                    {
                        mcdu.Leds.Fail = false;
                    }
                    mcdu.RefreshLeds();

                }
            }
            catch (Exception)
            {
            }

        }


        public override void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
        {
            try
            {

                string data = e.StringData
                    .Replace("~", "█")
                    .Replace(">", "▶")
                    .Replace("<", "◀")
                    .Replace("=", "■");

                mcdu.Output.Green();


                string incomingData;

                if (e.Address.Equals(_PLT_EUFD_LINE14.Address))
                {
                    var time = data.Substring(46, 10);
                    var fuel =data.Substring(0, 10);
                    mcdu.Output.Line(0).ClearRow().WriteLine($"{fuel}    {time}");


                }

                if (e.Address.Equals(_PLT_EUFD_LINE1.Address))
                {
                    incomingData = data.Substring(38, 17);
                    mcdu.Output.Line(1).WriteLine(incomingData);

                }

                if (e.Address.Equals(_PLT_EUFD_LINE2.Address))
                {
                    incomingData = data.Substring(38, 17);
                    mcdu.Output.Line(2).WriteLine(incomingData);
                }

                if (e.Address.Equals(_PLT_EUFD_LINE3.Address))
                {
                    incomingData = data.Substring(38, 17);
                    mcdu.Output.Line(3).WriteLine(incomingData);
                }

                if (e.Address.Equals(_PLT_EUFD_LINE4.Address))
                {
                    incomingData = data.Substring(38, 17);
                    mcdu.Output.Line(4).WriteLine(incomingData);
                }

                if (e.Address.Equals(_PLT_EUFD_LINE5.Address))
                {
                    incomingData = data.Substring(38, 17);
                    mcdu.Output.Line(5).WriteLine(incomingData);
                }

                
                mcdu.Output.Line(6).ClearRow();


                //// Radios Frequencies

                if (e.Address.Equals(_PLT_EUFD_LINE8.Address))
                {
                    incomingData = data.Substring(0, 18);
                    mcdu.Output.Line(7).WriteLine(incomingData);
                }
                if (e.Address.Equals(_PLT_EUFD_LINE9.Address))
                {
                    incomingData = data.Substring(0, 18);
                    mcdu.Output.Line(8).WriteLine(incomingData);
                    
                }
                if (e.Address.Equals(_PLT_EUFD_LINE10.Address))
                {
                    incomingData = data.Substring(0, 18);
                    mcdu.Output.Line(9).WriteLine(incomingData);

                }
                if (e.Address.Equals(_PLT_EUFD_LINE11.Address))
                {
                    incomingData = data.Substring(0, 18);
                    mcdu.Output.Line(10).WriteLine(incomingData);

                }
                if (e.Address.Equals(_PLT_EUFD_LINE12.Address))
                {
                    incomingData = data.Substring(0, 18);
                    mcdu.Output.Line(11).WriteLine(incomingData);
                }

                mcdu.Output.Line(12).Amber().WriteLine("- Keyboard -------------");

                if (e.Address.Equals(_PLT_KU_DISPLAY.Address))
                {
                    incomingData = data;
                    mcdu.Output.Line(13).Green().WriteLine(incomingData);

                }
            }


            catch
            {
                // Optionnel : log l'erreur
            }
        }

    }

}
