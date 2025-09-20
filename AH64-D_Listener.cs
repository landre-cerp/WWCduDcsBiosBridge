using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;

namespace WWCduDcsBiosBridge
{
    internal class AH64D_Listener : AircraftListener
    {
        // EUFD Display
        private DCSBIOSOutput? _PLT_EUFD_LINE1;
        private DCSBIOSOutput? _PLT_EUFD_LINE2;
        private DCSBIOSOutput? _PLT_EUFD_LINE3;
        private DCSBIOSOutput? _PLT_EUFD_LINE4;
        private DCSBIOSOutput? _PLT_EUFD_LINE5;

        private DCSBIOSOutput? _PLT_EUFD_LINE8;
        private DCSBIOSOutput? _PLT_EUFD_LINE9;
        private DCSBIOSOutput? _PLT_EUFD_LINE10;
        private DCSBIOSOutput? _PLT_EUFD_LINE11;
        private DCSBIOSOutput? _PLT_EUFD_LINE12;
        private DCSBIOSOutput? _PLT_EUFD_LINE14;

        // Keyboard display
        private DCSBIOSOutput? _PLT_KU_DISPLAY;

        // Brightness
        private DCSBIOSOutput? _PLT_EUFD_BRT;

        // Lights
        private DCSBIOSOutput? _PLT_MASTER_CAUTION_L;
        private DCSBIOSOutput? _PLT_MASTER_WARNING_L;

        private Dictionary<uint, Action<DCSBIOSDataEventArgs>>? _dataHandlers;

        protected override string GetFontFile() => "resources/ah64d-font-21x31.json";
        protected override string GetAircraftName() => "AH-64D";
        const int _AircraftNumber = 46;

        public AH64D_Listener(ICdu mcdu, UserOptions options) : base(mcdu, _AircraftNumber , options) {
        }

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

            // Note that they share the same Address but bit is different ! (10 and 11 ) 
            _PLT_MASTER_CAUTION_L = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("PLT_MASTER_CAUTION_L");
            _PLT_MASTER_WARNING_L = DCSBIOSControlLocator.GetUIntDCSBIOSOutput("PLT_MASTER_WARNING_L");

            _dataHandlers = new Dictionary<uint, Action<DCSBIOSDataEventArgs>>
            {
                { _PLT_EUFD_BRT!.Address, HandleEufdBrightness },
                // So we cannot Add 2 entries because they have the sameAddress ! 
                { _PLT_MASTER_CAUTION_L!.Address, HandleMasterWarning }
            };
        }

        private void HandleEufdBrightness(DCSBIOSDataEventArgs e)
        {
            if (options.DisableLightingManagement) return;

            int newValue = 0;

            if (ShouldHandleDCSBiosData(e, _PLT_EUFD_BRT!, out newValue))
            {
                int eufdBright = (int)newValue;
                eufdBright = 100 * eufdBright / 65536;
                mcdu.BacklightBrightnessPercent = eufdBright;
                mcdu.DisplayBrightnessPercent = eufdBright;
                mcdu.LedBrightnessPercent = eufdBright;
                mcdu.RefreshBrightnesses();
            }
        }

        private void HandleMasterWarning(DCSBIOSDataEventArgs e)
        {
            var newValue = 0;
            
            if (ShouldHandleDCSBiosData(e, _PLT_MASTER_CAUTION_L!, out newValue))
            {
                mcdu.Leds.Fail = (newValue == 1);
                mcdu.RefreshLeds();
            }

            if (ShouldHandleDCSBiosData(e, _PLT_MASTER_WARNING_L!, out newValue))
            {
                mcdu.Leds.Ind = (newValue == 1);
                mcdu.RefreshLeds();
            }

        }

        public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {

            try
            {
                UpdateCounter(e.Address, e.Data);
                if (_dataHandlers!.TryGetValue(e.Address, out var handler))
                {
                    handler(e);
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
                    .Replace("=", "■")
                    .Replace("#", "█");

                data = data.PadRight(60).Substring(0, 60); // Ensure string is exactly 60 characters long

                mcdu.Output.Green();

                var time = data.Substring(46, 10);
                var fuel = data.Substring(0, 10);

                UpdateLine(mcdu.Output.Line(0), _PLT_EUFD_LINE14!, e, $"{fuel}    {time}");

                var incomingData = data.Substring(38, 17);

                UpdateLine(mcdu.Output.Line(1), _PLT_EUFD_LINE1!, e, incomingData);
                UpdateLine(mcdu.Output.Line(2), _PLT_EUFD_LINE2!, e, incomingData);
                UpdateLine(mcdu.Output.Line(3), _PLT_EUFD_LINE3!, e, incomingData);
                UpdateLine(mcdu.Output.Line(4), _PLT_EUFD_LINE4!, e, incomingData);
                UpdateLine(mcdu.Output.Line(5), _PLT_EUFD_LINE5!, e, incomingData);
                
                mcdu.Output.Line(6).ClearRow();

                //// Radios Frequencies
                var radioData = data.Substring(0, 18);
                UpdateLine(mcdu.Output.Line(7), _PLT_EUFD_LINE8!, e, radioData);
                UpdateLine(mcdu.Output.Line(8), _PLT_EUFD_LINE9!, e, radioData);
                UpdateLine(mcdu.Output.Line(9), _PLT_EUFD_LINE10!, e, radioData);
                UpdateLine(mcdu.Output.Line(10), _PLT_EUFD_LINE11!, e, radioData);
                UpdateLine(mcdu.Output.Line(11), _PLT_EUFD_LINE12!, e, radioData);

                mcdu.Output.Line(12).Amber().WriteLine("- Keyboard -------------");

                UpdateLine(mcdu.Output.Line(13).Green(), _PLT_KU_DISPLAY!, e , data);
            }

            catch
            {
                // Optionnel : log l'erreur
            }
        }

        private void UpdateLine(Compositor display, DCSBIOSOutput? output, DCSBIOSStringDataEventArgs e, string data)
        {
            if (output == null || e.Address != output.Address) return;
            display.WriteLine(data);
        }

        protected bool ShouldHandleDCSBiosData(DCSBIOSDataEventArgs e, DCSBIOSOutput output, out int newValue)
        {
            if (e.Address != output.Address)
            {
                newValue = default;
                return false;
            }

            newValue = (int)output.GetUIntValue(e.Data);
            return true;
        }
    }
}
