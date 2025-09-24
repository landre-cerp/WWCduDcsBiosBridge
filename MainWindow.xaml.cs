using NLog;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WWCduDcsBiosBridge.Config;
using WWCduDcsBiosBridge.UI;

namespace WWCduDcsBiosBridge;

public partial class MainWindow : Window, IDisposable
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private DcsBiosConfig? config;
    private UserOptions? userOptions;
    private bool needsConfigEdit = false;
    private bool _disposed = false;
    private List<DeviceInfo>? detectedDevices;
    private BridgeManager? bridgeManager;

    public MainWindow()
    {
        InitializeComponent();
        LoadUserSettings();
        SetupLogging();
        LoadConfig();
        UpdateOptionsUIFromSettings();
        UpdateStartButtonState();
        DetectAndCreateDeviceTabs();
        Loaded += MainWindow_Loaded;
    }

    private void DetectAndCreateDeviceTabs()
    {
        try
        {
            var tabsToRemove = MainTabControl.Items.Cast<TabItem>()
                .Where(tab => tab != ConfigurationTab)
                .ToList();

            foreach (var tab in tabsToRemove)
            {
                MainTabControl.Items.Remove(tab);
            }

            detectedDevices = DeviceManager.DetectAndConnectDevices();

            if (detectedDevices.Any())
            {
                ShowStatus($"Detected {detectedDevices.Count} CDU device(s)", false);

                foreach (var deviceInfo in detectedDevices)
                {
                    var deviceTab = DeviceTabFactory.CreateDeviceTab(
                        deviceInfo,
                        bridgeManager?.IsStarted ?? false);
                    MainTabControl.Items.Add(deviceTab);
                }
            }
            else
            {
                ShowStatus("No CDU devices detected. Please ensure your device is connected.", true);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to detect devices");
            ShowStatus($"Failed to detect devices: {ex.Message}", true);
        }
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
        if (bridgeManager?.IsStarted == true)
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

                if (bridgeManager?.IsStarted == true)
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
            if (bridgeManager?.IsStarted != true)
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

        if (detectedDevices == null || !detectedDevices.Any())
        {
            ShowStatus("No CDU devices found. Please ensure your device is connected and refresh.", true);
            DetectAndCreateDeviceTabs();
            return;
        }

        UpdateUserOptionsFromUI();
        SaveUserSettings();

        StartButton.IsEnabled = false;
        StartButton.Content = "Starting...";

        SetOptionsEnabled(false);
        SetDeviceTabsEnabled(false);
        ConfigButton.IsEnabled = false;

        try
        {
            bridgeManager = new BridgeManager();
            await bridgeManager.StartAsync(detectedDevices, userOptions ?? new UserOptions(), config);

            ShowStatus($"Bridge started successfully with {bridgeManager.Contexts?.Count ?? 0} device(s)!", false);
            StartButton.Content = "Stop Bridge";
            StartButton.IsEnabled = true;

            Logger.Info("Bridge started successfully from WPF interface");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start bridge");
            ShowStatus($"Failed to start bridge: {ex.Message}", true);
            ResetStartButton();
            throw;
        }
    }

    private async Task StopBridge()
    {
        StartButton.IsEnabled = false;
        StartButton.Content = "Stopping...";

        try
        {
            if (bridgeManager != null)
            {
                await bridgeManager.StopAsync();
                bridgeManager = null;
            }

            ShowStatus("Bridge stopped successfully!", false);
            StartButton.Content = "Start Bridge";
            StartButton.IsEnabled = true;

            SetOptionsEnabled(true);
            SetDeviceTabsEnabled(true);
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
    }

    private void SetDeviceTabsEnabled(bool enabled)
    {
        foreach (TabItem tabItem in MainTabControl.Items)
        {
            if (tabItem == ConfigurationTab) continue;

            if (tabItem.Content is ScrollViewer scrollViewer &&
                scrollViewer.Content is StackPanel stackPanel)
            {
                UIHelpers.SetChildControlsEnabled(stackPanel, enabled);
            }
        }
    }

    private void ResetStartButton()
    {
        StartButton.IsEnabled = true;
        StartButton.Content = (bridgeManager?.IsStarted == true) ? "Stop Bridge" : "Start Bridge";
        SetOptionsEnabled(bridgeManager?.IsStarted != true);
        SetDeviceTabsEnabled(bridgeManager?.IsStarted != true);
        ConfigButton.IsEnabled = bridgeManager?.IsStarted != true;
    }

    private void LoadUserSettings()
    {
        userOptions = UserOptionsStorage.Load() ?? new UserOptions();
    }

    private void SaveUserSettings()
    {
        if (userOptions != null)
        {
            UserOptionsStorage.Save(userOptions);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = isError ? Brushes.Red : Brushes.Green;
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
        CH47SingleCduSwitch.IsEnabled = enabled;
    }

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        // Simplifié : pas besoin de multiple dispose calls
        if (bridgeManager != null)
        {
            try
            {
                // Si démarré ou en cours de démarrage, arrêter proprement
                if (bridgeManager.IsStarted)
                {
                    await bridgeManager.StopAsync();
                }
                else if (bridgeManager.Contexts != null)
                {
                    // Si en cours de démarrage, nettoyer manuellement les écrans
                    foreach (var ctx in bridgeManager.Contexts)
                    {
                        try
                        {
                            ctx?.Mcdu?.Output?.Clear();
                            ctx?.Mcdu?.RefreshDisplay();
                        }
                        catch { }
                    }
                }
                
                bridgeManager.Dispose();
                bridgeManager = null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error stopping bridge during exit");
            }
        }

        // Pas besoin d'appeler Dispose() ici car OnClosed le fera
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        // Simplifié : dispose une seule fois
        if (!_disposed)
        {
            Dispose();
        }
        base.OnClosed(e);
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
            // Plus besoin de dispose le bridgeManager ici s'il est déjà null
            bridgeManager?.Dispose();
            SaveUserSettings();
            DeviceManager.DisposeDevices(detectedDevices ?? []);
        }

        _disposed = true;
    }
}