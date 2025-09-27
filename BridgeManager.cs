using DCS_BIOS;
using NLog;
using WWCduDcsBiosBridge.Config;

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
            // Create device contexts
            Contexts = devices.Select(deviceInfo => 
                new DeviceContext(deviceInfo.Cdu, userOptions ?? new UserOptions(), config)).ToList();

            // Show startup screens
            foreach (var ctx in Contexts)
                ctx.ShowStartupScreen();

            // Wait for aircraft selection
            while (Contexts.Any(c => !c.IsSelectedAircraft))
                await Task.Delay(100);

            // Initialize DCS-BIOS
            InitializeDcsBios(config);

            // Start device bridges
            foreach (var ctx in Contexts)
                ctx.StartBridge();

            IsStarted = true;
            Logger.Info($"Bridge started successfully with {Contexts.Count} device(s)");
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
    public async Task StopAsync()
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

        await Task.CompletedTask;
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
                    StopAsync().GetAwaiter().GetResult();
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