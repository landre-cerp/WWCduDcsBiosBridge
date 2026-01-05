using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using WwDevicesDotNet;
using WwDevicesDotNet.WinWing.FcuAndEfis;
using WwDevicesDotNet.WinWing.Pap3;
using Newtonsoft.Json;
using System.IO;
using Timer = System.Timers.Timer;

namespace WWCduDcsBiosBridge.Aircrafts;

internal abstract class AircraftListener : IDcsBiosListener, IDisposable
{
    private static double _TICK_DISPLAY = 100;
    private readonly Timer _DisplayCDUTimer;
    protected ICdu mcdu;
    protected IFrontpanel? frontpanel;
    private FcuEfisDevice? _fcuEfisDevice;
    private Pap3Device? _pap3Device;
    protected FcuEfisState? _fcuEfisState;
    protected FcuEfisLeds? _fcuEfisLeds;
    protected Pap3State? _pap3State;
    protected Pap3Leds? _pap3Leds;

    private bool _disposed;

    private readonly DCSBIOSOutput _UpdateCounterDCSBIOSOutput;
    private static readonly object _UpdateCounterLockObject = new();
    private bool _HasSyncOnce;
    private uint _Count;

    protected readonly UserOptions options;

    protected const string DEFAULT_PAGE = "default";
    protected string _currentPage = DEFAULT_PAGE;

    protected Dictionary<string, Screen> pages = new()
        {
              {DEFAULT_PAGE, new Screen() }
        };

    public AircraftListener(ICdu mcdu, int aircraftNumber, UserOptions options, IFrontpanel? frontpanel = null)
    {
        this.mcdu = mcdu;
        this.frontpanel = frontpanel;
        this.options = options;
        DCSBIOSControlLocator.DCSAircraft = DCSAircraft.GetAircraft(aircraftNumber);
        _UpdateCounterDCSBIOSOutput = DCSBIOSOutput.GetUpdateCounter();

        _DisplayCDUTimer = new(_TICK_DISPLAY);
        _DisplayCDUTimer.Elapsed += (_, _) =>
        {
            mcdu.Screen.CopyFrom(pages[_currentPage]);
            mcdu.RefreshDisplay();
            
            // Update FCU/EFIS on the same timer if changes occurred
            if (_fcuEfisDevice != null)
            {
                if (_fcuEfisState != null)
                {
                    _fcuEfisDevice.UpdateDisplay(_fcuEfisState);
                }
                
                if (_fcuEfisLeds != null)
                {
                    _fcuEfisDevice.UpdateLeds(_fcuEfisLeds);
                }
            }
            
            // Update PAP3 on the same timer if changes occurred
            if (_pap3Device != null)
            {
                if (_pap3State != null)
                {
                    _pap3Device.UpdateDisplay(_pap3State);
                }
                
                if (_pap3Leds != null)
                {
                    _pap3Device.UpdateLeds(_pap3Leds);
                }
            }
        };
        
        // Cache FCU/EFIS device if frontpanel is an FCU
        _fcuEfisDevice = frontpanel as FcuEfisDevice;
        
        // Cache PAP3 device if frontpanel is a PAP3
        _pap3Device = frontpanel as Pap3Device;
            
        // Initialize reusable state objects if FCU/EFIS device is present
        if (_fcuEfisDevice != null)
        {
            _fcuEfisState = new FcuEfisState();
            _fcuEfisLeds = new FcuEfisLeds();
            InitializeFcuBrightness(options.DisableLightingManagement);
            App.Logger.Info("FCU/EFIS device detected and initialized");
        }
        
        // Initialize reusable state objects if PAP3 device is present
        if (_pap3Device != null)
        {
            _pap3State = new Pap3State();
            _pap3Leds = new Pap3Leds();
            InitializePap3Brightness(options.DisableLightingManagement);
            App.Logger.Info("PAP3 device detected and initialized");
        }
        
        if (frontpanel == null)
        {
            App.Logger.Info("No frontpanel device connected");
        }
    }

    public void Start()
    {
        InitializeDcsBiosControls();
        InitMcduFont();
        InitMcduBrightness(options.DisableLightingManagement);
        ShowStartupMessage();

        BIOSEventHandler.AttachStringListener(this);
        BIOSEventHandler.AttachDataListener(this);
        BIOSEventHandler.AttachConnectionListener(this);

        _DisplayCDUTimer.Start();
    }

    private void InitMcduBrightness(bool disabledBrightness)
    {
        if (disabledBrightness) return;
        mcdu.BacklightBrightnessPercent = 100;
        mcdu.LedBrightnessPercent = 100;
        mcdu.DisplayBrightnessPercent = 100;
    }

    private void InitializeFcuBrightness(bool disabledBrightness)
    {
        if (disabledBrightness || _fcuEfisDevice == null) return;
        _fcuEfisDevice.SetBrightness(128, 255, 255);
    }

    private void InitializePap3Brightness(bool disabledBrightness)
    {
        if (disabledBrightness || _pap3Device == null) return;
        _pap3Device.SetBrightness(128, 255, 255);
    }

    public void Stop()
    {
        _DisplayCDUTimer.Stop();

        BIOSEventHandler.DetachConnectionListener(this);
        BIOSEventHandler.DetachDataListener(this);
        BIOSEventHandler.DetachStringListener(this);
        
        mcdu.Output.Clear();
        mcdu.Cleanup();
        mcdu.RefreshDisplay();
        
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

    protected Screen AddNewPage(string pageName)
    {
        if (!pages.ContainsKey(pageName))
        {
            pages[pageName] = new Screen();
        }
        return pages[pageName];
    }

    private void ShowStartupMessage()
    {
        mcdu.Output.Clear().Green();
        mcdu.RefreshDisplay();
    }

    protected abstract void InitializeDcsBiosControls();

    public void DcsBiosConnectionActive(object sender, DCSBIOSConnectionEventArgs e)
    {
    }

    protected Compositor GetCompositor(string pageName)
    {
        if (!pages.ContainsKey(pageName))
        {
            pages[pageName] = new Screen();
        }
        return new Compositor(pages[pageName]);
    }

    /// <summary>
    /// Called when DCS-BIOS data is received
    /// Note that the same address may concern multiple controls
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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
            _DisplayCDUTimer.Dispose(); // Dispose the timer
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

}
