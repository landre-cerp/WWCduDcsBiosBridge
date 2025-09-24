using McduDotNet;
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
        // TODO: Enable LED mapping UI when feature is ready
        //stackPanel.Children.Add(CreateLedCheckBoxes(deviceInfo.Cdu));

        scrollViewer.Content = stackPanel;
        tabItem.Content = scrollViewer;

        return tabItem;
    }

    private static UIElement CreateLedCheckBoxes(ICdu cdu)
    {
        var leds = cdu.DeviceId switch
        {
            DeviceIdentifier deviceId when deviceId.Device.ToString().Contains("Pfp7", StringComparison.OrdinalIgnoreCase) => new[] { "Fail", "Ofst" },
            _ => new[] { "Fm1", "Fm2" }
        };

        var ledGroup = new GroupBox
        {
            Header = "LED Status",
            Padding = new Thickness(10, 10, 10, 10),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var ledStackPanel = new StackPanel();

        foreach (var ledName in leds)
        {
            var checkBox = new CheckBox
            {
                Content = ledName,
                Margin = new Thickness(0, 2, 0, 2),
                IsChecked = false
            };

            ledStackPanel.Children.Add(checkBox);
        }

        ledGroup.Content = ledStackPanel;
        return ledGroup;
    }

    private static StackPanel CreateDeviceInfoSection(DeviceInfo deviceInfo)
    {

        var deviceInfoStack = new StackPanel();

        var description = new TextBlock
        {
            Text = "Draft, incoming features on this tab",
            Margin = new Thickness(0, 2, 0, 2)
        };

        
        deviceInfoStack.Children.Add(description);

        return deviceInfoStack;
    }


}