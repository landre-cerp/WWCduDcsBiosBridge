using NLog;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using WWCduDcsBiosBridge.Config;
using System.Diagnostics;
using WWCduDcsBiosBridge.Services;
using WWCduDcsBiosBridge.Aircrafts;

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

    private const string GitHubOwner = "landre-cerp";
    private const string GitHubRepo = "WWCduDcsBiosBridge";

    // Dedicated update notification state
    private string? _updateMessage;
    private string? _updateUrl;
    private bool _isUpdateVisible;

    // Update service
    private readonly GitHubUpdateService _updateService = new(GitHubOwner, GitHubRepo);

    public string StatusMessage { get => _statusMessage; private set { _statusMessage = value; OnPropertyChanged(); } }
    public bool StatusIsError { get => _statusIsError; private set { _statusIsError = value; OnPropertyChanged(); } }
    public string? UpdateMessage { get => _updateMessage; private set { _updateMessage = value; OnPropertyChanged(); } }
    public string? UpdateUrl { get => _updateUrl; private set { _updateUrl = value; OnPropertyChanged(); } }
    public bool IsUpdateVisible { get => _isUpdateVisible; private set { _isUpdateVisible = value; OnPropertyChanged(); } }

    public bool IsBridgeRunning => bridgeManager?.IsStarted == true;
    public bool CanEdit => !IsBridgeRunning;

    public string AppVersion { get; }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool _isLoadingSettings = false;

    public MainWindow()
    {
        SetupLogging();
        InitializeComponent();

        AppVersion = AppVersionProvider.GetAppVersion();
        Title = $"WWCduDcsBiosBridge v{AppVersion}";

        LoadConfig();
        LoadUserSettings();
        _ = DetectDevicesAsync();
        UpdateState();
        Loaded += MainWindow_Loaded;
    }

    private void OnGlobalAircraftSelected(AircraftSelection selection)
    {
        // Forward the selection to the bridge manager
        bridgeManager?.SetGlobalAircraftSelection(selection);
        Logger.Info($"Global aircraft selected: {selection.AircraftId}, IsPilot: {selection.IsPilot}");
    }

    private void AircraftButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string tag)
            return;

        var selection = tag switch
        {
            "A10C" => new AircraftSelection(Aircrafts.SupportedAircrafts.A10C, true),
            "AH64D" => new AircraftSelection(Aircrafts.SupportedAircrafts.AH64D, true),
            "FA18C" => new AircraftSelection(Aircrafts.SupportedAircrafts.FA18C, true),
            "CH47_PLT" => new AircraftSelection(Aircrafts.SupportedAircrafts.CH47, true),
            "CH47_CPLT" => new AircraftSelection(Aircrafts.SupportedAircrafts.CH47, false),
            "F15E" => new AircraftSelection(Aircrafts.SupportedAircrafts.F15E, true),
            "M2000C" => new AircraftSelection(Aircrafts.SupportedAircrafts.M2000C, true),
            _ => null
        };

        if (selection != null)
        {
            // Update UI
            AircraftSelectionStatus.Text = $"Selected: {button.Content}";
            AircraftSelectionStatus.Foreground = System.Windows.Media.Brushes.Green;

            // Disable all aircraft buttons
            foreach (var btn in GlobalAircraftButtonGrid.Children.OfType<Button>())
            {
                btn.IsEnabled = false;
            }

            OnGlobalAircraftSelected(selection);
        }
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
            
            if (CanStartBridge() && userOptions.AutoStart)
            {
                Logger.Info("Auto-starting bridge...");
                await StartBridge();
            }
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

    private bool CanStartBridge()
    {
        return IsConfigValid() && devices.Count > 0 && !IsBridgeRunning;
    }

    private void BuildDeviceTabs()
    {
        if (devices.Count == 0)
        {
            ShowStatus("No devices detected. Please ensure your device is connected.", true);
            return;
        }
        try
        {
            var cduCount = devices.Count(d => d.Cdu != null);
            var frontpanelCount = devices.Count(d => d.Frontpanel != null);
            
            var statusParts = new List<string>();
            if (cduCount > 0)
                statusParts.Add($"{cduCount} CDU device{(cduCount != 1 ? "s" : "")}");
            if (frontpanelCount > 0)
                statusParts.Add($"{frontpanelCount} Frontpanel device{(frontpanelCount != 1 ? "s" : "")}");
            
            ShowStatus($"Detected {string.Join(" and ", statusParts)}", false);
            
            // Show global aircraft selection UI only if NO CDU devices
            if (cduCount == 0)
            {
                GlobalAircraftSelectionGroup.Visibility = Visibility.Visible;
                ShowStatus("No CDU detected. Start the bridge to select aircraft.", false);
                UpdateAircraftButtonState();
            }
            else
            {
                GlobalAircraftSelectionGroup.Visibility = Visibility.Collapsed;
            }
            
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

    private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        if (NeedsConfigEdit)
        {
            OpenConfigEditor();
        }

        try
        {
            await CheckForUpdatesAndNotifyAsync();
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to check GitHub for latest release");
        }
    }

    private void SetupLogging() => LogManager.ThrowConfigExceptions = true;

    private void LoadConfig()
    {
        var result = ConfigManager.TryLoad();
        result.Match(
            onSuccess: cfg =>
            {
                config = cfg;
                Logger.Info("Configuration loaded successfully.");
                return 0; // Unit equivalent
            },
            onFailure: error =>
            {
                StatusMessage = error;
                StatusIsError = true;
                Logger.Warn($"Configuration load failed: {error}");
                return 0;
            }
        );
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

        // Clear any previous error messages when starting
        ShowStatus("Starting bridge...", false);
        
        StartButton.IsEnabled = false;
        StartButton.Content = "Starting...";

        try
        {
            bridgeManager = new BridgeManager();
            OnPropertyChanged(nameof(IsBridgeRunning));
            OnPropertyChanged(nameof(CanEdit));
            
            var hasCdu = devices.Any(d => d.Cdu != null);
            if (!hasCdu)
            {
                foreach (var btn in GlobalAircraftButtonGrid.Children.OfType<Button>())
                {
                    btn.IsEnabled = true;
                }
                ShowStatus("Please select an aircraft to continue...", false);
            }
            
            await bridgeManager.StartAsync(devices, userOptions, config);

            ShowStatus($"Bridge started successfully with {bridgeManager.Contexts?.Count ?? 0} device(s)!", false);
            StartButton.Content = "Bridge Running";
            StartButton.IsEnabled = false;
            OnPropertyChanged(nameof(IsBridgeRunning));
            OnPropertyChanged(nameof(CanEdit));
            Logger.Info("Bridge started successfully from WPF interface");
            
            UpdateAircraftButtonState();
            
            if (userOptions.MinimizeOnStart)
            {
                WindowState = WindowState.Minimized;
                Logger.Info("Window minimized on bridge start as per user settings");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to start bridge");
            ShowStatus($"Failed to start bridge: {ex.Message}", true);
            ResetStartButton();
            ResetAircraftSelection();
        }
    }

    private void ResetStartButton()
    {
        StartButton.IsEnabled = !IsBridgeRunning && devices.Count > 0 && IsConfigValid();
        StartButton.Content = "Start Bridge";
        OnPropertyChanged(nameof(IsBridgeRunning));
        OnPropertyChanged(nameof(CanEdit));
    }

    private void ResetAircraftSelection()
    {
        AircraftSelectionStatus.Text = "Select an aircraft:";
        AircraftSelectionStatus.Foreground = System.Windows.Media.Brushes.White;
        
        UpdateAircraftButtonState();
    }

    private void UpdateAircraftButtonState()
    {
        var hasCdu = devices.Any(d => d.Cdu != null);
        var shouldEnableButtons = IsBridgeRunning && !hasCdu;
        
        foreach (var btn in GlobalAircraftButtonGrid.Children.OfType<Button>())
        {
            btn.IsEnabled = shouldEnableButtons;
        }
    }

    private void LoadUserSettings()
    {
        var result = UserOptionsStorage.TryLoad();
        if (result.IsSuccess)
        {
            userOptions = result.Value!;
            _isLoadingSettings = true;
            UpdateOptionsUIFromSettings();
            _isLoadingSettings = false;
        }
        else
        {
            Logger.Warn($"Failed to load user options: {result.Error}");
            userOptions = UserOptionsStorage.GetDefaultOptions();
        }
    }
    private void SaveUserSettings() => UserOptionsStorage.Save(userOptions);

    private void ShowStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusIsError = isError;
    }

    private void OptionCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoadingSettings)
        {
            return;
        }
        
        UpdateUserOptionsFromUI();
        SaveUserSettings();
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        OptionCheckBox_Changed(sender, e);
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
        userOptions.AutoStart = AutoStartCheckBox.IsChecked ?? false;
        userOptions.MinimizeOnStart = MinimizeOnStartCheckBox.IsChecked ?? false;
    }

    private void UpdateOptionsUIFromSettings()
    {
        _isLoadingSettings = true;
        
        DisplayBottomAlignedCheckBox.IsChecked = userOptions.DisplayBottomAligned;
        DisplayCMSCheckBox.IsChecked = userOptions.DisplayCMS;
        CH47LinkedBrightnessCheckBox.IsChecked = userOptions.LinkedScreenBrightness;
        DisableLightingManagementCheckBox.IsChecked = userOptions.DisableLightingManagement;
        CH47SingleCduSwitch.IsChecked = userOptions.Ch47CduSwitchWithSeat;
        AutoStartCheckBox.IsChecked = userOptions.AutoStart;
        MinimizeOnStartCheckBox.IsChecked = userOptions.MinimizeOnStart;
        _isLoadingSettings = false;
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

    private async Task CheckForUpdatesAndNotifyAsync()
    {
        try
        {
            var channel = AppVersionProvider.IsPreRelease(AppVersion) ? UpdateChannel.Prerelease : UpdateChannel.Stable;
            var result = await _updateService.CheckForUpdatesAsync(AppVersion, channel);
            if (result is { HasUpdate: true })
            {
                SetUpdateNotification($"New version available: {result.LatestTag}", result.HtmlUrl);
                Logger.Info($"New release available: {result.LatestTag} - {result.HtmlUrl}");
            }
        }
        catch (Exception ex)
        {
            Logger.Debug(ex, "Failed to check GitHub for latest release");
        }
    }

    private void SetUpdateNotification(string message, string? url)
    {
        UpdateMessage = message;
        UpdateUrl = url;
        IsUpdateVisible = true;
    }

    private void DismissUpdate_Click(object sender, RoutedEventArgs e)
    {
        IsUpdateVisible = false;
    }

    private void OpenUpdateLink_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UpdateUrl)) return;
        try
        {
            Process.Start(new ProcessStartInfo(UpdateUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Failed to open update URL");
        }
    }
}
