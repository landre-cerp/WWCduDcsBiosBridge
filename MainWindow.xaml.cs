using NLog;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WWCduDcsBiosBridge.Config;
using System.Threading;

namespace WWCduDcsBiosBridge;

public partial class MainWindow : Window, IDisposable, INotifyPropertyChanged
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private DcsBiosConfig config = new();
    private UserOptions userOptions = new();
    private readonly List<DeviceInfo> devices = new();

    private bool NeedsConfigEdit => !IsConfigValid();

    private bool _disposed = false;
    private BridgeManager? bridgeManager;
    private CancellationTokenSource? _detectCts;

    private string _statusMessage = "Ready.";
    private bool _statusIsError;

    public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(); } }
    public bool StatusIsError { get => _statusIsError; private set { _statusIsError = value; OnPropertyChanged(); } }

    public bool IsBridgeRunning => bridgeManager?.IsStarted == true;
    public bool CanEdit => !IsBridgeRunning;

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public MainWindow()
    {
        SetupLogging();
        InitializeComponent();
        LoadConfig();
        LoadUserSettings();
        _ = DetectDevicesAsync();
        UpdateState();
        Loaded += MainWindow_Loaded;
    }

    private bool IsConfigValid() => !string.IsNullOrWhiteSpace(config.DcsBiosJsonLocation);

    private async Task DetectDevicesAsync()
    {
        _detectCts?.Cancel();
        _detectCts = new CancellationTokenSource();
        devices.Clear();
        ShowStatus("Detecting devices...", false);

        try
        {
            var progress = new Progress<DeviceManager.DeviceDetectionProgress>(p =>
            {
                ShowStatus(p.Message, false);
            });
            var detected = await DeviceManager.DetectAndConnectDevicesAsync(progress, _detectCts.Token);
            devices.AddRange(detected);
            BuildDeviceTabs();
            UpdateStartButtonState();
        }
        catch (OperationCanceledException)
        {
            ShowStatus("Device detection cancelled", true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Async device detection failed");
            ShowStatus($"Device detection failed: {ex.Message}", true);
        }
    }

    private void BuildDeviceTabs()
    {
        if (devices.Count == 0)
        {
            ShowStatus("No CDU devices detected. Please ensure your device is connected.", true);
            return;
        }
        try
        {
            ShowStatus($"Detected {devices.Count} CDU device(s)", false);
            foreach (var deviceInfo in devices)
            {
                var deviceTab = UI.DeviceTabFactory.CreateDeviceTab(deviceInfo, IsBridgeRunning);
                MainTabControl.Items.Add(deviceTab);
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

    private void SetupLogging() => LogManager.ThrowConfigExceptions = true;

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
        if (IsBridgeRunning)
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

                if (IsBridgeRunning)
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
            await DetectDevicesAsync();
            UpdateState();
            if (devices.Count == 0) return;
        }

        UpdateUserOptionsFromUI();
        SaveUserSettings();

        StartButton.IsEnabled = false;
        StartButton.Content = "Starting...";

        try
        {
            bridgeManager = new BridgeManager();
            OnPropertyChanged(nameof(IsBridgeRunning));
            OnPropertyChanged(nameof(CanEdit));
            await bridgeManager.StartAsync(devices, userOptions, config);

            ShowStatus($"Bridge started successfully with {bridgeManager.Contexts?.Count ?? 0} device(s)!", false);
            StartButton.Content = "Bridge Running";
            StartButton.IsEnabled = false;
            OnPropertyChanged(nameof(IsBridgeRunning));
            OnPropertyChanged(nameof(CanEdit));
            Logger.Info("Bridge started successfully from WPF interface");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start bridge");
            ShowStatus($"Failed to start bridge: {ex.Message}", true);
            ResetStartButton();
        }
    }

    private void ResetStartButton()
    {
        StartButton.IsEnabled = !IsBridgeRunning && devices.Count > 0 && IsConfigValid();
        StartButton.Content = "Start Bridge";
        OnPropertyChanged(nameof(IsBridgeRunning));
        OnPropertyChanged(nameof(CanEdit));
    }

    private void LoadUserSettings() => userOptions = UserOptionsStorage.Load() ?? new UserOptions();
    private void SaveUserSettings() => UserOptionsStorage.Save(userOptions);

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }

    private void OptionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateUserOptionsFromUI();
        SaveUserSettings();
    }

    private void UpdateStartButtonState()
    {
        StartButton.IsEnabled = !IsBridgeRunning && IsConfigValid() && devices.Count > 0;
        if (!IsBridgeRunning && !(StartButton.Content?.ToString()?.Length > 0))
        {
            StartButton.Content = "Start Bridge";
        }
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

    private async void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        if (bridgeManager != null)
        {
            try
            {
                if (IsBridgeRunning)
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
                OnPropertyChanged(nameof(IsBridgeRunning));
                OnPropertyChanged(nameof(CanEdit));
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
            _detectCts?.Cancel();
            bridgeManager?.Dispose();
            SaveUserSettings();
            DeviceManager.DisposeDevices(devices);
        }

        _disposed = true;
    }
}