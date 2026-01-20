using WwDevicesDotNet;
using WwDevicesDotNet.Winctrl.FcuAndEfis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWCduDcsBiosBridge.UI;

/// <summary>
/// Factory class for creating device tabs in the UI
/// </summary>
public class DeviceTabFactory
{
    /// <summary>
    /// Creates a new device tab for the specified device
    /// </summary>
    /// <param name="deviceInfo">Device information</param>
    /// <param name="bridgeStarted">Whether the bridge is currently running</param>
    /// <returns>A configured TabItem</returns>
    public static TabItem CreateDeviceTab(
        DeviceInfo deviceInfo, 
        bool bridgeStarted)
    {
        var tabItem = new TabItem
        {
            Header = deviceInfo.DisplayName,
            Tag = deviceInfo
        };

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = new Thickness(10, 10, 10, 10)
        };

        var stackPanel = new StackPanel();

        stackPanel.Children.Add(CreateDeviceInfoSection(deviceInfo));
        
        // Add device-specific controls
        if (deviceInfo.Frontpanel != null)
        {
            stackPanel.Children.Add(CreateFcuTestSection(deviceInfo.Frontpanel));
        }
        else if (deviceInfo.Cdu != null)
        {
            // TODO: Enable LED mapping UI when feature is ready
            //stackPanel.Children.Add(CreateLedCheckBoxes(deviceInfo.Cdu));
        }

        scrollViewer.Content = stackPanel;
        tabItem.Content = scrollViewer;

        return tabItem;
    }

    private static UIElement CreateFcuTestSection(IFrontpanel frontpanel)
    {
        var fcuGroup = new GroupBox
        {
            Header = "FCU/EFIS Test Features",
            Padding = new Thickness(10, 10, 10, 10),
            Margin = new Thickness(0, 10, 0, 10)
        };

        var mainStack = new StackPanel();

        // Connection status
        var statusPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        statusPanel.Children.Add(new TextBlock { Text = "Connection Status: ", FontWeight = FontWeights.Bold });
        var statusText = new TextBlock 
        { 
            Text = frontpanel.IsConnected ? "Connected" : "Disconnected",
            Foreground = frontpanel.IsConnected ? Brushes.Green : Brushes.Red
        };
        statusPanel.Children.Add(statusText);
        mainStack.Children.Add(statusPanel);

        // Event monitor
        var eventGroup = new GroupBox
        {
            Header = "Event Monitor",
            Padding = new Thickness(5),
            Margin = new Thickness(0, 5, 0, 0)
        };

        var eventStack = new StackPanel();
        var eventLog = new TextBox
        {
            IsReadOnly = true,
            Height = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 10
        };

        var clearButton = new Button
        {
            Content = "Clear Log",
            Margin = new Thickness(0, 5, 0, 0),
            Padding = new Thickness(10, 2, 10, 2)
        };
        clearButton.Click += (s, e) => eventLog.Clear();

        // Subscribe to events
        frontpanel.ControlActivated += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                eventLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] PRESSED: {e.ControlId}\n");
                eventLog.ScrollToEnd();
            });
        };

        frontpanel.ControlDeactivated += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                eventLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] RELEASED: {e.ControlId}\n");
                eventLog.ScrollToEnd();
            });
        };

        frontpanel.Disconnected += (s, e) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                eventLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] DISCONNECTED\n");
                eventLog.ScrollToEnd();
                statusText.Text = "Disconnected";
                statusText.Foreground = Brushes.Red;
            });
        };

        eventStack.Children.Add(new TextBlock 
        { 
            Text = "Monitor button presses, releases, and rotary encoder movements:",
            Margin = new Thickness(0, 0, 0, 5)
        });
        eventStack.Children.Add(eventLog);
        eventStack.Children.Add(clearButton);

        eventGroup.Content = eventStack;
        mainStack.Children.Add(eventGroup);

        fcuGroup.Content = mainStack;
        return fcuGroup;
    }

    private static StackPanel CreateDeviceInfoSection(DeviceInfo deviceInfo)
    {
        var deviceInfoStack = new StackPanel();

        var deviceType = deviceInfo.Cdu != null ? "CDU Device" : "Frontpanel Device (FCU/EFIS)";
        var description = new TextBlock
        {
            Text = $"Device Type: {deviceType}",
            Margin = new Thickness(0, 2, 0, 2),
            FontWeight = FontWeights.Bold
        };

        deviceInfoStack.Children.Add(description);

        return deviceInfoStack;
    }
}