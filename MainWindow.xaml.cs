using DCS_BIOS;
using McduDotNet;
using NLog;
using System.Windows;
using WWCduDcsBiosBridge.Config;

namespace WWCduDcsBiosBridge;

public partial class MainWindow : Window
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private DcsBiosConfig? config;
    private DCSBIOS? dcsBios;
    private List<DeviceContext>? contexts;
    private UserOptions? userOptions;
    private bool bridgeStarted = false;
    private bool needsConfigEdit = false;

    public MainWindow()
    {
        InitializeComponent();
        LoadUserSettings();
        SetupLogging();
        LoadConfig();
        UpdateOptionsUIFromSettings();
        UpdateStartButtonState();
        Loaded += MainWindow_Loaded; // Subscribe to Loaded event
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
                needsConfigEdit = true; // Set flag instead of opening editor
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

                // If config is now valid, update status
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
            ConfigButton.IsEnabled = false; // Disable config editing after bridge starts

            var devices = CduFactory.FindLocalDevices().ToList();
            if (!devices.Any())
            {
                ShowStatus("No CDU devices found. Please ensure your device is connected.", true);
                StartButton.IsEnabled = true;
                StartButton.Content = "Start Bridge";
                SetOptionsEnabled(true);
                ConfigButton.IsEnabled = true;
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

                StartButton.Content = "Bridge Running";
            ShowStatus("Bridge started successfully!", false);

            bridgeStarted = true;

            Logger.Info("Bridge started successfully from WPF interface");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start bridge");
            ShowStatus($"Failed to start bridge: {ex.Message}", true);
                StartButton.IsEnabled = true;
                StartButton.Content = "Start Bridge";
            SetOptionsEnabled(true);
            ConfigButton.IsEnabled = true;
        }
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

    // Call this method whenever DcsBiosJsonLocation changes
    private void UpdateStartButtonState()
    {
        StartButton.IsEnabled = config != null && !string.IsNullOrWhiteSpace(config.DcsBiosJsonLocation);
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
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        dcsBios?.Shutdown();
        SaveUserSettings();
        base.OnClosed(e);
    }
}