using ClassLibraryCommon;
using DCS_BIOS.ControlLocator;
using DCS_BIOS.EventArgs;
using DCS_BIOS.Serialized;
using McduDotNet;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using Timer = System.Timers.Timer;

namespace WWCduDcsBiosBridge.Aircrafts;

internal abstract class AircraftListener : IDcsBiosListener, IDisposable
{
    private static double _TICK_DISPLAY = 100;
    private readonly Timer _DisplayCDUTimer;
    protected ICdu mcdu;

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

    public AircraftListener(ICdu mcdu, int aircraftNumber , UserOptions options)
    {
        this.mcdu = mcdu;
        this.options = options;
        DCSBIOSControlLocator.DCSAircraft = DCSAircraft.GetAircraft(aircraftNumber);
        _UpdateCounterDCSBIOSOutput = DCSBIOSOutput.GetUpdateCounter();

        _DisplayCDUTimer = new(_TICK_DISPLAY);
        _DisplayCDUTimer.Elapsed += (_, _) =>
        {
            mcdu.Screen.CopyFrom(pages[_currentPage]);
            mcdu.RefreshDisplay();
        };
    }

    public void Start()
    {
        InitializeDcsBiosControls();
        InitMcduFont();
        ShowStartupMessage();

        BIOSEventHandler.AttachStringListener(this);
        BIOSEventHandler.AttachDataListener(this);
        BIOSEventHandler.AttachConnectionListener(this);

        _DisplayCDUTimer.Start();
    }

    public void Stop()
    {
        _DisplayCDUTimer.Stop();

        BIOSEventHandler.DetachConnectionListener(this);
        BIOSEventHandler.DetachDataListener(this);
        BIOSEventHandler.DetachStringListener(this);
        
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
