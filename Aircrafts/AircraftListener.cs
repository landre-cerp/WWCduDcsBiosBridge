using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using Newtonsoft.Json;
using System.IO;
using Timer = System.Timers.Timer;
using WwDevicesDotNet;
using WwDevicesDotNet.WinWing.FcuAndEfis;
using WwDevicesDotNet.WinWing.Pap3;
using WWCduDcsBiosBridge.Frontpanels;

namespace WWCduDcsBiosBridge.Aircrafts;

internal abstract class AircraftListener : IDcsBiosListener, IDisposable
{
    private static double _TICK_DISPLAY = 100;
    private readonly Timer _DisplayCDUTimer;
    protected ICdu? mcdu;
    protected FrontpanelHub frontpanelHub;
    protected IFrontpanelState? frontpanelState;
    protected IFrontpanelLeds? frontpanelLeds;

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

    public AircraftListener(ICdu? mcdu, int aircraftNumber, UserOptions options, FrontpanelHub frontpanelHub)
    {
        this.mcdu = mcdu;
        this.frontpanelHub = frontpanelHub ?? throw new ArgumentNullException(nameof(frontpanelHub));
        this.options = options;
        DCSBIOSControlLocator.DCSAircraft = DCSAircraft.GetAircraft(aircraftNumber);
        _UpdateCounterDCSBIOSOutput = DCSBIOSOutput.GetUpdateCounter();

        _DisplayCDUTimer = new(_TICK_DISPLAY);
        _DisplayCDUTimer.Elapsed += (_, _) =>
        {
            if (this.mcdu != null)
            {
                this.mcdu.Screen.CopyFrom(pages[_currentPage]);
                this.mcdu.RefreshDisplay();
            }

            if (frontpanelHub.HasFrontpanels && frontpanelState != null)
            {
                frontpanelHub.UpdateDisplay(frontpanelState);
            }

            if (frontpanelHub.HasFrontpanels && frontpanelLeds != null)
            {
                frontpanelHub.UpdateLeds(frontpanelLeds);
            }
        };

        // Initialize frontpanel state and LEDs based on first adapter
        // All frontpanels will receive the same updates
        if (frontpanelHub.HasFrontpanels)
        {
            var firstAdapter = frontpanelHub.Adapters.First();
            if (firstAdapter is FcuEfisAdapter)
            {
                frontpanelState = new FcuEfisState();
                frontpanelLeds = new FcuEfisLeds();
                InitializeFrontpanelBrightness(128, 255, 255);
                App.Logger.Info("FCU/EFIS device detected and initialized");
            }
            else if (firstAdapter is Pap3Adapter)
            {
                frontpanelState = new Pap3State();
                frontpanelLeds = new Pap3Leds();
                InitializeFrontpanelBrightness(128, 255, 255);
                App.Logger.Info("PAP3 device detected and initialized");
            }
            else
            {
                App.Logger.Warn($"Unknown frontpanel adapter type: {firstAdapter.GetType().Name}");
            }
        }
        else
        {
            App.Logger.Info("No frontpanel devices connected");
        }
    }

    public void Start()
    {
        InitializeDcsBiosControls();
        

        if (mcdu != null)
        {
            InitMcduBrightness(options.DisableLightingManagement);
        }

        BIOSEventHandler.AttachStringListener(this);
        BIOSEventHandler.AttachDataListener(this);
        BIOSEventHandler.AttachConnectionListener(this);

        _DisplayCDUTimer.Start();

        if (mcdu != null)
        {
            ShowStartupMessage();
        }
    }

    private void InitMcduBrightness(bool disabledBrightness)
    {
        if (disabledBrightness || mcdu == null) return;
        mcdu.BacklightBrightnessPercent = 100;
        mcdu.LedBrightnessPercent = 100;
        mcdu.DisplayBrightnessPercent = 100;
    }

    private void InitializeFrontpanelBrightness(byte panelBacklight, byte lcdBacklight, byte ledBacklight)
    {
        if (options.DisableLightingManagement || !frontpanelHub.HasFrontpanels) return;
        frontpanelHub.SetBrightness(panelBacklight, lcdBacklight, ledBacklight);
    }

    public void Stop()
    {
        _DisplayCDUTimer.Stop();

        BIOSEventHandler.DetachConnectionListener(this);
        BIOSEventHandler.DetachDataListener(this);
        BIOSEventHandler.DetachStringListener(this);

        if (mcdu != null)
        {
            mcdu.Output.Clear();
            mcdu.Cleanup();
            mcdu.RefreshDisplay();
        }
    }

    protected abstract string GetFontFile();
    protected abstract string GetAircraftName();

    private void ShowStartupMessage()
    {
        if (mcdu == null) return;

        var output = GetCompositor(DEFAULT_PAGE);
        output.Clear()
            .Green()
            .Line(6).Centered("INITIALIZING...")
            .Line(7).Centered(GetAircraftName());
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

    protected Screen AddNewPage(string pageName)
    {
        if (!pages.ContainsKey(pageName))
        {
            pages[pageName] = new Screen();
        }

        return pages[pageName];
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
            _DisplayCDUTimer.Dispose();
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

                if (newCount == 0 && _Count == 255 || newCount - _Count == 1)
                {
                    _Count = newCount;
                }
                else if (newCount - _Count != 1)
                {
                    _Count = newCount;
                    Console.WriteLine($"UpdateCounter: Address {address} has unexpected value {data}. Expected {_Count + 1}.");
                }
            }
        }
    }
}
