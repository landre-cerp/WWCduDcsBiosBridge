using DCS_BIOS;
using McduDotNet;
using NLog;
using System.Windows;
using WWCduDcsBiosBridge.Config;

namespace WWCduDcsBiosBridge;

public partial class MainWindow : Window, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private DcsBiosConfig? config;
    private DCSBIOS? dcsBios;
    private List<DeviceContext>? contexts;
    private UserOptions? userOptions;
    private bool bridgeStarted = false;
    private bool needsConfigEdit = false;
    private bool _disposed = false;


    public MainWindow()
    {
        InitializeComponent();
        LoadUserSettings();
        SetupLogging();
        LoadConfig();
        UpdateOptionsUIFromSettings();
        UpdateStartButtonState();
        Loaded += MainWindow_Loaded; 
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (needsConfigEdit)
        {
            OpenConfigEditor();
        }
    }

    private void SetupLogging()
    {
        LogManager.ThrowConfigExceptions = true;
    }

    private void LoadConfig()
    {
        try
        {
            config = ConfigManager.Load();
            if (config == null)
            {
                config = new DcsBiosConfig
                {
                    ReceiveFromIpUdp = "239.255.50.10",
                    SendToIpUdp = "127.0.0.1",
                    ReceivePortUdp = 5010,
                    SendPortUdp = 7778,
                    DcsBiosJsonLocation = ""
                };
                ConfigManager.Save(config);
                ShowStatus("Please edit DCS-BIOS config", true);
                needsConfigEdit = true; 
            }
        }
        catch (ConfigException)
        {
            ShowStatus("Please edit DCS-BIOS config", true);
            needsConfigEdit = true;
        }
        catch (Exception)
        {
            ShowStatus("Please edit DCS-BIOS config", true);
            needsConfigEdit = true;
        }
    }

    private void ConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (bridgeStarted)
        {
            ShowStatus("Cannot edit DCS-BIOS configuration while bridge is running.", true);
            return;
        }
        OpenConfigEditor();
    }

    private void OpenConfigEditor()
    {
        try
        {
            var configToEdit = config ?? new DcsBiosConfig();
            var configWindow = new ConfigWindow(configToEdit);
            configWindow.Owner = this;

            if (configWindow.ShowDialog() == true)
            {
                config = configWindow.Config;
                UpdateStartButtonState(); 

                if (!string.IsNullOrWhiteSpace(config.DcsBiosJsonLocation))
                {
                    ShowStatus("Configuration loaded. Ready to start bridge.", false);
                    needsConfigEdit = false;
                }
                else
                {
                    ShowStatus("Please edit DCS-BIOS config", true);
                    needsConfigEdit = true;
                }

                if (dcsBios?.IsRunning == true)
                {
                    ShowStatus("Configuration updated. Please restart the bridge for changes to take effect.", false);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open configuration editor");
            ShowStatus($"Failed to open configuration editor: {ex.Message}", true);
        }
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!bridgeStarted)
            {
                await StartBridge();
            }
            else
            {
                await StopBridge();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start/stop bridge");
            ShowStatus($"Failed to start/stop bridge: {ex.Message}", true);
            ResetStartButton();
        }
    }

    private async Task StartBridge()
    {
        if (config == null)
        {
            ShowStatus("Configuration not loaded. Please check the configuration settings.", true);
            return;
        }

        UpdateUserOptionsFromUI();
        SaveUserSettings();

        StartButton.IsEnabled = false;
        StartButton.Content = "Starting...";

        SetOptionsEnabled(false);
        ConfigButton.IsEnabled = false; 

        var devices = CduFactory.FindLocalDevices().ToList();
        if (!devices.Any())
        {
            ShowStatus("No CDU devices found. Please ensure your device is connected.", true);
            ResetStartButton();
            return;
        }

        contexts = devices.Select(dev => new DeviceContext(
            CduFactory.ConnectLocal(dev),
            userOptions ?? new UserOptions(),
            config)).ToList();

        foreach (var ctx in contexts)
            ctx.ShowStartupScreen();

        while (contexts.Any(c => c.SelectedAircraft == -1))
            await Task.Delay(100);

        InitDcsBios();

        foreach (var ctx in contexts)
            ctx.StartBridge();

        ShowStatus("Bridge started successfully!", false);
        bridgeStarted = true;
        StartButton.Content = "Stop Bridge";
        StartButton.IsEnabled = true;

        Logger.Info("Bridge started successfully from WPF interface");
    }

    private Task StopBridge()
    {
        StartButton.IsEnabled = false;
        StartButton.Content = "Stopping...";

        try
        {
            dcsBios?.Shutdown();
            dcsBios = null;

            DisposeContexts();

            ShowStatus("Bridge stopped successfully!", false);
            bridgeStarted = false;
            StartButton.Content = "Start Bridge";
            StartButton.IsEnabled = true;

            SetOptionsEnabled(true);
            ConfigButton.IsEnabled = true; 

            Logger.Info("Bridge stopped successfully from WPF interface");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error occurred while stopping bridge");
            ShowStatus($"Error stopping bridge: {ex.Message}", true);
            ResetStartButton();
            throw;
        }

        return Task.CompletedTask;
    }

    private void DisposeContexts()
    {
        if (contexts != null)
        {
            foreach (var ctx in contexts)
                ctx?.Dispose();
            contexts = null;
        }
    }

    private void ResetStartButton()
    {
        StartButton.IsEnabled = true;
        StartButton.Content = bridgeStarted ? "Stop Bridge" : "Start Bridge";
        SetOptionsEnabled(!bridgeStarted);
        ConfigButton.IsEnabled = !bridgeStarted;
    }

    private void LoadUserSettings()
    {
        userOptions = UserOptionsStorage.Load() ?? new UserOptions();
    }

    private void SaveUserSettings()
    {
        UserOptionsStorage.Save(userOptions);
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = isError ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Green;
    }

    private void OptionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateUserOptionsFromUI();
        SaveUserSettings();
    }

    private void UpdateStartButtonState()
    {
        bool configValid = config != null && !string.IsNullOrWhiteSpace(config.DcsBiosJsonLocation);
        StartButton.IsEnabled = configValid;
        StartButton.Content = "Start Bridge";
    }

    private void UpdateUserOptionsFromUI()
    {
        if (userOptions == null) userOptions = new UserOptions();
        userOptions.DisplayBottomAligned = DisplayBottomAlignedCheckBox.IsChecked ?? false;
        userOptions.DisplayCMS = DisplayCMSCheckBox.IsChecked ?? false;
        userOptions.LinkedScreenBrightness = CH47LinkedBrightnessCheckBox.IsChecked ?? false;
        userOptions.DisableLightingManagement = DisableLightingManagementCheckBox.IsChecked ?? false;
        userOptions.Ch47CduSwitchWithSeat = CH47SingleCduSwitch.IsChecked ?? false;
    }

    private void UpdateOptionsUIFromSettings()
    {
        if (userOptions == null) userOptions = new UserOptions();
        DisplayBottomAlignedCheckBox.IsChecked = userOptions.DisplayBottomAligned;
        DisplayCMSCheckBox.IsChecked = userOptions.DisplayCMS;
        CH47LinkedBrightnessCheckBox.IsChecked = userOptions.LinkedScreenBrightness;
        DisableLightingManagementCheckBox.IsChecked = userOptions.DisableLightingManagement;
        CH47SingleCduSwitch.IsChecked = userOptions.Ch47CduSwitchWithSeat;
    }

    private void SetOptionsEnabled(bool enabled)
    {
        DisplayBottomAlignedCheckBox.IsEnabled = enabled;
        DisplayCMSCheckBox.IsEnabled = enabled;
        CH47LinkedBrightnessCheckBox.IsEnabled = enabled;
        DisableLightingManagementCheckBox.IsEnabled = enabled;
    }

    private void InitDcsBios()
    {
        dcsBios = new DCSBIOS(config!.ReceiveFromIpUdp, config!.SendToIpUdp,
                             config!.ReceivePortUdp, config!.SendPortUdp,
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

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Dispose();
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        Dispose();
        base.OnClosed(e);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    // Protected virtual Dispose(bool disposing) method
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Stop bridge if it's running before disposing
            if (bridgeStarted)
            {
                try
                {
                    dcsBios?.Shutdown();
                    dcsBios = null;
                    bridgeStarted = false;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error shutting down bridge during dispose");
                }
            }

            DisposeContexts();
            SaveUserSettings();
        }

        // Free unmanaged resources here (if any)

        _disposed = true;
    }

}