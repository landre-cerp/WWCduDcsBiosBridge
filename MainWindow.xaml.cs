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

    private DcsBiosConfig config = new();
    private UserOptions userOptions = new();
    private readonly List<DeviceInfo> devices = new();

    private bool NeedsConfigEdit => !IsConfigValid();

    private bool _disposed = false;
    private BridgeManager? bridgeManager;

    public MainWindow()
    {
        SetupLogging();
        InitializeComponent();
        LoadConfig();
        LoadUserSettings();
        DetectDevices();
        BuildDeviceTabs();
        UpdateState();
        Loaded += MainWindow_Loaded;
    }

    private bool IsConfigValid() => !string.IsNullOrWhiteSpace(config.DcsBiosJsonLocation);

    private void DetectDevices()
    {
        try
        {
            devices.Clear();
            var found = DeviceManager.DetectAndConnectDevices();
            if (found.Count > 0)
            {
                devices.AddRange(found);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to detect devices");
            devices.Clear();
        }
    }

    private void BuildDeviceTabs()
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

            if (devices.Count > 0)
            {
                ShowStatus($"Detected {devices.Count} CDU device(s)", false);

                foreach (var deviceInfo in devices)
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
            Logger.Error(ex, "Failed to build device tabs");
            ShowStatus($"Failed to build device tabs: {ex.Message}", true);
        }
    }

    private void UpdateState()
    {
        UpdateOptionsUIFromSettings();
        UpdateStartButtonState();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (NeedsConfigEdit)
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
            var loaded = ConfigManager.Load();
            if (loaded == null)
            {
                config = new DcsBiosConfig
                {
                    ReceiveFromIpUdp = "239.255.50.10",
                    SendToIpUdp = "127.0.0.1",
                    ReceivePortUdp = 5010,
                    SendPortUdp = 7778,
                    DcsBiosJsonLocation = string.Empty
                };
                ConfigManager.Save(config);
                ShowStatus("Please edit DCS-BIOS config", true);
            }
            else
            {
                config = loaded;
                if (!IsConfigValid())
                {
                    ShowStatus("Please edit DCS-BIOS config", true);
                }
            }
        }
        catch (ConfigException)
        {
            ShowStatus("Please edit DCS-BIOS config", true);
        }
        catch (Exception)
        {
            ShowStatus("Please edit DCS-BIOS config", true);
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
            var configWindow = new ConfigWindow(config);
            configWindow.Owner = this;

            if (configWindow.ShowDialog() == true)
            {
                config = configWindow.Config;
                UpdateState();

                if (IsConfigValid())
                {
                    ShowStatus("Configuration loaded. Ready to start bridge.", false);
                }
                else
                {
                    ShowStatus("Please edit DCS-BIOS config", true);
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
        await StartBridge();
    }

    private async Task StartBridge()
    {
        if (!IsConfigValid())
        {
            ShowStatus("Configuration not loaded. Please check the configuration settings.", true);
            return;
        }

        if (devices.Count == 0)
        {
            ShowStatus("No CDU devices found. Please ensure your device is connected and refresh.", true);
            DetectDevices();
            BuildDeviceTabs();
            UpdateState();
            if (devices.Count == 0) return;
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
            await bridgeManager.StartAsync(devices, userOptions, config);

            ShowStatus($"Bridge started successfully with {bridgeManager.Contexts?.Count ?? 0} device(s)!", false);
            StartButton.Content = "Bridge Running";
            StartButton.IsEnabled = false;

            Logger.Info("Bridge started successfully from WPF interface");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start bridge");
            ShowStatus($"Failed to start bridge: {ex.Message}", true);
            ResetStartButton();
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
        StartButton.Content = "Start Bridge";
        SetOptionsEnabled(bridgeManager?.IsStarted != true);
        SetDeviceTabsEnabled(bridgeManager?.IsStarted != true);
        ConfigButton.IsEnabled = bridgeManager?.IsStarted != true;
    }

    private void LoadUserSettings() => userOptions = UserOptionsStorage.Load() ?? new UserOptions();

    private void SaveUserSettings() => UserOptionsStorage.Save(userOptions);

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
        bool configValid = IsConfigValid();
        bool hasDevices = devices.Count > 0;
        StartButton.IsEnabled = configValid && hasDevices;
        StartButton.Content = "Start Bridge";
    }

    private void UpdateUserOptionsFromUI()
    {
        userOptions.DisplayBottomAligned = DisplayBottomAlignedCheckBox.IsChecked ?? false;
        userOptions.DisplayCMS = DisplayCMSCheckBox.IsChecked ?? false;
        userOptions.LinkedScreenBrightness = CH47LinkedBrightnessCheckBox.IsChecked ?? false;
        userOptions.DisableLightingManagement = DisableLightingManagementCheckBox.IsChecked ?? false;
        userOptions.Ch47CduSwitchWithSeat = CH47SingleCduSwitch.IsChecked ?? false;
    }

    private void UpdateOptionsUIFromSettings()
    {
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
        if (bridgeManager != null)
        {
            try
            {
                if (bridgeManager.IsStarted)
                {
                    await bridgeManager.StopAsync();
                }
                else if (bridgeManager.Contexts != null)
                {
                    foreach (var ctx in bridgeManager.Contexts)
                    {
                        try
                        {
                            ctx?.Mcdu?.Output?.Clear();
                            ctx?.Mcdu?.RefreshDisplay();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Error clearing or refreshing MCDU output during bridge shutdown");
                        }
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

        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
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
            bridgeManager?.Dispose();
            SaveUserSettings();
            DeviceManager.DisposeDevices(devices);
        }

        _disposed = true;
    }
}