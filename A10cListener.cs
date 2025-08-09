using ClassLibraryCommon;
using DCS_BIOS;
using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Interfaces;
using DCS_BIOS.Serialized;
using McduDotNet;
using System.Timers;
using Timer = System.Timers.Timer;

namespace McduDcsBiosBridge
{
    internal class A10cListener : IDCSBIOSStringListener, IDcsBiosDataListener, IDisposable
    {

        private DCSBIOSOutput _CDU_LINE_0;
        private DCSBIOSOutput _CDU_LINE_1;
        private DCSBIOSOutput _CDU_LINE_2;
        private DCSBIOSOutput _CDU_LINE_3;
        private DCSBIOSOutput _CDU_LINE_4;
        private DCSBIOSOutput _CDU_LINE_5;
        private DCSBIOSOutput _CDU_LINE_6;
        private DCSBIOSOutput _CDU_LINE_7;
        private DCSBIOSOutput _CDU_LINE_8;
        private DCSBIOSOutput _CDU_LINE_9;

        private DCSBIOSOutput _CDU_BRT; 
        private DCSBIOSOutput _MASTER_CAUTION; 

        private DCSBIOSOutput _CONSOLE_BRT; 
        private DCSBIOSOutput _NOSE_SW_GREENLIGHT;
        private DCSBIOSOutput _CANOPY_LED; 
        private DCSBIOSOutput _GUN_READY;

        private static double _TICK_DISPLAY = 200;
        private readonly Timer _DisplayCDUTimer = new(_TICK_DISPLAY);

        private bool _disposed;

        private readonly DCSBIOSOutput _UpdateCounterDCSBIOSOutput;
        private static readonly object _UpdateCounterLockObject = new();
        private bool _HasSyncOnce;
        private uint _Count;



        private IMcdu mcdu;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public A10cListener(IMcdu mcdu) {
            
            this.mcdu = mcdu;

            initBiosControls();

            BIOSEventHandler.AttachStringListener(this);
            BIOSEventHandler.AttachDataListener(this);

            _DisplayCDUTimer.Elapsed += TimedDisplayBufferOnCDU;
            _DisplayCDUTimer.Start();

        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        ~A10cListener()
        {
            Dispose();
        }


        private void initBiosControls()
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

        }

        private void TimedDisplayBufferOnCDU(object sender, ElapsedEventArgs e)
        {
            mcdu.RefreshDisplay();
        }


        public void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {
            try
            {
                bool refresh = false;
                UpdateCounter(e.Address, e.Data);

                if (e.Address == _CONSOLE_BRT.Address)
                {
                    mcdu.BacklightBrightnessPercent =
                        (int)(_CONSOLE_BRT.GetUIntValue(e.Data) * 100 / _CONSOLE_BRT.MaxValue);
                    refresh = true;
                }
                if (e.Address == _CANOPY_LED.Address)
                {
                    mcdu.Leds.Fm2 = _CANOPY_LED.GetUIntValue(e.Data) == 1;
                    refresh = true;
                }
                if (e.Address == _CDU_BRT.Address)
                {
                    int val = (int)_CDU_BRT.GetUIntValue(e.Data);
                    if (val == 0)
                        mcdu.DisplayBrightnessPercent = Math.Min(100, mcdu.DisplayBrightnessPercent - 5);
                    else if (val == 2)
                        mcdu.DisplayBrightnessPercent = Math.Min(100, mcdu.DisplayBrightnessPercent + 5);
                    // Always refresh Brightness. 
                    refresh = true;
                }
                if (e.Address == _NOSE_SW_GREENLIGHT.Address)
                {
                    mcdu.Leds.Ind = _NOSE_SW_GREENLIGHT.GetUIntValue(e.Data) == 1;
                    refresh = true;
                }
                if (e.Address == _GUN_READY.Address)
                {
                    mcdu.Leds.Fm1 = _GUN_READY.GetUIntValue(e.Data) == 1;
                    refresh = true;
                }
                if (e.Address == _MASTER_CAUTION.Address)
                {
                    mcdu.Leds.Fail = _MASTER_CAUTION.GetUIntValue(e.Data) == 1;
                    refresh = true;
                }

                if (refresh)
                {
                    mcdu.RefreshBrightnesses();
                    mcdu.RefreshLeds();
                }
            }
            catch
            {
                // Optionnel : log l'erreur
            }
        }


        public void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
        {
            try
            {

                //.WriteLine("  0123456789ABCDEF UDLR")
                //.WriteLine(" 2<grey> !\"#$%&'()*+,-./ ↑↓←→")
                //.WriteLine("<yellow>><white>3<grey>0123456789:;<=>? ▲▼◀▶<yellow><")
                //.WriteLine(" 4<grey>@ABCDEFGHIJKLMNO ☐°Δ⬡")
                //.WriteLine("<yellow>><white>5<grey>PQRSTUVWXYZ[\\]^_ █□■ <yellow><")
                //.WriteLine(" 6<grey>`abcdefghijklmno")
                //.WriteLine("<yellow>><white>7<grey>pqrstuvwxyz{|}~      <yellow><")
                //.Newline()
                //.Small()
                //.WriteLine("<large><yellow>><white><small>2<grey> !\"#$%&'()*+,-./ ↑↓←→<large><yellow><")
                //.WriteLine(" 3<grey>0123456789:;<=>? ▲▼◀▶")
                //.WriteLine("<large><yellow>><white><small>4<grey>@ABCDEFGHIJKLMNO ☐°Δ⬡<large><yellow><")
                //.WriteLine(" 5<grey>PQRSTUVWXYZ[\\]^_ █□■")
                //.WriteLine("<large><yellow>><white><small>6<grey>`abcdefghijklmno      <large><yellow><")
                //.WriteLine(" 7<grey>pqrstuvwxyz{|}~")


                string data = e.StringData
                    .Replace("»", "→")
                    .Replace("«", "←")
                    .Replace("¡", "☐")
                    .Replace("®", "Δ")
                    .Replace("©", "^")
                    .Replace("±", "_")
                    .Replace("?", "%");

                mcdu.Output.Green();

                var lineMap = new Dictionary<uint, int>
                    {
                        { _CDU_LINE_0.Address, 0 },
                        { _CDU_LINE_1.Address, 1 },
                        { _CDU_LINE_2.Address, 2 },
                        { _CDU_LINE_3.Address, 3 },
                        { _CDU_LINE_4.Address, 4 },
                        { _CDU_LINE_5.Address, 5 },
                        { _CDU_LINE_6.Address, 6 },
                        { _CDU_LINE_7.Address, 7 },
                        { _CDU_LINE_8.Address, 8 },
                        { _CDU_LINE_9.Address, 9 }
                    };

                if (lineMap.TryGetValue(e.Address, out int lineIndex))
                {
                    mcdu.Output.Line(lineIndex).WriteLine(data);
                }
            }
            catch
            {
                // Optionnel : log l'erreur
            }
        }


        public void Dispose()
        {
            if (_disposed) return;
            _DisplayCDUTimer.Stop();
            mcdu.Output.Clear();
            mcdu.RefreshDisplay();
            mcdu.Cleanup();
            _disposed = true;
            GC.SuppressFinalize(this); // évite que le finalizer soit appelé

        }

        protected void UpdateCounter(uint address, uint data)
        {
            lock (_UpdateCounterLockObject)
            {
                if (_UpdateCounterDCSBIOSOutput != null && _UpdateCounterDCSBIOSOutput.Address == address)
                {
                    var newCount = _UpdateCounterDCSBIOSOutput.GetUIntValue(data);
                    if (!_HasSyncOnce)
                    {
                        _Count = newCount;
                        _HasSyncOnce = true;
                        return;
                    }

                    // Max is 255
                    if (newCount == 0 && _Count == 255 || newCount - _Count == 1)
                    {
                        // All is well
                        _Count = newCount;
                    }
                    else if (newCount - _Count != 1)
                    {
                        // Not good
                        _Count = newCount;
                    }
                }
            }
        }

    }

}
