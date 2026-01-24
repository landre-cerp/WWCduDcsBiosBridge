using DCS_BIOS;
using NLog;
using WWCduDcsBiosBridge.Config;
using WWCduDcsBiosBridge.Aircrafts;
using WWCduDcsBiosBridge.Frontpanels;

namespace WWCduDcsBiosBridge;

/// <summary>
/// Manages the DCS-BIOS bridge lifecycle
/// </summary>
public class BridgeManager : IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public bool IsStarted { get; private set; }
    internal List<DeviceContext>? Contexts { get; private set; }
    
    private DCSBIOS? dcsBios;
    private bool _disposed = false;
    private TaskCompletionSource<AircraftSelection>? _globalAircraftSelectionTcs;

    /// <summary>
    /// Sets the global aircraft selection (used when no CDU is present)
    /// </summary>
    public void SetGlobalAircraftSelection(AircraftSelection selection)
    {
        _globalAircraftSelectionTcs?.TrySetResult(selection);
    }

    /// <summary>
    /// Starts the bridge with the specified devices and configuration
    /// </summary>
    public async Task StartAsync(List<DeviceInfo> devices, UserOptions userOptions, DcsBiosConfig config)
    {
        if (IsStarted)
            throw new InvalidOperationException("Bridge is already started");

        if (devices == null || !devices.Any())
            throw new ArgumentException("No devices provided");

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        try
        {
            // Create device contexts for all devices
            Contexts = new List<DeviceContext>();
            
            foreach (var deviceInfo in devices)
            {
                DeviceContext ctx;
                if (deviceInfo.Cdu != null)
                {
                    ctx = new DeviceContext(deviceInfo.Cdu, userOptions ?? new UserOptions(), config);
                }
                else if (deviceInfo.Frontpanel != null)
                {
                    ctx = new DeviceContext(deviceInfo.Frontpanel, userOptions ?? new UserOptions(), config);
                }
                else
                {
                    Logger.Warn("Skipping device with no CDU or Frontpanel interface");
                    continue;
                }
                Contexts.Add(ctx);
            }

            if (!Contexts.Any())
            {
                throw new InvalidOperationException("No valid devices found.");
            }

            var cduCount = Contexts.Count(c => c.IsCduDevice);
            var frontpanelCount = Contexts.Count(c => c.IsFrontpanelDevice);
            
            Logger.Info($"Created contexts for {cduCount} CDU device(s) and {frontpanelCount} Frontpanel device(s)");

            // Show startup screens only on CDU devices
            foreach (var ctx in Contexts.Where(c => c.IsCduDevice))
                ctx.ShowStartupScreen();

            // Wait for aircraft selection - GLOBAL across all devices
            AircraftSelection? selectedAircraft = null;
            var cduContexts = Contexts.Where(c => c.IsCduDevice).ToList();

            if (cduContexts.Any())
            {
                // If there are CDU devices, wait for selection on ANY CDU (first one wins)
                Logger.Info("Waiting for aircraft selection on any CDU device...");
                
                // Use a more efficient polling strategy with exponential backoff
                int delayMs = 50;
                const int maxDelayMs = 500;
                while (!cduContexts.Any(c => c.IsSelectedAircraft))
                {
                    await Task.Delay(delayMs);
                    if (delayMs < maxDelayMs)
                        delayMs = Math.Min(delayMs * 2, maxDelayMs);
                }
                
                // First CDU to select wins - use that selection globally
                selectedAircraft = cduContexts.First(c => c.IsSelectedAircraft).SelectedAircraft;
                Logger.Info($"Aircraft selected on CDU: {selectedAircraft!.AircraftId}, IsPilot: {selectedAircraft.IsPilot}");
            }
            else
            {
                // No CDU devices - wait for global UI selection
                Logger.Info("No CDU devices found. Waiting for global aircraft selection from UI...");
                _globalAircraftSelectionTcs = new TaskCompletionSource<AircraftSelection>();
                selectedAircraft = await _globalAircraftSelectionTcs.Task;
                Logger.Info($"Global aircraft selection received from UI: {selectedAircraft.AircraftId}, IsPilot: {selectedAircraft.IsPilot}");
            }

            // Propagate global aircraft selection to ALL contexts (CDU and Frontpanel)
            foreach (var ctx in Contexts.Where(c => !c.IsSelectedAircraft))
            {
                ctx.SetAircraftSelection(selectedAircraft!);
            }

            // Initialize DCS-BIOS
            InitializeDcsBios(config);

            // Build the frontpanel hub from all frontpanel devices
            var frontpanelHub = BuildFrontpanelHub(Contexts);
            Logger.Info($"Frontpanel hub created with {frontpanelHub.Count} device(s)");

            // Start device bridges - pass hub to all contexts
            foreach (var ctx in Contexts)
                ctx.StartBridge(frontpanelHub);

            IsStarted = true;
            Logger.Info($"Bridge started successfully with {Contexts.Count} device(s) ({cduCount} CDU, {frontpanelCount} Frontpanel)");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start bridge");
            await StopAsync(); // Clean up on failure
            throw;
        }
    }

    /// <summary>
    /// Gets the number of active contexts
    /// </summary>
    public int ContextCount => Contexts?.Count ?? 0;

    /// <summary>
    /// Stops the bridge and cleans up resources
    /// </summary>
    public Task StopAsync()
    {
        try
        {
            dcsBios?.Shutdown();
            dcsBios = null;

            DisposeContexts();

            IsStarted = false;
            Logger.Info("Bridge stopped successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error occurred while stopping bridge");
            throw;
        }

        return Task.CompletedTask;
    }

    private void InitializeDcsBios(DcsBiosConfig config)
    {
        dcsBios = new DCSBIOS(config.ReceiveFromIpUdp, config.SendToIpUdp,
                             config.ReceivePortUdp, config.SendPortUdp,
                             DcsBiosNotificationMode.Parse);

        if (!dcsBios.HasLastException())
        {
            if (!dcsBios.IsRunning)
            {
                dcsBios.Startup();
            }
            Logger.Info("DCS-BIOS started successfully.");
        }
        else
        {
            var exception = dcsBios.GetLastException();
            Logger.Error(exception);
            throw exception;
        }
    }

    private FrontpanelHub BuildFrontpanelHub(List<DeviceContext> contexts)
    {
        var adapters = new List<IFrontpanelAdapter>();

        foreach (var ctx in contexts.Where(c => c.IsFrontpanelDevice && c.Frontpanel != null))
        {
            var frontpanel = ctx.Frontpanel!;
            var adapter = FrontpanelAdapterFactory.CreateAdapter(frontpanel, frontpanel.DeviceId.ToString());
            if (adapter != null)
            {
                adapters.Add(adapter);
                Logger.Info($"Added frontpanel adapter: {adapter.DisplayName}");
            }
            else
            {
                Logger.Warn($"Unknown frontpanel type, skipping: {frontpanel.GetType().Name}");
            }
        }

        return new FrontpanelHub(adapters);
    }

    private void DisposeContexts()
    {
        if (Contexts != null)
        {
            foreach (var ctx in Contexts)
                ctx?.Dispose();
            Contexts = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            if (IsStarted)
            {
                try
                {
                    // Perform synchronous cleanup to avoid blocking in Dispose
                    // StopAsync is now effectively synchronous
                    dcsBios?.Shutdown();
                    dcsBios = null;
                    DisposeContexts();
                    IsStarted = false;
                    Logger.Info("Bridge stopped during dispose");
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error stopping bridge during dispose");
                }
            }
        }

        _disposed = true;
    }
}
