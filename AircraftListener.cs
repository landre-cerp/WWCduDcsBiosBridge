using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace WWCduDcsBiosBridge
{
    internal abstract class AircraftListener : IDcsBiosListener, IDisposable
    {
        private static double _TICK_DISPLAY = 100;
        private readonly Timer _DisplayCDUTimer;
        protected ICdu mcdu;
        protected readonly bool BottomAligned = false;
        protected readonly bool DisplayCMS = false;
        protected readonly int AircraftNumber;

        private bool _disposed;

        private readonly DCSBIOSOutput _UpdateCounterDCSBIOSOutput;
        private static readonly object _UpdateCounterLockObject = new();
        private bool _HasSyncOnce;
        private uint _Count;


        public AircraftListener(ICdu mcdu, int aircraftNumber, bool bottomAligned =false, bool displayCms = false  )
        {
            this.mcdu = mcdu;
            BottomAligned = bottomAligned;
            DisplayCMS = displayCms;
            AircraftNumber = aircraftNumber;
            DCSBIOSControlLocator.DCSAircraft = DCSAircraft.GetAircraft(AircraftNumber);
            _UpdateCounterDCSBIOSOutput = DCSBIOSOutput.GetUpdateCounter();

            _DisplayCDUTimer = new(_TICK_DISPLAY);
            _DisplayCDUTimer.Elapsed += (_, _) => mcdu.RefreshDisplay();


        }

        public void Start()
        {
            
            initBiosControls();

            InitMcduFont();
            ShowStartupMessage();

            BIOSEventHandler.AttachStringListener(this);
            BIOSEventHandler.AttachDataListener(this);
            BIOSEventHandler.AttachConnectionListener(this);


            _DisplayCDUTimer.Start();


        }

        public void Stop()
        {
            BIOSEventHandler.DetachConnectionListener(this);
            BIOSEventHandler.DetachDataListener(this);
            BIOSEventHandler.DetachStringListener(this);

            _DisplayCDUTimer.Stop();
            mcdu.Output.Clear();
            mcdu.RefreshDisplay();
            mcdu.Cleanup();

        }
        private void InitMcduFont()
        {
            var fontFile = GetFontFile();
            var json = File.ReadAllText(fontFile);
            var font = JsonConvert.DeserializeObject<McduFontFile>(json);
            mcdu.UseFont(font, true);
        }

        protected abstract string GetFontFile();
        protected abstract string GetAircraftName();
        
        private void ShowStartupMessage()
        {
            mcdu.Output.Clear().Green();
            mcdu.RefreshDisplay();
        }

        protected virtual void initBiosControls() { }

        public void DcsBiosConnectionActive(object sender, DCSBIOSConnectionEventArgs e)
        {
        }


        public abstract void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e);

        public abstract void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            
            if (disposing)
            {
                Stop();

            }

            _disposed = true;
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
                        Console.WriteLine($"UpdateCounter: Address {address} has unexpected value {data}. Expected {_Count + 1}.");
                    }
                }
            }
        }

        protected static (bool, uint) ShouldHandleDCSBiosData(DCSBIOSDataEventArgs e, DCSBIOSOutput output)
        {
            if (e.Address != output.Address) return (false, 0);
            var oldValue = output.LastUIntValue;
            var newValue = output.GetUIntValue(e.Data);
            if (oldValue == newValue) return (false, 0);

            return (true, output.GetUIntValue(e.Data));
        }


    }
}
